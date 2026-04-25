# Core 层编码规则（Assets/Scripts/Core/**）

## UIPanel 生命周期

```csharp
OnInit()     → BindReferences() → BindEvents()   // 预制体创建后调用一次
OnShow()     → RefreshUI()                        // 每次显示时调用
OnHide()                                           // 每次隐藏时调用
```

- **OnInit**: 只调用一次，绑定组件引用和事件
- **OnShow**: 每次面板显示时调用 → 调用 RefreshUI()
- **RefreshUI**: 只修改 `.text`/`.value`/`.color` 等属性，**零销毁零创建**

## 协程管理（F153 修复）

**重要：不要使用 StopAllCoroutines()**

```csharp
// 正确做法 — 只停过渡协程，不影响子类协程
if (transitionCoroutine != null)
{
    StopCoroutine(transitionCoroutine);
    transitionCoroutine = null;
}
transitionCoroutine = StartCoroutine(FadeInCoroutine());
```

- `transitionCoroutine` 字段用于管理面板过渡动画
- 子类协程（HP/MP条动画、状态图标、Boss震动）不被 Show/Hide 中断
- Show/Hide 只停 transitionCoroutine，用 StopTransition() 方法

### 遗留陷阱
1. **FadeOut 被 FadeIn 中断时 OnHide 不调用** — 子类仍需在 OnShow 重置状态
2. **displayValue = -1f 防御仍必要** — 首次 OnShow 无旧值可动画

## AudioManager 单例设计

### MonoBehaviour 单例而非纯C#单例
- AudioSource 必须挂载在 GameObject 上
- 采用 MonoBehaviour + DontDestroyOnLoad 模式
- 只有需要 AddComponent/Coroutine/AudioSource 的 Manager 才用 MonoBehaviour 单例

### AudioSource 轮播池（5通道）
- 预分配 5 个 AudioSource 组成轮播池（Round-Robin）
- `sfxIndex = (sfxIndex + 1) % sfxSources.Length`
- 避免动态创建/销毁触发 GC

### BGM Crossfade（F152）
- 使用 `Time.unscaledDeltaTime`（不受 timeScale 影响）
- 半时淡出+半时淡入策略（各占 duration 的 50%）
- 同曲跳过检测：`clip == current && isPlaying` 避免重复触发
- 切换 BGM 统一调用 `CrossfadeMusic()`，首次启动用 `PlayMusic()`

### 音效触发时序
- 技能音效放在被动技能检查和法力检查之后
- 被动技能无需音效，法力不足时不播放

### 初始化空引用防护
- SettingsPanel 滑块回调可能在 AudioManager 未初始化时触发
- 任何 Awake/Start 阶段可能被触发的 AudioManager 引用都需加空检查

## UIManager 栈管理

- `ShowPanel<T>()` — 压栈显示新面板
- `ReplacePanel<T>()` — 替换当前面板
- Hide 用 CanvasGroup（alpha=0），不用 SetActive(false)
- 避免破坏 RectTransform 层级
