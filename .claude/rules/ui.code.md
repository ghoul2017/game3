# UI 层编码规则（Assets/Scripts/UI/**）

## F111 模块拆分：UIBuilder 职责

UIBuilder 拆为三个独立模块，UIBuilder 保留为门面：

| 模块 | 职责 | 依赖 |
|------|------|------|
| `UIColors` | 37个颜色常量（纯静态，无依赖） | 无 |
| `UIFactory` | 12个控件创建方法 | UIColors + 布局辅助 |
| `UIPool` | PoolRow结构体 + CreatePoolRows | UIFactory + UIColors |
| `UIBuilder` | 门面转发 + SetStretch/SetAnchored 布局辅助 | UIColors, UIFactory, UIPool |

新增代码应**直接引用 `UIColors`/`UIFactory`**，无需经由 `UIBuilder` 中转。

## 配色体系（深色主题）

### 背景色系
| 用途 | 常量 | 色值 |
|------|------|------|
| 全屏背景 | `UIColors.DarkBg` | `#0d0d1a` |
| 面板背景 | `UIColors.PanelBg` | `#1a1a2e` |
| 卡片背景 | `UIColors.CardBg` | `#222244` |

### 文字色系
| 用途 | 常量 | 色值 |
|------|------|------|
| 标题/强调 | `UIColors.Gold` | `#c8a24c` |
| 正文 | `UIColors.TextWhite` | `#e0d8c0` |
| 次要文字 | `UIColors.TextDim` | `#958678` |

### 功能色（高饱和度）
| 用途 | 常量 | 色值 |
|------|------|------|
| 危险/HP | `UIColors.Red` | `#e74c3c` |
| 治愈/MP | `UIColors.Green` | `#58d68d` |
| 法力/当前 | `UIColors.Blue` | `#5dade2` |
| 魔法/精英 | `UIColors.Purple` | `#af7ac5` |
| EXP/宝物 | `UIColors.Orange` | `#f0c040` |

### 房间颜色（地图用）
| 房间类型 | 常量 | 色值 |
|----------|------|------|
| 当前房间 | `RoomCurrent` | `#5dade2` |
| 已清理 | `RoomCleared` | `#222244` |
| 怪物房 | `RoomMonster` | `#e74c3c` |
| 精英房 | `RoomElite` | `#af7ac5` |
| Boss房 | `RoomBoss` | `#F22613` |
| 宝箱房 | `RoomTreasure` | `#f0c040` |
| 泉水房 | `RoomSpring` | `#58d68d` |
| 商人间 | `RoomMerchant` | `#BF8C26` |
| 出口房 | `RoomExit` | `#5dade2` |

### 配色编码规则
1. **颜色唯一来源**: UIColors，严禁硬编码 `Color.white`/`Color.gray`/`new Color(...)`
2. **GetRoomColor default**: 必须返回 `UIColors.RoomCleared`，非硬编码
3. **工厂自动主题**: 新增面板通过 `UIFactory.CreateFullScreenBg()` / `UIFactory.CreatePanel()` 自动适配深色主题

## 面板编码规则

- **OnInit** → BindReferences() → BindEvents()（仅一次）
- **OnShow** → RefreshUI()（每次显示）
- **RefreshUI**: 只改 `.text`/`.value`/`.color`，**零销毁零创建**
- **动态列表用对象池**: 预创建行，SetActive 显隐，原地更新文字
- **绝不使用 Object.DestroyImmediate**
- **Hide 用 CanvasGroup（alpha=0）**，不用 SetActive(false)

### 协程动画时序
- 面板过渡由 transitionCoroutine 管理，不受子类协程影响
- 子类协程（伤害浮动、HP条动画等）不被 Show/Hide 中断
- RefreshUI 中的浮动文字用 `lastProcessedLogCount` 去重

## 网格地图 UI 规则

### ScrollRect 挂载位置
- 组件挂在 Viewport 对象上（MapPanel > Inner > Viewport），非 BorderedPanel 外层
- 层级：MapPanel > Inner > Viewport(ScrollRect+Mask) > Content(瓦片池)

### CurrentOutline 独立设计
- 当前房间高亮边框是独立 GameObject，不修改 tile 结构
- 每次 UpdateGridMap 重算位置和尺寸

### 滚轮缩放
- cellSize 范围 [60, 120]，步长 10，默认 80
- 鼠标在 Viewport 范围内才响应

### 战争迷雾渲染
- FogOverlay 是每个瓦片内的独立 Image（全尺寸，raycastTarget=false）
- 只改 `.color` 属性，零创建零销毁
- Hidden 态额外隐藏 Overlay/Icon/TypeLabel
