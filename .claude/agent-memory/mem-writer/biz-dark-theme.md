---
name: 深色主题UI配色规范
description: F128深色主题配色方案+F111拆分后模块职责。颜色单一来源为UIColors，UIBuilder为门面转发。
type: project
---

# 深色主题 UI 配色规范 (F128, F111重构)

**Why:** 全局 UI 从浅色系改造为 RPG 深色主题，配色方案需要在所有面板中保持一致。
**How to apply:** 所有 UI 颜色必须引用 `UIColors` 中定义的静态常量（或通过 `UIBuilder` 门面转发），严禁硬编码。

## 配色体系

| 角色 | 常量 | 色值 | 用途 |
|------|------|------|------|
| 全屏背景 | `DarkBg` | `#0d0d1a` (rgba: 0.051, 0.051, 0.102, 0.97) | 主 Canvas 背景 |
| 面板背景 | `PanelBg` | `#1a1a2e` (rgba: 0.102, 0.102, 0.180, 0.96) | 面板底色 |
| 卡片背景 | `CardBg` | `#222244` (rgba: 0.133, 0.133, 0.267, 0.95) | 卡片/行背景 |
| 标题色 | `Gold` | `#c8a24c` | 暗金标题、按钮文字 |
| 正文色 | `TextWhite` | `#e0d8c0` | 正文文字 |
| 次要文字 | `TextDim` | `#958678` | 次要/辅助文字 |

## 可访问性要求

- **次要文字对比度 >= 4.5:1**（WCAG AA 标准）
- TextDim 最终色值 `#958678`，经过对比度验证（初始值 `#8b7d6b` 对比度不足）
- 功能色（红/绿/蓝/紫/橙）使用高饱和度标准色值，直接适配深色背景，不使用浅色主题颜色乘以系数

## 功能色（高饱和度）

| 常量 | 色值 | 用途 |
|------|------|------|
| `Red` | `#e74c3c` | 危险/HP条 |
| `Green` | `#58d68d` | 治愈/已装备/MP条 |
| `Blue` | `#5dade2` | 法力/当前房间 |
| `Purple` | `#af7ac5` | 魔法/精英怪 |
| `Orange` | `#f0c040` | 金色图标/EXP条 |

## 房间颜色

| 常量 | 色值 | 用途 |
|------|------|------|
| `RoomCurrent` | `#5dade2` (alpha 0.9) | 玩家当前所在房间 |
| `RoomCleared` | `#222244` (alpha 0.5) | 已清理房间 |
| `RoomMonster` | `#e74c3c` (alpha 0.85) | 普通怪物房 |
| `RoomElite` | `#af7ac5` (alpha 0.85) | 精英怪物房 |
| `RoomBoss` | `#F22613` (alpha 0.92) | Boss 房 |
| `RoomTreasure` | `#f0c040` (alpha 0.85) | 宝箱房 |
| `RoomSpring` | `#58d68d` (alpha 0.85) | 泉水房 |
| `RoomMerchant` | `#BF8C26` (alpha 0.85) | 商人间 |
| `RoomExit` | `#5dade2` (alpha 0.9) | 出口房间 |

## 编码规则

1. **颜色唯一来源**: `UIColors` 是所有 UI 颜色常量的唯一定义处。`UIBuilder` 保留为门面，通过 `static =>` 属性转发到 `UIColors`，确保向后兼容——现有 `UIBuilder.xxx` 调用无需修改。
2. **禁止硬编码**: 严禁使用 `Color.white`、`Color.gray`、`new Color(...)` 等硬编码颜色，统一引用 `UIColors.xxx` 或 `UIBuilder.xxx`
3. **GetRoomColor 默认分支**: 地图房间颜色的 `default` 分支必须返回 `UIColors.RoomCleared`，而非硬编码值
4. **工厂方法自动主题**: `UIFactory.CreateFullScreenBg()` 自动应用 `DarkBg`，`UIFactory.CreatePanel()` 自动应用 `PanelBg`，新增面板通过工厂方法即自动适配深色主题

## F111 模块拆分

UIBuilder 拆为三个独立模块（F111），UIBuilder 保留为门面：

| 模块 | 职责 | 依赖 |
|------|------|------|
| `UIColors` | 37个颜色常量（纯静态，无依赖） | 无 |
| `UIFactory` | 12个控件创建方法 | UIColors + UIBuilder.SetStretch/SetAnchored |
| `UIPool` | PoolRow结构体 + CreatePoolRows | UIFactory.CreateUI + UIColors + UIBuilder.SetAnchored |
| `UIBuilder` | 门面转发 + SetStretch/SetAnchored布局辅助 | UIColors, UIFactory, UIPool |

新增代码应直接引用 `UIColors`/`UIFactory`，无需经由 `UIBuilder` 中转。

## 已知待修复项

验证中发现以下残留硬编码，后续功能迭代中逐步修复：
- `DungeonPanel.cs:259` — `bg.color = Color.gray;` (应为 UIBuilder.RoomCleared)
- `DungeonPanel.cs:271` — `iconText.color = Color.white;` (应为 UIBuilder.TextWhite)
- `MainPanel.cs:154` — `btnContinueBg.color = Color.white;` (应为 UIBuilder.ButtonNormal)
