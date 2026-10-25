using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public sealed class AdventureSession
    {
        public GameConfig Config
        {
            get;
        }
        public AdventureData Data
        {
            get; private set;
        }
        public SaveStore Store
        {
            get;
        }
        public string Notice { get; private set; } = "";
        public string SaveError { get; private set; } = "";
        public event Action Changed;

        public AdventureSession(GameConfig config, SaveStore store)
        {
            Config = config;
            Store = store;
        }

        public void Create(int profession, string name, int seed)
        {
            ProfessionDef definition = Config.Profession(profession);
            HeroData hero = new HeroData { profession = profession, name = string.IsNullOrWhiteSpace(name) ? "旅人" : name.Trim(), weapon = definition.startingWeapon };
            hero.skills.Add(definition.startingSkill);
            hero.AddItem(2001, 3);
            hero.AddItem(2002, 2);
            Stats stats = hero.GetStats(Config);
            hero.hp = stats.hp;
            hero.mp = stats.mp;
            Data = new AdventureData { seed = seed, randomState = seed == 0 ? 1 : seed, hero = hero };
            Log("抵达余烬镇。");
            Commit("");
        }

        public void Continue()
        {
            Data = Store.Load(Config);
            SaveError = "";
            Notice = "";
            Changed?.Invoke();
        }

        public bool EnterFloor(int number)
        {
            if (Data.battle != null || !Data.inTown || number < 1 || number > Data.unlockedFloor)
            {
                return Reject("当前无法进入这个楼层。");
            }
            SetFloor(number);
            Data.inTown = false;
            Log($"进入第{number}层 · {Config.Floor(number).region}。");
            return Commit("");
        }

        private void SetFloor(int number)
        {
            if (!Data.floors.Any(x => x.number == number))
            {
                Data.floors.Add(DungeonGenerator.Generate(Config, Data.seed, number));
            }
            Data.currentFloor = number;
        }

        public bool ReturnToTown()
        {
            if (Data.inTown || Data.battle != null)
            {
                return Reject("请先结束战斗，再返回城镇。");
            }
            ReturnCore(false);
            return Commit("");
        }

        private void ReturnCore(bool defeated)
        {
            foreach (FloorData floor in Data.floors)
            {
                foreach (RoomData room in floor.rooms)
                {
                    if (room.kind == RoomKind.Monster || room.kind == RoomKind.Elite || room.kind == RoomKind.Spring)
                    {
                        room.cleared = false;
                    }
                }
            }
            Data.battle = null;
            Data.inTown = true;
            if (defeated)
            {
                int loss = Data.hero.gold / 10;
                Data.hero.gold -= loss;
                RestoreCore();
                Log($"救援队将你带回余烬镇，遗失了{loss}金币。");
            }
            else
            {
                Log("返回余烬镇。");
            }
        }

        public bool Rest()
        {
            if (!Data.inTown)
            {
                return Reject("需要在城镇旅馆休息。");
            }
            RestoreCore();
            Log("在旅馆休息，生命与法力已完全恢复。");
            return Commit("");
        }

        private void RestoreCore()
        {
            Stats stats = Data.hero.GetStats(Config);
            Data.hero.hp = stats.hp;
            Data.hero.mp = stats.mp;
        }

        public bool Explore(int roomId)
        {
            if (Data.inTown || Data.battle != null)
            {
                return Reject("当前无法探索其他房间。");
            }
            RoomData room = Data.Current.Room(roomId);
            if (!room.revealed)
            {
                return Reject("先清理相邻房间，才能揭开这里的迷雾。");
            }
            if (room.cleared)
            {
                if (room.IsObstacle)
                {
                    Data.Current.RevealFrom(room);
                    return Commit("");
                }
                return Reject("这里已经清理完毕。");
            }
            if (room.IsObstacle)
            {
                Stats power = Data.hero.GetStats(Config);
                int current = room.kind == RoomKind.Wall ? power.attack : power.magic;
                string attribute = room.kind == RoomKind.Wall ? "物攻" : "法攻";
                if (current < room.breakPower)
                {
                    return Reject($"需要{attribute}{room.breakPower}，当前{current}。");
                }
                ClearRoom(room);
                Log($"破坏了{(room.kind == RoomKind.Wall ? "墙块" : "屏障")}，相邻区域已显露。");
                return Commit("");
            }
            switch (room.kind)
            {
                case RoomKind.Monster:
                case RoomKind.Elite:
                case RoomKind.Boss:
                    StartBattle(room);
                    return Commit("");
                case RoomKind.Chest:
                    EnsureOffers(room);
                    ClearRoom(room);
                    Data.hero.gold += room.reward * room.Tier;
                    GrantReward(room.offers[0]);
                    Log($"打开{room.Tier}阶宝箱，获得{room.reward * room.Tier}金币及{RewardText(room.offers[0]).Split('\n')[0]}。");
                    return Commit("");
                case RoomKind.Spring:
                    Stats stats = Data.hero.GetStats(Config);
                    Data.hero.hp = Math.Min(stats.hp, Data.hero.hp + stats.hp / 2);
                    Data.hero.mp = Math.Min(stats.mp, Data.hero.mp + stats.mp / 2);
                    Log("饮下泉水，恢复50%最大生命与法力。");
                    break;
                case RoomKind.Shrine:
                case RoomKind.Merchant:
                    EnsureOffers(room);
                    return Commit("");
                case RoomKind.Stairs:
                    return Descend();
            }
            ClearRoom(room);
            return Commit("");
        }

        public bool PassMerchant(int roomId)
        {
            if (Data.inTown || Data.battle != null)
            {
                return Reject("当前无法和旅商交谈。");
            }
            RoomData room = Data.Current.Room(roomId);
            if (!room.revealed || room.kind != RoomKind.Merchant || room.cleared)
            {
                return Reject("这里没有可交谈的旅商。");
            }
            ClearRoom(room);
            Log("旅商为你指明了前路。");
            return Commit("");
        }

        private void EnsureOffers(RoomData room)
        {
            if (room.offers != null && room.offers.Count > 0)
            {
                return;
            }
            room.offers = new List<RewardData>();
            List<RewardData> pool = new List<RewardData>();
            foreach (ItemDef item in Config.Items.Where(x => x.level <= Math.Max(Data.hero.level, Data.currentFloor + room.Tier)))
            {
                pool.Add(new RewardData { kind = 0, id = item.id, count = item.kind == ItemKind.Potion ? room.Tier + 1 : 1, price = item.price });
            }
            foreach (RelicDef relic in Config.Relics.Where(x => !Data.hero.relics.Contains(x.id)))
            {
                pool.Add(new RewardData { kind = 1, id = relic.id, price = 35 + room.Tier * 15 });
            }
            foreach (SkillDef skill in Config.Skills.Where(x => x.id < 900 && x.level <= Data.hero.level && (x.prerequisite == 0 || Data.hero.skills.Contains(x.prerequisite)) && Data.hero.SkillRank(x.id) < 3))
            {
                pool.Add(new RewardData { kind = 2, id = skill.id, price = Math.Max(20, skill.price) });
            }
            // 每次提供不同类别，物品按区块档次优先挑选较高等级。
            for (int i = 0; i < 3; i++)
            {
                List<int> kinds = pool.Select(x => x.kind).Distinct().ToList();
                if (kinds.Count == 0)
                {
                    room.offers.Add(new RewardData { kind = 3, count = room.reward * room.Tier * (i + 1) });
                    continue;
                }
                int kind = kinds[Next(kinds.Count)];
                List<RewardData> candidates = pool.Where(x => x.kind == kind).ToList();
                if (kind == 0 && room.Tier >= 3)
                {
                    int best = candidates.Max(x => Config.Item(x.id).level);
                    candidates = candidates.Where(x => Config.Item(x.id).level == best).ToList();
                }
                RewardData reward = candidates[Next(candidates.Count)];
                if (room.kind != RoomKind.Merchant)
                {
                    reward.price = 0;
                }
                room.offers.Add(reward);
                pool.RemoveAll(x => x.kind == kind);
            }
        }

        public string RewardText(RewardData reward)
        {
            switch (reward.kind)
            {
                case 0:
                    return $"{Config.Item(reward.id).name} ×{reward.count}\n{Config.Item(reward.id).description}\n需要等级 {Config.Item(reward.id).level}";
                case 1:
                    return $"{Config.Relic(reward.id).name}\n{Config.Relic(reward.id).description}";
                case 2:
                    return $"{Config.Skill(reward.id).name}\n{(Data.hero.skills.Contains(reward.id) ? $"升级至 {Data.hero.SkillRank(reward.id) + 1} 级" : "学习技能")}\n{Config.Skill(reward.id).description}\n每次升级：主动技能威力 +20%，被动属性增加一份。";
                default:
                    return $"金币 ×{reward.count}";
            }
        }

        private void GrantReward(RewardData reward)
        {
            if (reward.kind == 0)
            {
                Data.hero.AddItem(reward.id, reward.count);
            }
            else if (reward.kind == 1)
            {
                Data.hero.relics.Add(reward.id);
            }
            else if (reward.kind == 2)
            {
                if (!Data.hero.skills.Contains(reward.id))
                {
                    Data.hero.skills.Add(reward.id);
                }
                else
                {
                    if (Data.hero.skillRanks == null)
                    {
                        Data.hero.skillRanks = new List<ItemStack>();
                    }
                    ItemStack rank = Data.hero.skillRanks.Find(x => x.id == reward.id);
                    if (rank == null)
                    {
                        Data.hero.skillRanks.Add(new ItemStack { id = reward.id, count = 1 });
                    }
                    else
                    {
                        rank.count++;
                    }
                }
            }
            else
            {
                Data.hero.gold += reward.count;
            }
        }

        public bool ChooseReward(int roomId, int index)
        {
            if (Data.inTown || Data.battle != null)
            {
                return Reject("当前无法领取奖励。");
            }
            RoomData room = Data.Current.Room(roomId);
            if (!room.revealed || room.cleared || (room.kind != RoomKind.Shrine && room.kind != RoomKind.Merchant) || room.offers == null || index < 0 || index >= room.offers.Count)
            {
                return Reject("奖励选项无效。");
            }
            RewardData reward = room.offers[index];
            if (Data.hero.gold < reward.price)
            {
                return Reject("金币不足，可以稍后回来或放弃交易。");
            }
            if (reward.kind == 1 && Data.hero.relics.Contains(reward.id))
            {
                return Reject("已经拥有这件宝物，请选择其他奖励。");
            }
            if (reward.kind == 2 && Data.hero.SkillRank(reward.id) >= 3)
            {
                return Reject("该技能已满级，请选择其他奖励。");
            }
            Data.hero.gold -= reward.price;
            ClearRoom(room);
            GrantReward(reward);
            return Commit("");
        }

        private void ClearRoom(RoomData room)
        {
            room.cleared = true;
            Data.Current.RevealFrom(room);
        }

        private bool Descend()
        {
            if (!Data.Current.BossCleared)
            {
                return Reject("首领的封印仍在，击败本层首领后才能继续。");
            }
            if (Data.currentFloor == Config.Floors.Count)
            {
                return Reject(Data.completed ? "你已打通地下城，可以返回城镇回顾这段旅程。" : "先击败最后的首领。");
            }
            int next = Data.currentFloor + 1;
            Data.unlockedFloor = Math.Max(Data.unlockedFloor, next);
            SetFloor(next);
            Log($"来到第{next}层 · {Config.Floor(next).region}。");
            return Commit("");
        }

        public bool Buy(int id, int merchantRoom = -1)
        {
            ItemDef item = Config.Item(id);
            if (Data.battle != null || item.level > Data.hero.level)
            {
                return Reject("战斗中不能购买，商品也需要满足等级要求。");
            }
            if (!Data.inTown)
            {
                return Reject("地牢旅商请从三份报价中选择一份。");
            }
            if (Data.hero.gold < item.price)
            {
                return Reject($"金币不足，需要{item.price}金币。");
            }
            Data.hero.gold -= item.price;
            Data.hero.AddItem(id, 1);
            Log($"花费{item.price}金币购买{item.name}。");
            return Commit($"已获得{item.name}。");
        }

        public bool Sell(int id)
        {
            if (!Data.inTown || Data.battle != null || Data.hero.Count(id) == 0)
            {
                return Reject("只能在城镇出售背包中的物品。");
            }
            ItemDef item = Config.Item(id);
            Data.hero.AddItem(id, -1);
            Data.hero.gold += item.price / 2;
            return Commit($"已出售{item.name}，获得{item.price / 2}金币。");
        }

        public bool Equip(int id)
        {
            ItemDef item = Config.Item(id);
            HeroData hero = Data.hero;
            if (Data.battle != null || item.kind == ItemKind.Potion || item.level > hero.level || hero.Count(id) == 0)
            {
                return Reject("无法装备：检查物品、等级和战斗状态。");
            }
            int previous = item.kind == ItemKind.Weapon ? hero.weapon : item.kind == ItemKind.Armor ? hero.armor : hero.accessory;
            hero.AddItem(id, -1);
            if (previous != 0)
            {
                hero.AddItem(previous, 1);
            }
            if (item.kind == ItemKind.Weapon)
            {
                hero.weapon = id;
            }
            else if (item.kind == ItemKind.Armor)
            {
                hero.armor = id;
            }
            else
            {
                hero.accessory = id;
            }
            Stats stats = hero.GetStats(Config);
            hero.hp = Math.Min(hero.hp, stats.hp);
            hero.mp = Math.Min(hero.mp, stats.mp);
            return Commit($"已装备{item.name}。");
        }

        public bool Learn(int id)
        {
            SkillDef skill = Config.Skill(id);
            HeroData hero = Data.hero;
            if (!Data.inTown || Data.battle != null || skill.id >= 900 || hero.skills.Contains(id))
            {
                return Reject("请在公会学习尚未掌握的冒险技能。");
            }
            if (hero.level < skill.level || hero.gold < skill.price || (skill.prerequisite != 0 && !hero.skills.Contains(skill.prerequisite)))
            {
                return Reject($"学习需要等级{skill.level}、{skill.price}金币及前置技能。");
            }
            hero.gold -= skill.price;
            hero.skills.Add(id);
            Log($"掌握了{skill.name}。{skill.description}");
            return Commit($"已学会{skill.name}。");
        }

        public bool UsePotion(int id)
        {
            ItemDef item = Config.Item(id);
            Stats stats = Data.hero.GetStats(Config);
            if (Data.hero.Count(id) == 0 || item.kind != ItemKind.Potion)
            {
                return Reject("没有可使用的这件药水。");
            }
            if ((item.heal == 0 || Data.hero.hp == stats.hp) && (item.mana == 0 || Data.hero.mp == stats.mp))
            {
                return Reject("当前无需恢复，药水没有消耗。");
            }
            if (Data.battle != null)
            {
                return Act(0, id);
            }
            Data.hero.AddItem(id, -1);
            Data.hero.hp = Math.Min(stats.hp, Data.hero.hp + item.heal);
            Data.hero.mp = Math.Min(stats.mp, Data.hero.mp + item.mana);
            return Commit($"使用了{item.name}。");
        }

        public int SkillCost(SkillDef skill)
        {
            int discount = Data.hero.relics.Sum(x => Config.Relic(x).manaDiscount);
            return Math.Max(1, skill.cost * Math.Max(0, 100 - discount) / 100);
        }

        public string EnemyIntent
        {
            get
            {
                BattleData battle = Data.battle;
                if (battle == null)
                {
                    return "";
                }
                return battle.intent == 0 ? "普通攻击" : Config.Skill(battle.intent).name;
            }
        }

        public Stats EnemyStats(RoomData room)
        {
            MonsterDef monster = Config.Monster(room.monster);
            float scale = monster.boss ? 1f : Config.Floor(Data.currentFloor).scalePercent / 100f;
            if (room.kind == RoomKind.Elite)
            {
                scale *= 1.35f;
            }
            return new Stats
            {
                hp = (int)(monster.basis.hp * scale),
                mp = monster.basis.mp,
                attack = (int)(monster.basis.attack * scale),
                magic = (int)(monster.basis.magic * scale),
                defense = (int)(monster.basis.defense * scale),
                speed = monster.basis.speed
            };
        }

        private void StartBattle(RoomData room)
        {
            MonsterDef monster = Config.Monster(room.monster);
            Stats stats = EnemyStats(room);
            Stats heroStats = Data.hero.GetStats(Config);
            Data.battle = new BattleData
            {
                room = room.id,
                monster = room.monster,
                elite = room.kind == RoomKind.Elite,
                stats = stats,
                enemy = new FighterData { hp = stats.hp, mp = stats.mp },
                hero = new FighterData
                {
                    hp = Data.hero.hp,
                    mp = Data.hero.mp,
                    shield = heroStats.hp * Data.hero.relics.Sum(x => Config.Relic(x).openingShield) / 100
                }
            };
            ChooseIntent();
            Log($"遭遇{(Data.battle.elite ? "精英 · " : "")}{monster.name}。下一步：{EnemyIntent}。");
        }

        private void ChooseIntent()
        {
            BattleData battle = Data.battle;
            MonsterDef definition = Config.Monster(battle.monster);
            List<int> available = definition.skills.Where(x => Config.Skill(x).cost <= battle.enemy.mp).ToList();
            battle.intent = available.Count > 0 && Next(100) < (definition.boss ? 75 : 50) ? available[Next(available.Count)] : 0;
        }

        public bool Act(int skillId = 0, int potionId = 0)
        {
            BattleData battle = Data.battle;
            if (battle == null || Data.inTown)
            {
                return Reject("当前没有进行中的战斗。");
            }
            SkillDef skill = null;
            if (skillId > 0)
            {
                skill = Config.Skill(skillId);
                if (!Data.hero.skills.Contains(skillId) || skill.kind == SkillKind.Passive || battle.hero.mp < SkillCost(skill))
                {
                    return Reject("技能尚未掌握、为被动技能或法力不足，本轮没有推进。");
                }
            }
            if (skillId < -1 || (potionId != 0 && skillId != 0))
            {
                return Reject("战斗行动无效。");
            }
            ItemDef potion = potionId == 0 ? null : Config.Item(potionId);
            Stats heroStats = Data.hero.GetStats(Config);
            if (potion != null && (potion.kind != ItemKind.Potion || Data.hero.Count(potionId) == 0 || ((potion.heal == 0 || battle.hero.hp == heroStats.hp) && (potion.mana == 0 || battle.hero.mp == heroStats.mp))))
            {
                return Reject("药水不存在或当前无需恢复，本轮没有推进。");
            }

            bool heroFirst = heroStats.speed >= battle.stats.speed;
            Log($"第{battle.round}轮 · {(heroFirst ? "你先行动" : "敌人先行动")}");
            for (int turn = 0; turn < 2; turn++)
            {
                bool isHero = turn == 0 ? heroFirst : !heroFirst;
                FighterData actor = isHero ? battle.hero : battle.enemy;
                FighterData target = isHero ? battle.enemy : battle.hero;
                Stats actorStats = isHero ? heroStats : battle.stats;
                Stats targetStats = isHero ? battle.stats : heroStats;
                string actorName = isHero ? "你" : Config.Monster(battle.monster).name;
                Tick(actor, actorName);
                if (BattleEnded())
                {
                    return Commit("");
                }
                if (actor.stun > 0)
                {
                    actor.stun--;
                    Log($"{actorName}受到控制，无法行动。");
                    continue;
                }
                actor.guarding = false;
                if (isHero && potion != null)
                {
                    Data.hero.AddItem(potionId, -1);
                    actor.hp = Math.Min(actorStats.hp, actor.hp + potion.heal);
                    actor.mp = Math.Min(actorStats.mp, actor.mp + potion.mana);
                    Log($"你使用了{potion.name}。");
                }
                else if (isHero && skillId == -1)
                {
                    actor.guarding = true;
                    Log("你采取防御姿态，下一次行动前受到的直接伤害降低55%。");
                }
                else
                {
                    SkillDef action = isHero ? skill : battle.intent == 0 ? null : Config.Skill(battle.intent);
                    if (action != null)
                    {
                        actor.mp -= isHero ? SkillCost(action) : action.cost;
                    }
                    ExecuteEffect(actor, target, actorStats, targetStats, action, isHero, actorName);
                }
                if (BattleEnded())
                {
                    return Commit("");
                }
            }
            battle.round++;
            SyncHero();
            ChooseIntent();
            return Commit("");
        }

        private void Tick(FighterData fighter, string name)
        {
            int damage = 0;
            if (fighter.poisonTurns > 0)
            {
                fighter.poisonTurns--;
                damage += fighter.poison;
            }
            if (fighter.bleedTurns > 0)
            {
                fighter.bleedTurns--;
                damage += fighter.bleed;
            }
            if (damage > 0)
            {
                fighter.hp = Math.Max(0, fighter.hp - damage);
                Log($"{name}因持续伤害失去{damage}生命。");
            }
        }

        private void ExecuteEffect(FighterData actor, FighterData target, Stats source, Stats defense, SkillDef skill, bool isHero, string actorName)
        {
            SkillKind kind = skill == null ? SkillKind.Strike : skill.kind;
            int power = skill == null ? 100 : skill.power;
            if (isHero && skill != null)
            {
                power = power * (100 + 20 * (Data.hero.SkillRank(skill.id) - 1)) / 100;
            }
            int attack = skill != null && skill.magical ? source.magic : source.attack;
            string actionName = skill == null ? "普通攻击" : skill.name;
            if (kind == SkillKind.Heal)
            {
                int heal = Math.Min(source.hp - actor.hp, attack * power / 100);
                actor.hp += heal;
                Log($"{actorName}施放{actionName}，恢复{heal}生命。");
                return;
            }
            if (kind == SkillKind.Shield)
            {
                int shield = attack * power / 100 + source.defense * skill.defensePower / 100;
                actor.shield += shield;
                Log($"{actorName}施放{actionName}，获得{shield}护盾。");
                return;
            }
            float mitigation = skill != null && skill.magical ? 0.25f : 0.55f;
            int baseDamage = attack * power / 100 + (skill == null ? 0 : source.defense * skill.defensePower / 100);
            int damage = Math.Max(1, (int)((baseDamage - defense.defense * mitigation) * (90 + Next(21)) / 100f));
            if (target.guarding)
            {
                damage = Math.Max(1, damage * 45 / 100);
            }
            int absorbed = Math.Min(target.shield, damage);
            target.shield -= absorbed;
            damage = Math.Min(target.hp, damage - absorbed);
            target.hp -= damage;
            Log($"{actorName}使用{actionName}，造成{damage}伤害{(absorbed > 0 ? $"（护盾吸收{absorbed}）" : "")}。");
            if (kind == SkillKind.Poison)
            {
                target.poison = Math.Max(1, attack / 4);
                target.poisonTurns = skill.duration;
            }
            if (kind == SkillKind.Bleed)
            {
                target.bleed = Math.Max(1, attack / 4);
                target.bleedTurns = skill.duration;
            }
            if (kind == SkillKind.Stun)
            {
                target.stun = Math.Max(target.stun, skill.duration);
            }
            int lifesteal = kind == SkillKind.Drain ? 40 : 0;
            if (isHero)
            {
                lifesteal += Data.hero.relics.Sum(x => Config.Relic(x).lifesteal);
            }
            int healing = Math.Min(source.hp - actor.hp, damage * lifesteal / 100);
            if (healing > 0)
            {
                actor.hp += healing;
                Log($"{actorName}汲取{healing}生命。");
            }
            if (!isHero)
            {
                int reflected = damage * Data.hero.relics.Sum(x => Config.Relic(x).thorns) / 100;
                if (reflected > 0)
                {
                    actor.hp = Math.Max(0, actor.hp - reflected);
                    Log($"荆棘反弹{reflected}伤害。");
                }
            }
        }

        public bool Escape()
        {
            BattleData battle = Data.battle;
            if (battle == null || Config.Monster(battle.monster).boss)
            {
                return Reject("首领战无法撤退，需要战胜它。");
            }
            Log("你选择撤退，敌人发起最后一次追击。");
            if (battle.enemy.stun == 0)
            {
                SkillDef action = battle.intent == 0 ? null : Config.Skill(battle.intent);
                ExecuteEffect(battle.enemy, battle.hero, battle.stats, Data.hero.GetStats(Config), action, false, Config.Monster(battle.monster).name);
            }
            SyncHero();
            if (Data.hero.hp <= 0)
            {
                ReturnCore(true);
                return Commit("撤退时倒下了，救援队已将你送回城镇。");
            }
            if (battle.enemy.hp <= 0)
            {
                Victory();
                return Commit("");
            }
            Data.battle = null;
            return Commit("");
        }

        private void SyncHero()
        {
            Data.hero.hp = Data.battle.hero.hp;
            Data.hero.mp = Data.battle.hero.mp;
        }

        private bool BattleEnded()
        {
            SyncHero();
            if (Data.hero.hp <= 0)
            {
                ReturnCore(true);
                return true;
            }
            if (Data.battle.enemy.hp <= 0)
            {
                Victory();
                return true;
            }
            return false;
        }

        private void Victory()
        {
            BattleData battle = Data.battle;
            MonsterDef monster = Config.Monster(battle.monster);
            RoomData room = Data.Current.Room(battle.room);
            int multiplier = battle.elite ? 2 : 1;
            int experience = monster.experience * multiplier * (100 + Data.hero.relics.Sum(x => Config.Relic(x).experiencePercent)) / 100;
            int gold = monster.gold * multiplier * (100 + Data.hero.relics.Sum(x => Config.Relic(x).goldPercent)) / 100;
            ItemDef drop = monster.drop != 0 && Next(100) < monster.dropChance ? Config.Item(monster.drop) : null;
            ClearRoom(room);
            Data.battle = null;
            Data.hero.gold += gold;
            int levels = Data.hero.AddExperience(experience, Config);
            if (drop != null)
            {
                Data.hero.AddItem(drop.id, 1);
            }
            Log($"击败{monster.name}！获得{experience}经验、{gold}金币{(drop == null ? "" : $"、{drop.name}")}。");
            if (levels > 0)
            {
                Log($"升至等级{Data.hero.level}，生命与法力恢复。");
            }
            if (monster.boss && Data.currentFloor == Config.Floors.Count)
            {
                Data.completed = true;
                Log("深渊守门者倒下了。晨光重返余烬镇，你完成了这段冒险。");
            }
        }

        private void Log(string message)
        {
            Data.journal.Add(message);
            if (Data.journal.Count > 80)
            {
                Data.journal.RemoveAt(0);
            }
        }

        private int Next(int maximum)
        {
            uint state = unchecked((uint)Data.randomState);
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            Data.randomState = unchecked((int)state);
            return (int)(state % (uint)maximum);
        }

        private bool Reject(string message)
        {
            Notice = message;
            Changed?.Invoke();
            return false;
        }

        private bool Commit(string message)
        {
            Notice = message;
            try
            {
                Store.Save(Data);
                SaveError = "";
            }
            catch (Exception exception)
            {
                SaveError = $"保存失败：{exception.Message}。请检查目录后点击重试保存。";
                UnityEngine.Debug.LogError($"[存档] {SaveError}");
            }
            Changed?.Invoke();
            return true;
        }

        public void RetrySave()
        {
            Commit("当前冒险已保存。");
        }
    }
}
