using System.Collections.Generic;
using Game.Battle;
using UnityEngine;

namespace Game.Config
{
    [System.Serializable]
    public class ProfessionConfig
    {
        public Profession profession;
        public string name;
        public string desc;
        public int strBase;   public int strGrow;
        public int agiBase;   public int agiGrow;
        public int intBase;   public int intGrow;
        public int vitBase;   public int vitGrow;
        public int hpBase;    public int mpBase;
        public int atkBase;   public int defBase;
        public int critBase;  public int dodgeBase;
        public int speedBase;
        public List<int> defaultSkillIds = new List<int>();
    }

    [System.Serializable]
    public class SkillConfig
    {
        public int id;
        public string name;
        public string desc;
        public SkillType type;
        public Profession profession;
        public int mpCost;
        public int power;       // 技能威力（基于攻击力的倍率百分比）
        public int level;       // 技能等级
        public int prereqSkillId; // 前置技能ID，0=无
        public int learnCost;   // 学习金币花费
        public bool isAoE;
        public bool isHeal;     // 是否为治疗技能（恢复自身HP）
        public int buffId;      // Buff效果ID，0=无
        public int buffTurns;   // Buff持续回合
        public int debuffAttack;  // 降低敌人攻击力（百分比）
        public int debuffDefense; // 降低敌人防御力（百分比）
        public int debuffSpeed;   // 降低敌人速度（固定值）
        public int debuffTurns;   // Debuff持续回合

        // 状态效果技能字段（StatusAttack类型使用）
        public EffectType statusEffect;  // 附加的状态效果类型
        public int statusDuration;       // 状态持续回合
        public float statusPowerRate;    // 状态效果威力倍率（如流血=ATK*0.25中的0.25）
    }

    [System.Serializable]
    public class ItemConfig
    {
        public int id;
        public string name;
        public string desc;
        public ItemType type;
        public int price;
        public int atk;
        public int def;
        public int hp;
        public int mp;
        public int crit;
        public int dodge;
        public int restoreHp;
        public int restoreMp;
        public int reqLevel;
    }

    [System.Serializable]
    public class MonsterConfig
    {
        public int id;
        public string name;
        public int hp; public int mp;
        public int attack; public int defense; public int speed;
        public int expReward; public int goldReward;
        public int minFloor;
        public List<int> skillIds = new List<int>();
        public List<ItemDropConfig> drops = new List<ItemDropConfig>();
        public bool isBoss;
    }

    [System.Serializable]
    public class ItemDropConfig
    {
        public int itemId;
        public float chance;
    }

    [System.Serializable]
    public class BuffConfig
    {
        public int id;
        public string name;
        public string desc;
        public int effectType;  // 0=攻击加成 1=防御加成 2=攻防加成
        public int effectValue; // 加成百分比
    }

    [System.Serializable]
    public class TreasureConfig
    {
        public int id;
        public string name;
        public string desc;
        public int effectType; // 0=攻击加成 1=防御加成 2=生命加成 3=特殊效果
        public int effectValue;
    }
}
