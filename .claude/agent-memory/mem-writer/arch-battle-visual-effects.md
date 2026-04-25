---
name: 战斗视觉效果架构
description: F153 伤害浮动数字（日志驱动+去重）、Boss屏幕震动（信号传递+协程）、面板过渡动画
type: project
---

# 战斗视觉效果架构（F153）

## 伤害浮动数字

基于战斗日志（BattleData.log）驱动，在 RefreshUI 中自动解析新增日志触发浮动文字。

### 去重机制
- `lastProcessedLogCount` 记录上次处理到的日志索引
- OnShow 时重置为 0（重新开始追踪）
- RefreshUI 只处理 `newStart..log.Count` 范围内的新增条目
- 对象池：8 个 FloatingText 预创建，循环复用

### 颜色与位置
- 怪物受击伤害：显示在怪物信息区域上方
- 玩家受击伤害：显示在玩家信息区域下方
- 不同类型有不同颜色区分

**Why:** 浮动文字不能在日志写入时直接触发（时机不在 UI 层），必须在 RefreshUI 中从日志解析。但 RefreshUI 可能一帧被调用多次，去重防止同一伤害数字反复弹出。
**How to apply:** 新增浮动文字类型时，在 TriggerFloatingTextsFromLog 中添加日志关键字匹配分支，并选好颜色和锚点位置。

## Boss 屏幕震动

### 信号传递路径
```
BattleManager.MonsterAction()
  -> BattleData.bossSkillTriggered = true
  -> BattlePanel.RefreshUI() 检测并重置为 false
  -> TriggerBossShake()
  -> StartCoroutine（0.1s x 3次随机偏移 ±5px）
```

### 实现
- 操作 BattlePanel 自身的 RectTransform.localPosition
- 震动前保存原始位置，震动后恢复
- `shakeCoroutine` 独立管理，新旧震动互斥（新震动取消旧震动）

**Why:** bossSkillTriggered 是一次性信号（RefreshUI 消费后立即重置），不是持续状态。必须在 RefreshUI 中处理以确保 UI 线程安全。
**How to apply:** 新增类似"全屏闪白"等战斗视觉反馈时，复用同一信号模式：BattleManager 设标记 -> RefreshUI 检测并重置 -> 触发协程动画。
