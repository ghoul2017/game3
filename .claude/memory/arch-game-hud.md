---
name: GameHUD框架架构
description: F162+F165+F168+F176 永久HUD布局+右侧面板+GameWorldContainer子控制器+跨场景ClearAll+SetState模式+旧面板废弃
type: project
---

## GameHUD 框架（F162 + F165）

### 布局结构
| 区域 | 尺寸 | 定位 |
|------|------|------|
| GameWorldContainer | stretch-fill (右留260px, 底留70px) | 左侧主区域 |
| RightPanel | 260px宽，全高 | 右侧固定 |
| CombatLogSection | 180px高 | 右侧顶部 |
| CharInfoSection | 140px高 | 右侧中部 |
| BagSection | 填充剩余 | 右侧底部 |
| BottomSkillBar | 70px高，全宽 | 底部固定，6槽居中 |

### 不走 PrefabGenerator 流程

**Why:** GameHUD 挂载在 HUD Canvas 上（F161 三层架构的 HUD 层），不走 UIManager 的 ShowPanel/ShowWindow 路径，因此不需要预制体。UI 结构在 BindReferences() 中通过 BuildLayout() 直接构建。

**How to apply:** GameHUD 是唯一一个在 BindReferences 中直接构建 UI 的面板。其他面板仍走 PrefabGenerator/CreateVisuals 标准流程。如果后续 HUD 需要更复杂的布局，可以考虑迁移到预制体，但目前代码构建更简单。

### 初始隐藏防闪烁

BuildLayout 末尾设置 CanvasGroup alpha=0/interactable=false/blocksRaycasts=false，确保 HUD 创建时不可见。

**Why:** GameHUD.Create() 在初始化阶段调用，此时 GameState 可能还是 MainMenu。如果不隐藏，HUD 会闪现一帧再被 UpdateVisibility 隐藏。

**How to apply:** UpdateVisibility(GameState) 根据状态切换 Show/Hide。游戏启动时 GameState 通常是 MainMenu/None，HUD 保持隐藏直到进入 InTown/InDungeon/InBattle。

### 创建入口
```
GameHUD.Create() -> UIManager.GetHUDCanvas() -> new GameObject -> GetOrAddComponent<GameHUD>()
  -> Show() -> BindReferences -> BuildLayout -> OnShow -> RefreshAll
```
DontDestroyOnLoad 由 HUD Canvas 保证，GameHUD 自身不需要额外处理。

## 战斗日志区（F165）

### 数据流
```
GameHUD.RefreshCombatLog()
  -> BattleManager.Instance.CurrentBattle.log
  -> BattleLogFormatter.ColorizeLog(raw)     // 共享颜色标记工具类
  -> 12行Text对象池 (logTexts[])
  -> logScrollRect.normalizedPosition = (0,0) // 自动滚到底部
```

**Why:** BattleLogFormatter 从 BattlePanel.ColorizeLog 提取（F165），保证 HUD 日志和战斗面板日志使用完全相同的颜色规则。修改颜色逻辑只需改一处。
**How to apply:** 新增日志颜色规则时，只修改 BattleLogFormatter.ColorizeLog()，BattlePanel 和 GameHUD 自动同步。

### 对象池细节
- MAX_LOG_LINES=12，每行 lineHeight=14 + gap=2
- Content pivot=(0.5,1) 顶部锚定，行从上往下排列
- 取最近12条日志（startIdx = logCount - showCount），旧日志自然裁剪
- 非战斗状态（BattleManager.Instance.CurrentBattle==null）清空所有行

### ScrollRect 自动滚动
- content pivot 在顶部时，normalizedPosition=(0,0) 表示显示底部内容
- 仅在 showCount>0 时触发滚动，避免空日志时的无效操作

## 角色信息区（F165）

### 5行精炼布局（不可滚动）
| 行 | 内容 | Y偏移 |
|----|------|-------|
| 1 | 名字 Lv.XX 职业 | -22~-38 |
| 2 | HP条 + 数值 | -40~-54 |
| 3 | MP条 + 数值 | -56~-70 |
| 4 | ATK/DEF/SPD 文字 | -72~-88 |
| 5 | EXP条 + 数值 | -90~-104 |

- Player 为 null 时显示默认值（"--" / 0/0 / ATK:0 DEF:0 SPD:0）
- Slider 条使用 BuildBarWithLabel 统一构建（背景+填充+居中文字）

### 公开刷新方法
- `RefreshAll()` — 刷新全部四个区域
- `RefreshCombatLog()` / `RefreshCharInfo()` / `RefreshBag()` / `RefreshSkillBar()` — 单区域刷新
- 调用方：GameHUD.OnShow -> RefreshAll，或外部 GameState 变化后按需调用

## 背包区（F166）

### 自定义 BagRow 结构体

不复用 UIPool.PoolRow，使用独立的 BagRow struct。

**Why:** 背包行需要 sellBtn（出售按钮）而非 secondBtn，行高(22px)、字体(10pt)、布局比例与通用池行(30px/12pt)不同。强行复用 PoolRow 会导致字段语义不清且布局参数冲突。

**How to apply:** 如果后续新增类似的紧凑行（如技能栏列表），参考 BagRow 模式创建独立结构体，不要试图把所有行塞进 PoolRow。

### 三段式行布局
| 区域 | 宽度 | 说明 |
|------|------|------|
| 名称 | 55% | 物品名 + x数量 |
| 操作按钮 | 48x18 | 装备/使用，宝物类无此按钮 |
| 出售按钮 | 44x18 | 所有物品通用 |

- 行高 22px，间距 2px，20 行对象池
- ScrollRect 填充 BagSection 剩余空间（位于 CharInfoSection 下方）

### 物品分组与操作规则

物品按 ConfigData ID 分组，同 ID 合并显示为 "物品名 x数量"。

**消耗品场景限制（非显而易见）:**
| 物品 | 可用场景 | 不可用场景 |
|------|----------|------------|
| 回城卷轴 | InDungeon | InTown, InBattle |
| 炸弹 | InBattle | InTown, InDungeon |
| 解毒草 | InBattle | InTown, InDungeon |

**边界情况:**
- 宝物类(ItemType.Treasure)无操作按钮，只有出售按钮
- 等级不足时装备按钮显示 "LvN"（N为需求等级），按钮不可交互
- 装备操作自动卸下旧装备放入背包（隐式换装）
- 操作后同时刷新 RefreshBag + RefreshCharInfo（属性可能变化）

## 底部技能栏（F167）

### 布局
- 6个130x55px槽位水平居中于左侧游戏区域（全宽减去 RIGHT_PANEL_WIDTH/2 偏移）
- 空槽显示半透明占位，选中高亮通过修改 borderImage.color 实现

### EventTrigger 替代 Button（非显而易见）

技能槽使用 `EventTrigger`（PointerClick）而非 `Button.onClick`。

**Why:** Unity Button 只响应左键点击，而技能栏需要区分左键（选中）和右键（遗忘）。EventTrigger.PointerClick 回调中通过 `eventData.button == PointerEventData.InputButton.Right` 判断左右键。

**How to apply:** 后续需要区分鼠标按键的 UI 元素（如物品拖拽、快捷菜单），用 EventTrigger 替代 Button。如果只需要左键响应，仍用 Button 更简单。

### 遗忘技能索引边界

遗忘技能后需要修正 `_selectedSkillIndex`：
- 遗忘的是选中技能 → `_selectedSkillIndex = -1`（清除选中）
- 遗忘的是选中技能之前的技能 → 索引不变
- 遗忘的是选中技能之后的技能 → `_selectedSkillIndex--`（前移）
- 空槽位不可选中；技能配置缺失时显示 "???"

### 交互路径
```
BuildBottomSkillBar -> BuildSkillSlot(EventTrigger) -> BuildForgetOverlay(初始隐藏)
OnSkillSlotLeftClick -> UpdateSkillSlotSelection
OnSkillSlotRightClick -> forgetOverlay -> OnForgetConfirm -> player.ForgetSkill -> RefreshSkillBar
```

## GameWorldContainer 子控制器（F168）

### 双模式 UI 架构

GameWorldContainer（HUD 左侧主区域）可以挂载子控制器，这些控制器**不继承 UIPanel**，而是独立的 MonoBehaviour + static Create() 模式。

**Why:** GameWorldContainer 在 HUD Canvas 内，子控制器无需进入 UIManager 的面板栈/缓存系统。继承 UIPanel 会引入不必要的生命周期和栈管理开销。用 MonoBehaviour + Create() 更轻量，且可以自由控制 SetActive 显隐。

**How to apply:** 后续如果在 GameWorldContainer 中新增子场景（如地牢地图视图、战斗场景预览），都应采用 MonoBehaviour + static Create(parent) 模式，不走 UIPanel 流程。

### TownMapController（F168）

| 属性 | 值 |
|------|-----|
| 布局 | 5x4 固定网格，100px 格子 + 8px 间距 |
| 建筑数 | 4个（旅馆/商店/技能公会/地牢入口） |
| 交互 | EventTrigger（PointerEnter/Exit/Click） |
| 显隐 | gameObject.SetActive(true/false)，由 GameHUD 控制 |
| 创建时机 | 首次 ShowTownMap() 时懒创建 |

### GameState → TownMap 联动
```
GameManager.SetState(state) -> GameHUD.UpdateVisibility(state) [static]
  -> _instance.Show/Hide()          (HUD 自身)
  -> _instance.ShowTownMap/HideTownMap()  (TownMapController)
```
- InTown → Show HUD + Show TownMap
- InDungeon/InBattle → Show HUD + Hide TownMap
- MainMenu/CreatingRole → Hide HUD + Hide TownMap

**UpdateVisibility 设计**: static 方法通过 `_instance` 单例字段转发到实例方法。因为 GameManager 是纯 C# 类，无法持有 MonoBehaviour 引用，只能通过 static 桥接。

### TownMapController 建筑交互
| 建筑 | 行为 | 复用 |
|------|------|------|
| 旅馆 | player.FullRestore() + SaveGame + TipManager 提示 | 独立逻辑 |
| 商店 | UIManager.ShowWindow\<ShopWindow\>() | Window 层 |
| 技能公会 | UIManager.ShowPanel\<SkillLearnPanel\>() | 旧面板栈（暂未废弃） |
| 地牢入口 | new DungeonData() + ClearAll + SetState(InDungeon) + ShowPanel\<DungeonPanel\>() | 新流程 |

## 跨场景切换模式：ClearAll + SetState（F176）

### 核心模式

所有场景切换统一使用 `UIManager.ClearAll()` + `GameManager.SetState()` 两步：

```
ClearAll()                    — 清空旧面板栈（弹栈Hide + 关闭Window）
SetState(newState)            — 联动 GameHUD.UpdateVisibility + TownMapController 显隐
[可选] ShowPanel/ShowWindow   — 显示新场景面板
```

**Why:** 旧流程中 `GameManager.NewGame()`/`ContinueGame()` 内部调用 `SetState`，而调用方（MainPanel）也会触发状态相关操作，导致 `UpdateVisibility` 被重复调用。F176 将状态管理责任全部移到调用方，GameManager 只负责数据初始化，不触碰 GameState。

**How to apply:** 新增场景切换点时，先 `ClearAll()` 再 `SetState()`。不要在 GameManager 的数据方法中夹杂 SetState 调用。

### 完整场景切换路径

| 触发点 | 操作 | 目标状态 |
|--------|------|----------|
| MainPanel(开始冒险) | SetState(CreatingRole) + ReplacePanel\<CreateRolePanel\> | CreatingRole |
| MainPanel(继续冒险) | ClearAll + SetState(InTown) | InTown |
| CreateRolePanel(创建完成) | ClearAll + SetState(InTown) | InTown |
| TownMapController(商店) | ShowWindow\<ShopWindow\> | InTown（不变） |
| TownMapController(地牢入口) | ClearAll + SetState(InDungeon) + ShowPanel\<DungeonPanel\> | InDungeon |
| DungeonPanel(返回城镇) | ClearAll + SetState(InTown) | InTown |
| DungeonPanel(战败) | ClearAll + SetState(InTown) | InTown |
| GameHUD(回城卷轴) | ClearAll + SetState(InTown) | InTown |

### 旧面板废弃状态（F176）

以下面板标记 `[System.Obsolete]`，功能已迁移到新架构：

| 旧面板 | 废弃原因 | 替代方案 |
|--------|----------|----------|
| TownPanel | 城镇由 GameHUD+TownMapController 处理 | TownMapController (F168) |
| BagPanel | 背包集成到 GameHUD | GameHUD 背包区 (F166) |
| RoleInfoPanel | 角色信息集成到 GameHUD | GameHUD 角色信息区 (F165) |
| ShopPanel | 商店由 ShopWindow 替代 | ShopWindow (WindowBase, F163) |
| BattlePanel | 战斗由 DungeonPanel 内联处理 | DungeonPanel 内联战斗 (F171) |
| SkillLearnPanel | **暂未废弃**，等待 WindowBase 版本 | 仍由 TownMapController 调用 |
