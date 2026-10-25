using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Game
{
    public sealed class SaveStore
    {
        [Serializable]
        private class Document
        {
            public bool hasBattle;
            public AdventureData adventure;
        }

        public string Path
        {
            get;
        }
        public bool Exists => File.Exists(Path);

        public SaveStore(string path)
        {
            Path = path;
        }

        public void Save(AdventureData data)
        {
            string directory = System.IO.Path.GetDirectoryName(Path);
            Directory.CreateDirectory(directory);
            string temporary = $"{Path}.tmp";
            // JsonUtility 会把内联空类恢复为默认实例，用显式存在标记保存可选战斗。
            Document document = new Document { hasBattle = data.battle != null, adventure = data };
            File.WriteAllText(temporary, JsonUtility.ToJson(document, true));
            if (File.Exists(Path))
            {
                File.Replace(temporary, Path, null);
            }
            else
            {
                File.Move(temporary, Path);
            }
        }

        public AdventureData Load(GameConfig config)
        {
            Document document = JsonUtility.FromJson<Document>(File.ReadAllText(Path));
            Require(document != null && document.adventure != null, "存档内容缺失");
            AdventureData data = document.adventure;
            if (!document.hasBattle)
            {
                data.battle = null;
            }
            if (data.version == 2 && data.floors != null)
            {
                // 旧通道以已破坏墙块保留邻接和占地，新地图不再生成通道。
                foreach (RoomData room in data.floors.SelectMany(f => f.rooms).Where(r => r.kind == RoomKind.LegacyPassage))
                {
                    room.kind = RoomKind.Wall;
                    room.cleared = true;
                }
                data.version = 3;
            }
            if (data.hero != null && data.hero.skillRanks == null)
            {
                data.hero.skillRanks = new System.Collections.Generic.List<ItemStack>();
            }
            Validate(data, config);
            return data;
        }

        public static void Validate(AdventureData data, GameConfig config)
        {
            Require(data != null && data.version == 3 && data.hero != null, "存档版本或角色无效");
            HeroData hero = data.hero;
            config.Profession(hero.profession);
            Require(!string.IsNullOrWhiteSpace(hero.name) && hero.level >= 1 && hero.experience >= 0 && hero.experience < hero.ExperienceNeeded && hero.gold >= 0, "角色数据无效");
            Require(hero.skills != null && hero.relics != null && hero.bag != null, "角色物品数据缺失");
            Require(hero.skills.Distinct().Count() == hero.skills.Count && hero.relics.Distinct().Count() == hero.relics.Count && hero.bag.Select(x => x.id).Distinct().Count() == hero.bag.Count, "存档有重复物品或技能");
            Require(hero.skillRanks == null || (hero.skillRanks.Select(x => x.id).Distinct().Count() == hero.skillRanks.Count && hero.skillRanks.All(x => hero.skills.Contains(x.id) && x.count >= 1 && x.count <= 2)), "技能强化数据无效");
            foreach (int id in hero.skills)
            {
                SkillDef skill = config.Skill(id);
                Require(skill.id < 900, "冒险技能无效");
            }
            foreach (int id in hero.relics)
            {
                config.Relic(id);
            }
            foreach (ItemStack stack in hero.bag)
            {
                config.Item(stack.id);
                Require(stack.count > 0, "物品数量无效");
            }
            int[] equipment = { hero.weapon, hero.armor, hero.accessory };
            for (int slot = 0; slot < equipment.Length; slot++)
            {
                if (equipment[slot] != 0)
                {
                    ItemDef item = config.Item(equipment[slot]);
                    Require((int)item.kind == slot && item.level <= hero.level, "装备数据无效");
                }
            }
            Stats stats = hero.GetStats(config);
            Require(hero.hp > 0 && hero.hp <= stats.hp && hero.mp >= 0 && hero.mp <= stats.mp, "生命或法力无效");
            Require(data.currentFloor >= 1 && data.currentFloor <= data.unlockedFloor && data.unlockedFloor <= config.Floors.Count, "楼层进度无效");
            Require(data.floors != null && data.journal != null && data.floors.Select(x => x.number).Distinct().Count() == data.floors.Count, "楼层数据无效");
            foreach (FloorData floor in data.floors)
            {
                FloorDef definition = config.Floor(floor.number);
                Require(floor.number <= data.unlockedFloor && floor.rooms != null && floor.rooms.Count > 0, "已生成楼层无效");
                Require(floor.rooms.Count(x => x.kind == RoomKind.Entrance) == 1 && floor.rooms.Count(x => x.kind == RoomKind.Stairs) == 1, "地牢入口或楼梯缺失");
                Require(floor.rooms.Select(x => x.id).Distinct().Count() == floor.rooms.Count, "房间编号重复");
                foreach (RoomData room in floor.rooms)
                {
                    Require(room.kind != RoomKind.LegacyPassage && room.breakPower >= 0, "障碍数据无效");
                    Require(room.x >= 0 && room.y >= 0 && room.width > 0 && room.height > 0 && room.x + room.width <= definition.width && room.y + room.height <= definition.height && Enum.IsDefined(typeof(RoomKind), room.kind), "房间尺寸或类型无效");
                    if (room.kind == RoomKind.Monster || room.kind == RoomKind.Elite || room.kind == RoomKind.Boss)
                    {
                        config.Monster(room.monster);
                    }
                }
            }
            Require(data.inTown || data.floors.Any(x => x.number == data.currentFloor), "当前楼层缺失");
            if (data.battle != null)
            {
                BattleData battle = data.battle;
                Require(!data.inTown && battle.stats != null && battle.enemy != null && battle.hero != null, "战斗数据缺失");
                RoomData room = data.Current.Room(battle.room);
                Require(!room.cleared && room.revealed && room.monster == battle.monster, "战斗房间无效");
                Require(battle.enemy.hp > 0 && battle.enemy.hp <= battle.stats.hp && battle.enemy.mp >= 0 && battle.enemy.mp <= battle.stats.mp && battle.round > 0, "敌人状态无效");
                Require(battle.hero.hp == hero.hp && battle.hero.mp == hero.mp, "战斗角色状态不同步");
                if (battle.intent != 0)
                {
                    Require(config.Monster(battle.monster).skills.Contains(battle.intent) && config.Skill(battle.intent).cost <= battle.enemy.mp, "敌人意图无效");
                }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException($"[存档] {message}，原文件保留供检查。");
            }
        }
    }
}
