using System.Collections.Generic;
using Game.Config;
using UnityEngine;

namespace Game.Data
{
    [System.Serializable]
    public class PlayerData
    {
        public string name;
        public Profession profession;
        public int level = 1;
        public int exp;

        // 基础属性
        public int strength;
        public int agility;
        public int intelligence;
        public int vitality;

        // 战斗属性（计算得出）
        public int maxHp;
        public int hp;
        public int maxMp;
        public int mp;
        public int attack;
        public int defense;
        public int critRate;
        public int dodgeRate;
        public int speed;

        // 经济
        public int gold;

        // 装备槽
        public int weaponId;
        public int armorId;
        public int accessoryId;

        // 背包
        public List<int> bagItems = new List<int>();
        public int maxBagSize = 20;

        // 技能
        public List<int> learnedSkills = new List<int>();
        public List<int> equippedSkills = new List<int>();
        public int maxSkillSlots = 4;

        // 宝物
        public List<int> treasures = new List<int>();

        // 地牢进度
        public int dungeonFloor;
        public int dungeonRoomIndex;

        // Boss击杀记录（用于成就/统计）
        public List<int> defeatedBosses = new List<int>();

        public void RecalcStats()
        {
            var profCfg = ConfigManager.Instance.GetProfession(profession);
            if (profCfg == null) return;

            float lvl = level;
            maxHp = (int)((vitality * 10 + profCfg.hpBase) * (1 + lvl * 0.05f));
            maxMp = (int)((intelligence * 5 + profCfg.mpBase) * (1 + lvl * 0.03f));
            attack = (int)((strength * 2 + profCfg.atkBase) * (1 + lvl * 0.04f));
            defense = (int)((vitality * 1.5f + profCfg.defBase) * (1 + lvl * 0.03f));
            critRate = agility * 1 + profCfg.critBase;
            dodgeRate = agility * 1 + profCfg.dodgeBase;
            speed = agility + profCfg.speedBase;

            // 装备加成
            var weapon = ConfigManager.Instance.GetItem(weaponId);
            var armor = ConfigManager.Instance.GetItem(armorId);
            var accessory = ConfigManager.Instance.GetItem(accessoryId);
            if (weapon != null)
            {
                attack += weapon.atk;
                maxHp += weapon.hp;
                maxMp += weapon.mp;
                speed += weapon.speed;
                critRate += weapon.crit;
                dodgeRate += weapon.dodge;
                // 装备特殊效果：暴击率
                if (weapon.equipEffectType == EquipEffectType.CritChance)
                    critRate += weapon.equipEffectValue;
                // 装备特殊效果：闪避
                if (weapon.equipEffectType == EquipEffectType.Dodge)
                    dodgeRate += weapon.equipEffectValue;
            }
            if (armor != null)
            {
                defense += armor.def;
                maxHp += armor.hp;
                maxMp += armor.mp;
                speed += armor.speed;
                critRate += armor.crit;
                dodgeRate += armor.dodge;
                // 装备特殊效果：暴击率
                if (armor.equipEffectType == EquipEffectType.CritChance)
                    critRate += armor.equipEffectValue;
                // 装备特殊效果：闪避
                if (armor.equipEffectType == EquipEffectType.Dodge)
                    dodgeRate += armor.equipEffectValue;
            }
            if (accessory != null)
            {
                attack += accessory.atk;
                defense += accessory.def;
                maxHp += accessory.hp;
                maxMp += accessory.mp;
                speed += accessory.speed;
                critRate += accessory.crit;
                dodgeRate += accessory.dodge;
                // 装备特殊效果：暴击率
                if (accessory.equipEffectType == EquipEffectType.CritChance)
                    critRate += accessory.equipEffectValue;
                // 装备特殊效果：闪避
                if (accessory.equipEffectType == EquipEffectType.Dodge)
                    dodgeRate += accessory.equipEffectValue;
            }

            // 宝物加成（百分比）
            foreach (var tid in treasures)
            {
                var tc = ConfigManager.Instance.GetTreasure(tid);
                if (tc == null) continue;
                switch (tc.effectType)
                {
                    case 0: attack += (int)(attack * tc.effectValue / 100f); break;   // ATK%
                    case 1: defense += (int)(defense * tc.effectValue / 100f); break;  // DEF%
                    case 2: maxHp += (int)(maxHp * tc.effectValue / 100f); break;     // HP%
                }
            }

            if (hp > maxHp) hp = maxHp;
            if (mp > maxMp) mp = maxMp;
        }

        public int GetExpToNextLevel()
        {
            return level * 100 + 50;
        }

        public void AddExp(int amount)
        {
            exp += amount;
            while (exp >= GetExpToNextLevel())
            {
                exp -= GetExpToNextLevel();
                level++;
                Debug.Log($"[角色] 升级！Lv.{level}");
                var profCfg = ConfigManager.Instance.GetProfession(profession);
                strength += profCfg.strGrow;
                agility += profCfg.agiGrow;
                intelligence += profCfg.intGrow;
                vitality += profCfg.vitGrow;
                RecalcStats();
                hp = maxHp;
                mp = maxMp;
            }
        }

        public void FullRestore()
        {
            hp = maxHp;
            mp = maxMp;
        }

        public bool IsAlive => hp > 0;

        /// <summary>
        /// 学习技能：将技能加入已学列表（不重复）
        /// </summary>
        public bool LearnSkill(int skillId)
        {
            if (learnedSkills.Contains(skillId)) return false;
            learnedSkills.Add(skillId);
            return true;
        }
    }
}
