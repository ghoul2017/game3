using System.Collections.Generic;
using Game.Config;
using Game.Core;
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

            // 核心生成：随机游走算法生成2D网格
            GenerateGrid();

            // 从网格数据反向填充旧的线性列表，保持向后兼容
            SyncRoomsFromGrid();

            Debug.Log($"[地牢] 第{floorNum}层生成完毕 网格={gridWidth}x{gridHeight} 房间数={roomTiles.Count}");
        }

        /// <summary>将指定格子设为Room</summary>
        private void SetRoom(int x, int y, int w, int h)
        {
            for (int dx = 0; dx < w; dx++)
            {
                for (int dy = 0; dy < h; dy++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (!IsInBounds(px, py)) continue;

                    if (grid[px, py].tileType == TileType.Wall)
                    {
                        grid[px, py].tileType = TileType.Room;

                        // 只有原点格子加入 roomTiles 并记录房间尺寸
                        // 非原点格子仅标记为 Room，不参与内容分配
                        if (dx == 0 && dy == 0)
                        {
                            grid[px, py].width = w;
                            grid[px, py].height = h;
                            roomTiles.Add(grid[px, py]);
                        }
                    }
                }
            }
        }

        /// <summary>将目标位置的邻接Wall添加到前沿列表</summary>
        private void AddFrontierWalls(int x, int y, List<GridPos> frontier)
        {
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (IsInBounds(nx, ny) && grid[nx, ny].tileType == TileType.Wall)
                {
                    // 避免重复添加
                    if (!frontier.Contains(new GridPos(nx, ny)))
                    {
                        frontier.Add(new GridPos(nx, ny));
                    }
                }
            }
        }

        /// <summary>
        /// 尝试放置多尺寸房间（2x1, 1x2, 2x2）
        /// 20%概率尝试2x1或1x2，5%概率尝试2x2
        /// </summary>
        private bool TryPlaceMultiRoom(int x, int y, ref int roomCount)
        {
            float roll = Random.value;

            // 5%概率尝试2x2
            if (roll < 0.05f)
            {
                if (CanPlaceRoom(x, y, 2, 2))
                {
                    SetRoom(x, y, 2, 2);
                    roomCount += 4;
                    // 为新房间格子添加前沿
                    for (int dx = 0; dx < 2; dx++)
                    {
                        for (int dy = 0; dy < 2; dy++)
                        {
                            AddFrontierWalls(x + dx, y + dy, GetFrontierList());
                        }
                    }
                    return true;
                }
            }

            // 20%概率尝试2x1或1x2
            if (roll < 0.25f)
            {
                // 随机选择水平或垂直
                bool horizontal = Random.value < 0.5f;
                int w = horizontal ? 2 : 1;
                int h = horizontal ? 1 : 2;

                if (CanPlaceRoom(x, y, w, h))
                {
                    SetRoom(x, y, w, h);
                    roomCount += w * h;
                    for (int dx = 0; dx < w; dx++)
                    {
                        for (int dy = 0; dy < h; dy++)
                        {
                            AddFrontierWalls(x + dx, y + dy, GetFrontierList());
                        }
                    }
                    return true;
                }
            }

            return false;
        }

        // 引用当前的前沿列表（用于多尺寸房间添加前沿）
        private List<GridPos> _currentFrontier;

        private List<GridPos> GetFrontierList() => _currentFrontier;

        /// <summary>检查指定区域是否可以放置房间（所有格子都是Wall）</summary>
        private bool CanPlaceRoom(int x, int y, int w, int h)
        {
            for (int dx = 0; dx < w; dx++)
            {
                for (int dy = 0; dy < h; dy++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (!IsInBounds(px, py)) return false;
                    if (grid[px, py].tileType != TileType.Wall) return false;
                }
            }
            return true;
        }

        /// <summary>放置特殊房间：Boss、Exit、Shrine</summary>
        private void PlaceSpecialRooms()
        {
            startPos = FindCenterRoom();
            exitPos = startPos;

            // 找到距起点最远的房间作为关键房间位置
            DungeonTile farthest = FindFarthestRoom(startPos);

            // 每5层放置Boss房
            if (floor % 5 == 0)
            {
                if (farthest != null)
                {
                    farthest.roomType = DungeonEventType.Boss;
                    exitPos = farthest.gridPos;

                    // Exit在Boss相邻处或最远角
                    DungeonTile exitTile = FindAdjacentRoomOrFarthest(farthest.gridPos, startPos);
                    if (exitTile != null && exitTile != farthest)
                    {
                        exitTile.roomType = DungeonEventType.Exit;
                    }
                }
            }
            else
            {
                // 非Boss层：Exit在最远处
                if (farthest != null)
                {
                    farthest.roomType = DungeonEventType.Exit;
                    exitPos = farthest.gridPos;
                }
            }

            // 随机放置1-2个Shrine（仅当玩家还有未学技能时才放置，避免空神龛）
            var allSkills = ConfigManager.Instance.GetAllSkills();
            var player = GameManager.Instance.Player;
            bool hasUnlearned = false;
            foreach (var skill in allSkills)
            {
                if (!player.learnedSkills.Contains(skill.id))
                {
                    hasUnlearned = true;
                    break;
                }
            }
            if (hasUnlearned)
            {
                int shrineCount = Random.Range(1, 3);
                PlaceRandomRooms(DungeonEventType.Shrine, shrineCount);
            }
        }

        /// <summary>找到靠近中心的房间作为起点</summary>
        private GridPos FindCenterRoom()
        {
            int cx = gridWidth / 2;
            int cy = gridHeight / 2;

            // BFS从中心开始找最近的Room
            var visited = new HashSet<GridPos>();
            var queue = new Queue<GridPos>();
            queue.Enqueue(new GridPos(cx, cy));
            visited.Add(new GridPos(cx, cy));

            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            while (queue.Count > 0)
            {
                var pos = queue.Dequeue();
                var tile = GetTileAt(pos);
                if (tile != null && tile.tileType == TileType.Room)
                    return pos;

                for (int i = 0; i < 4; i++)
                {
                    var np = new GridPos(pos.x + dx[i], pos.y + dy[i]);
                    if (IsInBounds(np.x, np.y) && !visited.Contains(np))
                    {
                        visited.Add(np);
                        queue.Enqueue(np);
                    }
                }
            }

            // 兜底：返回第一个房间
            if (roomTiles.Count > 0)
                return roomTiles[0].gridPos;

            return new GridPos(0, 0);
        }

        /// <summary>通过BFS找到距起点最远的房间</summary>
        private DungeonTile FindFarthestRoom(GridPos from)
        {
            var distances = BFS(from);
            DungeonTile farthest = null;
            int maxDist = -1;

            foreach (var tile in roomTiles)
            {
                int d;
                if (distances.TryGetValue(tile.gridPos, out d) && d > maxDist)
                {
                    maxDist = d;
                    farthest = tile;
                }
            }

            return farthest;
        }

        /// <summary>找到一个在pos旁边的房间，或距startPos最远的房间</summary>
        private DungeonTile FindAdjacentRoomOrFarthest(GridPos pos, GridPos startPos)
        {
            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            // 优先找相邻房间（非Boss、非起点）
            var candidates = new List<DungeonTile>();
            for (int i = 0; i < 4; i++)
            {
                var np = new GridPos(pos.x + dx[i], pos.y + dy[i]);
                var tile = GetTileAt(np);
                if (tile != null && tile.tileType == TileType.Room
                    && tile.roomType != DungeonEventType.Boss
                    && np != startPos)
                {
                    candidates.Add(tile);
                }
            }

            if (candidates.Count > 0)
                return candidates[Random.Range(0, candidates.Count)];

            // 没有相邻的，找距起点最远的非Boss房间
            var distances = BFS(startPos);
            DungeonTile farthest = null;
            int maxDist = -1;

            foreach (var tile in roomTiles)
            {
                if (tile.roomType == DungeonEventType.Boss) continue;
                if (tile.gridPos == startPos) continue;

                int d;
                if (distances.TryGetValue(tile.gridPos, out d) && d > maxDist)
                {
                    maxDist = d;
                    farthest = tile;
                }
            }

            return farthest;
        }

        /// <summary>随机放置指定类型的房间</summary>
        private void PlaceRandomRooms(DungeonEventType type, int count)
        {
            // 可用房间列表：排除起点、Boss、Exit
            var available = new List<DungeonTile>();
            foreach (var tile in roomTiles)
            {
                if (tile.gridPos == startPos) continue;
                if (tile.roomType == DungeonEventType.Boss) continue;
                if (tile.roomType == DungeonEventType.Exit) continue;
                if (tile.roomType != DungeonEventType.Empty) continue;
                available.Add(tile);
            }

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int idx = Random.Range(0, available.Count);
                available[idx].roomType = type;
                available.RemoveAt(idx);
            }
        }

        /// <summary>BFS验证连通性，不连通则打通墙壁</summary>
        private void EnsureConnectivity()
        {
            var dist = BFS(startPos);

            // 检查exitPos是否可达
            if (!dist.ContainsKey(exitPos))
            {
                // 打通从startPos到exitPos的路径
                CarvePath(startPos, exitPos);
                dist = BFS(startPos);
            }

            // 检查所有房间是否可达
            bool allConnected = false;
            int maxFixes = 100; // 防止无限循环
            while (!allConnected && maxFixes > 0)
            {
                allConnected = true;
                foreach (var tile in roomTiles)
                {
                    if (!dist.ContainsKey(tile.gridPos))
                    {
                        // 从最近的已连通房间打通到此
                        var nearest = FindNearestConnected(tile.gridPos, dist);
                        if (nearest != null)
                        {
                            CarvePath(nearest.Value, tile.gridPos);
                        }
                        else
                        {
                            CarvePath(startPos, tile.gridPos);
                        }
                        dist = BFS(startPos);
                        allConnected = false;
                        maxFixes--;
                        break;
                    }
                }
            }
        }

        /// <summary>BFS返回从起点到各房间的距离</summary>
        private Dictionary<GridPos, int> BFS(GridPos from)
        {
            var distances = new Dictionary<GridPos, int>();
            var queue = new Queue<GridPos>();
            queue.Enqueue(from);
            distances[from] = 0;

            int[] dx = { 0, 0, -1, 1 };
            int[] dy = { -1, 1, 0, 0 };

            while (queue.Count > 0)
            {
                var pos = queue.Dequeue();
                int curDist = distances[pos];

                for (int i = 0; i < 4; i++)
                {
                    var np = new GridPos(pos.x + dx[i], pos.y + dy[i]);
                    if (!IsInBounds(np.x, np.y)) continue;
                    if (distances.ContainsKey(np)) continue;

                    var tile = grid[np.x, np.y];
                    if (tile.tileType == TileType.Room)
                    {
                        distances[np] = curDist + 1;
                        queue.Enqueue(np);
                    }
                }
            }

            return distances;
        }

        /// <summary>找到距离目标最近的已连通格子</summary>
        private GridPos? FindNearestConnected(GridPos target, Dictionary<GridPos, int> connected)
        {
            GridPos? best = null;
            int bestDist = int.MaxValue;

            foreach (var kv in connected)
            {
                int d = Mathf.Abs(kv.Key.x - target.x) + Mathf.Abs(kv.Key.y - target.y);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = kv.Key;
                }
            }

            return best;
        }

        /// <summary>在两个坐标之间打通路径（直线路径，将中间Wall变为Room）</summary>
        private void CarvePath(GridPos from, GridPos to)
        {
            int x = from.x;
            int y = from.y;

            // 先水平后垂直
            while (x != to.x)
            {
                x += (to.x > x) ? 1 : -1;
                CarveCell(x, y);
            }
            while (y != to.y)
            {
                y += (to.y > y) ? 1 : -1;
                CarveCell(x, y);
            }
        }

        /// <summary>将指定格子从Wall变为Room（如果还是Wall的话）</summary>
        private void CarveCell(int x, int y)
        {
            if (!IsInBounds(x, y)) return;
            if (grid[x, y].tileType == TileType.Room) return;

            grid[x, y].tileType = TileType.Room;
            grid[x, y].width = 1;
            grid[x, y].height = 1;
            roomTiles.Add(grid[x, y]);
        }

        /// <summary>遍历所有Room，建立四方向连通标记</summary>
        private void BuildConnections()
        {
            // 先清零
            foreach (var tile in roomTiles)
            {
                tile.connectUp = false;
                tile.connectDown = false;
                tile.connectLeft = false;
                tile.connectRight = false;
            }

            // 遍历所有Room，检查四方向邻居
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var tile = grid[x, y];
                    if (tile.tileType != TileType.Room) continue;

                    // 上（y-1）
                    if (y > 0 && grid[x, y - 1] != null && grid[x, y - 1].tileType == TileType.Room)
                        tile.connectUp = true;
                    // 下（y+1）
                    if (y < gridHeight - 1 && grid[x, y + 1] != null && grid[x, y + 1].tileType == TileType.Room)
                        tile.connectDown = true;
                    // 左（x-1）
                    if (x > 0 && grid[x - 1, y] != null && grid[x - 1, y].tileType == TileType.Room)
                        tile.connectLeft = true;
                    // 右（x+1）
                    if (x < gridWidth - 1 && grid[x + 1, y] != null && grid[x + 1, y].tileType == TileType.Room)
                        tile.connectRight = true;
                }
            }
        }

        /// <summary>
        /// 房间内容分配（生成后）：Monster 40%/Elite 8%/Treasure 10%/Spring 8%/Merchant 8%/Shrine 5%/Empty 16%
        /// Boss和Exit已由PlaceSpecialRooms放置，不再重复分配
        /// </summary>
        private void AssignRoomContent()
        {
            // 收集可分配的房间（排除起点、Boss、Exit、已有Shrine）
            var assignable = new List<DungeonTile>();
            foreach (var tile in roomTiles)
            {
                if (tile.gridPos == startPos) continue;
                if (tile.roomType == DungeonEventType.Boss) continue;
                if (tile.roomType == DungeonEventType.Exit) continue;
                if (tile.roomType == DungeonEventType.Shrine) continue;
                assignable.Add(tile);
            }

            // 按概率分配
            foreach (var tile in assignable)
            {
                float roll = Random.value;
                if (roll < 0.40f)
                    tile.roomType = DungeonEventType.Monster;
                else if (roll < 0.48f)
                    tile.roomType = DungeonEventType.Elite;
                else if (roll < 0.58f)
                    tile.roomType = DungeonEventType.Treasure;
                else if (roll < 0.66f)
                    tile.roomType = DungeonEventType.Spring;
                else if (roll < 0.74f)
                    tile.roomType = DungeonEventType.Merchant;
                else
                    tile.roomType = DungeonEventType.Empty;
            }
        }

        /// <summary>从网格数据同步填充旧的线性rooms列表，保持向后兼容</summary>
        private void SyncRoomsFromGrid()
        {
            rooms.Clear();
            currentRoomIndex = 0;

            // 起点房间作为第一个（索引0）
            var startTile = GetTileAt(startPos);
            if (startTile != null)
            {
                rooms.Add(new DungeonRoom
                {
                    type = startTile.roomType,
                    cleared = startTile.isCleared,
                    position = new Vector2Int(startPos.x, startPos.y)
                });
            }

            // BFS遍历所有可达房间，按距离排序
            var dist = BFS(startPos);
            var sorted = new List<DungeonTile>(roomTiles);
            sorted.Sort((a, b) =>
            {
                int da = dist.ContainsKey(a.gridPos) ? dist[a.gridPos] : int.MaxValue;
                int db = dist.ContainsKey(b.gridPos) ? dist[b.gridPos] : int.MaxValue;
                return da.CompareTo(db);
            });

            foreach (var tile in sorted)
            {
                // 跳过起点（已添加）
                if (tile.gridPos == startPos) continue;

                rooms.Add(new DungeonRoom
                {
                    type = tile.roomType,
                    cleared = tile.isCleared,
                    position = new Vector2Int(tile.gridPos.x, tile.gridPos.y)
                });
            }
        }

        /// <summary>
        /// 随机游走生成 — 使用前沿列表进行扩张
        /// </summary>
        private void GenerateGrid()
        {
            // Step 1: 计算网格尺寸 6 + min(floor-1, 4)
            int size = 6 + Mathf.Min(floor - 1, 4);
            gridWidth = size;
            gridHeight = size;

            int totalCells = gridWidth * gridHeight;
            int targetRooms = Mathf.RoundToInt(totalCells * Random.Range(0.40f, 0.55f));

            // Step 2(a): 全部初始化为Wall
            grid = new DungeonTile[gridWidth, gridHeight];
            roomTiles.Clear();

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = new DungeonTile
                    {
                        gridPos = new GridPos(x, y),
                        tileType = TileType.Wall
                    };
                }
            }

            // Step 2(b): 在网格中心附近放置起点
            int cx = gridWidth / 2;
            int cy = gridHeight / 2;
            cx = Mathf.Clamp(cx + Random.Range(-1, 2), 1, gridWidth - 2);
            cy = Mathf.Clamp(cy + Random.Range(-1, 2), 1, gridHeight - 2);

            SetRoom(cx, cy, 1, 1);

            List<GridPos> frontier = new List<GridPos>();
            _currentFrontier = frontier;
            AddFrontierWalls(cx, cy, frontier);

            int roomCount = 1;

            // Step 2(c): 随机游走扩张
            while (roomCount < targetRooms && frontier.Count > 0)
            {
                int idx = Random.Range(0, frontier.Count);
                GridPos wall = frontier[idx];
                frontier.RemoveAt(idx);

                if (grid[wall.x, wall.y].tileType == TileType.Room) continue;

                // 60%概率变为Room
                if (Random.value > 0.60f) continue;

                // 尝试多尺寸房间
                bool placed = TryPlaceMultiRoom(wall.x, wall.y, ref roomCount);

                if (!placed)
                {
                    SetRoom(wall.x, wall.y, 1, 1);
                    roomCount++;
                    AddFrontierWalls(wall.x, wall.y, frontier);
                }
            }

            _currentFrontier = null;

            // Step 2(d): 放置特殊房间
            PlaceSpecialRooms();

            // Step 2(e): BFS连通性保证
            EnsureConnectivity();

            // Step 2(f): 连通性标记
            BuildConnections();

            // 房间内容分配
            AssignRoomContent();

            // 起点默认已探索
            var startTile = GetTileAt(startPos);
            if (startTile != null)
            {
                startTile.isExplored = true;
                startTile.isCleared = true;
                startTile.roomType = DungeonEventType.Empty;
            }

            currentPos = startPos;

            // 初始化战争迷雾
            InitVisibility();
        }

        // ---- 战争迷雾 ----

        /// <summary>四方向偏移量（上/下/左/右）</summary>
        private static readonly int[] DirX = { 0, 0, -1, 1 };
        private static readonly int[] DirY = { -1, 1, 0, 0 };

        /// <summary>初始化战争迷雾：所有tile=Hidden，起点及相邻四方向=Visible</summary>
        public void InitVisibility()
        {
            // 全部标记为未探索
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y].isExplored = false;
                }
            }

            // 起点、起点相邻格子设为可见
            SetVisibleArea(startPos);
        }

        /// <summary>获取指定坐标的可见性状态</summary>
        public TileVisibility GetTileVisibility(int x, int y)
        {
            var tile = GetTileAt(x, y);
            if (tile == null) return TileVisibility.Hidden;

            // 当前玩家位置 → Visible
            if (currentPos.x == x && currentPos.y == y)
                return TileVisibility.Visible;

            // 当前位置的相邻格子 → Visible
            for (int i = 0; i < 4; i++)
            {
                if (currentPos.x + DirX[i] == x && currentPos.y + DirY[i] == y)
                    return TileVisibility.Visible;
            }

            // 已探索但不在视野内 → Explored
            if (tile.isExplored)
                return TileVisibility.Explored;

            // 未探索 → Hidden
            return TileVisibility.Hidden;
        }

        /// <summary>玩家移动到新位置：更新迷雾可见性</summary>
        public void OnMoveTo(GridPos newPos)
        {
            // 标记旧位置的相邻格子为已探索（如果不在新可见范围内）
            MarkExploredAround(currentPos, newPos);

            // 更新当前位置
            currentPos = newPos;

            // 标记新位置及相邻为可见
            SetVisibleArea(newPos);
        }

        /// <summary>将指定位置及四方向相邻格子标记为已探索</summary>
        private void SetVisibleArea(GridPos pos)
        {
            var centerTile = GetTileAt(pos);
            if (centerTile != null) centerTile.isExplored = true;

            for (int i = 0; i < 4; i++)
            {
                var tile = GetTileAt(pos.x + DirX[i], pos.y + DirY[i]);
                if (tile != null) tile.isExplored = true;
            }
        }

        /// <summary>将旧位置周围不再在新可见范围内的格子标记为已探索</summary>
        private void MarkExploredAround(GridPos oldPos, GridPos newPos)
        {
            // 收集新位置可见范围内的坐标
            var newVisible = new HashSet<GridPos>();
            newVisible.Add(newPos);
            for (int i = 0; i < 4; i++)
            {
                newVisible.Add(new GridPos(newPos.x + DirX[i], newPos.y + DirY[i]));
            }

            // 旧位置本身
            var oldTile = GetTileAt(oldPos);
            if (oldTile != null && !newVisible.Contains(oldPos))
                oldTile.isExplored = true;

            // 旧位置相邻
            for (int i = 0; i < 4; i++)
            {
                var nx = oldPos.x + DirX[i];
                var ny = oldPos.y + DirY[i];
                var np = new GridPos(nx, ny);
                var tile = GetTileAt(nx, ny);
                if (tile != null && !newVisible.Contains(np))
                    tile.isExplored = true;
            }
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
