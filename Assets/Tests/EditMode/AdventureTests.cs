using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using NUnit.Framework;
using UnityEngine;

public sealed class AdventureTests
{
    private GameConfig config;
    private string directory;
    private AdventureSession session;

    [SetUp]
    public void SetUp()
    {
        config = new GameConfig();
        directory = Path.Combine(Application.temporaryCachePath, $"game3-tests-{Guid.NewGuid():N}");
        session = new AdventureSession(config, new SaveStore(Path.Combine(directory, "adventure.json")));
        session.Create(0, "测试旅人", 1729);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void RandomFloorsAreConnectedAndContainRequiredRooms()
    {
        Dictionary<(int, int), int> shapes = new Dictionary<(int, int), int>();
        Dictionary<(int, int), int> walls = new Dictionary<(int, int), int>();
        List<int> contentCounts = new List<int>();
        List<int> obstacleCounts = new List<int>();
        for (int seed = 1; seed <= 60; seed++)
        {
            foreach (FloorDef definition in config.Floors)
            {
                FloorData floor = DungeonGenerator.Generate(config, seed, definition.number);
                RoomData entrance = floor.rooms.Single(r => r.kind == RoomKind.Entrance);
                Assert.That(entrance.x + entrance.width / 2f, Is.EqualTo(definition.width / 2f).Within(0.5f));
                Assert.That(entrance.y + entrance.height / 2f, Is.EqualTo(definition.height / 2f).Within(0.5f));
                Assert.That(entrance.revealed && entrance.cleared, Is.True);
                contentCounts.Add(floor.rooms.Count(r => !r.IsObstacle));
                obstacleCounts.Add(floor.rooms.Count(r => r.IsObstacle));
                int area = (definition.width - 2) * (definition.height - 2);
                int contentArea = floor.rooms.Where(r => !r.IsObstacle).Sum(r => r.width * r.height);
                int obstacleArea = floor.rooms.Where(r => r.IsObstacle).Sum(r => r.width * r.height);
                Assert.That(contentArea, Is.InRange(area * definition.contentMinPercent / 100, area * definition.contentMaxPercent / 100 + 15));
                Assert.That(obstacleArea, Is.InRange(area * definition.obstacleMinPercent / 100, area * definition.obstacleMaxPercent / 100 + 3));
                HashSet<int> reached = new HashSet<int> { floor.rooms[0].id };
                Queue<RoomData> queue = new Queue<RoomData>();
                queue.Enqueue(floor.rooms[0]);
                while (queue.Count > 0)
                {
                    RoomData room = queue.Dequeue();
                    foreach (RoomData adjacent in floor.rooms.Where(room.Adjacent))
                    {
                        if (reached.Add(adjacent.id))
                        {
                            queue.Enqueue(adjacent);
                        }
                    }
                }
                Assert.That(reached.Count, Is.EqualTo(floor.rooms.Count), $"种子{seed}第{definition.number}层");
                Assert.That(floor.rooms.Count(x => x.kind == RoomKind.Stairs), Is.EqualTo(1));
                Assert.That(floor.rooms.Count(x => x.kind == RoomKind.Boss), Is.EqualTo(definition.boss == 0 ? 0 : 1));
                Assert.That(floor.rooms.Any(x => x.kind == RoomKind.Spring), Is.True);
                Assert.That(floor.rooms.Any(x => x.kind == RoomKind.Chest), Is.True);
                Assert.That(floor.rooms.Any(x => x.kind == RoomKind.Shrine), Is.True);
                Assert.That(floor.rooms.Any(x => x.kind == RoomKind.Merchant), Is.True);
                Assert.That(floor.rooms.Any(x => x.kind == RoomKind.LegacyPassage), Is.False, "新地图不能生成通道块");
                Assert.That(floor.rooms.Any(x => x.kind == RoomKind.Wall), Is.True);
                Assert.That(floor.rooms.Any(x => x.kind == RoomKind.Barrier), Is.True);
                List<RoomData> openRooms = floor.rooms.Where(r => !r.IsObstacle).ToList();
                HashSet<RoomData> openSeen = new HashSet<RoomData> { openRooms[0] };
                Queue<RoomData> openQueue = new Queue<RoomData>();
                openQueue.Enqueue(openRooms[0]);
                while (openQueue.Count > 0)
                {
                    RoomData current = openQueue.Dequeue();
                    foreach (RoomData adjacent in openRooms.Where(current.Adjacent))
                    {
                        if (openSeen.Add(adjacent))
                        {
                            openQueue.Enqueue(adjacent);
                        }
                    }
                }
                Assert.That(openSeen.Count, Is.EqualTo(openRooms.Count), "正常通关不能强制破坏成长障碍");
                Assert.That(floor.rooms.Where(x => x.kind != RoomKind.LegacyPassage).Select(x => (x.width, x.height)).Distinct().Count(), Is.GreaterThan(1), "房间尺寸应有变化");
                Assert.That(floor.rooms.Any(r => r.kind != RoomKind.LegacyPassage && floor.rooms.Any(other => other.kind != RoomKind.LegacyPassage && r.Adjacent(other))), Is.True, "功能区块之间应能直接共边");
                Assert.That(floor.rooms.Sum(r => r.width * r.height), Is.LessThan((definition.width - 2) * (definition.height - 2)), "地图内部应保留没有区块的空间");
                HashSet<(int, int)> occupied = new HashSet<(int, int)>();
                foreach (RoomData room in floor.rooms)
                {
                    (int, int) shape = (Math.Min(room.width, room.height), Math.Max(room.width, room.height));
                    (int, int)[] allowed = room.IsObstacle
                        ? new[] { (1, 1), (1, 2), (1, 3), (1, 4) }
                        : new[] { (1, 1), (1, 2), (2, 2), (2, 3), (3, 3), (3, 4), (4, 4) };
                    Assert.That(allowed.Contains(shape), Is.True, $"非法区块尺寸：{room.kind} {room.width}×{room.height}");
                    Dictionary<(int, int), int> counts = room.IsObstacle ? walls : shapes;
                    counts[shape] = counts.TryGetValue(shape, out int count) ? count + 1 : 1;
                    Assert.That(room.x >= 1 && room.y >= 1 && room.x + room.width < definition.width && room.y + room.height < definition.height, Is.True, "区块应在地图边界内");
                    for (int x = room.x; x < room.x + room.width; x++)
                    {
                        for (int y = room.y; y < room.y + room.height; y++)
                        {
                            Assert.That(occupied.Add((x, y)), Is.True, "房间不能重叠");
                        }
                    }
                }
            }
        }
        Assert.That(contentCounts.Distinct().Count(), Is.GreaterThan(10), "内容数量应随空间与随机尺寸变化");
        Assert.That(obstacleCounts.Distinct().Count(), Is.GreaterThan(5), "障碍数量不应固定");
        Dictionary<(int, int), int> weights = new Dictionary<(int, int), int>
        {
            [(1, 1)] = 8,
            [(1, 2)] = 12,
            [(2, 2)] = 30,
            [(2, 3)] = 25,
            [(3, 3)] = 15,
            [(3, 4)] = 7,
            [(4, 4)] = 3
        };
        foreach (KeyValuePair<(int, int), int> entry in weights)
        {
            double percent = 100.0 * shapes[entry.Key] / contentCounts.Sum();
            Assert.That(percent, Is.EqualTo(entry.Value).Within(4), "实际尺寸分布不应因空间填补而明显偏离权重");
            TestContext.WriteLine($"功能块 {entry.Key}：{percent:F2}%");
        }
        foreach (KeyValuePair<(int, int), int> entry in walls.OrderBy(x => x.Key))
        {
            TestContext.WriteLine($"障碍 {entry.Key}：{100.0 * entry.Value / obstacleCounts.Sum():F2}%");
        }
        TestContext.WriteLine($"600张地图：功能块 {contentCounts.Min()}～{contentCounts.Max()}，障碍 {obstacleCounts.Min()}～{obstacleCounts.Max()}");
    }

    [Test]
    public void RewardChoicesPersistAndOnlyGrantOneAndSkillsUpgrade()
    {
        session.EnterFloor(1);
        RoomData shrine = session.Data.Current.rooms.First(r => r.kind == RoomKind.Shrine);
        shrine.revealed = true;
        Assert.That(session.Explore(shrine.id), Is.True);
        Assert.That(shrine.cleared, Is.False);
        Assert.That(shrine.offers.Count, Is.EqualTo(3));
        string offers = JsonUtility.ToJson(shrine);
        session.Continue();
        shrine = session.Data.Current.Room(shrine.id);
        Assert.That(JsonUtility.ToJson(shrine), Is.EqualTo(offers));
        Assert.That(session.ChooseReward(shrine.id, 0), Is.True);
        Assert.That(session.ChooseReward(shrine.id, 1), Is.False);
        RoomData merchant = session.Data.Current.rooms.First(r => r.kind == RoomKind.Merchant);
        merchant.revealed = true;
        merchant.offers = new List<RewardData> { new RewardData { kind = 2, id = 101, price = 20 } };
        session.Data.hero.gold = 0;
        Assert.That(session.ChooseReward(merchant.id, 0), Is.False);
        session.Data.hero.gold = 20;
        int rank = session.Data.hero.SkillRank(101);
        Assert.That(session.ChooseReward(merchant.id, 0), Is.True);
        Assert.That(session.Data.hero.SkillRank(101), Is.EqualTo(rank + 1));
        session.Continue();
        Assert.That(session.Data.hero.SkillRank(101), Is.EqualTo(rank + 1));
        RoomData enemy = session.Data.Current.rooms.First(r => r.kind == RoomKind.Monster);
        enemy.width = enemy.height = 1;
        int small = session.EnemyStats(enemy).hp;
        enemy.width = enemy.height = 4;
        Assert.That(session.EnemyStats(enemy).hp, Is.EqualTo(small), "同种怪物的属性不随区块大小改变");
    }

    [Test]
    public void RoomSizeChangesMonsterSpeciesProbabilityOnly()
    {
        int[] counts = new int[4];
        int[] guards = new int[4];
        for (int seed = 1; seed <= 300; seed++)
        {
            FloorData floor = DungeonGenerator.Generate(config, seed, 1);
            foreach (RoomData room in floor.rooms.Where(r => r.kind == RoomKind.Monster || r.kind == RoomKind.Elite))
            {
                counts[room.Tier - 1]++;
                if (room.monster == 2)
                {
                    guards[room.Tier - 1]++;
                }
            }
        }
        double previous = 0;
        for (int i = 0; i < 4; i++)
        {
            double percent = 100.0 * guards[i] / counts[i];
            Assert.That(percent, Is.GreaterThan(previous + 10), "大区块应更容易生成骷髅卫兵，而不是给穴居鼠增加属性");
            TestContext.WriteLine($"第1层面积档次{i + 1}：骷髅卫兵占{percent:F2}%，样本{counts[i]}");
            previous = percent;
        }
        session.EnterFloor(1);
        foreach (RoomKind kind in new[] { RoomKind.Monster, RoomKind.Elite })
        {
            RoomData room = new RoomData { monster = 2, kind = kind, width = 1, height = 1 };
            string small = JsonUtility.ToJson(session.EnemyStats(room));
            room.width = room.height = 4;
            Assert.That(JsonUtility.ToJson(session.EnemyStats(room)), Is.EqualTo(small));
        }
    }

    [Test]
    public void LayoutUsesMapAreaAndSeed()
    {
        FloorData original = DungeonGenerator.Generate(config, 825, 1);
        Assert.That(JsonUtility.ToJson(DungeonGenerator.Generate(config, 825, 1)), Is.EqualTo(JsonUtility.ToJson(original)), "同一种子应生成相同布局");
        FloorDef definition = config.Floor(1);
        definition.width = 56;
        definition.height = 40;
        FloorData larger = DungeonGenerator.Generate(config, 825, 1);
        Assert.That(larger.rooms.Count, Is.GreaterThan(original.rooms.Count * 1.5), "地图扩展后应按空间增加区块");
        definition.contentMinPercent = 35;
        definition.contentMaxPercent = 35;
        FloorData sparse = DungeonGenerator.Generate(config, 825, 1);
        Assert.That(sparse.rooms.Where(r => !r.IsObstacle).Sum(r => r.width * r.height), Is.LessThan(larger.rooms.Where(r => !r.IsObstacle).Sum(r => r.width * r.height)), "占地率应实际影响地图内容");
    }

    [TestCase(RoomKind.Wall)]
    [TestCase(RoomKind.Barrier)]
    public void ObstaclesRequirePowerAndRemainBrokenAfterReturnAndLoad(RoomKind kind)
    {
        session.EnterFloor(1);
        RoomData wall = session.Data.Current.rooms.First(r => r.kind == kind);
        wall.revealed = true;
        foreach (RoomData neighbor in session.Data.Current.rooms.Where(wall.Adjacent))
        {
            neighbor.revealed = false;
        }
        int gold = session.Data.hero.gold;
        int mana = session.Data.hero.mp;
        Assert.That(session.Explore(wall.id), Is.False);
        Assert.That(wall.cleared, Is.False);
        Assert.That(session.Data.hero.gold, Is.EqualTo(gold));
        Assert.That(session.Data.hero.mp, Is.EqualTo(mana));
        Assert.That(session.Data.Current.rooms.Where(wall.Adjacent).All(r => !r.revealed), Is.True);
        session.Data.hero.AddExperience(100000, config);
        Assert.That(session.Explore(wall.id), Is.True);
        Assert.That(session.Data.Current.rooms.Where(wall.Adjacent).All(r => r.revealed), Is.True);
        int id = wall.id;
        session.ReturnToTown();
        session.Continue();
        session.EnterFloor(1);
        Assert.That(session.Data.Current.Room(id).cleared, Is.True);
    }

    [Test]
    public void VoidCannotRevealAcrossGapAndLegacySaveUpgradesInMemory()
    {
        RoomData first = new RoomData { id = 0, x = 0, y = 0, width = 2, height = 2 };
        RoomData far = new RoomData { id = 1, x = 3, y = 0, width = 2, height = 2 };
        FloorData floor = new FloorData { rooms = new List<RoomData> { first, far } };
        floor.RevealFrom(first);
        Assert.That(far.revealed, Is.False, "不能跨过虚空显露远端区块");
        session.EnterFloor(1);
        RoomData legacy = session.Data.Current.rooms.First(r => r.IsObstacle);
        legacy.kind = RoomKind.LegacyPassage;
        session.Data.version = 2;
        session.RetrySave();
        string original = File.ReadAllText(session.Store.Path);
        session.Continue();
        Assert.That(session.Data.version, Is.EqualTo(3));
        Assert.That(session.Data.Current.Room(legacy.id).kind, Is.EqualTo(RoomKind.Wall));
        Assert.That(session.Data.Current.Room(legacy.id).cleared, Is.True);
        Assert.That(File.ReadAllText(session.Store.Path), Is.EqualTo(original), "读取旧存档不应自行改写原文件");
    }

    [Test]
    public void TownResetsOnlyRenewableRoomsAndPreservesMapAndLoot()
    {
        session.EnterFloor(1);
        FloorData floor = session.Data.Current;
        foreach (RoomData room in floor.rooms)
        {
            room.cleared = true;
            room.revealed = true;
        }
        string layout = string.Join(";", floor.rooms.Select(x => $"{x.id},{x.x},{x.y},{x.kind}"));
        session.ReturnToTown();
        session.EnterFloor(1);
        Assert.That(string.Join(";", floor.rooms.Select(x => $"{x.id},{x.x},{x.y},{x.kind}")), Is.EqualTo(layout));
        foreach (RoomData room in floor.rooms)
        {
            bool renewable = room.kind == RoomKind.Monster || room.kind == RoomKind.Elite || room.kind == RoomKind.Spring;
            Assert.That(room.cleared, Is.EqualTo(!renewable));
            Assert.That(room.revealed, Is.True);
        }
        RoomData chest = floor.rooms.First(x => x.kind == RoomKind.Chest);
        int gold = session.Data.hero.gold;
        Assert.That(session.Explore(chest.id), Is.False);
        Assert.That(session.Data.hero.gold, Is.EqualTo(gold));
        session.Continue();
        Assert.That(session.Data.Current.rooms.First(x => x.id == chest.id).cleared, Is.True);
    }

    [Test]
    public void CombatSaveResumesExactlyAndRewardsCannotRepeat()
    {
        session.EnterFloor(1);
        RoomData enemy = session.Data.Current.rooms.First(x => x.kind == RoomKind.Monster);
        enemy.revealed = true;
        session.Explore(enemy.id);
        session.Act(-1);
        string before = JsonUtility.ToJson(session.Data);
        AdventureSession restored = new AdventureSession(config, session.Store);
        restored.Continue();
        Assert.That(JsonUtility.ToJson(restored.Data), Is.EqualTo(before));
        session.Act();
        restored.Act();
        Assert.That(JsonUtility.ToJson(restored.Data), Is.EqualTo(JsonUtility.ToJson(session.Data)), "读档不能重新随机敌人意图或伤害");
        for (int i = 0; i < 100 && session.Data.battle != null; i++)
        {
            session.Act();
        }
        Assert.That(session.Data.inTown, Is.False);
        Assert.That(session.Data.Current.Room(enemy.id).cleared, Is.True);
        int gold = session.Data.hero.gold;
        int exp = session.Data.hero.experience;
        Assert.That(session.Act(), Is.False);
        Assert.That(session.Explore(enemy.id), Is.False);
        Assert.That(session.Data.hero.gold, Is.EqualTo(gold));
        Assert.That(session.Data.hero.experience, Is.EqualTo(exp));
    }

    [Test]
    public void InvalidActionsDoNotSpendResourcesOrAdvanceRounds()
    {
        int gold = session.Data.hero.gold;
        Assert.That(session.Learn(504), Is.False);
        Assert.That(session.Data.hero.gold, Is.EqualTo(gold));
        Assert.That(session.UsePotion(2001), Is.False);
        Assert.That(session.Data.hero.Count(2001), Is.EqualTo(3));
        session.EnterFloor(1);
        RoomData enemy = session.Data.Current.rooms.First(x => x.kind == RoomKind.Monster);
        enemy.revealed = true;
        session.Explore(enemy.id);
        string before = JsonUtility.ToJson(session.Data);
        Assert.That(session.Act(501), Is.False);
        Assert.That(session.Buy(2001), Is.False);
        Assert.That(session.ReturnToTown(), Is.False);
        Assert.That(JsonUtility.ToJson(session.Data), Is.EqualTo(before));
    }

    [Test]
    public void TradingEquipmentAndPassiveGrowthApplyCorrectStats()
    {
        session.Data.hero.gold = 2000;
        session.Data.hero.AddExperience(700, config);
        Assert.That(session.Buy(1111), Is.True);
        int previous = session.Data.hero.weapon;
        Assert.That(session.Equip(1111), Is.True);
        Assert.That(session.Data.hero.Count(previous), Is.EqualTo(1));
        Assert.That(session.Data.hero.Count(1111), Is.Zero);
        Stats before = session.Data.hero.GetStats(config);
        Assert.That(session.Learn(103), Is.True);
        Stats after = session.Data.hero.GetStats(config);
        Assert.That(after.attack - before.attack, Is.EqualTo(7));
        Assert.That(after.hp - before.hp, Is.EqualTo(35));
        Assert.That(session.Sell(previous), Is.True);
        Assert.That(session.Data.hero.Count(previous), Is.Zero);
        session.Continue();
        Assert.That(session.Data.hero.weapon, Is.EqualTo(1111));
        Assert.That(session.Data.hero.GetStats(config).hp, Is.EqualTo(after.hp));
    }

    [Test]
    public void BossGateDeathAndSaveValidationAreEnforced()
    {
        session.Data.unlockedFloor = 5;
        session.EnterFloor(5);
        RoomData stairs = session.Data.Current.rooms.Single(x => x.kind == RoomKind.Stairs);
        stairs.revealed = true;
        Assert.That(session.Explore(stairs.id), Is.False);
        Assert.That(session.Data.currentFloor, Is.EqualTo(5));
        RoomData boss = session.Data.Current.rooms.Single(x => x.kind == RoomKind.Boss);
        boss.revealed = true;
        session.Explore(boss.id);
        Assert.That(session.Escape(), Is.False);
        session.Data.hero.hp = session.Data.battle.hero.hp = 1;
        int gold = session.Data.hero.gold;
        session.Act(-1);
        Assert.That(session.Data.inTown, Is.True);
        Assert.That(session.Data.hero.gold, Is.EqualTo(gold - gold / 10));
        Assert.That(session.Data.hero.hp, Is.EqualTo(session.Data.hero.GetStats(config).hp));
        File.WriteAllText(session.Store.Path, "{\"version\":1}");
        Assert.Throws<InvalidDataException>(() => session.Continue());
        Assert.That(File.ReadAllText(session.Store.Path), Is.EqualTo("{\"version\":1}"));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void EveryProfessionCanCompleteCampaignUsingNormalActions(int profession)
    {
        session.Create(profession, "自动冒险者", 42 + profession);
        int defeats = 0;
        for (int step = 0; step < 5000 && !session.Data.completed; step++)
        {
            HeroData hero = session.Data.hero;
            Stats stats = hero.GetStats(config);
            if (session.Data.inTown)
            {
                session.Rest();
                foreach (SkillDef skill in config.Skills.Where(x => x.id < 900 && x.profession == profession && x.level <= hero.level && !hero.skills.Contains(x.id)))
                {
                    if (hero.gold >= skill.price)
                    {
                        session.Learn(skill.id);
                    }
                }
                bool magical = profession >= 3;
                foreach (ItemKind kind in new[] { ItemKind.Weapon, ItemKind.Armor, ItemKind.Accessory })
                {
                    ItemDef best = config.Items.Where(x => x.kind == kind && x.level <= hero.level && (kind != ItemKind.Weapon || (magical ? x.bonus.magic > 0 : x.bonus.attack > 0))).OrderByDescending(x => x.level).ThenByDescending(x => magical ? x.bonus.magic : x.bonus.attack).FirstOrDefault();
                    if (best == null)
                    {
                        continue;
                    }
                    int equipped = kind == ItemKind.Weapon ? hero.weapon : kind == ItemKind.Armor ? hero.armor : hero.accessory;
                    if (equipped != best.id && hero.gold >= best.price + 30)
                    {
                        session.Buy(best.id);
                        session.Equip(best.id);
                    }
                }
                session.EnterFloor(1);
                continue;
            }
            if (session.Data.battle != null)
            {
                BattleData battle = session.Data.battle;
                List<SkillDef> usable = hero.skills.Select(config.Skill).Where(x => x.kind != SkillKind.Passive && session.SkillCost(x) <= hero.mp).ToList();
                SkillDef healing = usable.FirstOrDefault(x => x.kind == SkillKind.Heal);
                if (hero.hp < stats.hp * 0.5 && healing != null)
                {
                    session.Act(healing.id);
                }
                else if (hero.hp < stats.hp * 0.45 && hero.Count(2001) > 0)
                {
                    session.UsePotion(2001);
                }
                else if (hero.mp < 8 && hero.Count(2002) > 0)
                {
                    session.UsePotion(2002);
                }
                else
                {
                    SkillDef action = usable.Where(x => x.kind != SkillKind.Heal && x.kind != SkillKind.Shield).OrderByDescending(x => x.power + (x.kind == SkillKind.Stun ? 55 : (x.kind == SkillKind.Poison && battle.enemy.poisonTurns == 0) || (x.kind == SkillKind.Bleed && battle.enemy.bleedTurns == 0) ? 100 : 0)).FirstOrDefault();
                    session.Act(action == null ? 0 : action.id);
                }
                if (session.Data.inTown)
                {
                    defeats++;
                }
                continue;
            }
            if (hero.hp < stats.hp * 0.55 || (hero.mp < stats.mp * 0.2 && profession >= 3))
            {
                session.ReturnToTown();
                continue;
            }
            List<RoomData> available = session.Data.Current.rooms.Where(x => x.revealed && !x.cleared && !x.IsObstacle).ToList();
            RoomData room = available.OrderBy(x => x.kind == RoomKind.Stairs && session.Data.currentFloor < session.Data.unlockedFloor ? -1 : x.kind == RoomKind.Shrine ? 0 : x.kind == RoomKind.Chest ? 1 : x.kind == RoomKind.Merchant ? 2 : x.kind == RoomKind.Spring ? (hero.hp < stats.hp * 0.8 || hero.mp < stats.mp * 0.7 ? 0 : 8) : x.kind == RoomKind.Stairs ? 10 : x.kind == RoomKind.Boss ? 9 : 5).FirstOrDefault();
            Assert.That(room, Is.Not.Null, "探索不能陷入无可交互房间的死局");
            if (room.kind == RoomKind.Merchant)
            {
                session.PassMerchant(room.id);
            }
            else
            {
                session.Explore(room.id);
                if (room.kind == RoomKind.Shrine)
                {
                    session.ChooseReward(room.id, 0);
                }
            }
            if (step % 50 == 0)
            {
                session.Continue();
            }
        }
        Assert.That(session.Data.completed, Is.True, $"{config.Profession(profession).name}未通关，抵达{session.Data.currentFloor}层，等级{session.Data.hero.level}，战败{defeats}次");
        Assert.That(defeats, Is.LessThan(8), "正常补给策略不应反复战败");
        session.Continue();
        Assert.That(session.Data.completed, Is.True);
        Assert.That(session.Data.Current.BossCleared, Is.True);
        TestContext.WriteLine($"{config.Profession(profession).name}通关：等级{session.Data.hero.level}，战败{defeats}次，金币{session.Data.hero.gold}");
    }

    [Test]
    public void SpellUsesMagicAndStunInterruptsEnemyAction()
    {
        session.Create(4, "法师", 123);
        session.Data.hero.AddExperience(60, config);
        Assert.That(session.Learn(502), Is.True);
        session.EnterFloor(1);
        RoomData room = session.Data.Current.rooms.First(x => x.kind == RoomKind.Monster);
        room.monster = 2;
        room.revealed = true;
        session.Explore(room.id);
        int heroHp = session.Data.hero.hp;
        int enemyHp = session.Data.battle.enemy.hp;
        Assert.That(session.Act(502), Is.True);
        Assert.That(enemyHp - session.Data.battle.enemy.hp, Is.GreaterThan(25), "伤害应取法术攻击，不能取低物理攻击");
        Assert.That(session.Data.hero.hp, Is.EqualTo(heroHp), "先手控制应打断敌人行动");
        Assert.That(session.Data.battle.enemy.stun, Is.Zero, "控制消耗一次行动后结束");
    }

    [Test]
    public void PoisonSurvivesSaveAndExpiresByEnemyTurns()
    {
        session.Create(1, "游侠", 123);
        session.EnterFloor(1);
        RoomData room = session.Data.Current.rooms.First(x => x.kind == RoomKind.Monster);
        room.monster = 2;
        room.revealed = true;
        session.Explore(room.id);
        session.Act(201);
        Assert.That(session.Data.battle.enemy.poisonTurns, Is.EqualTo(3));
        session.Continue();
        Assert.That(session.Data.battle.enemy.poisonTurns, Is.EqualTo(3));
        int hp = session.Data.battle.enemy.hp;
        session.Act(-1);
        Assert.That(session.Data.battle.enemy.poisonTurns, Is.EqualTo(2));
        Assert.That(session.Data.battle.enemy.hp, Is.LessThan(hp));
        session.Act(-1);
        session.Act(-1);
        Assert.That(session.Data.battle.enemy.poisonTurns, Is.Zero);
        int after = session.Data.battle.enemy.hp;
        session.Act(-1);
        Assert.That(session.Data.battle.enemy.hp, Is.EqualTo(after), "中毒到期后不再扣血");
    }

    [Test]
    public void ShieldAbsorbsDamageAndHealingUsesMana()
    {
        session.Create(2, "骑士", 123);
        session.EnterFloor(1);
        RoomData room = session.Data.Current.rooms.First(x => x.kind == RoomKind.Monster);
        room.monster = 2;
        room.revealed = true;
        session.Explore(room.id);
        int hp = session.Data.hero.hp;
        session.Act(301);
        Assert.That(session.Data.hero.hp, Is.EqualTo(hp));
        Assert.That(session.Data.battle.hero.shield, Is.GreaterThan(0));

        session.Create(3, "牧师", 123);
        session.Data.hero.AddExperience(60, config);
        Assert.That(session.Learn(402), Is.True);
        session.Data.hero.hp = 40;
        session.EnterFloor(1);
        room = session.Data.Current.rooms.First(x => x.kind == RoomKind.Monster);
        room.monster = 2;
        room.revealed = true;
        session.Explore(room.id);
        int mana = session.Data.hero.mp;
        session.Act(402);
        Assert.That(session.Data.hero.hp, Is.GreaterThan(40));
        Assert.That(session.Data.hero.mp, Is.EqualTo(mana - session.SkillCost(config.Skill(402))));
    }

    [Test]
    public void CrossProfessionSkillsAndPassivesPersistTogether()
    {
        Assert.That(session.Learn(501), Is.True, "战士可以研习法师初始技能");
        Assert.That(session.Learn(301), Is.True, "战士可以研习骑士护盾");
        session.Data.hero.gold = 500;
        session.Data.hero.AddExperience(700, config);
        Stats before = session.Data.hero.GetStats(config);
        Assert.That(session.Learn(503), Is.True, "满足等级和前置技能后，可以跨职业研习被动");
        Assert.That(session.Data.hero.GetStats(config).magic, Is.EqualTo(before.magic + 10));
        session.Continue();
        Assert.That(session.Data.hero.profession, Is.Zero);
        CollectionAssert.IsSubsetOf(new[] { 501, 301, 503 }, session.Data.hero.skills);
        session.EnterFloor(1);
        RoomData room = session.Data.Current.rooms.First(x => x.kind == RoomKind.Monster || x.kind == RoomKind.Elite);
        room.revealed = true;
        session.Explore(room.id);
        int mana = session.Data.hero.mp;
        session.Act(501);
        Assert.That(session.Data.hero.mp, Is.LessThan(mana));
    }
}
