using System.Collections.Generic;
using Game.Core;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class SkillLearnPanel : UIPanel
    {
        // 缓存引用
        private Text goldText;
        private const int SkillPoolSize = 8;
        private const int EqBtnPoolSize = 8;
        private List<UIBuilder.PoolRow> skillRows;

        // 已装备技能区按钮池
        private Text eqLabel;
        private Button[] eqBtns;
        private Text[] eqBtnLabels;

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);
            UIBuilder.CreateTitleBar(parent, "技能学习");

            // 金币栏
            var goldBar = UIBuilder.CreatePanel(parent, 0, 40, "GoldBar");
            var goldRt = goldBar.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0, 1);
            goldRt.anchorMax = new Vector2(1, 1);
            goldRt.pivot = new Vector2(0.5f, 1);
            goldRt.anchoredPosition = new Vector2(0, -70);
            goldRt.sizeDelta = new Vector2(0, 40);
            goldBar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.9f);
            var gold = UIBuilder.CreateText(goldBar.transform, "", 18, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "GoldText");
            UIBuilder.SetStretch(gold.gameObject);

            // 技能列表
            var listPanel = UIBuilder.CreateBorderedPanel(parent, 900, 500, "SkillList");
            listPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
            var inner = listPanel.transform.Find("Inner");
            UIBuilder.CreatePoolRows(inner, SkillPoolSize, 55, 210, 800);

            // 已装备技能区
            var eqPanel = UIBuilder.CreateBorderedPanel(parent, 900, 80, "EquippedSkills");
            eqPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -260);
            var eqInner = eqPanel.transform.Find("Inner");

            var eqLbl = UIBuilder.CreateText(eqInner, "已装备技能:", 16, UIBuilder.Gold, TextAnchor.MiddleLeft, name: "EqLabel");
            UIBuilder.SetAnchored(eqLbl.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-380, 0), new Vector2(120, 30));

            // 装备/卸下按钮池
            for (int i = 0; i < EqBtnPoolSize; i++)
            {
                var (btn, label) = UIBuilder.CreateButtonWithRef(eqInner, "", 110, 30, null, 14, $"EqBtn_{i}");
                btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220 + i * 130, 0);
                btn.gameObject.SetActive(false);
            }

            // 返回按钮
            UIBuilder.CreateButton(parent, "返回", 200, 45, null, 22, "BtnBack")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -340);
        }

        protected override void BindReferences()
        {
            goldText = FindComponent<Text>("GoldBar/GoldText");
            eqLabel = FindComponent<Text>("EquippedSkills/Inner/EqLabel");

            // 从预制体中重建技能行对象池引用
            var inner = FindChild("SkillList/Inner");
            skillRows = new List<UIBuilder.PoolRow>();
            for (int i = 0; i < inner.childCount && skillRows.Count < SkillPoolSize; i++)
            {
                var child = inner.GetChild(i);
                if (child.name == "PoolRow")
                {
                    skillRows.Add(new UIBuilder.PoolRow
                    {
                        obj = child.gameObject,
                        rt = child as RectTransform,
                        bg = child.GetComponent<Image>(),
                        nameText = child.Find("NameText")?.GetComponent<Text>(),
                        actionBtn = child.Find("ActionBtn")?.GetComponent<Button>(),
                        actionBtnLabel = child.Find("ActionBtn/Label")?.GetComponent<Text>(),
                        secondBtn = child.Find("SecondBtn")?.GetComponent<Button>(),
                        secondBtnLabel = child.Find("SecondBtn/Label")?.GetComponent<Text>()
                    });
                }
            }

            // 装备/卸下按钮池
            eqBtns = new Button[EqBtnPoolSize];
            eqBtnLabels = new Text[EqBtnPoolSize];
            for (int i = 0; i < EqBtnPoolSize; i++)
            {
                eqBtns[i] = FindComponent<Button>($"EquippedSkills/Inner/EqBtn_{i}");
                eqBtnLabels[i] = FindComponent<Text>($"EquippedSkills/Inner/EqBtn_{i}/Label");
            }
        }

        protected override void BindEvents()
        {
            FindComponent<Button>("BtnBack").onClick.AddListener(() => UIManager.Instance.Back());
        }

        public override void OnShow()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            var player = GameManager.Instance.Player;

            // 金币
            goldText.text = $"金币: {player.gold}    已装备技能: {player.equippedSkills.Count}/{player.maxSkillSlots}";

            // 技能列表
            var allSkills = ConfigManager.Instance.GetSkillsByProfession(player.profession);
            int shown = 0;
            for (int si = 0; si < allSkills.Count && shown < SkillPoolSize; si++)
            {
                var skill = allSkills[si];
                bool learned = player.learnedSkills.Contains(skill.id);
                bool canLearn = !learned
                    && (skill.prereqSkillId == 0 || player.learnedSkills.Contains(skill.prereqSkillId))
                    && player.gold >= skill.learnCost
                    && player.level >= skill.level;

                var row = skillRows[shown];
                row.obj.SetActive(true);
                row.rt.anchoredPosition = new Vector2(0, 210 - shown * 59);
                row.bg.color = learned
                    ? new Color(0.1f, 0.2f, 0.1f, 0.5f)
                    : new Color(0.1f, 0.1f, 0.16f, 0.5f);

                string typeStr = skill.type switch
                {
                    SkillType.Active => "主动",
                    SkillType.Passive => "被动",
                    SkillType.Buff => "增益",
                    _ => ""
                };
                string statStr = skill.power > 0 ? $"威力:{skill.power}%" : "";
                string info = $"{skill.name} [{typeStr}] {statStr}  MP:{skill.mpCost}  {skill.desc}";
                if (skill.prereqSkillId > 0)
                {
                    var prereq = ConfigManager.Instance.GetSkill(skill.prereqSkillId);
                    if (prereq != null) info += $"  (前置:{prereq.name})";
                }
                row.nameText.text = info;
                row.nameText.color = learned ? UIBuilder.Green : UIBuilder.TextWhite;

                // 学习按钮 / 已学标签
                row.actionBtn.gameObject.SetActive(true);
                row.actionBtn.onClick.RemoveAllListeners();
                row.secondBtn.gameObject.SetActive(false);

                if (learned)
                {
                    row.actionBtn.interactable = false;
                    row.actionBtnLabel.text = "已学";
                    row.actionBtnLabel.color = UIBuilder.Green;
                    row.actionBtn.image.color = new Color(0.1f, 0.2f, 0.1f, 0.5f);
                }
                else
                {
                    row.actionBtn.interactable = canLearn;
                    row.actionBtnLabel.text = $"{skill.learnCost}金";
                    row.actionBtnLabel.color = canLearn ? UIBuilder.Gold : UIBuilder.TextDim;
                    row.actionBtn.image.color = UIBuilder.ButtonNormal;
                    int capturedId = skill.id;
                    int capturedCost = skill.learnCost;
                    row.actionBtn.onClick.AddListener(() => LearnSkill(capturedId, capturedCost));
                }

                shown++;
            }

            // 隐藏未使用的技能行
            for (int i = shown; i < SkillPoolSize; i++)
                skillRows[i].obj.SetActive(false);

            // 已装备技能 → 卸下按钮
            int eqIdx = 0;
            foreach (var skillId in player.equippedSkills)
            {
                var skill = ConfigManager.Instance.GetSkill(skillId);
                if (skill == null || eqIdx >= EqBtnPoolSize) continue;
                var btn = eqBtns[eqIdx];
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                eqBtnLabels[eqIdx].text = $"{skill.name} ×";
                eqBtnLabels[eqIdx].color = UIBuilder.Red;
                btn.image.color = UIBuilder.ButtonNormal;
                btn.onClick.RemoveAllListeners();
                int capturedSkillId = skillId;
                btn.onClick.AddListener(() => UnequipSkill(capturedSkillId));
                eqIdx++;
            }

            // 未装备的已学技能 → 装备按钮
            foreach (var skillId in player.learnedSkills)
            {
                if (player.equippedSkills.Contains(skillId)) continue;
                if (player.equippedSkills.Count >= player.maxSkillSlots) continue;
                var skill = ConfigManager.Instance.GetSkill(skillId);
                if (skill == null || eqIdx >= EqBtnPoolSize) continue;
                var btn = eqBtns[eqIdx];
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                eqBtnLabels[eqIdx].text = $"{skill.name} +";
                eqBtnLabels[eqIdx].color = UIBuilder.Green;
                btn.image.color = UIBuilder.ButtonNormal;
                btn.onClick.RemoveAllListeners();
                int capturedSkillId = skillId;
                btn.onClick.AddListener(() => EquipSkill(capturedSkillId));
                eqIdx++;
            }

            // 隐藏未使用的按钮
            for (int i = eqIdx; i < EqBtnPoolSize; i++)
                eqBtns[i].gameObject.SetActive(false);
        }

        private void LearnSkill(int skillId, int cost)
        {
            Debug.Log($"[技能] 学习技能 id={skillId} 花费={cost}");
            var player = GameManager.Instance.Player;
            if (player.gold < cost || player.learnedSkills.Contains(skillId)) return;
            player.gold -= cost;
            player.learnedSkills.Add(skillId);
            // 自动装备
            if (player.equippedSkills.Count < player.maxSkillSlots)
                player.equippedSkills.Add(skillId);
            RefreshUI();
        }

        private void EquipSkill(int skillId)
        {
            Debug.Log($"[技能] 装备技能 id={skillId}");
            var player = GameManager.Instance.Player;
            if (player.equippedSkills.Count >= player.maxSkillSlots) return;
            if (!player.equippedSkills.Contains(skillId))
                player.equippedSkills.Add(skillId);
            RefreshUI();
        }

        private void UnequipSkill(int skillId)
        {
            Debug.Log($"[技能] 卸下技能 id={skillId}");
            GameManager.Instance.Player.equippedSkills.Remove(skillId);
            RefreshUI();
        }
    }
}
