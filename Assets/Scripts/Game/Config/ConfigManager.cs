using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Config
{
    /// <summary>
    /// 配置管理器，加载和管理游戏配置表
    /// </summary>
    public class ConfigManager
    {
        private static ConfigManager instance;
        public static ConfigManager Instance => instance ??= new ConfigManager();

        private Dictionary<Profession, ProfessionConfig> professions;
        private Dictionary<int, SkillConfig> skills;
        private Dictionary<int, ItemConfig> items;
        private Dictionary<int, MonsterConfig> monsters;
        private Dictionary<int, TreasureConfig> treasures;
        private Dictionary<int, BuffConfig> buffs;

        public void Init()
        {
            InitProfessions();
            InitSkills();
            InitItems();
            InitMonsters();
            InitTreasures();
            InitBuffs();
            Debug.Log("[ConfigManager] 配置加载完成");
        }

        public ProfessionConfig GetProfession(Profession prof)
            => professions.TryGetValue(prof, out var c) ? c : null;

        public SkillConfig GetSkill(int id)
            => skills.TryGetValue(id, out var c) ? c : null;

        public List<SkillConfig> GetSkillsByProfession(Profession prof)
            => skills.Values.Where(s => s.profession == prof).ToList();

        public ItemConfig GetItem(int id)
            => items.TryGetValue(id, out var c) ? c : null;

        public List<ItemConfig> GetItemsByType(ItemType type)
            => items.Values.Where(i => i.type == type).ToList();

        public MonsterConfig GetMonster(int id)
            => monsters.TryGetValue(id, out var c) ? c : null;

        public List<MonsterConfig> GetMonstersByFloor(int floor)
            => monsters.Values.Where(m => m.minFloor <= floor && !m.isBoss).ToList();

        public MonsterConfig GetBossByFloor(int floor)
            => monsters.Values.FirstOrDefault(m => m.isBoss && m.minFloor <= floor);

        public TreasureConfig GetTreasure(int id)
            => treasures.TryGetValue(id, out var c) ? c : null;

        public List<TreasureConfig> GetAllTreasures()
            => treasures.Values.ToList();

        public BuffConfig GetBuff(int id)
            => buffs.TryGetValue(id, out var c) ? c : null;

        // ---- 初始化配置 ----

        private void InitProfessions()
        {
            professions = new Dictionary<Profession, ProfessionConfig>
            {
                [Profession.Warrior] = new ProfessionConfig
                {
                    profession = Profession.Warrior, name = "战士", desc = "近战物理输出，高攻击高生命",
                    strBase = 12, strGrow = 3, agiBase = 6, agiGrow = 1,
                    intBase = 4, intGrow = 1, vitBase = 10, vitGrow = 2,
                    hpBase = 50, mpBase = 20, atkBase = 8, defBase = 4,
                    critBase = 5, dodgeBase = 3,
                    defaultSkillIds = new List<int> { 1001 }
                },
                [Profession.Ranger] = new ProfessionConfig
                {
                    profession = Profession.Ranger, name = "游侠", desc = "远程物理输出，高暴击高闪避",
                    strBase = 8, strGrow = 2, agiBase = 12, agiGrow = 3,
                    intBase = 6, intGrow = 1, vitBase = 6, vitGrow = 1,
                    hpBase = 35, mpBase = 30, atkBase = 6, defBase = 2,
                    critBase = 10, dodgeBase = 10,
                    defaultSkillIds = new List<int> { 2001 }
                },
                [Profession.Knight] = new ProfessionConfig
                {
                    profession = Profession.Knight, name = "骑士", desc = "近战坦克，高防御高生命",
                    strBase = 8, strGrow = 2, agiBase = 4, agiGrow = 1,
                    intBase = 4, intGrow = 1, vitBase = 14, vitGrow = 3,
                    hpBase = 70, mpBase = 15, atkBase = 5, defBase = 10,
                    critBase = 3, dodgeBase = 2,
                    defaultSkillIds = new List<int> { 3001 }
                },
                [Profession.Priest] = new ProfessionConfig
                {
                    profession = Profession.Priest, name = "牧师", desc = "治疗辅助，高法力高恢复",
                    strBase = 4, strGrow = 1, agiBase = 6, agiGrow = 1,
                    intBase = 12, intGrow = 3, vitBase = 8, vitGrow = 2,
                    hpBase = 40, mpBase = 60, atkBase = 3, defBase = 3,
                    critBase = 4, dodgeBase = 5,
                    defaultSkillIds = new List<int> { 4001 }
                },
                [Profession.Mage] = new ProfessionConfig
                {
                    profession = Profession.Mage, name = "法师", desc = "远程魔法输出，高魔法伤害",
                    strBase = 4, strGrow = 1, agiBase = 6, agiGrow = 1,
                    intBase = 14, intGrow = 3, vitBase = 6, vitGrow = 1,
                    hpBase = 30, mpBase = 70, atkBase = 4, defBase = 2,
                    critBase = 6, dodgeBase = 4,
                    defaultSkillIds = new List<int> { 5001 }
                }
            };
        }

        private void InitSkills()
        {
            skills = new Dictionary<int, SkillConfig>
            {
                // 战士技能
                [1001] = new SkillConfig { id = 1001, name = "猛击", desc = "强力的物理攻击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 5, power = 150, level = 1 },
                [1002] = new SkillConfig { id = 1002, name = "战吼", desc = "提升自身攻击力", type = SkillType.Buff, profession = Profession.Warrior, mpCost = 8, power = 0, level = 1, buffId = 1, buffTurns = 3 },
                [1003] = new SkillConfig { id = 1003, name = "旋风斩", desc = "强力横扫攻击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 12, power = 200, level = 3, prereqSkillId = 1001, isAoE = true },
                [1004] = new SkillConfig { id = 1004, name = "狂战之怒", desc = "血量越低攻击越高", type = SkillType.Passive, profession = Profession.Warrior, mpCost = 0, power = 0, level = 5, prereqSkillId = 1002 },

                // 游侠技能
                [2001] = new SkillConfig { id = 2001, name = "精准射击", desc = "高暴击的远程攻击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 5, power = 140, level = 1 },
                [2002] = new SkillConfig { id = 2002, name = "闪避姿态", desc = "提升闪避率", type = SkillType.Buff, profession = Profession.Ranger, mpCost = 6, power = 0, level = 1, buffId = 2, buffTurns = 3 },
                [2003] = new SkillConfig { id = 2003, name = "连射", desc = "连续两次攻击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 10, power = 120, level = 3, prereqSkillId = 2001 },
                [2004] = new SkillConfig { id = 2004, name = "致命一击", desc = "必定暴击的一击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 15, power = 250, level = 5, prereqSkillId = 2003 },

                // 骑士技能
                [3001] = new SkillConfig { id = 3001, name = "盾击", desc = "用盾牌攻击并降低敌人攻击", type = SkillType.Active, profession = Profession.Knight, mpCost = 5, power = 100, level = 1 },
                [3002] = new SkillConfig { id = 3002, name = "铁壁", desc = "大幅提升防御", type = SkillType.Buff, profession = Profession.Knight, mpCost = 8, power = 0, level = 1, buffId = 3, buffTurns = 3 },
                [3003] = new SkillConfig { id = 3003, name = "嘲讽", desc = "吸引敌人攻击并减伤", type = SkillType.Active, profession = Profession.Knight, mpCost = 6, power = 80, level = 3, prereqSkillId = 3001 },
                [3004] = new SkillConfig { id = 3004, name = "坚守", desc = "生命低于30%时自动减伤", type = SkillType.Passive, profession = Profession.Knight, mpCost = 0, power = 0, level = 5, prereqSkillId = 3002 },

                // 牧师技能
                [4001] = new SkillConfig { id = 4001, name = "治疗术", desc = "恢复自身生命值", type = SkillType.Active, profession = Profession.Priest, mpCost = 8, power = 100, level = 1, isHeal = true },
                [4002] = new SkillConfig { id = 4002, name = "祝福", desc = "提升全属性", type = SkillType.Buff, profession = Profession.Priest, mpCost = 10, power = 0, level = 1, buffId = 4, buffTurns = 3 },
                [4003] = new SkillConfig { id = 4003, name = "圣光", desc = "神圣伤害攻击", type = SkillType.Active, profession = Profession.Priest, mpCost = 12, power = 160, level = 3, prereqSkillId = 4001 },
                [4004] = new SkillConfig { id = 4004, name = "复活", desc = "战斗中自动复活一次", type = SkillType.Passive, profession = Profession.Priest, mpCost = 0, power = 0, level = 5, prereqSkillId = 4002 },

                // 法师技能
                [5001] = new SkillConfig { id = 5001, name = "火球术", desc = "基础火系魔法", type = SkillType.Active, profession = Profession.Mage, mpCost = 6, power = 160, level = 1 },
                [5002] = new SkillConfig { id = 5002, name = "冰甲", desc = "提升防御和减伤", type = SkillType.Buff, profession = Profession.Mage, mpCost = 8, power = 0, level = 1, buffId = 5, buffTurns = 3 },
                [5003] = new SkillConfig { id = 5003, name = "暴风雪", desc = "强力冰系范围攻击", type = SkillType.Active, profession = Profession.Mage, mpCost = 15, power = 220, level = 3, prereqSkillId = 5001, isAoE = true },
                [5004] = new SkillConfig { id = 5004, name = "魔力涌动", desc = "每回合恢复法力", type = SkillType.Passive, profession = Profession.Mage, mpCost = 0, power = 0, level = 5, prereqSkillId = 5002 }
            };
        }

        private void InitItems()
        {
            items = new Dictionary<int, ItemConfig>
            {
                // 武器
                [1001] = new ItemConfig { id = 1001, name = "铁剑", desc = "普通的铁剑", type = ItemType.Weapon, price = 50, atk = 5, reqLevel = 1 },
                [1002] = new ItemConfig { id = 1002, name = "钢剑", desc = "精钢打造的长剑", type = ItemType.Weapon, price = 150, atk = 10, reqLevel = 3 },
                [1003] = new ItemConfig { id = 1003, name = "秘银剑", desc = "秘银打造的利剑", type = ItemType.Weapon, price = 400, atk = 18, reqLevel = 6 },
                [1004] = new ItemConfig { id = 1004, name = "短弓", desc = "猎人使用的短弓", type = ItemType.Weapon, price = 50, atk = 4, crit = 3, reqLevel = 1 },
                [1005] = new ItemConfig { id = 1005, name = "长弓", desc = "精制的长弓", type = ItemType.Weapon, price = 160, atk = 9, crit = 5, reqLevel = 3 },
                [1006] = new ItemConfig { id = 1006, name = "法杖", desc = "基础法杖", type = ItemType.Weapon, price = 60, atk = 3, mp = 10, reqLevel = 1 },
                [1007] = new ItemConfig { id = 1007, name = "秘法之杖", desc = "蕴含魔力的法杖", type = ItemType.Weapon, price = 200, atk = 7, mp = 25, reqLevel = 4 },

                // 防具
                [2001] = new ItemConfig { id = 2001, name = "皮甲", desc = "轻便的皮甲", type = ItemType.Armor, price = 40, def = 3, hp = 10, reqLevel = 1 },
                [2002] = new ItemConfig { id = 2002, name = "锁子甲", desc = "环环相扣的锁甲", type = ItemType.Armor, price = 120, def = 7, hp = 25, reqLevel = 3 },
                [2003] = new ItemConfig { id = 2003, name = "板甲", desc = "重型板甲", type = ItemType.Armor, price = 350, def = 14, hp = 50, reqLevel = 6 },
                [2004] = new ItemConfig { id = 2004, name = "法师长袍", desc = "附魔的长袍", type = ItemType.Armor, price = 80, def = 2, mp = 15, reqLevel = 1 },

                // 饰品
                [3001] = new ItemConfig { id = 3001, name = "力量戒指", desc = "增加暴击率", type = ItemType.Accessory, price = 80, crit = 5, reqLevel = 1 },
                [3002] = new ItemConfig { id = 3002, name = "敏捷护符", desc = "增加闪避率", type = ItemType.Accessory, price = 80, dodge = 5, reqLevel = 1 },
                [3003] = new ItemConfig { id = 3003, name = "生命项链", desc = "增加生命值", type = ItemType.Accessory, price = 100, hp = 30, reqLevel = 2 },

                // 消耗品
                [4001] = new ItemConfig { id = 4001, name = "生命药水", desc = "恢复50点生命", type = ItemType.Consumable, price = 15, restoreHp = 50 },
                [4002] = new ItemConfig { id = 4002, name = "大生命药水", desc = "恢复120点生命", type = ItemType.Consumable, price = 40, restoreHp = 120 },
                [4003] = new ItemConfig { id = 4003, name = "法力药水", desc = "恢复30点法力", type = ItemType.Consumable, price = 15, restoreMp = 30 },
                [4004] = new ItemConfig { id = 4004, name = "大法力药水", desc = "恢复80点法力", type = ItemType.Consumable, price = 40, restoreMp = 80 },
                [4005] = new ItemConfig { id = 4005, name = "全恢复药水", desc = "恢复全部生命和法力", type = ItemType.Consumable, price = 100, restoreHp = 9999, restoreMp = 9999 }
            };
        }

        private void InitMonsters()
        {
            monsters = new Dictionary<int, MonsterConfig>
            {
                [1001] = new MonsterConfig { id = 1001, name = "史莱姆", hp = 30, attack = 5, defense = 2, speed = 3, expReward = 10, goldReward = 5, minFloor = 1, drops = { new ItemDropConfig { itemId = 4001, chance = 0.3f } } },
                [1002] = new MonsterConfig { id = 1002, name = "骷髅兵", hp = 50, attack = 8, defense = 4, speed = 5, expReward = 20, goldReward = 10, minFloor = 1, drops = { new ItemDropConfig { itemId = 4001, chance = 0.2f }, new ItemDropConfig { itemId = 1001, chance = 0.1f } } },
                [1003] = new MonsterConfig { id = 1003, name = "蝙蝠", hp = 25, attack = 6, defense = 1, speed = 10, expReward = 12, goldReward = 6, minFloor = 1, drops = { new ItemDropConfig { itemId = 3002, chance = 0.05f } } },
                [1004] = new MonsterConfig { id = 1004, name = "哥布林", hp = 60, attack = 10, defense = 5, speed = 6, expReward = 25, goldReward = 15, minFloor = 2, drops = { new ItemDropConfig { itemId = 4001, chance = 0.3f }, new ItemDropConfig { itemId = 1002, chance = 0.08f } } },
                [1005] = new MonsterConfig { id = 1005, name = "暗影狼", hp = 80, attack = 14, defense = 6, speed = 12, expReward = 35, goldReward = 20, minFloor = 3, drops = { new ItemDropConfig { itemId = 4003, chance = 0.2f } } },
                [1006] = new MonsterConfig { id = 1006, name = "石像鬼", hp = 120, attack = 12, defense = 15, speed = 3, expReward = 40, goldReward = 25, minFloor = 4, drops = { new ItemDropConfig { itemId = 2002, chance = 0.05f } } },
                [1007] = new MonsterConfig { id = 1007, name = "暗黑法师", hp = 70, attack = 18, defense = 4, speed = 8, expReward = 45, goldReward = 30, minFloor = 5, skillIds = { 5001 }, drops = { new ItemDropConfig { itemId = 1007, chance = 0.05f } } },

                // Boss
                [2001] = new MonsterConfig { id = 2001, name = "地牢守卫", hp = 200, attack = 15, defense = 10, speed = 6, expReward = 100, goldReward = 80, minFloor = 5, isBoss = true, drops = { new ItemDropConfig { itemId = 1003, chance = 0.3f }, new ItemDropConfig { itemId = 3001, chance = 0.2f } } },
                [2002] = new MonsterConfig { id = 2002, name = "堕落领主", hp = 500, attack = 25, defense = 18, speed = 8, expReward = 300, goldReward = 200, minFloor = 10, isBoss = true, drops = { new ItemDropConfig { itemId = 1003, chance = 0.5f }, new ItemDropConfig { itemId = 3003, chance = 0.3f } } }
            };
        }

        private void InitTreasures()
        {
            treasures = new Dictionary<int, TreasureConfig>
            {
                [1] = new TreasureConfig { id = 1, name = "勇者之证", desc = "攻击力+10%", effectType = 0, effectValue = 10 },
                [2] = new TreasureConfig { id = 2, name = "守护之盾", desc = "防御力+15%", effectType = 1, effectValue = 15 },
                [3] = new TreasureConfig { id = 3, name = "生命之泉", desc = "生命值+20%", effectType = 2, effectValue = 20 },
                [4] = new TreasureConfig { id = 4, name = "吸血之牙", desc = "攻击时恢复10%伤害值的生命", effectType = 3, effectValue = 10 },
                [5] = new TreasureConfig { id = 5, name = "幸运硬币", desc = "金币获取+30%", effectType = 3, effectValue = 30 },
                [6] = new TreasureConfig { id = 6, name = "经验宝石", desc = "经验获取+20%", effectType = 3, effectValue = 20 }
            };
        }

        private void InitBuffs()
        {
            buffs = new Dictionary<int, BuffConfig>
            {
                [1] = new BuffConfig { id = 1, name = "战吼强化", desc = "攻击力提升30%", effectType = 0, effectValue = 30 },
                [2] = new BuffConfig { id = 2, name = "闪避强化", desc = "防御力提升25%", effectType = 1, effectValue = 25 },
                [3] = new BuffConfig { id = 3, name = "铁壁强化", desc = "防御力提升50%", effectType = 1, effectValue = 50 },
                [4] = new BuffConfig { id = 4, name = "祝福强化", desc = "攻防提升20%", effectType = 2, effectValue = 20 },
                [5] = new BuffConfig { id = 5, name = "冰甲强化", desc = "防御力提升40%", effectType = 1, effectValue = 40 }
            };
        }
    }
}
