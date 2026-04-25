---
name: Boss战系统架构
description: F143 五层Boss设计、Boss战流程(警告弹窗->迎战->专属BGM->胜利奖励)、逃跑禁止、击杀记录、GetBossByFloor精确匹配陷阱
type: project
---

## Boss战系统架构 (F143)

**Why:** Boss战涉及跨多个系统的交互（地牢房间、战斗管理、UI面板、音频、数据持久化），且存在若干非直觉的设计决策（精确匹配陷阱、MsgOverlay复用、逃跑按钮双保险禁用）。修改Boss战流程或新增Boss时必须理解这些约束。

**How to apply:** 修改Boss配置、新增Boss、修改战斗流程中Boss相关逻辑、或修改DungeonPanel消息弹窗机制时，参考此文档理解完整的Boss战数据流和状态变量。

### 五层Boss配置

| 层 | Boss名 | id | HP | 特点 | 必掉装备id |
|----|--------|----|----|------|-----------|
| 5 | 暗影领主 | 2001 | 300 | 高攻+流血 | 1008 |
| 10 | 堕落骑士 | 2002 | 550 | 高防+眩晕+防御AI | 1009 |
| 15 | 深渊法师 | 2003 | 700 | debuff+沉默+易伤 | 2005 |
| 20 | 混沌巨兽 | 2004 | 1000 | 高血+高伤害+多阶段 | 2006 |
| 25 | 暗黑龙王 | 2005 | 1500 | 三阶段+全技能 | 3004 |

- Boss属性约为同层普通怪的 2.5 倍
- 所有Boss使用 `MonsterAIPattern.Boss`（AIManager三阶段策略，阈值60%/30%）
- 掉落常量：`GameConstants.BOSS_GOLD_FLOOR_MULTIPLIER=100`, `BOSS_EXP_FLOOR_MULTIPLIER=50`
- 额外奖励：floor*100金币 + floor*50经验 + 1件必掉稀有装备(chance=1f) + 1个随机宝物

### Boss战完整流程

```
DungeonPanel.HandleBattle(tile)
  -> tile.roomType == Boss ?
    -> pendingBossConfig = monsterCfg, pendingBossTile = tile
    -> pendingMessage = "强大的敌人出现了！..."
    -> msgBtnContinueLabel = "迎战"
    -> 显示MsgOverlay（复用单按钮模式，无新增UI）
  -> 玩家点击"迎战"(BtnContinue回调)
    -> 检查 pendingBossConfig != null
    -> 清除 pendingBossConfig/pendingBossTile/pendingMessage
    -> StartBattleWithConfig(dungeon, tile, cfg)
      -> BattleManager.StartBattle()
        -> isBossBattle = monster.isBoss
        -> 播放 Battle_BossWarning + Battle_BossBGM（专属BGM）
        -> BattlePanel.SetActionButtons() 中逃跑按钮不可用
  -> 战斗胜利 OnVictory()
    -> 停止BossBGM
    -> bossDefeated = true, bossKillTitle = "击败了 {name}！"
    -> 额外金币/经验加成
    -> 必掉装备（drops中chance=1f的条目）
    -> 额外随机宝物
    -> PlayerData.defeatedBosses.Add(m.id)（去重）
  -> 战斗失败 OnDefeat()
    -> 停止BossBGM
```

### 关键设计决策

1. **GetBossByFloor精确匹配+兜底**：先用 `minFloor == floor` 精确匹配，找不到再用 `minFloor <= floor` 取最近。避免floor=5时匹配到floor=10的Boss。

2. **MsgOverlay复用**：Boss警告复用DungeonPanel已有的消息覆盖层（单按钮模式），通过 `pendingBossConfig` 状态变量区分Boss警告与普通消息，避免新增UI组件。

3. **逃跑按钮双保险禁用**：
   - 战斗开始时 `SetActionButtons()` 检查 `isBossBattle` 设置 `btnEscape.interactable = false`
   - 回合切换时再次检查 `isBoss` 状态，防止逃跑按钮被重新启用

### Boss相关状态变量

| 变量 | 位置 | 用途 |
|------|------|------|
| `BattleData.isBossBattle` | 战斗数据 | 标记当前是否Boss战 |
| `BattleData.bossDefeated` | 战斗数据 | Boss是否被击败 |
| `BattleData.bossKillTitle` | 战斗数据 | 击杀标识文本 |
| `PlayerData.defeatedBosses` | 玩家数据 | 已击败Boss的id列表（持久化） |
| `DungeonPanel.pendingBossConfig` | 面板状态 | Boss警告确认前的Boss配置暂存 |
| `DungeonPanel.pendingBossTile` | 面板状态 | Boss警告确认前的瓦片暂存 |
| `MonsterConfig.isBoss` | 配置数据 | 标记怪物是否为Boss |

### Boss房间生成

在地牢生成阶段，`PlaceSpecialRooms` 中 `floor % 5 == 0` 时放置Boss房间。`GetMonstersByFloor` 使用 `!m.isBoss` 排除Boss，确保普通战斗不会抽到Boss。
