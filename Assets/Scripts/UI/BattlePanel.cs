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
        private Text monsterNameText;
        private Text monsterHpText;
        private Slider monsterHpSlider;
        private Text monsterAtkDefText;

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

        // ---- 缓存引用：战斗结果按钮 ----
        private Button btnResult;
        private Text btnResultLabel;

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

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            BuildUI();
        }

        private void BuildUI()
        {
            // 标题
            UIBuilder.CreateTitleBar(transform, "战  斗");

            // === 怪物信息区 ===
            var monsterPanelObj = UIBuilder.CreateBorderedPanel(transform, 800, 160, "MonsterPanel");
            monsterPanelObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 220);
            var monsterInner = monsterPanelObj.transform.Find("Inner");

            monsterNameText = UIBuilder.CreateText(monsterInner, "", 28, UIBuilder.Red, TextAnchor.MiddleCenter, true, "MonsterName");
            UIBuilder.SetAnchored(monsterNameText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(400, 35));

            monsterHpText = UIBuilder.CreateText(monsterInner, "", 18, UIBuilder.HpColor, name: "MonsterHpText");
            UIBuilder.SetAnchored(monsterHpText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-200, -60), new Vector2(200, 28));

            monsterHpSlider = UIBuilder.CreateSlider(monsterInner, 300, 20, UIBuilder.HpColor, "MonsterHpSlider");
            monsterHpSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, -70);

            monsterAtkDefText = UIBuilder.CreateText(monsterInner, "", 16, UIBuilder.TextDim, name: "MonsterAtkDef");
            UIBuilder.SetAnchored(monsterAtkDefText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(300, 24));

            // === 战斗日志区（对象池 6 行） ===
            var logPanelObj = UIBuilder.CreateBorderedPanel(transform, 800, 200, "LogPanel");
            logPanelObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
            var logInner = logPanelObj.transform.Find("Inner");

            logTexts = new Text[MaxLogLines];
            for (int i = 0; i < MaxLogLines; i++)
            {
                logTexts[i] = UIBuilder.CreateText(logInner, "", 16, UIBuilder.TextWhite, TextAnchor.MiddleLeft, name: $"Log_{i}");
                UIBuilder.SetAnchored(logTexts[i].gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0, -15 - i * 28), new Vector2(700, 26));
                logTexts[i].gameObject.SetActive(false);
            }

            // === 玩家信息条 ===
            var playerBar = UIBuilder.CreatePanel(transform, 800, 50, "PlayerBar");
            playerBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
            playerInfoText = UIBuilder.CreateText(playerBar.transform, "", 16, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "PlayerInfo");
            UIBuilder.SetStretch(playerInfoText.gameObject);

            // === 操作按钮 ===
            float btnY = -190;
            float btnW = 160;
            float gap = 180;

            btnAttack = UIBuilder.CreateButton(transform, "攻击", btnW, 45, OnAttack, 22, "BtnAttack");
            btnAttack.GetComponent<RectTransform>().anchoredPosition = new Vector2(-gap * 1.5f, btnY);

            btnSkill = UIBuilder.CreateButton(transform, "技能", btnW, 45, OnSkillMenu, 22, "BtnSkill");
            btnSkill.GetComponent<RectTransform>().anchoredPosition = new Vector2(-gap * 0.5f, btnY);

            btnItem = UIBuilder.CreateButton(transform, "物品", btnW, 45, OnItemMenu, 22, "BtnItem");
            btnItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(gap * 0.5f, btnY);

            btnEscape = UIBuilder.CreateButton(transform, "逃跑", btnW, 45, OnEscape, 22, "BtnEscape");
            btnEscape.GetComponent<RectTransform>().anchoredPosition = new Vector2(gap * 1.5f, btnY);

            // === 技能子面板（对象池 8 行） ===
            skillPanel = UIBuilder.CreateBorderedPanel(transform, 600, 350, "SkillPanel");
            skillPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
            skillPanel.gameObject.SetActive(false);

            var skillInner = skillPanel.transform.Find("Inner");
            skillRows = new SkillRow[MaxSkillRows];
            for (int i = 0; i < MaxSkillRows; i++)
            {
                var rowObj = new GameObject($"SkillRow_{i}");
                rowObj.transform.SetParent(skillInner, false);
                var rowRt = rowObj.AddComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(500, 35);
                rowRt.anchoredPosition = new Vector2(0, 130 - i * 38);

                var rowBtn = UIBuilder.CreateButton(rowObj.transform, "", 500, 35, null, 16, "Btn");
                UIBuilder.SetStretch(rowBtn.gameObject);
                var rowLabel = rowBtn.transform.Find("Label").GetComponent<Text>();
                rowObj.SetActive(false);

                skillRows[i] = new SkillRow { obj = rowObj, btn = rowBtn, label = rowLabel };
            }

            // 技能面板关闭按钮
            UIBuilder.CreateButton(skillInner, "关闭", 240, 35,
                () => skillPanel.gameObject.SetActive(false), 18, "BtnCloseSkill")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 130 - MaxSkillRows * 38);

            // === 物品选择子面板（新增，对象池 8 行） ===
            itemPanel = UIBuilder.CreateBorderedPanel(transform, 600, 350, "ItemPanel");
            itemPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
            itemPanel.gameObject.SetActive(false);

            var itemInner = itemPanel.transform.Find("Inner");
            itemRows = new ItemRow[MaxItemRows];
            for (int i = 0; i < MaxItemRows; i++)
            {
                var rowObj = new GameObject($"ItemRow_{i}");
                rowObj.transform.SetParent(itemInner, false);
                var rowRt = rowObj.AddComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(500, 35);
                rowRt.anchoredPosition = new Vector2(0, 130 - i * 38);

                var rowBtn = UIBuilder.CreateButton(rowObj.transform, "", 500, 35, null, 16, "Btn");
                UIBuilder.SetStretch(rowBtn.gameObject);
                var rowLabel = rowBtn.transform.Find("Label").GetComponent<Text>();
                rowObj.SetActive(false);

                itemRows[i] = new ItemRow { obj = rowObj, btn = rowBtn, label = rowLabel };
            }

            // 物品面板关闭按钮
            UIBuilder.CreateButton(itemInner, "关闭", 240, 35,
                () => itemPanel.gameObject.SetActive(false), 18, "BtnCloseItem")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 130 - MaxItemRows * 38);

            // === 战斗结果按钮（默认隐藏） ===
            var (resultBtn, resultLabel) = UIBuilder.CreateButtonWithRef(transform, "", 220, 50, OnBattleEnd, 24, "BtnResult");
            resultBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -260);
            btnResult = resultBtn;
            btnResultLabel = resultLabel;
            btnResult.gameObject.SetActive(false);
        }

        public override void OnShow()
        {
            skillPanel.gameObject.SetActive(false);
            itemPanel.gameObject.SetActive(false);
            btnResult.gameObject.SetActive(false);
            RefreshUI();
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
            monsterNameText.text = m.name;
            monsterHpText.text = $"HP: {m.hp}/{m.maxHp}";
            monsterHpSlider.value = m.maxHp > 0 ? (float)m.hp / m.maxHp : 0;
            monsterAtkDefText.text = $"攻击:{m.attack}  防御:{m.defense}";

            // 战斗日志（池化 6 行）
            int showCount = Mathf.Min(b.log.Count, MaxLogLines);
            int startIdx = b.log.Count - showCount;
            for (int i = 0; i < MaxLogLines; i++)
            {
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

            // 玩家信息（含buff加成）
            int buffAtk = BattleManager.Instance.CurrentBattle?.playerBuffAttack ?? 0;
            int buffDef = BattleManager.Instance.CurrentBattle?.playerBuffDefense ?? 0;
            int buffTurns = BattleManager.Instance.CurrentBattle?.playerBuffTurns ?? 0;
            string atkStr = buffAtk > 0 ? $"{p.attack}+{buffAtk}" : $"{p.attack}";
            string defStr = buffDef > 0 ? $"{p.defense}+{buffDef}" : $"{p.defense}";
            string buffInfo = buffTurns > 0 ? $"  [增益{buffTurns}回合]" : "";
            playerInfoText.text = $"HP:{p.hp}/{p.maxHp}  MP:{p.mp}/{p.maxMp}  攻击:{atkStr}  防御:{defStr}{buffInfo}";

            // 战斗结束处理
            if (b.isFinished)
            {
                btnAttack.interactable = false;
                btnSkill.interactable = false;
                btnItem.interactable = false;
                btnEscape.interactable = false;
                skillPanel.gameObject.SetActive(false);
                itemPanel.gameObject.SetActive(false);
                btnResultLabel.text = b.isVictory ? "继续探索" : "返回城镇";
                btnResult.gameObject.SetActive(true);
            }
            else
            {
                btnAttack.interactable = true;
                btnSkill.interactable = true;
                btnItem.interactable = true;
                btnEscape.interactable = true;
                btnResult.gameObject.SetActive(false);
            }
        }

        private void OnAttack()
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;
            BattleManager.Instance.PlayerAttack();
            if (!b.isFinished) BattleManager.Instance.MonsterTurn();
            RefreshUI();
        }

        private void OnSkillMenu()
        {
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
                row.label.text = $"{skill.name} (MP:{skill.mpCost})";
                row.btn.onClick.RemoveAllListeners();
                int captured = skillId;
                row.btn.onClick.AddListener(() =>
                {
                    skillPanel.gameObject.SetActive(false);
                    OnUseSkill(captured);
                });
                row.obj.SetActive(true);
                idx++;
            }
            for (int i = idx; i < MaxSkillRows; i++)
                skillRows[i].obj.SetActive(false);
        }

        private void OnUseSkill(int skillId)
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;
            BattleManager.Instance.PlayerSkill(skillId);
            if (!b.isFinished) BattleManager.Instance.MonsterTurn();
            RefreshUI();
        }

        private void OnItemMenu()
        {
            bool showItem = !itemPanel.gameObject.activeSelf;
            skillPanel.gameObject.SetActive(false);
            itemPanel.gameObject.SetActive(showItem);
            if (showItem) PopulateItemPanel();
        }

        private void PopulateItemPanel()
        {
            var player = GameManager.Instance.Player;
            int idx = 0;
            for (int i = 0; i < player.bagItems.Count; i++)
            {
                var item = ConfigManager.Instance.GetItem(player.bagItems[i]);
                if (item == null || item.type != ItemType.Consumable) continue;
                if (idx >= MaxItemRows) break;

                var row = itemRows[idx];
                string desc = "";
                if (item.restoreHp > 0) desc += $" HP+{item.restoreHp}";
                if (item.restoreMp > 0) desc += $" MP+{item.restoreMp}";
                row.label.text = $"{item.name}{desc}";
                row.btn.onClick.RemoveAllListeners();
                int capturedItemId = player.bagItems[i];
                row.btn.onClick.AddListener(() =>
                {
                    itemPanel.gameObject.SetActive(false);
                    OnUseItemById(capturedItemId);
                });
                row.obj.SetActive(true);
                idx++;
            }
            for (int i = idx; i < MaxItemRows; i++)
                itemRows[i].obj.SetActive(false);
        }

        private void OnUseItemById(int itemId)
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;
            BattleManager.Instance.PlayerUseItem(itemId);
            if (!b.isFinished) BattleManager.Instance.MonsterTurn();
            RefreshUI();
        }

        private void OnEscape()
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;
            BattleManager.Instance.TryEscape();
            RefreshUI();
        }

        private void OnBattleEnd()
        {
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
    }
}
