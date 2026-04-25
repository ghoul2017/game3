---
name: Logger日志系统架构
description: F112 统一日志系统：等级分类规则、Conditional条件编译设计决策
type: project
---

## Logger 日志系统（F112）

位于 `Assets/Scripts/Core/Logger.cs`，静态类，命名空间 `Game.Core`。

### 日志等级分类规则

| 等级 | 方法 | 适用场景 |
|------|------|----------|
| Debug | `LogD(tag, msg)` | 面板生命周期/初始化细节，仅 Editor 输出 |
| Info | `LogI(tag, msg)` | 业务流程（战斗/地牢/存档） |
| Warning | `LogW(tag, msg)` | 异常但非致命（组件查找失败/无存档） |
| Error | `LogE(tag, msg)` | 致命错误（Canvas不存在/存档失败） |

### 设计决策：[Conditional("UNITY_EDITOR")] 而非 #if

LogD 使用 `[Conditional("UNITY_EDITOR")]` 特性而非 `#if UNITY_EDITOR` 预处理指令包裹调用点。

**Why:** Conditional 特性由编译器在调用端直接移除整个方法调用，比 #if 更干净 -- 不需要在每个调用点都写 #if/#endif，也不会遗留悬挂代码。行为等价但代码更整洁。

**How to apply:** 新增仅 Editor 使用的日志方法时同样使用 [Conditional] 特性；不要在调用点用 #if 包裹 Logger.LogD 调用。
