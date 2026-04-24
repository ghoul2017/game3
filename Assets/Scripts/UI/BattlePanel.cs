using System.Collections;
using System.Collections.Generic;
using Game.Battle;
using Game.Core;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class BattlePanel : UIPanel
    {
        private const int MaxLogLines = 6;
        private const int MaxSkillRows = 8;
        private const int MaxItemRows = 8;
        private const int MaxStatusSlots = 4;

        // ---- 缓存引用：怪物信息 ----
        private Text monsterIconText;
        private Text monsterNameText;
        private Text monsterHpText;
        private Slider monsterHpSlider;
        private Image monsterHpFillImage;
        private Text monsterAtkDefText;
        private Text monsterTypeText;

        // ---- 缓存引用：怪物状态图标池 ----
        private StatusSlot[] monsterStatusSlots;

        // ---- 缓存引用：回合/速度信息 ----
        private Text turnInfoText;

        // ---- 缓存引用：战斗日志池 ----
        private Text[] logTexts;

        // ---- 缓存引用：玩家信息 ----
        private Text playerInfoText;
        private Slider playerHpSlider;
        private Slider playerMpSlider;
        private Image playerHpFillImage;
        private Text playerAtkModText;
        private Text playerDefModText;
        private Text playerSpdModText;

        // ---- 缓存引用：玩家状态图标池 ----
        private StatusSlot[] playerStatusSlots;

        // ---- 缓存引用：操作按钮 ----
        private Button btnAttack;
        private Button btnSkill;
        private Text btnSkillLabel;
        private Button btnItem;
        private Button btnEscape;

        // ---- 缓存引用：技能子面板 ----
        private GameObject skillPanel;
        private SkillRow[] skillRows;

        // ---- 缓存引用：物品选择子面板 ----
        private GameObject itemPanel;
        private ItemRow[] itemRows;
        private Text itemEmptyText;

        // ---- 缓存引用：战斗结果按钮 ----
        private Button btnResult;
        private Text btnResultLabel;

        // ---- 自动战斗 ----
        private Button btnAuto;
        private Text btnAutoLabel;
        private bool isAutoBattle;
        private Coroutine autoBattleCoroutine;

        // ---- 奖励面板 ----
        private GameObject rewardOverlay;
        private Text rewardText;

        // ---- 回合切换动画 ----
        private Coroutine turnTransitionCoroutine;

        // ---- HP/MP平滑动画 ----
        private Coroutine monsterHpAnimCoroutine;
        private Coroutine playerHpAnimCoroutine;
        private Coroutine playerMpAnimCoroutine;
        private float monsterHpDisplayValue;
        private float playerHpDisplayValue;
        private float playerMpDisplayValue;

        // ---- 状态图标动画追踪 ----
        private List<Coroutine> statusAnimCoroutines = new List<Coroutine>();

        /// <summary>状态图标槽位结构体</summary>
        private struct StatusSlot
        {
            public GameObject obj;
            public Image bgImage;
            public Text labelText;
        }

        private struct SkillRow
        {
            public GameObject obj;
            public Button btn;
            public Text label;
        }

        private struct ItemRow
        {
            public GameObject obj;
            public Button btn;
            public Text label;
        }

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);

            // 标题
            UIBuilder.CreateTitleBar(parent, "战       斗");

            // === 回合归属提示（标题下方）===
            var turnInfoObj = UIBuilder.CreateText(parent, "", 18, UIBuilder.Orange, TextAnchor.MiddleCenter, true, "TurnInfo");
            UIBuilder.SetAnchored(turnInfoObj.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -55), new Vector2(500, 22));

            // === 怪物信息区 ===
            var monsterPanelObj = UIBuilder.CreateBorderedPanel(parent, 900, 160, "MonsterPanel");
            var monsterRt = monsterPanelObj.GetComponent<RectTransform>();
            monsterRt.anchorMin = new Vector2(0, 1);
            monsterRt.anchorMax = new Vector2(1, 1);
            monsterRt.pivot = new Vector2(0.5f, 1);
            monsterRt.anchoredPosition = new Vector2(0, -80);
            monsterRt.sizeDelta = new Vector2(0, 160);

            var monsterInner = monsterPanelObj.transform.Find("Inner");

            // 怪物类型图标（左侧大字）
            var iconText = UIBuilder.CreateText(monsterInner, "", 48, UIBuilder.Red, TextAnchor.MiddleCenter, true, "MonsterIcon");
            UIBuilder.SetAnchored(iconText.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(-370, -10), new Vector2(70, 70));

            // 怪物类型标签（右侧顶部）
            var typeText = UIBuilder.CreateText(monsterInner, "", 15, UIBuilder.TextDim, TextAnchor.MiddleRight, false, "MonsterType");
            UIBuilder.SetAnchored(typeText.gameObject, new Vector2(1, 1f), new Vector2(1, 1f),
                new Vector2(-15, -20), new Vector2(100, 22));

            var nameText = UIBuilder.CreateText(monsterInner, "", 26, UIBuilder.Red, TextAnchor.MiddleCenter, true, "MonsterName");
            UIBuilder.SetAnchored(nameText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(380, 32));

            var hpText = UIBuilder.CreateText(monsterInner, "", 17, UIBuilder.HpColor, name: "MonsterHpText");
            UIBuilder.SetAnchored(hpText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-180, -55), new Vector2(180, 24));

            var hpSlider = UIBuilder.CreateSlider(monsterInner, 280, 18, UIBuilder.HpColor, "MonsterHpSlider");
            hpSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, -60);

            var atkDefText = UIBuilder.CreateText(monsterInner, "", 15, UIBuilder.TextDim, name: "MonsterAtkDef");
            UIBuilder.SetAnchored(atkDefText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(280, 22));

            // 怪物状态图标区（底部一排4个）
            var monsterStatusContainer = CreateUI(monsterInner, "MonsterStatusIcons");
            var mscRt = monsterStatusContainer.GetComponent<RectTransform>();
            mscRt.anchorMin = new Vector2(0.5f, 0);
            mscRt.anchorMax = new Vector2(0.5f, 0);
            mscRt.pivot = new Vector2(0.5f, 0);
            mscRt.anchoredPosition = new Vector2(0, 5);
            mscRt.sizeDelta = new Vector2(MaxStatusSlots * 24, 20);

            for (int i = 0; i < MaxStatusSlots; i++)
            {
                CreateStatusSlot(monsterStatusContainer.transform, i, i * 24 - (MaxStatusSlots * 24 - 24) / 2f);
            }

            // === 玩家信息条（底部锚定，按钮上方 10px）===
            var playerBar = UIBuilder.CreatePanel(parent, 900, 80, "PlayerBar");
            var playerBarRt = playerBar.GetComponent<RectTransform>();
            playerBarRt.anchorMin = new Vector2(0, 0);
            playerBarRt.anchorMax = new Vector2(1, 0);
            playerBarRt.pivot = new Vector2(0.5f, 0);
            playerBarRt.anchoredPosition = new Vector2(0, 88);
            playerBarRt.sizeDelta = new Vector2(-20, 80);

            // 玩家信息文字（左上）
            var playerInfoObj = UIBuilder.CreateText(playerBar.transform, "", 14, UIBuilder.Gold, TextAnchor.MiddleLeft, name: "PlayerInfo");
            UIBuilder.SetAnchored(playerInfoObj.gameObject, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 22), new Vector2(800, 22));

            // 玩家HP条
            var pHpSlider = UIBuilder.CreateSlider(playerBar.transform, 280, 14, UIBuilder.HpColor, "PlayerHpSlider");
            var pHpRt = pHpSlider.GetComponent<RectTransform>();
            pHpRt.anchorMin = new Vector2(0, 0.5f);
            pHpRt.anchorMax = new Vector2(0, 0.5f);
            pHpRt.anchoredPosition = new Vector2(-60, -8);
            pHpRt.sizeDelta = new Vector2(280, 14);

            // 玩家MP条
            var pMpSlider = UIBuilder.CreateSlider(playerBar.transform, 280, 14, UIBuilder.MpColor, "PlayerMpSlider");
            var pMpRt = pMpSlider.GetComponent<RectTransform>();
            pMpRt.anchorMin = new Vector2(0, 0.5f);
            pMpRt.anchorMax = new Vector2(0, 0.5f);
            pMpRt.anchoredPosition = new Vector2(-60, -26);
            pMpRt.sizeDelta = new Vector2(280, 14);

            // Buff修正标签（ATK/DEF/SPD）
            var atkMod = UIBuilder.CreateText(playerBar.transform, "", 13, UIBuilder.Green, TextAnchor.MiddleLeft, name: "AtkMod");
            UIBuilder.SetAnchored(atkMod.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(150, -4), new Vector2(120, 18));

            var defMod = UIBuilder.CreateText(playerBar.transform, "", 13, UIBuilder.Green, TextAnchor.MiddleLeft, name: "DefMod");
            UIBuilder.SetAnchored(defMod.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(150, -20), new Vector2(120, 18));

            var spdMod = UIBuilder.CreateText(playerBar.transform, "", 13, UIBuilder.Green, TextAnchor.MiddleLeft, name: "SpdMod");
            UIBuilder.SetAnchored(spdMod.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(150, -36), new Vector2(120, 18));

            // 玩家状态图标区
            var playerStatusContainer = CreateUI(playerBar.transform, "PlayerStatusIcons");
            var pscRt = playerStatusContainer.GetComponent<RectTransform>();
            pscRt.anchorMin = new Vector2(1, 0.5f);
            pscRt.anchorMax = new Vector2(1, 0.5f);
            pscRt.pivot = new Vector2(1, 0.5f);
            pscRt.anchoredPosition = new Vector2(-10, -10);
            pscRt.sizeDelta = new Vector2(MaxStatusSlots * 24, 20);

            for (int i = 0; i < MaxStatusSlots; i++)
            {
                CreateStatusSlot(playerStatusContainer.transform, i, i * 24 - (MaxStatusSlots * 24 - 24) / 2f);
            }

            // === 战斗日志区（拉伸填充 MonsterPanel 和 PlayerBar 之间）===
            var logPanelObj = UIBuilder.CreateBorderedPanel(parent, 900, 220, "LogPanel");
            var logRt = logPanelObj.GetComponent<RectTransform>();
            logRt.anchorMin = new Vector2(0, 0);
            logRt.anchorMax = new Vector2(1, 1);
            logRt.pivot = new Vector2(0.5f, 0.5f);
            logRt.offsetMin = new Vector2(10, 178);
            logRt.offsetMax = new Vector2(-10, -270);

            var logInner = logPanelObj.transform.Find("Inner");

            for (int i = 0; i < MaxLogLines; i++)
            {
                var logObj = UIBuilder.CreateText(logInner, "", 16, UIBuilder.TextWhite, TextAnchor.MiddleLeft, name: $"Log_{i}");
                UIBuilder.SetAnchored(logObj.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0, -12 - i * 30), new Vector2(800, 28));
            }

            // === 操作按钮（底部锚定，确保始终在屏幕内）===
            float btnBottomY = 30;
            float btnW = 180;
            float btnH = 48;
            float gap = 40;

            var (atkBtn, _) = UIBuilder.CreateButtonWithRef(parent, "攻  击", btnW, btnH, null, 22, "BtnAttack");
            UIBuilder.SetAnchored(atkBtn.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-gap * 3 - btnW * 1.5f, btnBottomY), new Vector2(btnW, btnH));

            var (skBtn, _) = UIBuilder.CreateButtonWithRef(parent, "技  能", btnW, btnH, null, 22, "BtnSkill");
            UIBuilder.SetAnchored(skBtn.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-gap, btnBottomY), new Vector2(btnW, btnH));

            var (itBtn, _) = UIBuilder.CreateButtonWithRef(parent, "物  品", btnW, btnH, null, 22, "BtnItem");
            UIBuilder.SetAnchored(itBtn.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(gap, btnBottomY), new Vector2(btnW, btnH));

            var (esBtn, _) = UIBuilder.CreateButtonWithRef(parent, "逃  跑", btnW, btnH, null, 22, "BtnEscape");
            UIBuilder.SetAnchored(esBtn.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(gap * 3 + btnW * 1.5f, btnBottomY), new Vector2(btnW, btnH));

            // 自动战斗按钮（右上角）
            var (autoBtn, _) = UIBuilder.CreateButtonWithRef(parent, "Auto", 80, 36, null, 16, "BtnAuto");
            UIBuilder.SetAnchored(autoBtn.gameObject, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-20, -80), new Vector2(80, 36));

            // === 技能子面板（对象池 8 行） ===
            var sp = UIBuilder.CreateBorderedPanel(parent, 650, 340, "SkillPanel");
            var spRt = sp.GetComponent<RectTransform>();
            spRt.anchorMin = new Vector2(0, 0);
            spRt.anchorMax = new Vector2(1, 1);
            spRt.pivot = new Vector2(0.5f, 0.5f);
            spRt.anchoredPosition = new Vector2(0, -50);
            spRt.sizeDelta = new Vector2(-100, -200);
            sp.gameObject.SetActive(false);
            var skillInner = sp.transform.Find("Inner");

            for (int i = 0; i < MaxSkillRows; i++)
            {
                var rowObj = new GameObject($"SkillRow_{i}");
                rowObj.transform.SetParent(skillInner, false);
                var rowRt = rowObj.AddComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(550, 34);
                rowRt.anchoredPosition = new Vector2(0, 125 - i * 36);

                var (rowBtn, _) = UIBuilder.CreateButtonWithRef(rowObj.transform, "", 550, 34, null, 16, "Btn");
                UIBuilder.SetStretch(rowBtn.gameObject);
            }

            // 技能面板关闭按钮
            var (closeSkillBtn, _) = UIBuilder.CreateButtonWithRef(skillInner, "关闭", 220, 32, null, 16, "BtnCloseSkill");
            UIBuilder.SetAnchored(closeSkillBtn.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 125 - MaxSkillRows * 36), new Vector2(220, 32));

            // === 物品选择子面板（对象池 8 行） ===
            var ip = UIBuilder.CreateBorderedPanel(parent, 650, 340, "ItemPanel");
            var ipRt = ip.GetComponent<RectTransform>();
            ipRt.anchorMin = new Vector2(0, 0);
            ipRt.anchorMax = new Vector2(1, 1);
            ipRt.pivot = new Vector2(0.5f, 0.5f);
            ipRt.anchoredPosition = new Vector2(0, -50);
            ipRt.sizeDelta = new Vector2(-100, -200);
            ip.gameObject.SetActive(false);
            var itemInner = ip.transform.Find("Inner");

            for (int i = 0; i < MaxItemRows; i++)
            {
                var rowObj = new GameObject($"ItemRow_{i}");
                rowObj.transform.SetParent(itemInner, false);
                var rowRt = rowObj.AddComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(550, 34);
                rowRt.anchoredPosition = new Vector2(0, 125 - i * 36);

                var (rowBtn, _) = UIBuilder.CreateButtonWithRef(rowObj.transform, "", 550, 34, null, 16, "Btn");
                UIBuilder.SetStretch(rowBtn.gameObject);
            }

            // 物品面板空提示
            var emptyText = UIBuilder.CreateText(itemInner, "没有可用的消耗品", 17, UIBuilder.TextDim, TextAnchor.MiddleCenter, name: "ItemEmpty");
            UIBuilder.SetAnchored(emptyText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(500, 28));
            emptyText.gameObject.SetActive(false);

            // 物品面板关闭按钮
            var (closeItemBtn, _) = UIBuilder.CreateButtonWithRef(itemInner, "关闭", 220, 32, null, 16, "BtnCloseItem");
            UIBuilder.SetAnchored(closeItemBtn.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 125 - MaxItemRows * 36), new Vector2(220, 32));

            // === 奖励面板（默认隐藏） ===
            var rewardOverlayObj = new GameObject("RewardOverlay");
            rewardOverlayObj.transform.SetParent(parent, false);
            var rewardOverlayRt = rewardOverlayObj.AddComponent<RectTransform>();
            UIBuilder.SetStretch(rewardOverlayObj);
            var rewardBg = rewardOverlayObj.AddComponent<Image>();
            rewardBg.color = new Color(UIBuilder.DarkBg.r, UIBuilder.DarkBg.g, UIBuilder.DarkBg.b, 0.92f);
            rewardOverlayObj.AddComponent<CanvasGroup>();
            rewardOverlayObj.SetActive(false);

            var rewardTextObj = UIBuilder.CreateText(rewardOverlayObj.transform, "", 22, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "RewardText");
            UIBuilder.SetStretch(rewardTextObj.gameObject);

            var (rewardBtn, _) = UIBuilder.CreateButtonWithRef(rewardOverlayObj.transform, "继续", 160, 44, null, 20, "BtnRewardContinue");
            UIBuilder.SetAnchored(rewardBtn.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -80), new Vector2(160, 44));

            // === 战斗结果按钮（默认隐藏） ===
            var (resultBtn, _) = UIBuilder.CreateButtonWithRef(parent, "", 240, 55, null, 24, "BtnResult");
            UIBuilder.SetAnchored(resultBtn.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -290), new Vector2(240, 55));
            resultBtn.gameObject.SetActive(false);
        }

        /// <summary>创建单个状态图标槽位（16x16方块+缩写文字）</summary>
        private static void CreateStatusSlot(Transform parent, int index, float xPos)
        {
            var slotObj = new GameObject($"StatusSlot_{index}");
            slotObj.transform.SetParent(parent, false);
            var slotRt = slotObj.AddComponent<RectTransform>();
            slotRt.sizeDelta = new Vector2(20, 20);
            slotRt.anchoredPosition = new Vector2(xPos, 0);

            var bg = slotObj.AddComponent<Image>();
            bg.color = Color.gray;

            // 缩写文字覆盖在方块上
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(slotObj.transform, false);
            var labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelObj.AddComponent<Text>();
            label.text = "";
            label.fontSize = 10;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 添加CanvasGroup用于淡出动画
            slotObj.AddComponent<CanvasGroup>();

            slotObj.SetActive(false);
        }

        /// <summary>CreateVisuals中使用的内部UI创建方法</summary>
        private static GameObject CreateUI(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        protected override void BindReferences()
        {
            // 怪物信息
            turnInfoText = FindComponent<Text>("TurnInfo");
            monsterIconText = FindComponent<Text>("MonsterPanel/Inner/MonsterIcon");
            monsterTypeText = FindComponent<Text>("MonsterPanel/Inner/MonsterType");
            monsterNameText = FindComponent<Text>("MonsterPanel/Inner/MonsterName");
            monsterHpText = FindComponent<Text>("MonsterPanel/Inner/MonsterHpText");
            monsterHpSlider = FindComponent<Slider>("MonsterPanel/Inner/MonsterHpSlider");
            monsterHpFillImage = FindComponent<Image>("MonsterPanel/Inner/MonsterHpSlider/Fill Area/Fill");
            monsterAtkDefText = FindComponent<Text>("MonsterPanel/Inner/MonsterAtkDef");

            // 怪物状态图标池
            monsterStatusSlots = new StatusSlot[MaxStatusSlots];
            for (int i = 0; i < MaxStatusSlots; i++)
            {
                var slotTr = transform.Find($"MonsterPanel/Inner/MonsterStatusIcons/StatusSlot_{i}");
                if (slotTr == null) continue;
                var slotObj = slotTr.gameObject;
                monsterStatusSlots[i] = new StatusSlot
                {
                    obj = slotObj,
                    bgImage = slotTr.GetComponent<Image>(),
                    labelText = slotTr.Find("Label")?.GetComponent<Text>()
                };
                slotObj.SetActive(false);
            }

            // 战斗日志池
            logTexts = new Text[MaxLogLines];
            for (int i = 0; i < MaxLogLines; i++)
            {
                logTexts[i] = FindComponent<Text>($"LogPanel/Inner/Log_{i}");
                logTexts[i].gameObject.SetActive(false);
            }

            // 玩家信息
            playerInfoText = FindComponent<Text>("PlayerBar/PlayerInfo");
            playerHpSlider = FindComponent<Slider>("PlayerBar/PlayerHpSlider");
            playerMpSlider = FindComponent<Slider>("PlayerBar/PlayerMpSlider");
            playerHpFillImage = FindComponent<Image>("PlayerBar/PlayerHpSlider/Fill Area/Fill");
            playerAtkModText = FindComponent<Text>("PlayerBar/AtkMod");
            playerDefModText = FindComponent<Text>("PlayerBar/DefMod");
            playerSpdModText = FindComponent<Text>("PlayerBar/SpdMod");

            // 玩家状态图标池
            playerStatusSlots = new StatusSlot[MaxStatusSlots];
            for (int i = 0; i < MaxStatusSlots; i++)
            {
                var slotTr = transform.Find($"PlayerBar/PlayerStatusIcons/StatusSlot_{i}");
                if (slotTr == null) continue;
                var slotObj = slotTr.gameObject;
                playerStatusSlots[i] = new StatusSlot
                {
                    obj = slotObj,
                    bgImage = slotTr.GetComponent<Image>(),
                    labelText = slotTr.Find("Label")?.GetComponent<Text>()
                };
                slotObj.SetActive(false);
            }

            // 操作按钮
            btnAttack = FindComponent<Button>("BtnAttack");
            btnSkill = FindComponent<Button>("BtnSkill");
            btnSkillLabel = FindComponent<Text>("BtnSkill/Label");
            btnItem = FindComponent<Button>("BtnItem");
            btnEscape = FindComponent<Button>("BtnEscape");

            // 技能子面板
            var skillPanelTr = transform.Find("SkillPanel");
            skillPanel = skillPanelTr != null ? skillPanelTr.gameObject : null;
            skillRows = new SkillRow[MaxSkillRows];
            for (int i = 0; i < MaxSkillRows; i++)
            {
                var rowTr = transform.Find($"SkillPanel/Inner/SkillRow_{i}");
                if (rowTr == null) { skillRows[i] = new SkillRow(); continue; }
                var rowObj = rowTr.gameObject;
                var btnTr = rowObj.transform.Find("Btn");
                var rowBtn = btnTr != null ? btnTr.GetComponent<Button>() : null;
                var labelTr = btnTr != null ? btnTr.Find("Label") : null;
                var rowLabel = labelTr != null ? labelTr.GetComponent<Text>() : null;
                rowObj.SetActive(false);
                skillRows[i] = new SkillRow { obj = rowObj, btn = rowBtn, label = rowLabel };
            }

            // 物品子面板
            var itemPanelTr = transform.Find("ItemPanel");
            itemPanel = itemPanelTr != null ? itemPanelTr.gameObject : null;
            itemRows = new ItemRow[MaxItemRows];
            for (int i = 0; i < MaxItemRows; i++)
            {
                var rowTr = transform.Find($"ItemPanel/Inner/ItemRow_{i}");
                if (rowTr == null) { itemRows[i] = new ItemRow(); continue; }
                var rowObj = rowTr.gameObject;
                var btnTr = rowObj.transform.Find("Btn");
                var rowBtn = btnTr != null ? btnTr.GetComponent<Button>() : null;
                var labelTr = btnTr != null ? btnTr.Find("Label") : null;
                var rowLabel = labelTr != null ? labelTr.GetComponent<Text>() : null;
                rowObj.SetActive(false);
                itemRows[i] = new ItemRow { obj = rowObj, btn = rowBtn, label = rowLabel };
            }
            itemEmptyText = FindComponent<Text>("ItemPanel/Inner/ItemEmpty");

            // 战斗结果按钮
            btnResult = FindComponent<Button>("BtnResult");
            btnResultLabel = FindComponent<Text>("BtnResult/Label");

            // 自动战斗按钮
            btnAuto = FindComponent<Button>("BtnAuto");
            btnAutoLabel = FindComponent<Text>("BtnAuto/Label");

            // 奖励面板
            rewardOverlay = transform.Find("RewardOverlay").gameObject;
            rewardText = FindComponent<Text>("RewardOverlay/RewardText");
        }

        protected override void BindEvents()
        {
            if (btnAttack != null) btnAttack.onClick.AddListener(() => { PlayUIClick(); OnAttack(); });
            if (btnSkill != null) btnSkill.onClick.AddListener(() => { PlayUIClick(); OnSkillMenu(); });
            if (btnItem != null) btnItem.onClick.AddListener(() => { PlayUIClick(); OnItemMenu(); });
            if (btnEscape != null) btnEscape.onClick.AddListener(() => { PlayUIClick(); OnEscape(); });
            if (btnResult != null) btnResult.onClick.AddListener(() => { PlayUIClick(); OnBattleEnd(); });
            if (btnAuto != null) btnAuto.onClick.AddListener(() => { PlayUIClick(); OnToggleAutoBattle(); });

            var rewardContinue = FindComponent<Button>("RewardOverlay/BtnRewardContinue");
            if (rewardContinue != null) rewardContinue.onClick.AddListener(() => { PlayUIClick(); OnRewardContinue(); });

            var closeSkillBtn = FindComponent<Button>("SkillPanel/Inner/BtnCloseSkill");
            if (closeSkillBtn != null) closeSkillBtn.onClick.AddListener(() => { PlayUIClick(); if (skillPanel != null) skillPanel.SetActive(false); });

            var closeItemBtn = FindComponent<Button>("ItemPanel/Inner/BtnCloseItem");
            if (closeItemBtn != null) closeItemBtn.onClick.AddListener(() => { PlayUIClick(); if (itemPanel != null) itemPanel.SetActive(false); });
        }

        public override void OnShow()
        {
            isAutoBattle = false;
            autoBattleCoroutine = null;
            turnTransitionCoroutine = null;
            monsterHpDisplayValue = -1f;
            playerHpDisplayValue = -1f;
            playerMpDisplayValue = -1f;
            statusAnimCoroutines.Clear();
            if (skillPanel != null) skillPanel.SetActive(false);
            if (itemPanel != null) itemPanel.SetActive(false);
            if (rewardOverlay != null) rewardOverlay.SetActive(false);
            if (btnResult != null) btnResult.gameObject.SetActive(false);
            if (btnAutoLabel != null) btnAutoLabel.text = "Auto";
            RefreshUI();
        }

        public override void OnHide()
        {
            StopAutoBattle();
            StopTurnTransition();
            StopBarAnimations();
            StopStatusAnimations();
        }

        /// <summary>
        /// 零销毁刷新：只修改 .text / .value / .color / .interactable / .SetActive
        /// </summary>
        private void RefreshUI()
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null) return;
            var p = GameManager.Instance.Player;
            var m = b.monster;

            // 怪物信息
            if (monsterNameText != null) monsterNameText.text = m.name;
            if (monsterIconText != null)
            {
                monsterIconText.text = m.isBoss ? "王" : "怪";
                monsterIconText.color = m.isBoss ? UIBuilder.Gold : UIBuilder.Red;
            }
            if (monsterTypeText != null) monsterTypeText.text = m.isBoss ? "BOSS" : (m.level > 0 ? $"Lv.{m.level}" : "");
            if (monsterHpText != null) monsterHpText.text = $"HP: {m.hp}/{m.maxHp}";

            // 怪物HP条平滑动画
            float monsterTargetHp = m.maxHp > 0 ? (float)m.hp / m.maxHp : 0f;
            AnimateBar(ref monsterHpAnimCoroutine, ref monsterHpDisplayValue, monsterTargetHp,
                monsterHpSlider, monsterHpFillImage, true, m.hp, m.maxHp);

            if (monsterAtkDefText != null)
            {
                int monsterSpeed = BattleManager.Instance.GetEffectiveMonsterSpeed();
                monsterAtkDefText.text = $"攻击:{m.attack}  防御:{m.defense}  速度:{monsterSpeed}";
            }

            // 回合归属和双方速度
            if (turnInfoText != null && !b.isFinished)
            {
                int playerSpeed = BattleManager.Instance.GetEffectivePlayerSpeed();
                int monsterSpeed = BattleManager.Instance.GetEffectiveMonsterSpeed();
                string turnLabel = b.isPlayerTurn ? "你的回合" : "敌人回合";
                turnInfoText.text = $"{turnLabel}  (你 速度:{playerSpeed} | 敌方 速度:{monsterSpeed})";
                turnInfoText.color = b.isPlayerTurn ? UIBuilder.Gold : UIBuilder.Red;
            }
            else if (turnInfoText != null && b.isFinished)
            {
                turnInfoText.text = "";
            }

            // 战斗日志（颜色标记）
            if (logTexts != null)
            {
                int showCount = Mathf.Min(b.log.Count, MaxLogLines);
                int startIdx = b.log.Count - showCount;
                for (int i = 0; i < MaxLogLines; i++)
                {
                    if (logTexts[i] == null) continue;
                    if (i < showCount)
                    {
                        logTexts[i].text = ColorizeLog(b.log[startIdx + i]);
                        logTexts[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        logTexts[i].gameObject.SetActive(false);
                    }
                }
            }

            // 玩家信息（精简：HP/MP数字在条旁显示）
            if (playerInfoText != null)
            {
                playerInfoText.text = $"HP:{p.hp}/{p.maxHp}  MP:{p.mp}/{p.maxMp}  Lv.{p.level}";
            }

            // 玩家HP/MP条平滑动画
            float playerTargetHp = p.maxHp > 0 ? (float)p.hp / p.maxHp : 0f;
            AnimateBar(ref playerHpAnimCoroutine, ref playerHpDisplayValue, playerTargetHp,
                playerHpSlider, playerHpFillImage, true, p.hp, p.maxHp);

            float playerTargetMp = p.maxMp > 0 ? (float)p.mp / p.maxMp : 0f;
            AnimateBar(ref playerMpAnimCoroutine, ref playerMpDisplayValue, playerTargetMp,
                playerMpSlider, null, false, p.mp, p.maxMp);

            // Buff/Debuff修正值标签
            UpdateModifierLabels(b, p);

            // 状态图标刷新
            UpdateStatusIcons(monsterStatusSlots, b.monsterEffects, true);
            UpdateStatusIcons(playerStatusSlots, b.playerEffects, false);

            // 战斗结束处理
            if (b.isFinished)
            {
                isAutoBattle = false;
                if (btnAutoLabel != null) btnAutoLabel.text = "Auto";
                if (btnAttack != null) btnAttack.interactable = false;
                if (btnSkill != null) btnSkill.interactable = false;
                if (btnItem != null) btnItem.interactable = false;
                if (btnEscape != null) btnEscape.interactable = false;
                if (skillPanel != null) skillPanel.SetActive(false);
                if (itemPanel != null) itemPanel.SetActive(false);

                if (b.isVictory)
                {
                    string rewardMsg = $"战斗胜利！\n\n获得 {b.rewardExp} 经验\n获得 {b.rewardGold} 金币";
                    foreach (var itemName in b.rewardItems)
                        rewardMsg += $"\n获得物品：{itemName}";
                    if (rewardText != null) rewardText.text = rewardMsg;
                    if (rewardOverlay != null) rewardOverlay.SetActive(true);
                    if (btnResult != null) btnResult.gameObject.SetActive(false);
                }
                else
                {
                    if (rewardOverlay != null) rewardOverlay.SetActive(false);
                    if (btnResultLabel != null) btnResultLabel.text = "返回城镇";
                    if (btnResult != null) btnResult.gameObject.SetActive(true);
                }
            }
            else
            {
                // 沉默检测：禁用技能按钮（灰色+solid文字）
                bool silenced = BattleManager.Instance.IsPlayerSilenced();
                if (btnAttack != null) btnAttack.interactable = true;
                if (btnSkill != null)
                {
                    btnSkill.interactable = !silenced;
                    if (btnSkillLabel != null)
                    {
                        btnSkillLabel.color = silenced ? UIBuilder.TextDim : UIBuilder.Gold;
                    }
                }
                if (btnItem != null) btnItem.interactable = true;
                if (btnEscape != null) btnEscape.interactable = true;
                if (btnResult != null) btnResult.gameObject.SetActive(false);
            }
        }

        // ==================== 战斗日志颜色标记 ====================

        /// <summary>
        /// 将战斗日志文本标记为富文本颜色。
        /// 规则：伤害→红色，治疗→绿色，状态施加→黄色，闪避→蓝色，普通→白色
        /// </summary>
        private string ColorizeLog(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            // 闪避（优先级高）
            if (raw.Contains("闪避"))
                return $"<color=#3498db>{raw}</color>";

            // 伤害类
            if (raw.Contains("造成") && raw.Contains("伤害"))
                return $"<color=#e74c3c>{raw}</color>";

            // DoT伤害（流血/中毒损失HP）
            if (raw.Contains("损失") && raw.Contains("HP"))
                return $"<color=#e74c3c>{raw}</color>";

            // 治疗类
            if (raw.Contains("恢复") && (raw.Contains("生命") || raw.Contains("法力") || raw.Contains("HP")))
                return $"<color=#2ecc71>{raw}</color>";

            // 状态施加/效果
            if (raw.Contains("受到") && raw.Contains("效果"))
                return $"<color=#f39c12>{raw}</color>";
            if (raw.Contains("效果消失"))
                return $"<color=#f39c12>{raw}</color>";
            if (raw.Contains("眩晕") || raw.Contains("沉默") || raw.Contains("无法行动"))
                return $"<color=#f39c12>{raw}</color>";

            // 增益/Buff
            if (raw.Contains("增益") || raw.Contains("攻击+") || raw.Contains("防御+"))
                return $"<color=#3498db>{raw}</color>";

            // 速度判定
            if (raw.Contains("[速度]"))
                return $"<color=#e0d8c0>{raw}</color>";

            // 复活
            if (raw.Contains("复活"))
                return $"<color=#2ecc71>{raw}</color>";

            // 默认：普通信息白色
            return $"<color=#e0d8c0>{raw}</color>";
        }

        // ==================== HP/MP条平滑动画 ====================

        /// <summary>
        /// 启动条形平滑动画。只在目标值变化时启动新协程。
        /// </summary>
        private void AnimateBar(ref Coroutine coroutine, ref float displayValue, float targetValue,
            Slider slider, Image fillImage, bool isHp, int currentVal, int maxVal)
        {
            if (slider == null) return;

            // 首次设置时直接赋值（无动画）
            if (displayValue < 0f)
            {
                displayValue = targetValue;
                slider.value = targetValue;
                // 低血量警告颜色
                if (isHp && fillImage != null)
                    fillImage.color = GetHpBarColor(targetValue);
                return;
            }

            // 值未变化，跳过
            if (Mathf.Approximately(displayValue, targetValue)) return;

            // 停止旧动画，启动新动画
            if (coroutine != null) StopCoroutine(coroutine);
            float from = displayValue;
            displayValue = targetValue;
            coroutine = StartCoroutine(BarLerpCoroutine(from, targetValue, slider, fillImage, isHp));
        }

        private IEnumerator BarLerpCoroutine(float fromValue, float toValue,
            Slider slider, Image fillImage, bool isHp)
        {
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float value = Mathf.Lerp(fromValue, toValue, t);
                slider.value = value;

                // 低血量颜色渐变（HP条专用）
                if (isHp && fillImage != null)
                {
                    fillImage.color = GetHpBarColor(value);
                }

                yield return null;
            }

            slider.value = toValue;
            if (isHp && fillImage != null)
                fillImage.color = GetHpBarColor(toValue);
        }

        /// <summary>
        /// HP条颜色：正常绿→低血量红。低于30%时从绿渐变到红。
        /// </summary>
        private Color GetHpBarColor(float ratio)
        {
            // 正常：绿色
            Color normalColor = new Color(0.345f, 0.839f, 0.553f); // #58d68d
            // 危险：红色
            Color dangerColor = new Color(0.906f, 0.298f, 0.235f); // #e74c3c

            if (ratio >= 0.3f) return normalColor;
            // 0~0.3之间线性插值
            float t = ratio / 0.3f;
            return Color.Lerp(dangerColor, normalColor, t);
        }

        private void StopBarAnimations()
        {
            if (monsterHpAnimCoroutine != null) { StopCoroutine(monsterHpAnimCoroutine); monsterHpAnimCoroutine = null; }
            if (playerHpAnimCoroutine != null) { StopCoroutine(playerHpAnimCoroutine); playerHpAnimCoroutine = null; }
            if (playerMpAnimCoroutine != null) { StopCoroutine(playerMpAnimCoroutine); playerMpAnimCoroutine = null; }
        }

        // ==================== 状态图标系统 ====================

        /// <summary>获取状态类型对应的颜色</summary>
        private static Color GetStatusColor(EffectType type)
        {
            return type switch
            {
                EffectType.Bleed => new Color(0.906f, 0.298f, 0.235f),     // 流血红 #e74c3c
                EffectType.Poison => new Color(0.345f, 0.839f, 0.553f),    // 中毒绿 #58d68d
                EffectType.Stun => new Color(0.941f, 0.753f, 0.251f),      // 眩晕黄 #f0c040
                EffectType.Silence => new Color(0.588f, 0.529f, 0.475f),   // 沉默灰 #958678
                EffectType.Vulnerable => new Color(0.941f, 0.596f, 0.157f),// 易伤橙 #f09828
                EffectType.BuffATK => new Color(0.365f, 0.678f, 0.886f),   // Buff蓝 #5dade2
                EffectType.BuffDEF => new Color(0.365f, 0.678f, 0.886f),   // Buff蓝 #5dade2
                EffectType.BuffSpeed => new Color(0.365f, 0.678f, 0.886f), // Buff蓝 #5dade2
                _ => Color.gray
            };
        }

        /// <summary>获取状态类型的缩写文字</summary>
        private static string GetStatusAbbrev(EffectType type)
        {
            return type switch
            {
                EffectType.Bleed => "流血",
                EffectType.Poison => "中毒",
                EffectType.Stun => "眩晕",
                EffectType.Silence => "沉默",
                EffectType.Vulnerable => "易伤",
                EffectType.BuffATK => "+ATK",
                EffectType.BuffDEF => "+DEF",
                EffectType.BuffSpeed => "+SPD",
                _ => "?"
            };
        }

        /// <summary>更新状态图标池（对象池复用）</summary>
        private void UpdateStatusIcons(StatusSlot[] slots, List<StatusEffect> effects, bool isMonster)
        {
            if (slots == null || effects == null) return;

            int showCount = Mathf.Min(effects.Count, MaxStatusSlots);
            for (int i = 0; i < MaxStatusSlots; i++)
            {
                if (slots[i].obj == null) continue;
                if (i < showCount)
                {
                    var effect = effects[i];
                    var slot = slots[i];

                    // 设置颜色和文字
                    Color effectColor = GetStatusColor(effect.type);
                    if (slot.bgImage != null) slot.bgImage.color = effectColor;
                    if (slot.labelText != null)
                        slot.labelText.text = GetStatusAbbrev(effect.type);

                    // 重置CanvasGroup（可能被淡出动画改过）
                    var cg = slot.obj.GetComponent<CanvasGroup>();
                    if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = false; }

                    // 重置缩放（可能被弹入动画改过）
                    slot.obj.transform.localScale = Vector3.one;

                    bool wasInactive = !slot.obj.activeSelf;
                    slot.obj.SetActive(true);

                    // 新激活的图标播放弹入动画
                    if (wasInactive)
                        PlayStatusPopIn(slot.obj);
                }
                else
                {
                    // 隐藏多余槽位
                    if (slots[i].obj.activeSelf)
                    {
                        // 播放淡出动画
                        PlayStatusFadeOut(slots[i].obj);
                    }
                }
            }
        }

        /// <summary>状态图标弹入动画：1.0→1.3→1.0，持续0.3s</summary>
        private void PlayStatusPopIn(GameObject obj)
        {
            StopStatusAnimationsFor(obj);
            var cor = StartCoroutine(StatusPopInCoroutine(obj));
            statusAnimCoroutines.Add(cor);
        }

        private IEnumerator StatusPopInCoroutine(GameObject obj)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            float peakScale = 1.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // 先放大到1.3再回到1.0
                float scale = t < 0.5f
                    ? Mathf.Lerp(1f, peakScale, t * 2f)
                    : Mathf.Lerp(peakScale, 1f, (t - 0.5f) * 2f);
                if (obj != null) obj.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            if (obj != null) obj.transform.localScale = Vector3.one;
        }

        /// <summary>状态图标淡出动画：alpha 1→0，0.2s后隐藏</summary>
        private void PlayStatusFadeOut(GameObject obj)
        {
            StopStatusAnimationsFor(obj);
            var cor = StartCoroutine(StatusFadeOutCoroutine(obj));
            statusAnimCoroutines.Add(cor);
        }

        private IEnumerator StatusFadeOutCoroutine(GameObject obj)
        {
            var cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) { obj.SetActive(false); yield break; }

            float duration = 0.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = 1f - t;
                yield return null;
            }
            cg.alpha = 0f;
            obj.SetActive(false);
            cg.alpha = 1f;
        }

        /// <summary>停掉指定对象上的状态动画协程</summary>
        private void StopStatusAnimationsFor(GameObject obj)
        {
            // 简单方案：不能直接关联，直接清理所有过期的协程引用
            // 协程内部检查obj != null已足够安全
        }

        private void StopStatusAnimations()
        {
            foreach (var cor in statusAnimCoroutines)
            {
                if (cor != null) StopCoroutine(cor);
            }
            statusAnimCoroutines.Clear();
        }

        // ==================== Buff/Debuff修正标签 ====================

        /// <summary>更新ATK/DEF/SPD修正值小标签</summary>
        private void UpdateModifierLabels(BattleData b, PlayerData p)
        {
            if (b == null || p == null) return;

            // 计算修正值 = 旧buff + 状态效果修正
            int atkMod = b.playerBuffAttack + BattleManager.Instance.GetEffectModifier(true, EffectType.BuffATK);
            int defMod = b.playerBuffDefense + BattleManager.Instance.GetEffectModifier(true, EffectType.BuffDEF);
            int spdMod = b.playerBuffSpeed + BattleManager.Instance.GetEffectModifier(true, EffectType.BuffSpeed);

            // 怪物debuff效果（减益显示为负数）
            if (atkMod != 0 || defMod != 0 || spdMod != 0)
            {
                SetModifierText(playerAtkModText, "ATK", atkMod);
                SetModifierText(playerDefModText, "DEF", defMod);
                SetModifierText(playerSpdModText, "SPD", spdMod);
            }
            else
            {
                if (playerAtkModText != null) playerAtkModText.gameObject.SetActive(false);
                if (playerDefModText != null) playerDefModText.gameObject.SetActive(false);
                if (playerSpdModText != null) playerSpdModText.gameObject.SetActive(false);
            }
        }

        private void SetModifierText(Text text, string statName, int mod)
        {
            if (text == null) return;
            if (mod == 0)
            {
                text.gameObject.SetActive(false);
                return;
            }
            text.gameObject.SetActive(true);
            string sign = mod > 0 ? "+" : "";
            text.text = $"{statName}:{sign}{mod}";
            // 正增益绿色，负增益红色
            text.color = mod > 0 ? UIBuilder.Green : UIBuilder.Red;
        }

        // ==================== 统一回合流程 ====================

        /// <summary>
        /// 执行完整回合：RoundStart -> 先手方Action -> 后手方Action -> RoundEnd
        /// 使用协程实现回合切换0.2s过渡动画
        /// </summary>
        private void ExecuteRound(System.Action playerAction)
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;

            // 停止之前的过渡动画
            StopTurnTransition();

            // 1. 回合开始：Tick双方状态效果 + 判定先手
            BattleManager.Instance.RoundStart();
            if (b.isFinished) { RefreshUI(); return; }

            // 先更新回合归属显示，0.2s后再执行行动
            SetActionButtons(false);
            RefreshUI();
            turnTransitionCoroutine = StartCoroutine(ExecuteRoundAfterTransition(playerAction));
        }

        private IEnumerator ExecuteRoundAfterTransition(System.Action playerAction)
        {
            yield return new WaitForSeconds(0.2f);

            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) { turnTransitionCoroutine = null; yield break; }

            // 2. 根据先手顺序执行行动
            if (b.isPlayerTurn)
            {
                // 玩家先手：玩家行动 -> 怪物行动
                playerAction();
                if (!b.isFinished) BattleManager.Instance.MonsterAction();
            }
            else
            {
                // 怪物先手：怪物行动 -> 玩家行动
                BattleManager.Instance.MonsterAction();
                if (!b.isFinished) playerAction();
            }

            // 3. 回合结束：衰减buff
            if (!b.isFinished) BattleManager.Instance.RoundEnd();

            RefreshUI();

            // 恢复按钮
            if (b != null && !b.isFinished) SetActionButtons(true);

            turnTransitionCoroutine = null;
        }

        // ==================== 玩家操作 ====================

        private void OnAttack()
        {
            StopAutoBattle();
            ExecuteRound(() => BattleManager.Instance.PlayerActionAttack());
        }

        private void OnSkillMenu()
        {
            StopAutoBattle();
            // 沉默时点击技能按钮，不打开面板
            if (BattleManager.Instance.IsPlayerSilenced()) return;
            bool showSkill = !skillPanel.gameObject.activeSelf;
            itemPanel.gameObject.SetActive(false);
            skillPanel.gameObject.SetActive(showSkill);
            if (showSkill) PopulateSkillPanel();
        }

        private void PopulateSkillPanel()
        {
            var player = GameManager.Instance.Player;
            int idx = 0;
            foreach (var skillId in player.equippedSkills)
            {
                var skill = ConfigManager.Instance.GetSkill(skillId);
                if (skill == null) continue;
                if (idx >= MaxSkillRows) break;

                var row = skillRows[idx];
                if (row.obj == null || row.btn == null || row.label == null) { idx++; continue; }
                bool canUse = player.mp >= skill.mpCost;
                row.label.text = $"{skill.name}  (MP:{skill.mpCost})";
                row.label.color = canUse ? UIBuilder.Gold : UIBuilder.TextDim;
                row.btn.interactable = canUse;
                row.btn.onClick.RemoveAllListeners();
                if (canUse)
                {
                    int captured = skillId;
                    row.btn.onClick.AddListener(() =>
                    {
                        if (skillPanel != null) skillPanel.SetActive(false);
                        OnUseSkill(captured);
                    });
                }
                row.obj.SetActive(true);
                idx++;
            }
            for (int i = idx; i < MaxSkillRows; i++)
            {
                if (skillRows[i].obj != null)
                    skillRows[i].obj.SetActive(false);
            }
        }

        private void OnUseSkill(int skillId)
        {
            StopAutoBattle();
            ExecuteRound(() => BattleManager.Instance.PlayerActionSkill(skillId));
        }

        private void OnItemMenu()
        {
            StopAutoBattle();
            bool showItem = !itemPanel.gameObject.activeSelf;
            skillPanel.gameObject.SetActive(false);
            itemPanel.gameObject.SetActive(showItem);
            if (showItem) PopulateItemPanel();
        }

        private void PopulateItemPanel()
        {
            var player = GameManager.Instance.Player;
            // 按 itemId 分组统计数量
            var groups = new Dictionary<int, int>();
            for (int i = 0; i < player.bagItems.Count; i++)
            {
                int id = player.bagItems[i];
                var item = ConfigManager.Instance.GetItem(id);
                if (item == null || item.type != ItemType.Consumable) continue;
                if (!groups.ContainsKey(id)) groups[id] = 0;
                groups[id]++;
            }

            if (groups.Count == 0)
            {
                for (int i = 0; i < MaxItemRows; i++)
                {
                    if (itemRows[i].obj != null) itemRows[i].obj.SetActive(false);
                }
                if (itemEmptyText != null) itemEmptyText.gameObject.SetActive(true);
                return;
            }
            if (itemEmptyText != null) itemEmptyText.gameObject.SetActive(false);

            int idx = 0;
            foreach (var kv in groups)
            {
                if (idx >= MaxItemRows) break;
                var item = ConfigManager.Instance.GetItem(kv.Key);
                if (item == null) continue;
                string desc = "";
                if (item.restoreHp > 0) desc += $" HP+{item.restoreHp}";
                if (item.restoreMp > 0) desc += $" MP+{item.restoreMp}";
                string countStr = kv.Value > 1 ? $" x{kv.Value}" : "";

                var row = itemRows[idx];
                if (row.obj == null || row.btn == null || row.label == null) { idx++; continue; }
                row.label.text = $"{item.name}{countStr}{desc}";
                row.btn.onClick.RemoveAllListeners();
                int capturedItemId = kv.Key;
                row.btn.onClick.AddListener(() =>
                {
                    if (itemPanel != null) itemPanel.SetActive(false);
                    OnUseItemById(capturedItemId);
                });
                row.obj.SetActive(true);
                idx++;
            }
            for (int i = idx; i < MaxItemRows; i++)
            {
                if (itemRows[i].obj != null)
                    itemRows[i].obj.SetActive(false);
            }
        }

        private void OnUseItemById(int itemId)
        {
            StopAutoBattle();
            ExecuteRound(() => BattleManager.Instance.PlayerActionUseItem(itemId));
        }

        private void OnEscape()
        {
            StopAutoBattle();
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;

            // 逃跑使用独立流程（RoundStart -> 逃跑判定 -> 失败时怪物行动）
            BattleManager.Instance.TryEscape();
            RefreshUI();
        }

        private void OnBattleEnd()
        {
            StopAutoBattle();
            var b = BattleManager.Instance.CurrentBattle;
            if (b.isVictory)
            {
                UIManager.Instance.Back();
            }
            else
            {
                var player = GameManager.Instance.Player;
                player.FullRestore();
                GameManager.Instance.SetState(GameState.InTown);
                GameManager.Instance.SaveGame();
                UIManager.Instance.ReplacePanel<TownPanel>();
            }
        }

        // ==================== 回合切换动画 ====================

        private void StopTurnTransition()
        {
            if (turnTransitionCoroutine != null)
            {
                StopCoroutine(turnTransitionCoroutine);
                turnTransitionCoroutine = null;
            }
        }

        private void SetActionButtons(bool interactable)
        {
            if (btnAttack != null) btnAttack.interactable = interactable;
            if (btnItem != null) btnItem.interactable = interactable;
            if (btnEscape != null) btnEscape.interactable = interactable;

            // 技能按钮单独处理沉默状态
            if (btnSkill != null)
            {
                if (interactable)
                {
                    bool silenced = BattleManager.Instance.IsPlayerSilenced();
                    btnSkill.interactable = !silenced;
                }
                else
                {
                    btnSkill.interactable = false;
                }
            }
        }

        // ==================== 自动战斗 ====================

        private void OnRewardContinue()
        {
            rewardOverlay.SetActive(false);
            btnResultLabel.text = "继续探索";
            btnResult.gameObject.SetActive(true);
        }

        private void OnToggleAutoBattle()
        {
            if (isAutoBattle)
                StopAutoBattle();
            else
                StartAutoBattle();
        }

        private void StartAutoBattle()
        {
            isAutoBattle = true;
            btnAutoLabel.text = "停止";
            autoBattleCoroutine = StartCoroutine(AutoBattleCoroutine());
        }

        private void StopAutoBattle()
        {
            if (!isAutoBattle) return;
            isAutoBattle = false;
            if (autoBattleCoroutine != null)
            {
                StopCoroutine(autoBattleCoroutine);
                autoBattleCoroutine = null;
            }
            btnAutoLabel.text = "Auto";
        }

        /// <summary>
        /// 自动战斗：每回合使用普攻（不受沉默影响），遵循统一回合流程
        /// </summary>
        private IEnumerator AutoBattleCoroutine()
        {
            while (isAutoBattle)
            {
                var b = BattleManager.Instance.CurrentBattle;
                if (b == null || b.isFinished) break;

                // 1. 回合开始：Tick双方状态效果 + 判定先手
                BattleManager.Instance.RoundStart();
                if (b.isFinished) { RefreshUI(); break; }

                // 2. 根据先手顺序执行行动（自动战斗使用普攻，不受沉默影响）
                if (b.isPlayerTurn)
                {
                    BattleManager.Instance.PlayerActionAttack();
                    if (!b.isFinished) BattleManager.Instance.MonsterAction();
                }
                else
                {
                    BattleManager.Instance.MonsterAction();
                    if (!b.isFinished) BattleManager.Instance.PlayerActionAttack();
                }

                // 3. 回合结束：衰减buff
                if (!b.isFinished) BattleManager.Instance.RoundEnd();

                RefreshUI();

                // 战斗结束则停止
                if (b.isFinished) break;

                yield return new WaitForSeconds(0.6f);
            }

            isAutoBattle = false;
            autoBattleCoroutine = null;
            btnAutoLabel.text = "Auto";
        }
    }
}
