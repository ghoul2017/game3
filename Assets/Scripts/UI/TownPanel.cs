using Game.Core;
using Game.Data;
using Game.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class TownPanel : UIPanel
    {
        // ---- 缓存引用 ----
        private Text infoText;

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);

            // 标题栏
            UIBuilder.CreateTitleBar(parent, "城       镇");

            // 角色信息栏（顶部）
            var bar = UIBuilder.CreatePanel(parent, 0, 50, "InfoBar");
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1);
            barRt.anchoredPosition = new Vector2(0, -70);
            barRt.sizeDelta = new Vector2(0, 50);
            bar.GetComponent<Image>().color = UIBuilder.InfoBarBg;

            var infoTextObj = UIBuilder.CreateText(bar.transform, "", 17, UIBuilder.Gold, TextAnchor.MiddleCenter, name: "InfoText");
            UIBuilder.SetStretch(infoTextObj.gameObject);

            // 功能按钮区 — 优化间距，6个按钮居中分布
            float centerX = 0;
            float startY = 150;
            float gap = 65;
            float btnW = 300;
            float btnH = 52;

            var (btnDungeon, _) = UIBuilder.CreateButtonWithRef(parent, "进 入 地 牢", btnW, btnH, null, 24, "BtnDungeon");
            UIBuilder.SetAnchored(btnDungeon.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(centerX, startY), new Vector2(btnW, btnH));

            var (btnRest, _) = UIBuilder.CreateButtonWithRef(parent, "休 息 恢 复", btnW, btnH, null, 22, "BtnRest");
            UIBuilder.SetAnchored(btnRest.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(centerX, startY - gap), new Vector2(btnW, btnH));

            var (btnShop, _) = UIBuilder.CreateButtonWithRef(parent, "商       店", btnW, btnH, null, 22, "BtnShop");
            UIBuilder.SetAnchored(btnShop.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(centerX, startY - gap * 2), new Vector2(btnW, btnH));

            var (btnSkill, _) = UIBuilder.CreateButtonWithRef(parent, "技 能 学 习", btnW, btnH, null, 22, "BtnSkill");
            UIBuilder.SetAnchored(btnSkill.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(centerX, startY - gap * 3), new Vector2(btnW, btnH));

            var (btnRole, _) = UIBuilder.CreateButtonWithRef(parent, "角 色 信 息", btnW, btnH, null, 22, "BtnRole");
            UIBuilder.SetAnchored(btnRole.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(centerX, startY - gap * 4), new Vector2(btnW, btnH));

            var (btnSave, _) = UIBuilder.CreateButtonWithRef(parent, "存 档 退 出", btnW, btnH, null, 22, "BtnSave");
            UIBuilder.SetAnchored(btnSave.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(centerX, startY - gap * 5), new Vector2(btnW, btnH));
        }

        protected override void BindReferences()
        {
            infoText = FindComponent<Text>("InfoBar/InfoText");
        }

        protected override void BindEvents()
        {
            FindComponent<Button>("BtnDungeon").onClick.AddListener(() =>
            {
                Debug.Log("[操作] 进入地牢");
                var dungeon = new DungeonData();
                dungeon.GenerateFloor(GameManager.Instance.Player.dungeonFloor > 0 ?
                    GameManager.Instance.Player.dungeonFloor : 1);
                GameManager.Instance.Dungeon = dungeon;
                GameManager.Instance.SetState(GameState.InDungeon);
                UIManager.Instance.ReplacePanel<DungeonPanel>();
            });

            FindComponent<Button>("BtnRest").onClick.AddListener(() =>
            {
                Debug.Log("[操作] 休息恢复");
                GameManager.Instance.Player.FullRestore();
                GameManager.Instance.SaveGame();
                RefreshUI();
            });

            FindComponent<Button>("BtnShop").onClick.AddListener(() =>
            {
                Debug.Log("[操作] 打开商店");
                UIManager.Instance.ShowPanel<ShopPanel>();
            });

            FindComponent<Button>("BtnSkill").onClick.AddListener(() =>
            {
                Debug.Log("[操作] 打开技能学习");
                UIManager.Instance.ShowPanel<SkillLearnPanel>();
            });

            FindComponent<Button>("BtnRole").onClick.AddListener(() =>
            {
                Debug.Log("[操作] 查看角色信息");
                UIManager.Instance.ShowPanel<RoleInfoPanel>();
            });

            FindComponent<Button>("BtnSave").onClick.AddListener(() =>
            {
                Debug.Log("[操作] 存档退出");
                GameManager.Instance.SaveGame();
                GameManager.Instance.SetState(GameState.MainMenu);
                UIManager.Instance.ReplacePanel<MainPanel>();
            });
        }

        public override void OnShow()
        {
            // 城镇背景音乐
            AudioManager.Instance?.CrossfadeMusic(AudioClipType.BGM_Peaceful, 1.0f);
            RefreshUI();
        }

        private void RefreshUI()
        {
            var player = GameManager.Instance.Player;
            if (player == null) return;

            var profCfg = ConfigManager.Instance.GetProfession(player.profession);
            infoText.text = $"{player.name}  Lv.{player.level} {profCfg.name}  HP:{player.hp}/{player.maxHp}  MP:{player.mp}/{player.maxMp}  金币:{player.gold}  地牢层:{player.dungeonFloor}";
        }
    }
}
