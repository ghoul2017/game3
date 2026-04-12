---
name: 自动化开发工作流
description: game3 的自动化 Agent 工作流体系，学习自 game1 项目
type: reference
---

# 自动化开发工作流

学习自 `E:\worku\game1` 项目，基于 Anthropic 长时间运行 Agent 最佳实践。

## 核心文件

- `.claude/agents/coding-agent.md` — 编码 Agent（每 session 一个 feature）
- `.claude/agents/initializer.md` — 初始化 Agent（仅首次运行）
- `.claude/commands/dev.md` — 单次开发
- `.claude/commands/devloop.md` — 循环开发（8轮/session，3连失败停止）
- `.claude/commands/status.md` — 进度查看
- `.claude/settings.local.json` — 权限全开，只禁 git push 和 rm -rf
- `doc/feature_list.json` — 功能清单（只改 passes 字段）
- `doc/progress.txt` — 跨 session 进度（格式：`日期 ID 状态 - 描述`）
- `dev.cmd` / `devloop.cmd` — 无人值守脚本（`claude -p "运行 /dev" --dangerously-skip-permissions`）

## 关键规则

- 每次 session 只做一个 feature
- 严禁删除或修改 feature_list.json 中的条目，只改 passes
- session 结束时代码必须干净可编译
- 构建失败无法修复用 `git checkout -- <file>` 回退
- 不使用 git push

## 当前状态

- commit 3e126ec 已提交工作流基础设施
- F008 (UIBuilder 对象池) 已完成
- 下一个: F009 (UIPanel 基类 Hide 改用 CanvasGroup)
