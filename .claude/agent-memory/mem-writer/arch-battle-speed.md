---
name: 战斗速度系统
description: F130速度属性实装：回合顺序判定、速度闪避机制、加速/减速Buff、执行时序规则
type: project
---

# 战斗速度系统（F130）

## 1. 速度计算公式

**公式**: `speed = agility + speedBase + buffSpeed`

- `agility` — 职业基础敏捷（玩家）或怪物配置值
- `speedBase` — 职业固定速度系数（战士/骑士/牧师/法师=10，游侠=12）
- `buffSpeed` — 当前活跃的加速/减速Buff累计值（PlayerData.playerBuffSpeed / BattleMonster.buffSpeed）

**Why:** 游侠同时享有最高敏捷(12)和最高speedBase(12)，天然先手优势。部分高速怪物（如暗影狼speed=12）可以抢得先手。
**How to apply:** 新增职业或怪物时，speedBase 和 speed 配置直接决定回合顺序，需注意数值平衡。

## 2. 回合顺序判定

`BattleManager.DetermineTurnOrder()` 在以下时机调用：
- 战斗开始（StartBattle）
- 每轮玩家行动前（确保当前回合顺序正确）
- Buff 衰减导致速度变化后（重新判定回合归属）

判定结果存入 `isPlayerTurn`。F140起由统一回合流程管理（详见 arch-unified-round.md）：
- `isPlayerTurn=true` → 先执行 playerAction()，再执行 MonsterAction()
- `isPlayerTurn=false` → 先执行 MonsterAction()，再执行 playerAction()

**Why:** F140统一回合流程后，TickEffects只在RoundStart执行一次（之前每个Action各Tick一次导致双重Tick），回合顺序由BattlePanel.ExecuteRound统一编排。
**How to apply:** 修改战斗流程时，必须通过ExecuteRound或AutoBattleCoroutine触发，保持"每轮每人一次"的约束。

## 3. 速度闪避机制

`BattleManager.TrySpeedDodge(bool isPlayerAttacking)` 在攻击命中判定前调用：
- 速度差 = 攻击方速度 - 防守方速度
- 若速度差 < SPEED_DODGE_THRESHOLD(5)，不触发闪避
- 否则闪避率 = 速度差 x SPEED_DODGE_RATE_PER_POINT(2) / 100
- 例：速度差 10 → 20% 闪避率；速度差 25 → 50%（上限100%）

调用位置：
- PlayerAttack 中（判断怪物是否闪避玩家攻击）
- MonsterTurn 中（判断玩家是否闪避怪物攻击）

**Why:** 使用固定阈值+线性系数，简洁且有策略深度。高速度角色不仅先手，还有闪避优势。
**How to apply:** 调整闪避平衡性时修改 GameConstants 中的两个常量，不需要改逻辑代码。

## 4. 加速/减速技能

| 技能 | ID | 职业 | 效果 | Buff ID |
|------|-----|------|------|---------|
| 加速术 | 2005 | 游侠 | 速度+10，持续3回合 | 6 |
| 减速术 | 5005 | 法师 | 敌人速度-10，持续3回合 | 7 |

**Why:** 游侠获得加速保持先手优势+闪避加成；法师获得减速削弱敌人回合顺序+增加己方闪避概率。职业特色互补。
**How to apply:** 新增速效 Buff 时，在 ConfigManager 中配置 buffSpeed/debuffSpeed 和 buffTurns，BattleManager 的 Buff 衰减逻辑自动处理。

## 5. BattlePanel 显示

TurnInfo 区域显示当前回合归属（玩家先手/怪物先手）和双方速度值。速度值通过 `BattleManager.GetEffectivePlayerSpeed()` / `GetEffectiveMonsterSpeed()` 获取，含 Buff 加成。
