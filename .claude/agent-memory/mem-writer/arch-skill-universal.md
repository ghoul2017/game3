---
name: 技能系统通用化与标签页架构
description: F144-F145技能全职业通用、7标签页分类（含Debuff/DoT/Control归入控制标签页）、技能ID三段分配、SkillPoolSize=48
type: project
---

# 技能系统通用化（F144-F145）

**Why:** F144 取消了技能学习的职业限制，改为按技能类型分7个标签页展示。F145 新增18个通用技能和3种新SkillType，扩大了技能池和标签页过滤逻辑。
**How to apply:** 后续涉及技能筛选、学习的功能应使用 GetSkillsByTab() 而非 GetSkillsByProfession()。新增技能类型时注意 Control 标签页的聚合逻辑。

## 设计决策

1. **职业限制取消**: 任何角色可学任何技能。初始创建职业仍给对应默认技能，但后续学习无限制。
2. **profession 字段保留不删除**: SkillConfig.profession 字段继续存在，避免破坏序列化兼容和 Boss 技能配置。GetSkillsByProfession 方法保留但不再被 UI 调用。
3. **Debuff/DoT/Control 统一归入"控制"标签页**: F145 新增 SkillType.Debuff/DoT/Control 三种类型，在标签页中统一展示在"控制"标签下，保持7标签页结构不变。战斗执行逻辑：DoT和Control与StatusAttack一致（造成伤害+施加状态），Debuff不造成伤害仅施加减益效果。

## 边界情况

### 技能ID三段分配
| 范围 | 用途 | 数量 |
|------|------|------|
| 1001-5006 | 职业专属技能（战士/游侠/骑士/牧师/法师） | 按职业分 |
| 6001-6018 | 通用技能（全职业可学，F145新增） | 18个，覆盖7种类型 |
| 9001+ | Boss专属技能 | 不可学习 |

- Boss 技能 ID >= 9000，GetAllSkills() 过滤掉 9000+ ID，不出现在技能学习列表中。

### BuffConfig 新增（F145）
| ID | 效果 | 数值 |
|----|------|------|
| 8 | ATK+20% | 属性修正 |
| 9 | DEF+25% | 属性修正 |
| 10 | SPD+10% | 属性修正 |

### 治疗技能双轨判断（陷阱）
- SkillType.Heal 枚举值是 F144 新增的，但**现有技能数据仍使用 isHeal=true + type=SkillType.Active** 的组合。
- GetSkillsByTab(SkillTab.Heal) 通过 `s.isHeal` 判断而非 `s.type == SkillType.Heal`。
- 新增治疗技能可以用 SkillType.Heal，但读取旧数据时必须兼容 isHeal 字段。

### 燃烧效果（Burn，F145新增）
- EffectType.Burn 与流血/中毒共享相同的 Tick 逻辑（每回合造成固定伤害），属于 DoT 类别。
- 在 GetEffectColor() 中需添加 Burn 对应颜色映射。

### 被动技能生效路径（F145新增）
- **法力汲取(6017)**: 在 PlayerActionSkill 的伤害计算后触发 MP 回复。
- **击杀回蓝(6018)**: 在 OnVictory() 中触发 MP 回复（非战斗中触发，而是在胜利结算时）。

## 架构

### 标签页分类（SkillTab 枚举）
| 标签 | 过滤逻辑 |
|------|----------|
| All | 全部（id < 9000） |
| Attack | !isHeal && type == Active |
| Heal | isHeal == true |
| Buff | type == Buff |
| Control | type == StatusAttack / Debuff / DoT / Control |
| Passive | type == Passive |
| Learned | learnedSkills 包含该技能ID |

### 调用链
```
SkillLearnPanel.RefreshUI()
  -> ConfigManager.GetSkillsByTab(tab, learnedSkills)
  -> GetAllSkills() 先过滤 id < 9000
  -> 按标签页 switch 过滤
  -> 对象池遍历显示（SkillPoolSize=48）
```

SkillPoolSize 从 32 扩大到 48，以容纳全部44个非Boss技能（26个职业专属 + 18个通用）。
