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

        // ---- 缓存引用：怪物信息 ----
        private Text monsterIconText;
        private Text monsterNameText;
        private Text monsterHpText;
        private Slider monsterHpSlider;
        private Text monsterAtkDefText;
        private Text monsterTypeText;

        // ---- 缓存引用：战斗日志池 ----
        private Text[] logTexts;

        // ---- 缓存引用：玩家信息 ----
        private Text playerInfoText;

        // ---- 缓存引用：操作按钮 ----
        private Button btnAttack;
        private Button btnSkill;
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

            // === 玩家信息条（底部锚定，按钮上方 10px）===
            var playerBar = UIBuilder.CreatePanel(parent, 900, 45, "PlayerBar");
            var playerBarRt = playerBar.GetComponent<RectTransform>();
            playerBarRt.anchorMin = new Vector2(0, 0);
            playerBarRt.anchorMax = new Vector2(1, 0);
            playerBarRt.pivot = new Vector2(0.5f, 0);
            playerBarRt.anchoredPosition = new Vector2(0, 88);
            playerBarRt.sizeDelta = new Vector2(-20, 45);

            // === 战斗日志区（拉伸填充 MonsterPanel 和 PlayerBar 之间）===
            var logPanelObj = UIBuilder.CreateBorderedPanel(parent, 900, 220, "LogPanel");
            var logRt = logPanelObj.GetComponent<RectTransform>();
            logRt.anchorMin = new Vector2(0, 0);
            logRt.anchorMax = new Vector2(1, 1);
            logRt.pivot = new Vector2(0.5f, 0.5f);
            logRt.offsetMin = new Vector2(10, 143);
            logRt.offsetMax = new Vector2(-10, -250);

            var logInner = logPanelObj.transform.Find("Inner");

            for (int i = 0; i < MaxLogLines; i++)
            {
                var logObj = UIBuilder.CreateText(logInner, "", 16, UIBuilder.TextWhite, TextAnchor.MiddleLeft, name: $"Log_{i}");
                UIBuilder.SetAnchored(logObj.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0, -12 - i * 30), new Vector2(800, 28));
            }

            var playerInfoObj = UIBuilder.CreateText(playerBar.transform, "", 15, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "PlayerInfo");
            UIBuilder.SetStretch(playerInfoObj.gameObject);

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

        protected override void BindReferences()
        {
            // 怪物信息
            monsterIconText = FindComponent<Text>("MonsterPanel/Inner/MonsterIcon");
            monsterTypeText = FindComponent<Text>("MonsterPanel/Inner/MonsterType");
            monsterNameText = FindComponent<Text>("MonsterPanel/Inner/MonsterName");
            monsterHpText = FindComponent<Text>("MonsterPanel/Inner/MonsterHpText");
            monsterHpSlider = FindComponent<Slider>("MonsterPanel/Inner/MonsterHpSlider");
            monsterAtkDefText = FindComponent<Text>("MonsterPanel/Inner/MonsterAtkDef");

            // 战斗日志池
            logTexts = new Text[MaxLogLines];
            for (int i = 0; i < MaxLogLines; i++)
            {
                logTexts[i] = FindComponent<Text>($"LogPanel/Inner/Log_{i}");
                logTexts[i].gameObject.SetActive(false);
            }

            // 玩家信息
            playerInfoText = FindComponent<Text>("PlayerBar/PlayerInfo");

            // 操作按钮
            btnAttack = FindComponent<Button>("BtnAttack");
            btnSkill = FindComponent<Button>("BtnSkill");
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
        }

        /// <summary>
        /// 零销毁刷新：只修改 .text / .value / .interactable / .SetActive
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
            if (monsterHpSlider != null) monsterHpSlider.value = m.maxHp > 0 ? (float)m.hp / m.maxHp : 0;
            if (monsterAtkDefText != null) monsterAtkDefText.text = $"攻击:{m.attack}  防御:{m.defense}";

            // 战斗日志（池化 6 行）
            if (logTexts != null)
            {
                int showCount = Mathf.Min(b.log.Count, MaxLogLines);
                int startIdx = b.log.Count - showCount;
                for (int i = 0; i < MaxLogLines; i++)
                {
                    if (logTexts[i] == null) continue;
                    if (i < showCount)
                    {
                        logTexts[i].text = b.log[startIdx + i];
                        logTexts[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        logTexts[i].gameObject.SetActive(false);
                    }
                }
            }

            // 玩家信息（含buff加成）
            if (playerInfoText != null)
            {
                int buffAtk = b.playerBuffAttack;
                int buffDef = b.playerBuffDefense;
                int buffTurns = b.playerBuffTurns;
                string atkStr = buffAtk > 0 ? $"{p.attack}+{buffAtk}" : $"{p.attack}";
                string defStr = buffDef > 0 ? $"{p.defense}+{buffDef}" : $"{p.defense}";
                string buffInfo = buffTurns > 0 ? $"  [增益{buffTurns}回合]" : "";
                playerInfoText.text = $"HP:{p.hp}/{p.maxHp}  MP:{p.mp}/{p.maxMp}  攻击:{atkStr}  防御:{defStr}{buffInfo}";
            }

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
                    // 显示奖励面板
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
                if (btnAttack != null) btnAttack.interactable = true;
                if (btnSkill != null) btnSkill.interactable = true;
                if (btnItem != null) btnItem.interactable = true;
                if (btnEscape != null) btnEscape.interactable = true;
                if (btnResult != null) btnResult.gameObject.SetActive(false);
            }
        }

        private void OnAttack()
        {
            StopAutoBattle();
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;
            BattleManager.Instance.PlayerAttack();
            if (!b.isFinished) BattleManager.Instance.MonsterTurn();
            RefreshUI();
        }

        private void OnSkillMenu()
        {
            StopAutoBattle();
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
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;
            BattleManager.Instance.PlayerSkill(skillId);
            if (!b.isFinished) BattleManager.Instance.MonsterTurn();
            RefreshUI();
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
                if (itemRows[i].obj != null) itemRows[i].obj.SetActive(false);
            }
        }

        private void OnUseItemById(int itemId)
        {
            StopAutoBattle();
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;
            BattleManager.Instance.PlayerUseItem(itemId);
            if (!b.isFinished) BattleManager.Instance.MonsterTurn();
            RefreshUI();
        }

        private void OnEscape()
        {
            StopAutoBattle();
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;
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

        // ---- 自动战斗 ----

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

        private System.Collections.IEnumerator AutoBattleCoroutine()
        {
            while (isAutoBattle)
            {
                var b = BattleManager.Instance.CurrentBattle;
                if (b == null || b.isFinished) break;

                // 玩家攻击
                BattleManager.Instance.PlayerAttack();

                // 如果怪物没死，怪物回合
                if (!b.isFinished)
                    BattleManager.Instance.MonsterTurn();

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
