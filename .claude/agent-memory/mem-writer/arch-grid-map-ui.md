---
name: 网格地图UI架构
description: F133 ScrollRect可滚动缩放地图的设计决策：组件挂载位置、当前房间高亮策略、缩放机制
type: project
---

# 网格地图 UI 架构决策

**Why:** DungeonPanel 从线性列表切换到 2D 网格渲染时，做了几个非标准但关键的 UI 架构选择，影响后续地图相关功能的开发。
**How to apply:** 修改 DungeonPanel 地图渲染或新增类似 ScrollRect+网格面板时，参考这些决策。

## ScrollRect 挂载位置

ScrollRect 组件挂在 Viewport 对象上（MapPanel > Inner > Viewport），而非 BorderedPanel 外层。
- **原因**: 确保 ScrollRect 的拖拽事件和瓦片 Button 的点击事件正确传递，避免被外层面板拦截
- **层级**: MapPanel > Inner > Viewport(ScrollRect+Mask) > Content(瓦片池)

## CurrentOutline 独立设计

当前房间高亮边框是一个独立的 GameObject（"CurrentOutline"），覆盖在所有瓦片上方，不修改任何 tile 的结构。
- **结构**: 外层 Image 设为金色(ColorCurrentOutline)，内层 Image 设为 Color.clear，形成空心边框效果
- **位置跟随**: 每次 UpdateGridMap 时重新计算 anchoredPosition 和 sizeDelta
- **好处**: 瓦片对象池结构统一，高亮逻辑与瓦片着色逻辑完全解耦

## 滚轮缩放

- cellSize 范围 [60, 120]，步长 10，默认 80
- 在 Update() 中检测鼠标是否在 Viewport 范围内才响应滚轮
- 缩放后调用 UpdateGridMap 重算所有瓦片位置和 content 尺寸

## 房间着色方案

每种 DungeonEventType 对应固定颜色和图标文字（定义在 DungeonPanel 静态字段中）：
- Monster=深红/怪, Elite=亮红/精, Boss=深紫/王, Treasure=金/宝
- Spring=蓝/泉, Merchant=绿/商, Shrine=紫/神, Exit=灰白/梯, Empty=暗蓝灰
- 已清房间叠加灰色半透明覆盖层(ColorClearedOverlay)
