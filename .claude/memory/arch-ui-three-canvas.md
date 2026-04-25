---
name: UIManager三层Canvas架构
description: F161 HUD/Window/Tip三层独立Canvas(sortingOrder:5/10/20)，面板缓存跨层互斥约束
type: project
---

## 三层 Canvas 架构（F161）

### 分层设计
| 层 | sortingOrder | Canvas变量 | 用途 | 新API |
|----|-------------|-----------|------|-------|
| HUD | 5 | canvasHUD | 永久固定布局 | ShowPanel（旧） |
| Window | 10 | canvasWindow | 非全屏窗口 | ShowWindow/CloseWindow |
| Tip | 20 | canvasTip | 浮动提示 | GetTipCanvas() |

- 每层独立 Canvas + CanvasScaler(1920x1080) + GraphicRaycaster
- 三个 Canvas 均 DontDestroyOnLoad

### 面板缓存与跨层约束

**关键约束**: panelCache 按类型名（typeof(T).Name）索引，同一面板类型不能同时存在于 HUD 栈和 Window 层。

- ShowWindow 先 CloseWindowInternal 旧窗口：WindowBase 子类 Destroy+清缓存，非 WindowBase 回退 Hide()（F163 改造）
- ShowPanel 先 Hide 栈顶面板（Stack 管理），再创建/复用面板
- 两者共享 panelCache，但管理方式不同（currentWindow vs panelStack），互不干扰

### GetOrCreatePanel 挂载机制
- 接受 parent 参数（决定面板挂到哪个 Canvas）
- 缓存命中时自动 SetParent(parent, false) 重新挂载到目标 Canvas
- 这意味着面板可以从 HUD 层移到 Window 层（反之亦然）

### 旧 API 兼容
- ShowPanel/Back/ReplacePanel 标记 [Obsolete]，建议用 ShowWindow/CloseWindow
- ClearAll() 同时清理 panelStack 和 currentWindow
- 旧 API 面板挂到 canvasHUD.transform，新 API 挂到 canvasWindow.transform
