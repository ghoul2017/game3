---
name: 统一回合流程架构
description: F140 速度+状态效果集成后的统一回合流程：RoundStart->先手Action->后手Action->RoundEnd，兼容包装器，逃跑独立路径
type: project
---

# 统一回合流程（F140）

## 1. 回合四阶段

每个逻辑回合严格按照以下顺序执行，TickEffects 只执行一次（在 RoundStart 中）：

```
RoundStart  -> 先手方Action -> 后手方Action -> RoundEnd
(Tick双方     (根据isPlayerTurn     (另一方          (DecayBuffs
 +判定先手)    决定先后)             的Action)        旧buff衰减)
```

**Why:** F140之前每个Action内部各自调用TickEffects，导致每逻辑回合Tick两次（DoT双重伤害、duration双重递减）。统一后TickEffects只在RoundStart调用一次，语义正确。
**How to apply:** 所有战斗行动必须通过ExecuteRound(BattlePanel)或AutoBattleCoroutine触发，不要绕过RoundStart直接调用Action方法。

## 2. 两种调用路径

### 手动操作（BattlePanel）
```
UI按钮 -> OnAttack/OnUseSkill/OnUseItemById
       -> ExecuteRound(lambda)
       -> RoundStart (Tick+判定先手)
       -> SetActionButtons(false) + RefreshUI
       -> StartCoroutine(0.2s过渡动画)
       -> ExecuteRoundAfterTransition:
            if isPlayerTurn: playerAction() -> MonsterAction()
            else:            MonsterAction() -> playerAction()
       -> RoundEnd (DecayBuffs)
       -> RefreshUI + SetActionButtons(true)
```

### 自动战斗
```
AutoBattleCoroutine (while循环):
  -> RoundStart
  -> if isPlayerTurn: PlayerActionAttack() -> MonsterAction()
     else:            MonsterAction() -> PlayerActionAttack()
  -> RoundEnd
  -> RefreshUI
  -> WaitForSeconds(0.6s)
```

**Why:** 手动操作有0.2s回合过渡动画（先更新回合归属显示再执行），自动战斗无过渡直接执行。
**How to apply:** 新增行动类型时，在BattlePanel中通过ExecuteRound包装；自动战斗默认使用普攻。

## 3. 兼容包装器

旧方法保留为兼容入口，内部调用 RoundStart + 新Action方法：

| 旧方法 | 内部实现 |
|--------|---------|
| PlayerAttack() | RoundStart() + PlayerActionAttack() |
| PlayerSkill(id) | RoundStart() + PlayerActionSkill(id) |
| PlayerUseItem(id) | RoundStart() + PlayerActionUseItem(id) |
| MonsterTurn() | MonsterAction()（无RoundStart） |

**Why:** 保持外部调用方（如测试代码或未来NPC战斗）不需要改调用方式。MonsterTurn不含RoundStart因为怪物行动是回合后半段，Tick已在玩家行动前完成。
**How to apply:** 新代码优先使用ExecuteRound流程而非兼容包装器。兼容包装器可能造成Tick时序不一致（如果两个包装器在同一帧被连续调用）。

## 4. 逃跑独立路径

TryEscape() 不走ExecuteRound，而是自包含流程：
```
TryEscape():
  RoundStart() -> 检查isFinished
  -> ActionEscape()
  -> if (!isFinished): MonsterAction()  // 逃跑失败时怪物行动
```

**Why:** 逃跑失败后怪物行动是惩罚机制，且不需要RoundEnd（失败不消耗回合buff）。逃跑成功直接标记isFinished。
**How to apply:** 修改逃跑逻辑时注意不走ExecuteRound的RoundEnd。

## 5. Action方法中的Stun/Silence检查

每个Action方法（PlayerActionAttack/Skill/UseItem, MonsterAction, ActionEscape）内部自行检查Stun：
- Stun -> 跳过行动（返回提示文字）
- Silence -> 仅PlayerActionSkill检查，其他Action不受影响

**Why:** Stun在RoundStart的TickEffects递减后仍然生效（duration>0），所以需要在Action执行时再次检查。TickEffects只负责duration管理和DoT伤害，不负责行动阻断。
**How to apply:** 新增Action方法必须包含Stun检查（HasEffect(isPlayer, EffectType.Stun)），在行动逻辑最前面。

## 相关文件

- `BattleManager.cs`: RoundStart/RoundEnd/PlayerAction系列/MonsterAction/ActionEscape/TryEscape + 兼容包装器
- `BattlePanel.cs`: ExecuteRound/ExecuteRoundAfterTransition/AutoBattleCoroutine/OnEscape
