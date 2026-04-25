---
name: 战争迷雾系统架构
description: F134 三态迷雾实现：Hidden/Explored/Visible状态定义、实时计算设计决策、InitVisibility时序陷阱、UI渲染层
type: project
---

# 战争迷雾系统架构

**Why:** 战争迷雾的状态计算采用了"实时计算而非持久化"的非直觉设计，且存在 InitVisibility 时序陷阱，后续修改移动/视野逻辑时必须理解这些决策。
**How to apply:** 修改地牢视野/移动/房间触发相关代码时，参考此文档理解迷雾状态流转和已知边界情况。

## 三态定义 (TileVisibility 枚举)

| 状态 | 含义 | UI 表现 |
|------|------|---------|
| Hidden | 未探索 | 纯黑不透明 `rgba(0,0,0,1)` |
| Explored | 去过但不在当前视野 | 黑色半透明 `rgba(0,0,0,0.6)` |
| Visible | 当前位置 + 四方向相邻 | 完全透明 `rgba(0,0,0,0)` |

## 设计决策：实时计算 vs 持久化

- **isExplored** (bool) 持久化存储在 DungeonTile 上 — 玩家是否曾经到过该格子附近
- **TileVisibility** 不持久化 — 通过 `GetTileVisibility(x, y)` 实时计算
- **计算逻辑**: 先检查是否在 currentPos 或四方向相邻 (Visible) -> 再检查 isExplored (Explored) -> 否则 Hidden
- **好处**: 避免三态字段与 isExplored 之间的状态同步问题；移动后无需批量更新旧视野的可见性状态

## 视野更新流程

```
玩家点击格子 → DungeonPanel.OnClickGridTile
  → dungeon.OnMoveTo(newPos)
    → MarkExploredAround(oldPos, newPos)  // 旧位置周围标 explored（排除新视野范围）
    → currentPos = newPos
    → SetVisibleArea(newPos)              // 新位置及四方向标 explored
  → RefreshUI → UpdateGridMap
    → dungeon.GetTileVisibility(x, y)     // 实时计算每个格子的可见性
    → 渲染 fogOverlay 颜色
```

## InitVisibility 时序陷阱

- `InitVisibility()` 在 `GenerateGrid()` 末尾调用
- 它会将**所有**格子的 isExplored 设为 false（包括之前在 GenerateGrid 中设置的 startTile.isExplored = true）
- 但随后 `SetVisibleArea(startPos)` 会重新标记起点及四方向为 explored
- **结论**: 在 GenerateGrid 中设置 isExplored 是徒劳的，InitVisibility 会清除。若后续在 InitVisibility 前添加其他 explored 标记，会被清掉。

## UI 渲染层

- 每个瓦片对象内有 `FogOverlay` GameObject（在所有内容之上，最顶层）
- FogOverlay 是一个全尺寸 Image，锚点铺满父对象，不接收射线检测 (raycastTarget=false)
- 仅修改 `.color` 属性实现迷雾效果，零创建零销毁
- Hidden 状态下额外隐藏 Overlay/Icon/TypeLabel 子对象
- Explored 状态下显示房间类型图标但叠加半透明黑
- Visible 状态下 fogOverlay 完全透明，所有内容正常显示
