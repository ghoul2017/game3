---
name: 状态效果系统架构
description: F138 StatusEffect框架：三类效果(DoT/控制/属性修正)、叠加规则、Tick时序、与旧buff共存设计
type: project
---

# 状态效果系统（F138）

## 1. 效果分类

| 类别 | EffectType | 作用 | 边界行为 |
|------|-----------|------|---------|
| DoT | Bleed, Poison, Burn | 每回合固定伤害 | 致死时检查复活被动；怪物DoT致死直接判胜 |
| 控制 | Stun | 跳过行动回合 | 同时禁用物品使用和逃跑（非仅技能） |
| 控制 | Silence | 无法使用技能 | 不影响普攻/物品/逃跑 |
| 属性修正 | BuffATK/DEF/Speed | 加成对应属性值 | 通过GetEffectiveATK/DEF/Speed叠加计算 |
| 易伤 | Vulnerable | 受到伤害增加百分比 | value为增幅百分比 |

**Why:** 每类效果的边界行为不同，Stun最严格（禁全部行动），Silence最宽松（仅禁技能）。DoT致死走独立的死亡判定路径，可触发复活。
**How to apply:** 新增效果类型时，必须明确：(1)属于哪一类别；(2)致死/阻断时是否需要特殊处理；(3)哪些行动被限制。

## 2. 叠加规则

同类型效果施加时（`ApplyEffect`）：
- `duration = Max(现有duration, 新duration)` — 取最长
- `value = 新value` — 取最新值覆盖
- 不会叠加两份同类效果

**Why:** 避免同类型效果无限堆叠导致数值爆炸。duration取max保证效果不会因重复施加而缩短。
**How to apply:** 若未来需要允许同类型叠加（如两层流血），需修改ApplyEffect的合并逻辑。

## 3. Tick时序

F140起，`TickEffects(isPlayer)` 只在 `RoundStart()` 中调用一次（双方各Tick一次）：
- DoT先结算伤害，再duration递减，再检查过期
- 反向遍历（i从Count-1到0），安全删除过期效果
- Tick后立即检查死亡，再检查Stun决定是否跳过行动

调用位置：`BattleManager.RoundStart()` 中 `TickEffects(true)` + `TickEffects(false)`。

每个Action方法（PlayerActionAttack/Skill/UseItem, MonsterAction, ActionEscape）内部不再调用TickEffects，仅检查Stun/Silence状态。

**Why:** F140之前每个Action各Tick一次导致双重Tick（DoT双重伤害、duration双重递减）。统一后每逻辑回合Tick一次，语义正确。
**How to apply:** 新增Action方法时，不要在内部调用TickEffects。Stun/Silence检查用 `HasEffect(isPlayer, EffectType.Stun/Silence)`。

## 4. 新旧系统共存

- **旧buff字段**: `playerBuffAttack/Defense/Speed` + `buffTurns`（BattleData和BattleMonster上）
- **新StatusEffect列表**: `playerEffects` / `monsterEffects`（List<StatusEffect>）
- `GetEffectiveATK/DEF/Speed` 同时读取两者：`基础值 + 旧buff + StatusEffect修正`
- 旧buff继续由原有衰减逻辑管理，新效果由TickEffects管理

**Why:** 渐进迁移策略，避免一次性重写所有buff逻辑。旧字段由技能配置的buffCfg驱动，新StatusEffect由ApplyEffect驱动。
**How to apply:** 当技能需要施加复杂效果（如DoT、控制）时用新的StatusEffect系统；简单的属性加成暂时可继续用旧buff。未来可统一迁移。

## 5. target标识设计

使用 `bool isPlayer` 标识目标，非枚举或接口：
- `isPlayer=true` → 操作 `playerEffects` / `PlayerData`
- `isPlayer=false` → 操作 `monsterEffects` / `BattleMonster`

**Why:** 与BattleData的playerXxx/monsterXxx对称设计一致，API签名简洁（4个方法都用isPlayer参数）。
**How to apply:** 所有StatusEffect相关方法（ApplyEffect/RemoveEffect/TickEffects/HasEffect/GetEffectModifier）均使用isPlayer参数，新增方法保持一致。

## 6. UI颜色映射（F142）

| EffectType | 颜色 | 色值 | 说明 |
|------------|------|------|------|
| Bleed | 红 | #e74c3c | 同UIBuilder.Red |
| Poison | 绿 | #58d68d | 同UIBuilder.Green |
| Stun | 黄 | #f0c040 | 同UIBuilder.Orange |
| Silence | 灰 | #958678 | 同UIBuilder.TextDim |
| Vulnerable | 橙 | #f09828 | 专用橙色（非UIBuilder常量） |
| BuffATK/DEF/Speed | 蓝 | #5dade2 | 同UIBuilder.Blue |

**Why:** 状态图标和战斗日志颜色需要视觉区分。大部分复用 UIBuilder 功能色，仅 Vulnerable 使用专用橙色 #f09828。
**How to apply:** 新增 EffectType 时，在 BattlePanel.GetEffectColor() 的 switch-case 中添加对应颜色映射，优先复用 UIBuilder 常量。
