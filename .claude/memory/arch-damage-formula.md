---
name: 伤害公式与金币奖励机制
description: F150 数值平衡：伤害公式改为乘法防御模型、金币奖励按层缩放规则
type: project
---

## 伤害公式（F150 重调）

计算路径：`BattleManager.CalcDamage(atk, def)` / `CalcPlayerDamage(atk, def)`

```
baseDmg = max(1, atk - (int)(def * DEFENSE_REDUCTION_RATE))
variance = max(1, (int)(baseDmg * DAMAGE_VARIANCE_RATE))
result  = max(1, baseDmg + Random.Range(-variance, variance+1))
```

### 关键常量（GameConstants）

| 常量 | 值 | 说明 |
|------|----|------|
| DEFENSE_REDUCTION_RATE | 0.6f | 防御减伤系数（乘法模型） |
| DAMAGE_VARIANCE_RATE | 0.15f | 伤害浮动比例 |
| MIN_DAMAGE | 1 | 最小伤害 |

**Why:** F150 将 DEFENSE_DIVISOR(int=2, 除法模型) 改为 DEFENSE_REDUCTION_RATE(float=0.6, 乘法模型)。乘法模型更灵活可调，防御从50%减效改为60%减效。DAMAGE_VARIANCE_RATE 从 0.1f 调至 0.15f 增加战斗随机性。

**How to apply:** 调整数值平衡时修改 DEFENSE_REDUCTION_RATE 即可线性调整防御收益。不要再引入除法模型。旧名 DEFENSE_DIVISOR 已删除。

## 金币奖励缩放规则

在 `DungeonPanel.StartBattleWithConfig` 中：

- **普通/精英怪**: `goldReward = monsterCfg.goldReward * floor * rewardMul`
- **Boss**: `goldReward = monsterCfg.goldReward`（不乘 floor）

然后在 `BattleManager.OnVictory` 中：
- Boss 额外加成: `goldReward += floor * BOSS_GOLD_FLOOR_MULTIPLIER`
- 贪婪之戒: `goldReward *= 2`
- 幸运硬币宝物: `goldReward *= (1 + effectValue/100)`

**Why:** Boss 不在 DungeonPanel 中乘 floor，因为 BattleManager.OnVictory 已有独立的 floor 加成（`floor * BOSS_GOLD_FLOOR_MULTIPLIER`）。两个路径同时乘 floor 会导致指数级膨胀。

**How to apply:** 修改金币奖励时注意 Boss 和普通怪走不同缩放路径，不要合并。

## 需要同步更新的引用文件

`reference_game_ids.md` 中游戏常量部分仍记录旧值 `DEFENSE_DIVISOR=2` 和 `DAMAGE_VARIANCE_RATE=0.1f`，下次有权限时应更新为 `DEFENSE_REDUCTION_RATE=0.6f` 和 `DAMAGE_VARIANCE_RATE=0.15f`。
