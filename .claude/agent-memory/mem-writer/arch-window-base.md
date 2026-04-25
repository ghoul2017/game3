---
name: WindowBase可拖动窗口架构
description: F163 WindowBase抽象基类设计、生命周期、拖拽、UIManager窗口Destroy策略
type: project
---

## WindowBase 可拖动窗口架构（F163）

### 类设计
- `WindowBase : UIPanel, IDragHandler, IBeginDragHandler, IEndDragHandler`
- **abstract** `BuildContent(Transform)` — 强制子类实现内容构建
- **virtual** `GetWindowTitle()` / `GetWindowWidth()` / `GetWindowHeight()` — 提供默认值（"窗口"/700/500），允许覆盖
- **virtual** `OnWindowOpen()` / `OnWindowClose()` — 窗口打开/关闭回调

### 窗口布局结构
```
Root (居中锚定, pivot=0.5,0.5)
  └─ WindowContainer (BorderedPanel, 居中)
       ├─ TitleBar (顶部锚定, 40px, 可拖拽)
       │    ├─ TitleText (左对齐, Gold色)
       │    ├─ BtnClose (右上角, 30x30, 红色X)
       │    └─ Divider (底部金色分隔线, alpha=0.25)
       └─ Content (拉伸填满, offset top留出标题栏40px)
            └─ [子类 BuildContent 在此构建]
```

### 拖拽机制
- 仅标题栏区域可拖拽（`IsPointerOverTitleBar` 检测）
- `OnBeginDrag` 记录起始指针位置和窗口位置
- `OnDrag` 计算指针 delta 叠加到窗口位置
- 拖拽不限制窗口不超出屏幕边界

### 窗口生命周期
```
ShowWindow<T>() → CloseWindowInternal(旧窗口) → GetOrCreatePanel → panel.Show()
→ OnInit → BindReferences() → BuildWindowStructure() → BuildContent(子类)
→ OnShow → RefreshUI() → OnWindowOpen(子类)
```

### UIManager 窗口管理关键行为
- **WindowBase 子类关闭时 Destroy**（不缓存）: `CloseWindowInternal` 调用 `InvokeWindowClose()` → `panelCache.Remove` → `Object.Destroy`
- **非 WindowBase 面板回退到 Hide()**: 兼容旧 UIPanel 子类
- **单窗口互斥**: `currentWindow` 只允许同时存在一个窗口
- 关闭按钮调用 `UIManager.Instance.CloseWindow()`

### 子类实现要点
- 只需 override `BuildContent(Transform contentParent)` 构建内容区控件
- 可选 override `GetWindowTitle/Width/Height` 自定义窗口外观
- `RefreshUI()` 默认只更新标题文字，子类 override 扩展
