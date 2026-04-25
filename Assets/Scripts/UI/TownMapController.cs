using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Core;
using Game.Data;

namespace Game.UI
{
    /// <summary>
    /// 城镇网格地图 — 5x4固定布局，4个建筑可点击交互
    /// 挂载到 GameHUD 的 GameWorldContainer 子对象
    /// </summary>
    public class TownMapController : MonoBehaviour
    {
        // ============ 网格常量 ============
        private const int COLS = 5;
        private const int ROWS = 4;
        private const float CELL_SIZE = 100f;
        private const float CELL_GAP = 8f;

        // ============ 建筑定义 ============
        private enum BuildingType
        {
            None,       // 路径
            Inn,        // 旅馆
            Shop,       // 商店
            SkillGuild, // 技能公会
            Dungeon     // 地牢入口
        }

        // 建筑布局 [row, col]
        private static readonly BuildingType[,] buildingLayout = new BuildingType[ROWS, COLS]
        {
            { BuildingType.None, BuildingType.None,       BuildingType.None, BuildingType.None,       BuildingType.None },
            { BuildingType.None, BuildingType.Inn,        BuildingType.None, BuildingType.Shop,       BuildingType.None },
            { BuildingType.None, BuildingType.None,       BuildingType.None, BuildingType.None,       BuildingType.None },
            { BuildingType.None, BuildingType.SkillGuild, BuildingType.None, BuildingType.Dungeon,    BuildingType.None }
        };

        // ============ 组件引用 ============
        private readonly Image[] cellImages = new Image[ROWS * COLS];
        private readonly Text[] cellTexts = new Text[ROWS * COLS];
        private readonly Color[] cellBaseColors = new Color[ROWS * COLS];

        // ============ 创建入口 ============

        /// <summary>
        /// 创建 TownMapController 实例，挂载到 parent 下
        /// </summary>
        public static TownMapController Create(Transform parent)
        {
            var obj = new GameObject("TownMap");
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            UIBuilder.SetStretch(obj);

            var controller = obj.AddComponent<TownMapController>();
            return controller;
        }

        private void Awake()
        {
            BuildGrid();
        }

        // ============ 网格构建 ============

        private void BuildGrid()
        {
            var selfRt = GetComponent<RectTransform>();

            // 网格总尺寸
            float totalW = COLS * CELL_SIZE + (COLS - 1) * CELL_GAP;
            float totalH = ROWS * CELL_SIZE + (ROWS - 1) * CELL_GAP;

            // 居中锚定
            selfRt.anchorMin = new Vector2(0.5f, 0.5f);
            selfRt.anchorMax = new Vector2(0.5f, 0.5f);
            selfRt.pivot = new Vector2(0.5f, 0.5f);
            selfRt.sizeDelta = new Vector2(totalW, totalH);
            selfRt.anchoredPosition = Vector2.zero;

            // 标题
            var titleObj = UIFactory.CreateUI(selfRt, "Title");
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0, 30);
            titleRt.anchoredPosition = new Vector2(0, -10);
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "-- 城  镇 --";
            titleText.fontSize = 22;
            titleText.color = UIColors.Gold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 网格容器
            var gridObj = UIFactory.CreateUI(selfRt, "Grid");
            var gridRt = gridObj.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.5f, 0.5f);
            gridRt.anchorMax = new Vector2(0.5f, 0.5f);
            gridRt.pivot = new Vector2(0.5f, 0.5f);
            gridRt.sizeDelta = new Vector2(totalW, totalH);
            // 向下偏移标题高度
            gridRt.anchoredPosition = new Vector2(0, -15);

            for (int row = 0; row < ROWS; row++)
            {
                for (int col = 0; col < COLS; col++)
                {
                    int idx = row * COLS + col;
                    BuildingType type = buildingLayout[row, col];
                    CreateCell(gridRt, row, col, idx, type);
                }
            }
        }

        private void CreateCell(RectTransform parent, int row, int col, int idx, BuildingType type)
        {
            // 计算位置（row=0 在顶部，y 向上为正）
            float x = col * (CELL_SIZE + CELL_GAP);
            float y = (ROWS - 1 - row) * (CELL_SIZE + CELL_GAP);
            float centerX = (COLS - 1) * (CELL_SIZE + CELL_GAP) / 2f;
            float centerY = (ROWS - 1) * (CELL_SIZE + CELL_GAP) / 2f;
            float posX = x - centerX;
            float posY = y - centerY;

            var cellObj = UIFactory.CreateUI(parent, $"Cell_{row}_{col}");
            var cellRt = cellObj.GetComponent<RectTransform>();
            cellRt.anchorMin = new Vector2(0.5f, 0.5f);
            cellRt.anchorMax = new Vector2(0.5f, 0.5f);
            cellRt.pivot = new Vector2(0.5f, 0.5f);
            cellRt.sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);
            cellRt.anchoredPosition = new Vector2(posX, posY);

            // 背景图片
            var img = cellObj.AddComponent<Image>();
            Color baseColor = GetCellColor(type);
            img.color = baseColor;
            img.raycastTarget = (type != BuildingType.None);

            cellImages[idx] = img;
            cellBaseColors[idx] = baseColor;

            // 建筑名称文字
            string label = GetCellLabel(type);
            var textObj = UIFactory.CreateUI(cellObj.transform, "Label");
            UIBuilder.SetStretch(textObj);
            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.fontSize = type != BuildingType.None ? 16 : 12;
            text.color = type != BuildingType.None ? UIColors.TextWhite : UIColors.TextDim;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            cellTexts[idx] = text;

            // 路径格子不可交互
            if (type == BuildingType.None) return;

            // 建筑格子添加交互
            var trigger = cellObj.AddComponent<EventTrigger>();

            // 悬停高亮
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            int capturedIdx = idx;
            enterEntry.callback.AddListener((_) => OnCellHover(capturedIdx, true));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((_) => OnCellHover(capturedIdx, false));
            trigger.triggers.Add(exitEntry);

            // 点击
            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            BuildingType capturedType = type;
            clickEntry.callback.AddListener((_) => OnCellClick(capturedType));
            trigger.triggers.Add(clickEntry);
        }

        // ============ 颜色与标签 ============

        private static Color GetCellColor(BuildingType type)
        {
            return type switch
            {
                BuildingType.Inn => UIColors.RoomSpring,        // 绿色 — 旅馆
                BuildingType.Shop => UIColors.RoomMerchant,     // 橙色 — 商店
                BuildingType.SkillGuild => UIColors.Purple,     // 紫色 — 技能公会
                BuildingType.Dungeon => UIColors.RoomBoss,      // 红色 — 地牢入口
                _ => UIColors.CardBg                             // 暗色 — 路径
            };
        }

        private static string GetCellLabel(BuildingType type)
        {
            return type switch
            {
                BuildingType.Inn => "旅  馆",
                BuildingType.Shop => "商  店",
                BuildingType.SkillGuild => "技能公会",
                BuildingType.Dungeon => "地牢入口",
                _ => ""
            };
        }

        /// <summary>悬停时变亮/恢复</summary>
        private void OnCellHover(int idx, bool enter)
        {
            if (cellImages[idx] == null) return;
            if (enter)
            {
                Color c = cellBaseColors[idx];
                cellImages[idx].color = new Color(
                    Mathf.Min(1f, c.r * 1.25f),
                    Mathf.Min(1f, c.g * 1.25f),
                    Mathf.Min(1f, c.b * 1.25f),
                    c.a
                );
            }
            else
            {
                cellImages[idx].color = cellBaseColors[idx];
            }
        }

        // ============ 建筑交互 ============

        private void OnCellClick(BuildingType type)
        {
            AudioManager.Instance?.PlaySFX(AudioClipType.UI_Click);

            switch (type)
            {
                case BuildingType.Inn:
                    OnInnClick();
                    break;
                case BuildingType.Shop:
                    OnShopClick();
                    break;
                case BuildingType.SkillGuild:
                    OnSkillGuildClick();
                    break;
                case BuildingType.Dungeon:
                    OnDungeonClick();
                    break;
            }
        }

        /// <summary>旅馆：休息恢复HP/MP + 存档</summary>
        private void OnInnClick()
        {
            var player = GameManager.Instance?.Player;
            if (player == null) return;

            player.FullRestore();
            GameManager.Instance.SaveGame();
            AudioManager.Instance?.PlaySFX(AudioClipType.Dungeon_HealWater);

            // 显示提示
            if (TipManager.Instance != null)
            {
                TipManager.Instance.ShowAutoTip("体力已完全恢复！",
                    new Vector2(0, 200), 2.5f);
            }
        }

        /// <summary>商店：打开商店面板</summary>
        private void OnShopClick()
        {
            UIManager.Instance.ShowPanel<ShopPanel>();
        }

        /// <summary>技能公会：打开技能学习面板</summary>
        private void OnSkillGuildClick()
        {
            UIManager.Instance.ShowPanel<SkillLearnPanel>();
        }

        /// <summary>地牢入口：生成地牢 + 切换到地牢</summary>
        private void OnDungeonClick()
        {
            var player = GameManager.Instance?.Player;
            if (player == null) return;

            var dungeon = new DungeonData();
            dungeon.GenerateFloor(player.dungeonFloor > 0 ? player.dungeonFloor : 1);
            GameManager.Instance.Dungeon = dungeon;
            GameManager.Instance.SetState(GameState.InDungeon);
            UIManager.Instance.ReplacePanel<DungeonPanel>();
        }
    }
}
