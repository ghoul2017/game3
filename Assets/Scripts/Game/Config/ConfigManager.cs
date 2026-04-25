using System.Collections.Generic;
using System.Linq;
using Game.Battle;
using Game.Core;
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
            Logger.LogI("ConfigManager", "配置加载完成（JSON外部配置）");
        }

        /// <summary>重新加载所有配置（热更新支持）</summary>
        public void ReloadConfig()
        {
            Init();
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
            => monsters.Values.Where(m => m.minFloor <= floor && (m.maxFloor == 0 || m.maxFloor >= floor) && !m.isBoss).ToList();

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

        /// <summary>从Resources加载JSON配置，失败返回null</summary>
        private T LoadJson<T>(string path) where T : class
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset != null)
                return JsonUtility.FromJson<T>(asset.text);
            return null;
        }

        private void InitProfessions()
        {
            professions = new Dictionary<Profession, ProfessionConfig>();
            var data = LoadJson<ProfessionConfigList>("Config/professions");
            if (data?.list != null)
            {
                foreach (var c in data.list)
                    professions[c.profession] = c;
                return;
            }
            // fallback 硬编码
            Logger.LogW("ConfigManager", "professions.json加载失败，使用硬编码fallback");
            professions = new Dictionary<Profession, ProfessionConfig>
            {
                [Profession.Warrior] = new ProfessionConfig { profession = Profession.Warrior, name = "战士", desc = "近战物理输出，高攻击高生命", strBase = 12, strGrow = 3, agiBase = 6, agiGrow = 1, intBase = 4, intGrow = 1, vitBase = 10, vitGrow = 2, hpBase = 50, mpBase = 20, atkBase = 8, defBase = 4, critBase = 5, dodgeBase = 3, speedBase = 10, defaultSkillIds = new List<int> { 1001 } },
                [Profession.Ranger] = new ProfessionConfig { profession = Profession.Ranger, name = "游侠", desc = "远程物理输出，高暴击高闪避", strBase = 8, strGrow = 2, agiBase = 12, agiGrow = 3, intBase = 6, intGrow = 1, vitBase = 6, vitGrow = 1, hpBase = 35, mpBase = 30, atkBase = 6, defBase = 2, critBase = 10, dodgeBase = 10, speedBase = 12, defaultSkillIds = new List<int> { 2001 } },
                [Profession.Knight] = new ProfessionConfig { profession = Profession.Knight, name = "骑士", desc = "近战坦克，高防御高生命", strBase = 8, strGrow = 2, agiBase = 4, agiGrow = 1, intBase = 4, intGrow = 1, vitBase = 14, vitGrow = 3, hpBase = 70, mpBase = 15, atkBase = 5, defBase = 10, critBase = 3, dodgeBase = 2, speedBase = 10, defaultSkillIds = new List<int> { 3001 } },
                [Profession.Priest] = new ProfessionConfig { profession = Profession.Priest, name = "牧师", desc = "治疗辅助，高法力高恢复", strBase = 4, strGrow = 1, agiBase = 6, agiGrow = 1, intBase = 12, intGrow = 3, vitBase = 8, vitGrow = 2, hpBase = 40, mpBase = 60, atkBase = 3, defBase = 3, critBase = 4, dodgeBase = 5, speedBase = 10, defaultSkillIds = new List<int> { 4001 } },
                [Profession.Mage] = new ProfessionConfig { profession = Profession.Mage, name = "法师", desc = "远程魔法输出，高魔法伤害", strBase = 4, strGrow = 1, agiBase = 6, agiGrow = 1, intBase = 14, intGrow = 3, vitBase = 6, vitGrow = 1, hpBase = 30, mpBase = 70, atkBase = 4, defBase = 2, critBase = 6, dodgeBase = 4, speedBase = 10, defaultSkillIds = new List<int> { 5001 } }
            };
        }

        private void InitSkills()
        {
            skills = new Dictionary<int, SkillConfig>();
            var data = LoadJson<SkillConfigList>("Config/skills");
            if (data?.list != null)
            {
                foreach (var c in data.list) skills[c.id] = c;
                return;
            }
            Logger.LogW("ConfigManager", "skills.json加载失败，使用硬编码fallback");
            skills = new Dictionary<int, SkillConfig>
            {
                [1001] = new SkillConfig { id = 1001, name = "猛击", desc = "强力的物理攻击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 5, power = 150, level = 1, learnCost = 100 },
                [1002] = new SkillConfig { id = 1002, name = "战吼", desc = "提升自身攻击力", type = SkillType.Buff, profession = Profession.Warrior, mpCost = 8, power = 0, level = 1, buffId = 1, buffTurns = 3, learnCost = 150 },
                [1003] = new SkillConfig { id = 1003, name = "旋风斩", desc = "强力横扫攻击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 20, power = 200, level = 3, prereqSkillId = 1001, isAoE = true, learnCost = 400 },
                [1005] = new SkillConfig { id = 1005, name = "割裂", desc = "撕裂伤口，附加流血3回合", type = SkillType.StatusAttack, profession = Profession.Warrior, mpCost = 20, power = 80, level = 2, statusEffect = EffectType.Bleed, statusDuration = 3, statusPowerRate = 0.25f, learnCost = 300 },
                [1006] = new SkillConfig { id = 1006, name = "破甲", desc = "破坏敌人护甲，附加易伤2回合", type = SkillType.StatusAttack, profession = Profession.Warrior, mpCost = 20, power = 90, level = 3, prereqSkillId = 1005, statusEffect = EffectType.Vulnerable, statusDuration = 2, statusPowerRate = 0.3f, learnCost = 450 },
                [1004] = new SkillConfig { id = 1004, name = "狂战之怒", desc = "血量越低攻击越高", type = SkillType.Passive, profession = Profession.Warrior, mpCost = 0, power = 0, level = 5, prereqSkillId = 1002, learnCost = 800 },
                [2001] = new SkillConfig { id = 2001, name = "精准射击", desc = "高暴击的远程攻击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 5, power = 140, level = 1, learnCost = 100 },
                [2002] = new SkillConfig { id = 2002, name = "闪避姿态", desc = "提升闪避率", type = SkillType.Buff, profession = Profession.Ranger, mpCost = 6, power = 0, level = 1, buffId = 2, buffTurns = 3, learnCost = 150 },
                [2005] = new SkillConfig { id = 2005, name = "加速术", desc = "提升自身速度10点，持续3回合", type = SkillType.Buff, profession = Profession.Ranger, mpCost = 6, power = 0, level = 2, buffId = 6, buffTurns = 3, learnCost = 300 },
                [2003] = new SkillConfig { id = 2003, name = "连射", desc = "连续两次攻击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 22, power = 120, level = 3, prereqSkillId = 2001, learnCost = 400 },
                [2006] = new SkillConfig { id = 2006, name = "毒刃", desc = "淬毒一击，附加中毒3回合", type = SkillType.StatusAttack, profession = Profession.Ranger, mpCost = 25, power = 70, level = 2, statusEffect = EffectType.Poison, statusDuration = 3, statusPowerRate = 0.15f, learnCost = 350 },
                [2004] = new SkillConfig { id = 2004, name = "致命一击", desc = "必定暴击的一击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 35, power = 250, level = 5, prereqSkillId = 2003, learnCost = 900 },
                [3001] = new SkillConfig { id = 3001, name = "盾击", desc = "用盾牌攻击并降低敌人攻击", type = SkillType.Active, profession = Profession.Knight, mpCost = 5, power = 100, level = 1, debuffAttack = 30, debuffTurns = 2, learnCost = 120 },
                [3002] = new SkillConfig { id = 3002, name = "铁壁", desc = "大幅提升防御", type = SkillType.Buff, profession = Profession.Knight, mpCost = 8, power = 0, level = 1, buffId = 3, buffTurns = 3, learnCost = 150 },
                [3005] = new SkillConfig { id = 3005, name = "重击", desc = "沉重一击，附加眩晕2回合", type = SkillType.StatusAttack, profession = Profession.Knight, mpCost = 30, power = 60, level = 2, statusEffect = EffectType.Stun, statusDuration = 3, learnCost = 350 },
                [3003] = new SkillConfig { id = 3003, name = "嘲讽", desc = "吸引敌人攻击并减伤", type = SkillType.Active, profession = Profession.Knight, mpCost = 20, power = 80, level = 3, prereqSkillId = 3001, debuffAttack = 20, debuffTurns = 3, learnCost = 450 },
                [3004] = new SkillConfig { id = 3004, name = "坚守", desc = "生命低于30%时自动减伤", type = SkillType.Passive, profession = Profession.Knight, mpCost = 0, power = 0, level = 5, prereqSkillId = 3002, learnCost = 700 },
                [4001] = new SkillConfig { id = 4001, name = "治疗术", desc = "恢复自身生命值", type = SkillType.Active, profession = Profession.Priest, mpCost = 8, power = 100, level = 1, isHeal = true, learnCost = 120 },
                [4002] = new SkillConfig { id = 4002, name = "祝福", desc = "提升全属性", type = SkillType.Buff, profession = Profession.Priest, mpCost = 10, power = 0, level = 1, buffId = 4, buffTurns = 3, learnCost = 180 },
                [4003] = new SkillConfig { id = 4003, name = "圣光", desc = "神圣伤害攻击", type = SkillType.Active, profession = Profession.Priest, mpCost = 22, power = 160, level = 3, prereqSkillId = 4001, learnCost = 400 },
                [4004] = new SkillConfig { id = 4004, name = "复活", desc = "战斗中自动复活一次", type = SkillType.Passive, profession = Profession.Priest, mpCost = 0, power = 0, level = 5, prereqSkillId = 4002, learnCost = 1000 },
                [5001] = new SkillConfig { id = 5001, name = "火球术", desc = "基础火系魔法", type = SkillType.Active, profession = Profession.Mage, mpCost = 6, power = 160, level = 1, learnCost = 100 },
                [5002] = new SkillConfig { id = 5002, name = "冰甲", desc = "提升防御和减伤", type = SkillType.Buff, profession = Profession.Mage, mpCost = 8, power = 0, level = 1, buffId = 5, buffTurns = 3, learnCost = 160 },
                [5005] = new SkillConfig { id = 5005, name = "减速术", desc = "降低敌人速度10点，持续3回合", type = SkillType.Active, profession = Profession.Mage, mpCost = 6, power = 80, level = 2, debuffSpeed = 10, debuffTurns = 3, learnCost = 300 },
                [5006] = new SkillConfig { id = 5006, name = "封印", desc = "封印敌人技能，附加沉默2回合", type = SkillType.StatusAttack, profession = Profession.Mage, mpCost = 25, power = 50, level = 2, statusEffect = EffectType.Silence, statusDuration = 3, learnCost = 350 },
                [5003] = new SkillConfig { id = 5003, name = "暴风雪", desc = "强力冰系范围攻击", type = SkillType.Active, profession = Profession.Mage, mpCost = 25, power = 220, level = 3, prereqSkillId = 5001, isAoE = true, learnCost = 500 },
                [5004] = new SkillConfig { id = 5004, name = "魔力涌动", desc = "每回合恢复法力", type = SkillType.Passive, profession = Profession.Mage, mpCost = 0, power = 0, level = 5, prereqSkillId = 5002, learnCost = 750 },
                [6001] = new SkillConfig { id = 6001, name = "烈焰斩", desc = "附带火焰的强力斩击", type = SkillType.Active, profession = Profession.Warrior, mpCost = 20, power = 130, level = 5, learnCost = 300 },
                [6002] = new SkillConfig { id = 6002, name = "暗影步", desc = "暗影中突袭敌人", type = SkillType.Active, profession = Profession.Ranger, mpCost = 10, power = 110, level = 3, learnCost = 200 },
                [6003] = new SkillConfig { id = 6003, name = "龙息术", desc = "喷射灼热龙息，攻击全体敌人", type = SkillType.Active, profession = Profession.Mage, mpCost = 35, power = 150, level = 10, isAoE = true, learnCost = 600 },
                [6004] = new SkillConfig { id = 6004, name = "连击", desc = "快速连续两次攻击", type = SkillType.Active, profession = Profession.Ranger, mpCost = 20, power = 85, level = 4, isAoE = true, learnCost = 250 },
                [6005] = new SkillConfig { id = 6005, name = "生命涌泉", desc = "涌出生命之泉恢复体力", type = SkillType.Active, profession = Profession.Priest, mpCost = 25, power = 80, level = 6, isHeal = true, learnCost = 350 },
                [6006] = new SkillConfig { id = 6006, name = "神圣祝福", desc = "强大的神圣治愈之力", type = SkillType.Active, profession = Profession.Priest, mpCost = 35, power = 150, level = 12, isHeal = true, learnCost = 800 },
                [6007] = new SkillConfig { id = 6007, name = "战斗怒吼", desc = "提升攻击力20%，持续3回合", type = SkillType.Buff, profession = Profession.Warrior, mpCost = 20, power = 0, level = 5, buffId = 8, buffTurns = 3, learnCost = 300 },
                [6008] = new SkillConfig { id = 6008, name = "铁壁防御", desc = "提升防御力25%，持续3回合", type = SkillType.Buff, profession = Profession.Knight, mpCost = 20, power = 0, level = 5, buffId = 9, buffTurns = 3, learnCost = 300 },
                [6009] = new SkillConfig { id = 6009, name = "急速术", desc = "提升速度10点，持续3回合", type = SkillType.Buff, profession = Profession.Mage, mpCost = 20, power = 0, level = 7, buffId = 10, buffTurns = 3, learnCost = 400 },
                [6010] = new SkillConfig { id = 6010, name = "弱化术", desc = "降低敌人攻击力25%，持续3回合", type = SkillType.Debuff, profession = Profession.Mage, mpCost = 15, power = 0, level = 5, debuffAttack = 25, debuffTurns = 3, learnCost = 300 },
                [6011] = new SkillConfig { id = 6011, name = "碎甲击", desc = "击碎敌人护甲，降低防御30%", type = SkillType.Active, profession = Profession.Warrior, mpCost = 12, power = 90, level = 4, debuffDefense = 30, debuffTurns = 3, learnCost = 250 },
                [6012] = new SkillConfig { id = 6012, name = "迟缓术", desc = "降低敌人速度8点，持续3回合", type = SkillType.Debuff, profession = Profession.Mage, mpCost = 15, power = 0, level = 5, debuffSpeed = 8, debuffTurns = 3, learnCost = 300 },
                [6013] = new SkillConfig { id = 6013, name = "燃烧", desc = "点燃敌人，每回合造成火焰伤害，持续3回合", type = SkillType.DoT, profession = Profession.Mage, mpCost = 25, power = 80, level = 6, statusEffect = EffectType.Burn, statusDuration = 3, statusPowerRate = 0.3f, learnCost = 350 },
                [6014] = new SkillConfig { id = 6014, name = "剧毒云", desc = "释放剧毒云雾，每回合造成中毒伤害，持续4回合", type = SkillType.DoT, profession = Profession.Ranger, mpCost = 28, power = 70, level = 7, statusEffect = EffectType.Poison, statusDuration = 4, statusPowerRate = 0.2f, learnCost = 400 },
                [6015] = new SkillConfig { id = 6015, name = "冰冻术", desc = "冰冻敌人2回合，并造成伤害", type = SkillType.Control, profession = Profession.Mage, mpCost = 35, power = 60, level = 8, statusEffect = EffectType.Stun, statusDuration = 2, learnCost = 500 },
                [6016] = new SkillConfig { id = 6016, name = "恐惧术", desc = "恐惧敌人，使其沉默2回合", type = SkillType.Control, profession = Profession.Mage, mpCost = 30, power = 50, level = 7, statusEffect = EffectType.Silence, statusDuration = 2, learnCost = 400 },
                [6017] = new SkillConfig { id = 6017, name = "法力汲取", desc = "技能造成伤害时回复伤害值10%的法力", type = SkillType.Passive, profession = Profession.Mage, mpCost = 0, power = 0, level = 10, learnCost = 500 },
                [6018] = new SkillConfig { id = 6018, name = "击杀回蓝", desc = "击杀怪物时回复20点法力", type = SkillType.Passive, profession = Profession.Mage, mpCost = 0, power = 0, level = 8, learnCost = 400 },
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
            items = new Dictionary<int, ItemConfig>();
            var data = LoadJson<ItemConfigList>("Config/items");
            if (data?.list != null)
            {
                foreach (var c in data.list) items[c.id] = c;
                return;
            }
            Logger.LogW("ConfigManager", "items.json加载失败，使用硬编码fallback");
            items = new Dictionary<int, ItemConfig>
            {
                [1001] = new ItemConfig { id = 1001, name = "短剑", desc = "普通的短剑", type = ItemType.Weapon, price = 50, sellPrice = 25, atk = 5, reqLevel = 1 },
                [1002] = new ItemConfig { id = 1002, name = "长剑", desc = "精钢打造的长剑", type = ItemType.Weapon, price = 150, sellPrice = 75, atk = 10, reqLevel = 3 },
                [1003] = new ItemConfig { id = 1003, name = "战斧", desc = "沉重但威力强大的战斧", type = ItemType.Weapon, price = 300, sellPrice = 150, atk = 15, hp = 20, reqLevel = 5 },
                [1004] = new ItemConfig { id = 1004, name = "秘银剑", desc = "秘银打造的轻盈利剑", type = ItemType.Weapon, price = 500, sellPrice = 250, atk = 20, speed = 3, reqLevel = 7 },
                [1005] = new ItemConfig { id = 1005, name = "暗影之刃", desc = "暗影力量笼罩的利刃", type = ItemType.Weapon, price = 800, sellPrice = 400, atk = 25, reqLevel = 9, equipEffectType = EquipEffectType.CritChance, equipEffectValue = 8 },
                [1006] = new ItemConfig { id = 1006, name = "龙牙剑", desc = "龙牙锻造的嗜血之剑", type = ItemType.Weapon, price = 1200, sellPrice = 600, atk = 35, reqLevel = 12, equipEffectType = EquipEffectType.Lifesteal, equipEffectValue = 10 },
                [1007] = new ItemConfig { id = 1007, name = "圣剑", desc = "神圣力量加持的圣洁之剑", type = ItemType.Weapon, price = 1800, sellPrice = 900, atk = 45, reqLevel = 15, equipEffectType = EquipEffectType.BossDamageBonus, equipEffectValue = 20 },
                [1008] = new ItemConfig { id = 1008, name = "混沌之刃", desc = "混沌之力孕育的不确定性之刃", type = ItemType.Weapon, price = 2500, sellPrice = 1250, atk = 55, reqLevel = 18, equipEffectType = EquipEffectType.DamageVariance, equipEffectValue = 30 },
                [2001] = new ItemConfig { id = 2001, name = "布甲", desc = "基础的布质护甲", type = ItemType.Armor, price = 30, sellPrice = 15, def = 3, reqLevel = 1 },
                [2002] = new ItemConfig { id = 2002, name = "皮甲", desc = "轻便坚韧的皮甲", type = ItemType.Armor, price = 100, sellPrice = 50, def = 6, reqLevel = 3 },
                [2003] = new ItemConfig { id = 2003, name = "锁子甲", desc = "环环相扣的锁甲", type = ItemType.Armor, price = 250, sellPrice = 125, def = 10, hp = 30, reqLevel = 5 },
                [2004] = new ItemConfig { id = 2004, name = "板甲", desc = "重型板甲，防御坚固", type = ItemType.Armor, price = 450, sellPrice = 225, def = 15, reqLevel = 7 },
                [2005] = new ItemConfig { id = 2005, name = "秘银甲", desc = "秘银编织的轻盈铠甲", type = ItemType.Armor, price = 800, sellPrice = 400, def = 20, reqLevel = 10, equipEffectType = EquipEffectType.Dodge, equipEffectValue = 5 },
                [2006] = new ItemConfig { id = 2006, name = "龙鳞甲", desc = "龙鳞锻造的重甲", type = ItemType.Armor, price = 1400, sellPrice = 700, def = 28, hp = 80, reqLevel = 13 },
                [2007] = new ItemConfig { id = 2007, name = "暗金甲", desc = "暗金铸造的诅咒铠甲", type = ItemType.Armor, price = 2200, sellPrice = 1100, def = 35, reqLevel = 16, equipEffectType = EquipEffectType.Thorns, equipEffectValue = 15 },
                [3001] = new ItemConfig { id = 3001, name = "力量戒指", desc = "蕴含力量的戒指", type = ItemType.Accessory, price = 120, sellPrice = 60, atk = 8, reqLevel = 1 },
                [3002] = new ItemConfig { id = 3002, name = "敏捷护符", desc = "提升速度和闪避的护符", type = ItemType.Accessory, price = 120, sellPrice = 60, speed = 5, dodge = 5, reqLevel = 1 },
                [3003] = new ItemConfig { id = 3003, name = "生命护符", desc = "蕴含生命力的护符", type = ItemType.Accessory, price = 150, sellPrice = 75, hp = 100, reqLevel = 3 },
                [3004] = new ItemConfig { id = 3004, name = "勇气勋章", desc = "勇气加持的荣誉勋章", type = ItemType.Accessory, price = 300, sellPrice = 150, atk = 12, def = 8, reqLevel = 6 },
                [3005] = new ItemConfig { id = 3005, name = "贤者之石", desc = "蕴含魔力的贤者遗物", type = ItemType.Accessory, price = 600, sellPrice = 300, mp = 50, reqLevel = 10, equipEffectType = EquipEffectType.SkillCostReduce, equipEffectValue = 15 },
                [3006] = new ItemConfig { id = 3006, name = "凤凰羽毛", desc = "凤凰尾羽，死亡时复活一次", type = ItemType.Accessory, price = 1000, sellPrice = 500, reqLevel = 14, equipEffectType = EquipEffectType.AutoRevive, equipEffectValue = 50 },
                [4001] = new ItemConfig { id = 4001, name = "生命药水", desc = "恢复50点生命", type = ItemType.Consumable, price = 15, restoreHp = 50 },
                [4002] = new ItemConfig { id = 4002, name = "大生命药水", desc = "恢复120点生命", type = ItemType.Consumable, price = 40, restoreHp = 120 },
                [4003] = new ItemConfig { id = 4003, name = "法力药水", desc = "恢复30点法力", type = ItemType.Consumable, price = 15, restoreMp = 30 },
                [4004] = new ItemConfig { id = 4004, name = "大法力药水", desc = "恢复80点法力", type = ItemType.Consumable, price = 40, restoreMp = 80 },
                [4005] = new ItemConfig { id = 4005, name = "全恢复药水", desc = "恢复全部生命和法力", type = ItemType.Consumable, price = 100, restoreHp = 9999, restoreMp = 9999 },
                [4006] = new ItemConfig { id = 4006, name = "解毒药", desc = "移除中毒状态", type = ItemType.Consumable, price = 50, consumableEffect = ConsumableEffectType.CurePoison },
                [4007] = new ItemConfig { id = 4007, name = "兴奋剂", desc = "攻击力+20%，持续3场战斗", type = ItemType.Consumable, price = 150, consumableEffect = ConsumableEffectType.AtkBuff, consumableEffectValue = 3 },
                [4008] = new ItemConfig { id = 4008, name = "护盾卷轴", desc = "下次受伤减伤50%", type = ItemType.Consumable, price = 100, consumableEffect = ConsumableEffectType.Shield },
                [4009] = new ItemConfig { id = 4009, name = "回城卷轴", desc = "地牢中直接回城镇", type = ItemType.Consumable, price = 80, consumableEffect = ConsumableEffectType.Teleport },
                [4010] = new ItemConfig { id = 4010, name = "经验书", desc = "下一场战斗经验+50%", type = ItemType.Consumable, price = 200, consumableEffect = ConsumableEffectType.ExpBoost },
                [4011] = new ItemConfig { id = 4011, name = "炸弹", desc = "战斗中造成50点伤害，无视防御", type = ItemType.Consumable, price = 300, consumableEffect = ConsumableEffectType.Bomb, consumableEffectValue = 50 }
            };
        }

        private void InitMonsters()
        {
            monsters = new Dictionary<int, MonsterConfig>();
            var data = LoadJson<MonsterConfigList>("Config/monsters");
            if (data?.list != null)
            {
                foreach (var c in data.list) monsters[c.id] = c;
                return;
            }
            Logger.LogW("ConfigManager", "monsters.json加载失败，使用硬编码fallback");
            monsters = new Dictionary<int, MonsterConfig>
            {
                [1001] = new MonsterConfig { id = 1001, name = "史莱姆", desc = "软绵绵的粘液生物", icon = "史", hp = 30, attack = 5, defense = 2, speed = 3, expReward = 10, goldReward = 8, minFloor = 1, maxFloor = 6, aiPattern = MonsterAIPattern.Balanced, drops = { new ItemDropConfig { itemId = 4001, chance = 0.3f } } },
                [1002] = new MonsterConfig { id = 1002, name = "骷髅兵", desc = "被黑暗力量唤醒的亡灵战士", icon = "骷", hp = 50, attack = 8, defense = 4, speed = 5, expReward = 20, goldReward = 10, minFloor = 1, maxFloor = 8, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 4001, chance = 0.2f }, new ItemDropConfig { itemId = 1001, chance = 0.1f } } },
                [1003] = new MonsterConfig { id = 1003, name = "巨型蝙蝠", desc = "洞穴中变异的巨大蝙蝠", icon = "蝠", hp = 25, attack = 6, defense = 1, speed = 10, expReward = 12, goldReward = 8, minFloor = 1, maxFloor = 7, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 3002, chance = 0.05f } } },
                [1004] = new MonsterConfig { id = 1004, name = "哥布林", desc = "狡猾的绿色小恶魔", icon = "哥", hp = 60, attack = 10, defense = 5, speed = 6, expReward = 25, goldReward = 12, minFloor = 2, maxFloor = 9, aiPattern = MonsterAIPattern.Balanced, drops = { new ItemDropConfig { itemId = 4001, chance = 0.3f }, new ItemDropConfig { itemId = 1002, chance = 0.08f } } },
                [1005] = new MonsterConfig { id = 1005, name = "暗影狼", desc = "被暗影侵蚀的野狼", icon = "狼", hp = 80, attack = 14, defense = 6, speed = 12, expReward = 35, goldReward = 10, minFloor = 3, maxFloor = 10, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 4003, chance = 0.2f } } },
                [1010] = new MonsterConfig { id = 1010, name = "重甲兵", desc = "穿着厚重铠甲的守卫", icon = "重", hp = 100, attack = 13, defense = 12, speed = 4, mp = 25, expReward = 38, goldReward = 12, minFloor = 3, maxFloor = 11, skillIds = { 3005 }, aiPattern = MonsterAIPattern.Defensive, drops = { new ItemDropConfig { itemId = 4001, chance = 0.3f } } },
                [1008] = new MonsterConfig { id = 1008, name = "毒蛇", desc = "剧毒蛇类", icon = "蛇", hp = 55, attack = 11, defense = 3, speed = 14, mp = 20, expReward = 30, goldReward = 10, minFloor = 2, maxFloor = 9, skillIds = { 2006 }, aiPattern = MonsterAIPattern.Debuffer, drops = { new ItemDropConfig { itemId = 4001, chance = 0.25f } } },
                [1006] = new MonsterConfig { id = 1006, name = "石像鬼", desc = "石化守护者", icon = "石", hp = 120, attack = 12, defense = 15, speed = 3, expReward = 40, goldReward = 10, minFloor = 4, maxFloor = 12, aiPattern = MonsterAIPattern.Defensive, drops = { new ItemDropConfig { itemId = 2002, chance = 0.05f } } },
                [1007] = new MonsterConfig { id = 1007, name = "暗黑法师", desc = "掌握黑暗魔法的法师", icon = "暗", hp = 70, attack = 18, defense = 4, speed = 8, mp = 35, expReward = 45, goldReward = 10, minFloor = 5, maxFloor = 13, skillIds = { 6013 }, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 4003, chance = 0.2f } } },
                [1009] = new MonsterConfig { id = 1009, name = "暗影法师", desc = "操控暗影之力的法师", icon = "影", hp = 65, attack = 16, defense = 3, speed = 9, mp = 35, expReward = 50, goldReward = 12, minFloor = 5, maxFloor = 14, skillIds = { 5006 }, aiPattern = MonsterAIPattern.Debuffer, drops = { new ItemDropConfig { itemId = 4003, chance = 0.2f } } },
                [1011] = new MonsterConfig { id = 1011, name = "丧尸", desc = "腐烂的不死生物", icon = "丧", hp = 130, attack = 15, defense = 8, speed = 3, mp = 30, expReward = 45, goldReward = 8, minFloor = 5, maxFloor = 14, skillIds = { 1005 }, aiPattern = MonsterAIPattern.Balanced, drops = { new ItemDropConfig { itemId = 4001, chance = 0.3f } } },
                [1012] = new MonsterConfig { id = 1012, name = "亡灵骑士", desc = "堕落的骑士灵魂", icon = "亡", hp = 150, attack = 22, defense = 12, speed = 7, mp = 40, expReward = 65, goldReward = 8, minFloor = 10, maxFloor = 19, skillIds = { 3001, 6011 }, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 4002, chance = 0.2f } } },
                [1013] = new MonsterConfig { id = 1013, name = "火元素", desc = "纯粹火焰凝聚的元素生物", icon = "火", hp = 110, attack = 25, defense = 6, speed = 9, mp = 30, expReward = 70, goldReward = 9, minFloor = 10, maxFloor = 19, skillIds = { 6013, 5001 }, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 4003, chance = 0.25f } } },
                [1014] = new MonsterConfig { id = 1014, name = "食人魔", desc = "体型庞大的食人巨怪", icon = "食", hp = 200, attack = 20, defense = 14, speed = 4, mp = 30, expReward = 75, goldReward = 10, minFloor = 10, maxFloor = 19, skillIds = { 6011 }, aiPattern = MonsterAIPattern.Balanced, drops = { new ItemDropConfig { itemId = 4002, chance = 0.25f } } },
                [1015] = new MonsterConfig { id = 1015, name = "暗影刺客", desc = "潜伏在黑暗中的致命杀手", icon = "刺", hp = 90, attack = 28, defense = 5, speed = 15, mp = 30, expReward = 80, goldReward = 12, minFloor = 10, maxFloor = 19, skillIds = { 6010, 6015 }, aiPattern = MonsterAIPattern.Debuffer, drops = { new ItemDropConfig { itemId = 3001, chance = 0.08f } } },
                [1016] = new MonsterConfig { id = 1016, name = "冰霜巨人", desc = "远古冰封的巨人", icon = "冰", hp = 280, attack = 26, defense = 20, speed = 5, mp = 40, expReward = 110, goldReward = 10, minFloor = 15, maxFloor = 24, skillIds = { 6012, 6015 }, aiPattern = MonsterAIPattern.Defensive, drops = { new ItemDropConfig { itemId = 4004, chance = 0.15f } } },
                [1017] = new MonsterConfig { id = 1017, name = "恶魔猎犬", desc = "来自地狱的猎犬", icon = "犬", hp = 180, attack = 32, defense = 10, speed = 14, mp = 30, expReward = 120, goldReward = 12, minFloor = 15, maxFloor = 24, skillIds = { 6013, 1005 }, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 4005, chance = 0.05f } } },
                [1018] = new MonsterConfig { id = 1018, name = "魅魔", desc = "以幻魅惑人心的恶魔", icon = "魅", hp = 160, attack = 24, defense = 8, speed = 12, mp = 50, expReward = 130, goldReward = 12, minFloor = 15, maxFloor = 24, skillIds = { 6016, 6010, 5006 }, aiPattern = MonsterAIPattern.Debuffer, drops = { new ItemDropConfig { itemId = 4004, chance = 0.2f } } },
                [1019] = new MonsterConfig { id = 1019, name = "深渊之眼", desc = "悬浮在空中的巨大眼球", icon = "眼", hp = 200, attack = 30, defense = 12, speed = 10, mp = 60, expReward = 140, goldReward = 10, minFloor = 15, maxFloor = 24, skillIds = { 6012, 6014, 5006 }, aiPattern = MonsterAIPattern.Balanced, drops = { new ItemDropConfig { itemId = 3003, chance = 0.1f } } },
                [1020] = new MonsterConfig { id = 1020, name = "远古龙裔", desc = "远古巨龙的后裔", icon = "龙", hp = 350, attack = 38, defense = 18, speed = 10, mp = 60, expReward = 200, goldReward = 10, minFloor = 20, maxFloor = 0, skillIds = { 6001, 6013, 6003 }, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 4005, chance = 0.1f } } },
                [1021] = new MonsterConfig { id = 1021, name = "虚空行者", desc = "穿梭于虚空维度的神秘存在", icon = "虚", hp = 250, attack = 35, defense = 14, speed = 18, mp = 80, expReward = 220, goldReward = 12, minFloor = 20, maxFloor = 0, skillIds = { 6016, 5006, 6010 }, aiPattern = MonsterAIPattern.Debuffer, drops = { new ItemDropConfig { itemId = 4005, chance = 0.1f } } },
                [1022] = new MonsterConfig { id = 1022, name = "堕落天使", desc = "背叛天界的堕天使", icon = "堕", hp = 320, attack = 36, defense = 16, speed = 13, mp = 100, expReward = 240, goldReward = 12, minFloor = 20, maxFloor = 0, skillIds = { 6006, 6008, 6001 }, aiPattern = MonsterAIPattern.Balanced, drops = { new ItemDropConfig { itemId = 3003, chance = 0.12f } } },
                [1023] = new MonsterConfig { id = 1023, name = "暗黑龙将", desc = "暗黑龙王麾下最强的龙人将军", icon = "将", hp = 400, attack = 42, defense = 22, speed = 11, mp = 70, expReward = 280, goldReward = 15, minFloor = 23, maxFloor = 0, skillIds = { 6004, 6010, 6015 }, aiPattern = MonsterAIPattern.Aggressive, drops = { new ItemDropConfig { itemId = 2003, chance = 0.08f } } },
                [2001] = new MonsterConfig { id = 2001, name = "暗影领主", desc = "统治地牢上层的暗影之王", icon = "主", hp = 300, attack = 22, defense = 10, speed = 8, mp = 50, expReward = 250, goldReward = 100, minFloor = 5, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9001, 9002 }, drops = { new ItemDropConfig { itemId = 1005, chance = 1f } } },
                [2002] = new MonsterConfig { id = 2002, name = "堕落骑士", desc = "背弃誓言的圣骑士", icon = "骑", hp = 550, attack = 28, defense = 28, speed = 6, mp = 60, expReward = 500, goldReward = 200, minFloor = 10, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9003, 9004 }, drops = { new ItemDropConfig { itemId = 1006, chance = 1f } } },
                [2003] = new MonsterConfig { id = 2003, name = "深渊法师", desc = "钻研深渊禁术的魔导师", icon = "渊", hp = 700, attack = 35, defense = 15, speed = 10, mp = 100, expReward = 750, goldReward = 300, minFloor = 15, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9005, 9006 }, drops = { new ItemDropConfig { itemId = 2005, chance = 1f } } },
                [2004] = new MonsterConfig { id = 2004, name = "混沌巨兽", desc = "由混沌之力孕育的巨兽", icon = "兽", hp = 1000, attack = 42, defense = 25, speed = 7, mp = 80, expReward = 1000, goldReward = 400, minFloor = 20, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9007, 9008 }, drops = { new ItemDropConfig { itemId = 2006, chance = 1f } } },
                [2005] = new MonsterConfig { id = 2005, name = "暗黑龙王", desc = "地牢最深处的终极统治者", icon = "王", hp = 1500, attack = 50, defense = 30, speed = 12, mp = 120, expReward = 1500, goldReward = 500, minFloor = 25, isBoss = true, aiPattern = MonsterAIPattern.Boss, skillIds = { 9009, 9010, 9011 }, drops = { new ItemDropConfig { itemId = 3006, chance = 1f } } }
            };
        }

        private void InitTreasures()
        {
            treasures = new Dictionary<int, TreasureConfig>();
            var data = LoadJson<TreasureConfigList>("Config/treasures");
            if (data?.list != null)
            {
                foreach (var c in data.list) treasures[c.id] = c;
                return;
            }
            Logger.LogW("ConfigManager", "treasures.json加载失败，使用硬编码fallback");
            treasures = new Dictionary<int, TreasureConfig>
            {
                [1] = new TreasureConfig { id = 1, name = "勇者之证", desc = "攻击力+10%", effectType = 0, effectValue = 10 },
                [2] = new TreasureConfig { id = 2, name = "守护之盾", desc = "防御力+15%", effectType = 1, effectValue = 15 },
                [3] = new TreasureConfig { id = 3, name = "生命之泉", desc = "生命值+20%", effectType = 2, effectValue = 20 },
                [4] = new TreasureConfig { id = 4, name = "吸血之牙", desc = "攻击时恢复10%伤害值的生命", effectType = 3, effectValue = 10 },
                [5] = new TreasureConfig { id = 5, name = "幸运硬币", desc = "金币获取+30%", effectType = 3, effectValue = 30 },
                [6] = new TreasureConfig { id = 6, name = "经验宝石", desc = "经验获取+20%", effectType = 3, effectValue = 20 },
                [7] = new TreasureConfig { id = 7, name = "暴击之眼", desc = "暴击率+10%", effectType = 4, effectValue = 10 },
                [8] = new TreasureConfig { id = 8, name = "风行之靴", desc = "闪避率+10%", effectType = 5, effectValue = 10 },
                [9] = new TreasureConfig { id = 9, name = "贤者之心", desc = "技能消耗-20%", effectType = 6, effectValue = 20 },
                [10] = new TreasureConfig { id = 10, name = "守护结界", desc = "战斗开始获得20%最大HP的护盾", effectType = 7, effectValue = 20 },
                [11] = new TreasureConfig { id = 11, name = "荆棘之甲", desc = "受击10%概率反伤50%", effectType = 8, effectValue = 50 },
                [12] = new TreasureConfig { id = 12, name = "贪婪之戒", desc = "掉落翻倍但受伤+15%", effectType = 9, effectValue = 15 }
            };
        }

        private void InitBuffs()
        {
            buffs = new Dictionary<int, BuffConfig>();
            var data = LoadJson<BuffConfigList>("Config/buffs");
            if (data?.list != null)
            {
                foreach (var c in data.list) buffs[c.id] = c;
                return;
            }
            Logger.LogW("ConfigManager", "buffs.json加载失败，使用硬编码fallback");
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
