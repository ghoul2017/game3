using System.Collections;
using System.Collections.Generic;
using Game.Battle;
using Game.Config;
using Game.Core;
using Game.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// GameHUD 框架 — 永久存在的固定布局
    /// 左侧游戏世界区(stretch-fill) + 右侧面板(260px) + 底部技能栏(70px)
    /// 挂载到 HUD Canvas，DontDestroyOnLoad
    /// </summary>
    public class GameHUD : UIPanel
    {
        private static GameHUD _instance;
        /// <summary>运行时单例，由 Create() 设置</summary>
        public static GameHUD Instance => _instance;

        // ============ 布局常量 ============
        private const float RIGHT_PANEL_WIDTH = 260f;
        private const float BOTTOM_BAR_HEIGHT = 70f;
        private const float COMBAT_LOG_HEIGHT = 180f;
        private const float CHAR_INFO_HEIGHT = 140f;
        private const int MAX_SKILL_SLOTS = 6;
        private const float SKILL_SLOT_W = 130f;
        private const float SKILL_SLOT_H = 55f;
        private const float SKILL_SLOT_GAP = 6f;

        // 战斗日志对象池
        private const int MAX_LOG_LINES = 12;

        // 背包对象池
        private const int MAX_BAG_LINES = 20;
        private const float BAG_ROW_HEIGHT = 22f;
        private const float BAG_ROW_GAP = 2f;

        // ============ 组件引用 ============
        private RectTransform gameWorldContainer;
        private RectTransform rightPanel;

        // 战斗日志
        private readonly Text[] logTexts = new Text[MAX_LOG_LINES];
        private ScrollRect logScrollRect;

        // 角色信息
        private Text charNameText;
        private Slider hpSlider;
        private Text hpText;
        private Slider mpSlider;
        private Text mpText;
        private Text statsText;    // ATK/DEF/SPD 一行
        private Slider expSlider;
        private Text expText;

        // 背包
        private Text goldText;
        private Text emptyBagText;
        private ScrollRect bagScrollRect;
        private readonly List<BagRow> bagRows = new List<BagRow>(MAX_BAG_LINES);

        // 技能栏
        private readonly SkillSlot[] skillSlots = new SkillSlot[MAX_SKILL_SLOTS];
        private int _selectedSkillIndex = -1;

        // 遗忘确认弹窗
        private GameObject forgetOverlay;
        private Text forgetMsgText;
        private Button forgetConfirmBtn;
        private int pendingForgetSlotIndex = -1;

        // 城镇地图
        private TownMapController townMapController;

        /// <summary>技能槽结构体</summary>
        private struct SkillSlot
        {
            public GameObject obj;
            public Image bgImage;
            public Image borderImage;
            public Text nameText;
            public Text mpText;
        }

        /// <summary>背包行结构体</summary>
        private struct BagRow
        {
            public GameObject obj;
            public Image bg;
            public Text nameText;
            public Button actionBtn;
            public Text actionBtnLabel;
            public Button sellBtn;
            public Text sellBtnLabel;
        }

        // ============ 生命周期 ============

        protected override void BindReferences()
        {
            BuildLayout();
        }

        protected override void BindEvents()
        {
            // 技能栏点击事件在 BuildSkillSlot 中绑定
        }

        public override void OnShow()
        {
            RefreshAll();
        }

        public override void OnHide()
        {
            // HUD 隐藏时清理（一般不隐藏）
        }

        // ============ 布局构建 ============

        private void BuildLayout()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // 全屏背景
            var bgObj = UIFactory.CreateUI(root, "Bg");
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = UIColors.DarkBg;
            UIBuilder.SetStretch(bgObj);

            BuildRightPanel(root);
            BuildBottomSkillBar(root);
            BuildGameWorldContainer(root);

            // 初始隐藏，等 GameState 切换到 InTown/InDungeon 再显示
            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 左侧游戏世界容器 — stretch-fill，右部留出260px，底部留出70px
        /// </summary>
        private void BuildGameWorldContainer(RectTransform parent)
        {
            var obj = UIFactory.CreateUI(parent, "GameWorldContainer");
            var img = obj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // 透明，仅作容器
            img.raycastTarget = false;
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(0, BOTTOM_BAR_HEIGHT);     // 底部留空
            rt.offsetMax = new Vector2(-RIGHT_PANEL_WIDTH, 0);     // 右部留空
            gameWorldContainer = rt;
        }

        /// <summary>
        /// 右侧面板(260px宽) — 三段：战斗日志(180px) + 角色信息(140px) + 背包(填充)
        /// </summary>
        private void BuildRightPanel(RectTransform parent)
        {
            // 右侧面板容器
            var panelObj = UIFactory.CreateUI(parent, "RightPanel");
            var panelImg = panelObj.AddComponent<Image>();
            panelImg.color = UIColors.PanelBg;
            var panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 0f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 0.5f);
            panelRt.offsetMin = new Vector2(-RIGHT_PANEL_WIDTH, 0);
            panelRt.offsetMax = new Vector2(0, 0);
            rightPanel = panelRt;

            // 左边框线
            var borderObj = UIFactory.CreateUI(panelObj.transform, "LeftBorder");
            var borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.20f);
            var borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = new Vector2(0f, 0f);
            borderRt.anchorMax = new Vector2(0f, 1f);
            borderRt.pivot = new Vector2(0f, 0.5f);
            borderRt.sizeDelta = new Vector2(2, 0);
            borderRt.anchoredPosition = Vector2.zero;

            BuildCombatLogSection(panelObj.transform);
            BuildCharInfoSection(panelObj.transform);
            BuildBagSection(panelObj.transform);
        }

        /// <summary>
        /// 战斗日志区 — 固定180px高，顶部，ScrollRect + Mask + 12行Text对象池
        /// </summary>
        private void BuildCombatLogSection(Transform parent)
        {
            float sectionHeight = COMBAT_LOG_HEIGHT;
            float padding = 4f;

            // 区块容器
            var sectionObj = UIFactory.CreateUI(parent, "CombatLogSection");
            var sectionRt = sectionObj.GetComponent<RectTransform>();
            sectionRt.anchorMin = new Vector2(0f, 1f);
            sectionRt.anchorMax = new Vector2(1f, 1f);
            sectionRt.pivot = new Vector2(0.5f, 1f);
            sectionRt.offsetMin = new Vector2(0, -sectionHeight);
            sectionRt.offsetMax = new Vector2(0, 0);

            // 区块背景
            var bgObj = UIFactory.CreateUI(sectionObj.transform, "Bg");
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = UIColors.CardBg;
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(padding, padding);
            bgRt.offsetMax = new Vector2(-padding, -padding);

            // 标题
            var titleObj = UIFactory.CreateUI(bgObj.transform, "Title");
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0, 24);
            titleRt.anchoredPosition = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "战斗日志";
            titleText.fontSize = 16;
            titleText.color = UIColors.Gold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 分隔线
            var divObj = UIFactory.CreateUI(bgObj.transform, "Divider");
            var divImg = divObj.AddComponent<Image>();
            divImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.15f);
            var divRt = divObj.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0f, 1f);
            divRt.anchorMax = new Vector2(1f, 1f);
            divRt.pivot = new Vector2(0.5f, 1f);
            divRt.sizeDelta = new Vector2(0, 1);
            divRt.anchoredPosition = new Vector2(0, -24);

            // Viewport（含 Mask + ScrollRect）
            var viewportObj = UIFactory.CreateUI(bgObj.transform, "Viewport");
            var viewportImg = viewportObj.AddComponent<Image>();
            viewportImg.color = UIColors.ViewportBg;
            viewportImg.raycastTarget = true;
            var viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.anchorMin = new Vector2(0f, 0f);
            viewportRt.anchorMax = new Vector2(1f, 1f);
            viewportRt.offsetMin = new Vector2(4, 4);
            viewportRt.offsetMax = new Vector2(-4, -28);

            // Content（日志行的容器）
            float lineHeight = 14f;
            float lineGap = 2f;
            float contentHeight = MAX_LOG_LINES * (lineHeight + lineGap);
            var contentObj = UIFactory.CreateUI(viewportObj.transform, "Content");
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, contentHeight);
            contentRt.anchoredPosition = Vector2.zero;

            // 12行Text对象池
            for (int i = 0; i < MAX_LOG_LINES; i++)
            {
                var lineObj = UIFactory.CreateUI(contentObj.transform, $"Log_{i}");
                var lineRt = lineObj.GetComponent<RectTransform>();
                lineRt.anchorMin = new Vector2(0f, 1f);
                lineRt.anchorMax = new Vector2(1f, 1f);
                lineRt.pivot = new Vector2(0f, 1f);
                lineRt.sizeDelta = new Vector2(0, lineHeight);
                lineRt.anchoredPosition = new Vector2(0, -i * (lineHeight + lineGap));

                var logText = lineObj.AddComponent<Text>();
                logText.text = "";
                logText.fontSize = 12;
                logText.color = UIColors.TextDim;
                logText.alignment = TextAnchor.LowerLeft;
                logText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                logText.supportRichText = true;
                logText.horizontalOverflow = HorizontalWrapMode.Wrap;
                logText.verticalOverflow = VerticalWrapMode.Truncate;
                lineObj.SetActive(false);
                logTexts[i] = logText;
            }

            // ScrollRect
            logScrollRect = bgObj.AddComponent<ScrollRect>();
            logScrollRect.content = contentRt;
            logScrollRect.viewport = viewportRt;
            logScrollRect.horizontal = false;
            logScrollRect.vertical = true;
            logScrollRect.movementType = ScrollRect.MovementType.Clamped;
            logScrollRect.scrollSensitivity = 20f;
            logScrollRect.elasticity = 0.1f;
        }

        /// <summary>
        /// 角色信息区 — 固定140px高，战斗日志下方，5行精炼布局
        /// 第1行：名Lv职 | 第2行：HP条 | 第3行：MP条 | 第4行：ATK/DEF/SPD | 第5行：EXP条
        /// </summary>
        private void BuildCharInfoSection(Transform parent)
        {
            float offsetY = COMBAT_LOG_HEIGHT;
            float sectionHeight = CHAR_INFO_HEIGHT;
            float padding = 4f;
            float leftMargin = 8f;
            float rightMargin = 8f;
            float barHeight = 12f;

            // 区块容器
            var sectionObj = UIFactory.CreateUI(parent, "CharInfoSection");
            var sectionRt = sectionObj.GetComponent<RectTransform>();
            sectionRt.anchorMin = new Vector2(0f, 1f);
            sectionRt.anchorMax = new Vector2(1f, 1f);
            sectionRt.pivot = new Vector2(0.5f, 1f);
            sectionRt.offsetMin = new Vector2(0, -(offsetY + sectionHeight));
            sectionRt.offsetMax = new Vector2(0, -offsetY);

            // 区块背景
            var bgObj = UIFactory.CreateUI(sectionObj.transform, "Bg");
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = UIColors.CardBg;
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(padding, padding);
            bgRt.offsetMax = new Vector2(-padding, -padding);

            // 标题
            var titleObj = UIFactory.CreateUI(bgObj.transform, "Title");
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0, 20);
            titleRt.anchoredPosition = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "角色";
            titleText.fontSize = 14;
            titleText.color = UIColors.Gold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 分隔线
            var divObj = UIFactory.CreateUI(bgObj.transform, "Divider");
            var divImg = divObj.AddComponent<Image>();
            divImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.15f);
            var divRt = divObj.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0f, 1f);
            divRt.anchorMax = new Vector2(1f, 1f);
            divRt.pivot = new Vector2(0.5f, 1f);
            divRt.sizeDelta = new Vector2(0, 1);
            divRt.anchoredPosition = new Vector2(0, -20);

            // ---- 第1行：名字 LvXX 职业 (y: -22 ~ -38) ----
            var nameObj = UIFactory.CreateUI(bgObj.transform, "CharName");
            var nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.offsetMin = new Vector2(leftMargin, -38);
            nameRt.offsetMax = new Vector2(-rightMargin, -22);
            charNameText = nameObj.AddComponent<Text>();
            charNameText.text = "--";
            charNameText.fontSize = 13;
            charNameText.color = UIColors.TextWhite;
            charNameText.alignment = TextAnchor.MiddleLeft;
            charNameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // ---- 第2行：HP条 (y: -40 ~ -54) ----
            BuildBarWithLabel(bgObj.transform, "HPBar", UIColors.Red, -40, -54, leftMargin, rightMargin, barHeight,
                out hpSlider, out hpText);

            // ---- 第3行：MP条 (y: -56 ~ -70) ----
            BuildBarWithLabel(bgObj.transform, "MPBar", UIColors.Blue, -56, -70, leftMargin, rightMargin, barHeight,
                out mpSlider, out mpText);

            // ---- 第4行：ATK/DEF/SPD 一行 (y: -72 ~ -88) ----
            var statsObj = UIFactory.CreateUI(bgObj.transform, "Stats");
            var statsRt = statsObj.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0f, 1f);
            statsRt.anchorMax = new Vector2(1f, 1f);
            statsRt.pivot = new Vector2(0f, 1f);
            statsRt.offsetMin = new Vector2(leftMargin, -88);
            statsRt.offsetMax = new Vector2(-rightMargin, -72);
            statsText = statsObj.AddComponent<Text>();
            statsText.text = "ATK:0 DEF:0 SPD:0";
            statsText.fontSize = 12;
            statsText.color = UIColors.TextDim;
            statsText.alignment = TextAnchor.MiddleLeft;
            statsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // ---- 第5行：EXP条 (y: -90 ~ -104) ----
            BuildBarWithLabel(bgObj.transform, "EXPBar", UIColors.ExpColor, -90, -104, leftMargin, rightMargin, barHeight,
                out expSlider, out expText);
        }

        /// <summary>创建带居中文字标签的Slider条</summary>
        private void BuildBarWithLabel(Transform parent, string barName, Color fillColor,
            float topOffset, float bottomOffset, float leftMargin, float rightMargin, float barHeight,
            out Slider slider, out Text labelText)
        {
            var barObj = UIFactory.CreateUI(parent, barName);
            var barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0f, 1f);
            barRt.offsetMin = new Vector2(leftMargin, bottomOffset);
            barRt.offsetMax = new Vector2(-rightMargin, topOffset);

            var bgImg = barObj.AddComponent<Image>();
            bgImg.color = UIColors.SliderBg;

            var fillObj = UIFactory.CreateUI(barObj.transform, "Fill");
            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(1, 1);
            fillRt.offsetMax = new Vector2(-1, -1);
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = fillColor;

            slider = barObj.AddComponent<Slider>();
            slider.interactable = false;
            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;
            slider.value = 1f;

            var labelObj = UIFactory.CreateUI(barObj.transform, "Label");
            UIBuilder.SetStretch(labelObj);
            labelText = labelObj.AddComponent<Text>();
            labelText.text = "0/0";
            labelText.fontSize = 10;
            labelText.color = UIColors.TextWhite;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// <summary>
        /// 背包区 — 填充剩余空间（右侧面板底部）
        /// ScrollRect + Mask 可滚动，20行对象池(22px紧凑行高)，顶部金币显示
        /// </summary>
        private void BuildBagSection(Transform parent)
        {
            float offsetY = COMBAT_LOG_HEIGHT + CHAR_INFO_HEIGHT;
            float padding = 4f;
            float titleHeight = 20f;
            float goldBarHeight = 18f;

            // 区块容器
            var sectionObj = UIFactory.CreateUI(parent, "BagSection");
            var sectionRt = sectionObj.GetComponent<RectTransform>();
            sectionRt.anchorMin = new Vector2(0f, 0f);
            sectionRt.anchorMax = new Vector2(1f, 1f);
            sectionRt.pivot = new Vector2(0.5f, 1f);
            sectionRt.offsetMin = new Vector2(0, 0);
            sectionRt.offsetMax = new Vector2(0, -offsetY);

            // 区块背景
            var bgObj = UIFactory.CreateUI(sectionObj.transform, "Bg");
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = UIColors.CardBg;
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(padding, padding);
            bgRt.offsetMax = new Vector2(-padding, -padding);

            // 标题
            var titleObj = UIFactory.CreateUI(bgObj.transform, "Title");
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0, titleHeight);
            titleRt.anchoredPosition = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "背包";
            titleText.fontSize = 14;
            titleText.color = UIColors.Gold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 分隔线
            var divObj = UIFactory.CreateUI(bgObj.transform, "Divider");
            var divImg = divObj.AddComponent<Image>();
            divImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.15f);
            var divRt = divObj.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0f, 1f);
            divRt.anchorMax = new Vector2(1f, 1f);
            divRt.pivot = new Vector2(0.5f, 1f);
            divRt.sizeDelta = new Vector2(0, 1);
            divRt.anchoredPosition = new Vector2(0, -titleHeight);

            // 金币栏
            var goldObj = UIFactory.CreateUI(bgObj.transform, "GoldBar");
            var goldRt = goldObj.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0f, 1f);
            goldRt.anchorMax = new Vector2(1f, 1f);
            goldRt.pivot = new Vector2(0.5f, 1f);
            goldRt.offsetMin = new Vector2(6, -(titleHeight + 1 + goldBarHeight));
            goldRt.offsetMax = new Vector2(-6, -(titleHeight + 1));
            goldText = goldObj.AddComponent<Text>();
            goldText.text = "金币: 0";
            goldText.fontSize = 12;
            goldText.color = UIColors.Orange;
            goldText.alignment = TextAnchor.MiddleCenter;
            goldText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 空背包提示
            var emptyObj = UIFactory.CreateUI(bgObj.transform, "EmptyBag");
            var emptyRt = emptyObj.GetComponent<RectTransform>();
            emptyRt.anchorMin = Vector2.zero;
            emptyRt.anchorMax = Vector2.one;
            emptyRt.offsetMin = Vector2.zero;
            emptyRt.offsetMax = Vector2.zero;
            emptyBagText = emptyObj.AddComponent<Text>();
            emptyBagText.text = "背包是空的";
            emptyBagText.fontSize = 13;
            emptyBagText.color = UIColors.TextDim;
            emptyBagText.alignment = TextAnchor.MiddleCenter;
            emptyBagText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            emptyObj.SetActive(false);

            // Viewport（含 Mask + ScrollRect）
            float topOffset = titleHeight + 1 + goldBarHeight + 2;
            var viewportObj = UIFactory.CreateUI(bgObj.transform, "Viewport");
            var viewportImg = viewportObj.AddComponent<Image>();
            viewportImg.color = UIColors.ViewportBg;
            viewportImg.raycastTarget = true;
            var viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.anchorMin = new Vector2(0f, 0f);
            viewportRt.anchorMax = new Vector2(1f, 1f);
            viewportRt.offsetMin = new Vector2(2, 2);
            viewportRt.offsetMax = new Vector2(-2, -topOffset);

            // Content（背包行容器）
            float contentHeight = MAX_BAG_LINES * (BAG_ROW_HEIGHT + BAG_ROW_GAP);
            var contentObj = UIFactory.CreateUI(viewportObj.transform, "Content");
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0, contentHeight);
            contentRt.anchoredPosition = Vector2.zero;

            // 20行对象池（紧凑22px行高）
            for (int i = 0; i < MAX_BAG_LINES; i++)
            {
                float y = -i * (BAG_ROW_HEIGHT + BAG_ROW_GAP);
                var row = CreateBagRow(contentObj.transform, y);
                row.obj.SetActive(false);
                bagRows.Add(row);
            }

            // ScrollRect
            bagScrollRect = bgObj.AddComponent<ScrollRect>();
            bagScrollRect.content = contentRt;
            bagScrollRect.viewport = viewportRt;
            bagScrollRect.horizontal = false;
            bagScrollRect.vertical = true;
            bagScrollRect.movementType = ScrollRect.MovementType.Clamped;
            bagScrollRect.scrollSensitivity = 16f;
            bagScrollRect.elasticity = 0.1f;
        }

        /// <summary>创建单行背包行（22px高，含名称+操作按钮+出售按钮）</summary>
        private BagRow CreateBagRow(Transform parent, float y)
        {
            var obj = UIFactory.CreateUI(parent, "BagRow");
            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(0, BAG_ROW_HEIGHT);
            rt.anchoredPosition = new Vector2(0, y);

            var bg = obj.AddComponent<Image>();
            bg.color = UIColors.RowEven;
            bg.raycastTarget = true;

            // 名称文字（左侧区域）
            var nameObj = UIFactory.CreateUI(obj.transform, "NameText");
            var nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(0.55f, 1f);
            nameRt.offsetMin = new Vector2(4, 0);
            nameRt.offsetMax = new Vector2(-2, 0);
            var nameText = nameObj.AddComponent<Text>();
            nameText.text = "";
            nameText.fontSize = 11;
            nameText.color = UIColors.TextWhite;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;

            // 操作按钮（右侧第一个）
            var (actionBtn, actionBtnLabel) = UIFactory.CreateButtonWithRef(obj.transform, "", 48, 18,
                null, 11, "ActionBtn");
            var actionRt = actionBtn.GetComponent<RectTransform>();
            actionRt.anchorMin = new Vector2(0.55f, 0.5f);
            actionRt.anchorMax = new Vector2(0.55f, 0.5f);
            actionRt.pivot = new Vector2(0f, 0.5f);
            actionRt.sizeDelta = new Vector2(48, 18);
            actionRt.anchoredPosition = new Vector2(0, 0);

            // 出售按钮（右侧第二个）
            var (sellBtn, sellBtnLabel) = UIFactory.CreateButtonWithRef(obj.transform, "", 44, 18,
                null, 10, "SellBtn");
            var sellRt = sellBtn.GetComponent<RectTransform>();
            sellRt.anchorMin = new Vector2(1f, 0.5f);
            sellRt.anchorMax = new Vector2(1f, 0.5f);
            sellRt.pivot = new Vector2(1f, 0.5f);
            sellRt.sizeDelta = new Vector2(44, 18);
            sellRt.anchoredPosition = new Vector2(-2, 0);

            return new BagRow
            {
                obj = obj, bg = bg,
                nameText = nameText,
                actionBtn = actionBtn, actionBtnLabel = actionBtnLabel,
                sellBtn = sellBtn, sellBtnLabel = sellBtnLabel
            };
        }

        /// <summary>
        /// 底部技能栏 — 70px高，6个技能槽(130x55px)，居中排列
        /// </summary>
        private void BuildBottomSkillBar(RectTransform parent)
        {
            // 技能栏容器
            var barObj = UIFactory.CreateUI(parent, "BottomSkillBar");
            var barImg = barObj.AddComponent<Image>();
            barImg.color = UIColors.PanelBg;
            var barRt = barObj.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.offsetMin = new Vector2(0, 0);
            barRt.offsetMax = new Vector2(0, BOTTOM_BAR_HEIGHT);

            // 顶部边框线
            var topBorderObj = UIFactory.CreateUI(barObj.transform, "TopBorder");
            var topBorderImg = topBorderObj.AddComponent<Image>();
            topBorderImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.20f);
            var topBorderRt = topBorderObj.GetComponent<RectTransform>();
            topBorderRt.anchorMin = new Vector2(0f, 1f);
            topBorderRt.anchorMax = new Vector2(1f, 1f);
            topBorderRt.pivot = new Vector2(0.5f, 1f);
            topBorderRt.sizeDelta = new Vector2(0, 2);
            topBorderRt.anchoredPosition = Vector2.zero;

            // 6个技能槽居中排列（减去右侧面板宽度）
            float totalWidth = MAX_SKILL_SLOTS * SKILL_SLOT_W + (MAX_SKILL_SLOTS - 1) * SKILL_SLOT_GAP;
            float startX = -totalWidth / 2f + SKILL_SLOT_W / 2f - RIGHT_PANEL_WIDTH / 2f;

            for (int i = 0; i < MAX_SKILL_SLOTS; i++)
            {
                float x = startX + i * (SKILL_SLOT_W + SKILL_SLOT_GAP);
                BuildSkillSlot(barObj.transform, i, x);
            }

            // 遗忘确认弹窗（初始隐藏）
            BuildForgetOverlay(barObj.transform);
        }

        private void BuildSkillSlot(Transform parent, int index, float xPos)
        {
            var slotObj = UIFactory.CreateUI(parent, $"SkillSlot_{index}");
            var slotRt = slotObj.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0.5f, 0.5f);
            slotRt.anchorMax = new Vector2(0.5f, 0.5f);
            slotRt.pivot = new Vector2(0.5f, 0.5f);
            slotRt.sizeDelta = new Vector2(SKILL_SLOT_W, SKILL_SLOT_H);
            slotRt.anchoredPosition = new Vector2(xPos, 0);

            // 槽位背景（同时作为Button的目标图形）
            var bgImg = slotObj.AddComponent<Image>();
            bgImg.color = UIColors.CardBg;

            // 边框（选中时变为金色）
            var borderObj = UIFactory.CreateUI(slotObj.transform, "Border");
            var borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.30f);
            var borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-1, -1);
            borderRt.offsetMax = new Vector2(1, 1);

            // 技能名称（上半区域）
            var nameObj = UIFactory.CreateUI(slotObj.transform, "SkillName");
            var nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.45f);
            nameRt.anchorMax = new Vector2(1f, 0.95f);
            nameRt.offsetMin = new Vector2(4, 0);
            nameRt.offsetMax = new Vector2(-4, 0);
            var nameText = nameObj.AddComponent<Text>();
            nameText.text = "";
            nameText.fontSize = 13;
            nameText.color = UIColors.TextWhite;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // MP消耗（下半区域）
            var mpObj = UIFactory.CreateUI(slotObj.transform, "MpCost");
            var mpRt = mpObj.GetComponent<RectTransform>();
            mpRt.anchorMin = new Vector2(0f, 0.05f);
            mpRt.anchorMax = new Vector2(1f, 0.45f);
            mpRt.offsetMin = new Vector2(4, 0);
            mpRt.offsetMax = new Vector2(-4, 0);
            var mpText = mpObj.AddComponent<Text>();
            mpText.text = "";
            mpText.fontSize = 11;
            mpText.color = UIColors.Blue;
            mpText.alignment = TextAnchor.MiddleCenter;
            mpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // EventTrigger（同时处理左键和右键）
            var trigger = slotObj.AddComponent<EventTrigger>();
            int slotIndex = index;

            // 左键点击：选中/取消
            var leftEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            leftEntry.callback.AddListener((data) =>
            {
                var ped = data as PointerEventData;
                if (ped != null && ped.button == PointerEventData.InputButton.Left)
                    OnSkillSlotLeftClick(slotIndex);
                else if (ped != null && ped.button == PointerEventData.InputButton.Right)
                    OnSkillSlotRightClick(slotIndex);
            });
            trigger.triggers.Add(leftEntry);

            skillSlots[index] = new SkillSlot
            {
                obj = slotObj,
                bgImage = bgImg,
                borderImage = borderImg,
                nameText = nameText,
                mpText = mpText
            };
        }

        /// <summary>
        /// 遗忘确认弹窗 — 半透明遮罩 + 提示文字 + 确认/取消按钮
        /// </summary>
        private void BuildForgetOverlay(Transform parent)
        {
            var overlayObj = UIFactory.CreateUI(parent, "ForgetOverlay");
            var overlayRt = overlayObj.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            var overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.color = new Color(UIColors.DarkBg.r, UIColors.DarkBg.g, UIColors.DarkBg.b, 0.88f);

            overlayObj.AddComponent<CanvasGroup>();

            // 提示文字
            var msgObj = UIFactory.CreateUI(overlayObj.transform, "MsgText");
            UIBuilder.SetStretch(msgObj);
            var msgRt = msgObj.GetComponent<RectTransform>();
            msgRt.offsetMin = new Vector2(20, 30);
            msgRt.offsetMax = new Vector2(-20, -10);
            forgetMsgText = msgObj.AddComponent<Text>();
            forgetMsgText.text = "";
            forgetMsgText.fontSize = 14;
            forgetMsgText.color = UIColors.Gold;
            forgetMsgText.alignment = TextAnchor.MiddleCenter;
            forgetMsgText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 取消按钮
            var cancelBtn = UIFactory.CreateButton(overlayObj.transform, "取消", 100, 30, null, 14, "BtnCancel");
            var cancelRt = cancelBtn.GetComponent<RectTransform>();
            cancelRt.anchorMin = new Vector2(0.5f, 0f);
            cancelRt.anchorMax = new Vector2(0.5f, 0f);
            cancelRt.pivot = new Vector2(1f, 0f);
            cancelRt.anchoredPosition = new Vector2(-8, 8);
            cancelBtn.onClick.AddListener(() =>
            {
                pendingForgetSlotIndex = -1;
                if (forgetOverlay != null) forgetOverlay.SetActive(false);
            });

            // 确认按钮（金色）
            forgetConfirmBtn = UIFactory.CreateGoldButton(overlayObj.transform, "遗忘", 100, 30, null, 14, "BtnConfirm");
            var confirmRt = forgetConfirmBtn.GetComponent<RectTransform>();
            confirmRt.anchorMin = new Vector2(0.5f, 0f);
            confirmRt.anchorMax = new Vector2(0.5f, 0f);
            confirmRt.pivot = new Vector2(0f, 0f);
            confirmRt.anchoredPosition = new Vector2(8, 8);
            forgetConfirmBtn.onClick.AddListener(OnForgetConfirm);

            forgetOverlay = overlayObj;
            overlayObj.SetActive(false);
        }

        // ============ 技能栏交互 ============

        /// <summary>当前选中的技能槽索引，-1表示无选中</summary>
        public int SelectedSkillIndex => _selectedSkillIndex;

        /// <summary>获取选中技能的ID，无选中返回0</summary>
        public int GetSelectedSkillId()
        {
            if (_selectedSkillIndex < 0) return 0;
            var player = GameManager.Instance?.Player;
            if (player == null) return 0;
            var skills = player.learnedSkills;
            if (_selectedSkillIndex >= skills.Count) return 0;
            return skills[_selectedSkillIndex];
        }

        /// <summary>左键点击技能槽：选中/取消选中</summary>
        private void OnSkillSlotLeftClick(int index)
        {
            var player = GameManager.Instance?.Player;
            if (player == null) return;
            var skills = player.learnedSkills;
            // 空槽位不可选中
            if (index >= skills.Count) return;

            if (_selectedSkillIndex == index)
            {
                // 再次点击取消选中
                _selectedSkillIndex = -1;
            }
            else
            {
                // 选中新槽位，取消旧选中
                _selectedSkillIndex = index;
            }
            UpdateSkillSlotSelection();
        }

        /// <summary>更新技能槽选中高亮状态</summary>
        private void UpdateSkillSlotSelection()
        {
            for (int i = 0; i < MAX_SKILL_SLOTS; i++)
            {
                if (skillSlots[i].borderImage == null) continue;
                bool selected = (i == _selectedSkillIndex);
                skillSlots[i].borderImage.color = selected
                    ? UIColors.Gold
                    : new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.30f);
            }
        }

        /// <summary>右键弹出遗忘确认</summary>
        private void OnSkillSlotRightClick(int index)
        {
            var player = GameManager.Instance?.Player;
            if (player == null) return;
            var skills = player.learnedSkills;
            if (index >= skills.Count) return;

            var skillCfg = ConfigManager.Instance.GetSkill(skills[index]);
            if (skillCfg == null) return;

            pendingForgetSlotIndex = index;
            forgetMsgText.text = $"确认遗忘技能 [{skillCfg.name}]？";
            forgetOverlay.SetActive(true);
        }

        /// <summary>确认遗忘技能</summary>
        private void OnForgetConfirm()
        {
            if (pendingForgetSlotIndex < 0) return;
            var player = GameManager.Instance?.Player;
            if (player == null) return;
            var skills = player.learnedSkills;
            if (pendingForgetSlotIndex < skills.Count)
            {
                int skillId = skills[pendingForgetSlotIndex];
                player.ForgetSkill(skillId);
                // 如果遗忘的是选中技能，清除选中
                if (_selectedSkillIndex == pendingForgetSlotIndex)
                    _selectedSkillIndex = -1;
                else if (_selectedSkillIndex > pendingForgetSlotIndex)
                    _selectedSkillIndex--; // 索引前移
            }
            pendingForgetSlotIndex = -1;
            forgetOverlay.SetActive(false);
            RefreshSkillBar();
        }

        // ============ 公开刷新方法 ============

        /// <summary>刷新所有区域</summary>
        public void RefreshAll()
        {
            RefreshCombatLog();
            RefreshCharInfo();
            RefreshBag();
            RefreshSkillBar();
        }

        /// <summary>
        /// 刷新战斗日志 — 从 BattleManager 读取日志，使用对象池12行显示，颜色标记
        /// </summary>
        public void RefreshCombatLog()
        {
            var b = BattleManager.Instance?.CurrentBattle;
            var log = b?.log;
            int logCount = log != null ? log.Count : 0;

            int showCount = Mathf.Min(logCount, MAX_LOG_LINES);
            int startIdx = logCount - showCount;

            for (int i = 0; i < MAX_LOG_LINES; i++)
            {
                if (logTexts[i] == null) continue;
                if (i < showCount)
                {
                    logTexts[i].text = BattleLogFormatter.ColorizeLog(log[startIdx + i]);
                    logTexts[i].gameObject.SetActive(true);
                }
                else
                {
                    logTexts[i].gameObject.SetActive(false);
                }
            }

            // 自动滚动到底部
            if (logScrollRect != null && showCount > 0)
            {
                logScrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }

        /// <summary>
        /// 刷新角色信息 — 从 GameManager.Player 读取，5行精炼布局
        /// </summary>
        public void RefreshCharInfo()
        {
            if (charNameText == null) return;

            var player = GameManager.Instance?.Player;
            if (player == null)
            {
                charNameText.text = "--";
                hpSlider.value = 0f;
                hpText.text = "0/0";
                mpSlider.value = 0f;
                mpText.text = "0/0";
                statsText.text = "ATK:0 DEF:0 SPD:0";
                expSlider.value = 0f;
                expText.text = "0/0";
                return;
            }

            // 第1行：名字 LvXX 职业
            string profName = GetProfessionDisplayName(player.profession);
            charNameText.text = $"{player.name}  Lv.{player.level}  {profName}";

            // 第2行：HP条
            hpSlider.value = player.maxHp > 0 ? (float)player.hp / player.maxHp : 0f;
            hpText.text = $"{player.hp}/{player.maxHp}";

            // 第3行：MP条
            mpSlider.value = player.maxMp > 0 ? (float)player.mp / player.maxMp : 0f;
            mpText.text = $"{player.mp}/{player.maxMp}";

            // 第4行：ATK/DEF/SPD
            statsText.text = $"ATK:{player.attack}  DEF:{player.defense}  SPD:{player.speed}";

            // 第5行：EXP条
            int expToNext = player.GetExpToNextLevel();
            float expRatio = expToNext > 0 ? (float)player.exp / expToNext : 0f;
            expSlider.value = expRatio;
            expText.text = $"{player.exp}/{expToNext}";
        }

        /// <summary>获取职业显示名称</summary>
        private static string GetProfessionDisplayName(Profession prof)
        {
            return prof switch
            {
                Profession.Warrior => "战士",
                Profession.Ranger => "游侠",
                Profession.Knight => "骑士",
                Profession.Priest => "牧师",
                Profession.Mage => "法师",
                _ => prof.ToString()
            };
        }

        /// <summary>刷新背包 — 读取 bagItems 按 ID 分组，显示 [类型标]名称 x数量 [操作按钮]</summary>
        public void RefreshBag()
        {
            var player = GameManager.Instance?.Player;

            // 金币显示
            if (goldText != null)
            {
                int gold = player != null ? player.gold : 0;
                goldText.text = $"金币: {gold}  ({player?.bagItems.Count ?? 0}/{player?.maxBagSize ?? 20})";
            }

            if (player == null)
            {
                for (int i = 0; i < bagRows.Count; i++)
                    if (bagRows[i].obj != null) bagRows[i].obj.SetActive(false);
                if (emptyBagText != null) emptyBagText.gameObject.SetActive(true);
                return;
            }

            // 按 ID 分组
            var itemCounts = new Dictionary<int, int>();
            foreach (var itemId in player.bagItems)
            {
                if (!itemCounts.ContainsKey(itemId)) itemCounts[itemId] = 0;
                itemCounts[itemId]++;
            }

            int idx = 0;
            foreach (var kv in itemCounts)
            {
                if (idx >= MAX_BAG_LINES) break;
                var itemCfg = ConfigManager.Instance.GetItem(kv.Key);
                if (itemCfg == null) continue;

                var row = bagRows[idx];
                if (row.obj == null) { idx++; continue; }
                row.obj.SetActive(true);

                // 交替行色
                if (row.bg != null) row.bg.color = idx % 2 == 0 ? UIColors.RowEven : UIColors.RowOdd;

                // 类型标签
                string typeTag = itemCfg.type switch
                {
                    ItemType.Weapon => "武器",
                    ItemType.Armor => "防具",
                    ItemType.Accessory => "饰品",
                    ItemType.Consumable => "消耗",
                    ItemType.Treasure => "宝物",
                    _ => ""
                };

                // 名称（含数量）
                string display = kv.Value > 1 ? $"[{typeTag}]{itemCfg.name} x{kv.Value}" : $"[{typeTag}]{itemCfg.name}";
                if (row.nameText != null) row.nameText.text = display;

                // 操作按钮
                int capturedId = kv.Key;
                bool isEquipType = itemCfg.type == ItemType.Weapon || itemCfg.type == ItemType.Armor || itemCfg.type == ItemType.Accessory;

                if (isEquipType)
                {
                    bool isEquipped = (itemCfg.type == ItemType.Weapon && player.weaponId == capturedId)
                        || (itemCfg.type == ItemType.Armor && player.armorId == capturedId)
                        || (itemCfg.type == ItemType.Accessory && player.accessoryId == capturedId);
                    bool levelOk = player.level >= itemCfg.reqLevel;

                    if (row.actionBtn != null)
                    {
                        row.actionBtn.gameObject.SetActive(true);
                        row.actionBtn.interactable = !isEquipped && levelOk;
                        row.actionBtn.image.color = isEquipped ? UIColors.ButtonDisable : UIColors.ButtonNormal;
                        row.actionBtn.onClick.RemoveAllListeners();
                        if (!isEquipped && levelOk)
                        {
                            int id = capturedId;
                            row.actionBtn.onClick.AddListener(() => EquipItem(id));
                        }
                    }
                    if (row.actionBtnLabel != null)
                    {
                        row.actionBtnLabel.text = isEquipped ? "已装" : (levelOk ? "装备" : $"Lv{itemCfg.reqLevel}");
                        row.actionBtnLabel.color = isEquipped ? UIColors.Green : (levelOk ? UIColors.Gold : UIColors.TextDim);
                        row.actionBtnLabel.fontSize = 10;
                    }
                }
                else if (itemCfg.type == ItemType.Consumable)
                {
                    if (row.actionBtn != null)
                    {
                        row.actionBtn.gameObject.SetActive(true);
                        row.actionBtn.interactable = true;
                        row.actionBtn.image.color = UIColors.ButtonNormal;
                        row.actionBtn.onClick.RemoveAllListeners();
                        int id = capturedId;
                        row.actionBtn.onClick.AddListener(() => UseItem(id));
                    }
                    if (row.actionBtnLabel != null)
                    {
                        row.actionBtnLabel.text = "使用";
                        row.actionBtnLabel.color = UIColors.Gold;
                        row.actionBtnLabel.fontSize = 10;
                    }
                }
                else
                {
                    if (row.actionBtn != null) row.actionBtn.gameObject.SetActive(false);
                }

                // 出售按钮
                int sellPrice = itemCfg.sellPrice > 0 ? itemCfg.sellPrice : itemCfg.price / 2;
                if (row.sellBtn != null)
                {
                    row.sellBtn.gameObject.SetActive(true);
                    row.sellBtn.interactable = true;
                    row.sellBtn.image.color = UIColors.ButtonNormal;
                    row.sellBtn.onClick.RemoveAllListeners();
                    int sId = capturedId;
                    int sPrice = sellPrice;
                    row.sellBtn.onClick.AddListener(() => SellItem(sId, sPrice));
                }
                if (row.sellBtnLabel != null)
                {
                    row.sellBtnLabel.text = $"{sellPrice}金";
                    row.sellBtnLabel.color = UIColors.Orange;
                    row.sellBtnLabel.fontSize = 9;
                }

                idx++;
            }

            // 隐藏多余行
            for (int i = idx; i < bagRows.Count; i++)
            {
                if (bagRows[i].obj != null) bagRows[i].obj.SetActive(false);
            }

            // 空背包提示
            if (emptyBagText != null) emptyBagText.gameObject.SetActive(idx == 0);
        }

        /// <summary>装备物品</summary>
        private void EquipItem(int itemId)
        {
            var player = GameManager.Instance?.Player;
            if (player == null) return;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (item == null) return;
            if (player.level < item.reqLevel) return;

            switch (item.type)
            {
                case ItemType.Weapon:
                    if (player.weaponId != 0) player.bagItems.Add(player.weaponId);
                    player.weaponId = itemId; break;
                case ItemType.Armor:
                    if (player.armorId != 0) player.bagItems.Add(player.armorId);
                    player.armorId = itemId; break;
                case ItemType.Accessory:
                    if (player.accessoryId != 0) player.bagItems.Add(player.accessoryId);
                    player.accessoryId = itemId; break;
            }
            player.bagItems.Remove(itemId);
            player.RecalcStats();
            RefreshBag();
            RefreshCharInfo();
        }

        /// <summary>使用消耗品</summary>
        private void UseItem(int itemId)
        {
            var player = GameManager.Instance?.Player;
            if (player == null) return;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (item == null) return;

            // 回城卷轴仅地牢中可用
            if (item.consumableEffect == ConsumableEffectType.Teleport)
            {
                if (GameManager.Instance.State != GameState.InDungeon) return;
                player.bagItems.Remove(itemId);
                GameManager.Instance.Dungeon.OnReturnTown();
                GameManager.Instance.SetState(GameState.InTown);
                GameManager.Instance.SaveGame();
                UIManager.Instance.ReplacePanel<TownPanel>();
                return;
            }

            // 炸弹/解毒药仅战斗中可用
            if (item.consumableEffect == ConsumableEffectType.Bomb) return;
            if (item.consumableEffect == ConsumableEffectType.CurePoison) return;

            if (item.restoreHp > 0) player.hp = Mathf.Min(player.hp + item.restoreHp, player.maxHp);
            if (item.restoreMp > 0) player.mp = Mathf.Min(player.mp + item.restoreMp, player.maxMp);

            switch (item.consumableEffect)
            {
                case ConsumableEffectType.AtkBuff:
                    player.atkBuffBattles += item.consumableEffectValue;
                    break;
                case ConsumableEffectType.Shield:
                    player.hasShieldScroll = true;
                    break;
                case ConsumableEffectType.ExpBoost:
                    player.hasExpBoost = true;
                    break;
            }

            player.bagItems.Remove(itemId);
            RefreshBag();
            RefreshCharInfo();
        }

        /// <summary>出售物品</summary>
        private void SellItem(int itemId, int price)
        {
            var player = GameManager.Instance?.Player;
            if (player == null) return;
            player.bagItems.Remove(itemId);
            player.gold += price;
            RefreshBag();
        }

        /// <summary>刷新技能栏 — 读取 learnedSkills 显示技能名+MP消耗，空槽显示占位</summary>
        public void RefreshSkillBar()
        {
            var player = GameManager.Instance?.Player;
            var skills = player?.learnedSkills;

            for (int i = 0; i < MAX_SKILL_SLOTS; i++)
            {
                var slot = skillSlots[i];
                if (slot.nameText == null) continue;

                if (skills != null && i < skills.Count)
                {
                    var skillCfg = ConfigManager.Instance.GetSkill(skills[i]);
                    if (skillCfg != null)
                    {
                        slot.nameText.text = skillCfg.name;
                        slot.mpText.text = skillCfg.mpCost > 0 ? $"MP:{skillCfg.mpCost}" : "";
                        slot.nameText.color = UIColors.TextWhite;
                        slot.bgImage.color = UIColors.CardBg;
                    }
                    else
                    {
                        slot.nameText.text = "???";
                        slot.mpText.text = "";
                        slot.nameText.color = UIColors.TextDim;
                        slot.bgImage.color = UIColors.CardBg;
                    }
                }
                else
                {
                    // 空槽位
                    slot.nameText.text = "";
                    slot.mpText.text = "";
                    slot.bgImage.color = new Color(UIColors.CardBg.r, UIColors.CardBg.g, UIColors.CardBg.b, 0.40f);
                }
            }

            // 确保选中状态正确
            UpdateSkillSlotSelection();
        }

        // ============ GameState 联动 ============

        /// <summary>
        /// 根据 GameState 自动切换 HUD 可见性
        /// MainMenu/CreatingRole -> 隐藏；InTown/InDungeon/InBattle -> 显示
        /// </summary>
        public static void UpdateVisibility(GameState state)
        {
            if (_instance == null) return;
            switch (state)
            {
                case GameState.MainMenu:
                case GameState.CreatingRole:
                case GameState.None:
                    _instance.Hide();
                    _instance.HideTownMap();
                    break;
                case GameState.InTown:
                    _instance.Show();
                    _instance.ShowTownMap();
                    break;
                case GameState.InDungeon:
                case GameState.InBattle:
                    _instance.Show();
                    _instance.HideTownMap();
                    break;
            }
        }

        // ============ 城镇地图 ============

        /// <summary>显示城镇地图（InTown 时调用）</summary>
        private void ShowTownMap()
        {
            if (townMapController == null)
            {
                townMapController = TownMapController.Create(gameWorldContainer);
            }
            townMapController.gameObject.SetActive(true);
        }

        /// <summary>隐藏城镇地图</summary>
        private void HideTownMap()
        {
            if (townMapController != null)
            {
                townMapController.gameObject.SetActive(false);
            }
        }

        // ============ DontDestroyOnLoad ============

        /// <summary>
        /// 将 HUD 挂载到 HUD Canvas 并标记 DontDestroyOnLoad。
        /// 由 UIManager 或 GameEntry 调用。
        /// </summary>
        public static GameHUD Create()
        {
            var uiMgr = UIManager.EnsureCreated();
            var hudCanvas = uiMgr.GetHUDCanvas();
            if (hudCanvas == null)
            {
                Debug.LogError("[GameHUD] HUD Canvas 不存在");
                return null;
            }

            // 检查是否已存在
            var existing = hudCanvas.GetComponentInChildren<GameHUD>();
            if (existing != null)
            {
                _instance = existing;
                return existing;
            }

            // 创建 HUD 对象
            var hudObj = new GameObject("GameHUD");
            hudObj.AddComponent<RectTransform>();
            hudObj.transform.SetParent(hudCanvas.transform, false);

            var hud = hudObj.GetOrAddComponent<GameHUD>();
            _instance = hud;
            // 标记不随场景销毁（Canvas 本身已 DontDestroyOnLoad，所以无需额外处理）
            return hud;
        }
    }
}
