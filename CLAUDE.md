# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
    Core/           - 框架层（UIManager, UIPanel, UIBuilder, GameManager, DataManager, GameEntry）
    Game/
      Battle/       - 战斗系统（BattleManager）
      Config/       - 配置系统（ConfigManager, ConfigData, GameEnums）
      Data/         - 数据模型（PlayerData, DungeonData, BattleData, GameData）
      Dungeon/      - 地牢系统（规划中）
      Item/         - 物品系统（规划中）
      Player/       - 玩家系统（规划中）
      Town/         - 城镇系统（规划中）
    UI/             - UI面板（MainPanel, TownPanel, BattlePanel, DungeonPanel, ShopPanel, BagPanel, SkillLearnPanel, RoleInfoPanel, CreateRolePanel）
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
| `.claude/agents/initializer.md` | 初始化 Agent（首次运行） |
| `.claude/agents/coding-agent.md` | 编码 Agent（每 session 一个功能） |
| `.claude/agents/planner-agent.md` | 需求拆解 Agent（产品经理角色，交互式） |

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

## UI 架构关键约定

- **UIPanel 基类** → 所有面板继承，重写 `OnInit()`/`OnShow()`/`OnHide()`
- **UIManager** → 单例，栈式面板管理（`ShowPanel<T>()`/`ReplacePanel<T>()`）
- **UIBuilder** → 静态工具类，代码动态构建 UI（无需预制体）
- **UI 配色**: RPG 风格 — 深蓝/紫底色 + 金色强调色（常量在 UIBuilder 中）

### UI 面板编码规则（重要）

- **OnInit()** 中创建所有 UI 控件并**缓存到字段**
- **RefreshUI()** 只修改 `.text`/`.value`/`.color` 等属性，**零销毁零创建**
- **动态列表用对象池**：预创建行，SetActive 显隐，原地更新文字
- **绝不使用 Object.DestroyImmediate** — 它是导致文字消失、面板错乱的根本原因
- **Hide 用 CanvasGroup**（alpha=0），不用 SetActive(false) — 避免破坏 RectTransform 层级

## 开发注意事项

- 新增 C# 脚本放在 `Assets/Scripts/` 下合理的子目录中
- Unity 的 `.meta` 文件必须与资源文件一同提交到版本控制
- `Library/`、`Temp/`、`obj/` 目录不应纳入版本控制
- 场景文件（`.unity`）为 YAML 格式，合并时需注意冲突
- 命名空间: `Game.Core`、`Game.UI`、`Game.Battle`、`Game.Config`、`Game.Data`

## 路径作用域规则

根据编辑的文件路径自动应用对应的编码标准（定义在 `.claude/rules/`）：

| 路径 | 规则文件 | 核心要点 |
|---|---|---|
| `Assets/Scripts/Core/**` | `core.code.md` | 单例模式、UIPanel 生命周期、UIManager 栈管理 |
| `Assets/Scripts/UI/**` | `ui.code.md` | OnInit 缓存、RefreshUI 只改属性、对象池、禁用 DestroyImmediate |
| `Assets/Scripts/Game/Battle/**` | `battle.code.md` | 回合制流程、Buff 处理、伤害计算 |
| `Assets/Scripts/Game/**` | `game.code.md` | 配置加载、数据模型、命名空间规范 |


## 用户偏好

- 保持专业性，不要迎合用户
- 回复使用中文
- 代码注释使用中文
- 提交前确认编译通过
- 允许在分析阶段提问，实现阶段全自动
- 实现代码后自动 commit，无需额外确认
- 记忆文件必须实际写入文件，口头答应不算数
- 记忆文件写在项目目录 `.claude/memory/` 下，便于 Git 管理
- 编码和 Git 规则见 `.claude/rules/`

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

Agents 位于 .claude/agents/，带 YAML frontmatter（name, description, tools, model）
Commands 位于 .claude/commands/，包含完整工作流逻辑
```

## 记忆系统

项目知识存储在 `.claude/memory/` 目录（Git 管理，团队共享）。通过下方索引按需查阅。
详细规则见 `.claude/memory/memory-rules.md`。