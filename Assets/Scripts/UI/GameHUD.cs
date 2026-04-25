using System.Collections.Generic;
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

        // ============ 组件引用 ============
        private RectTransform gameWorldContainer;
        private RectTransform rightPanel;

        // 战斗日志
        private Text combatLogText;
        private readonly List<string> combatLogEntries = new List<string>();
        private const int MAX_LOG_ENTRIES = 50;

        // 角色信息
        private Text charNameText;
        private Text charLevelText;
        private Slider hpSlider;
        private Text hpText;
        private Slider mpSlider;
        private Text mpText;
        private Text goldText;

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
        /// 战斗日志区 — 固定180px高，顶部
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

            // 日志文本
            var logObj = UIFactory.CreateUI(bgObj.transform, "LogText");
            var logRt = logObj.GetComponent<RectTransform>();
            logRt.anchorMin = new Vector2(0f, 0f);
            logRt.anchorMax = new Vector2(1f, 1f);
            logRt.offsetMin = new Vector2(8, 4);
            logRt.offsetMax = new Vector2(-8, -28);
            combatLogText = logObj.AddComponent<Text>();
            combatLogText.text = "";
            combatLogText.fontSize = 14;
            combatLogText.color = UIColors.TextDim;
            combatLogText.alignment = TextAnchor.LowerLeft;
            combatLogText.verticalOverflow = VerticalWrapMode.Truncate;
            combatLogText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            combatLogText.supportRichText = false;
        }

        /// <summary>
        /// 角色信息区 — 固定140px高，战斗日志下方
        /// </summary>
        private void BuildCharInfoSection(Transform parent)
        {
            float offsetY = COMBAT_LOG_HEIGHT;
            float sectionHeight = CHAR_INFO_HEIGHT;
            float padding = 4f;

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
            titleRt.sizeDelta = new Vector2(0, 22);
            titleRt.anchoredPosition = Vector2.zero;
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "角色";
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

            // 角色名称
            var nameObj = UIFactory.CreateUI(bgObj.transform, "CharName");
            var nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(0.5f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.offsetMin = new Vector2(8, -40);
            nameRt.offsetMax = new Vector2(0, -24);
            charNameText = nameObj.AddComponent<Text>();
            charNameText.text = "--";
            charNameText.fontSize = 14;
            charNameText.color = UIColors.TextWhite;
            charNameText.alignment = TextAnchor.MiddleLeft;
            charNameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 角色等级
            var lvlObj = UIFactory.CreateUI(bgObj.transform, "CharLevel");
            var lvlRt = lvlObj.GetComponent<RectTransform>();
            lvlRt.anchorMin = new Vector2(0.5f, 1f);
            lvlRt.anchorMax = new Vector2(1f, 1f);
            lvlRt.pivot = new Vector2(1f, 1f);
            lvlRt.offsetMin = new Vector2(0, -40);
            lvlRt.offsetMax = new Vector2(-8, -24);
            charLevelText = lvlObj.AddComponent<Text>();
            charLevelText.text = "Lv.1";
            charLevelText.fontSize = 14;
            charLevelText.color = UIColors.Gold;
            charLevelText.alignment = TextAnchor.MiddleRight;
            charLevelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // HP 条
            var hpBarObj = UIFactory.CreateUI(bgObj.transform, "HPBar");
            var hpBarRt = hpBarObj.GetComponent<RectTransform>();
            hpBarRt.anchorMin = new Vector2(0f, 1f);
            hpBarRt.anchorMax = new Vector2(1f, 1f);
            hpBarRt.pivot = new Vector2(0.5f, 1f);
            hpBarRt.offsetMin = new Vector2(8, -58);
            hpBarRt.offsetMax = new Vector2(-8, -44);
            var hpBgImg = hpBarObj.AddComponent<Image>();
            hpBgImg.color = UIColors.SliderBg;

            var hpFillObj = UIFactory.CreateUI(hpBarObj.transform, "Fill");
            var hpFillRt = hpFillObj.GetComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = Vector2.one;
            hpFillRt.offsetMin = new Vector2(1, 1);
            hpFillRt.offsetMax = new Vector2(-1, -1);
            var hpFillImg = hpFillObj.AddComponent<Image>();
            hpFillImg.color = UIColors.Red;

            hpSlider = hpBarObj.AddComponent<Slider>();
            hpSlider.interactable = false;
            hpSlider.fillRect = hpFillRt;
            hpSlider.targetGraphic = hpFillImg;
            hpSlider.value = 1f;

            // HP 文字
            var hpLabelObj = UIFactory.CreateUI(hpBarObj.transform, "HPLabel");
            UIBuilder.SetStretch(hpLabelObj);
            hpText = hpLabelObj.AddComponent<Text>();
            hpText.text = "0/0";
            hpText.fontSize = 12;
            hpText.color = UIColors.TextWhite;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // MP 条
            var mpBarObj = UIFactory.CreateUI(bgObj.transform, "MPBar");
            var mpBarRt = mpBarObj.GetComponent<RectTransform>();
            mpBarRt.anchorMin = new Vector2(0f, 1f);
            mpBarRt.anchorMax = new Vector2(1f, 1f);
            mpBarRt.pivot = new Vector2(0.5f, 1f);
            mpBarRt.offsetMin = new Vector2(8, -74);
            mpBarRt.offsetMax = new Vector2(-8, -60);
            var mpBgImg = mpBarObj.AddComponent<Image>();
            mpBgImg.color = UIColors.SliderBg;

            var mpFillObj = UIFactory.CreateUI(mpBarObj.transform, "Fill");
            var mpFillRt = mpFillObj.GetComponent<RectTransform>();
            mpFillRt.anchorMin = Vector2.zero;
            mpFillRt.anchorMax = Vector2.one;
            mpFillRt.offsetMin = new Vector2(1, 1);
            mpFillRt.offsetMax = new Vector2(-1, -1);
            var mpFillImg = mpFillObj.AddComponent<Image>();
            mpFillImg.color = UIColors.Blue;

            mpSlider = mpBarObj.AddComponent<Slider>();
            mpSlider.interactable = false;
            mpSlider.fillRect = mpFillRt;
            mpSlider.targetGraphic = mpFillImg;
            mpSlider.value = 1f;

            // MP 文字
            var mpLabelObj = UIFactory.CreateUI(mpBarObj.transform, "MPLabel");
            UIBuilder.SetStretch(mpLabelObj);
            mpText = mpLabelObj.AddComponent<Text>();
            mpText.text = "0/0";
            mpText.fontSize = 12;
            mpText.color = UIColors.TextWhite;
            mpText.alignment = TextAnchor.MiddleCenter;
            mpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 金币
            var goldObj = UIFactory.CreateUI(bgObj.transform, "Gold");
            var goldRt = goldObj.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0f, 1f);
            goldRt.anchorMax = new Vector2(1f, 1f);
            goldRt.pivot = new Vector2(0.5f, 1f);
            goldRt.offsetMin = new Vector2(8, -92);
            goldRt.offsetMax = new Vector2(-8, -78);
            goldText = goldObj.AddComponent<Text>();
            goldText.text = "0 G";
            goldText.fontSize = 13;
            goldText.color = UIColors.Orange;
            goldText.alignment = TextAnchor.MiddleLeft;
            goldText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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

        /// <summary>刷新战斗日志（stub）</summary>
        public void RefreshCombatLog()
        {
            if (combatLogText == null) return;
            // 后续接入战斗系统后填充真实日志
            if (combatLogEntries.Count == 0)
            {
                combatLogText.text = "暂无战斗记录";
            }
            else
            {
                combatLogText.text = string.Join("\n", combatLogEntries);
            }
        }

        /// <summary>刷新角色信息</summary>
        public void RefreshCharInfo()
        {
            if (charNameText == null) return;

            var player = GameManager.Instance?.Player;
            if (player == null)
            {
                charNameText.text = "--";
                charLevelText.text = "Lv.1";
                hpSlider.value = 0f;
                hpText.text = "0/0";
                mpSlider.value = 0f;
                mpText.text = "0/0";
                goldText.text = "0 G";
                return;
            }

            charNameText.text = player.name;
            charLevelText.text = $"Lv.{player.level}";
            hpSlider.value = player.maxHp > 0 ? (float)player.hp / player.maxHp : 0f;
            hpText.text = $"{player.hp}/{player.maxHp}";
            mpSlider.value = player.maxMp > 0 ? (float)player.mp / player.maxMp : 0f;
            mpText.text = $"{player.mp}/{player.maxMp}";
            goldText.text = $"{player.gold} G";
        }

        /// <summary>刷新背包（stub）</summary>
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

        /// <summary>刷新技能栏（stub）</summary>
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

        // ============ 战斗日志操作 ============

        /// <summary>添加一条战斗日志</summary>
        public void AddCombatLog(string entry)
        {
            combatLogEntries.Add(entry);
            if (combatLogEntries.Count > MAX_LOG_ENTRIES)
                combatLogEntries.RemoveAt(0);
            RefreshCombatLog();
        }

        /// <summary>清空战斗日志</summary>
        public void ClearCombatLog()
        {
            combatLogEntries.Clear();
            RefreshCombatLog();
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
