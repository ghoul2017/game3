using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public static class DungeonGenerator
    {
        public static FloorData Generate(GameConfig config, int seed, int number)
        {
            FloorDef definition = config.Floor(number);
            Random random = new Random(unchecked(seed * 397 ^ number * 7919));
            FloorData floor = new FloorData { number = number };
            int[,] cells = new int[definition.width, definition.height];
            RoomData entrance = new RoomData
            {
                x = (definition.width - 2) / 2,
                y = (definition.height - 2) / 2,
                width = 2,
                height = 2
            };
            AddRoom(floor, cells, entrance, false);
            ReserveVoid(cells, definition, entrance, random);
            int area = (definition.width - 2) * (definition.height - 2);
            int contentArea = area * random.Next(definition.contentMinPercent, definition.contentMaxPercent + 1) / 100;
            Grow(floor, cells, ContentShapes, contentArea - 4, false, definition, random);
            List<RoomData> contentRooms = floor.rooms.ToList();
            if (contentRooms.Count < 10)
            {
                throw new InvalidOperationException("[地牢] 可用空间不足以容纳楼层内容");
            }
            int obstacleArea = area * random.Next(definition.obstacleMinPercent, definition.obstacleMaxPercent + 1) / 100;
            Grow(floor, cells, ObstacleShapes, obstacleArea, true, definition, random);
            Dictionary<int, int> distances = new Dictionary<int, int> { [entrance.id] = 0 };
            Queue<RoomData> queue = new Queue<RoomData>();
            queue.Enqueue(entrance);
            while (queue.Count > 0)
            {
                RoomData current = queue.Dequeue();
                foreach (RoomData room in contentRooms.Where(current.Adjacent))
                {
                    if (!distances.ContainsKey(room.id))
                    {
                        distances.Add(room.id, distances[current.id] + 1);
                        queue.Enqueue(room);
                    }
                }
            }
            if (distances.Count != contentRooms.Count)
            {
                throw new InvalidOperationException("[地牢] 功能区块生成后不连通");
            }
            List<RoomData> ordered = contentRooms.OrderByDescending(x => distances[x.id]).ToList();
            List<MonsterDef> monsters = config.Monsters.Where(x => !x.boss && x.minFloor <= number && x.maxFloor >= number).OrderBy(x => x.spawnRank).ThenBy(x => x.id).ToList();
            foreach (RoomData room in contentRooms)
            {
                int roll = random.Next(100);
                room.kind = roll < definition.elitePercent ? RoomKind.Elite : roll < definition.monsterPercent + definition.elitePercent ? RoomKind.Monster : roll < 74 ? RoomKind.Chest : roll < 88 ? RoomKind.Spring : roll < 95 ? RoomKind.Shrine : RoomKind.Merchant;
                int[] weights = Enumerable.Range(0, monsters.Count).Select(i =>
                    (4 - room.Tier) * (monsters.Count - i) * (monsters.Count - i) + (room.Tier - 1) * (i + 1) * (i + 1)).ToArray();
                int monsterRoll = random.Next(weights.Sum());
                for (int i = 0; i < monsters.Count; i++)
                {
                    monsterRoll -= weights[i];
                    if (monsterRoll < 0)
                    {
                        room.monster = monsters[i].id;
                        break;
                    }
                }
                room.reward = definition.chestGold + random.Next(definition.chestGold / 2 + 1);
            }
            entrance.kind = RoomKind.Entrance;
            entrance.cleared = true;
            ordered[0].kind = RoomKind.Stairs;
            if (definition.boss != 0)
            {
                ordered[1].kind = RoomKind.Boss;
                ordered[1].monster = definition.boss;
            }
            ordered[2].kind = RoomKind.Spring;
            ordered[3].kind = RoomKind.Chest;
            ordered[4].kind = RoomKind.Shrine;
            ordered[5].kind = RoomKind.Merchant;
            ordered[6].kind = RoomKind.Monster;
            ordered[7].kind = RoomKind.Monster;
            floor.RevealFrom(entrance);
            return floor;
        }

        private readonly struct Shape
        {
            public readonly int width;
            public readonly int height;
            public readonly int weight;

            public Shape(int width, int height, int weight)
            {
                this.width = width;
                this.height = height;
                this.weight = weight;
            }
        }

        // 权重按区块数量计；非正方形允许横竖旋转，不通过拉伸填补剩余空间。
        private static readonly Shape[] ContentShapes =
        {
            new Shape(1, 1, 8), new Shape(1, 2, 12),
            new Shape(2, 2, 30), new Shape(2, 3, 25),
            new Shape(3, 3, 15), new Shape(3, 4, 7), new Shape(4, 4, 3)
        };
        private static readonly Shape[] ObstacleShapes =
        {
            new Shape(1, 1, 15), new Shape(1, 2, 35), new Shape(1, 3, 30), new Shape(1, 4, 20)
        };

        private static void ReserveVoid(int[,] cells, FloorDef definition, RoomData entrance, Random random)
        {
            int target = (definition.width - 2) * (definition.height - 2) * random.Next(12, 17) / 100;
            int reserved = 0;
            // 预留分散空缺，避免向外生长时把地图中心填成一整块。
            for (int attempt = 0; attempt < 200 && reserved < target; attempt++)
            {
                int width = random.Next(3, 7);
                int height = random.Next(2, 5);
                int x = random.Next(2, definition.width - width - 1);
                int y = random.Next(2, definition.height - height - 1);
                if (x < entrance.x + entrance.width + 2 && x + width > entrance.x - 2 &&
                    y < entrance.y + entrance.height + 2 && y + height > entrance.y - 2)
                {
                    continue;
                }
                for (int dx = x; dx < x + width; dx++)
                {
                    for (int dy = y; dy < y + height; dy++)
                    {
                        if (cells[dx, dy] == 0)
                        {
                            cells[dx, dy] = -1;
                            reserved++;
                        }
                    }
                }
            }
        }

        private static void Grow(FloorData floor, int[,] cells, Shape[] shapes, int targetArea, bool obstacle, FloorDef definition, Random random)
        {
            List<Shape> available = shapes.ToList();
            int usedArea = 0;
            int obstacleCount = 0;
            while (usedArea < targetArea && available.Count > 0)
            {
                int roll = random.Next(available.Sum(s => s.weight));
                Shape selected = available[0];
                foreach (Shape shape in available)
                {
                    selected = shape;
                    roll -= shape.weight;
                    if (roll < 0)
                    {
                        break;
                    }
                }
                List<RoomData> positions = Placements(cells, definition, selected.width, selected.height);
                if (selected.width != selected.height)
                {
                    positions.AddRange(Placements(cells, definition, selected.height, selected.width));
                }
                if (positions.Count == 0)
                {
                    available.Remove(selected);
                    continue;
                }
                RoomData room = positions[random.Next(positions.Count)];
                if (obstacle)
                {
                    room.kind = obstacleCount++ % 2 == 0 ? RoomKind.Wall : RoomKind.Barrier;
                    room.breakPower = room.kind == RoomKind.Wall ? definition.wallPower : definition.barrierPower;
                }
                AddRoom(floor, cells, room, obstacle);
                usedArea += room.width * room.height;
            }
        }

        private static List<RoomData> Placements(int[,] cells, FloorDef definition, int width, int height)
        {
            List<RoomData> positions = new List<RoomData>();
            for (int x = 1; x + width < definition.width; x++)
            {
                for (int y = 1; y + height < definition.height; y++)
                {
                    bool free = true;
                    for (int dx = x; dx < x + width && free; dx++)
                    {
                        for (int dy = y; dy < y + height; dy++)
                        {
                            if (cells[dx, dy] != 0)
                            {
                                free = false;
                                break;
                            }
                        }
                    }
                    if (!free)
                    {
                        continue;
                    }
                    bool adjacent = false;
                    for (int dx = x; dx < x + width && !adjacent; dx++)
                    {
                        adjacent = cells[dx, y - 1] == 1 || cells[dx, y + height] == 1;
                    }
                    for (int dy = y; dy < y + height && !adjacent; dy++)
                    {
                        adjacent = cells[x - 1, dy] == 1 || cells[x + width, dy] == 1;
                    }
                    if (adjacent)
                    {
                        positions.Add(new RoomData { x = x, y = y, width = width, height = height });
                    }
                }
            }
            return positions;
        }

        private static void AddRoom(FloorData floor, int[,] cells, RoomData room, bool obstacle)
        {
            room.id = floor.rooms.Count;
            floor.rooms.Add(room);
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    cells[x, y] = obstacle ? 2 : 1;
                }
            }
        }
    }
}
