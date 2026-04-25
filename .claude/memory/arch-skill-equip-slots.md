---
name: 技能系统改造（F160）
description: F160取消装备槽概念，learnedSkills即可用技能，容量6，equippedSkills保留[Obsolete]兼容旧存档
type: project
---

# 技能系统改造（F160）

**Why:** F146 的4装备槽设计增加了一层不必要的间接性（学习+装备两步），简化为"学习即可用"模型。equippedSkills 字段保留 [Obsolete] 标记用于旧存档反序列化兼容。
**How to apply:** 新代码直接使用 learnedSkills，不引用 equippedSkills。被动技能检查路径 HasPassive() 已改为查 learnedSkills。技能上限为6（MAX_SKILL_SLOTS）。

## 核心变更

- `MAX_SKILL_SLOTS` 从 4 改为 6
- `learnedSkills` 即为可用技能列表，不再有装备/卸下操作
- `equippedSkills` 字段标记 `[System.Obsolete]`，仅用于旧存档反序列化
- 新增 `ForgetSkill(id)` 方法支持遗忘腾出槽位

## 边界条件

- **槽满学习失败退还金币**: `SkillLearnPanel.LearnSkill()` 先扣金币，若 `player.LearnSkill()` 返回 false 则立即退还
- **被动技能不可主动使用**: `BattlePanel.PopulateSkillPanel()` 跳过 `SkillType.Passive`
- **被动触发路径**: `BattleManager.HasPassive()` → `player.learnedSkills.Contains()`（原走 equippedSkills，F160 统一改为 learnedSkills）

## 旧记忆（F146，已废弃）

F146 的4装备槽、0占位符、尾部裁剪、多消费者安全跳过模式已全部被 F160 取代。equippedSkills 列表不再有活跃消费者。
