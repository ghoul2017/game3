using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [System.Serializable]
    public class MonsterData
    {
        public int id;
        public string name;
        public int hp;
        public int mp;
        public int attack;
        public int defense;
        public int speed;
        public List<int> skillIds = new List<int>();
        public int expReward;
        public int goldReward;
        public List<ItemDrop> drops = new List<ItemDrop>();
    }

    [System.Serializable]
    public class ItemDrop
    {
        public int itemId;
        public float chance; // 0.0 ~ 1.0
    }
}
