using System.Collections.Generic;
using System.Linq;
using Game.Battle;
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

        /// <summary>获取所有技能配置（不含Boss技能9000+）</summary>
        public List<SkillConfig> GetAllSkills()
            => skills.Values.Where(s => s.id < 9000).ToList();

        /// <summary>按标签页分类获取技能（不含Boss技能9000+）</summary>
        public List<SkillConfig> GetSkillsByTab(SkillTab tab, List<int> learnedSkills)
        {
            var all = GetAllSkills();
            switch (tab)
            {
                case SkillTab.All:
                    return all;
                case SkillTab.Attack:
                    return all.Where(s => !s.isHeal && s.type == SkillType.Active).ToList();
                case SkillTab.Heal:
                    return all.Where(s => s.isHeal).ToList();
                case SkillTab.Buff:
                    return all.Where(s => s.type == SkillType.Buff).ToList();
                case SkillTab.Control:
                    return all.Where(s => s.type == SkillType.StatusAttack
                        || s.type == SkillType.Debuff
                        || s.type == SkillType.DoT
                        || s.type == SkillType.Control).ToList();
                case SkillTab.Passive:
                    return all.Where(s => s.type == SkillType.Passive).ToList();
                case SkillTab.Learned:
                    return all.Where(s => learnedSkills.Contains(s.id)).ToList();
                default:
                    return all;
            }
        }

        public ItemConfig GetItem(int id)
            => items.TryGetValue(id, out var c) ? c : null;

        public List<ItemConfig> GetItemsByType(ItemType type)
            => items.Values.Where(i => i.type == type).ToList();

        public MonsterConfig GetMonster(int id)
            => monsters.TryGetValue(id, out var c) ? c : null;

        public List<MonsterConfig> GetMonstersByFloor(int floor)
            => monsters.Values.Where(m => m.minFloor <= floor && !m.isBoss).ToList();

        public MonsterConfig GetBossByFloor(int floor)
        {
            // 精确匹配：找到 minFloor == floor 的Boss
            var exact = monsters.Values.FirstOrDefault(m => m.isBoss && m.minFloor == floor);
            if (exact != null) return exact;
            // 兜底：找到 minFloor <= floor 的最近Boss
            MonsterConfig best = null;
            foreach (var m in monsters.Values)
            {
                if (!m.isBoss || m.minFloor > floor) continue;
                if (best == null || m.minFloor > best.minFloor)
                    best = m;
            }
            return best;
        }

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
                    critBase = 5, dodgeBase = 3, speedBase = 10,
                    defaultSkillIds = new List<int> { 1001 }
                },
                [Profession.Ranger] = new ProfessionConfig
                {
                    profession = Profession.Ranger, name = "游侠", desc = "远程物理输出，高暴击高闪避",
                    strBase = 8, strGrow = 2, agiBase = 12, agiGrow = 3,
                    intBase = 6, intGrow = 1, vitBase = 6, vitGrow = 1,
                    hpBase = 35, mpBase = 30, atkBase = 6, defBase = 2,
                    critBase = 10, dodgeBase = 10, speedBase = 12,
                    defaultSkillIds = new List<int> { 2001 }
                },
                [Profession.Knight] = new ProfessionConfig
                {
                    profession = Profession.Knight, name = "骑士", desc = "近战坦克，高防御高生命",
                    strBase = 8, strGrow = 2, agiBase = 4, agiGrow = 1,
                    intBase = 4, intGrow = 1, vitBase = 14, vitGrow = 3,
                    hpBase = 70, mpBase = 15, atkBase = 5, defBase = 10,
                    critBase = 3, dodgeBase = 2, speedBase = 10,
                    defaultSkillIds = new List<int> { 3001 }
                },
                [Profession.Priest] = new ProfessionConfig
                {
                    profession = Profession.Priest, name = "牧师", desc = "治疗辅助，高法力高恢复",
                    strBase = 4, strGrow = 1, agiBase = 6, agiGrow = 1,
                    intBase = 12, intGrow = 3, vitBase = 8, vitGrow = 2,
                    hpBase = 40, mpBase = 60, atkBase = 3, defBase = 3,
                    critBase = 4, dodgeBase = 5, speedBase = 10,
                    defaultSkillIds = new List<int> { 4001 }
                },
                [Profession.Mage] = new ProfessionConfig
                {
                    profession = Profession.Mage, name = "法师", desc = "远程魔法输出，高魔法伤害",
                    strBase = 4, strGrow = 1, agiBase = 6, agiGrow = 1,
                    intBase = 14, intGrow = 3, vitBase = 6, vitGrow = 1,
                    hpBase = 30, mpBase = 70, atkBase = 4, defBase = 2,
                    critBase = 6, dodgeBase = 4, speedBase = 10,
                    defaultSkillIds = new List<int> { 5001 }
                }
            };
        }

        private void InitSkills()
        {
            skills = new Dictionary<int, SkillConfig>
            {
                // ==================== 战士技能 ====================
                // 低级(level=1): learnCost=100-200
                [1001] = new SkillConfig { id = 1001, name = "猛击", desc = "强力的物理攻击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 5, power = 150, level = 1, learnCost = 100 },
                [1002] = new SkillConfig { id = 1002, name = "战吼", desc = "提升自身攻击力", type = SkillType.Buff, profession = Profession.Warrior, mpCost = 8, power = 0, level = 1, buffId = 1, buffTurns = 3, learnCost = 150 },
                // 中级(level=2-3): learnCost=300-500
                [1003] = new SkillConfig { id = 1003, name = "旋风斩", desc = "强力横扫攻击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 12, power = 200, level = 3, prereqSkillId = 1001, isAoE = true, learnCost = 400 },
                [1005] = new SkillConfig { id = 1005, name = "割裂", desc = "撕裂伤口，附加流血3回合", type = SkillType.StatusAttack, profession = Profession.Warrior, mpCost = 20, power = 80, level = 2, statusEffect = EffectType.Bleed, statusDuration = 3, statusPowerRate = 0.25f, learnCost = 300 },
                [1006] = new SkillConfig { id = 1006, name = "破甲", desc = "破坏敌人护甲，附加易伤2回合", type = SkillType.StatusAttack, profession = Profession.Warrior, mpCost = 20, power = 90, level = 3, prereqSkillId = 1005, statusEffect = EffectType.Vulnerable, statusDuration = 2, statusPowerRate = 0.3f, learnCost = 450 },
                // 高级(level>=4): learnCost=600-1000
                [1004] = new SkillConfig { id = 1004, name = "狂战之怒", desc = "血量越低攻击越高", type = SkillType.Passive, profession = Profession.Warrior, mpCost = 0, power = 0, level = 5, prereqSkillId = 1002, learnCost = 800 },

                // ==================== 游侠技能 ====================
                [2001] = new SkillConfig { id = 2001, name = "精准射击", desc = "高暴击的远程攻击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 5, power = 140, level = 1, learnCost = 100 },
                [2002] = new SkillConfig { id = 2002, name = "闪避姿态", desc = "提升闪避率", type = SkillType.Buff, profession = Profession.Ranger, mpCost = 6, power = 0, level = 1, buffId = 2, buffTurns = 3, learnCost = 150 },
                [2005] = new SkillConfig { id = 2005, name = "加速术", desc = "提升自身速度10点，持续3回合", type = SkillType.Buff, profession = Profession.Ranger, mpCost = 6, power = 0, level = 2, buffId = 6, buffTurns = 3, learnCost = 300 },
                [2003] = new SkillConfig { id = 2003, name = "连射", desc = "连续两次攻击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 10, power = 120, level = 3, prereqSkillId = 2001, learnCost = 400 },
                [2006] = new SkillConfig { id = 2006, name = "毒刃", desc = "淬毒一击，附加中毒3回合", type = SkillType.StatusAttack, profession = Profession.Ranger, mpCost = 25, power = 70, level = 2, statusEffect = EffectType.Poison, statusDuration = 3, statusPowerRate = 0.15f, learnCost = 350 },
                [2004] = new SkillConfig { id = 2004, name = "致命一击", desc = "必定暴击的一击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 15, power = 250, level = 5, prereqSkillId = 2003, learnCost = 900 },

                // ==================== 骑士技能 ====================
                [3001] = new SkillConfig { id = 3001, name = "盾击", desc = "用盾牌攻击并降低敌人攻击", type = SkillType.Active, profession = Profession.Knight, mpCost = 5, power = 100, level = 1, debuffAttack = 30, debuffTurns = 2, learnCost = 120 },
                [3002] = new SkillConfig { id = 3002, name = "铁壁", desc = "大幅提升防御", type = SkillType.Buff, profession = Profession.Knight, mpCost = 8, power = 0, level = 1, buffId = 3, buffTurns = 3, learnCost = 150 },
                [3005] = new SkillConfig { id = 3005, name = "重击", desc = "沉重一击，附加眩晕2回合", type = SkillType.StatusAttack, profession = Profession.Knight, mpCost = 30, power = 60, level = 2, statusEffect = EffectType.Stun, statusDuration = 3, learnCost = 350 },
                [3003] = new SkillConfig { id = 3003, name = "嘲讽", desc = "吸引敌人攻击并减伤", type = SkillType.Active, profession = Profession.Knight, mpCost = 6, power = 80, level = 3, prereqSkillId = 3001, debuffAttack = 20, debuffTurns = 3, learnCost = 450 },
                [3004] = new SkillConfig { id = 3004, name = "坚守", desc = "生命低于30%时自动减伤", type = SkillType.Passive, profession = Profession.Knight, mpCost = 0, power = 0, level = 5, prereqSkillId = 3002, learnCost = 700 },

                // ==================== 牧师技能 ====================
                [4001] = new SkillConfig { id = 4001, name = "治疗术", desc = "恢复自身生命值", type = SkillType.Active, profession = Profession.Priest, mpCost = 8, power = 100, level = 1, isHeal = true, learnCost = 120 },
                [4002] = new SkillConfig { id = 4002, name = "祝福", desc = "提升全属性", type = SkillType.Buff, profession = Profession.Priest, mpCost = 10, power = 0, level = 1, buffId = 4, buffTurns = 3, learnCost = 180 },
                [4003] = new SkillConfig { id = 4003, name = "圣光", desc = "神圣伤害攻击", type = SkillType.Active, profession = Profession.Priest, mpCost = 12, power = 160, level = 3, prereqSkillId = 4001, learnCost = 400 },
                [4004] = new SkillConfig { id = 4004, name = "复活", desc = "战斗中自动复活一次", type = SkillType.Passive, profession = Profession.Priest, mpCost = 0, power = 0, level = 5, prereqSkillId = 4002, learnCost = 1000 },

                // ==================== 法师技能 ====================
                [5001] = new SkillConfig { id = 5001, name = "火球术", desc = "基础火系魔法", type = SkillType.Active, profession = Profession.Mage, mpCost = 6, power = 160, level = 1, learnCost = 100 },
                [5002] = new SkillConfig { id = 5002, name = "冰甲", desc = "提升防御和减伤", type = SkillType.Buff, profession = Profession.Mage, mpCost = 8, power = 0, level = 1, buffId = 5, buffTurns = 3, learnCost = 160 },
                [5005] = new SkillConfig { id = 5005, name = "减速术", desc = "降低敌人速度10点，持续3回合", type = SkillType.Active, profession = Profession.Mage, mpCost = 6, power = 80, level = 2, debuffSpeed = 10, debuffTurns = 3, learnCost = 300 },
                [5006] = new SkillConfig { id = 5006, name = "封印", desc = "封印敌人技能，附加沉默2回合", type = SkillType.StatusAttack, profession = Profession.Mage, mpCost = 25, power = 50, level = 2, statusEffect = EffectType.Silence, statusDuration = 3, learnCost = 350 },
                [5003] = new SkillConfig { id = 5003, name = "暴风雪", desc = "强力冰系范围攻击", type = SkillType.Active, profession = Profession.Mage, mpCost = 15, power = 220, level = 3, prereqSkillId = 5001, isAoE = true, learnCost = 500 },
                [5004] = new SkillConfig { id = 5004, name = "魔力涌动", desc = "每回合恢复法力", type = SkillType.Passive, profession = Profession.Mage, mpCost = 0, power = 0, level = 5, prereqSkillId = 5002, learnCost = 750 },

                // ==================== 通用技能（全职业可学） ====================
                // 攻击类
                [6001] = new SkillConfig { id = 6001, name = "烈焰斩", desc = "附带火焰的强力斩击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 15, power = 130, level = 5, learnCost = 300 },
                [6002] = new SkillConfig { id = 6002, name = "暗影步", desc = "暗影中突袭敌人", type = SkillType.Active, profession = Profession.Ranger, mpCost = 10, power = 110, level = 3, learnCost = 200 },
                [6003] = new SkillConfig { id = 6003, name = "龙息术", desc = "喷射灼热龙息，攻击全体敌人", type = SkillType.Active, profession = Profession.Mage, mpCost = 25, power = 150, level = 10, isAoE = true, learnCost = 600 },
                [6004] = new SkillConfig { id = 6004, name = "连击", desc = "快速连续两次攻击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 12, power = 85, level = 4, isAoE = true, learnCost = 250 },
                // 治疗类
                [6005] = new SkillConfig { id = 6005, name = "生命涌泉", desc = "涌出生命之泉恢复体力", type = SkillType.Active, profession = Profession.Priest, mpCost = 20, power = 80, level = 6, isHeal = true, learnCost = 350 },
                [6006] = new SkillConfig { id = 6006, name = "神圣祝福", desc = "强大的神圣治愈之力", type = SkillType.Active, profession = Profession.Priest, mpCost = 35, power = 150, level = 12, isHeal = true, learnCost = 800 },
                // Buff类
                [6007] = new SkillConfig { id = 6007, name = "战斗怒吼", desc = "提升攻击力20%，持续3回合", type = SkillType.Buff, profession = Profession.Warrior, mpCost = 15, power = 0, level = 5, buffId = 8, buffTurns = 3, learnCost = 300 },
                [6008] = new SkillConfig { id = 6008, name = "铁壁防御", desc = "提升防御力25%，持续3回合", type = SkillType.Buff, profession = Profession.Knight, mpCost = 15, power = 0, level = 5, buffId = 9, buffTurns = 3, learnCost = 300 },
                [6009] = new SkillConfig { id = 6009, name = "急速术", desc = "提升速度10点，持续3回合", type = SkillType.Buff, profession = Profession.Mage, mpCost = 20, power = 0, level = 7, buffId = 10, buffTurns = 3, learnCost = 400 },
                // Debuff类（减益技能，不造成伤害）
                [6010] = new SkillConfig { id = 6010, name = "弱化术", desc = "降低敌人攻击力25%，持续3回合", type = SkillType.Debuff, profession = Profession.Mage, mpCost = 15, power = 0, level = 5, debuffAttack = 25, debuffTurns = 3, learnCost = 300 },
                [6011] = new SkillConfig { id = 6011, name = "碎甲击", desc = "击碎敌人护甲，降低防御30%", type = SkillType.Active, profession = Profession.Warrior, mpCost = 12, power = 90, level = 4, debuffDefense = 30, debuffTurns = 3, learnCost = 250 },
                [6012] = new SkillConfig { id = 6012, name = "迟缓术", desc = "降低敌人速度8点，持续3回合", type = SkillType.Debuff, profession = Profession.Mage, mpCost = 15, power = 0, level = 5, debuffSpeed = 8, debuffTurns = 3, learnCost = 300 },
                // DoT类（持续伤害技能）
                [6013] = new SkillConfig { id = 6013, name = "燃烧", desc = "点燃敌人，每回合造成火焰伤害，持续3回合", type = SkillType.DoT, profession = Profession.Mage, mpCost = 18, power = 80, level = 6, statusEffect = EffectType.Burn, statusDuration = 3, statusPowerRate = 0.3f, learnCost = 350 },
                [6014] = new SkillConfig { id = 6014, name = "剧毒云", desc = "释放剧毒云雾，每回合造成中毒伤害，持续4回合", type = SkillType.DoT, profession = Profession.Ranger, mpCost = 22, power = 70, level = 7, statusEffect = EffectType.Poison, statusDuration = 4, statusPowerRate = 0.2f, learnCost = 400 },
                // 控制类
                [6015] = new SkillConfig { id = 6015, name = "冰冻术", desc = "冰冻敌人2回合，并造成伤害", type = SkillType.Control, profession = Profession.Mage, mpCost = 25, power = 60, level = 8, statusEffect = EffectType.Stun, statusDuration = 2, learnCost = 500 },
                [6016] = new SkillConfig { id = 6016, name = "恐惧术", desc = "恐惧敌人，使其沉默2回合", type = SkillType.Control, profession = Profession.Mage, mpCost = 20, power = 50, level = 7, statusEffect = EffectType.Silence, statusDuration = 2, learnCost = 400 },
                // 被动类
                [6017] = new SkillConfig { id = 6017, name = "法力汲取", desc = "技能造成伤害时回复伤害值10%的法力", type = SkillType.Passive, profession = Profession.Mage, mpCost = 0, power = 0, level = 10, learnCost = 500 },
                [6018] = new SkillConfig { id = 6018, name = "击杀回蓝", desc = "击杀怪物时回复20点法力", type = SkillType.Passive, profession = Profession.Mage, mpCost = 0, power = 0, level = 8, learnCost = 400 },

                // ==================== Boss专属技能（不可学习） ====================
                [9001] = new SkillConfig { id = 9001, name = "暗影斩", desc = "暗影领主的强力攻击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 5, power = 180 },
                [9002] = new SkillConfig { id = 9002, name = "暗影撕裂", desc = "撕裂目标，附加流血4回合", type = SkillType.StatusAttack, profession = Profession.Warrior, mpCost = 8, power = 100, statusEffect = EffectType.Bleed, statusDuration = 4, statusPowerRate = 0.3f },
                [9003] = new SkillConfig { id = 9003, name = "堕落冲击", desc = "堕落骑士的沉重打击，附加眩晕", type = SkillType.StatusAttack, profession = Profession.Knight, mpCost = 8, power = 140, statusEffect = EffectType.Stun, statusDuration = 2 },
                [9004] = new SkillConfig { id = 9004, name = "铁壁", desc = "大幅提升自身防御", type = SkillType.Buff, profession = Profession.Knight, mpCost = 10, power = 0, buffId = 3, buffTurns = 3 },
                [9005] = new SkillConfig { id = 9005, name = "暗影诅咒", desc = "施加暗影诅咒，附加易伤3回合", type = SkillType.StatusAttack, profession = Profession.Mage, mpCost = 8, power = 80, statusEffect = EffectType.Vulnerable, statusDuration = 3, statusPowerRate = 0.35f },
                [9006] = new SkillConfig { id = 9006, name = "深渊禁锢", desc = "深渊之力封印目标，附加沉默3回合", type = SkillType.StatusAttack, profession = Profession.Mage, mpCost = 10, power = 90, statusEffect = EffectType.Silence, statusDuration = 3 },
                [9007] = new SkillConfig { id = 9007, name = "混沌猛击", desc = "混沌巨兽的毁灭性攻击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 8, power = 220 },
                [9008] = new SkillConfig { id = 9008, name = "混沌毒雾", desc = "释放毒雾，附加中毒4回合", type = SkillType.StatusAttack, profession = Profession.Ranger, mpCost = 10, power = 100, statusEffect = EffectType.Poison, statusDuration = 4, statusPowerRate = 0.2f },
                [9009] = new SkillConfig { id = 9009, name = "龙息", desc = "暗黑龙王的灼热龙息", type = SkillType.Active, profession = Profession.Mage, mpCost = 10, power = 250 },
                [9010] = new SkillConfig { id = 9010, name = "暗黑领域", desc = "暗黑领域笼罩，附加易伤+沉默", type = SkillType.StatusAttack, profession = Profession.Mage, mpCost = 15, power = 120, statusEffect = EffectType.Vulnerable, statusDuration = 3, statusPowerRate = 0.4f },
                [9011] = new SkillConfig { id = 9011, name = "龙鳞护甲", desc = "龙鳞硬化，大幅提升防御", type = SkillType.Buff, profession = Profession.Knight, mpCost = 12, power = 0, buffId = 3, buffTurns = 4 }
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
                [4005] = new ItemConfig { id = 4005, name = "全恢复药水", desc = "恢复全部生命和法力", type = ItemType.Consumable, price = 100, restoreHp = 9999, restoreMp = 9999 },

                // Boss专属稀有装备（必掉）
                [1008] = new ItemConfig { id = 1008, name = "暗影之刃", desc = "暗影领主的佩刀，攻击+22", type = ItemType.Weapon, price = 500, atk = 22, reqLevel = 3 },
                [1009] = new ItemConfig { id = 1009, name = "堕落圣剑", desc = "堕落骑士的圣剑，攻击+30", type = ItemType.Weapon, price = 800, atk = 30, reqLevel = 5 },
                [2005] = new ItemConfig { id = 2005, name = "深渊法袍", desc = "深渊法师的法袍，防御+20 生命+80", type = ItemType.Armor, price = 900, def = 20, hp = 80, reqLevel = 7 },
                [2006] = new ItemConfig { id = 2006, name = "混沌铠甲", desc = "混沌巨兽的甲壳制成，防御+28 生命+120", type = ItemType.Armor, price = 1200, def = 28, hp = 120, reqLevel = 9 },
                [3004] = new ItemConfig { id = 3004, name = "龙牙项链", desc = "暗黑龙王的牙齿制成的项链，暴击+15 闪避+10", type = ItemType.Accessory, price = 1500, crit = 15, dodge = 10, reqLevel = 10 }
            };
        }

        private void InitMonsters()
        {
            monsters = new Dictionary<int, MonsterConfig>
            {
                [1001] = new MonsterConfig { id = 1001, name = "史莱姆", hp = 30, attack = 5, defense = 2, speed = 3, expReward = 10, goldReward = 5, minFloor = 1, aiPattern = MonsterAIPattern.Balanced, drops = { new ItemDropConfig { itemId = 4001, chance = 0.3f } } },
                [1002] = new MonsterConfig { id = 1002, name = "骷髅兵", hp = 50, attack = 8, defense = 4, speed = 5, expReward = 20, goldReward = 10, minFloor = 1, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 4001, chance = 0.2f }, new ItemDropConfig { itemId = 1001, chance = 0.1f } } },
                [1003] = new MonsterConfig { id = 1003, name = "蝙蝠", hp = 25, attack = 6, defense = 1, speed = 10, expReward = 12, goldReward = 6, minFloor = 1, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 3002, chance = 0.05f } } },
                [1004] = new MonsterConfig { id = 1004, name = "哥布林", hp = 60, attack = 10, defense = 5, speed = 6, expReward = 25, goldReward = 15, minFloor = 2, aiPattern = MonsterAIPattern.Balanced, drops = { new ItemDropConfig { itemId = 4001, chance = 0.3f }, new ItemDropConfig { itemId = 1002, chance = 0.08f } } },
                [1005] = new MonsterConfig { id = 1005, name = "暗影狼", hp = 80, attack = 14, defense = 6, speed = 12, expReward = 35, goldReward = 20, minFloor = 3, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 4003, chance = 0.2f } } },
                [1006] = new MonsterConfig { id = 1006, name = "石像鬼", hp = 120, attack = 12, defense = 15, speed = 3, expReward = 40, goldReward = 25, minFloor = 4, aiPattern = MonsterAIPattern.Defensive, drops = { new ItemDropConfig { itemId = 2002, chance = 0.05f } } },
                [1007] = new MonsterConfig { id = 1007, name = "暗黑法师", hp = 70, attack = 18, defense = 4, speed = 8, expReward = 45, goldReward = 30, minFloor = 5, skillIds = { 5001 }, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 1007, chance = 0.05f } } },

                // 带状态效果的怪物
                [1008] = new MonsterConfig { id = 1008, name = "毒蛇", hp = 55, attack = 11, defense = 3, speed = 14, expReward = 30, goldReward = 18, minFloor = 2, skillIds = { 2006 }, aiPattern = MonsterAIPattern.Debuffer, drops = { new ItemDropConfig { itemId = 4001, chance = 0.25f } } },
                [1009] = new MonsterConfig { id = 1009, name = "暗影法师", hp = 65, attack = 16, defense = 3, speed = 9, expReward = 50, goldReward = 35, minFloor = 5, skillIds = { 5006 }, aiPattern = MonsterAIPattern.Debuffer, drops = { new ItemDropConfig { itemId = 4003, chance = 0.2f } } },
                [1010] = new MonsterConfig { id = 1010, name = "重甲兵", hp = 100, attack = 13, defense = 12, speed = 4, expReward = 38, goldReward = 28, minFloor = 3, skillIds = { 3005 }, aiPattern = MonsterAIPattern.Defensive, drops = { new ItemDropConfig { itemId = 4001, chance = 0.3f } } },

                // ==================== Boss（每5层一个，属性约为同层普通怪2.5倍）====================
                // L5 暗影领主：高攻+流血技
                [2001] = new MonsterConfig { id = 2001, name = "暗影领主", hp = 300, attack = 22, defense = 10, speed = 8, mp = 50, expReward = 250, goldReward = 500, minFloor = 5, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9001, 9002 }, drops = { new ItemDropConfig { itemId = 1008, chance = 1f } } },
                // L10 堕落骑士：高防+眩晕+防御AI
                [2002] = new MonsterConfig { id = 2002, name = "堕落骑士", hp = 550, attack = 28, defense = 28, speed = 6, mp = 60, expReward = 500, goldReward = 1000, minFloor = 10, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9003, 9004 }, drops = { new ItemDropConfig { itemId = 1009, chance = 1f } } },
                // L15 深渊法师：debuff为主+沉默+易伤
                [2003] = new MonsterConfig { id = 2003, name = "深渊法师", hp = 700, attack = 35, defense = 15, speed = 10, mp = 100, expReward = 750, goldReward = 1500, minFloor = 15, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9005, 9006 }, drops = { new ItemDropConfig { itemId = 2005, chance = 1f } } },
                // L20 混沌巨兽：高血量+高伤害+多阶段
                [2004] = new MonsterConfig { id = 2004, name = "混沌巨兽", hp = 1000, attack = 42, defense = 25, speed = 7, mp = 80, expReward = 1000, goldReward = 2000, minFloor = 20, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9007, 9008 }, drops = { new ItemDropConfig { itemId = 2006, chance = 1f } } },
                // L25 最终Boss暗黑龙王：三阶段+全技能类型
                [2005] = new MonsterConfig { id = 2005, name = "暗黑龙王", hp = 1500, attack = 50, defense = 30, speed = 12, mp = 120, expReward = 1500, goldReward = 2500, minFloor = 25, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9009, 9010, 9011 }, drops = { new ItemDropConfig { itemId = 3004, chance = 1f } } }
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
                [5] = new BuffConfig { id = 5, name = "冰甲强化", desc = "防御力提升40%", effectType = 1, effectValue = 40 },
                [6] = new BuffConfig { id = 6, name = "加速", desc = "速度+10", effectType = 3, effectValue = 10 },
                [7] = new BuffConfig { id = 7, name = "减速", desc = "敌人速度-10", effectType = 4, effectValue = 10 },
                [8] = new BuffConfig { id = 8, name = "战斗怒吼", desc = "攻击力提升20%", effectType = 0, effectValue = 20 },
                [9] = new BuffConfig { id = 9, name = "铁壁防御", desc = "防御力提升25%", effectType = 1, effectValue = 25 },
                [10] = new BuffConfig { id = 10, name = "急速", desc = "速度+10", effectType = 3, effectValue = 10 }
            };
        }
    }
}
