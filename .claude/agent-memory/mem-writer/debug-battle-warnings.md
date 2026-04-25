---
name: 战斗系统待修复警告
description: F140验证阶段发现的代码问题：dodgeChance无上限、不必要的堆分配
type: project
---

# 战斗系统待修复警告（F140 验证）

## 1. TrySpeedDodge dodgeChance 无上限钳制

**位置**: `BattleManager.TrySpeedDodge()`

**问题**: `dodgeChance = speedDiff * SPEED_DODGE_RATE_PER_POINT / 100f` 无 MathF.Min(dodgeChance, 1f) 钳制。当速度差极大时，dodgeChance 可远超1.0，虽然 `Random.value < dodgeChance` 在>1时恒成立不影响正确性，但语义不严谨。

**修复建议**: 加一行 `dodgeChance = Mathf.Min(dodgeChance, 1f);`

## 2. new StatusEffect(...) 不必要的堆分配

**位置**: `BattleManager.ApplyEffect()` 和 `PlayerActionSkill()`/`MonsterAction()` 中的状态攻击分支

**问题**: `new StatusEffect(skill.statusEffect, effectValue, skill.statusDuration).GetName()` 仅用于获取效果名称，每次施法都产生一次不必要的GC分配。

**修复建议**: 将GetName改为静态方法 `StatusEffect.GetName(EffectType)` 不需要实例化，或使用switch映射。

## 3. rewardOverlay Find 无 null 保护

**位置**: `BattlePanel.BindReferences()` 第343行 `rewardOverlay = transform.Find("RewardOverlay").gameObject`

**问题**: 如果Find返回null，访问.gameObject会抛NullReferenceException。其他引用使用FindComponent<T>有内置null安全。

**修复建议**: 改为 `var t = transform.Find("RewardOverlay"); rewardOverlay = t != null ? t.gameObject : null;`

## 4. OnSkillMenu/OnItemMenu 缺少 null 检查

**位置**: `BattlePanel.OnSkillMenu()` 第576行 `skillPanel.gameObject.activeSelf`

**问题**: 直接访问 `skillPanel.gameObject` 而非先检查 `skillPanel != null`。OnItemMenu 同理。

**修复建议**: 添加 null guard: `if (skillPanel == null) return;`
