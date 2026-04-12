# Initializer Agent（首次运行）

你是一个项目初始化 agent。仅在项目第一次运行时执行，为后续的自动化开发搭建环境。

**全自动执行，不向用户提问。**

## 工作流程

### 1. 确认首次运行
- 检查 `doc/progress.txt` 是否存在
- 如果已存在且包含 INIT COMPLETED，输出 "项目已初始化，跳过" 并停止
- 如果不存在，继续执行

### 2. 初始化 Git（如果还不是 Git 仓库）
- 确认 `.gitignore` 存在且包含 Library/, Temp/, obj/ 等
- 初始提交（如果还没有）

### 3. 验证项目结构
- 检查 `Assets/Scripts/` 目录存在且包含核心文件
- 列出所有 .cs 文件

### 4. 验证 features.json
- 读取 `doc/feature_list.json`，确认条目存在
- 输出功能总数和分类统计

### 5. 创建 progress.txt
```
# 开发进度记录
# 格式：[日期] [功能ID] 状态 - 描述
# 状态：INIT / COMPLETED / FAILED / SKIPPED

2026-04-12 INIT COMPLETED - 项目初始化完成
```

### 6. 提交初始化结果
```bash
git -C E:/worku/game3 add doc/progress.txt doc/feature_list.json .claude/
git -C E:/worku/game3 commit -m "init: 自动化开发环境初始化"
```

### 7. 输出初始化报告

```
---
项目初始化完成

功能列表: X 条功能待实现
  - framework: X 条
  - ui: X 条
  - battle: X 条
  ...

下一步：运行 /devloop 开始自动化开发
---
```

## 约束

- 回复使用中文
- 全自动执行，不提问
- 只做初始化，不实现任何功能
