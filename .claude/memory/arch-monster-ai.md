---
name: 怪物AI模式架构
description: F141+F147 五种AI行为模式、怪物分配(28种)、Boss三阶段阈值、决策调用链、怪物mp陷阱与楼层分布
type: project
---

## 怪物AI模式系统 (F141)

### 五种AI模式与怪物分配

| 模式 | 策略 | 对应怪物 |
|------|------|----------|
| Aggressive | 70%最高伤技能 / 30%普攻 | 骷髅兵、蝙蝠、暗影狼、暗黑法师 |
| Defensive | HP<50%时40%治疗buff/30%普攻/30%其他；HP>=50%走均衡 | 石像鬼、重甲兵 |
| Debuffer | 玩家无debuff时70%上debuff；已有则60%普攻/40%技能 | 毒蛇、暗影法师 |
| Balanced | 50%普攻 / 50%随机技能 | 史莱姆、哥布林 |
| Boss | HP>60%走Aggressive；30-60%混合Debuffer+Defensive；<30%全技能+伤害+20% | 两个Boss |

### 架构设计

- **AIManager单例**：与BattleManager同层，职责分离
- **AIDecision返回值**：解耦决策与执行，BattleManager根据action字段分派普攻或技能
- **damageBonus**：乘法叠加到现有dmgMultiplier上（Boss狂暴阶段=1.2x）
- **Boss三阶段阈值**：60%和30%两条HP分界线，基于monster.hp / monster.maxHp

### 边界处理

- **无技能怪物**：skillIds为null或空时直接返回普攻（Balanced默认路径）
- **MP不足**：hasMonsterSkillMp=false时直接返回普攻，由外部m.mp > 0判定传入
- **重复debuff**：IsDuplicateDebuff检查玩家已有同类型状态效果，避免重复施加StatusAttack技能
- **降级策略**：各模式查找技能返回null时，统一降级为普攻

### 调用链

```
BattleManager.MonsterAction()
  -> AIManager.Instance.DecideAction(monster, battle, hasMonsterSkillMp)
    -> switch(aiPattern) -> DecideXxx策略方法
    -> 返回 AIDecision
  -> BattleManager根据action类型执行普攻或技能
    -> damageBonus乘法叠加到最终伤害: rawDmg * dmgMultiplier * decision.damageBonus
```

### 怪物 mp 字段陷阱 (F147 BUG FIX)

**关键**: `BattleManager.MonsterAction()` 中 `hasMonsterSkillMp` 由 `m.mp > 0` 决定。`MonsterConfig.mp` 是 int 类型，默认值为 0。如果怪物配置了 `skillIds` 但未显式设置 `mp` 字段，则 `m.mp` 为 0，`m.mp > 0` 为 false，AI 永远判定无 MP 可用技能，所有技能永远不会释放。

**规则**: 任何有 `skillIds` 的怪物必须显式设置 `mp` 字段（如 `mp = 30`）。没有技能的怪物可省略 `mp`（走纯普攻路径）。

### 怪物楼层分布 (F147)

28种普通怪物按楼层区间分布（不含5个Boss）：

| 区间 | 层 | 特点 | 代表怪物 |
|------|----|------|----------|
| 入门 | 1-4 | 低属性、Balanced/Aggressive AI | 史莱姆、骷髅兵、蝙蝠 |
| 法术引入 | 5-9 | DoT/Debuffer AI出现 | 暗黑法师、暗影法师、丧尸 |
| 高伤控制 | 10-14 | Debuff+Control技能组合 | 亡灵骑士、火元素、暗影刺客 |
| 强力debuff | 15-19 | 多技能组合、高属性 | 冰霜巨人、魅魔、深渊之眼 |
| 终局挑战 | 20-25 | Boss级属性+三技能、maxFloor=0 | 远古龙裔、虚空行者、暗黑龙将 |

- `maxFloor=0` 表示无上限（20+层怪物设为0，可在任意高楼出现）
- `GetMonstersByFloor(floor)` 过滤条件: `minFloor <= floor && (maxFloor == 0 || maxFloor >= floor) && !isBoss`
