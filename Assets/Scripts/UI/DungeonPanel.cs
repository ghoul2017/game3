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
        private const int MaxRooms = 20;
        private string pendingMessage;

        // ---- 缓存引用 ----
        private Text titleText;
        private Text statusText;
        private GameObject msgOverlay;
        private Text msgText;

        // 房间对象池
        private RoomCell[] roomCells;

        private struct RoomCell
        {
            public GameObject obj;
            public RectTransform rt;
            public Image bg;
            public Text iconText;
            public Button btn;
        }

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);

            // 标题栏
            UIBuilder.CreateTitleBar(transform, "地牢");
            titleText = transform.Find("TitleBar/Title").GetComponent<Text>();

            // 状态栏
            var bar = UIBuilder.CreatePanel(transform, 0, 45, "StatusBar");
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1);
            barRt.anchoredPosition = new Vector2(0, -65);
            barRt.sizeDelta = new Vector2(0, 45);
            bar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.9f);
            statusText = UIBuilder.CreateText(bar.transform, "", 16, UIBuilder.TextWhite, TextAnchor.MiddleCenter, name: "StatusText");
            UIBuilder.SetStretch(statusText.gameObject);

            // 地图面板
            var mapPanel = UIBuilder.CreateBorderedPanel(transform, 1400, 450, "MapPanel");
            mapPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
            var inner = mapPanel.transform.Find("Inner");

            // 创建房间对象池（最大 20 个房间）
            roomCells = new RoomCell[MaxRooms];
            for (int i = 0; i < MaxRooms; i++)
            {
                roomCells[i] = CreateRoomCell(inner, i);
                roomCells[i].obj.SetActive(false);
            }

            // 消息覆盖层（默认隐藏）
            msgOverlay = new GameObject("MsgOverlay");
            msgOverlay.transform.SetParent(inner, false);
            UIBuilder.SetStretch(msgOverlay);
            var oImg = msgOverlay.AddComponent<Image>();
            oImg.color = new Color(0, 0, 0, 0.75f);
            var overlayCg = msgOverlay.AddComponent<CanvasGroup>();
            msgOverlay.SetActive(false);

            msgText = UIBuilder.CreateText(msgOverlay.transform, "", 26, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "MsgText");
            UIBuilder.SetStretch(msgText.gameObject);

            UIBuilder.CreateButton(msgOverlay.transform, "继续", 160, 40, () =>
            {
                pendingMessage = null;
                RefreshUI();
            }, 20, "BtnContinue").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);

            // 底部按钮区
            float btnY = -220;

            UIBuilder.CreateButton(transform, "返回城镇", 200, 45, () =>
            {
                Debug.Log("[操作] 返回城镇");
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
        }

        private RoomCell CreateRoomCell(Transform parent, int index)
        {
            var obj = new GameObject($"Room_{index}");
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(RoomBtnSize, RoomBtnSize);

            var bg = obj.AddComponent<Image>();
            bg.color = Color.gray;

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(obj.transform, false);
            UIBuilder.SetStretch(iconObj);
            var iconText = iconObj.AddComponent<Text>();
            iconText.fontSize = 24;
            iconText.color = Color.white;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            iconText.raycastTarget = false;

            var btn = obj.AddComponent<Button>();
            int capturedIndex = index;
            btn.onClick.AddListener(() => OnClickRoom(capturedIndex));

            return new RoomCell
            {
                obj = obj, rt = rt, bg = bg,
                iconText = iconText, btn = btn
            };
        }

        public override void OnShow()
        {
            RefreshUI();
        }

        /// <summary>
        /// 零销毁刷新：只修改属性
        /// </summary>
        private void RefreshUI()
        {
            var dungeon = GameManager.Instance.Dungeon;
            var player = GameManager.Instance.Player;
            if (dungeon == null) return;

            // 标题
            titleText.text = $"地牢 第{dungeon.floor} 层";

            // 状态栏
            statusText.text = $"HP:{player.hp}/{player.maxHp}  MP:{player.mp}/{player.maxMp}  攻击:{player.attack}  防御:{player.defense}  金币:{player.gold}";

            // 房间地图
            UpdateRoomMap(dungeon);

            // 消息覆盖
            if (!string.IsNullOrEmpty(pendingMessage))
            {
                msgText.text = pendingMessage;
                msgOverlay.SetActive(true);
            }
            else
            {
                msgOverlay.SetActive(false);
            }
        }

        private void UpdateRoomMap(DungeonData dungeon)
        {
            int total = dungeon.rooms.Count;
            int cols = Mathf.Min(total, 10);
            int rows = Mathf.CeilToInt((float)total / cols);
            float totalWidth = cols * (RoomBtnSize + RoomGap) - RoomGap;
            float totalHeight = rows * (RoomBtnSize + RoomGap) - RoomGap;
            float offsetX = -totalWidth / 2 + RoomBtnSize / 2;
            float offsetY = totalHeight / 2 - RoomBtnSize / 2;

            for (int i = 0; i < MaxRooms; i++)
            {
                var cell = roomCells[i];
                if (i < total)
                {
                    var room = dungeon.rooms[i];
                    cell.obj.SetActive(true);

                    int col = i % cols;
                    int row = i / cols;
                    cell.rt.anchoredPosition = new Vector2(
                        offsetX + col * (RoomBtnSize + RoomGap),
                        offsetY - row * (RoomBtnSize + RoomGap));

                    cell.bg.color = GetRoomColor(room, i == dungeon.currentRoomIndex);
                    cell.iconText.text = GetRoomIcon(room);

                    // 只有当前房间和下一房间可交互
                    cell.btn.interactable = (i == dungeon.currentRoomIndex || i == dungeon.currentRoomIndex + 1);
                }
                else
                {
                    cell.obj.SetActive(false);
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
            Debug.Log($"[操作] 点击房间 index={index} 当前={GameManager.Instance.Dungeon.currentRoomIndex}");
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
                    Debug.Log($"[地牢] 遭遇战斗 type={room.type}");
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
                    Debug.Log("[地牢] 发现宝箱");
                    HandleTreasure(room);
                    break;
                case DungeonEventType.Spring:
                    Debug.Log("[地牢] 发现泉水");
                    HandleSpring(room);
                    break;
                case DungeonEventType.Merchant:
                    Debug.Log("[地牢] 遇到商人");
                    UIManager.Instance.ShowPanel<ShopPanel>();
                    break;
                case DungeonEventType.Exit:
                    Debug.Log("[地牢] 进入下一层");
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
            pendingMessage = $"发现宝箱！获得 {goldFound} 金币";
            var consumables = ConfigManager.Instance.GetItemsByType(ItemType.Consumable);
            if (consumables.Count > 0 && Random.value < 0.5f && player.bagItems.Count < player.maxBagSize)
            {
                var item = consumables[Random.Range(0, consumables.Count)];
                player.bagItems.Add(item.id);
                pendingMessage += $"\n获得物品：{item.name}";
            }
            room.cleared = true;
        }

        private void HandleSpring(DungeonRoom room)
        {
            var player = GameManager.Instance.Player;
            int hpHeal = player.maxHp / 3;
            int mpHeal = player.maxMp / 3;
            player.hp = Mathf.Min(player.hp + hpHeal, player.maxHp);
            player.mp = Mathf.Min(player.mp + mpHeal, player.maxMp);
            pendingMessage = $"发现泉水！\n恢复 {hpHeal} 生命，{mpHeal} 法力";
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
            // 直接刷新，不销毁重建
            RefreshUI();
        }
    }
}
