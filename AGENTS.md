# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## 项目概述

这是一个基于 Unity 的 2D 地牢探索类游戏项目（参考"方块地牢2"）。

- **Unity 版本**: 2021.3.26f1c1（安装路径: `C:\Program Files\Unity\Hub\Editor\2021.3.26f1c1`）
- **渲染管线**: Universal Render Pipeline (URP) 12.1.11，使用 2D 渲染器
- **项目模板**: Universal 2D Template (com.unity.template.universal-2d@1.1.4)
- **目标框架**: .NET Framework 4.7.1 / C# 9.0

## 构建与运行

此项目使用 Unity Editor 进行构建和运行，无命令行构建脚本。在 Unity Editor 中打开 `E:\worku\game3` 目录即可。

Unity 项目文件（`.csproj` / `.sln`）由 Unity Editor 自动生成，不应手动修改。

## 项目结构

```
Assets/
  Scripts/
    Core/           - 框架层（UIManager, UIPanel, UIBuilder, PrefabLoader, GameManager, DataManager, SettingsManager, GameEntry）
    Game/
      Battle/       - 战斗系统（BattleManager）
      Config/       - 配置系统（ConfigManager, ConfigData, GameEnums, GameConstants）
      Data/         - 数据模型（PlayerData, DungeonData, BattleData, GameData）
    UI/             - UI面板（MainPanel, CreateRolePanel, TownPanel, DungeonPanel, BattlePanel, ShopPanel, BagPanel, SkillLearnPanel, RoleInfoPanel, SettingsPanel）
  Editor/           - 编辑器工具（PrefabGenerator）
  Resources/
    UI/             - UI面板预制体（10个面板）
    Prefabs/        - 通用预制体
doc/
  feature_list.json   - 功能清单（JSON格式，Agent 工作流核心文件）
  progress.txt        - 跨 session 进度记录
  参考项目/            - 参考资料，方块地牢2截图和策划分析
```

## 自动化开发工作流

基于 [Anthropic 长时间运行 Agent 最佳实践](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)，采用三 Agent 架构：

### 核心文件

| 文件 | 用途 |
|---|---|
| `doc/feature_list.json` | 功能清单（JSON 格式，每个功能有 id/category/desc/passes） |
| `doc/progress.txt` | 跨 session 进度记录 |
| `.Codex/agents/dev-worker.md` | 编码 Agent（代码实现，读写+搜索+Bash） |
| `.Codex/agents/verify-feature.md` | 功能验证 Agent（只读，验证需求覆盖） |
| `.Codex/agents/verify-quality.md` | 质量验证 Agent（只读，审查代码质量） |
| `.Codex/agents/mem-writer.md` | 知识归档 Agent（读写+搜索，归档开发知识） |

### 开发流程

```
/plan          → 交互式：讨论需求 → 拆解为原子功能 → 追加到 feature_list.json
/init          → 首次：初始化环境、验证 features.json
/dev           → 单次：读取进度 → 实现一个功能 → 测试 → commit
/devloop       → 循环：连续执行 /dev，带配额守卫（8轮/session，3连失败停止）
/status        → 查看当前进度
/rc            → review + commit
```

### 每个 session 标准流程（/dev）

```
pwd → 读 progress.txt → 读 feature_list.json → git log
→ 选第一条 passes=false 的功能
→ 实现 → 代码自检 → 更新 feature_list.json（只改 passes 字段）
→ git commit → 更新 progress.txt → 输出 session 报告
```

### 关键规则

- **每次只做一个功能** — 严禁一次实现多个
- **严禁删除或修改 feature_list.json 中的条目** — 只允许修改 passes 字段
- **认真测试后才标记 passes: true**
- **session 结束时代码必须干净** — 可编译、可 commit
- **构建失败无法修复时回退** — `git checkout -- <file>`

### 无人值守脚本

```bash
dev.cmd          # 单次开发（非交互模式）
devloop.cmd [N]  # 循环开发，默认 8 轮
```

## 核心架构决策

### UI 架构（F111 模块拆分）

UIBuilder 拆分为三个独立模块，UIBuilder 保留为门面：

| 模块 | 职责 |
|------|------|
| `UIColors` | 所有 UI 颜色常量的唯一定义处 |
| `UIFactory` | 控件创建方法（自动应用深色主题） |
| `UIPool` | 对象池（PoolRow + CreatePoolRows） |
| `UIBuilder` | 门面转发 + 布局辅助（SetStretch/SetAnchored） |

- **颜色唯一来源**: `UIColors`，严禁硬编码 `Color.white`/`Color.gray`/`new Color(...)`
- UI 配色: RPG 风格深色主题 — 深蓝/紫底色 `#0d0d1a`/`#1a1a2e` + 金色强调色 `#c8a24c`
- **完整色板见 `.Codex/rules/ui.code.md`**

### 音频系统

- **AudioManager**: MonoBehaviour 单例 + DontDestroyOnLoad
- **SFX**: 5 通道 AudioSource 轮播池（Round-Robin），避免 GC
- **BGM**: Crossfade 半时淡出+半时淡入，`Time.unscaledDeltaTime` 不受暂停影响
- 场景音乐流转: TownPanel/DungeonPanel/BattleManager 的 OnShow/生命周期中触发

### 战斗系统

- **统一回合流程**: RoundStart(Tick) → 先手Action → 后手Action → RoundEnd(DecayBuffs)
- **TickEffects 只在 RoundStart 执行一次**（F140 修复双重 Tick）
- **伤害公式**: 乘法防御模型 `DEFENSE_REDUCTION_RATE=0.6` + 浮动 `DAMAGE_VARIANCE_RATE=0.15`
- **速度体系**: 决定回合顺序 + 速度差闪避机制
- **Boss 战**: 五层 Boss（层 5/10/15/20/25）+ 专属 BGM + 逃跑禁用
- **完整规则见 `.Codex/rules/battle.code.md`**

### 网格地牢系统（F131-F136）

- **数据模型**: GridPos(自定义struct) + DungeonTile(四方向连通) + ContentData
- **生成算法**: 随机游走扩张 + 多尺寸房间 + BFS 连通性保障
- **迁移策略**: 旧 rooms 与新 grid 双数据同步，逐功能分阶段迁移
- **战争迷雾**: 三态实时计算（Hidden/Explored/Visible），isExplored 持久化
- **完整规则见 `.Codex/rules/game.code.md`**

### UI 面板编码规则（重要）

- **CreateVisuals()** 静态方法构建 UI 结构（仅 PrefabGenerator 调用，运行时不执行）
- **OnInit → BindReferences() → BindEvents()** | **OnShow → RefreshUI()**
- **RefreshUI()** 只修改 `.text`/`.value`/`.color` 等属性，**零销毁零创建**
- **动态列表用对象池**：预创建行，SetActive 显隐，原地更新文字
- **绝不使用 Object.DestroyImmediate**
- **Hide 用 CanvasGroup（alpha=0）**，不用 SetActive(false)
- **过渡协程精准停止**（transitionCoroutine），不使用 StopAllCoroutines

### 技能系统

- **通用化**: 全职业可学任何技能（F144），ID 三段分配（职业/通用/Boss）
- **7 标签页**: All/Attack/Heal/Buff/Control/Passive/Learned
- **无装备槽（F160）**: learnedSkills 即可用，maxSkillSlots=6
- **完整规则见 `.Codex/rules/game.code.md`**

### UI 架构（F161-F176）

- **三层 Canvas**: HUD(sortingOrder=5) / Window(10) / Tip(20)，面板缓存跨层互斥
- **GameHUD**: 永久 HUD（日志区/角色信息/背包/技能栏）+ GameWorldContainer 子控制器
- **WindowBase**: 可拖动窗口基类，Destroy 不缓存，单窗口互斥
- **TipManager**: struct 池 + HoverTip/AutoTip 两种提示
- **场景切换**: ClearAll + SetState 两步模式（F176）
- **旧面板废弃**: TownPanel/BagPanel/RoleInfoPanel/ShopPanel/BattlePanel 已标记 [Obsolete]
- **完整规则见 `.Codex/rules/core.code.md` + `.Codex/rules/ui.code.md`**

### 战斗架构（F170-F171）

- **BattleController**: 非 MonoBehaviour 单例，事件驱动（OnRoundComplete/OnBattleEnd）
- **内联战斗**: DungeonPanel 订阅 BattleController，两次点击模式
- **怪物 AI**: 五种模式（Aggressive/Defensive/Debuffer/Balanced/Boss），mp 字段陷阱
- **完整规则见 `.Codex/rules/battle.code.md`**

### 配置系统（F113）

- **JSON 外置**: 6 个配置文件在 `Assets/Resources/Config/`
- **JsonUtility 限制**: 不支持顶层数组（需包装类）、枚举序列化为整数
- **双层机制**: JSON 优先 + 硬编码 fallback
- **完整规则见 `.Codex/rules/game.code.md`**

## 开发注意事项

- 新增 C# 脚本放在 `Assets/Scripts/` 下合理的子目录中
- Unity 的 `.meta` 文件必须与资源文件一同提交到版本控制
- `Library/`、`Temp/`、`obj/` 目录不应纳入版本控制
- 场景文件（`.unity`）为 YAML 格式，合并时需注意冲突
- 命名空间: `Game.Core`、`Game.UI`、`Game.Battle`、`Game.Config`、`Game.Data`

## 路径作用域规则

根据编辑的文件路径自动应用对应的编码标准（定义在 `.Codex/rules/`）：

| 路径 | 规则文件 | 核心要点 |
|---|---|---|
| `Assets/Scripts/Core/**` | `core.code.md` | UIPanel 生命周期、transitionCoroutine 精准停止、AudioManager 单例+轮播池、三层Canvas架构(F161)、跨场景ClearAll+SetState(F176)、GameWorldContainer子控制器模式 |
| `Assets/Scripts/UI/**` | `ui.code.md` | 深色主题配色(UIColors 唯一来源)、F111 模块拆分、对象池、CanvasGroup 隐藏、网格地图渲染、WindowBase可拖动窗口(F163)、TipManager浮动提示(F164)、GameHUD布局+旧面板废弃(F162-F176) |
| `Assets/Scripts/Game/Battle/**` | `battle.code.md` | 统一回合四阶段流程、TickEffects 仅一次、速度/伤害/Boss 公式体系、BattleController事件驱动(F170)、内联战斗(F171)、怪物AI五模式+mp陷阱(F147)、视觉效果、已知待修复 |
| `Assets/Scripts/Game/**` | `game.code.md` | 网格地牢数据模型+迁移策略、战争迷雾、技能通用化(F144)+取消装备槽(F160)、Buff配置、ConfigManager JSON架构+JsonUtility限制(F113)、战斗数值常量 |


## 用户偏好

- 保持专业性，不要迎合用户
- 回复使用中文
- 代码注释使用中文
- 提交前确认编译通过
- 允许在分析阶段提问，实现阶段全自动
- 实现代码后自动 commit，无需额外确认
- 记忆文件必须实际写入文件，口头答应不算数
- 记忆文件写在项目目录 `.Codex/memory/` 下，便于 Git 管理
- 编码和 Git 规则见 `.Codex/rules/`

## 自动化流水线

```
/pp        → 需求拆解（交互式，产品经理角色）
/dev       → 单功能开发（协调流程：准备→开发→验证→记录→提交→报告）
/devloop   → 循环开发（配额守卫：8轮/session，3连失败停止）
/qdev      → 临时任务（交互式分析 + 自动流水线）
/status    → 进度报告
/init      → 首次初始化
/agent     → 创建/优化 agent 或 command
```

### 流水线架构

```
Commands（主会话中运行，可使用 Agent 工具委托子 agent）
  /dev, /qdev, /devloop ──┬── dev-worker      (代码实现，tools: 读写+搜索+Bash)
                          ├── verify-feature  (功能验证，tools: 只读)  ─┐
                          ├── verify-quality  (质量验证，tools: 只读)  ─┤ 并行
                          └── mem-writer      (知识归档，tools: 读写+搜索)

Agents 位于 .Codex/agents/，带 YAML frontmatter（name, description, tools, model）
Commands 位于 .Codex/commands/，包含完整工作流逻辑
```

## 记忆系统

项目知识存储在 `.Codex/memory/` 目录（Git 管理，团队共享）。通过下方索引按需查阅。
详细规则见 `.Codex/memory/memory-rules.md`。