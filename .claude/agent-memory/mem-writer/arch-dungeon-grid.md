---
name: 网格地牢架构与迁移计划
description: F131起DungeonData从线性列表迁移至2D网格的架构决策、数据模型、生成算法、分阶段迁移策略
type: project
---

# 网格地牢系统 — 架构与迁移

**Why:** F131-F136 构成一次跨功能的渐进式重构，理解迁移策略对后续功能开发至关重要。
**How to apply:** 修改 DungeonData/DungeonPanel/BattleManager 中地牢相关代码时，先确认当前迁移阶段，使用对应的新/旧接口。

## 数据模型

- `GridPos` — 自定义 struct，非 Unity.Vector2Int。原因：Vector2Int 在 JSON 序列化时字段名不符，自定义 struct 避免 Unity 序列化兼容性问题
- `DungeonTile` — 网格格子，TileType.Room 或 TileType.Wall，含四方向连通标记 + ContentData
- `ContentData` — 房间内容（monsterId/treasureId/goldAmount/itemId/skillId/monsterLevel），替代旧 DungeonRoom 的零散字段
- `DungeonData.grid[,]` — 2D 数组 [x,y]，roomTiles 列表辅助遍历

## F132 网格地牢生成算法

### 调用链
GenerateGrid() -> SetRoom -> AddFrontierWalls -> TryPlaceMultiRoom -> PlaceSpecialRooms -> EnsureConnectivity -> BuildConnections -> AssignRoomContent -> SyncRoomsFromGrid

### 随机游走扩张算法
- 中心起点，维护前沿Wall候选列表（frontier）
- 每步从frontier中随机取一个Wall，60%概率变Room，40%跳过
- 新Room自动将其周围Wall加入frontier
- 自然产生不规则但有机的地牢形态

### 多尺寸房间设计
- 大房间（2x2/2x3/3x2等）以**原点格子（左上角）**作为唯一内容入口
- 其他格子保持 tileType=Room 但不加入 roomTiles，不参与内容分配
- BFS连通性和BuildConnections不受影响（非原点格子仍然是Room可被路径经过）

### 连通性保障
- BFS检测所有Room是否可达，不可达则 CarvePath 打通（先水平后垂直）
- `maxFixes=100` 上限防止无限循环
- `FindCenterRoom` 兜底返回 roomTiles[0]（无中心时取第一个房间）

## 分阶段迁移策略

| 阶段 | 功能 | 内容 |
|------|------|------|
| F131 | 数据模型 | 定义新网格字段，旧 rooms/currentRoomIndex 标 Obsolete 保留 |
| F132 | 生成算法 | 替换 GenerateGrid() 为真正的随机游走生成 |
| F133 | 网格渲染 | DungeonPanel 改用 ScrollRect+色块渲染网格 |
| F134 | 战争迷雾 | 未探索/已探索/当前三状态可见性 |
| F135 | 玩家移动 | 点击/WASD 移动，替代旧的线性前进 |
| F136 | 房间触发 | 进入房间根据 tile 内容触发事件 |

关键原则：**旧接口不删除，消费者(DungeonPanel/BattleManager)继续用旧接口，直到对应阶段才迁移**。F132已完成随机游走生成替换。

## F133 网格地图渲染

### 坐标映射规则
- 网格瓦片使用左上角锚定：`pivot=(0,1)`, `anchorMin/Max=(0,1)`
- tile坐标(x,y)到UI位置：`anchoredPosition = (x * cellSize, -y * cellSize)`（y轴向下为负）
- 对象池上限 MaxTiles=100（10x10），超出跳过渲染

### 旧数据双向兼容（迁移期）
- `FindRoomIndex(dungeon, x, y)` — 在旧 rooms 列表中按坐标匹配，找到返回索引，未找到返回 currentRoomIndex
- `FindMatchingRoom(dungeon, tile)` — 在旧 rooms 中按 gridPos 匹配，未找到则创建临时 DungeonRoom（兜底）
- BattleManager 仍使用 DungeonRoom，所有房间事件处理完都会同步 `tile.isCleared` 和 `room.cleared`
- 迁移完成后（F136），这些兼容方法和旧 rooms 字段将删除

## 双数据同步

在 F131 阶段，GenerateFloor() 同时维护旧 rooms 和新 grid 数据。OnReturnTown() 双重同步 rooms.cleared 和 tile.isCleared。迁移完成后旧字段删除。
