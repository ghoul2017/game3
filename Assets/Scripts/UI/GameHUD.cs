using System.Collections.Generic;
using Game.Battle;
using Game.Config;
using Game.Core;
using Game.Data;
using UnityEngine;
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
        // ============ 布局常量 ============
        private const float RIGHT_PANEL_WIDTH = 260f;
        private const float BOTTOM_BAR_HEIGHT = 70f;
        private const float COMBAT_LOG_HEIGHT = 180f;
        private const float CHAR_INFO_HEIGHT = 140f;
        private const int MAX_SKILL_SLOTS = 6;
        private const float SKILL_SLOT_SIZE = 56f;
        private const float SKILL_SLOT_GAP = 8f;

        // 战斗日志对象池
        private const int MAX_LOG_LINES = 12;

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
        private Text bagCountText;

        // 技能栏
        private readonly Image[] skillSlotIcons = new Image[MAX_SKILL_SLOTS];
        private readonly Text[] skillSlotLabels = new Text[MAX_SKILL_SLOTS];

        // ============ 生命周期 ============

        protected override void BindReferences()
        {
            BuildLayout();
        }

        protected override void BindEvents()
        {
            // 技能栏点击事件等后续功能接入
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
        /// </summary>
        private void BuildBagSection(Transform parent)
        {
            float offsetY = COMBAT_LOG_HEIGHT + CHAR_INFO_HEIGHT;
            float padding = 4f;

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
            titleRt.sizeDelta = new Vector2(0, 22);
            titleRt.anchoredPosition = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "背包";
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
            divRt.anchoredPosition = new Vector2(0, -22);

            // 背包计数
            var countObj = UIFactory.CreateUI(bgObj.transform, "BagCount");
            var countRt = countObj.GetComponent<RectTransform>();
            countRt.anchorMin = new Vector2(0f, 1f);
            countRt.anchorMax = new Vector2(1f, 1f);
            countRt.pivot = new Vector2(0.5f, 1f);
            countRt.offsetMin = new Vector2(8, -40);
            countRt.offsetMax = new Vector2(-8, -26);
            bagCountText = countObj.AddComponent<Text>();
            bagCountText.text = "0/20";
            bagCountText.fontSize = 14;
            bagCountText.color = UIColors.TextDim;
            bagCountText.alignment = TextAnchor.MiddleCenter;
            bagCountText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// <summary>
        /// 底部技能栏 — 70px高，6个技能槽，居中排列
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

            // 6个技能槽居中排列
            float totalWidth = MAX_SKILL_SLOTS * SKILL_SLOT_SIZE + (MAX_SKILL_SLOTS - 1) * SKILL_SLOT_GAP;
            float startX = -totalWidth / 2f + SKILL_SLOT_SIZE / 2f;

            for (int i = 0; i < MAX_SKILL_SLOTS; i++)
            {
                float x = startX + i * (SKILL_SLOT_SIZE + SKILL_SLOT_GAP);
                BuildSkillSlot(barObj.transform, i, x);
            }
        }

        private void BuildSkillSlot(Transform parent, int index, float xPos)
        {
            var slotObj = UIFactory.CreateUI(parent, $"SkillSlot_{index}");
            var slotRt = slotObj.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0.5f, 0.5f);
            slotRt.anchorMax = new Vector2(0.5f, 0.5f);
            slotRt.pivot = new Vector2(0.5f, 0.5f);
            slotRt.sizeDelta = new Vector2(SKILL_SLOT_SIZE, SKILL_SLOT_SIZE);
            slotRt.anchoredPosition = new Vector2(xPos, 0);

            // 槽位背景
            var bgImg = slotObj.AddComponent<Image>();
            bgImg.color = UIColors.CardBg;

            // 边框
            var borderObj = UIFactory.CreateUI(slotObj.transform, "Border");
            var borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.30f);
            var borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-1, -1);
            borderRt.offsetMax = new Vector2(1, 1);

            // 技能图标（占位）
            var iconObj = UIFactory.CreateUI(slotObj.transform, "Icon");
            var iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.color = UIColors.TextDim;
            skillSlotIcons[index] = iconImg;

            // 序号标签
            var labelObj = UIFactory.CreateUI(slotObj.transform, "Label");
            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0.3f);
            var labelText = labelObj.AddComponent<Text>();
            labelText.text = (index + 1).ToString();
            labelText.fontSize = 11;
            labelText.color = UIColors.TextDim;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            skillSlotLabels[index] = labelText;
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

        /// <summary>刷新背包</summary>
        public void RefreshBag()
        {
            if (bagCountText == null) return;

            var player = GameManager.Instance?.Player;
            if (player == null)
            {
                bagCountText.text = "0/20";
                return;
            }
            bagCountText.text = $"{player.bagItems.Count}/{player.maxBagSize}";
        }

        /// <summary>刷新技能栏</summary>
        public void RefreshSkillBar()
        {
            var player = GameManager.Instance?.Player;
            var skills = player?.learnedSkills;

            for (int i = 0; i < MAX_SKILL_SLOTS; i++)
            {
                if (skillSlotIcons[i] == null) continue;

                if (skills != null && i < skills.Count)
                {
                    skillSlotIcons[i].color = UIColors.Blue;
                }
                else
                {
                    skillSlotIcons[i].color = UIColors.TextDim;
                }
            }
        }

        // ============ GameState 联动 ============

        /// <summary>
        /// 根据 GameState 自动切换 HUD 可见性
        /// MainMenu/CreatingRole -> 隐藏；InTown/InDungeon/InBattle -> 显示
        /// </summary>
        public void UpdateVisibility(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                case GameState.CreatingRole:
                case GameState.None:
                    Hide();
                    break;
                case GameState.InTown:
                case GameState.InDungeon:
                case GameState.InBattle:
                    Show();
                    break;
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
            if (existing != null) return existing;

            // 创建 HUD 对象
            var hudObj = new GameObject("GameHUD");
            hudObj.AddComponent<RectTransform>();
            hudObj.transform.SetParent(hudCanvas.transform, false);

            var hud = hudObj.GetOrAddComponent<GameHUD>();
            // 标记不随场景销毁（Canvas 本身已 DontDestroyOnLoad，所以无需额外处理）
            return hud;
        }
    }
}
