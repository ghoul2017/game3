# 方块与地下城 - 技术文档 (Knowledge Base)

> 本文档是 AI 助手的知识库，用于快速定位问题根源并进行修复。

---

## 1. 项目概览

| 项目 | 信息 |
|------|------|
| 项目名称 | 方块与地下城 (Block Dungeon) |
| 类型 | Unity 2D 地牢探索RPG游戏 |
| Unity版本 | 2021.3.26f1c1 |
| 渲染管线 | Universal Render Pipeline (URP) 12.1.11 |
| 目标框架 | .NET Framework 4.7.1 / C# 9.0 |
| 参考游戏 | 方块地牢2 |

---

## 2. 核心架构

### 2.1 项目结构

```
Assets/
├── Scripts/
│   ├── Core/           # 框架层 - 核心系统
│   ├── Game/           # 游戏层 - 业务逻辑
│   │   ├── Battle/     # 战斗系统
│   │   ├── Config/     # 配置系统
│   │   ├── Data/       # 数据模型
│   │   ├── Dungeon/    # 地牢系统(规划)
│   │   ├── Item/       # 物品系统(规划)
│   │   ├── Player/     # 玩家系统(规划)
│   │   └── Town/       # 城镇系统(规划)
│   └── UI/             # UI面板层
├── Resources/
│   ├── Prefabs/        # 通用预制体
│   └── UI/             # UI面板预制体
└── Scenes/             # 场景文件
```

### 2.2 命名空间

| 命名空间 | 用途 |
|----------|------|
| `Game.Core` | 核心框架 (UIManager, GameManager, UIBuilder等) |
| `Game.UI` | UI面板 (MainPanel, BattlePanel等) |
| `Game.Battle` | 战斗系统 (BattleManager) |
| `Game.Config` | 配置数据 (ConfigManager, 枚举, 配置类) |
| `Game.Data` | 游戏数据 (PlayerData, DungeonData等) |

---

## 3. 核心类速查表

### 3.1 框架层 (Core)

| 文件 | 职责 | 关键方法 |
|------|------|----------|
| `GameEntry.cs` | 游戏启动入口 | `Start()` - 初始化UI系统，显示MainPanel |
| `UIManager.cs` | UI单例管理 | `ShowPanel<T>()`, `ReplacePanel<T>()`, `Back()` |
| `UIPanel.cs` | 面板基类 | `OnInit()`, `OnShow()`, `OnHide()`, `BindReferences()`, `BindEvents()` |
| `UIBuilder.cs` | UI控件工厂 | `CreateButton()`, `CreateText()`, `CreateSlider()`, `CreatePoolRows()` |
| `PrefabLoader.cs` | 预制体加载 | `LoadPanelPrefab()`, `Instantiate()` |
| `GameManager.cs` | 游戏状态 | `SetState()`, `NewGame()`, `ContinueGame()`, `SaveGame()` |
| `DataManager.cs` | 存档管理 | `Load()`, `Save()`, `HasSave()` |
| `SettingsManager.cs` | 设置管理 | `Load()`, `Save()`, `ApplyFullscreen()`, `ApplyResolution()` |

### 3.2 游戏层 (Game)

#### 配置系统 (Config)

| 文件 | 职责 |
|------|------|
| `ConfigManager.cs` | 配置管理器，加载职业/技能/物品/怪物/宝物/Buff配置 |
| `ConfigData.cs` | 配置数据类 (ProfessionConfig, SkillConfig, ItemConfig等) |
| `GameEnums.cs` | 枚举定义 (Profession, ItemType, SkillType, DungeonEventType) |

#### 数据模型 (Data)

| 文件 | 类 | 关键字段 |
|------|-----|----------|
| `PlayerData.cs` | PlayerData | name, profession, level, exp, 属性(STR/AGI/INT/VIT), hp/mp, 装备, 背包, 技能, 宝物 |
| `DungeonData.cs` | DungeonData, DungeonRoom | floor, rooms[], currentRoomIndex |
| `BattleData.cs` | BattleData, BattleMonster | monster, isPlayerTurn, isFinished, log, playerBuff |

#### 战斗系统 (Battle)

| 文件 | 职责 | 关键方法 |
|------|------|----------|
| `BattleManager.cs` | 战斗逻辑 | `StartBattle()`, `PlayerAttack()`, `PlayerSkill()`, `MonsterTurn()`, `TryEscape()` |

### 3.3 UI面板 (UI)

| 面板 | 文件 | 职责 |
|------|------|------|
| MainPanel | 主菜单 | 开始游戏、继续冒险、设置、退出 |
| CreateRolePanel | 角色创建 | 选择职业、命名 |
| TownPanel | 城镇 | 休息、商店、技能学习、进入地牢、背包、角色信息 |
| DungeonPanel | 地牢探索 | 房间地图、状态栏、技能快捷栏 |
| BattlePanel | 战斗 | 怪物信息、战斗日志、操作按钮、技能/物品子面板 |
| BagPanel | 背包 | 物品列表、装备/使用 |
| ShopPanel | 商店 | 购买/出售物品 |
| SkillLearnPanel | 技能学习 | 学习新技能 |
| RoleInfoPanel | 角色信息 | 属性/装备/技能/宝物 |
| SettingsPanel | 设置 | 音量、全屏、分辨率、语言 |

---

## 4. UI架构规范

### 4.1 UIPanel 生命周期

```
Show() → OnInit() (首次) → OnShow() (每次)
Hide() → OnHide()
```

### 4.2 预制体模式（当前主流）

```csharp
public class XxxPanel : UIPanel
{
    // 1. 缓存组件引用
    private Button btnOk;
    private Text titleText;

    // 2. BindReferences - 绑定预制体中的组件
    protected override void BindReferences()
    {
        btnOk = FindComponent<Button>("BtnOk");
        titleText = FindComponent<Text>("TitleText");
    }

    // 3. BindEvents - 绑定事件
    protected override void BindEvents()
    {
        btnOk.onClick.AddListener(OnOk);
    }

    // 4. OnShow - 每次显示时刷新
    public override void OnShow()
    {
        RefreshUI();
    }

    // 5. RefreshUI - 零销毁刷新
    private void RefreshUI()
    {
        titleText.text = "新标题";  // 只修改属性
        btnOk.interactable = true;
    }
}
```

### 4.3 UI编码铁律

1. **OnInit()** 创建所有UI控件并缓存到字段
2. **RefreshUI()** 只修改 `.text` / `.value` / `.color` 等属性，**零销毁零创建**
3. **动态列表用对象池**：预创建行，`SetActive` 显隐，原地更新文字
4. **禁止使用 Object.DestroyImmediate** — 会导致文字消失、面板错乱
5. **Hide 用 CanvasGroup**（alpha=0），不用 `SetActive(false)` — 避免破坏 RectTransform 层级

---

## 5. 关键枚举速查

### 5.1 职业枚举 (Profession)

```csharp
Warrior  = 战士  // 近战物理输出，高攻击高生命
Ranger   = 游侠  // 远程物理输出，高暴击高闪避
Knight   = 骑士  // 近战坦克，高防御高生命
Priest   = 牧师  // 治疗辅助，高法力高恢复
Mage     = 法师  // 远程魔法输出，高魔法伤害
```

### 5.2 物品类型 (ItemType)

```csharp
Weapon      // 武器
Armor       // 防具
Accessory   // 饰品
Consumable  // 消耗品
Treasure    // 宝物/遗物
```

### 5.3 技能类型 (SkillType)

```csharp
Active   // 主动攻击技能
Passive  // 被动技能
Buff     // 增益技能
```

### 5.4 地牢事件类型 (DungeonEventType)

```csharp
Empty     // 空房间
Monster   // 普通怪物
Elite     // 精英怪物
Boss      // Boss战
Treasure  // 宝箱
Spring    // 泉水
Merchant  // 商人
Exit      // 出口/下一层
```

---

## 6. 配置ID速查

### 6.1 职业起始属性ID

| 职业 | 基础属性 |
|------|----------|
| 战士 | STR:12, AGI:6, INT:4, VIT:10 |
| 游侠 | STR:8, AGI:12, INT:6, VIT:6 |
| 骑士 | STR:8, AGI:4, INT:4, VIT:14 |
| 牧师 | STR:4, AGI:6, INT:12, VIT:8 |
| 法师 | STR:4, AGI:6, INT:14, VIT:6 |

### 6.2 技能ID范围

| 职业 | 技能ID范围 | 示例技能 |
|------|------------|----------|
| 战士 | 1001-1004 | 猛击(1001), 战吼(1002), 旋风斩(1003) |
| 游侠 | 2001-2004 | 精准射击(2001), 闪避姿态(2002) |
| 骑士 | 3001-3004 | 盾击(3001), 铁壁(3002) |
| 牧师 | 4001-4004 | 治疗术(4001), 祝福(4002) |
| 法师 | 5001-5004 | 火球术(5001), 冰甲(5002) |

### 6.3 物品ID范围

| 类型 | ID范围 | 示例 |
|------|--------|------|
| 武器 | 1001-1007 | 铁剑(1001), 短弓(1004), 法杖(1006) |
| 防具 | 2001-2004 | 皮甲(2001), 锁子甲(2002), 板甲(2003) |
| 饰品 | 3001-3003 | 力量戒指(3001), 敏捷护符(3002) |
| 消耗品 | 4001-4005 | 生命药水(4001), 法力药水(4003) |

### 6.4 怪物ID

| ID | 名称 | 层数 | 类型 |
|----|------|------|------|
| 1001 | 史莱姆 | 1层 | 普通 |
| 1002 | 骷髅兵 | 1层 | 普通 |
| 1003 | 蝙蝠 | 1层 | 普通 |
| 1004 | 哥布林 | 2层 | 普通 |
| 1005 | 暗影狼 | 3层 | 普通 |
| 2001 | 地牢守卫 | 5层 | Boss |
| 2002 | 堕落领主 | 10层 | Boss |

### 6.5 宝物ID

| ID | 名称 | 效果 |
|----|------|------|
| 1 | 勇者之证 | 攻击力+10% |
| 2 | 守护之盾 | 防御力+15% |
| 3 | 生命之泉 | 生命值+20% |
| 4 | 吸血之牙 | 攻击恢复10%伤害生命 |
| 5 | 幸运硬币 | 金币获取+30% |
| 6 | 经验宝石 | 经验获取+20% |

---

## 7. 存档系统

### 7.1 存档路径

```csharp
Path.Combine(Application.persistentDataPath, "save.json")
```

### 7.2 存档内容 (PlayerData)

- 角色名、职业、等级、经验
- 四维属性、战斗属性
- 装备、背包、已学技能、已装备技能
- 宝物、地牢进度、金币

---

## 8. 开发命令

### 8.1 开发脚本

```bash
dev.cmd          # 单次开发
devloop.cmd [N]  # 循环开发，默认8轮
```

### 8.2 关键规则

1. **每次只做一个功能** — 严禁一次实现多个
2. **严禁删除或修改 feature_list.json** — 只允许修改 passes 字段
3. **认真测试后才标记 passes: true**
4. **session结束时代码必须干净** — 可编译、可commit
5. **构建失败无法修复时回退** — `git checkout -- <file>`

---

## 9. 常见问题排查

### 9.1 UI文字不显示

**原因**：Resources.GetBuiltinResource 生成的字体在运行时无法渲染中文

**解决方案**：UIPanel.FixFonts() 方法用 Font.CreateDynamicFontFromOSFont 替换

### 9.2 面板显示错乱

**原因**：使用了 Object.DestroyImmediate 或 SetActive(false) 隐藏面板

**解决方案**：使用 CanvasGroup.alpha = 0 隐藏面板

### 9.3 找不到预制体

**原因**：预制体路径不正确

**路径格式**：`Resources/UI/{PanelClassName}`，如 `Resources/UI/MainPanel`

### 9.4 属性计算错误

**原因**：RecalcStats() 未在属性变更后调用

**解决方案**：装备、升级、获得宝物后必须调用 `player.RecalcStats()`

---

## 10. 依赖关系图

```
GameEntry
    └── UIManager
            ├── MainPanel
            ├── CreateRolePanel
            ├── TownPanel
            │       ├── DungeonPanel
            │       │       └── BattlePanel
            │       ├── BagPanel
            │       ├── ShopPanel
            │       ├── SkillLearnPanel
            │       ├── RoleInfoPanel
            │       └── SettingsPanel
            └── [其他面板...]

GameManager (单例)
    ├── Player (PlayerData)
    ├── Dungeon (DungeonData)
    └── State (GameState枚举)

BattleManager (单例)
    └── CurrentBattle (BattleData)
            └── Monster (BattleMonster)

ConfigManager (单例)
    ├── Professions (职业配置)
    ├── Skills (技能配置)
    ├── Items (物品配置)
    ├── Monsters (怪物配置)
    ├── Treasures (宝物配置)
    └── Buffs (Buff配置)
```

---

## 11. 快速修复指南

| 问题 | 文件位置 | 修复方法 |
|------|----------|----------|
| 战斗属性不对 | PlayerData.RecalcStats() | 检查装备/宝物加成是否正确应用 |
| 技能无法释放 | BattleManager.PlayerSkill() | 检查MP是否足够，类型是否正确 |
| 存档读档失败 | DataManager.Load() | 检查JSON序列化兼容性 |
| 面板绑定失败 | UIPanel.BindReferences() | 检查预制体路径和组件名称 |
| 宝物效果不生效 | PlayerData.RecalcStats() | 检查treasures列表遍历逻辑 |

---

---

## 12. ClaudeCode 开发规范与自动化

### 12.1 开发命令（`.claude/commands/`）

| 命令 | 文件 | 说明 |
|------|------|------|
| `/dev` | `dev.md` | 单次开发：读取进度→选功能→实现→提交→更新 |
| `/devloop [N]` | `devloop.md` | 循环开发，默认8轮/次，3连失败停止 |
| `/plan` | `plan.md` | 交互式需求拆解，产品经理角色 |
| `/init` | `init.md` | 项目首次运行初始化 |
| `/status` | `status.md` | 查看当前开发进度 |
| `/rc` | `rc.md` | review + commit |

### 12.2 Agent 定义（`.claude/agents/`）

| Agent | 文件 | 职责 |
|-------|------|------|
| `coding-agent` | `coding-agent.md` | 增量开发，每次只做1个功能，全自动 |
| `initializer` | `initializer.md` | 首次运行初始化 |
| `planner` | `planner-agent.md` | 需求拆解，交互式讨论确认 |

### 12.3 权限配置（`.claude/settings.local.json`）

```json
{
  "permissions": {
    "allow": ["Read", "Edit", "Write", "Glob", "Grep", "Agent", "Bash"],
    "deny": ["Bash(git push:*)", "Bash(rm -rf:*)"]
  }
}
```

### 12.4 Hook 配置（`.claude/settings.json`）

- **Stop Hook**: 自动调用 `.claude/scripts/build-check.sh` 编译检查
- Exit 0 = 编译成功，Exit 2 = 编译失败（Claude 继续修复）

### 12.5 Memory 索引（`.claude/memory/`）

| 文档 | 说明 |
|------|------|
| `game_design.md` | 游戏核心设计：职业/养成/玩法 |
| `square_dungeon2_analysis.md` | 参考游戏核心机制和数值设计 |
| `ref_project_style.md` | 4个参考项目共同架构模式 |
| `feedback_prefab_ui.md` | UI使用预制体决策记录 |

### 12.6 开发流程（coding-agent 规范）

```bash
# 1. 定位与读取
pwd → 读 progress.txt → 读 feature_list.json → git log

# 2. 选择功能
选第一条 passes=false 的功能

# 3. 实现
遵循 CLAUDE.md 约定，Unity项目无法命令行编译，代码自检

# 4. 测试
代码自检 + 集成检查 + 模式检查

# 5. 更新
feature_list.json 只改 passes 字段
progress.txt 追加记录

# 6. 提交
git add → git commit -m "feat: [F001] 功能描述"
```

### 12.7 核心约束

- **每次只做一个功能** — 严禁一次实现多个
- **严禁删除/修改 feature_list.json** — 只允许改 passes 字段
- **认真测试后才标记 passes: true**
- **session结束时代码必须干净** — 可编译、可commit
- **回复使用中文，commit message使用中文**
- **禁止 git push** — 推送由用户自行决定

### 12.8 UI面板重写铁律

1. `OnInit()` 中创建所有UI控件并缓存到字段
2. `RefreshUI()` 只修改 `.text`/`.value`/`.color` 等属性，**零销毁零创建**
3. 动态列表用对象池：预创建行，`SetActive` 显隐，原地更新文字
4. **绝不使用 Object.DestroyImmediate** — 会导致文字消失
5. Hide 用 `CanvasGroup.alpha=0`，不用 `SetActive(false)`

---

## 13. 预制体迁移记录

### 13.1 已迁移面板 (10/10 ✅)

| 面板 | 迁移ID | 预制体路径 |
|------|--------|------------|
| MainPanel | F102 | Resources/UI/MainPanel |
| CreateRolePanel | F102 | Resources/UI/CreateRolePanel |
| TownPanel | F102 | Resources/UI/TownPanel |
| BagPanel | F103 | Resources/UI/BagPanel |
| RoleInfoPanel | F104 | Resources/UI/RoleInfoPanel |
| ShopPanel | F105 | Resources/UI/ShopPanel |
| SkillLearnPanel | F106 | Resources/UI/SkillLearnPanel |
| SettingsPanel | F107 | Resources/UI/SettingsPanel |
| DungeonPanel | F108 | Resources/UI/DungeonPanel |
| BattlePanel | F109 | Resources/UI/BattlePanel |

### 13.2 PrefabGenerator 使用

```csharp
// Assets/Editor/PrefabGenerator.cs
// 批量生成所有面板预制体
[MenuItem("Tools/Generate All UI Prefabs")]
public static void GenerateAllPrefabs()
```

---

## 14. 游戏数值常量 (GameConstants)

### 14.1 战斗常量

| 常量 | 值 | 用途 |
|------|-----|------|
| `MONSTER_SKILL_CHANCE` | 0.3f | 怪物技能触发概率 |
| `ESCAPE_CHANCE` | 0.5f | 逃跑成功率 |
| `DEFEAT_GOLD_LOSS_RATE` | 0.1f | 失败金币损失比例 |
| `DAMAGE_VARIANCE_RATE` | 0.1f | 伤害浮动比例 |
| `MIN_DAMAGE` | 1 | 最小伤害 |
| `DEFENSE_DIVISOR` | 2 | 防御减伤系数 |

### 14.2 地牢常量

| 常量 | 值 | 用途 |
|------|-----|------|
| `TREASURE_CHANCE` | 0.4f | 宝箱宝物概率 |
| `CONSUMABLE_DROP_CHANCE` | 0.3f | 消耗品掉落概率 |
| `MIN_ROOMS` | 5 | 房间最小数量 |
| `MAX_ROOMS` | 15 | 房间最大数量 |

### 14.3 宝物ID常量

| 常量 | 值 | 宝物名称 |
|------|-----|----------|
| `TREASURE_ID_LIFESTEAL` | 4 | 吸血之牙 |
| `TREASURE_ID_LUCKY_COIN` | 5 | 幸运硬币 |
| `TREASURE_ID_EXP_GEM` | 6 | 经验宝石 |

---

*最后更新: 2026-04-14*
*维护者: ClaudeCode → CodeBuddy*
*版本: v1.2.0 (预制体迁移 + 常量提取)*
