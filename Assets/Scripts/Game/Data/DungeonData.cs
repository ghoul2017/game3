using System.Collections.Generic;
using Game.Config;
using UnityEngine;

namespace Game.Data
{
    [System.Serializable]
    public class DungeonData
    {
        public int floor = 1;

        // ---- 旧线性列表（保留，供其他模块使用） ----

        /// <summary>[旧接口] 线性房间列表，DungeonPanel等模块仍在使用</summary>
        [System.Obsolete("请使用 Grid 二维数组，此字段仅做向后兼容")]
        public List<DungeonRoom> rooms = new List<DungeonRoom>();

        /// <summary>[旧接口] 当前房间索引</summary>
        [System.Obsolete("请使用 CurrentPos 网格坐标，此字段仅做向后兼容")]
        public int currentRoomIndex;

        public int maxFloor = 10;

        /// <summary>[旧接口] 当前房间</summary>
        public DungeonRoom CurrentRoom => currentRoomIndex < rooms.Count ? rooms[currentRoomIndex] : null;

        // ---- 新2D网格系统 ----

        /// <summary>网格宽度</summary>
        public int gridWidth;

        /// <summary>网格高度</summary>
        public int gridHeight;

        /// <summary>2D网格数组 [x, y]</summary>
        public DungeonTile[,] grid;

        /// <summary>所有房间瓦片引用（方便遍历，不含墙壁）</summary>
        public List<DungeonTile> roomTiles = new List<DungeonTile>();

        /// <summary>起点坐标（入口房间）</summary>
        public GridPos startPos;

        /// <summary>当前玩家所在坐标</summary>
        public GridPos currentPos;

        /// <summary>出口坐标（Boss/Exit房间）</summary>
        public GridPos exitPos;

        // ---- 网格辅助方法 ----

        /// <summary>获取指定坐标的瓦片，越界返回null</summary>
        public DungeonTile GetTileAt(int x, int y)
        {
            if (!IsInBounds(x, y)) return null;
            return grid[x, y];
        }

        /// <summary>获取指定坐标的瓦片（GridPos版本）</summary>
        public DungeonTile GetTileAt(GridPos pos)
        {
            return GetTileAt(pos.x, pos.y);
        }

        /// <summary>坐标是否在网格范围内</summary>
        public bool IsInBounds(int x, int y)
        {
            return grid != null && x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
        }

        /// <summary>获取所有房间类型的瓦片列表</summary>
        public List<DungeonTile> GetRoomTiles()
        {
            return roomTiles;
        }

        /// <summary>获取当前玩家所在的瓦片</summary>
        public DungeonTile CurrentTile => GetTileAt(currentPos);

        // ---- 地牢生成 ----

        public void GenerateFloor(int floorNum)
        {
            floor = floorNum;
            rooms.Clear();
            currentRoomIndex = 0;
            Debug.Log($"[地牢] 生成第 {floorNum} 层");

            int roomCount = 8 + floorNum * 2;
            if (roomCount > 20) roomCount = 20;

            // 第一个房间为空（入口）
            rooms.Add(new DungeonRoom { type = DungeonEventType.Empty, cleared = true });

            for (int i = 1; i < roomCount - 1; i++)
            {
                var room = new DungeonRoom();
                float roll = Random.value;
                if (roll < 0.40f) room.type = DungeonEventType.Monster;
                else if (roll < 0.50f) room.type = DungeonEventType.Elite;
                else if (roll < 0.60f) room.type = DungeonEventType.Treasure;
                else if (roll < 0.70f) room.type = DungeonEventType.Spring;
                else if (roll < 0.78f) room.type = DungeonEventType.Merchant;
                else room.type = DungeonEventType.Empty;
                rooms.Add(room);
            }

            // 最后一个房间为 Boss 或 出口
            if (floorNum % 5 == 0)
                rooms.Add(new DungeonRoom { type = DungeonEventType.Boss });
            else
                rooms.Add(new DungeonRoom { type = DungeonEventType.Exit });

            // 同步生成网格数据
            GenerateGrid();
        }

        /// <summary>
        /// 根据旧的线性rooms列表生成2D网格数据
        /// 网格布局：将线性房间排列为网格，房间之间自动设置连通性
        /// </summary>
        private void GenerateGrid()
        {
            int total = rooms.Count;

            // 根据房间数量计算网格尺寸
            int cols = Mathf.Min(total, 10);
            int rows = Mathf.CeilToInt((float)total / cols);
            gridWidth = cols;
            gridHeight = rows;

            grid = new DungeonTile[cols, rows];
            roomTiles.Clear();

            for (int i = 0; i < total; i++)
            {
                int x = i % cols;
                int y = i / cols;

                var room = rooms[i];
                var tile = new DungeonTile
                {
                    gridPos = new GridPos(x, y),
                    tileType = TileType.Room,
                    roomType = room.type,
                    isExplored = (i == 0), // 入口默认已探索
                    isCleared = room.cleared,
                    width = 1,
                    height = 1
                };

                // 将旧DungeonRoom的内容数据同步到网格瓦片
                tile.content.monsterId = room.monsterId;

                grid[x, y] = tile;
                roomTiles.Add(tile);
            }

            // 填充剩余格子为墙壁
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (grid[x, y] == null)
                    {
                        grid[x, y] = new DungeonTile
                        {
                            gridPos = new GridPos(x, y),
                            tileType = TileType.Wall
                        };
                    }
                }
            }

            // 设置连通性：相邻的房间格子互相连通
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    var tile = grid[x, y];
                    if (tile.tileType != TileType.Room) continue;

                    // 上（y-1）
                    if (y > 0 && grid[x, y - 1] != null && grid[x, y - 1].tileType == TileType.Room)
                        tile.connectUp = true;
                    // 下（y+1）
                    if (y < rows - 1 && grid[x, y + 1] != null && grid[x, y + 1].tileType == TileType.Room)
                        tile.connectDown = true;
                    // 左（x-1）
                    if (x > 0 && grid[x - 1, y] != null && grid[x - 1, y].tileType == TileType.Room)
                        tile.connectLeft = true;
                    // 右（x+1）
                    if (x < cols - 1 && grid[x + 1, y] != null && grid[x + 1, y].tileType == TileType.Room)
                        tile.connectRight = true;
                }
            }

            // 设置关键坐标
            startPos = new GridPos(0, 0);
            currentPos = startPos;
            exitPos = new GridPos((total - 1) % cols, (total - 1) / cols);

            Debug.Log($"[地牢] 网格生成完毕 尺寸={cols}x{rows} 房间数={roomTiles.Count}");
        }

        public void OnReturnTown()
        {
            // 返回城镇后，小怪、精英和泉水恢复
            foreach (var room in rooms)
            {
                if (room.type == DungeonEventType.Monster ||
                    room.type == DungeonEventType.Elite ||
                    room.type == DungeonEventType.Spring)
                    room.cleared = false;
            }

            // 同步网格数据
            foreach (var tile in roomTiles)
            {
                if (tile.roomType == DungeonEventType.Monster ||
                    tile.roomType == DungeonEventType.Elite ||
                    tile.roomType == DungeonEventType.Spring)
                    tile.isCleared = false;
            }
        }
    }

    [System.Serializable]
    public class DungeonRoom
    {
        public DungeonEventType type;
        public bool cleared;
        public int monsterId;  // 怪物配置ID
        public Vector2Int position;
    }
}
