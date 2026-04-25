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

        private Text nameText;
        private Text descText;
        private readonly Text[] attrTexts = new Text[4];
        private readonly Text[] battleTexts = new Text[6];
        private InputField nameInput;
        private Text hintText;

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);

            // ---- 标题 ----
            var title = UIBuilder.CreateText(parent, "选择职业", 32, UIBuilder.Gold, TextAnchor.MiddleCenter, true, "Title");
            UIBuilder.SetAnchored(title.gameObject,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -50), new Vector2(280, 45));

            // ---- 职业按钮（5 个）----
            var professions = new[] { "Warrior", "Ranger", "Knight", "Priest", "Mage" };
            float totalW = 5 * 160 + 4 * 20; // 5个按钮 + 4个间距
            float startX = -totalW / 2 + 80;
            for (int i = 0; i < professions.Length; i++)
            {
                var (btn, _) = UIBuilder.CreateButtonWithRef(parent, "", 160, 48, null, 20, $"Btn_{professions[i]}");
                UIBuilder.SetAnchored(btn.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + i * 180, 220), new Vector2(160, 48));
            }

            // ---- 职业详情面板（边框）----
            var detailPanel = UIBuilder.CreateBorderedPanel(parent, 600, 340, "DetailPanel");
            var dpRt = detailPanel.GetComponent<RectTransform>();
            dpRt.anchorMin = new Vector2(0, 1);
            dpRt.anchorMax = new Vector2(1, 1);
            dpRt.pivot = new Vector2(0.5f, 1);
            dpRt.anchoredPosition = new Vector2(0, -110);
            dpRt.sizeDelta = new Vector2(0, 340);

            var profName = UIBuilder.CreateText(detailPanel.transform, "", 26, UIBuilder.Gold, TextAnchor.MiddleCenter, true, "ProfName");
            UIBuilder.SetAnchored(profName.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -28), new Vector2(400, 36));

            var profDesc = UIBuilder.CreateText(detailPanel.transform, "", 17, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "ProfDesc");
            UIBuilder.SetAnchored(profDesc.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -65), new Vector2(550, 28));

            // 基础属性（力量、敏捷、智力、体力）
            for (int i = 0; i < 4; i++)
            {
                var attr = UIBuilder.CreateText(detailPanel.transform, "", 17, UIBuilder.TextWhite, TextAnchor.MiddleLeft, false, $"Attr_{i}");
                UIBuilder.SetAnchored(attr.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0, -105 - i * 28), new Vector2(420, 26));
            }

            // 战斗属性（两列）
            for (int i = 0; i < 6; i++)
            {
                int col = i / 3;
                int row = i % 3;
                var battle = UIBuilder.CreateText(detailPanel.transform, "", 15, UIBuilder.TextDim, TextAnchor.MiddleLeft, false, $"Battle_{i}");
                UIBuilder.SetAnchored(battle.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(-80 + col * 260, -230 - row * 24), new Vector2(220, 22));
            }

            // ---- 角色名输入区 ----
            var nameLabel = UIBuilder.CreateText(parent, "角色名:", 18, UIBuilder.TextWhite, TextAnchor.MiddleRight, false, "NameLabel");
            UIBuilder.SetAnchored(nameLabel.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-90, -270), new Vector2(90, 32));

            var inputObj = new GameObject("NameInput");
            inputObj.transform.SetParent(parent, false);
            var inputRt = inputObj.AddComponent<RectTransform>();
            inputRt.sizeDelta = new Vector2(200, 32);
            inputRt.anchorMin = new Vector2(0.5f, 0.5f);
            inputRt.anchorMax = new Vector2(0.5f, 0.5f);
            inputRt.pivot = new Vector2(0.5f, 0.5f);
            inputRt.anchoredPosition = new Vector2(40, -270);

            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = UIBuilder.SliderBg;
            inputObj.AddComponent<InputField>();

            var inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(inputObj.transform, false);
            var inputTextRt = inputTextObj.AddComponent<RectTransform>();
            inputTextRt.anchorMin = Vector2.zero;
            inputTextRt.anchorMax = Vector2.one;
            inputTextRt.offsetMin = new Vector2(8, 2);
            inputTextRt.offsetMax = new Vector2(-8, -2);
            var inputText = inputTextObj.AddComponent<Text>();
            inputText.text = "冒险者";
            inputText.fontSize = 18;
            inputText.color = UIBuilder.TextWhite;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            var placeholderRt = placeholderObj.AddComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = new Vector2(8, 2);
            placeholderRt.offsetMax = new Vector2(-8, -2);
            var placeholder = placeholderObj.AddComponent<Text>();
            placeholder.text = "输入角色名...";
            placeholder.fontSize = 18;
            placeholder.color = UIBuilder.TextDim;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var inputField = inputObj.GetComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;
            inputField.characterLimit = 8;

            // ---- 提示文本 ----
            var hint = UIBuilder.CreateText(parent, "", 15, UIBuilder.Red, TextAnchor.MiddleCenter, false, "HintText");
            UIBuilder.SetAnchored(hint.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -310), new Vector2(400, 26));

            // ---- 底部按钮（底部锚定，确保始终在屏幕内）----
            var (btnStart, _) = UIBuilder.CreateButtonWithRef(parent, "开始游戏", 220, 50, null, 22, "BtnStart");
            UIBuilder.SetAnchored(btnStart.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 90), new Vector2(220, 50));

            var (btnBack, _) = UIBuilder.CreateButtonWithRef(parent, "返回", 220, 50, null, 22, "BtnBack");
            UIBuilder.SetAnchored(btnBack.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 30), new Vector2(220, 50));
        }

        protected override void BindReferences()
        {
            // 职业按钮
            var professions = new[] { Profession.Warrior, Profession.Ranger, Profession.Knight, Profession.Priest, Profession.Mage };
            foreach (var prof in professions)
            {
                string btnName = $"Btn_{prof}";
                professionButtons[prof] = FindComponent<Button>(btnName);
                professionBtnLabels[prof] = FindComponent<Text>($"{btnName}/Label");
            }

            // 详情面板
            nameText = FindComponent<Text>("DetailPanel/ProfName");
            descText = FindComponent<Text>("DetailPanel/ProfDesc");

            for (int i = 0; i < 4; i++)
                attrTexts[i] = FindComponent<Text>($"DetailPanel/Attr_{i}");

            for (int i = 0; i < 6; i++)
                battleTexts[i] = FindComponent<Text>($"DetailPanel/Battle_{i}");

            // 角色名输入
            nameInput = FindComponent<InputField>("NameInput");

            // 提示文本
            hintText = FindComponent<Text>("HintText");
        }

        protected override void BindEvents()
        {
            var professions = new[] { Profession.Warrior, Profession.Ranger, Profession.Knight, Profession.Priest, Profession.Mage };
            foreach (var prof in professions)
            {
                Profession captured = prof;
                professionButtons[prof].onClick.AddListener(() => SelectProfession(captured));
            }

            FindComponent<Button>("BtnStart").onClick.AddListener(OnCreateRole);
            FindComponent<Button>("BtnBack").onClick.AddListener(() => UIManager.Instance.Back());
        }

        public override void OnShow()
        {
            // 首次显示时设置职业按钮文字（CreateVisuals 无法访问 ConfigManager）
            foreach (var kv in professionBtnLabels)
            {
                var profCfg = ConfigManager.Instance.GetProfession(kv.Key);
                if (profCfg != null)
                    kv.Value.text = profCfg.name;
            }
            SelectProfession(Profession.Warrior);
        }

        private void SelectProfession(Profession prof)
        {
            selectedProfession = prof;
            RefreshUI();
        }

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

            nameText.text = cfg.name;
            descText.text = cfg.desc;

            string[] attrNames = { "力量", "敏捷", "智力", "体力" };
            int[] attrBases = { cfg.strBase, cfg.agiBase, cfg.intBase, cfg.vitBase };
            int[] attrGrows = { cfg.strGrow, cfg.agiGrow, cfg.intGrow, cfg.vitGrow };
            for (int i = 0; i < 4; i++)
                attrTexts[i].text = $"{attrNames[i]}: {attrBases[i]} (+{attrGrows[i]}/级)";

            string[] battleNames = { "生命", "法力", "攻击", "防御", "暴击", "闪避" };
            int[] battleValues = { cfg.hpBase, cfg.mpBase, cfg.atkBase, cfg.defBase, cfg.critBase, cfg.dodgeBase };
            for (int i = 0; i < 6; i++)
                battleTexts[i].text = $"{battleNames[i]}: {battleValues[i]}";

            hintText.text = "";
        }

        private void OnCreateRole()
        {
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

            foreach (var skillId in profCfg.defaultSkillIds)
            {
                player.LearnSkill(skillId);
            }

            player.RecalcStats();
            player.FullRestore();

            GameManager.Instance.Player = player;
            GameManager.Instance.SaveGame();
            // 新流程：清空旧面板栈，GameHUD+TownMapController由GameState联动
            UIManager.Instance.ClearAll();
            GameManager.Instance.SetState(GameState.InTown);
        }
    }
}
