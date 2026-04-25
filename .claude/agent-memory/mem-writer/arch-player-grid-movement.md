---
name: 玩家网格移动架构
description: F135 网格移动系统：连通性检查策略、移动锁机制、RefreshUI与动画时序、键盘输入守卫条件
type: project
---

# 玩家网格移动架构 (F135)

**Why:** 移动系统涉及多个非直觉的时序决策和输入守卫，后续修改移动/房间触发/UI刷新逻辑时必须理解这些约束。
**How to apply:** 修改 DungeonPanel 移动相关代码、新增移动方式（如自动寻路）、或修改协程动画时，参考此文档理解调用链和时序约束。

## 连通性检查策略

使用 DungeonTile 的 `connectUp/Down/Left/Right` 四方向布尔字段检查移动合法性，而非检查邻居 `tileType == Room`。
- **原因**: F132 建立的四方向连通标记精确描述了哪些方向可通行；两个 Room 格子可能不相邻，或被 Wall 隔开
- **结论**: 永远不要用"邻居格子是否为Room"来判断能否移动，必须用 connectXxx 字段

## 移动调用链与时序

```
TryMoveTo(direction)
  -> dungeon.OnMoveTo(newPos)        // 更新迷雾 + currentPos
  -> RefreshUI()                      // 立即刷新瓦片（迷雾/房间状态）
  -> SmoothMoveIndicator 协程(0.2s)   // 指示器平滑移动动画
  -> TriggerRoomEvent()               // 动画完成后触发事件
```

关键时序约束：
- **RefreshUI 在动画前调用** — 迷雾/瓦片状态立即更新，不等动画
- **TriggerRoomEvent 在动画后调用** — 事件（战斗、宝箱等）在视觉到达后才触发
- **UpdateGridMap 中 isMoving==true 时跳过指示器位置更新** — 避免协程动画被覆盖重置

## isMoving 移动锁

- `isMoving` 布尔字段在协程开始时设 true、结束时设 false
- 动画期间（0.2s）阻止所有移动输入：点击和键盘均无效
- 防止连续快速按键导致状态混乱

## 键盘输入守卫条件

HandleKeyboardMove 在三种情况下不响应：
1. `isMoving == true` — 动画进行中
2. `canvasGroup.alpha < 0.5` — 面板处于隐藏/半隐藏状态
3. `pendingMessage != null` — 消息覆盖层（宝物获得/事件提示）正在显示

**注意**: 第 2、3 条不是显而易见的，容易被遗漏。如果新增键盘输入场景，必须检查这三个前置条件。

## 已清房间 vs 未清房间

- 已清房间：仅移动 + 动画，不触发事件
- 未清房间：移动 + 动画 + 触发 OnRoomEnter 事件
