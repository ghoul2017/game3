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
        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            BuildUI();
        }

        public override void OnShow()
        {
            RefreshUI();
        }

        private void BuildUI()
        {
            // 标题
            UIBuilder.CreateTitleBar(transform, "战  斗");

            // 怪物信息区（上半部）
            var monsterPanel = UIBuilder.CreateBorderedPanel(transform, 800, 160, "MonsterPanel");
            monsterPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 220);

            // 战斗日志（中部）
            var logPanel = UIBuilder.CreateBorderedPanel(transform, 800, 200, "LogPanel");
            logPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);

            // 玩家信息条
            var playerBar = UIBuilder.CreatePanel(transform, 800, 50, "PlayerBar");
            playerBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);

            // 按钮区
            float btnY = -190;
            float btnW = 160;
            float gap = 180;

            UIBuilder.CreateButton(transform, "攻击", btnW, 45, OnAttack, 22, "BtnAttack")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-gap * 1.5f, btnY);

            UIBuilder.CreateButton(transform, "技能", btnW, 45, OnSkillMenu, 22, "BtnSkill")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-gap * 0.5f, btnY);

            UIBuilder.CreateButton(transform, "物品", btnW, 45, OnUseItem, 22, "BtnItem")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(gap * 0.5f, btnY);

            UIBuilder.CreateButton(transform, "逃跑", btnW, 45, OnEscape, 22, "BtnEscape")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(gap * 1.5f, btnY);

            // 技能子面板（默认隐藏）
            var skillPanel = UIBuilder.CreateBorderedPanel(transform, 600, 200, "SkillPanel");
            skillPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -190);
            skillPanel.gameObject.SetActive(false);
        }

        private void RefreshUI()
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null) return;
            var p = GameManager.Instance.Player;
            var m = b.monster;

            // 怪物信息
            var monsterPanel = transform.Find("MonsterPanel/Inner");
            if (monsterPanel != null)
            {
                for (int i = monsterPanel.childCount - 1; i >= 0; i--)
                    Object.Destroy(monsterPanel.GetChild(i).gameObject);

                var nameText = UIBuilder.CreateText(monsterPanel, m.name, 28, UIBuilder.Red, TextAnchor.MiddleCenter, true);
                UIBuilder.SetAnchored(nameText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(400, 35));

                // HP条
                var hpLabel = UIBuilder.CreateText(monsterPanel, $"HP: {m.hp}/{m.maxHp}", 18, UIBuilder.HpColor);
                UIBuilder.SetAnchored(hpLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-250, -60), new Vector2(200, 28));

                var hpSlider = UIBuilder.CreateSlider(monsterPanel, 300, 20, UIBuilder.HpColor, "MonsterHp");
                hpSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, -70);
                hpSlider.value = m.maxHp > 0 ? (float)m.hp / m.maxHp : 0;

                var atkText = UIBuilder.CreateText(monsterPanel, $"攻击:{m.attack}  防御:{m.defense}", 16, UIBuilder.TextDim);
                UIBuilder.SetAnchored(atkText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(300, 24));
            }

            // 战斗日志
            var logPanel = transform.Find("LogPanel/Inner");
            if (logPanel != null)
            {
                for (int i = logPanel.childCount - 1; i >= 0; i--)
                    Object.Destroy(logPanel.GetChild(i).gameObject);

                int showCount = Mathf.Min(b.log.Count, 6);
                int startIdx = b.log.Count - showCount;
                for (int i = 0; i < showCount; i++)
                {
                    var logText = UIBuilder.CreateText(logPanel, b.log[startIdx + i], 16, UIBuilder.TextWhite, TextAnchor.MiddleLeft);
                    UIBuilder.SetAnchored(logText.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(-350, -15 - i * 28), new Vector2(700, 26));
                }
            }

            // 玩家信息
            var playerBar = transform.Find("PlayerBar");
            if (playerBar != null)
            {
                for (int i = playerBar.childCount - 1; i >= 0; i--)
                    Object.Destroy(playerBar.GetChild(i).gameObject);
                string info = $"HP:{p.hp}/{p.maxHp}  MP:{p.mp}/{p.maxMp}  攻击:{p.attack}  防御:{p.defense}";
                var t = UIBuilder.CreateText(playerBar, info, 16, UIBuilder.Gold, TextAnchor.MiddleCenter);
                UIBuilder.SetStretch(t.gameObject);
            }

            // 战斗结束处理
            if (b.isFinished)
            {
                SetButtonsInteractable(false);
                // 添加结果按钮
                if (transform.Find("BtnResult") == null)
                {
                    string resultText = b.isVictory ? "继续探索" : "返回城镇";
                    UIBuilder.CreateButton(transform, resultText, 220, 50, OnBattleEnd, 24, "BtnResult")
                        .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -260);
                }
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            string[] btnNames = { "BtnAttack", "BtnSkill", "BtnItem", "BtnEscape" };
            foreach (var name in btnNames)
            {
                var btn = transform.Find(name)?.GetComponent<Button>();
                if (btn != null) btn.interactable = interactable;
            }
        }

        private void OnAttack()
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;

            string msg = BattleManager.Instance.PlayerAttack();
            if (!b.isFinished)
                msg += "\n" + BattleManager.Instance.MonsterTurn();
            RefreshUI();
        }

        private void OnSkillMenu()
        {
            var skillPanel = transform.Find("SkillPanel");
            if (skillPanel == null) return;

            bool isActive = skillPanel.gameObject.activeSelf;
            skillPanel.gameObject.SetActive(!isActive);

            if (!isActive)
            {
                var inner = skillPanel.Find("Inner");
                if (inner == null) return;
                for (int i = inner.childCount - 1; i >= 0; i--)
                    Object.Destroy(inner.GetChild(i).gameObject);

                var player = GameManager.Instance.Player;
                int idx = 0;
                foreach (var skillId in player.equippedSkills)
                {
                    var skill = ConfigManager.Instance.GetSkill(skillId);
                    if (skill == null) continue;
                    int captured = skillId;
                    float y = 70 - idx * 40;
                    UIBuilder.CreateButton(inner, $"{skill.name} (MP:{skill.mpCost})", 240, 35,
                        () => { skillPanel.gameObject.SetActive(false); OnUseSkill(captured); }, 18, $"Skill_{skillId}")
                        .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, y);
                    idx++;
                }

                // 关闭按钮
                UIBuilder.CreateButton(inner, "关闭", 240, 35,
                    () => skillPanel.gameObject.SetActive(false), 18, "BtnCloseSkill")
                    .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 70 - idx * 40);
            }
        }

        private void OnUseSkill(int skillId)
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;

            string msg = BattleManager.Instance.PlayerSkill(skillId);
            if (!b.isFinished)
                msg += "\n" + BattleManager.Instance.MonsterTurn();
            RefreshUI();
        }

        private void OnUseItem()
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return;

            var player = GameManager.Instance.Player;
            // 使用背包中第一个消耗品
            int consumableIdx = -1;
            for (int i = 0; i < player.bagItems.Count; i++)
            {
                var item = ConfigManager.Instance.GetItem(player.bagItems[i]);
                if (item != null && item.type == ItemType.Consumable)
                {
                    consumableIdx = i;
                    break;
                }
            }

            if (consumableIdx >= 0)
            {
                string msg = BattleManager.Instance.PlayerUseItem(player.bagItems[consumableIdx]);
                if (!b.isFinished)
                    msg += "\n" + BattleManager.Instance.MonsterTurn();
            }
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
                // 继续探索，回到地牢
                UIManager.Instance.Back();
            }
            else
            {
                // 战败，回到城镇
                var player = GameManager.Instance.Player;
                player.FullRestore();
                GameManager.Instance.SetState(GameState.InTown);
                GameManager.Instance.SaveGame();
                UIManager.Instance.ReplacePanel<TownPanel>();
            }
        }
    }
}
