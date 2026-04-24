using System.Collections.Generic;
using Game.Config;
using Game.Data;

namespace Game.Data
{
    public class BattleData
    {
        public BattleMonster monster;
        public DungeonRoom room;
        public bool isPlayerTurn = true;
        public bool isFinished;
        public bool isVictory;
        public string lastAction;
        public List<string> log = new List<string>();

        // 玩家buff（与怪物buff对称）
        public int playerBuffAttack;   // 攻击buff加成
        public int playerBuffDefense;  // 防御buff加成
        public int playerBuffTurns;    // buff剩余回合

        // 战斗奖励
        public int rewardExp;
        public int rewardGold;
        public List<string> rewardItems = new List<string>();
    }

    [System.Serializable]
    public class BattleMonster
    {
        public int id;
        public string name;
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int attack;
        public int defense;
        public int speed;
        public List<int> skillIds = new List<int>();
        public int expReward;
        public int goldReward;
        public List<ItemDropConfig> drops = new List<ItemDropConfig>();
        public bool isBoss;
        public int level;
        public int buffAttack;   // 攻击buff加成
        public int buffDefense;  // 防御buff加成
        public int buffTurns;    // buff剩余回合
    }
}
