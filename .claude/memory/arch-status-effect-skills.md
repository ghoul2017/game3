---
name: 状态效果技能架构
description: F139 StatusAttack技能类型设计、数值计算、眩晕duration陷阱
type: project
---

## StatusAttack 技能类型

SkillType 新增 `StatusAttack` 枚举值，表示"先造成伤害，再附加状态效果"。与 Active/Buff 区分：Active=纯伤害，Buff=纯增益，StatusAttack=伤害+状态。

## 数值计算（CalcStatusEffectValue）

统一入口 `BattleManager.CalcStatusEffectValue(SkillConfig, int attackerAtk)`，根据 EffectType 分发：

| EffectType | value 计算 | 说明 |
|---|---|---|
| Bleed / Poison / Burn | `ATK * statusPowerRate`，最小1 | DoT 每回合伤害。Burn（F145新增）与流血/中毒共享相同Tick逻辑 |
| Vulnerable | `(int)(statusPowerRate * 100)` | 百分比数值（0.3 -> 30，即+30%受伤） |
| Stun / Silence | 0 | 无数值，只需 duration |

## 配置 duration 的 +1 陷阱

**关键**: TickEffects 在每回合**开头**执行（先递减 duration），然后才检查眩晕/沉默。

- 若希望眩晕实际生效 2 回合，配置 `statusDuration` 必须设为 **3**（不是 2）
- 原因：第1回合 TickEffects 递减到 2（生效），第2回合递减到 1（生效），第3回合递减到 0（过期移除）
- 配置示例：骑士重击"眩晕2回合" -> `statusDuration = 3`；法师封印"沉默2回合" -> `statusDuration = 3`

**绝不能在眩晕检查后手动 RemoveEffect** -- 那样 duration 配置完全失效，眩晕永远只生效 1 回合。正确做法是让 TickEffects 自然管理 duration 递减和过期移除。

## StatusAttack 与吸血之牙

StatusAttack 复用了 Active 技能的完整伤害计算路径（包括伤害计算、防御减免），吸血之牙等宝物效果同样生效。只是额外附加 `ApplyEffect()` 状态效果。

## 怪物使用状态技能

怪物技能池（monsterSkills）中加入 StatusAttack 类型技能，走 MonsterAction 中的技能使用路径，ApplyEffect 的 isPlayer 参数为 true（施加给玩家）。

## 相关文件

- `BattleManager.cs`: CalcStatusEffectValue, ExecutePlayerSkill/MonsterAction 中的 StatusAttack 分支
- `ConfigManager.cs`: 技能配置（1005割裂/2006毒刃/3005重击/5006封印/1006破甲）
- `ConfigData.cs`: SkillConfig 新增 statusPowerRate/statusEffect/statusDuration 字段
- `GameEnums.cs`: SkillType.StatusAttack/Debuff/DoT/Control, EffectType 新增 Bleed/Poison/Stun/Silence/Vulnerable/Burn
