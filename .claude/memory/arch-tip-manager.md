---
name: TipManager浮动信息提示架构
description: F164 TipManager对象池(8个TipItem struct)+HoverTip(鼠标跟随单例)+AutoTip(定时淡出)+三层Canvas Tip层
type: project
---

## TipManager 浮动信息提示系统（F164）

### 挂载位置
- 由 UIManager.InitTipManager() 创建，挂载到 Canvas_Tip（sortingOrder=20）下
- MonoBehaviour 单例 + DontDestroyOnLoad（随 Canvas_Tip 一起不销毁）

### 两种 Tip 类型

| 类型 | 行为 | 数量约束 |
|------|------|---------|
| HoverTip | 鼠标跟随（每帧 Update）+ 偏移(15,-10) | 同时只有一个（hoverTipIndex 跟踪） |
| AutoTip | 定时淡出，固定位置 | 多个可共存（各自独立协程） |

### TipItem struct 设计决策
- **用 struct 而非 class**：只在 TipManager 内部使用，struct 数组无 GC 压力
- **陷阱**：修改 struct 的值类型字段（inUse/kind）后必须写回 `pool[idx] = tip`
- 引用类型字段（Text/CanvasGroup/RectTransform）的修改通过引用直接生效，无需写回
- 但 fadeCoroutine（Coroutine 引用类型）赋值后也需写回，因为同时伴随 inUse/kind 修改

### HoverTip 单例约束
- 重复调用 ShowHoverTip 时只更新内容（item.text.text = content），不新建 Tip
- hideTipIndex >= 0 表示已有 HoverTip，复用该槽位
- HideHoverTip 立即释放槽位并重置 hoverTipIndex = -1

### AutoTip 淡出时序
- duration 默认 2.5s，**fadeTime=0.3s 包含在 duration 内**
- 实际等待 `duration - fadeTime` 后开始淡出
- 淡出使用 `Time.unscaledDeltaTime`（不受 timeScale 影响）
- 池满时静默丢弃（FindFreeSlot 返回 -1，不报错）

### 池管理
- POOL_SIZE=8，预创建，SetActive 显隐
- ReleaseSlot：精准停 fadeCoroutine（不用 StopAllCoroutines）、隐藏、重置状态
- ResizeToFit：用 cachedTextGenerator 计算文字尺寸 + 内边距(20,12)
