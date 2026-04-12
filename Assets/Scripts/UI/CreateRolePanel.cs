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
        private Profession selectedProfession = Profession.Warrior;
        private readonly Dictionary<Profession, GameObject> professionButtons = new Dictionary<Profession, GameObject>();

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);

            // 标题
            var title = UIBuilder.CreateText(transform, "选择职业", 36, UIBuilder.Gold, TextAnchor.MiddleCenter, true);
            UIBuilder.SetAnchored(title.gameObject,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -60), new Vector2(300, 50));

            // 职业列表
            var professions = new[] { Profession.Warrior, Profession.Ranger, Profession.Knight, Profession.Priest, Profession.Mage };
            float startX = -480;
            for (int i = 0; i < professions.Length; i++)
            {
                var prof = professions[i];
                var cfg = ConfigManager.Instance.GetProfession(prof);
                var btn = UIBuilder.CreateButton(transform, cfg.name, 160, 45, () => SelectProfession(prof), 22, $"Btn_{prof}");
                var rt = btn.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(startX + i * 200, 260);
                professionButtons[prof] = btn.gameObject;
            }

            // 职业详情区域
            var detailPanel = UIBuilder.CreateBorderedPanel(transform, 500, 300, "DetailPanel");
            var detailRt = detailPanel.GetComponent<RectTransform>();
            detailRt.anchoredPosition = new Vector2(0, 20);
            detailPanel.name = "DetailPanel";

            // 按钮区
            var btnStart = UIBuilder.CreateButton(transform, "开始游戏", 200, 50, OnCreateRole, 24, "BtnStart");
            btnStart.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);

            var btnBack = UIBuilder.CreateButton(transform, "返回", 200, 50, () => UIManager.Instance.Back(), 24, "BtnBack");
            btnBack.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -270);

            SelectProfession(Profession.Warrior);
        }

        private void SelectProfession(Profession prof)
        {
            selectedProfession = prof;
            Debug.Log($"[操作] 选择职业 -> {prof}");
            // 更新按钮高亮
            foreach (var kv in professionButtons)
            {
                var img = kv.Value.GetComponent<Image>();
                img.color = kv.Key == prof ? UIBuilder.ButtonHover : UIBuilder.ButtonNormal;
            }
            UpdateDetail();
        }

        private void UpdateDetail()
        {
            var cfg = ConfigManager.Instance.GetProfession(selectedProfession);
            var detail = transform.Find("DetailPanel");
            if (detail == null) return;

            // 清除旧内容
            for (int i = detail.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(detail.GetChild(i).gameObject);

            // 职业名
            var nameText = UIBuilder.CreateText(detail, cfg.name, 28, UIBuilder.Gold, TextAnchor.MiddleCenter, true);
            UIBuilder.SetAnchored(nameText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(300, 40));

            // 描述
            var descText = UIBuilder.CreateText(detail, cfg.desc, 18, UIBuilder.TextDim, TextAnchor.MiddleCenter);
            UIBuilder.SetAnchored(descText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -65), new Vector2(450, 30));

            // 属性
            string[] attrNames = { "力量", "敏捷", "智力", "体力" };
            int[] attrBases = { cfg.strBase, cfg.agiBase, cfg.intBase, cfg.vitBase };
            int[] attrGrows = { cfg.strGrow, cfg.agiGrow, cfg.intGrow, cfg.vitGrow };
            for (int i = 0; i < 4; i++)
            {
                var attrText = UIBuilder.CreateText(detail, $"{attrNames[i]}: {attrBases[i]} (+{attrGrows[i]}/级)", 18, UIBuilder.TextWhite, TextAnchor.MiddleLeft);
                UIBuilder.SetAnchored(attrText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110 - i * 30), new Vector2(360, 28));
            }

            // 战斗属性
            string[] battleNames = { "生命", "法力", "攻击", "防御", "暴击", "闪避" };
            int[] battleValues = { cfg.hpBase, cfg.mpBase, cfg.atkBase, cfg.defBase, cfg.critBase, cfg.dodgeBase };
            for (int i = 0; i < battleNames.Length; i++)
            {
                int col = i / 3;
                int row = i % 3;
                var bText = UIBuilder.CreateText(detail, $"{battleNames[i]}: {battleValues[i]}", 16, UIBuilder.TextDim, TextAnchor.MiddleLeft);
                UIBuilder.SetAnchored(bText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-100 + col * 250, -250 - row * 26), new Vector2(200, 24));
            }
        }

        private void OnCreateRole()
        {
            Debug.Log($"[操作] 创建角色 职业={selectedProfession}");
            var player = new PlayerData
            {
                name = $"冒险者",
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
