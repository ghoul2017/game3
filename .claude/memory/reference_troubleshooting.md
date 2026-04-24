---
name: 常见问题排查与快速修复
description: UI文字不显示、面板错乱、属性计算错误等问题的原因和修复方法
type: reference
---

## 常见问题排查

### UI文字不显示
**原因**: Resources.GetBuiltinResource 生成的字体在运行时无法渲染中文
**修复**: UIPanel.FixFonts() 用 Font.CreateDynamicFontFromOSFont 替换

### 面板显示错乱
**原因**: 使用了 Object.DestroyImmediate 或 SetActive(false) 隐藏面板
**修复**: 用 CanvasGroup.alpha=0 隐藏面板

### 找不到预制体
**原因**: 路径不正确
**格式**: `Resources/UI/{PanelClassName}`，如 `Resources/UI/MainPanel`

### 属性计算错误
**原因**: RecalcStats() 未在属性变更后调用
**修复**: 装备/升级/获得宝物后必须调用 player.RecalcStats()

## 快速修复指南

| 问题 | 文件位置 | 修复方法 |
|------|----------|----------|
| 战斗属性不对 | PlayerData.RecalcStats() | 检查装备/宝物加成 |
| 技能无法释放 | BattleManager.PlayerSkill() | 检查MP和技能类型 |
| 存档失败 | DataManager.Load() | 检查JSON兼容性 |
| 面板绑定失败 | UIPanel.BindReferences() | 检查预制体路径和组件名 |
| 宝物不生效 | PlayerData.RecalcStats() | 检查treasures遍历 |

## 依赖关系
```
GameEntry → UIManager → 各面板
GameManager → Player(PlayerData) + Dungeon(DungeonData) + State(GameState)
BattleManager → CurrentBattle(BattleData) → Monster(BattleMonster)
ConfigManager → Professions + Skills + Items + Monsters + Treasures + Buffs
```
