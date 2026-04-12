using Game.Core;
using Game.Data;
using Game.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class TownPanel : UIPanel
    {
        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);

            // 标题栏
            var titleBar = UIBuilder.CreateTitleBar(transform, "城  镇");

            // 角色信息栏（顶部）
            BuildPlayerInfoBar();

            // 功能按钮区
            float centerX = 0;
            float startY = 100;
            float gap = 70;

            UIBuilder.CreateButton(transform, "进入地牢", 260, 55, () =>
            {
                var dungeon = new DungeonData();
                dungeon.GenerateFloor(GameManager.Instance.Player.dungeonFloor > 0 ?
                    GameManager.Instance.Player.dungeonFloor : 1);
                GameManager.Instance.Dungeon = dungeon;
                GameManager.Instance.SetState(GameState.InDungeon);
                UIManager.Instance.ReplacePanel<DungeonPanel>();
            }, 26, "BtnDungeon").GetComponent<RectTransform>().anchoredPosition = new Vector2(centerX, startY);

            UIBuilder.CreateButton(transform, "休息恢复", 260, 55, () =>
            {
                GameManager.Instance.Player.FullRestore();
                GameManager.Instance.SaveGame();
                RefreshInfo();
            }, 24, "BtnRest").GetComponent<RectTransform>().anchoredPosition = new Vector2(centerX, startY - gap);

            UIBuilder.CreateButton(transform, "商店", 260, 55, () =>
            {
                UIManager.Instance.ShowPanel<ShopPanel>();
            }, 24, "BtnShop").GetComponent<RectTransform>().anchoredPosition = new Vector2(centerX, startY - gap * 2);

            UIBuilder.CreateButton(transform, "技能学习", 260, 55, () =>
            {
                UIManager.Instance.ShowPanel<SkillLearnPanel>();
            }, 24, "BtnSkill").GetComponent<RectTransform>().anchoredPosition = new Vector2(centerX, startY - gap * 3);

            UIBuilder.CreateButton(transform, "角色信息", 260, 55, () =>
            {
                UIManager.Instance.ShowPanel<RoleInfoPanel>();
            }, 24, "BtnRole").GetComponent<RectTransform>().anchoredPosition = new Vector2(centerX, startY - gap * 4);

            UIBuilder.CreateButton(transform, "存档退出", 260, 55, () =>
            {
                GameManager.Instance.SaveGame();
                GameManager.Instance.SetState(GameState.MainMenu);
                UIManager.Instance.ReplacePanel<MainPanel>();
            }, 24, "BtnSave").GetComponent<RectTransform>().anchoredPosition = new Vector2(centerX, startY - gap * 5);
        }

        public override void OnShow()
        {
            RefreshInfo();
        }

        private void BuildPlayerInfoBar()
        {
            var bar = UIBuilder.CreatePanel(transform, 0, 50, "InfoBar");
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1);
            barRt.anchoredPosition = new Vector2(0, -65);
            barRt.sizeDelta = new Vector2(0, 50);
            bar.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.9f);
        }

        private void RefreshInfo()
        {
            var player = GameManager.Instance.Player;
            if (player == null) return;

            var bar = transform.Find("InfoBar");
            if (bar == null) return;

            // 清除旧内容
            for (int i = bar.childCount - 1; i >= 0; i--)
                Object.Destroy(bar.GetChild(i).gameObject);

            var profCfg = ConfigManager.Instance.GetProfession(player.profession);
            string infoText = $"{player.name}  Lv.{player.level} {profCfg.name}  HP:{player.hp}/{player.maxHp}  MP:{player.mp}/{player.maxMp}  金币:{player.gold}  地牢层:{player.dungeonFloor}";
            var text = UIBuilder.CreateText(bar, infoText, 18, UIBuilder.Gold, TextAnchor.MiddleCenter);
            UIBuilder.SetStretch(text.gameObject);
        }
    }
}
