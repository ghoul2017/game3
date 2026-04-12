using Game.Battle;
using Game.Core;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class DungeonPanel : UIPanel
    {
        private const int RoomBtnSize = 70;
        private const int RoomGap = 10;

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            BuildUI();
        }

        public override void OnShow()
        {
            RefreshUI();
        }

        private void BuildUI()
        {
            UIBuilder.CreateTitleBar(transform, $"地牢 第{GameManager.Instance.Dungeon?.floor ?? 1} 层");

            // 房间地图区域
            var mapPanel = UIBuilder.CreateBorderedPanel(transform, 1400, 450, "MapPanel");
            var mapRt = mapPanel.GetComponent<RectTransform>();
            mapRt.anchoredPosition = new Vector2(0, 80);

            // 底部按钮区
            float btnY = -220;

            UIBuilder.CreateButton(transform, "返回城镇", 200, 45, () =>
            {
                GameManager.Instance.Dungeon.OnReturnTown();
                GameManager.Instance.Player.dungeonFloor = GameManager.Instance.Dungeon.floor;
                GameManager.Instance.SetState(GameState.InTown);
                GameManager.Instance.SaveGame();
                UIManager.Instance.ReplacePanel<TownPanel>();
            }, 22, "BtnBack").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, btnY);

            UIBuilder.CreateButton(transform, "背包", 140, 40, () =>
            {
                UIManager.Instance.ShowPanel<BagPanel>();
            }, 20, "BtnBag").GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, btnY);

            UIBuilder.CreateButton(transform, "角色", 140, 40, () =>
            {
                UIManager.Instance.ShowPanel<RoleInfoPanel>();
            }, 20, "BtnRole").GetComponent<RectTransform>().anchoredPosition = new Vector2(200, btnY);

            BuildStatusBar();
        }

        private void BuildStatusBar()
        {
            var bar = UIBuilder.CreatePanel(transform, 0, 45, "StatusBar");
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -65);
            rt.sizeDelta = new Vector2(0, 45);
            bar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.9f);
        }

        private void RefreshUI()
        {
            var dungeon = GameManager.Instance.Dungeon;
            var player = GameManager.Instance.Player;
            if (dungeon == null) return;

            // 更新标题
            var titleText = transform.Find("TitleBar/Title");
            if (titleText != null)
                titleText.GetComponent<Text>().text = $"地牢 第{dungeon.floor} 层";

            // 更新地图
            var mapPanel = transform.Find("MapPanel");
            if (mapPanel != null) UpdateMap(mapPanel);

            // 更新状态栏
            var statusBar = transform.Find("StatusBar");
            if (statusBar != null)
            {
                for (int i = statusBar.childCount - 1; i >= 0; i--)
                    Object.Destroy(statusBar.GetChild(i).gameObject);
                string status = $"HP:{player.hp}/{player.maxHp}  MP:{player.mp}/{player.maxMp}  攻击:{player.attack}  防御:{player.defense}  金币:{player.gold}";
                var t = UIBuilder.CreateText(statusBar, status, 16, UIBuilder.TextWhite, TextAnchor.MiddleCenter);
                UIBuilder.SetStretch(t.gameObject);
            }
        }

        private void UpdateMap(Transform mapPanel)
        {
            var dungeon = GameManager.Instance.Dungeon;
            var inner = mapPanel.Find("Inner");
            if (inner == null) return;

            for (int i = inner.childCount - 1; i >= 0; i--)
                Object.Destroy(inner.GetChild(i).gameObject);

            int total = dungeon.rooms.Count;
            int cols = Mathf.Min(total, 10);
            int rows = Mathf.CeilToInt((float)total / cols);
            float totalWidth = cols * (RoomBtnSize + RoomGap) - RoomGap;
            float totalHeight = rows * (RoomBtnSize + RoomGap) - RoomGap;
            float offsetX = -totalWidth / 2 + RoomBtnSize / 2;
            float offsetY = totalHeight / 2 - RoomBtnSize / 2;

            for (int i = 0; i < total; i++)
            {
                var room = dungeon.rooms[i];
                int col = i % cols;
                int row = i / cols;
                float x = offsetX + col * (RoomBtnSize + RoomGap);
                float y = offsetY - row * (RoomBtnSize + RoomGap);

                var roomObj = new GameObject($"Room_{i}");
                roomObj.transform.SetParent(inner, false);
                var rt = roomObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(RoomBtnSize, RoomBtnSize);
                rt.anchoredPosition = new Vector2(x, y);

                var img = roomObj.AddComponent<Image>();
                img.color = GetRoomColor(room, i == dungeon.currentRoomIndex);

                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(roomObj.transform, false);
                var iconRt = iconObj.AddComponent<RectTransform>();
                UIBuilder.SetStretch(iconObj);
                var iconText = iconObj.AddComponent<Text>();
                iconText.text = GetRoomIcon(room);
                iconText.fontSize = 24;
                iconText.color = Color.white;
                iconText.alignment = TextAnchor.MiddleCenter;
                iconText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);

                // 当前房间或下一房间可点击
                if (i == dungeon.currentRoomIndex || i == dungeon.currentRoomIndex + 1)
                {
                    var btn = roomObj.AddComponent<Button>();
                    int capturedIndex = i;
                    btn.onClick.AddListener(() => OnClickRoom(capturedIndex));
                }
            }
        }

        private Color GetRoomColor(DungeonRoom room, bool isCurrent)
        {
            if (isCurrent) return new Color(0.3f, 0.6f, 0.9f, 0.9f);
            if (room.cleared) return new Color(0.2f, 0.2f, 0.2f, 0.6f);
            return room.type switch
            {
                DungeonEventType.Monster => new Color(0.7f, 0.2f, 0.2f, 0.8f),
                DungeonEventType.Elite => new Color(0.8f, 0.1f, 0.5f, 0.8f),
                DungeonEventType.Boss => new Color(0.9f, 0.1f, 0.1f, 0.9f),
                DungeonEventType.Treasure => new Color(0.8f, 0.7f, 0.1f, 0.8f),
                DungeonEventType.Spring => new Color(0.1f, 0.6f, 0.3f, 0.8f),
                DungeonEventType.Merchant => new Color(0.6f, 0.4f, 0.1f, 0.8f),
                DungeonEventType.Exit => new Color(0.2f, 0.4f, 0.8f, 0.9f),
                _ => new Color(0.3f, 0.3f, 0.4f, 0.7f)
            };
        }

        private string GetRoomIcon(DungeonRoom room)
        {
            return room.type switch
            {
                DungeonEventType.Monster => "怪",
                DungeonEventType.Elite => "精",
                DungeonEventType.Boss => "王",
                DungeonEventType.Treasure => "宝",
                DungeonEventType.Spring => "泉",
                DungeonEventType.Merchant => "商",
                DungeonEventType.Exit => "梯",
                _ => "○"
            };
        }

        private void OnClickRoom(int index)
        {
            var dungeon = GameManager.Instance.Dungeon;
            if (index != dungeon.currentRoomIndex && index != dungeon.currentRoomIndex + 1) return;
            if (index > dungeon.currentRoomIndex)
                dungeon.currentRoomIndex = index;

            var room = dungeon.CurrentRoom;
            if (room == null || room.cleared) { RefreshUI(); return; }

            switch (room.type)
            {
                case DungeonEventType.Monster:
                case DungeonEventType.Elite:
                case DungeonEventType.Boss:
                    var monsters = ConfigManager.Instance.GetMonstersByFloor(dungeon.floor);
                    var monsterCfg = room.type == DungeonEventType.Boss
                        ? ConfigManager.Instance.GetBossByFloor(dungeon.floor)
                        : monsters.Count > 0 ? monsters[Random.Range(0, monsters.Count)] : null;
                    if (monsterCfg != null)
                    {
                        var battleData = new BattleData
                        {
                            monster = new BattleMonster
                            {
                                id = monsterCfg.id, name = monsterCfg.name,
                                hp = monsterCfg.hp, maxHp = monsterCfg.hp,
                                attack = monsterCfg.attack, defense = monsterCfg.defense,
                                speed = monsterCfg.speed, skillIds = monsterCfg.skillIds,
                                expReward = monsterCfg.expReward, goldReward = monsterCfg.goldReward,
                                drops = monsterCfg.drops
                            },
                            room = room
                        };
                        BattleManager.Instance.StartBattle(battleData);
                        UIManager.Instance.ShowPanel<BattlePanel>();
                    }
                    break;

                case DungeonEventType.Treasure:
                    HandleTreasure(room);
                    break;
                case DungeonEventType.Spring:
                    HandleSpring(room);
                    break;
                case DungeonEventType.Merchant:
                    UIManager.Instance.ShowPanel<ShopPanel>();
                    break;
                case DungeonEventType.Exit:
                    HandleExit();
                    break;
            }
            RefreshUI();
        }

        private void HandleTreasure(DungeonRoom room)
        {
            var player = GameManager.Instance.Player;
            int goldFound = Random.Range(10, 30) * GameManager.Instance.Dungeon.floor;
            player.gold += goldFound;
            var consumables = ConfigManager.Instance.GetItemsByType(ItemType.Consumable);
            if (consumables.Count > 0 && Random.value < 0.5f && player.bagItems.Count < player.maxBagSize)
            {
                var item = consumables[Random.Range(0, consumables.Count)];
                player.bagItems.Add(item.id);
            }
            room.cleared = true;
        }

        private void HandleSpring(DungeonRoom room)
        {
            var player = GameManager.Instance.Player;
            player.hp = Mathf.Min(player.hp + player.maxHp / 3, player.maxHp);
            player.mp = Mathf.Min(player.mp + player.maxMp / 3, player.maxMp);
            room.cleared = true;
        }

        private void HandleExit()
        {
            var player = GameManager.Instance.Player;
            player.dungeonFloor = GameManager.Instance.Dungeon.floor + 1;
            var dungeon = new DungeonData();
            dungeon.GenerateFloor(player.dungeonFloor);
            GameManager.Instance.Dungeon = dungeon;
            GameManager.Instance.SetState(GameState.InDungeon);
            for (int i = transform.childCount - 1; i >= 0; i--)
                Object.Destroy(transform.GetChild(i).gameObject);
            OnInit();
            RefreshUI();
        }
    }
}
