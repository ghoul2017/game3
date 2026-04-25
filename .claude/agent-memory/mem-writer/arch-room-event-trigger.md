---
name: 网格房间事件触发架构
description: F136-F137 房间内容触发分发机制：战斗回传OnShow延迟处理、出口确认/随机事件弹窗双选项、神殿技能筛选、空房间降级处理
type: project
---

# 网格房间事件触发架构 (F136)

**Why:** 房间事件触发涉及多个非直觉的时序和状态管理决策，尤其是战斗回传和出口确认的延迟处理机制。后续修改房间事件、新增房间类型、或修改面板栈交互时必须理解这些约束。
**How to apply:** 修改 TriggerRoomEvent 分发、新增房间类型 handler、或修改 DungeonPanel/BattlePanel 面板栈关系时，参考此文档理解回传时序和状态变量用法。

## 战斗回传：OnShow 延迟处理

BattlePanel 通过 `UIManager.ShowPanel<BattlePanel>()` 叠加在 DungeonPanel 之上（不是 ReplacePanel），因此 DungeonPanel 不会销毁。战斗结束后 BattlePanel 关闭，DungeonPanel 的 OnShow 被调用。

**时序链**:
```
HandleBattle -> 记录 pendingBattleTile = tile -> ShowPanel<BattlePanel>
  ... 战斗进行 ...
BattlePanel 关闭 -> DungeonPanel.OnShow()
  -> 检查 pendingBattleTile != null
  -> 检查 battle.isFinished && battle.isVictory
  -> 标记 tile.isCleared + room.cleared = true
  -> pendingBattleTile = null
  -> RefreshUI()
```

**关键点**: 战斗结果不能在 HandleBattle 中直接处理，因为战斗尚未开始；必须在 OnShow 中延迟处理。非战斗类事件（宝箱/泉水/神殿/空房）直接在 handler 中同步标记 isCleared。

## 出口确认：pendingExitFloor 状态变量

Exit 房间触发时：
1. **先标记 isCleared**（不等待确认）— 出口房间进入即视为已清理
2. 设置 `pendingMessage` 显示确认文字
3. 设置 `pendingExitFloor = nextFloor` 作为状态标记

`pendingExitFloor` 非 0 表示当前弹窗是出口确认（而非普通消息），点击确认时调用 `ConfirmEnterNextFloor()` 而非直接关闭消息。这避免了将普通宝箱/泉水消息误判为出口确认。

通关时 `pendingExitMaxFloor = int.MaxValue`（无尽模式）。

## 神殿（Shrine）技能筛选逻辑

```
全技能池 -> 排除已学技能 -> 随机选一个 -> 免费学习
```

- 使用 `ConfigManager.GetAllSkills()` 获取全量技能
- F146 起包含所有类型（含被动技能），不再排除 Passive 类型
- `player.LearnSkill(id)` 方法处理技能添加到已学列表
- **所有技能已学时回退为恢复 HP（maxHp/2）**，而非无操作或显示错误
- 学习后调用 `TryRefreshSkillLearnPanel()` 刷新可能打开的技能面板

## Merchant 事件

Merchant 类型直接 `ShowPanel<ShopPanel>()`，无额外逻辑。ShopPanel 自己处理商品展示和交易。

## 空房间随机事件 (F137)

MsgOverlay 弹窗复用机制扩展为**双选项模式**：在原有 BtnContinue 基础上新增 BtnOption2，通过 `pendingEvent` (EmptyRoomEvent?) 区分事件弹窗与普通消息。

**状态变量**:
- `pendingEvent` — 当前事件类型枚举，null 表示无事件（区分出口确认和普通消息）
- `pendingEventOption1` / `pendingEventOption2` — 两个选项的回调 Action
- `pendingEventTile` — 关联房间瓦片，事件完成后标记 isCleared

**弹出逻辑**: ShowMessage 内检查 `pendingEvent.HasValue`，若有则激活 BtnOption2 并设置两按钮文本和回调。两按钮回调均会清除 pendingEvent 状态。

**五种事件概率分配** (roll 阈值):
- GoldBag 40% | Consumable 20% | Trap 20% | WanderingMerchant 10% | MysticPower 10%

**降级处理**:
- 消耗品事件：背包已满时降级为少量金币补偿
- 流浪商人事件：无物品可卖时降级为纯文字提示（无购买选项）
- 陷阱伤害：百分比伤害保底 HP=1 不会致死

**概率注释不准确**: AssignRoomContent 中空房间实际概率为 `roll >= 0.74` = 26%（非注释中的16%），乘以50%触发率约13%。代码注释 `16%*50%=8%` 有误但未影响游戏体验。

## 已清房间标记模式

所有 handler 统一模式：`tile.isCleared = true` + `FindMatchingRoom(dungeon, tile).cleared = true`，然后 RefreshUI 刷新地图显示灰色覆盖层。这是双数据同步（新 grid + 旧 rooms）的最后一阶段，迁移完成后旧 rooms 同步行可删除。
