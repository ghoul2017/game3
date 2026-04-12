---
name: 参考项目代码风格
description: 4个参考项目(game1/client, game1/server, sgserver, server)的共同架构模式和编码习惯
type: reference
---

# 参考项目共同架构基因

## 四个参考项目
| 项目 | 路径 | 语言 | 定位 |
|------|------|------|------|
| game1/client | E:\worku\game1\client | C# (Unity) | Unity客户端原型 |
| game1/server | E:\worku\game1\server | C# (.NET 8) | 游戏服务器 |
| sgserver | E:\work\sgserver | Java 8 | 成熟MMO服务器 |
| server | E:\work\server | Java 21 | 新架构服务器 |

## 共同架构模式（game3应继承）

### 1. Manager 单例模式
每个子系统一个 `XxxManager` 单例，持有业务逻辑和状态。

### 2. UIPanel 生命周期（来自game1/client）
- `OnInit()` — 首次显示，仅调用一次
- `OnShow()` — 每次显示
- `OnHide()` — 每次隐藏
- `OnDestroyed()` — 销毁时
- UIManager 用 Stack<UIPanel> 管理面板栈，支持 Show/Back/Replace

### 3. UIBuilder 静态工厂 + PrefabLoader 预制体
- 早期面板用 UIBuilder 纯代码构建（F001-F081）
- 从 F089 起新面板改用预制体，通过 PrefabLoader 加载（见 feedback_prefab_ui.md）
- RPG暗色配色常量化（DarkBg, Gold, ButtonNormal 等）
- CreateText/CreateButton/CreateSlider/CreatePanel 等工厂方法仍可用于局部控件

### 4. 命名约定
- 类名：PascalCase
- 方法名：PascalCase
- 变量名：camelCase
- 命名空间：与目录对应
- 消息命名：*Req / *Ack / *Ntf

### 5. 目录结构
每个系统一个目录，包含 Manager + Handler + Entity
```
Scripts/
  Core/      — UIManager, UIPanel, UIBuilder, GameEntry
  Game/
    Config/  — 配置数据和管理器
    Data/    — 数据模型
    Player/  — 玩家系统
    Dungeon/ — 地牢系统
    Battle/  — 战斗系统
    Town/    — 城镇系统
    Item/    — 物品系统
  UI/        — 所有UI面板
```

### 6. Canvas 初始化
- RenderMode: ScreenSpaceOverlay
- CanvasScaler: ScaleWithScreenSize, 1920x1080, match 0.5
- 自动创建 EventSystem
- DontDestroyOnLoad 跨场景
