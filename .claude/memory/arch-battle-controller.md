---
name: BattleController轻量战斗控制器架构
description: F170 非MonoBehaviour单例+事件驱动(OnRoundComplete/OnBattleEnd)，封装ExecuteXxx，不引用UI类，作为BattleManager上层编排器
type: project
---

# BattleController 轻量战斗控制器（F170）

## 1. 设计定位

BattleController 不是重复实现战斗逻辑，而是 BattleManager 的上层编排器。它封装完整回合流程并产出结构化的 RoundResult/BattleResult 数据，使 UI 层未来可订阅事件替代直接调用 BattleManager。

**Why:** BattleManager 内嵌回合逻辑但耦合了日志字符串；UI（BattlePanel）通过 ExecuteRound 回调+过渡动画驱动。BattleController 提供纯逻辑、无 UI 依赖的事件驱动入口，支持 F171 地牢内联战斗等非 BattlePanel 场景。
**How to apply:** 新的战斗触发点（如地牢内联战斗、自动战斗测试）优先使用 BattleController.ExecuteXxx()，不再直接调用 BattleManager 的回合方法。

## 2. 类设计

- **非 MonoBehaviour 单例**: `BattleController.Instance`，纯 C# 类，不需要挂载 GameObject
- **零 UI 引用**: 不引用 BattlePanel 或任何 UI 类
- **两个事件**: `OnRoundComplete(RoundResult)` 和 `OnBattleEnd(BattleResult)`
- **三个入口**: `ExecuteAttack()` / `ExecuteSkill(int)` / `ExecuteItem(int)`

## 3. 调用链

```
BattleController.ExecuteXxx()
  -> BattleManager.RoundStart() (Tick+判定先手)
  -> playerAction() / MonsterAction() (根据先手顺序)
  -> BattleManager.RoundEnd() (DecayBuffs)
  -> BattleData.log 写入
  -> BuildAndFireResult():
       提取本回合新增日志 (logCountAtRoundStart 偏移量)
       构建 RoundResult (含双方HP/MP/日志)
       触发 OnRoundComplete
       若 isFinished -> 构建 BattleResult -> 触发 OnBattleEnd
```

## 4. 日志提取机制

`logCountAtRoundStart` 记录回合开始前 BattleData.log 的数量，BuildAndFireResult 用偏移量提取本回合新增日志条目。这是非侵入式的——不修改 BattleManager 的日志写入逻辑。

日志分类用 `IsPlayerActionLog()` 启发式判断（以"你"开头 vs 包含怪物名），分发到 playerActionLogs / monsterActionLogs。

## 5. 逃跑独立流程

ExecuteEscape() 与 BattleManager.TryEscape 保持一致的独立流程：
- RoundStart -> ActionEscape -> 失败时 MonsterAction
- `if (!b.isFinished) RoundEnd()` 保护：逃跑成功跳过 RoundEnd（与统一回合架构一致）
- 逃跑失败仍执行 RoundEnd（消耗回合 buff）

## 6. 结果数据模型

| 类 | 字段 | 用途 |
|----|------|------|
| RoundResult | isPlayerTurn, playerActionLogs, monsterActionLogs, roundLogs | 每回合完整数据 |
| RoundResult | battleEnded, isVictory, playerHp/Mp, monsterHp | 战斗状态快照 |
| BattleResult | isVictory, expReward, goldReward, rewardItems | 战斗结算数据 |
| BattleResult | isBossBattle, bossKillTitle, lostGold | Boss/失败特殊数据 |

## 相关文件

- `Assets/Scripts/Game/Battle/BattleController.cs`: 控制器本体
- `Assets/Scripts/Game/Battle/BattleManager.cs`: 被编排的底层战斗逻辑
- `Assets/Scripts/Game/Data/BattleData.cs`: 战斗数据（log/isFinished/isVictory 等）
