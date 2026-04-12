using System.Collections.Generic;
using Game.Config;
using UnityEngine;

namespace Game.Data
{
    [System.Serializable]
    public class DungeonData
    {
        public int floor = 1;
        public List<DungeonRoom> rooms = new List<DungeonRoom>();
        public int currentRoomIndex;
        public int maxFloor = 10;

        public DungeonRoom CurrentRoom => currentRoomIndex < rooms.Count ? rooms[currentRoomIndex] : null;

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
        }

        public void OnReturnTown()
        {
            // 返回城镇后，小怪和泉水恢复
            foreach (var room in rooms)
            {
                if (room.type == DungeonEventType.Monster || room.type == DungeonEventType.Spring)
                    room.cleared = false;
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
