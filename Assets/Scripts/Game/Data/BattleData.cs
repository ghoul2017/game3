using System.Collections.Generic;
using Game.Battle;
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
        public int playerBuffSpeed;    // 速度buff加成
        public int playerBuffTurns;    // buff剩余回合

        // 状态效果列表
        public List<StatusEffect> playerEffects = new List<StatusEffect>();
        public List<StatusEffect> monsterEffects = new List<StatusEffect>();

        // 战斗奖励
        public int rewardExp;
        public int rewardGold;
        public List<string> rewardItems = new List<string>();

        // 护盾系统
        public int shieldAmount;    // 当前护盾值，优先吸收伤害

        // Boss战特殊标记
        public bool isBossBattle;          // 是否Boss战
        public bool bossDefeated;          // Boss是否被击败（用于记录到defeatedBosses）
        public string bossKillTitle;       // Boss击杀标识（如"击败了暗影领主！"）
        public bool bossSkillTriggered;    // Boss使用技能标记（UI层读取后触发震动并重置）
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
        public MonsterAIPattern aiPattern = MonsterAIPattern.Balanced;
        public int level;
        public int buffAttack;   // 攻击buff加成
        public int buffDefense;  // 防御buff加成
        public int buffSpeed;    // 速度buff加成
        public int buffTurns;    // buff剩余回合
    }
}
