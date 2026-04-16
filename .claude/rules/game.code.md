# Game 通用游戏逻辑编码规则

**路径**: `Assets/Scripts/Game/**`

包含：Battle、Config、Data、Dungeon、Item、Player、Town

## 配置加载

所有配置通过 `ConfigManager` 单例访问：

```csharp
// 获取职业配置
var classConfig = ConfigManager.Instance.GetClassConfig(classId);

// 获取技能配置
var skillConfig = ConfigManager.Instance.GetSkillConfig(skillId);
```

配置数据结构定义在 `ConfigData.cs`，枚举定义在 `GameEnums.cs`。

## 数据模型

所有数据模型在 `Assets/Scripts/Game/Data/`：

| 文件 | 用途 |
|---|---|
| `PlayerData.cs` | 玩家属性、等级、装备、技能 |
| `DungeonData.cs` | 地牢地图、房间状态 |
| `BattleData.cs` | 战斗状态、Buff、回合数 |
| `GameData.cs` | 整体存档结构 |

数据持久化通过 `DataManager` 的 JSON 序列化：

```csharp
// 保存
DataManager.Instance.SaveGame();

// 加载
var data = DataManager.Instance.LoadGame();
```

## 命名空间规范

| 目录 | 命名空间 |
|---|---|
| `Game/Battle/` | `Game.Battle` |
| `Game/Config/` | `Game.Config` |
| `Game/Data/` | `Game.Data` |
| `Game/Dungeon/` | `Game.Dungeon` |
| `Game/Item/` | `Game.Item` |
| `Game/Player/` | `Game.Player` |
| `Game/Town/` | `Game.Town` |

## 类命名规范

| 类型 | 前缀/后缀 | 示例 |
|---|---|---|
| Manager 单例 | `XXXManager` | `BattleManager`, `ConfigManager` |
| Data 数据类 | `XXXData` | `PlayerData`, `SkillData` |
| Config 配置类 | `XXXConfig` | `ClassConfig`, `SkillConfig` |
| Enum 枚举 | PascalCase | `BuffType`, `SkillType` |

## 禁止操作

- **禁止** 在数据模型中包含 Unity 组件引用 — 保持纯 C# 结构
- **禁止** 硬编码数值 — 放入 Config 或 Constants
- **禁止** 跨模块直接访问私有字段 — 通过公共方法/属性

## 代码风格

- 使用 PascalCase 命名公共方法和属性
- 使用 _camelCase 命名私有字段
- 每个文件一个类
- 文件名与类名一致
