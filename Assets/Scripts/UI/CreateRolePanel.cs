using System.Collections.Generic;
using Game.Core;
using Game.Config;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class CreateRolePanel : UIPanel
    {
        // ---- 缓存引用 ----
        private Profession selectedProfession = Profession.Warrior;
        private readonly Dictionary<Profession, Button> professionButtons = new Dictionary<Profession, Button>();
        private readonly Dictionary<Profession, Text> professionBtnLabels = new Dictionary<Profession, Text>();

        // 详情区域
        private Text nameText;
        private Text descText;

        // 4 个基础属性行
        private readonly Text[] attrTexts = new Text[4];

        // 6 个战斗属性行（两列，每列 3 行）
        private readonly Text[] battleTexts = new Text[6];

        // 角色名输入
        private InputField nameInput;

        // 提示文本
        private Text hintText;

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);

            // ---- 标题 ----
            var title = UIBuilder.CreateText(transform, "选择职业", 36, UIBuilder.Gold, TextAnchor.MiddleCenter, true, "Title");
            UIBuilder.SetAnchored(title.gameObject,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -60), new Vector2(300, 50));

            // ---- 职业按钮（5 个）----
            var professions = new[] { Profession.Warrior, Profession.Ranger, Profession.Knight, Profession.Priest, Profession.Mage };
            float startX = -480;
            for (int i = 0; i < professions.Length; i++)
            {
                var prof = professions[i];
                var cfg = ConfigManager.Instance.GetProfession(prof);
                var (btn, label) = UIBuilder.CreateButtonWithRef(transform, cfg.name, 160, 45,
                    () => SelectProfession(prof), 22, $"Btn_{prof}");
                btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(startX + i * 200, 260);
                professionButtons[prof] = btn;
                professionBtnLabels[prof] = label;
            }

            // ---- 职业详情面板（边框）----
            var detailPanel = UIBuilder.CreateBorderedPanel(transform, 500, 300, "DetailPanel");
            var detailRt = detailPanel.GetComponent<RectTransform>();
            detailRt.anchoredPosition = new Vector2(0, 20);

            // 职业名
            nameText = UIBuilder.CreateText(detailPanel.transform, "", 28, UIBuilder.Gold, TextAnchor.MiddleCenter, true, "ProfName");
            UIBuilder.SetAnchored(nameText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -30), new Vector2(300, 40));

            // 描述
            descText = UIBuilder.CreateText(detailPanel.transform, "", 18, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "ProfDesc");
            UIBuilder.SetAnchored(descText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -65), new Vector2(450, 30));

            // 基础属性（力量、敏捷、智力、体力）
            for (int i = 0; i < 4; i++)
            {
                attrTexts[i] = UIBuilder.CreateText(detailPanel.transform, "", 18, UIBuilder.TextWhite, TextAnchor.MiddleLeft, false, $"Attr_{i}");
                UIBuilder.SetAnchored(attrTexts[i].gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0, -110 - i * 30), new Vector2(360, 28));
            }

            // 战斗属性（两列：生命/法力/攻击  防御/暴击/闪避）
            for (int i = 0; i < 6; i++)
            {
                int col = i / 3;
                int row = i % 3;
                battleTexts[i] = UIBuilder.CreateText(detailPanel.transform, "", 16, UIBuilder.TextDim, TextAnchor.MiddleLeft, false, $"Battle_{i}");
                UIBuilder.SetAnchored(battleTexts[i].gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(-100 + col * 250, -250 - row * 26), new Vector2(200, 24));
            }

            // ---- 角色名输入区 ----
            var nameLabel = UIBuilder.CreateText(transform, "角色名:", 20, UIBuilder.TextWhite, TextAnchor.MiddleRight, false, "NameLabel");
            UIBuilder.SetAnchored(nameLabel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-80, -145), new Vector2(100, 35));

            var inputObj = new GameObject("NameInput");
            inputObj.transform.SetParent(transform, false);
            var inputRt = inputObj.AddComponent<RectTransform>();
            inputRt.sizeDelta = new Vector2(200, 35);
            inputRt.anchorMin = new Vector2(0.5f, 0.5f);
            inputRt.anchorMax = new Vector2(0.5f, 0.5f);
            inputRt.pivot = new Vector2(0.5f, 0.5f);
            inputRt.anchoredPosition = new Vector2(50, -145);

            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            nameInput = inputObj.AddComponent<InputField>();
            var inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(inputObj.transform, false);
            var inputTextRt = inputTextObj.AddComponent<RectTransform>();
            inputTextRt.anchorMin = Vector2.zero;
            inputTextRt.anchorMax = Vector2.one;
            inputTextRt.offsetMin = new Vector2(8, 2);
            inputTextRt.offsetMax = new Vector2(-8, -2);
            var inputText = inputTextObj.AddComponent<Text>();
            inputText.text = "冒险者";
            inputText.fontSize = 20;
            inputText.color = UIBuilder.TextWhite;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);

            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholderRt = placeholderObj.AddComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = new Vector2(8, 2);
            placeholderRt.offsetMax = new Vector2(-8, -2);
            var placeholder = placeholderObj.AddComponent<Text>();
            placeholder.text = "输入角色名";
            placeholder.fontSize = 20;
            placeholder.color = UIBuilder.TextDim;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.font = Font.CreateDynamicFontFromOSFont("Arial", 20);

            nameInput.textComponent = inputText;
            nameInput.placeholder = placeholder;
            nameInput.characterLimit = 8;

            // ---- 提示文本 ----
            hintText = UIBuilder.CreateText(transform, "", 16, UIBuilder.Red, TextAnchor.MiddleCenter, false, "HintText");
            UIBuilder.SetAnchored(hintText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -180), new Vector2(400, 28));

            // ---- 底部按钮 ----
            UIBuilder.CreateButton(transform, "开始游戏", 200, 50, OnCreateRole, 24, "BtnStart")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -220);

            UIBuilder.CreateButton(transform, "返回", 200, 50, () => UIManager.Instance.Back(), 24, "BtnBack")
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -290);

            // 初始选中战士
            SelectProfession(Profession.Warrior);
        }

        public override void OnShow()
        {
            // 每次显示时刷新
            RefreshUI();
        }

        private void SelectProfession(Profession prof)
        {
            selectedProfession = prof;
            RefreshUI();
        }

        /// <summary>
        /// 零销毁刷新：只修改 text/color 属性
        /// </summary>
        private void RefreshUI()
        {
            // 更新按钮高亮
            foreach (var kv in professionButtons)
            {
                var img = kv.Value.GetComponent<Image>();
                img.color = kv.Key == selectedProfession ? UIBuilder.ButtonHover : UIBuilder.ButtonNormal;
            }

            var cfg = ConfigManager.Instance.GetProfession(selectedProfession);
            if (cfg == null) return;

            // 职业名和描述
            nameText.text = cfg.name;
            descText.text = cfg.desc;

            // 基础属性
            string[] attrNames = { "力量", "敏捷", "智力", "体力" };
            int[] attrBases = { cfg.strBase, cfg.agiBase, cfg.intBase, cfg.vitBase };
            int[] attrGrows = { cfg.strGrow, cfg.agiGrow, cfg.intGrow, cfg.vitGrow };
            for (int i = 0; i < 4; i++)
            {
                attrTexts[i].text = $"{attrNames[i]}: {attrBases[i]} (+{attrGrows[i]}/级)";
            }

            // 战斗属性
            string[] battleNames = { "生命", "法力", "攻击", "防御", "暴击", "闪避" };
            int[] battleValues = { cfg.hpBase, cfg.mpBase, cfg.atkBase, cfg.defBase, cfg.critBase, cfg.dodgeBase };
            for (int i = 0; i < 6; i++)
            {
                battleTexts[i].text = $"{battleNames[i]}: {battleValues[i]}";
            }

            // 清除提示
            hintText.text = "";
        }

        private void OnCreateRole()
        {
            // 验证角色名
            string roleName = string.IsNullOrEmpty(nameInput.text) ? "冒险者" : nameInput.text.Trim();
            if (string.IsNullOrEmpty(roleName))
                roleName = "冒险者";

            Debug.Log($"[操作] 创建角色 职业={selectedProfession} 名字={roleName}");

            var player = new PlayerData
            {
                name = roleName,
                profession = selectedProfession,
                gold = 100
            };

            var profCfg = ConfigManager.Instance.GetProfession(selectedProfession);
            player.strength = profCfg.strBase;
            player.agility = profCfg.agiBase;
            player.intelligence = profCfg.intBase;
            player.vitality = profCfg.vitBase;

            // 初始技能
            foreach (var skillId in profCfg.defaultSkillIds)
            {
                player.learnedSkills.Add(skillId);
                player.equippedSkills.Add(skillId);
            }

            player.RecalcStats();
            player.FullRestore();

            GameManager.Instance.Player = player;
            GameManager.Instance.SetState(GameState.InTown);
            GameManager.Instance.SaveGame();
            UIManager.Instance.ReplacePanel<TownPanel>();
        }
    }
}
