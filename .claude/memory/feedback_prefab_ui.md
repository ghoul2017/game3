---
name: UI 使用预制体而非纯代码构建
description: 从 F089 开始，新 UI 面板改用预制体而非 UIBuilder 硬编码构建
type: feedback
---

从 F089 开始，UI 面板改用预制体（Prefab）构建，不再写死在代码里。
**Why:** 用户希望 UI 布局与逻辑分离，使用 Unity 预制体更便于维护和调整。
**How to apply:** 新面板（如 SettingsPanel）创建预制体到 Resources/Prefabs 目录，代码通过 PrefabLoader 加载。已有面板的增强功能（如 DungeonPanel、BattlePanel）也尽量迁移到预制体模式。
