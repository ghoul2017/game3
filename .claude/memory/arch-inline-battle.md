---
name: 地牢内联战斗架构
description: F171 DungeonPanel订阅BattleController事件驱动内联战斗，首个非BattlePanel的战斗消费者，含防连点/面板禁用/被动技能回退等边界处理
type: project
---

# 地牢内联战斗（F171）

## 1. 设计定位

F171 是 BattleController（F170）事件驱动架构的**首个非 BattlePanel 消费者**，验证了 F170 "零 UI 引用 + 事件驱动" 的设计目标。DungeonPanel 直接订阅 BattleController 事件，不再经过 BattlePanel 中转。

**Why:** 地牢探索中点击怪物触发战斗是高频操作，跳过 BattlePanel 面板切换可减少 UI 层级跳转，提升体验流畅度。
**How to apply:** 未来新增战斗触发场景（如事件战斗、竞技场）可复用同一模式：订阅 BattleController 事件 + 自定义动画/结算逻辑。

## 2. 内联战斗流程

```
点击怪物 -> DungeonPanel.StartInlineBattle()
  -> BattleManager.StartBattle()（非 BattlePanel）
  -> 订阅 BattleController.OnRoundComplete + OnBattleEnd
  -> 玩家再次点击 -> ExecuteInlineAction()
  -> BattleController.ExecuteAttack/Skill()
  -> OnInlineRoundComplete -> RoundAnimationCoroutine
  -> OnBattleEnd -> HandleInlineBattleFinished
```

核心特点：**两次点击模式** -- 第一次点击触发战斗开始，第二次点击执行回合行动。这区别于 BattlePanel 的一次点击直接行动。

## 3. 边界处理（非显而易见）

| 场景 | 处理方式 |
|------|----------|
| 回合动画中防连点 | isRoundAnimating 标记，动画期间忽略输入 |
| 内联战斗中禁止操作 | 禁用返回城镇/背包/角色面板按钮 |
| 被动技能 | 回退到普通攻击（ExecuteAttack），不报错 |
| MP 不足 | 提示信息，不执行技能、不消耗回合 |
| 动画协程管理 | 精准停止（独立协程引用），不用 StopAllCoroutines |

## 4. 跨功能依赖

| 依赖 | 说明 |
|------|------|
| F170 BattleController | 事件源（OnRoundComplete/OnBattleEnd） |
| F169 地牢点击直交互 | 点击触发入口 |
| F162 GameHUD | ClearSkillSelection() 清除技能选择状态 |

## 相关文件

- Assets/Scripts/UI/DungeonPanel.cs: 内联战斗订阅、动画、结算
- Assets/Scripts/UI/GameHUD.cs: ClearSkillSelection 辅助方法
- Assets/Scripts/Game/Battle/BattleController.cs: 事件源
