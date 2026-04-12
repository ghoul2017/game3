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
        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            UIBuilder.CreateTitleBar(transform, "技能学习");

            // 金币
            var goldBar = UIBuilder.CreatePanel(transform, 0, 40, "GoldBar");
            var goldRt = goldBar.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(0, 1);
            goldRt.anchorMax = new Vector2(1, 1);
            goldRt.pivot = new Vector2(0.5f, 1);
            goldRt.anchoredPosition = new Vector2(0, -70);
            goldRt.sizeDelta = new Vector2(0, 40);
            goldBar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.9f);

            // 技能列表
            var listPanel = UIBuilder.CreateBorderedPanel(transform, 900, 500, "SkillList");
            listPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);

            // 已装备技能区
            var equippedPanel = UIBuilder.CreateBorderedPanel(transform, 900, 80, "EquippedSkills");
            equippedPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -260);

            UIBuilder.CreateButton(transform, "返回", 200, 45, () =>
            {
                UIManager.Instance.Back();
            }, 22, "BtnBack").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -340);
        }

        public override void OnShow()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            var player = GameManager.Instance.Player;
            var profCfg = ConfigManager.Instance.GetProfession(player.profession);

            // 金币
            var goldBar = transform.Find("GoldBar");
            if (goldBar != null)
            {
                for (int i = goldBar.childCount - 1; i >= 0; i--)
                    Object.Destroy(goldBar.GetChild(i).gameObject);
                var goldText = UIBuilder.CreateText(goldBar, $"金币: {player.gold}    已装备技能: {player.equippedSkills.Count}/{player.maxSkillSlots}", 18, UIBuilder.Gold, TextAnchor.MiddleCenter);
                UIBuilder.SetStretch(goldText.gameObject);
            }

            // 可学技能列表
            var inner = transform.Find("SkillList/Inner");
            if (inner != null)
            {
                for (int i = inner.childCount - 1; i >= 0; i--)
                    Object.Destroy(inner.GetChild(i).gameObject);

                var allSkills = ConfigManager.Instance.GetSkillsByProfession(player.profession);
                int idx = 0;
                foreach (var skill in allSkills)
                {
                    bool learned = player.learnedSkills.Contains(skill.id);
                    bool canLearn = !learned
                        && (skill.prereqSkillId == 0 || player.learnedSkills.Contains(skill.prereqSkillId))
                        && player.gold >= skill.learnCost
                        && player.level >= skill.level;

                    float y = 200 - idx * 65;

                    var row = new GameObject($"Skill_{skill.id}");
                    row.transform.SetParent(inner, false);
                    var rowRt = row.AddComponent<RectTransform>();
                    rowRt.sizeDelta = new Vector2(800, 55);
                    rowRt.anchoredPosition = new Vector2(0, y);

                    var rowImg = row.AddComponent<Image>();
                    rowImg.color = learned ? new Color(0.1f, 0.2f, 0.1f, 0.5f) : new Color(0.1f, 0.1f, 0.16f, 0.5f);

                    string typeStr = skill.type switch { SkillType.Active => "主动", SkillType.Passive => "被动", SkillType.Buff => "增益", _ => "" };
                    string statStr = skill.power > 0 ? $"威力:{skill.power}%" : "";
                    string info = $"{skill.name} [{typeStr}] {statStr}  MP:{skill.mpCost}  {skill.desc}";
                    if (skill.prereqSkillId > 0)
                    {
                        var prereq = ConfigManager.Instance.GetSkill(skill.prereqSkillId);
                        if (prereq != null) info += $"  (前置:{prereq.name})";
                    }

                    var nameText = UIBuilder.CreateText(row, info, 15, learned ? UIBuilder.Green : UIBuilder.TextWhite, TextAnchor.MiddleLeft);
                    UIBuilder.SetAnchored(nameText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(-350, 0), new Vector2(550, 45));

                    if (learned)
                    {
                        UIBuilder.CreateText(row, "已学", 16, UIBuilder.Green, TextAnchor.MiddleCenter)
                            .GetComponent<RectTransform>().anchoredPosition = new Vector2(300, 0);
                    }
                    else
                    {
                        int capturedId = skill.id;
                        int capturedCost = skill.learnCost;
                        var btn = UIBuilder.CreateButton(row, $"{skill.learnCost}金", 90, 35,
                            () => LearnSkill(capturedId, capturedCost), 16, $"Learn_{skill.id}");
                        btn.interactable = canLearn;
                        btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(300, 0);
                    }
                    idx++;
                }
            }

            // 已装备技能
            var eqInner = transform.Find("EquippedSkills/Inner");
            if (eqInner != null)
            {
                for (int i = eqInner.childCount - 1; i >= 0; i--)
                    Object.Destroy(eqInner.GetChild(i).gameObject);

                var label = UIBuilder.CreateText(eqInner, "已装备技能:", 16, UIBuilder.Gold, TextAnchor.MiddleLeft);
                UIBuilder.SetAnchored(label.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(-380, 0), new Vector2(120, 30));

                int eqIdx = 0;
                foreach (var skillId in player.equippedSkills)
                {
                    var skill = ConfigManager.Instance.GetSkill(skillId);
                    if (skill == null) continue;
                    int capturedSkillId = skillId;
                    var btn = UIBuilder.CreateButton(eqInner, $"{skill.name} ×", 110, 30,
                        () => UnequipSkill(capturedSkillId), 14, $"Uneq_{skillId}");
                    btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220 + eqIdx * 130, 0);
                    eqIdx++;
                }

                // 未装备的已学技能可以装备
                foreach (var skillId in player.learnedSkills)
                {
                    if (player.equippedSkills.Contains(skillId)) continue;
                    var skill = ConfigManager.Instance.GetSkill(skillId);
                    if (skill == null || player.equippedSkills.Count >= player.maxSkillSlots) continue;
                    int capturedSkillId = skillId;
                    var btn = UIBuilder.CreateButton(eqInner, $"{skill.name} +", 110, 30,
                        () => EquipSkill(capturedSkillId), 14, $"Eq_{skillId}");
                    btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220 + eqIdx * 130, 0);
                    eqIdx++;
                }
            }
        }

        private void LearnSkill(int skillId, int cost)
        {
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
            var player = GameManager.Instance.Player;
            if (player.equippedSkills.Count >= player.maxSkillSlots) return;
            if (!player.equippedSkills.Contains(skillId))
                player.equippedSkills.Add(skillId);
            RefreshUI();
        }

        private void UnequipSkill(int skillId)
        {
            GameManager.Instance.Player.equippedSkills.Remove(skillId);
            RefreshUI();
        }
    }
}
