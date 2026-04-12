# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个基于 Unity 的 2D 地牢探索类游戏项目（参考"方块地牢2"）。项目处于早期阶段，目前只有默认场景，尚无自定义脚本。

- **Unity 版本**: 2021.3.26f1c1（安装路径: `C:\Program Files\Unity\Hub\Editor\2021.3.26f1c1`）
- **渲染管线**: Universal Render Pipeline (URP) 12.1.11，使用 2D 渲染器
- **项目模板**: Universal 2D Template (com.unity.template.universal-2d@1.1.4)
- **目标框架**: .NET Framework 4.7.1 / C# 9.0
- **IDE 支持**: Rider, Visual Studio, VS Code

## 构建与运行

此项目使用 Unity Editor 进行构建和运行，无命令行构建脚本。在 Unity Editor 中打开 `E:\worku\game3` 目录即可。

Unity 项目文件（`.csproj` / `.sln`）由 Unity Editor 自动生成，不应手动修改。

## 项目结构

```
Assets/
  Scenes/          - 游戏场景（当前仅有 SampleScene）
  Settings/        - URP 渲染设置（Renderer2D.asset, UniversalRP.asset）
doc/
  项目设计.docx      - 项目设计文档
  参考项目/          - 参考资料，包含方块地牢2策划拆解分析PDF
```

## 关键依赖包

- `com.unity.feature.2d` - 2D 功能集（Sprite、Tilemap、Physics2D）
- `com.unity.render-pipelines.universal` - URP 渲染管线
- `com.unity.textmeshpro` - 文本渲染
- `com.unity.modules.tilemap` / `com.unity.modules.physics2d` - Tilemap 地图和 2D 物理

## 开发注意事项

- 新增 C# 脚本放在 `Assets/` 下合理的子目录中（如 `Assets/Scripts/`）
- Unity 的 `.meta` 文件必须与资源文件一同提交到版本控制
- `Library/`、`Temp/`、`obj/` 目录不应纳入版本控制
- 场景文件（`.unity`）为 YAML 格式，合并时需注意冲突
