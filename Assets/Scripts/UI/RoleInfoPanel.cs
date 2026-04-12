using System.Collections.Generic;
using Game.Core;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class RoleInfoPanel : UIPanel
    {
        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            UIBuilder.CreateTitleBar(transform, "角色信息");

            // 左栏：基础属性
            var attrPanel = UIBuilder.CreateBorderedPanel(transform, 420, 350, "AttrPanel");
            attrPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-240, 60);

            // 右栏：装备+技能
            var equipPanel = UIBuilder.CreateBorderedPanel(transform, 420, 160, "EquipPanel");
            equipPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(240, 155);

            var skillPanel = UIBuilder.CreateBorderedPanel(transform, 420, 160, "SkillPanel");
            skillPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(240, -35);

            // 经验条
            var expPanel = UIBuilder.CreateBorderedPanel(transform, 420, 50, "ExpPanel");
            expPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-240, -150);

            // 宝物
            var treasurePanel = UIBuilder.CreateBorderedPanel(transform, 900, 80, "TreasurePanel");
            treasurePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -230);

            UIBuilder.CreateButton(transform, "返回", 200, 45, () =>
            {
                UIManager.Instance.Back();
            }, 22, "BtnBack").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -310);
        }

        public override void OnShow()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            var player = GameManager.Instance.Player;
            var profCfg = ConfigManager.Instance.GetProfession(player.profession);

            // 基础属性
            var attrInner = transform.Find("AttrPanel/Inner");
            if (attrInner != null)
            {
                for (int i = attrInner.childCount - 1; i >= 0; i--)
                    Object.Destroy(attrInner.GetChild(i).gameObject);

                var title = UIBuilder.CreateText(attrInner, $"{player.name}  Lv.{player.level}  {profCfg.name}", 22, UIBuilder.Gold, TextAnchor.MiddleCenter, true);
                UIBuilder.SetAnchored(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(380, 35));

                string[] names = { "力量", "敏捷", "智力", "体力", "", "生命", "法力", "攻击", "防御", "暴击率", "闪避率" };
                string[] values = {
                    player.strength.ToString(), player.agility.ToString(), player.intelligence.ToString(), player.vitality.ToString(),
                    "",
                    $"{player.hp}/{player.maxHp}", $"{player.mp}/{player.maxMp}",
                    player.attack.ToString(), player.defense.ToString(),
                    $"{player.critRate}%", $"{player.dodgeRate}%"
                };

                for (int i = 0; i < names.Length; i++)
                {
                    if (string.IsNullOrEmpty(names[i])) continue;
                    float y = -50 - (i > 4 ? i - 1 : i) * 28;
                    Color color = i < 4 ? UIBuilder.TextWhite : UIBuilder.Blue;
                    var attrText = UIBuilder.CreateText(attrInner, $"{names[i]}:  {values[i]}", 16, color, TextAnchor.MiddleLeft);
                    UIBuilder.SetAnchored(attrText.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(-160, y), new Vector2(320, 26));
                }
            }

            // 装备
            var equipInner = transform.Find("EquipPanel/Inner");
            if (equipInner != null)
            {
                for (int i = equipInner.childCount - 1; i >= 0; i--)
                    Object.Destroy(equipInner.GetChild(i).gameObject);

                var eqTitle = UIBuilder.CreateText(equipInner, "装备", 18, UIBuilder.Gold, TextAnchor.MiddleCenter, true);
                UIBuilder.SetAnchored(eqTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -5), new Vector2(200, 26));

                string[] slots = { "武器", "防具", "饰品" };
                int[] ids = { player.weaponId, player.armorId, player.accessoryId };
                for (int i = 0; i < 3; i++)
                {
                    var item = ConfigManager.Instance.GetItem(ids[i]);
                    string itemName = item != null ? item.name : "空";
                    Color nameColor = item != null ? UIBuilder.TextWhite : UIBuilder.TextDim;
                    var eqText = UIBuilder.CreateText(equipInner, $"{slots[i]}: {itemName}", 16, nameColor, TextAnchor.MiddleLeft);
                    UIBuilder.SetAnchored(eqText.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(-160, -35 - i * 30), new Vector2(320, 26));
                }
            }

            // 技能
            var skillInner = transform.Find("SkillPanel/Inner");
            if (skillInner != null)
            {
                for (int i = skillInner.childCount - 1; i >= 0; i--)
                    Object.Destroy(skillInner.GetChild(i).gameObject);

                var skTitle = UIBuilder.CreateText(skillInner, "已装备技能", 18, UIBuilder.Gold, TextAnchor.MiddleCenter, true);
                UIBuilder.SetAnchored(skTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -5), new Vector2(200, 26));

                int idx = 0;
                foreach (var skillId in player.equippedSkills)
                {
                    var skill = ConfigManager.Instance.GetSkill(skillId);
                    if (skill == null) continue;
                    string typeStr = skill.type switch { SkillType.Active => "主", SkillType.Passive => "被", SkillType.Buff => "增", _ => "" };
                    var skText = UIBuilder.CreateText(skillInner, $"[{typeStr}] {skill.name} - {skill.desc}", 15, UIBuilder.TextWhite, TextAnchor.MiddleLeft);
                    UIBuilder.SetAnchored(skText.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(-160, -35 - idx * 26), new Vector2(380, 24));
                    idx++;
                }
            }

            // 经验条
            var expInner = transform.Find("ExpPanel/Inner");
            if (expInner != null)
            {
                for (int i = expInner.childCount - 1; i >= 0; i--)
                    Object.Destroy(expInner.GetChild(i).gameObject);

                int expToNext = player.GetExpToNextLevel();
                float expRatio = expToNext > 0 ? (float)player.exp / expToNext : 0f;

                var expSlider = UIBuilder.CreateSlider(expInner, 300, 20, UIBuilder.ExpColor, "ExpSlider");
                expSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, 0);
                expSlider.value = expRatio;

                var expText = UIBuilder.CreateText(expInner, $"EXP: {player.exp}/{expToNext}", 14, UIBuilder.TextDim, TextAnchor.MiddleCenter);
                UIBuilder.SetAnchored(expText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(300, 20));
            }

            // 宝物
            var treasureInner = transform.Find("TreasurePanel/Inner");
            if (treasureInner != null)
            {
                for (int i = treasureInner.childCount - 1; i >= 0; i--)
                    Object.Destroy(treasureInner.GetChild(i).gameObject);

                var trTitle = UIBuilder.CreateText(treasureInner, "宝物:", 16, UIBuilder.Gold, TextAnchor.MiddleLeft);
                UIBuilder.SetAnchored(trTitle.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(-400, 0), new Vector2(60, 28));

                if (player.treasures.Count == 0)
                {
                    var emptyText = UIBuilder.CreateText(treasureInner, "暂无宝物", 15, UIBuilder.TextDim, TextAnchor.MiddleLeft);
                    UIBuilder.SetAnchored(emptyText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(-330, 0), new Vector2(200, 28));
                }
                else
                {
                    int tIdx = 0;
                    foreach (var tId in player.treasures)
                    {
                        var treasure = ConfigManager.Instance.GetTreasure(tId);
                        if (treasure == null) continue;
                        var trText = UIBuilder.CreateText(treasureInner, $"{treasure.name}: {treasure.desc}", 15, UIBuilder.Gold, TextAnchor.MiddleLeft);
                        UIBuilder.SetAnchored(trText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                            new Vector2(-330 + tIdx * 300, 0), new Vector2(290, 28));
                        tIdx++;
                    }
                }
            }
        }
    }
}
