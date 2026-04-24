using System;
using System.Collections.Generic;
using Game.Config;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 网格坐标（简单值类型，避免依赖Unity.Vector2Int的序列化问题）
    /// </summary>
    [Serializable]
    public struct GridPos
    {
        public int x;
        public int y;

        public GridPos(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static readonly GridPos Zero = new GridPos(0, 0);

        public static bool operator ==(GridPos a, GridPos b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(GridPos a, GridPos b) => !(a == b);
        public override bool Equals(object obj) => obj is GridPos p && this == p;
        public override int GetHashCode() => x * 1000 + y;
    }

    /// <summary>
    /// 地牢网格瓦片 — 代表2D网格中的一个格子（房间或墙壁）
    /// </summary>
    [Serializable]
    public class DungeonTile
    {
        // ---- 网格定位 ----

        /// <summary>网格坐标</summary>
        public GridPos gridPos;

        /// <summary>瓦片类型（Room=可通行, Wall=不可通行）</summary>
        public TileType tileType = TileType.Wall;

        /// <summary>房间类型（仅 tileType==Room 时有效）</summary>
        public DungeonEventType roomType = DungeonEventType.Empty;

        /// <summary>房间尺寸（默认1x1，支持多尺寸房间）</summary>
        public int width = 1;

        /// <summary>房间尺寸（默认1x1，支持多尺寸房间）</summary>
        public int height = 1;

        // ---- 探索状态 ----

        /// <summary>是否已探索（玩家曾到达相邻格）</summary>
        public bool isExplored;

        /// <summary>是否已清理（完成事件/战斗）</summary>
        public bool isCleared;

        // ---- 连通性：四方向（上/下/左/右），相邻格为Room时为true ----

        /// <summary>上方向连通</summary>
        public bool connectUp;

        /// <summary>下方向连通</summary>
        public bool connectDown;

        /// <summary>左方向连通</summary>
        public bool connectLeft;

        /// <summary>右方向连通</summary>
        public bool connectRight;

        // ---- 内容数据 ----

        /// <summary>房间内容数据（怪物/宝箱/金币等）</summary>
        public ContentData content = new ContentData();

        /// <summary>
        /// 获取指定方向的连通状态
        /// </summary>
        public bool GetConnection(int direction)
        {
            switch (direction)
            {
                case 0: return connectUp;
                case 1: return connectDown;
                case 2: return connectLeft;
                case 3: return connectRight;
                default: return false;
            }
        }

        /// <summary>
        /// 设置指定方向的连通状态
        /// </summary>
        public void SetConnection(int direction, bool value)
        {
            switch (direction)
            {
                case 0: connectUp = value; break;
                case 1: connectDown = value; break;
                case 2: connectLeft = value; break;
                case 3: connectRight = value; break;
            }
        }

        /// <summary>
        /// 是否为可通行的房间类型
        /// </summary>
        public bool IsRoom => tileType == TileType.Room;
    }

    /// <summary>
    /// 地牢瓦片内容数据 — 保留现有DungeonRoom的内容字段
    /// </summary>
    [Serializable]
    public class ContentData
    {
        /// <summary>怪物配置ID</summary>
        public int monsterId;

        /// <summary>宝物配置ID</summary>
        public int treasureId;

        /// <summary>金币数量</summary>
        public int goldAmount;

        /// <summary>物品配置ID列表（商人售卖物品）</summary>
        public List<int> itemIds = new List<int>();

        /// <summary>技能配置ID（神殿技能）</summary>
        public int skillId;

        /// <summary>怪物等级缩放</summary>
        public int monsterLevel;
    }
}
