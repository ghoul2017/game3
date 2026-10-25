using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game
{
    public enum RoomKind
    {
        Entrance, Monster, Elite, Chest, Spring, Shrine, Merchant, Stairs, Boss, LegacyPassage, Wall, Barrier
    }
    public enum ItemKind
    {
        Weapon, Armor, Accessory, Potion
    }
    public enum SkillKind
    {
        Strike, Poison, Bleed, Heal, Shield, Stun, Drain, Passive
    }

    [Serializable]
    public class Stats
    {
        public int hp;
        public int mp;
        public int attack;
        public int magic;
        public int defense;
        public int speed;

        public void Add(Stats other, int count = 1)
        {
            hp += other.hp * count;
            mp += other.mp * count;
            attack += other.attack * count;
            magic += other.magic * count;
            defense += other.defense * count;
            speed += other.speed * count;
        }
    }

    [Serializable]
    public class ProfessionDef
    {
        public int id;
        public string name;
        public string title;
        public string description;
        public string symbol;
        public Stats basis;
        public Stats growth;
        public int startingSkill;
        public int startingWeapon;
    }

    [Serializable]
    public class SkillDef
    {
        public int id;
        public int profession;
        public string name;
        public string description;
        public SkillKind kind;
        public int level;
        public int prerequisite;
        public int price;
        public int cost;
        public int power;
        public int defensePower;
        public int duration;
        public bool magical;
        public Stats bonus = new Stats();
    }

    [Serializable]
    public class ItemDef
    {
        public int id;
        public string name;
        public string description;
        public ItemKind kind;
        public int level;
        public int price;
        public Stats bonus = new Stats();
        public int heal;
        public int mana;
    }

    [Serializable]
    public class MonsterDef
    {
        public int id;
        public string name;
        public string description;
        public string symbol;
        public int minFloor;
        public int maxFloor;
        public bool boss;
        public int spawnRank;
        public Stats basis;
        public int experience;
        public int gold;
        public List<int> skills;
        public int drop;
        public int dropChance;
    }

    [Serializable]
    public class RelicDef
    {
        public int id;
        public string name;
        public string description;
        public Stats bonus = new Stats();
        public int lifesteal;
        public int thorns;
        public int goldPercent;
        public int experiencePercent;
        public int manaDiscount;
        public int openingShield;
    }

    [Serializable]
    public class FloorDef
    {
        public int number;
        public string region;
        public string subtitle;
        public int width;
        public int height;
        public int contentMinPercent;
        public int contentMaxPercent;
        public int obstacleMinPercent;
        public int obstacleMaxPercent;
        public int monsterPercent;
        public int elitePercent;
        public int scalePercent;
        public int chestGold;
        public int boss;
        public int wallPower;
        public int barrierPower;
    }

    public sealed class GameConfig
    {
        [Serializable]
        private class Table<T>
        {
            public List<T> list;
        }
        public readonly List<ProfessionDef> Professions;
        public readonly List<SkillDef> Skills;
        public readonly List<ItemDef> Items;
        public readonly List<MonsterDef> Monsters;
        public readonly List<RelicDef> Relics;
        public readonly List<FloorDef> Floors;

        public GameConfig()
        {
            Professions = Load<ProfessionDef>("professions");
            Skills = Load<SkillDef>("skills");
            Items = Load<ItemDef>("items");
            Monsters = Load<MonsterDef>("monsters");
            Relics = Load<RelicDef>("treasures");
            Floors = Load<FloorDef>("floors");
            Validate();
        }

        private static List<T> Load<T>(string name)
        {
            TextAsset asset = Resources.Load<TextAsset>($"Config/{name}");
            if (asset == null)
            {
                throw new InvalidOperationException($"[配置] 缺少 Config/{name}.json");
            }
            Table<T> table = JsonUtility.FromJson<Table<T>>(asset.text);
            if (table == null || table.list == null || table.list.Count == 0)
            {
                throw new InvalidOperationException($"[配置] {name} 没有有效数据");
            }
            return table.list;
        }

        public ProfessionDef Profession(int id) => Professions.Single(x => x.id == id);
        public SkillDef Skill(int id) => Skills.Single(x => x.id == id);
        public ItemDef Item(int id) => Items.Single(x => x.id == id);
        public MonsterDef Monster(int id) => Monsters.Single(x => x.id == id);
        public RelicDef Relic(int id) => Relics.Single(x => x.id == id);
        public FloorDef Floor(int number) => Floors.Single(x => x.number == number);

        public void Validate()
        {
            Unique(Professions.Select(x => x.id), "职业");
            Unique(Skills.Select(x => x.id), "技能");
            Unique(Items.Select(x => x.id), "物品");
            Unique(Monsters.Select(x => x.id), "怪物");
            Unique(Relics.Select(x => x.id), "宝物");
            Unique(Floors.Select(x => x.number), "楼层");
            Require(Professions.Count == 5, "需要五个职业");
            foreach (ProfessionDef profession in Professions)
            {
                Require(Skill(profession.startingSkill).profession == profession.id, "初始技能职业不符");
                Require(Item(profession.startingWeapon).kind == ItemKind.Weapon, "初始武器类型错误");
                Require(profession.basis.hp > 0 && profession.basis.mp >= 0, "职业生命与法力错误");
            }
            foreach (SkillDef skill in Skills)
            {
                Profession(skill.profession);
                Require(skill.cost >= 0 && skill.price >= 0 && skill.level > 0 && Enum.IsDefined(typeof(SkillKind), skill.kind), "技能数值错误");
                if (skill.prerequisite != 0)
                {
                    SkillDef prerequisite = Skill(skill.prerequisite);
                    Require(prerequisite.profession == skill.profession && prerequisite.level < skill.level, "技能前置应为同职业低等级技能");
                }
            }
            foreach (ItemDef item in Items)
            {
                Require(item.price > 0 && item.level > 0 && Enum.IsDefined(typeof(ItemKind), item.kind), "物品配置错误");
            }
            foreach (MonsterDef monster in Monsters)
            {
                Require(monster.boss || monster.spawnRank > 0, "普通怪物需配置出现档次");
                Require(monster.basis.hp > 0 && monster.experience >= 0 && monster.gold >= 0, "怪物配置错误");
                Require(monster.dropChance >= 0 && monster.dropChance <= 100, "掉落概率应为百分比");
                foreach (int skill in monster.skills)
                {
                    Require(Skill(skill).kind != SkillKind.Passive, "怪物不能主动使用被动技能");
                }
                if (monster.drop != 0)
                {
                    Item(monster.drop);
                }
            }
            for (int number = 1; number <= Floors.Count; number++)
            {
                FloorDef floor = Floor(number);
                Require(floor.width >= 22 && floor.height >= 14, "地牢尺寸错误");
                Require(floor.contentMinPercent >= 35 && floor.contentMaxPercent >= floor.contentMinPercent && floor.contentMaxPercent <= 65, "内容区块占地率错误");
                Require(floor.obstacleMinPercent >= 2 && floor.obstacleMaxPercent >= floor.obstacleMinPercent && floor.obstacleMaxPercent + floor.contentMaxPercent <= 75, "障碍占地率错误");
                Require(floor.monsterPercent >= 0 && floor.elitePercent >= 0 && floor.monsterPercent + floor.elitePercent <= 70, "房间比例错误");
                Require(floor.wallPower > 0 && floor.barrierPower > 0, "障碍强度必须为正数");
                Require(floor.scalePercent > 0 && floor.chestGold > 0, "楼层数值错误");
                Require(Monsters.Any(x => !x.boss && x.minFloor <= number && x.maxFloor >= number), "楼层没有普通怪物");
                if (floor.boss != 0)
                {
                    Require(Monster(floor.boss).boss, "首领配置错误");
                }
            }
            Require(Floors.Last().boss != 0, "最后一层需要首领");
        }

        private static void Unique(IEnumerable<int> ids, string name)
        {
            List<int> values = ids.ToList();
            Require(values.Distinct().Count() == values.Count, $"{name}编号重复");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"[配置] {message}");
            }
        }
    }
}
