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
        // ---- 网格地图常量 ----
        private const int MaxGridSize = 10;
        private const int MaxTiles = MaxGridSize * MaxGridSize; // 100
        private const float MinCellSize = 60f;
        private const float MaxCellSize = 120f;
        private const float DefaultCellSize = 80f;
        private const float ZoomStep = 10f;

        // ---- 房间颜色方案（深色主题适配） ----
        private static readonly Color ColorWall      = HexColor(0x0d0d0d);      // 极黑
        private static readonly Color ColorMonster   = HexColor(0xc0392b);      // 深红
        private static readonly Color ColorElite     = HexColor(0xe74c3c);      // 亮红
        private static readonly Color ColorTreasure  = HexColor(0xf39c12);      // 金
        private static readonly Color ColorSpring    = HexColor(0x3498db);      // 蓝
        private static readonly Color ColorMerchant  = HexColor(0x2ecc71);      // 绿
        private static readonly Color ColorShrine    = HexColor(0x9b59b6);      // 紫
        private static readonly Color ColorBoss      = HexColor(0x8e44ad);      // 深紫
        private static readonly Color ColorExit      = HexColor(0xbdc3c7);      // 灰白
        private static readonly Color ColorEmpty     = HexColor(0x2c3e50);      // 暗蓝灰
        private static readonly Color ColorCurrentOutline = HexColor(0xc8a24c); // 金色边框
        private static readonly Color ColorClearedOverlay = new Color(0.5f, 0.5f, 0.5f, 0.35f); // 灰色半透明
        private static readonly Color ColorFogHidden   = new Color(0f, 0f, 0f, 1f);            // 未探索：纯黑不透明
        private static readonly Color ColorFogExplored = new Color(0f, 0f, 0f, 0.6f);          // 已探索：黑色半透明
        private static readonly Color ColorFogVisible  = new Color(0f, 0f, 0f, 0f);            // 可见：完全透明
        private static readonly Color ColorExploredCleared = new Color(0.5f, 0.5f, 0.5f, 0.3f); // 已探索+已清：灰色叠加

        private static Color HexColor(int rgb)
        {
            float r = ((rgb >> 16) & 0xFF) / 255f;
            float g = ((rgb >> 8) & 0xFF) / 255f;
            float b = (rgb & 0xFF) / 255f;
            return new Color(r, g, b);
        }

        private string pendingMessage;
        private float cellSize = DefaultCellSize;

        // ---- 随机房间事件 ----
        /// <summary>空房间事件类型枚举</summary>
        private enum EmptyRoomEvent
        {
            GoldBag,       // 发现金币袋
            Consumable,    // 发现消耗品
            Trap,          // 地板陷阱
            WanderingMerchant, // 流浪商人
            MysticPower    // 神秘力量
        }

        /// <summary>当前待处理的事件（null表示无事件）</summary>
        private EmptyRoomEvent? pendingEvent;

        /// <summary>事件弹窗中的选项回调（按钮1/按钮2）</summary>
        private System.Action pendingEventOption1;
        private System.Action pendingEventOption2;

        /// <summary>事件关联的房间瓦片（用于标记 isCleared）</summary>
        private DungeonTile pendingEventTile;

        // 事件弹窗第二个按钮引用
        private Button msgBtnOption2;
        private Text msgBtnContinueLabel;
        private Text msgBtnOption2Label;
        private RectTransform msgBtnContinueRt;

        // ---- 移动状态 ----
        private bool isMoving;           // 移动动画进行中，阻止连续输入
        private const float MoveDuration = 0.2f; // 平滑移动过渡时长（秒）
        private CanvasGroup cachedCanvasGroup;   // 缓存，避免每帧 GetComponent

        // ---- 战斗回传：战斗结束后标记对应房间 isCleared ----
        private DungeonTile pendingBattleTile;

        // ---- 缓存引用 ----
        private Text titleText;
        private Text statusText;
        private GameObject msgOverlay;
        private Text msgText;

        // ScrollRect 地图区
        private ScrollRect mapScrollRect;
        private RectTransform mapContent;
        private GameObject mapViewport;

        // 网格瓦片对象池
        private GridTile[] gridTiles;

        // 当前房间高亮边框
        private Image currentRoomOutline;

        private struct GridTile
        {
            public GameObject obj;
            public RectTransform rt;
            public Image bg;
            public Image overlay;  // 已清房间半透明叠加
            public Image fogOverlay;  // 战争迷雾层
            public Text iconText;
            public Text typeLabel;
            public Button btn;
        }

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

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);
            UIBuilder.CreateTitleBar(parent, "地       牢");

            // 状态栏（顶部信息条）
            var bar = UIBuilder.CreatePanel(parent, 0, 45, "StatusBar");
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1);
            barRt.anchoredPosition = new Vector2(0, -70);
            barRt.sizeDelta = new Vector2(0, 45);
            bar.GetComponent<Image>().color = UIBuilder.InfoBarBg;
            var statusTextObj = UIBuilder.CreateText(bar.transform, "", 16, UIBuilder.TextWhite, TextAnchor.MiddleCenter, name: "StatusText");
            UIBuilder.SetStretch(statusTextObj.gameObject);

            // ===== 左侧：地图面板（ScrollRect 可滚动缩放） =====
            var mapPanel = UIBuilder.CreateBorderedPanel(parent, 880, 500, "MapPanel");
            var mapPanelRt = mapPanel.GetComponent<RectTransform>();
            mapPanelRt.anchorMin = new Vector2(0, 0);
            mapPanelRt.anchorMax = new Vector2(1, 1);
            mapPanelRt.pivot = new Vector2(0.5f, 0.5f);
            mapPanelRt.anchoredPosition = new Vector2(-50, 20);
            mapPanelRt.sizeDelta = new Vector2(-280, -170);

            var mapInner = mapPanel.transform.Find("Inner");

            // ScrollRect Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(mapInner, false);
            var vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = UIBuilder.ViewportBg;
            var vpMask = viewport.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;

            // ScrollRect Content 容器
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1);
            contentRt.anchorMax = new Vector2(0.5f, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(MaxGridSize * DefaultCellSize, MaxGridSize * DefaultCellSize);

            // ScrollRect 组件（挂在 Viewport 上，确保拖拽和点击事件正确传递）
            var scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = vpRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 1f;
            scrollRect.decelerationRate = 0.135f;

            // 当前房间高亮边框（独立于瓦片池，仅一个）
            var outlineObj = new GameObject("CurrentOutline");
            outlineObj.transform.SetParent(contentRt, false);
            var outlineRt = outlineObj.AddComponent<RectTransform>();
            outlineRt.sizeDelta = new Vector2(DefaultCellSize, DefaultCellSize);
            outlineRt.anchorMin = new Vector2(0, 1);
            outlineRt.anchorMax = new Vector2(0, 1);
            outlineRt.pivot = new Vector2(0, 1);
            var outlineImg = outlineObj.AddComponent<Image>();
            outlineImg.color = ColorCurrentOutline;
            outlineImg.raycastTarget = false;

            var outlineInner = new GameObject("Inner");
            outlineInner.transform.SetParent(outlineObj.transform, false);
            var oiRt = outlineInner.AddComponent<RectTransform>();
            oiRt.anchorMin = Vector2.zero;
            oiRt.anchorMax = Vector2.one;
            oiRt.offsetMin = new Vector2(3, 3);
            oiRt.offsetMax = new Vector2(-3, -3);
            var oiImg = outlineInner.AddComponent<Image>();
            oiImg.color = Color.clear;
            oiImg.raycastTarget = false;

            // 网格瓦片对象池（最大 100 = 10x10）
            for (int i = 0; i < MaxTiles; i++)
            {
                CreateGridTileVisual(contentRt, i);
            }

            // 消息覆盖层（默认隐藏）
            var msgOverlay = new GameObject("MsgOverlay");
            msgOverlay.transform.SetParent(mapInner, false);
            UIBuilder.SetStretch(msgOverlay);
            var oImg = msgOverlay.AddComponent<Image>();
            oImg.color = new Color(UIBuilder.DarkBg.r, UIBuilder.DarkBg.g, UIBuilder.DarkBg.b, 0.88f);
            msgOverlay.AddComponent<CanvasGroup>();
            msgOverlay.SetActive(false);

            var msgTextObj = UIBuilder.CreateText(msgOverlay.transform, "", 26, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "MsgText");
            UIBuilder.SetStretch(msgTextObj.gameObject);

            var (btnContinue, _) = UIBuilder.CreateButtonWithRef(msgOverlay.transform, "继续", 160, 40, null, 20, "BtnContinue");
            UIBuilder.SetAnchored(btnContinue.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-90, -60), new Vector2(160, 40));

            // 事件弹窗第二个选项按钮（默认隐藏，仅事件弹窗时显示）
            var (btnOption2, _) = UIBuilder.CreateButtonWithRef(msgOverlay.transform, "离开", 160, 40, null, 20, "BtnOption2");
            UIBuilder.SetAnchored(btnOption2.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(90, -60), new Vector2(160, 40));

            // ===== 右侧：状态面板 =====
            CreateStatusPanelVisuals(parent);

            // ===== 底部技能快捷栏 =====
            CreateSkillBarVisuals(parent);

            // ===== 底部按钮区 =====
            float btnW = 150;
            float btnH = 45;

            var (btnBack, _) = UIBuilder.CreateButtonWithRef(parent, "返回城镇", btnW, btnH, null, 20, "BtnBack");
            UIBuilder.SetAnchored(btnBack.gameObject,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-180, 30), new Vector2(btnW, btnH));

            var (btnBag, _) = UIBuilder.CreateButtonWithRef(parent, "背包", btnW, btnH, null, 18, "BtnBag");
            UIBuilder.SetAnchored(btnBag.gameObject,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 30), new Vector2(btnW, btnH));

            var (btnRole, _) = UIBuilder.CreateButtonWithRef(parent, "角色", btnW, btnH, null, 18, "BtnRole");
            UIBuilder.SetAnchored(btnRole.gameObject,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(180, 30), new Vector2(btnW, btnH));
        }

        private static void CreateStatusPanelVisuals(Transform parent)
        {
            var panel = UIBuilder.CreateBorderedPanel(parent, 240, 500, "StatusPanel");
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1, 0);
            panelRt.anchorMax = new Vector2(1, 1);
            panelRt.pivot = new Vector2(1, 0.5f);
            panelRt.anchoredPosition = new Vector2(-20, 20);
            panelRt.sizeDelta = new Vector2(240, -170);

            var inner = panel.transform.Find("Inner");

            float y = 200;
            float sliderW = 200;
            float barH = 18;
            float rowGap = 60;

            // HP 条
            var hpLabel = UIBuilder.CreateText(inner, "HP", 15, UIBuilder.HpColor, TextAnchor.MiddleLeft, true, "HpLabel");
            UIBuilder.SetAnchored(hpLabel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-70, y), new Vector2(36, 22));
            var hpSliderObj = UIBuilder.CreateSlider(inner, sliderW, barH, UIBuilder.HpColor, "HpSlider");
            UIBuilder.SetAnchored(hpSliderObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(20, y), new Vector2(sliderW, barH));
            var hpTextObj = UIBuilder.CreateText(inner, "", 13, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "HpText");
            UIBuilder.SetAnchored(hpTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y - 18), new Vector2(sliderW, 16));

            y -= rowGap;

            // MP 条
            var mpLabel = UIBuilder.CreateText(inner, "MP", 15, UIBuilder.MpColor, TextAnchor.MiddleLeft, true, "MpLabel");
            UIBuilder.SetAnchored(mpLabel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-70, y), new Vector2(36, 22));
            var mpSliderObj = UIBuilder.CreateSlider(inner, sliderW, barH, UIBuilder.MpColor, "MpSlider");
            UIBuilder.SetAnchored(mpSliderObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(20, y), new Vector2(sliderW, barH));
            var mpTextObj = UIBuilder.CreateText(inner, "", 13, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "MpText");
            UIBuilder.SetAnchored(mpTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y - 18), new Vector2(sliderW, 16));

            y -= rowGap;

            // EXP 条
            var expLabel = UIBuilder.CreateText(inner, "EXP", 15, UIBuilder.ExpColor, TextAnchor.MiddleLeft, true, "ExpLabel");
            UIBuilder.SetAnchored(expLabel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-70, y), new Vector2(36, 22));
            var expSliderObj = UIBuilder.CreateSlider(inner, sliderW, barH, UIBuilder.ExpColor, "ExpSlider");
            UIBuilder.SetAnchored(expSliderObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(20, y), new Vector2(sliderW, barH));
            var expTextObj = UIBuilder.CreateText(inner, "", 13, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "ExpText");
            UIBuilder.SetAnchored(expTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y - 18), new Vector2(sliderW, 16));

            y -= rowGap + 5;

            // 分隔线
            var div = UIBuilder.CreateDivider(inner);
            var divRt = div.GetComponent<RectTransform>();
            divRt.anchoredPosition = new Vector2(0, y + 8);
            divRt.sizeDelta = new Vector2(210, 2);

            y -= 12;

            // 层数/金币/攻防信息
            var floorTextObj = UIBuilder.CreateText(inner, "", 17, UIBuilder.Gold, TextAnchor.MiddleLeft, true, "FloorText");
            UIBuilder.SetAnchored(floorTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y), new Vector2(210, 24));
            y -= 28;
            var goldTextObj = UIBuilder.CreateText(inner, "", 17, UIBuilder.Gold, TextAnchor.MiddleLeft, false, "GoldText");
            UIBuilder.SetAnchored(goldTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y), new Vector2(210, 24));
            y -= 28;
            var statsTextObj = UIBuilder.CreateText(inner, "", 15, UIBuilder.TextDim, TextAnchor.MiddleLeft, false, "StatsText");
            UIBuilder.SetAnchored(statsTextObj.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y), new Vector2(210, 22));
        }

        private static void CreateSkillBarVisuals(Transform parent)
        {
            float slotW = 160;
            float slotH = 55;
            float gap = 15;
            float totalW = MaxSkillSlots * slotW + (MaxSkillSlots - 1) * gap;
            float startX = -totalW / 2 + slotW / 2;
            float barY = -245;

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

        /// <summary>创建网格瓦片视觉结构（对象池）</summary>
        private static void CreateGridTileVisual(Transform parent, int index)
        {
            // 外层
            var obj = new GameObject($"Tile_{index}");
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(DefaultCellSize, DefaultCellSize);
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            var bg = obj.AddComponent<Image>();
            bg.color = ColorWall;

            // 已清叠加层
            var overlayObj = new GameObject("Overlay");
            overlayObj.transform.SetParent(obj.transform, false);
            var olRt = overlayObj.AddComponent<RectTransform>();
            olRt.anchorMin = Vector2.zero;
            olRt.anchorMax = Vector2.one;
            olRt.offsetMin = Vector2.zero;
            olRt.offsetMax = Vector2.zero;
            var overlay = overlayObj.AddComponent<Image>();
            overlay.color = ColorClearedOverlay;
            overlay.raycastTarget = false;

            // 图标文字
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(obj.transform, false);
            var iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.3f);
            iconRt.anchorMax = new Vector2(1, 1);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var iconText = iconObj.AddComponent<Text>();
            iconText.fontSize = 20;
            iconText.color = Color.white;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            iconText.raycastTarget = false;

            // 类型标签
            var labelObj = new GameObject("TypeLabel");
            labelObj.transform.SetParent(obj.transform, false);
            var labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 0.3f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var typeLabel = labelObj.AddComponent<Text>();
            typeLabel.fontSize = 9;
            typeLabel.color = UIBuilder.TextWhite;
            typeLabel.alignment = TextAnchor.MiddleCenter;
            typeLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            typeLabel.raycastTarget = false;

            // 战争迷雾层（最顶层，覆盖所有内容）
            var fogObj = new GameObject("FogOverlay");
            fogObj.transform.SetParent(obj.transform, false);
            var fogRt = fogObj.AddComponent<RectTransform>();
            fogRt.anchorMin = Vector2.zero;
            fogRt.anchorMax = Vector2.one;
            fogRt.offsetMin = Vector2.zero;
            fogRt.offsetMax = Vector2.zero;
            var fogImg = fogObj.AddComponent<Image>();
            fogImg.color = ColorFogHidden;
            fogImg.raycastTarget = false;

            obj.AddComponent<Button>();
        }

        protected override void BindReferences()
        {
            cachedCanvasGroup = GetComponent<CanvasGroup>();
            titleText = FindComponent<Text>("TitleBar/Title");
            statusText = FindComponent<Text>("StatusBar/StatusText");

            // ScrollRect 地图区
            mapScrollRect = FindComponent<ScrollRect>("MapPanel/Inner/Viewport");
            mapContent = FindComponent<RectTransform>("MapPanel/Inner/Viewport/Content");
            mapViewport = transform.Find("MapPanel/Inner/Viewport").gameObject;

            // 当前房间高亮边框
            currentRoomOutline = FindComponent<Image>("MapPanel/Inner/Viewport/Content/CurrentOutline");

            // 网格瓦片对象池
            gridTiles = new GridTile[MaxTiles];
            for (int i = 0; i < MaxTiles; i++)
            {
                string path = $"MapPanel/Inner/Viewport/Content/Tile_{i}";
                var tileObj = transform.Find(path).gameObject;
                gridTiles[i] = new GridTile
                {
                    obj = tileObj,
                    rt = tileObj.GetComponent<RectTransform>(),
                    bg = tileObj.GetComponent<Image>(),
                    overlay = tileObj.transform.Find("Overlay").GetComponent<Image>(),
                    fogOverlay = tileObj.transform.Find("FogOverlay").GetComponent<Image>(),
                    iconText = tileObj.transform.Find("Icon").GetComponent<Text>(),
                    typeLabel = tileObj.transform.Find("TypeLabel").GetComponent<Text>(),
                    btn = tileObj.GetComponent<Button>()
                };
                tileObj.SetActive(false);
            }

            // 消息覆盖
            msgOverlay = transform.Find("MapPanel/Inner/MsgOverlay").gameObject;
            msgText = FindComponent<Text>("MapPanel/Inner/MsgOverlay/MsgText");
            msgBtnOption2 = FindComponent<Button>("MapPanel/Inner/MsgOverlay/BtnOption2");
            msgBtnContinueLabel = FindComponent<Text>("MapPanel/Inner/MsgOverlay/BtnContinue/Label");
            msgBtnOption2Label = FindComponent<Text>("MapPanel/Inner/MsgOverlay/BtnOption2/Label");
            msgBtnContinueRt = FindComponent<RectTransform>("MapPanel/Inner/MsgOverlay/BtnContinue");

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
            // 网格瓦片点击：通过 index 反算 GridPos
            for (int i = 0; i < MaxTiles; i++)
            {
                int capturedIndex = i;
                gridTiles[i].btn.onClick.AddListener(() =>
                {
                    PlayUIClick();
                    OnClickGridTile(capturedIndex);
                });
            }

            FindComponent<Button>("MapPanel/Inner/MsgOverlay/BtnContinue").onClick.AddListener(() =>
            {
                PlayUIClick();
                // 检查是否有Boss战警告待确认
                if (pendingBossConfig != null)
                {
                    var cfg = pendingBossConfig;
                    var bTile = pendingBossTile;
                    pendingBossConfig = null;
                    pendingBossTile = null;
                    pendingMessage = null;
                    StartBattleWithConfig(GameManager.Instance.Dungeon, bTile, cfg);
                    return;
                }
                // 检查是否有事件选项1待执行
                if (pendingEvent.HasValue && pendingEventOption1 != null)
                {
                    var action = pendingEventOption1;
                    ClearPendingEvent();
                    action();
                }
                // 检查是否有出口待确认
                else if (pendingExitFloor > 0)
                {
                    ConfirmEnterNextFloor();
                }
                else
                {
                    pendingMessage = null;
                }
                RefreshUI();
            });

            // 事件弹窗第二个选项按钮
            msgBtnOption2.onClick.AddListener(() =>
            {
                PlayUIClick();
                if (pendingEvent.HasValue && pendingEventOption2 != null)
                {
                    var action = pendingEventOption2;
                    ClearPendingEvent();
                    action();
                }
                else
                {
                    pendingMessage = null;
                }
                RefreshUI();
            });

            FindComponent<Button>("BtnBack").onClick.AddListener(() =>
            {
                PlayUIClick();
                Debug.Log("[操作] 返回城镇");
                GameManager.Instance.Dungeon.OnReturnTown();
                GameManager.Instance.Player.dungeonFloor = GameManager.Instance.Dungeon.floor;
                GameManager.Instance.SetState(GameState.InTown);
                GameManager.Instance.SaveGame();
                UIManager.Instance.ReplacePanel<TownPanel>();
            });

            FindComponent<Button>("BtnBag").onClick.AddListener(() =>
            {
                PlayUIClick();
                UIManager.Instance.ShowPanel<BagPanel>();
            });

            FindComponent<Button>("BtnRole").onClick.AddListener(() =>
            {
                PlayUIClick();
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
            // 战斗面板关闭后回到地牢面板：检查是否有战斗胜利待处理
            if (pendingBattleTile != null)
            {
                var battle = BattleManager.Instance.CurrentBattle;
                if (battle != null && battle.isFinished && battle.isVictory)
                {
                    // 战斗胜利：标记房间为已清理
                    pendingBattleTile.isCleared = true;
                    var room = FindMatchingRoom(GameManager.Instance.Dungeon, pendingBattleTile);
                    room.cleared = true;
                    Debug.Log($"[地牢] 战斗胜利，房间已清理 pos=({pendingBattleTile.gridPos.x},{pendingBattleTile.gridPos.y})");
                }
                pendingBattleTile = null;
            }

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

            // 顶部状态栏
            var curTile = dungeon.CurrentTile;
            statusText.text = curTile != null
                ? $"坐标 ({dungeon.currentPos.x},{dungeon.currentPos.y})  房间数 {dungeon.roomTiles.Count}"
                : "";

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

            // 网格地图
            UpdateGridMap(dungeon);

            // 消息覆盖
            if (!string.IsNullOrEmpty(pendingMessage))
            {
                msgText.text = pendingMessage;
                msgOverlay.SetActive(true);

                // 事件弹窗模式：显示/隐藏第二个按钮，调整按钮1位置和文字
                if (pendingEvent.HasValue)
                {
                    msgBtnOption2.gameObject.SetActive(true);
                    // 双按钮布局：左-90 右+90
                    msgBtnContinueRt.anchoredPosition = new Vector2(-90, -60);
                }
                else
                {
                    msgBtnOption2.gameObject.SetActive(false);
                    // 单按钮居中
                    msgBtnContinueRt.anchoredPosition = new Vector2(0, -60);
                }
            }
            else
            {
                msgOverlay.SetActive(false);
                msgBtnOption2.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 根据网格数据刷新地图瓦片（零创建零销毁）+ 战争迷雾
        /// </summary>
        private void UpdateGridMap(DungeonData dungeon)
        {
            if (dungeon.grid == null) return;

            int w = dungeon.gridWidth;
            int h = dungeon.gridHeight;

            // 更新 content 尺寸
            mapContent.sizeDelta = new Vector2(w * cellSize, h * cellSize);

            // 隐藏当前房间高亮
            bool currentFound = false;

            // 遍历所有瓦片
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (idx >= MaxTiles) continue;

                    var tile = gridTiles[idx];
                    var dungeonTile = dungeon.grid[x, y];
                    bool isCurrent = dungeon.currentPos.x == x && dungeon.currentPos.y == y;

                    // 获取可见性状态
                    var visibility = dungeon.GetTileVisibility(x, y);

                    // 位置和大小
                    tile.obj.SetActive(true);
                    tile.rt.pivot = new Vector2(0, 1); // 左上角锚定
                    tile.rt.anchorMin = new Vector2(0, 1);
                    tile.rt.anchorMax = new Vector2(0, 1);
                    tile.rt.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                    tile.rt.sizeDelta = new Vector2(cellSize, cellSize);

                    // === 战争迷雾渲染 ===
                    if (visibility == TileVisibility.Hidden)
                    {
                        // 未探索：纯黑覆盖，不显示任何内容
                        tile.bg.color = ColorWall;
                        tile.overlay.color = Color.clear;
                        tile.fogOverlay.color = ColorFogHidden;
                        tile.iconText.text = "";
                        tile.typeLabel.text = "";
                        tile.btn.interactable = false;
                    }
                    else if (visibility == TileVisibility.Explored)
                    {
                        // 已探索但不在视野内：显示房间颜色但alpha降低
                        if (dungeonTile.tileType == TileType.Wall)
                        {
                            tile.bg.color = ColorWall;
                            tile.overlay.color = Color.clear;
                            tile.iconText.text = "";
                            tile.typeLabel.text = "";
                        }
                        else
                        {
                            // 房间着色降低透明度
                            var roomColor = GetTileColor(dungeonTile.roomType);
                            tile.bg.color = new Color(roomColor.r, roomColor.g, roomColor.b, 0.4f);
                            tile.iconText.text = GetTileIcon(dungeonTile.roomType);
                            tile.typeLabel.text = GetTileTypeName(dungeonTile.roomType, false);

                            // 已清房间再加灰色叠加
                            if (dungeonTile.isCleared)
                            {
                                tile.overlay.color = ColorExploredCleared;
                            }
                            else
                            {
                                tile.overlay.color = Color.clear;
                            }
                        }

                        tile.fogOverlay.color = ColorFogExplored;
                        tile.btn.interactable = false;
                    }
                    else // Visible
                    {
                        // 完全可见：正常渲染
                        if (dungeonTile.tileType == TileType.Wall)
                        {
                            tile.bg.color = ColorWall;
                            tile.overlay.color = Color.clear;
                            tile.iconText.text = "";
                            tile.typeLabel.text = "";
                            tile.btn.interactable = false;
                        }
                        else
                        {
                            tile.bg.color = GetTileColor(dungeonTile.roomType);
                            tile.iconText.text = GetTileIcon(dungeonTile.roomType);
                            tile.typeLabel.text = GetTileTypeName(dungeonTile.roomType, isCurrent);

                            // 已清房间覆盖灰色半透明
                            if (dungeonTile.isCleared && !isCurrent)
                            {
                                tile.overlay.color = ColorClearedOverlay;
                            }
                            else
                            {
                                tile.overlay.color = Color.clear;
                            }

                            // 只有当前房间和相邻可达房间可交互
                            tile.btn.interactable = isCurrent || IsAdjacentToCurrent(dungeon, x, y);
                        }

                        tile.fogOverlay.color = ColorFogVisible;
                    }

                    // 当前房间高亮（仅可见状态下显示）
                    if (isCurrent && dungeonTile.tileType == TileType.Room && visibility == TileVisibility.Visible)
                    {
                        currentFound = true;
                        currentRoomOutline.rectTransform.pivot = new Vector2(0, 1);
                        currentRoomOutline.rectTransform.anchorMin = new Vector2(0, 1);
                        currentRoomOutline.rectTransform.anchorMax = new Vector2(0, 1);
                        // 动画进行中不覆盖位置，由协程控制
                        if (!isMoving)
                            currentRoomOutline.rectTransform.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                        currentRoomOutline.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                        currentRoomOutline.gameObject.SetActive(true);
                    }
                }
            }

            // 隐藏多余瓦片
            int totalUsed = w * h;
            for (int i = totalUsed; i < MaxTiles; i++)
            {
                gridTiles[i].obj.SetActive(false);
            }

            if (!currentFound)
            {
                currentRoomOutline.gameObject.SetActive(false);
            }
        }

        /// <summary>判断指定坐标是否与当前玩家位置相邻且连通</summary>
        private bool IsAdjacentToCurrent(DungeonData dungeon, int x, int y)
        {
            var cp = dungeon.currentPos;
            int dx = x - cp.x;
            int dy = y - cp.y;
            // 必须上下左右相邻（距离=1）
            if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) return false;
            // 检查当前瓦片对应方向的连通性
            var currentTile = dungeon.CurrentTile;
            if (currentTile == null) return false;
            // 上(y-1) 下(y+1) 左(x-1) 右(x+1)
            if (dy == -1 && !currentTile.connectUp) return false;
            if (dy == 1 && !currentTile.connectDown) return false;
            if (dx == -1 && !currentTile.connectLeft) return false;
            if (dx == 1 && !currentTile.connectRight) return false;
            // 目标必须是房间
            var tile = dungeon.GetTileAt(x, y);
            return tile != null && tile.tileType == TileType.Room;
        }

        /// <summary>根据房间类型获取颜色</summary>
        private Color GetTileColor(DungeonEventType roomType)
        {
            return roomType switch
            {
                DungeonEventType.Monster => ColorMonster,
                DungeonEventType.Elite => ColorElite,
                DungeonEventType.Boss => ColorBoss,
                DungeonEventType.Treasure => ColorTreasure,
                DungeonEventType.Spring => ColorSpring,
                DungeonEventType.Merchant => ColorMerchant,
                DungeonEventType.Shrine => ColorShrine,
                DungeonEventType.Exit => ColorExit,
                _ => ColorEmpty
            };
        }

        /// <summary>根据房间类型获取图标文字</summary>
        private string GetTileIcon(DungeonEventType roomType)
        {
            return roomType switch
            {
                DungeonEventType.Monster => "怪",
                DungeonEventType.Elite => "精",
                DungeonEventType.Boss => "王",
                DungeonEventType.Treasure => "宝",
                DungeonEventType.Spring => "泉",
                DungeonEventType.Merchant => "商",
                DungeonEventType.Exit => "梯",
                DungeonEventType.Shrine => "神",
                _ => ""
            };
        }

        /// <summary>根据房间类型获取类型名</summary>
        private string GetTileTypeName(DungeonEventType roomType, bool isCurrent)
        {
            if (isCurrent) return "当前";
            return roomType switch
            {
                DungeonEventType.Monster => "怪物",
                DungeonEventType.Elite => "精英",
                DungeonEventType.Boss => "BOSS",
                DungeonEventType.Treasure => "宝箱",
                DungeonEventType.Spring => "泉水",
                DungeonEventType.Merchant => "商人",
                DungeonEventType.Exit => "出口",
                DungeonEventType.Shrine => "神殿",
                _ => ""
            };
        }

        /// <summary>网格瓦片点击处理：通过 index 反算 GridPos</summary>
        private void OnClickGridTile(int index)
        {
            var dungeon = GameManager.Instance.Dungeon;
            if (dungeon.grid == null) return;
            if (isMoving) return;

            int w = dungeon.gridWidth;
            int x = index % w;
            int y = index / w;

            // 当前位置点击：不移动，但可触发房间事件
            bool isCurrent = dungeon.currentPos.x == x && dungeon.currentPos.y == y;
            if (isCurrent)
            {
                var curTile = dungeon.CurrentTile;
                if (curTile != null && !curTile.isCleared)
                    TriggerRoomEvent(dungeon, curTile);
                RefreshUI();
                return;
            }

            // 检查相邻且连通
            if (!IsAdjacentToCurrent(dungeon, x, y)) return;

            var tile = dungeon.GetTileAt(x, y);
            if (tile == null || tile.tileType != TileType.Room) return;

            // 执行移动
            TryMoveTo(dungeon, x, y);
        }

        /// <summary>尝试移动到指定相邻坐标（点击/键盘共用入口）</summary>
        private void TryMoveTo(DungeonData dungeon, int targetX, int targetY)
        {
            if (isMoving) return;

            var tile = dungeon.GetTileAt(targetX, targetY);
            if (tile == null || tile.tileType != TileType.Room) return;

            // 更新战争迷雾 + 移动数据
            dungeon.OnMoveTo(new GridPos(targetX, targetY));
            dungeon.currentRoomIndex = FindRoomIndex(dungeon, targetX, targetY);
            AudioManager.Instance?.PlaySFX(AudioClipType.Dungeon_Move);

            // 平滑移动指示器动画
            StartCoroutine(SmoothMoveIndicator(targetX, targetY, () =>
            {
                // 动画完成后处理房间事件
                if (!tile.isCleared)
                    TriggerRoomEvent(dungeon, tile);
                RefreshUI();
            }));

            // 立即刷新地图（迷雾/瓦片状态）
            RefreshUI();
        }

        /// <summary>处理房间事件逻辑</summary>
        private void TriggerRoomEvent(DungeonData dungeon, DungeonTile tile)
        {
            switch (tile.roomType)
            {
                case DungeonEventType.Monster:
                case DungeonEventType.Elite:
                case DungeonEventType.Boss:
                    HandleBattle(dungeon, tile);
                    break;
                case DungeonEventType.Treasure:
                    Debug.Log("[地牢] 发现宝箱");
                    HandleTreasure(tile);
                    break;
                case DungeonEventType.Spring:
                    Debug.Log("[地牢] 发现泉水");
                    HandleSpring(tile);
                    break;
                case DungeonEventType.Merchant:
                    Debug.Log("[地牢] 遇到商人");
                    UIManager.Instance.ShowPanel<ShopPanel>();
                    break;
                case DungeonEventType.Exit:
                    Debug.Log("[地牢] 进入下一层");
                    HandleExit();
                    break;
                case DungeonEventType.Shrine:
                    Debug.Log("[地牢] 发现神殿");
                    HandleShrine(tile);
                    break;
                case DungeonEventType.Empty:
                    Debug.Log("[地牢] 空房间");
                    HandleEmptyRoom(dungeon, tile);
                    break;
            }
        }

        /// <summary>平滑移动玩家位置指示器（Lerp过渡）</summary>
        private System.Collections.IEnumerator SmoothMoveIndicator(int targetX, int targetY, System.Action onComplete)
        {
            isMoving = true;
            float elapsed = 0f;
            Vector2 startPos = currentRoomOutline.rectTransform.anchoredPosition;
            Vector2 endPos = new Vector2(targetX * cellSize, -targetY * cellSize);

            while (elapsed < MoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / MoveDuration);
                // 缓动：SmoothStep
                t = t * t * (3f - 2f * t);
                currentRoomOutline.rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            currentRoomOutline.rectTransform.anchoredPosition = endPos;
            isMoving = false;
            onComplete?.Invoke();
        }

        /// <summary>在旧 rooms 列表中查找匹配的房间索引（向后兼容）</summary>
        private int FindRoomIndex(DungeonData dungeon, int x, int y)
        {
            for (int i = 0; i < dungeon.rooms.Count; i++)
            {
                if (dungeon.rooms[i].position.x == x && dungeon.rooms[i].position.y == y)
                    return i;
            }
            return dungeon.currentRoomIndex;
        }

        /// <summary>处理战斗事件</summary>
        private void HandleBattle(DungeonData dungeon, DungeonTile tile)
        {
            Debug.Log($"[地牢] 遭遇战斗 type={tile.roomType}");
            var monsters = ConfigManager.Instance.GetMonstersByFloor(dungeon.floor);
            var monsterCfg = tile.roomType == DungeonEventType.Boss
                ? ConfigManager.Instance.GetBossByFloor(dungeon.floor)
                : monsters.Count > 0 ? monsters[Random.Range(0, monsters.Count)] : null;
            if (monsterCfg != null)
            {
                // Boss战：显示警告弹窗，玩家确认后进入战斗
                if (tile.roomType == DungeonEventType.Boss)
                {
                    pendingBattleTile = tile;
                    pendingMessage = $"强大的敌人出现了！\n{monsterCfg.name} 挡住了去路！\n\n无法从这里逃跑...";
                    msgBtnContinueLabel.text = "迎战";
                    // 存储Boss配置供确认后使用
                    pendingBossConfig = monsterCfg;
                    pendingBossTile = tile;
                    RefreshUI();
                    return;
                }

                StartBattleWithConfig(dungeon, tile, monsterCfg);
            }
        }

        /// <summary>存储待确认的Boss战配置</summary>
        private MonsterConfig pendingBossConfig;
        private DungeonTile pendingBossTile;

        /// <summary>使用指定怪物配置开始战斗</summary>
        private void StartBattleWithConfig(DungeonData dungeon, DungeonTile tile, MonsterConfig monsterCfg)
        {
            // 记录战斗对应的瓦片，战斗胜利后标记 isCleared
            pendingBattleTile = tile;

            float floorScale = 1f + (dungeon.floor - 1) * 0.15f;
            float eliteScale = tile.roomType == DungeonEventType.Elite ? 1.5f : 1f;
            float totalScale = floorScale * eliteScale;

            int scaledHp = Mathf.RoundToInt(monsterCfg.hp * totalScale);
            int scaledAtk = Mathf.RoundToInt(monsterCfg.attack * totalScale);
            int scaledDef = Mathf.RoundToInt(monsterCfg.defense * totalScale);
            int rewardMul = tile.roomType == DungeonEventType.Elite ? 2 : 1;

            // Boss战使用原始名，不做缩放（Boss属性已经够高）
            string displayName = tile.roomType == DungeonEventType.Boss
                ? monsterCfg.name
                : tile.roomType == DungeonEventType.Elite
                    ? $"精英{monsterCfg.name}" : monsterCfg.name;

            // 向后兼容：找到对应旧 DungeonRoom
            var room = FindMatchingRoom(dungeon, tile);

            var battleData = new BattleData
            {
                monster = new BattleMonster
                {
                    id = monsterCfg.id, name = displayName,
                    hp = scaledHp, maxHp = scaledHp,
                    mp = monsterCfg.mp, maxMp = monsterCfg.mp,
                    attack = scaledAtk, defense = scaledDef,
                    speed = monsterCfg.speed, skillIds = monsterCfg.skillIds,
                    expReward = monsterCfg.expReward * rewardMul,
                    goldReward = monsterCfg.goldReward * rewardMul,
                    drops = monsterCfg.drops,
                    isBoss = monsterCfg.isBoss,
                    aiPattern = monsterCfg.aiPattern,
                    level = Mathf.RoundToInt(monsterCfg.minFloor * 2 * totalScale) + 1
                },
                room = room
            };
            BattleManager.Instance.StartBattle(battleData);
            UIManager.Instance.ShowPanel<BattlePanel>();
        }

        /// <summary>根据 DungeonTile 查找旧 rooms 列表中对应的 DungeonRoom（兼容 BattleManager）</summary>
        private DungeonRoom FindMatchingRoom(DungeonData dungeon, DungeonTile tile)
        {
            foreach (var room in dungeon.rooms)
            {
                if (room.position.x == tile.gridPos.x && room.position.y == tile.gridPos.y)
                    return room;
            }
            // 兜底：创建临时
            return new DungeonRoom
            {
                type = tile.roomType,
                cleared = tile.isCleared,
                position = new Vector2Int(tile.gridPos.x, tile.gridPos.y)
            };
        }

        private void HandleTreasure(DungeonTile tile)
        {
            AudioManager.Instance?.PlaySFX(AudioClipType.Dungeon_Discover);
            var player = GameManager.Instance.Player;
            int goldFound = Random.Range(10, 30) * GameManager.Instance.Dungeon.floor;
            player.gold += goldFound;
            pendingMessage = $"发现宝箱！获得 {goldFound} 金币";

            var allTreasures = ConfigManager.Instance.GetAllTreasures();
            var available = allTreasures.FindAll(t => !player.treasures.Contains(t.id));
            if (available.Count > 0 && Random.value < GameConstants.TREASURE_CHANCE)
            {
                var treasure = available[Random.Range(0, available.Count)];
                player.treasures.Add(treasure.id);
                player.RecalcStats();
                pendingMessage += $"\n获得宝物：{treasure.name} — {treasure.desc}";
            }

            var consumables = ConfigManager.Instance.GetItemsByType(ItemType.Consumable);
            if (consumables.Count > 0 && Random.value < GameConstants.CONSUMABLE_DROP_CHANCE && player.bagItems.Count < player.maxBagSize)
            {
                var item = consumables[Random.Range(0, consumables.Count)];
                player.bagItems.Add(item.id);
                pendingMessage += $"\n获得物品：{item.name}";
            }
            tile.isCleared = true;

            // 同步旧 rooms
            var room = FindMatchingRoom(GameManager.Instance.Dungeon, tile);
            room.cleared = true;
        }

        private void HandleSpring(DungeonTile tile)
        {
            AudioManager.Instance?.PlaySFX(AudioClipType.Dungeon_Discover);
            var player = GameManager.Instance.Player;
            int hpHeal = player.maxHp / 3;
            int mpHeal = player.maxMp / 3;
            player.hp = Mathf.Min(player.hp + hpHeal, player.maxHp);
            player.mp = Mathf.Min(player.mp + mpHeal, player.maxMp);
            pendingMessage = $"发现泉水！\n恢复 {hpHeal} 生命，{mpHeal} 法力";
            tile.isCleared = true;

            var room = FindMatchingRoom(GameManager.Instance.Dungeon, tile);
            room.cleared = true;
        }

        /// <summary>技能神殿：遍历全技能池，筛选未学技能，随机免费学习一个</summary>
        private void HandleShrine(DungeonTile tile)
        {
            AudioManager.Instance?.PlaySFX(AudioClipType.Dungeon_Discover);
            var player = GameManager.Instance.Player;

            // 遍历全技能池，筛选玩家未学的非被动技能
            var allSkills = ConfigManager.Instance.GetAllSkills();
            var unlearned = new List<SkillConfig>();
            foreach (var skill in allSkills)
            {
                if (skill.type == SkillType.Passive) continue;
                if (player.learnedSkills.Contains(skill.id)) continue;
                unlearned.Add(skill);
            }

            if (unlearned.Count > 0)
            {
                // 随机选一个免费学习
                var chosen = unlearned[Random.Range(0, unlearned.Count)];
                player.LearnSkill(chosen.id);
                pendingMessage = $"技能神殿：习得 {chosen.name}！\n{chosen.desc}";
                Debug.Log($"[神殿] 习得技能 {chosen.name}(id={chosen.id})");
            }
            else
            {
                // 所有技能已学，改为恢复效果
                int hpHeal = player.maxHp / 2;
                player.hp = Mathf.Min(player.hp + hpHeal, player.maxHp);
                pendingMessage = "技能神殿：你已领悟所有技能！\n恢复了生命值";
            }

            tile.isCleared = true;

            var room = FindMatchingRoom(GameManager.Instance.Dungeon, tile);
            room.cleared = true;
        }

        /// <summary>出口处理：显示确认弹窗，点击继续进入下一层</summary>
        private void HandleExit()
        {
            var currentDungeon = GameManager.Instance.Dungeon;
            int nextFloor = currentDungeon.floor + 1;

            // 标记出口房间为已清理
            var exitTile = currentDungeon.CurrentTile;
            if (exitTile != null)
            {
                exitTile.isCleared = true;
                var exitRoom = FindMatchingRoom(currentDungeon, exitTile);
                exitRoom.cleared = true;
            }

            // 通关检查
            if (nextFloor > currentDungeon.maxFloor)
            {
                pendingMessage = "恭喜通关！\n你征服了地牢所有楼层！\n\n可继续探索无尽模式...";
                pendingExitFloor = nextFloor;
                pendingExitMaxFloor = int.MaxValue;
            }
            else
            {
                pendingMessage = $"发现通往第 {nextFloor} 层的出口！\n点击继续前往下一层";
                pendingExitFloor = nextFloor;
                pendingExitMaxFloor = currentDungeon.maxFloor;
            }

            RefreshUI();
        }

        // ---- F137: 随机房间事件 ----

        /// <summary>清除事件弹窗的待处理状态</summary>
        private void ClearPendingEvent()
        {
            pendingEvent = null;
            pendingEventOption1 = null;
            pendingEventOption2 = null;
            pendingEventTile = null;
            pendingMessage = null;
        }

        /// <summary>标记事件关联房间为已清理</summary>
        private void MarkEventTileCleared()
        {
            if (pendingEventTile != null)
            {
                pendingEventTile.isCleared = true;
                var room = FindMatchingRoom(GameManager.Instance.Dungeon, pendingEventTile);
                room.cleared = true;
            }
        }

        /// <summary>
        /// 空房间事件触发：50%概率触发随机事件（16%空房间 * 50% = 总约8%）
        /// 事件类型及概率：金币袋(40%)、消耗品(20%)、陷阱(20%)、流浪商人(10%)、神秘力量(10%)
        /// </summary>
        private void HandleEmptyRoom(DungeonData dungeon, DungeonTile tile)
        {
            // 50%概率触发事件
            if (Random.value >= 0.50f)
            {
                // 无事件，直接标记已清
                tile.isCleared = true;
                var room = FindMatchingRoom(dungeon, tile);
                room.cleared = true;
                return;
            }

            // 根据概率决定事件类型
            float roll = Random.value;
            EmptyRoomEvent eventType;
            if (roll < 0.40f)
                eventType = EmptyRoomEvent.GoldBag;
            else if (roll < 0.60f)
                eventType = EmptyRoomEvent.Consumable;
            else if (roll < 0.80f)
                eventType = EmptyRoomEvent.Trap;
            else if (roll < 0.90f)
                eventType = EmptyRoomEvent.WanderingMerchant;
            else
                eventType = EmptyRoomEvent.MysticPower;

            Debug.Log($"[地牢] 空房间触发随机事件: {eventType}");
            pendingEvent = eventType;
            pendingEventTile = tile;

            switch (eventType)
            {
                case EmptyRoomEvent.GoldBag:
                    HandleEventGoldBag();
                    break;
                case EmptyRoomEvent.Consumable:
                    HandleEventConsumable();
                    break;
                case EmptyRoomEvent.Trap:
                    HandleEventTrap();
                    break;
                case EmptyRoomEvent.WanderingMerchant:
                    HandleEventWanderingMerchant();
                    break;
                case EmptyRoomEvent.MysticPower:
                    HandleEventMysticPower();
                    break;
            }

            RefreshUI();
        }

        /// <summary>事件：发现金币袋 — floor*20~50金</summary>
        private void HandleEventGoldBag()
        {
            var player = GameManager.Instance.Player;
            var dungeon = GameManager.Instance.Dungeon;
            int minGold = dungeon.floor * 20;
            int maxGold = dungeon.floor * 50;
            int goldAmount = Random.Range(minGold, maxGold + 1);

            pendingMessage = $"你在角落发现了一个金币袋！\n大约有 {goldAmount} 枚金币。";

            // 按钮1: 拿走金币
            msgBtnContinueLabel.text = "拿走";
            pendingEventOption1 = () =>
            {
                player.gold += goldAmount;
                MarkEventTileCleared();
                pendingMessage = $"获得 {goldAmount} 金币！";
                msgBtnContinueLabel.text = "继续";
                RefreshUI();
            };

            // 按钮2: 离开（不拿）
            msgBtnOption2Label.text = "离开";
            pendingEventOption2 = () =>
            {
                MarkEventTileCleared();
                msgBtnContinueLabel.text = "继续";
                RefreshUI();
            };
        }

        /// <summary>事件：发现消耗品 — 随机药水/卷轴</summary>
        private void HandleEventConsumable()
        {
            var player = GameManager.Instance.Player;
            var consumables = ConfigManager.Instance.GetItemsByType(ItemType.Consumable);

            if (consumables.Count == 0 || player.bagItems.Count >= player.maxBagSize)
            {
                // 背包满或无消耗品，改为少量金币
                int goldAmount = Random.Range(5, 15) * GameManager.Instance.Dungeon.floor;
                pendingMessage = "你发现了一些散落的物品，\n但背包已满无法携带。\n旁边有几枚金币。";
                msgBtnContinueLabel.text = "拿走金币";
                pendingEventOption1 = () =>
                {
                    player.gold += goldAmount;
                    MarkEventTileCleared();
                    pendingMessage = $"获得 {goldAmount} 金币！";
                    msgBtnContinueLabel.text = "继续";
                    RefreshUI();
                };
                msgBtnOption2Label.text = "离开";
                pendingEventOption2 = () =>
                {
                    MarkEventTileCleared();
                    msgBtnContinueLabel.text = "继续";
                    RefreshUI();
                };
                return;
            }

            var item = consumables[Random.Range(0, consumables.Count)];
            pendingMessage = $"你发现了一瓶 {item.name}！\n{item.desc}";

            msgBtnContinueLabel.text = "拾取";
            pendingEventOption1 = () =>
            {
                player.bagItems.Add(item.id);
                MarkEventTileCleared();
                pendingMessage = $"获得 {item.name}！";
                msgBtnContinueLabel.text = "继续";
                RefreshUI();
            };

            msgBtnOption2Label.text = "丢弃";
            pendingEventOption2 = () =>
            {
                MarkEventTileCleared();
                msgBtnContinueLabel.text = "继续";
                RefreshUI();
            };
        }

        /// <summary>事件：地板陷阱 — 扣除当前HP的10-20%</summary>
        private void HandleEventTrap()
        {
            var player = GameManager.Instance.Player;
            float hpPercent = Random.Range(0.10f, 0.21f); // 10-20%
            int damage = Mathf.Max(1, Mathf.RoundToInt(player.hp * hpPercent));

            pendingMessage = "你踩中了隐藏的地板陷阱！\n尖刺刺穿了你的鞋子。";

            // 按钮1: 忍痛继续
            msgBtnContinueLabel.text = "忍痛继续";
            pendingEventOption1 = () =>
            {
                player.hp = Mathf.Max(1, player.hp - damage);
                MarkEventTileCleared();
                pendingMessage = $"受到 {damage} 点伤害！\n剩余HP: {player.hp}/{player.maxHp}";
                msgBtnContinueLabel.text = "继续";
                RefreshUI();
            };

            // 按钮2: 小心避开（无伤害但无事发生）
            msgBtnOption2Label.text = "小心避开";
            pendingEventOption2 = () =>
            {
                MarkEventTileCleared();
                pendingMessage = "你小心翼翼地绕过了陷阱。";
                msgBtnContinueLabel.text = "继续";
                RefreshUI();
            };
        }

        /// <summary>事件：流浪商人 — 3件随机物品8折</summary>
        private void HandleEventWanderingMerchant()
        {
            var player = GameManager.Instance.Player;
            var dungeon = GameManager.Instance.Dungeon;

            // 从所有非消耗品中随机选3件
            var allItems = new List<ItemConfig>();
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Weapon));
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Armor));
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Accessory));
            allItems.AddRange(ConfigManager.Instance.GetItemsByType(ItemType.Consumable));

            // 随机选取3件
            var shopItems = new List<ItemConfig>();
            for (int i = 0; i < 3 && allItems.Count > 0; i++)
            {
                int idx = Random.Range(0, allItems.Count);
                shopItems.Add(allItems[idx]);
                allItems.RemoveAt(idx);
            }

            if (shopItems.Count == 0)
            {
                // 无物品可卖，改为少量金币
                int goldAmount = Random.Range(10, 25) * dungeon.floor;
                pendingMessage = "一位流浪商人向你打了个招呼，\n但他今天没什么好卖的。\n他给了你一些金币作为路费。";
                msgBtnContinueLabel.text = "感谢";
                pendingEventOption1 = () =>
                {
                    player.gold += goldAmount;
                    MarkEventTileCleared();
                    pendingMessage = $"获得 {goldAmount} 金币！";
                    msgBtnContinueLabel.text = "继续";
                    RefreshUI();
                };
                msgBtnOption2Label.text = "离开";
                pendingEventOption2 = () =>
                {
                    MarkEventTileCleared();
                    msgBtnContinueLabel.text = "继续";
                    RefreshUI();
                };
                return;
            }

            // 构建商品列表文字（8折价格）
            string itemList = "";
            for (int i = 0; i < shopItems.Count; i++)
            {
                int discountPrice = Mathf.RoundToInt(shopItems[i].price * 0.8f);
                itemList += $"\n  {shopItems[i].name} - {discountPrice}金 ({shopItems[i].desc})";
            }

            pendingMessage = $"一位流浪商人出现在你面前！\n所有商品8折优惠：{itemList}";

            // 按钮1: 逛逛（进入购买菜单）
            msgBtnContinueLabel.text = "逛逛";
            pendingEventOption1 = () =>
            {
                // 标记已清
                pendingEventTile.content.itemIds.Clear();
                foreach (var item in shopItems)
                    pendingEventTile.content.itemIds.Add(item.id);
                MarkEventTileCleared();

                // 重新设置事件状态以保持双按钮弹窗
                pendingEvent = EmptyRoomEvent.WanderingMerchant;
                ShowMerchantBuyMenu(shopItems, 0);
            };

            msgBtnOption2Label.text = "不买";
            pendingEventOption2 = () =>
            {
                MarkEventTileCleared();
                msgBtnContinueLabel.text = "继续";
                RefreshUI();
            };
        }

        /// <summary>流浪商人购买菜单：逐个展示商品供购买</summary>
        private void ShowMerchantBuyMenu(List<ItemConfig> items, int startIndex)
        {
            var player = GameManager.Instance.Player;

            // 找到第一个买得起且背包有空间的
            int buyableIdx = -1;
            for (int i = startIndex; i < items.Count; i++)
            {
                int discountPrice = Mathf.RoundToInt(items[i].price * 0.8f);
                if (player.gold >= discountPrice && player.bagItems.Count < player.maxBagSize)
                {
                    buyableIdx = i;
                    break;
                }
            }

            if (buyableIdx < 0)
            {
                pendingMessage = "你没有足够的金币或背包已满。\n商人离去了。";
                msgBtnContinueLabel.text = "继续";
                pendingEvent = null;
                pendingEventOption1 = null;
                pendingEventOption2 = null;
                RefreshUI();
                return;
            }

            var item = items[buyableIdx];
            int price = Mathf.RoundToInt(item.price * 0.8f);
            pendingMessage = $"商人向你推荐：\n{item.name} — {item.desc}\n价格: {price} 金币（你有 {player.gold} 金）";

            msgBtnContinueLabel.text = $"购买({price}金)";
            pendingEventOption1 = () =>
            {
                player.gold -= price;
                player.bagItems.Add(item.id);
                pendingMessage = $"购买了 {item.name}！";
                msgBtnContinueLabel.text = "继续";
                pendingEvent = null;
                pendingEventOption1 = null;
                pendingEventOption2 = null;
                RefreshUI();
            };

            msgBtnOption2Label.text = "不买了";
            pendingEventOption2 = () =>
            {
                pendingMessage = "你谢绝了商人的好意。";
                msgBtnContinueLabel.text = "继续";
                pendingEvent = null;
                pendingEventOption1 = null;
                pendingEventOption2 = null;
                RefreshUI();
            };

            RefreshUI();
        }

        /// <summary>事件：神秘力量 — 全恢复HP+MP 或 +20%经验</summary>
        private void HandleEventMysticPower()
        {
            var player = GameManager.Instance.Player;
            int expBonus = Mathf.RoundToInt(player.GetExpToNextLevel() * 0.20f);

            pendingMessage = "一道神秘的光芒笼罩了你！\n你感受到一股强大而温柔的力量。\n请选择接收的祝福：";

            // 按钮1: 全恢复
            msgBtnContinueLabel.text = "生命恢复";
            pendingEventOption1 = () =>
            {
                player.hp = player.maxHp;
                player.mp = player.maxMp;
                MarkEventTileCleared();
                pendingMessage = $"神秘力量恢复了你的生命力！\nHP: {player.hp}/{player.maxHp}\nMP: {player.mp}/{player.maxMp}";
                msgBtnContinueLabel.text = "继续";
                pendingEvent = null;
                pendingEventOption1 = null;
                RefreshUI();
            };

            // 按钮2: 经验加成
            msgBtnOption2Label.text = $"经验+{expBonus}";
            pendingEventOption2 = () =>
            {
                int oldLevel = player.level;
                player.AddExp(expBonus);
                MarkEventTileCleared();
                string levelUpMsg = player.level > oldLevel ? $"\n恭喜升级！当前 Lv.{player.level}" : "";
                pendingMessage = $"获得 {expBonus} 点经验！{levelUpMsg}";
                msgBtnContinueLabel.text = "继续";
                pendingEvent = null;
                pendingEventOption1 = null;
                RefreshUI();
            };
        }

        /// <summary>待确认的出口层数（非0表示有出口待确认）</summary>
        private int pendingExitFloor;
        private int pendingExitMaxFloor;

        /// <summary>确认进入下一层（弹窗点击继续后触发）</summary>
        private void ConfirmEnterNextFloor()
        {
            var player = GameManager.Instance.Player;
            player.dungeonFloor = pendingExitFloor;
            var dungeon = new DungeonData();
            dungeon.GenerateFloor(pendingExitFloor);
            dungeon.maxFloor = pendingExitMaxFloor;
            GameManager.Instance.Dungeon = dungeon;
            GameManager.Instance.SetState(GameState.InDungeon);
            GameManager.Instance.SaveGame();

            pendingMessage = null;
            pendingExitFloor = 0;
        }

        /// <summary>
        /// 滚轮缩放 + 键盘移动（WASD/方向键）
        /// </summary>
        private void Update()
        {
            HandleScrollZoom();
            HandleKeyboardMove();
        }

        private void HandleScrollZoom()
        {
            if (mapViewport == null || !mapViewport.activeInHierarchy) return;

            // 检测鼠标是否在地图视口范围内
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                mapViewport.transform as RectTransform, Input.mousePosition))
                return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.01f) return;

            float oldSize = cellSize;
            if (scroll > 0)
                cellSize = Mathf.Min(cellSize + ZoomStep, MaxCellSize);
            else
                cellSize = Mathf.Max(cellSize - ZoomStep, MinCellSize);

            // 没有变化则跳过
            if (Mathf.Approximately(oldSize, cellSize)) return;

            // 刷新地图布局
            var dungeon = GameManager.Instance.Dungeon;
            if (dungeon != null && dungeon.grid != null)
            {
                UpdateGridMap(dungeon);
            }
        }

        /// <summary>键盘移动：WASD/方向键，检查连通方向后移动</summary>
        private void HandleKeyboardMove()
        {
            if (isMoving) return;

            // 面板隐藏时（CanvasGroup alpha=0）不响应
            if (cachedCanvasGroup != null && cachedCanvasGroup.alpha < 0.5f) return;

            var dungeon = GameManager.Instance.Dungeon;
            if (dungeon == null || dungeon.grid == null) return;

            // 消息覆盖层打开时不响应键盘移动
            if (!string.IsNullOrEmpty(pendingMessage)) return;

            var currentTile = dungeon.CurrentTile;
            if (currentTile == null) return;

            // 读取方向输入（WASD + 方向键）
            int dx = 0, dy = 0;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dy = -1;    // 上（y-1）
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dy = 1;  // 下（y+1）
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dx = -1; // 左（x-1）
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dx = 1; // 右（x+1）
            else return; // 无方向键按下

            // 检查当前瓦片对应方向的连通性
            bool connected = false;
            if (dy == -1) connected = currentTile.connectUp;
            else if (dy == 1) connected = currentTile.connectDown;
            else if (dx == -1) connected = currentTile.connectLeft;
            else if (dx == 1) connected = currentTile.connectRight;

            if (!connected) return;

            int targetX = dungeon.currentPos.x + dx;
            int targetY = dungeon.currentPos.y + dy;

            // 边界检查
            if (!dungeon.IsInBounds(targetX, targetY)) return;

            TryMoveTo(dungeon, targetX, targetY);
        }
    }
}
