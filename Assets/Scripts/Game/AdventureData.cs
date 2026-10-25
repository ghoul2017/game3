using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    [Serializable]
    public class ItemStack
    {
        public int id;
        public int count;
    }

    [Serializable]
    public class HeroData
    {
        public string name;
        public int profession;
        public int level = 1;
        public int experience;
        public int gold = 80;
        public int hp;
        public int mp;
        public int weapon;
        public int armor;
        public int accessory;
        public List<int> skills = new List<int>();
        public List<ItemStack> skillRanks = new List<ItemStack>();
        public int SkillRank(int id) => 1 + (skillRanks == null ? 0 : skillRanks.Where(x => x.id == id).Sum(x => x.count));
        public List<int> relics = new List<int>();
        public List<ItemStack> bag = new List<ItemStack>();

        public Stats GetStats(GameConfig config)
        {
            ProfessionDef professionDef = config.Profession(profession);
            Stats result = new Stats();
            result.Add(professionDef.basis);
            result.Add(professionDef.growth, level - 1);
            foreach (int id in new[] { weapon, armor, accessory })
            {
                if (id != 0)
                {
                    result.Add(config.Item(id).bonus);
                }
            }
            foreach (int id in skills)
            {
                if (config.Skill(id).kind == SkillKind.Passive)
                {
                    result.Add(config.Skill(id).bonus, SkillRank(id));
                }
            }
            foreach (int id in relics)
            {
                result.Add(config.Relic(id).bonus);
            }
            return result;
        }

        public int Count(int id) => bag.Where(x => x.id == id).Sum(x => x.count);

        public void AddItem(int id, int count)
        {
            ItemStack stack = bag.Find(x => x.id == id);
            if (stack == null)
            {
                if (count < 0)
                {
                    throw new InvalidOperationException("[背包] 物品不足");
                }
                stack = new ItemStack { id = id };
                bag.Add(stack);
            }
            if (stack.count + count < 0)
            {
                throw new InvalidOperationException("[背包] 物品不足");
            }
            stack.count += count;
            if (stack.count == 0)
            {
                bag.Remove(stack);
            }
        }

        public int ExperienceNeeded => 35 + level * 25;

        public int AddExperience(int amount, GameConfig config)
        {
            int previous = level;
            experience += amount;
            while (experience >= ExperienceNeeded)
            {
                experience -= ExperienceNeeded;
                level++;
            }
            if (level != previous)
            {
                Stats stats = GetStats(config);
                hp = stats.hp;
                mp = stats.mp;
            }
            return level - previous;
        }
    }

    [Serializable]
    public class RewardData
    {
        public int kind; // 0：物品，1：宝物，2：技能，3：金币。
        public int id;
        public int count = 1;
        public int price;
    }

    [Serializable]
    public class RoomData
    {
        public int id;
        public int x;
        public int y;
        public int width = 1;
        public int height = 1;
        public RoomKind kind;
        public int monster;
        public bool revealed;
        public bool cleared;
        public int reward;
        public int breakPower;
        public List<RewardData> offers = new List<RewardData>();
        public int Tier => width * height <= 2 ? 1 : width * height <= 6 ? 2 : width * height <= 9 ? 3 : 4;
        public bool IsObstacle => kind == RoomKind.Wall || kind == RoomKind.Barrier;

        public bool Adjacent(RoomData other)
        {
            bool horizontal = (x + width == other.x || other.x + other.width == x) && y < other.y + other.height && y + height > other.y;
            bool vertical = (y + height == other.y || other.y + other.height == y) && x < other.x + other.width && x + width > other.x;
            return horizontal || vertical;
        }
    }

    [Serializable]
    public class FloorData
    {
        public int number;
        public List<RoomData> rooms = new List<RoomData>();
        public RoomData Room(int id) => rooms.Single(x => x.id == id);

        public void RevealFrom(RoomData source)
        {
            source.revealed = true;
            foreach (RoomData room in rooms.Where(source.Adjacent))
            {
                room.revealed = true;
            }
        }

        public bool BossCleared => rooms.All(x => x.kind != RoomKind.Boss || x.cleared);
    }

    [Serializable]
    public class FighterData
    {
        public int hp;
        public int mp;
        public int shield;
        public int poison;
        public int poisonTurns;
        public int bleed;
        public int bleedTurns;
        public int stun;
        public bool guarding;
    }

    [Serializable]
    public class BattleData
    {
        public int room;
        public int monster;
        public int round = 1;
        public int intent;
        public bool elite;
        public Stats stats;
        public FighterData enemy = new FighterData();
        public FighterData hero = new FighterData();
    }

    [Serializable]
    public class AdventureData
    {
        public int version = 3;
        public int seed;
        public int randomState = 1;
        public HeroData hero;
        public bool inTown = true;
        public bool completed;
        public int currentFloor = 1;
        public int unlockedFloor = 1;
        public List<FloorData> floors = new List<FloorData>();
        public BattleData battle;
        public List<string> journal = new List<string>();
        public FloorData Current => floors.Single(x => x.number == currentFloor);
    }
}
