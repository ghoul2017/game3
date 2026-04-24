using Game.Core;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class RoleInfoPanel : UIPanel
    {
        // 缓存引用
        private Text titleText;
        private Text[] attrTexts = new Text[10];
        private Text[] equipTexts = new Text[3];
        private Text[] skillTexts = new Text[4];
        private Slider expSlider;
        private Text expText;
        private Text emptyTreasureText;
        private Text[] treasureTexts = new Text[6];

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);
            UIBuilder.CreateTitleBar(parent, "角色信息");

            var attrPanel = UIBuilder.CreateBorderedPanel(parent, 420, 350, "AttrPanel");
            attrPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-240, 60);
            var attrInner = attrPanel.transform.Find("Inner");

            var title = UIBuilder.CreateText(attrInner, "", 22, UIBuilder.Gold, TextAnchor.MiddleCenter, true, "Title");
            UIBuilder.SetAnchored(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(380, 35));

            for (int i = 0; i < 10; i++)
            {
                Color color = i < 4 ? UIBuilder.TextWhite : UIBuilder.Blue;
                float y = -50 - (i < 4 ? i : i + 1) * 28;
                var t = UIBuilder.CreateText(attrInner, "", 16, color, TextAnchor.MiddleLeft, name: $"Attr_{i}");
                UIBuilder.SetAnchored(t.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-48, y), new Vector2(320, 26));
            }

            var equipPanel = UIBuilder.CreateBorderedPanel(parent, 420, 160, "EquipPanel");
            equipPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(240, 155);
            var equipInner = equipPanel.transform.Find("Inner");

            var equipTitle = UIBuilder.CreateText(equipInner, "装备", 18, UIBuilder.Gold, TextAnchor.MiddleCenter, true, "EquipTitle");
            UIBuilder.SetAnchored(equipTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -5), new Vector2(200, 26));

            for (int i = 0; i < 3; i++)
            {
                var t = UIBuilder.CreateText(equipInner, "", 16, UIBuilder.TextDim, TextAnchor.MiddleLeft, name: $"Equip_{i}");
                UIBuilder.SetAnchored(t.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-48, -35 - i * 30), new Vector2(320, 26));
            }

            var skillPanel = UIBuilder.CreateBorderedPanel(parent, 420, 160, "SkillPanel");
            skillPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(240, -35);
            var skillInner = skillPanel.transform.Find("Inner");

            var skillTitle = UIBuilder.CreateText(skillInner, "已装备技能", 18, UIBuilder.Gold, TextAnchor.MiddleCenter, true, "SkillTitle");
            UIBuilder.SetAnchored(skillTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -5), new Vector2(200, 26));

            for (int i = 0; i < 4; i++)
            {
                var t = UIBuilder.CreateText(skillInner, "", 15, UIBuilder.TextWhite, TextAnchor.MiddleLeft, name: $"Skill_{i}");
                UIBuilder.SetAnchored(t.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-10, -35 - i * 26), new Vector2(380, 24));
                t.gameObject.SetActive(false);
            }

            var expPanel = UIBuilder.CreateBorderedPanel(parent, 420, 50, "ExpPanel");
            expPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-240, -150);
            var expInner = expPanel.transform.Find("Inner");

            var slider = UIBuilder.CreateSlider(expInner, 300, 20, UIBuilder.ExpColor, "ExpSlider");
            slider.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, 0);

            var expTxt = UIBuilder.CreateText(expInner, "", 14, UIBuilder.TextDim, TextAnchor.MiddleCenter, name: "ExpText");
            UIBuilder.SetAnchored(expTxt.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(300, 20));

            var treasurePanel = UIBuilder.CreateBorderedPanel(parent, 900, 80, "TreasurePanel");
            treasurePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -230);
            var treasureInner = treasurePanel.transform.Find("Inner");

            var trTitle = UIBuilder.CreateText(treasureInner, "宝物:", 16, UIBuilder.Gold, TextAnchor.MiddleLeft, name: "TreasureTitle");
            UIBuilder.SetAnchored(trTitle.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400, 0), new Vector2(60, 28));

            var empty = UIBuilder.CreateText(treasureInner, "暂无宝物", 15, UIBuilder.TextDim, TextAnchor.MiddleLeft, name: "EmptyTreasure");
            UIBuilder.SetAnchored(empty.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330, 0), new Vector2(200, 28));
            empty.gameObject.SetActive(false);

            for (int i = 0; i < 6; i++)
            {
                var t = UIBuilder.CreateText(treasureInner, "", 15, UIBuilder.Gold, TextAnchor.MiddleLeft, name: $"Treasure_{i}");
                UIBuilder.SetAnchored(t.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330 + i * 300, 0), new Vector2(290, 28));
                t.gameObject.SetActive(false);
            }

            // 返回按钮（底部锚定，确保始终在屏幕内）
            var btnBack = UIBuilder.CreateButton(parent, "返回", 200, 45, null, 22, "BtnBack");
            var btnBackRt = btnBack.GetComponent<RectTransform>();
            btnBackRt.anchorMin = new Vector2(0.5f, 0);
            btnBackRt.anchorMax = new Vector2(0.5f, 0);
            btnBackRt.pivot = new Vector2(0.5f, 0);
            btnBackRt.anchoredPosition = new Vector2(0, 30);
        }

        protected override void BindReferences()
        {
            titleText = FindComponent<Text>("AttrPanel/Inner/Title");

            for (int i = 0; i < 10; i++)
                attrTexts[i] = FindComponent<Text>($"AttrPanel/Inner/Attr_{i}");

            for (int i = 0; i < 3; i++)
                equipTexts[i] = FindComponent<Text>($"EquipPanel/Inner/Equip_{i}");

            for (int i = 0; i < 4; i++)
                skillTexts[i] = FindComponent<Text>($"SkillPanel/Inner/Skill_{i}");

            expSlider = FindComponent<Slider>("ExpPanel/Inner/ExpSlider");
            expText = FindComponent<Text>("ExpPanel/Inner/ExpText");

            emptyTreasureText = FindComponent<Text>("TreasurePanel/Inner/EmptyTreasure");
            for (int i = 0; i < 6; i++)
                treasureTexts[i] = FindComponent<Text>($"TreasurePanel/Inner/Treasure_{i}");
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
            var profCfg = ConfigManager.Instance.GetProfession(player.profession);

            titleText.text = $"{player.name}  Lv.{player.level}  {profCfg.name}";

            string[] names = { "力量", "敏捷", "智力", "体力", "生命", "法力", "攻击", "防御", "暴击率", "闪避率" };
            string[] values = {
                player.strength.ToString(), player.agility.ToString(), player.intelligence.ToString(), player.vitality.ToString(),
                $"{player.hp}/{player.maxHp}", $"{player.mp}/{player.maxMp}",
                player.attack.ToString(), player.defense.ToString(),
                $"{player.critRate}%", $"{player.dodgeRate}%"
            };
            for (int i = 0; i < 10; i++)
                attrTexts[i].text = $"{names[i]}:  {values[i]}";

            string[] slots = { "武器", "防具", "饰品" };
            int[] ids = { player.weaponId, player.armorId, player.accessoryId };
            for (int i = 0; i < 3; i++)
            {
                var item = ConfigManager.Instance.GetItem(ids[i]);
                equipTexts[i].text = $"{slots[i]}: {(item != null ? item.name : "空")}";
                equipTexts[i].color = item != null ? UIBuilder.TextWhite : UIBuilder.TextDim;
            }

            int sIdx = 0;
            foreach (var skillId in player.equippedSkills)
            {
                if (sIdx >= 4) break;
                var skill = ConfigManager.Instance.GetSkill(skillId);
                if (skill == null) continue;
                string typeStr = skill.type switch { SkillType.Active => "主", SkillType.Passive => "被", SkillType.Buff => "增", SkillType.StatusAttack => "状", SkillType.Heal => "治", SkillType.Debuff => "减", SkillType.DoT => "持", SkillType.Control => "控", _ => "" };
                skillTexts[sIdx].text = $"[{typeStr}] {skill.name} - {skill.desc}";
                skillTexts[sIdx].gameObject.SetActive(true);
                sIdx++;
            }
            for (int i = sIdx; i < 4; i++)
                skillTexts[i].gameObject.SetActive(false);

            int expToNext = player.GetExpToNextLevel();
            float expRatio = expToNext > 0 ? (float)player.exp / expToNext : 0f;
            expSlider.value = expRatio;
            expText.text = $"EXP: {player.exp}/{expToNext}";

            if (player.treasures.Count == 0)
            {
                emptyTreasureText.gameObject.SetActive(true);
                for (int i = 0; i < 6; i++) treasureTexts[i].gameObject.SetActive(false);
            }
            else
            {
                emptyTreasureText.gameObject.SetActive(false);
                int tIdx = 0;
                foreach (var tId in player.treasures)
                {
                    if (tIdx >= 6) break;
                    var treasure = ConfigManager.Instance.GetTreasure(tId);
                    if (treasure == null) continue;
                    treasureTexts[tIdx].text = $"{treasure.name}: {treasure.desc}";
                    treasureTexts[tIdx].gameObject.SetActive(true);
                    tIdx++;
                }
                for (int i = tIdx; i < 6; i++) treasureTexts[i].gameObject.SetActive(false);
            }
        }
    }
}
