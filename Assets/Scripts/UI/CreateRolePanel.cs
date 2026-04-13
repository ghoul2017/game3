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
            nameText = FindComponent<Text>("DetailPanel/Inner/ProfName");
            descText = FindComponent<Text>("DetailPanel/Inner/ProfDesc");

            for (int i = 0; i < 4; i++)
                attrTexts[i] = FindComponent<Text>($"DetailPanel/Inner/Attr_{i}");

            for (int i = 0; i < 6; i++)
                battleTexts[i] = FindComponent<Text>($"DetailPanel/Inner/Battle_{i}");

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
