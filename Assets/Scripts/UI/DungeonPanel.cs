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
        private const int RoomBtnSize = 80;
        private const int RoomGap = 8;
        private const int MaxRooms = 20;
        private string pendingMessage;

        // ---- 缓存引用 ----
        private Text titleText;
        private Text statusText;
        private GameObject msgOverlay;
        private Text msgText;

        // 右侧状态栏引用
        private Slider hpSlider;
        private Text hpText;
        private Slider mpSlider;
        private Text mpText;
        private Slider expSlider;
        private Text expText;
        private Text floorText;
        private Text goldText;
        private Text statsText;

        // 底部技能栏引用
        private const int MaxSkillSlots = 4;
        private Text[] skillLabels = new Text[MaxSkillSlots];
        private Text[] skillMpLabels = new Text[MaxSkillSlots];

        // 房间对象池
        private RoomCell[] roomCells;

        private struct RoomCell
        {
            public GameObject obj;
            public RectTransform rt;
            public Image border;
            public Image bg;
            public Text iconText;
            public Text typeLabel;
            public Button btn;
        }

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);
            UIBuilder.CreateTitleBar(parent, "地牢");

            // 状态栏
            var bar = UIBuilder.CreatePanel(parent, 0, 45, "StatusBar");
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1);
            barRt.anchoredPosition = new Vector2(0, -65);
            barRt.sizeDelta = new Vector2(0, 45);
            bar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.9f);
            var statusTextObj = UIBuilder.CreateText(bar.transform, "", 16, UIBuilder.TextWhite, TextAnchor.MiddleCenter, name: "StatusText");
            UIBuilder.SetStretch(statusTextObj.gameObject);

            // 地图面板（缩窄为右侧状态栏让出空间）
            var mapPanel = UIBuilder.CreateBorderedPanel(parent, 1050, 480, "MapPanel");
            mapPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-160, 80);

            // ===== 右侧状态面板 =====
            CreateStatusPanelVisuals(parent);

            // 创建房间对象池（最大 20 个房间）
            var inner = mapPanel.transform.Find("Inner");
            for (int i = 0; i < MaxRooms; i++)
            {
                CreateRoomCellVisual(inner, i);
            }

            // 消息覆盖层（默认隐藏）
            var msgOverlay = new GameObject("MsgOverlay");
            msgOverlay.transform.SetParent(inner, false);
            UIBuilder.SetStretch(msgOverlay);
            var oImg = msgOverlay.AddComponent<Image>();
            oImg.color = new Color(0, 0, 0, 0.75f);
            msgOverlay.AddComponent<CanvasGroup>();
            msgOverlay.SetActive(false);

            var msgTextObj = UIBuilder.CreateText(msgOverlay.transform, "", 26, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "MsgText");
            UIBuilder.SetStretch(msgTextObj.gameObject);

            var (btnContinue, _) = UIBuilder.CreateButtonWithRef(msgOverlay.transform, "继续", 160, 40, null, 20, "BtnContinue");
            UIBuilder.SetAnchored(btnContinue.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -60), new Vector2(160, 40));

            // 底部技能快捷栏
            CreateSkillBarVisuals(parent);

            // 底部按钮区
            float btnY = -310;

            var (btnBack, _) = UIBuilder.CreateButtonWithRef(parent, "返回城镇", 200, 45, null, 22, "BtnBack");
            UIBuilder.SetAnchored(btnBack.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, btnY), new Vector2(200, 45));

            var (btnBag, _) = UIBuilder.CreateButtonWithRef(parent, "背包", 140, 40, null, 20, "BtnBag");
            UIBuilder.SetAnchored(btnBag.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200, btnY), new Vector2(140, 40));

            var (btnRole, _) = UIBuilder.CreateButtonWithRef(parent, "角色", 140, 40, null, 20, "BtnRole");
            UIBuilder.SetAnchored(btnRole.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(200, btnY), new Vector2(140, 40));
        }

        private static void CreateStatusPanelVisuals(Transform parent)
        {
            var panel = UIBuilder.CreateBorderedPanel(parent, 300, 480, "StatusPanel");
            UIBuilder.SetAnchored(panel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(535, 80), new Vector2(300, 480));
            var inner = panel.transform.Find("Inner");

            float y = 200;
            float sliderW = 250;
            float barH = 20;
            float rowGap = 65;

            // HP 条
            var hpLabel = UIBuilder.CreateText(inner, "HP", 16, UIBuilder.HpColor, TextAnchor.MiddleLeft, true, "HpLabel");
            UIBuilder.SetAnchored(hpLabel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-85, y), new Vector2(40, 24));
            var hpSliderObj = UIBuilder.CreateSlider(inner, sliderW, barH, UIBuilder.HpColor, "HpSlider");
            UIBuilder.SetAnchored(hpSliderObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(30, y), new Vector2(sliderW, barH));
            var hpTextObj = UIBuilder.CreateText(inner, "", 14, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "HpText");
            UIBuilder.SetAnchored(hpTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y - 20), new Vector2(sliderW, 18));

            y -= rowGap;

            // MP 条
            var mpLabel = UIBuilder.CreateText(inner, "MP", 16, UIBuilder.MpColor, TextAnchor.MiddleLeft, true, "MpLabel");
            UIBuilder.SetAnchored(mpLabel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-85, y), new Vector2(40, 24));
            var mpSliderObj = UIBuilder.CreateSlider(inner, sliderW, barH, UIBuilder.MpColor, "MpSlider");
            UIBuilder.SetAnchored(mpSliderObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(30, y), new Vector2(sliderW, barH));
            var mpTextObj = UIBuilder.CreateText(inner, "", 14, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "MpText");
            UIBuilder.SetAnchored(mpTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y - 20), new Vector2(sliderW, 18));

            y -= rowGap;

            // EXP 条
            var expLabel = UIBuilder.CreateText(inner, "EXP", 16, UIBuilder.ExpColor, TextAnchor.MiddleLeft, true, "ExpLabel");
            UIBuilder.SetAnchored(expLabel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-85, y), new Vector2(40, 24));
            var expSliderObj = UIBuilder.CreateSlider(inner, sliderW, barH, UIBuilder.ExpColor, "ExpSlider");
            UIBuilder.SetAnchored(expSliderObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(30, y), new Vector2(sliderW, barH));
            var expTextObj = UIBuilder.CreateText(inner, "", 14, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "ExpText");
            UIBuilder.SetAnchored(expTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y - 20), new Vector2(sliderW, 18));

            y -= rowGap + 10;

            // 分隔线
            var div = UIBuilder.CreateDivider(inner);
            var divRt = div.GetComponent<RectTransform>();
            divRt.anchoredPosition = new Vector2(0, y + 10);
            divRt.sizeDelta = new Vector2(260, 2);

            y -= 15;

            // 层数/金币/攻防信息
            var floorTextObj = UIBuilder.CreateText(inner, "", 18, UIBuilder.Gold, TextAnchor.MiddleLeft, true, "FloorText");
            UIBuilder.SetAnchored(floorTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y), new Vector2(260, 26));
            y -= 30;
            var goldTextObj = UIBuilder.CreateText(inner, "", 18, UIBuilder.Gold, TextAnchor.MiddleLeft, false, "GoldText");
            UIBuilder.SetAnchored(goldTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y), new Vector2(260, 26));
            y -= 30;
            var statsTextObj = UIBuilder.CreateText(inner, "", 16, UIBuilder.TextDim, TextAnchor.MiddleLeft, false, "StatsText");
            UIBuilder.SetAnchored(statsTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y), new Vector2(260, 26));
        }

        private static void CreateSkillBarVisuals(Transform parent)
        {
            float slotW = 140;
            float slotH = 50;
            float gap = 20;
            float totalW = MaxSkillSlots * slotW + (MaxSkillSlots - 1) * gap;
            float startX = -totalW / 2 + slotW / 2;
            float barY = -240;

            for (int i = 0; i < MaxSkillSlots; i++)
            {
                var slot = UIBuilder.CreateBorderedPanel(parent, slotW, slotH, $"SkillSlot_{i}");
                UIBuilder.SetAnchored(slot,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + i * (slotW + gap), barY), new Vector2(slotW, slotH));

                // 技能名称
                var nameLabel = UIBuilder.CreateText(slot.transform.Find("Inner"), "", 16, UIBuilder.TextWhite,
                    TextAnchor.MiddleCenter, false, "SkillName");
                UIBuilder.SetAnchored(nameLabel.gameObject,
                    new Vector2(0, 0.35f), new Vector2(1, 1),
                    new Vector2(0, 0), new Vector2(0, 0));

                // MP消耗标签
                var mpLabel = UIBuilder.CreateText(slot.transform.Find("Inner"), "", 12, UIBuilder.MpColor,
                    TextAnchor.MiddleCenter, false, "SkillMp");
                UIBuilder.SetAnchored(mpLabel.gameObject,
                    new Vector2(0, 0), new Vector2(1, 0.4f),
                    new Vector2(0, 0), new Vector2(0, 0));
            }
        }

        private static void CreateRoomCellVisual(Transform parent, int index)
        {
            // 外层：边框
            var obj = new GameObject($"Room_{index}");
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(RoomBtnSize, RoomBtnSize);

            var border = obj.AddComponent<Image>();
            border.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);

            // 内层：背景
            var innerObj = new GameObject("Inner");
            innerObj.transform.SetParent(obj.transform, false);
            var innerRt = innerObj.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(3, 3);
            innerRt.offsetMax = new Vector2(-3, -3);
            var bg = innerObj.AddComponent<Image>();
            bg.color = Color.gray;

            // 图标
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(innerObj.transform, false);
            var iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.35f);
            iconRt.anchorMax = new Vector2(1, 1);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var iconText = iconObj.AddComponent<Text>();
            iconText.fontSize = 26;
            iconText.color = Color.white;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.font = Font.CreateDynamicFontFromOSFont("Arial", 26);
            iconText.raycastTarget = false;

            // 类型标签
            var labelObj = new GameObject("TypeLabel");
            labelObj.transform.SetParent(innerObj.transform, false);
            var labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 0.35f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var typeLabel = labelObj.AddComponent<Text>();
            typeLabel.fontSize = 11;
            typeLabel.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            typeLabel.alignment = TextAnchor.MiddleCenter;
            typeLabel.font = Font.CreateDynamicFontFromOSFont("Arial", 11);
            typeLabel.raycastTarget = false;

            obj.AddComponent<Button>();
        }

        protected override void BindReferences()
        {
            titleText = FindComponent<Text>("TitleBar/Title");
            statusText = FindComponent<Text>("StatusBar/StatusText");

            // 地图面板房间池
            roomCells = new RoomCell[MaxRooms];
            for (int i = 0; i < MaxRooms; i++)
            {
                string path = $"MapPanel/Inner/Room_{i}";
                var cellObj = transform.Find(path).gameObject;
                roomCells[i] = new RoomCell
                {
                    obj = cellObj,
                    rt = cellObj.GetComponent<RectTransform>(),
                    border = cellObj.GetComponent<Image>(),
                    bg = cellObj.transform.Find("Inner").GetComponent<Image>(),
                    iconText = cellObj.transform.Find("Inner/Icon").GetComponent<Text>(),
                    typeLabel = cellObj.transform.Find("Inner/TypeLabel").GetComponent<Text>(),
                    btn = cellObj.GetComponent<Button>()
                };
                cellObj.SetActive(false);
            }

            // 消息覆盖
            msgOverlay = transform.Find("MapPanel/Inner/MsgOverlay").gameObject;
            msgText = FindComponent<Text>("MapPanel/Inner/MsgOverlay/MsgText");

            // 右侧状态面板
            hpSlider = FindComponent<Slider>("StatusPanel/Inner/HpSlider");
            hpText = FindComponent<Text>("StatusPanel/Inner/HpText");
            mpSlider = FindComponent<Slider>("StatusPanel/Inner/MpSlider");
            mpText = FindComponent<Text>("StatusPanel/Inner/MpText");
            expSlider = FindComponent<Slider>("StatusPanel/Inner/ExpSlider");
            expText = FindComponent<Text>("StatusPanel/Inner/ExpText");
            floorText = FindComponent<Text>("StatusPanel/Inner/FloorText");
            goldText = FindComponent<Text>("StatusPanel/Inner/GoldText");
            statsText = FindComponent<Text>("StatusPanel/Inner/StatsText");

            // 底部技能栏
            for (int i = 0; i < MaxSkillSlots; i++)
            {
                skillLabels[i] = FindComponent<Text>($"SkillSlot_{i}/Inner/SkillName");
                skillMpLabels[i] = FindComponent<Text>($"SkillSlot_{i}/Inner/SkillMp");
            }
        }

        protected override void BindEvents()
        {
            for (int i = 0; i < MaxRooms; i++)
            {
                int capturedIndex = i;
                roomCells[i].btn.onClick.AddListener(() => OnClickRoom(capturedIndex));
            }

            FindComponent<Button>("MapPanel/Inner/MsgOverlay/BtnContinue").onClick.AddListener(() =>
            {
                pendingMessage = null;
                RefreshUI();
            });

            FindComponent<Button>("BtnBack").onClick.AddListener(() =>
            {
                Debug.Log("[操作] 返回城镇");
                GameManager.Instance.Dungeon.OnReturnTown();
                GameManager.Instance.Player.dungeonFloor = GameManager.Instance.Dungeon.floor;
                GameManager.Instance.SetState(GameState.InTown);
                GameManager.Instance.SaveGame();
                UIManager.Instance.ReplacePanel<TownPanel>();
            });

            FindComponent<Button>("BtnBag").onClick.AddListener(() =>
            {
                UIManager.Instance.ShowPanel<BagPanel>();
            });

            FindComponent<Button>("BtnRole").onClick.AddListener(() =>
            {
                UIManager.Instance.ShowPanel<RoleInfoPanel>();
            });
        }

        private void UpdateSkillBar(PlayerData player)
        {
            for (int i = 0; i < MaxSkillSlots; i++)
            {
                if (i < player.equippedSkills.Count)
                {
                    var skillCfg = ConfigManager.Instance.GetSkill(player.equippedSkills[i]);
                    if (skillCfg != null)
                    {
                        skillLabels[i].text = skillCfg.name;
                        skillMpLabels[i].text = $"MP {skillCfg.mpCost}";
                    }
                    else
                    {
                        skillLabels[i].text = "???";
                        skillMpLabels[i].text = "";
                    }
                }
                else
                {
                    skillLabels[i].text = "空";
                    skillMpLabels[i].text = "";
                }
            }
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

            // 顶部状态栏（简要）
            statusText.text = $"房间 {dungeon.currentRoomIndex + 1}/{dungeon.rooms.Count}";

            // 右侧状态面板
            hpSlider.value = player.maxHp > 0 ? (float)player.hp / player.maxHp : 0;
            hpText.text = $"HP  {player.hp}/{player.maxHp}";
            mpSlider.value = player.maxMp > 0 ? (float)player.mp / player.maxMp : 0;
            mpText.text = $"MP  {player.mp}/{player.maxMp}";

            int expNeeded = player.level * 100;
            expSlider.value = expNeeded > 0 ? (float)player.exp / expNeeded : 0;
            expText.text = $"Lv.{player.level}  {player.exp}/{expNeeded}";

            floorText.text = $"第 {dungeon.floor} 层";
            goldText.text = $"金币: {player.gold}";
            statsText.text = $"攻击: {player.attack}  防御: {player.defense}";

            // 底部技能栏
            UpdateSkillBar(player);

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
                    bool isCurrent = i == dungeon.currentRoomIndex;
                    cell.obj.SetActive(true);

                    int col = i % cols;
                    int row = i / cols;
                    cell.rt.anchoredPosition = new Vector2(
                        offsetX + col * (RoomBtnSize + RoomGap),
                        offsetY - row * (RoomBtnSize + RoomGap));

                    cell.bg.color = GetRoomColor(room, isCurrent);
                    cell.border.color = isCurrent ? UIBuilder.Gold : new Color(0.3f, 0.3f, 0.3f, 0.6f);
                    cell.iconText.text = GetRoomIcon(room);
                    cell.typeLabel.text = GetRoomTypeName(room, isCurrent);

                    // 只有当前房间和下一房间可交互
                    cell.btn.interactable = (isCurrent || i == dungeon.currentRoomIndex + 1);
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

        private string GetRoomTypeName(DungeonRoom room, bool isCurrent)
        {
            if (isCurrent) return "当前";
            if (room.cleared) return "已清";
            return room.type switch
            {
                DungeonEventType.Monster => "怪物",
                DungeonEventType.Elite => "精英",
                DungeonEventType.Boss => "BOSS",
                DungeonEventType.Treasure => "宝箱",
                DungeonEventType.Spring => "泉水",
                DungeonEventType.Merchant => "商人",
                DungeonEventType.Exit => "出口",
                _ => ""
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
                                drops = monsterCfg.drops,
                                isBoss = monsterCfg.isBoss,
                                level = monsterCfg.minFloor * 2 + 1
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

            // 40% 几率获得宝物遗物
            var allTreasures = ConfigManager.Instance.GetAllTreasures();
            var available = allTreasures.FindAll(t => !player.treasures.Contains(t.id));
            if (available.Count > 0 && Random.value < 0.4f)
            {
                var treasure = available[Random.Range(0, available.Count)];
                player.treasures.Add(treasure.id);
                player.RecalcStats();
                pendingMessage += $"\n获得宝物：{treasure.name} — {treasure.desc}";
            }

            // 30% 几率获得消耗品
            var consumables = ConfigManager.Instance.GetItemsByType(ItemType.Consumable);
            if (consumables.Count > 0 && Random.value < 0.3f && player.bagItems.Count < player.maxBagSize)
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
