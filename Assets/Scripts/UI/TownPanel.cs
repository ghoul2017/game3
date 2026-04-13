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
