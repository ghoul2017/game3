---
name: UIPanel协程动画时序陷阱与修复
description: UIPanel.Show()/Hide()不再使用StopAllCoroutines，改为只停transitionCoroutine，避免中断子类协程动画
type: project
---

# UIPanel 协程动画时序问题与修复（F142->F153）

## 旧问题（F142，已修复）

UIPanel.Show() 曾经调用 `StopAllCoroutines()`，会杀掉所有子类协程动画（HP/MP条、状态图标等）。子类需要通过 `displayValue = -1f` 等防御措施来应对。

## F153 修复方案

改为只停止面板过渡协程，不再使用 StopAllCoroutines：

```csharp
// Show() 中 — 只停过渡协程，不影响子类协程
if (transitionCoroutine != null)
{
    StopCoroutine(transitionCoroutine);
    transitionCoroutine = null;
}
transitionCoroutine = StartCoroutine(FadeInCoroutine());
```

UIPanel 新增 `transitionCoroutine` 字段和 `StopTransition()` 方法，Show/Hide 都只操作这一个协程。

## 仍需注意的陷阱

1. **FadeOutCoroutine 末尾调用 OnHide()**：如果 FadeOut 被新的 FadeIn 中断（如快速 Show->Hide->Show），OnHide 不会被调用。
2. **displayValue = -1f 防御仍然必要**：首次 OnShow 仍需直接赋值而非动画，但原因变为"无旧值可动画"而非"协程被杀"。

**Why:** StopAllCoroutines 是过度杀伤，会中断 BattlePanel 的 HP/MP 条动画、状态图标动画、Boss 震动等所有协程。改为精准停止只影响过渡效果。
**How to apply:** 子类协程不再被 Show()/Hide() 中断。但子类仍需在 OnShow 中重置 displayValue 等状态，因为面板切换时数值来源可能变化。
