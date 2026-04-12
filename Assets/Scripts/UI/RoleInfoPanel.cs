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
        private Text[] attrTexts = new Text[10];   // 力/敏/智/体 + HP/MP/攻/防/暴/闪
        private Text[] equipTexts = new Text[3];    // 武器/防具/饰品
        private Text[] skillTexts = new Text[4];    // maxSkillSlots
        private Slider expSlider;
        private Text expText;
        private Text emptyTreasureText;
        private Text[] treasureTexts = new Text[6]; // 宝物行

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            UIBuilder.CreateTitleBar(transform, "角色信息");

            // ── 左栏：基础属性 ──
            var attrPanel = UIBuilder.CreateBorderedPanel(transform, 420, 350, "AttrPanel");
            attrPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-240, 60);
            var attrInner = attrPanel.transform.Find("Inner");

            titleText = UIBuilder.CreateText(attrInner, "", 22, UIBuilder.Gold, TextAnchor.MiddleCenter, true);
            UIBuilder.SetAnchored(titleText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -20), new Vector2(380, 35));

            for (int i = 0; i < 10; i++)
            {
                // 前四项为白色（基础属性），后六项为蓝色（战斗属性）
                Color color = i < 4 ? UIBuilder.TextWhite : UIBuilder.Blue;
                // index 4 起多偏移 28px，形成视觉分隔（对应原来的空行 index=4）
                float y = -50 - (i < 4 ? i : i + 1) * 28;
                attrTexts[i] = UIBuilder.CreateText(attrInner, "", 16, color, TextAnchor.MiddleLeft);
                UIBuilder.SetAnchored(attrTexts[i].gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(-48, y), new Vector2(320, 26));
            }

            // ── 右栏上：装备 ──
            var equipPanel = UIBuilder.CreateBorderedPanel(transform, 420, 160, "EquipPanel");
            equipPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(240, 155);
            var equipInner = equipPanel.transform.Find("Inner");

            var equipTitle = UIBuilder.CreateText(equipInner, "装备", 18, UIBuilder.Gold, TextAnchor.MiddleCenter, true);
            UIBuilder.SetAnchored(equipTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -5), new Vector2(200, 26));

            for (int i = 0; i < 3; i++)
            {
                equipTexts[i] = UIBuilder.CreateText(equipInner, "", 16, UIBuilder.TextDim, TextAnchor.MiddleLeft);
                UIBuilder.SetAnchored(equipTexts[i].gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(-48, -35 - i * 30), new Vector2(320, 26));
            }

            // ── 右栏下：技能 ──
            var skillPanel = UIBuilder.CreateBorderedPanel(transform, 420, 160, "SkillPanel");
            skillPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(240, -35);
            var skillInner = skillPanel.transform.Find("Inner");

            var skillTitle = UIBuilder.CreateText(skillInner, "已装备技能", 18, UIBuilder.Gold, TextAnchor.MiddleCenter, true);
            UIBuilder.SetAnchored(skillTitle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -5), new Vector2(200, 26));

            for (int i = 0; i < 4; i++)
            {
                skillTexts[i] = UIBuilder.CreateText(skillInner, "", 15, UIBuilder.TextWhite, TextAnchor.MiddleLeft);
                UIBuilder.SetAnchored(skillTexts[i].gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(-10, -35 - i * 26), new Vector2(380, 24));
                skillTexts[i].gameObject.SetActive(false);
            }

            // ── 经验条 ──
            var expPanel = UIBuilder.CreateBorderedPanel(transform, 420, 50, "ExpPanel");
            expPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-240, -150);
            var expInner = expPanel.transform.Find("Inner");

            expSlider = UIBuilder.CreateSlider(expInner, 300, 20, UIBuilder.ExpColor, "ExpSlider");
            expSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, 0);

            expText = UIBuilder.CreateText(expInner, "", 14, UIBuilder.TextDim, TextAnchor.MiddleCenter);
            UIBuilder.SetAnchored(expText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(300, 20));

            // ── 宝物 ──
            var treasurePanel = UIBuilder.CreateBorderedPanel(transform, 900, 80, "TreasurePanel");
            treasurePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -230);
            var treasureInner = treasurePanel.transform.Find("Inner");

            var trTitle = UIBuilder.CreateText(treasureInner, "宝物:", 16, UIBuilder.Gold, TextAnchor.MiddleLeft);
            UIBuilder.SetAnchored(trTitle.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-400, 0), new Vector2(60, 28));

            emptyTreasureText = UIBuilder.CreateText(treasureInner, "暂无宝物", 15, UIBuilder.TextDim, TextAnchor.MiddleLeft);
            UIBuilder.SetAnchored(emptyTreasureText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-330, 0), new Vector2(200, 28));
            emptyTreasureText.gameObject.SetActive(false);

            for (int i = 0; i < 6; i++)
            {
                treasureTexts[i] = UIBuilder.CreateText(treasureInner, "", 15, UIBuilder.Gold, TextAnchor.MiddleLeft);
                UIBuilder.SetAnchored(treasureTexts[i].gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-330 + i * 300, 0), new Vector2(290, 28));
                treasureTexts[i].gameObject.SetActive(false);
            }

            // ── 返回按钮 ──
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

            // 标题
            titleText.text = $"{player.name}  Lv.{player.level}  {profCfg.name}";

            // 10 行属性
            string[] names = { "力量", "敏捷", "智力", "体力", "生命", "法力", "攻击", "防御", "暴击率", "闪避率" };
            string[] values = {
                player.strength.ToString(), player.agility.ToString(), player.intelligence.ToString(), player.vitality.ToString(),
                $"{player.hp}/{player.maxHp}", $"{player.mp}/{player.maxMp}",
                player.attack.ToString(), player.defense.ToString(),
                $"{player.critRate}%", $"{player.dodgeRate}%"
            };
            for (int i = 0; i < 10; i++)
                attrTexts[i].text = $"{names[i]}:  {values[i]}";

            // 装备（3 槽位）
            string[] slots = { "武器", "防具", "饰品" };
            int[] ids = { player.weaponId, player.armorId, player.accessoryId };
            for (int i = 0; i < 3; i++)
            {
                var item = ConfigManager.Instance.GetItem(ids[i]);
                equipTexts[i].text = $"{slots[i]}: {item != null ? item.name : "空"}";
                equipTexts[i].color = item != null ? UIBuilder.TextWhite : UIBuilder.TextDim;
            }

            // 技能
            int sIdx = 0;
            foreach (var skillId in player.equippedSkills)
            {
                if (sIdx >= 4) break;
                var skill = ConfigManager.Instance.GetSkill(skillId);
                if (skill == null) continue;
                string typeStr = skill.type switch { SkillType.Active => "主", SkillType.Passive => "被", SkillType.Buff => "增", _ => "" };
                skillTexts[sIdx].text = $"[{typeStr}] {skill.name} - {skill.desc}";
                skillTexts[sIdx].gameObject.SetActive(true);
                sIdx++;
            }
            for (int i = sIdx; i < 4; i++)
                skillTexts[i].gameObject.SetActive(false);

            // 经验条
            int expToNext = player.GetExpToNextLevel();
            float expRatio = expToNext > 0 ? (float)player.exp / expToNext : 0f;
            expSlider.value = expRatio;
            expText.text = $"EXP: {player.exp}/{expToNext}";

            // 宝物
            if (player.treasures.Count == 0)
            {
                emptyTreasureText.gameObject.SetActive(true);
                for (int i = 0; i < 6; i++)
                    treasureTexts[i].gameObject.SetActive(false);
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
                for (int i = tIdx; i < 6; i++)
                    treasureTexts[i].gameObject.SetActive(false);
            }
        }
    }
}
