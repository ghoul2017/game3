using UnityEngine;
using Game.Data;
using Game.UI;

namespace Game.Core
{
    public enum GameState
    {
        None,
        MainMenu,
        CreatingRole,
        InTown,
        InDungeon,
        InBattle
    }

    /// <summary>
    /// 游戏全局状态管理器
    /// </summary>
    public class GameManager
    {
        private static GameManager instance;
        public static GameManager Instance => instance ??= new GameManager();

        public GameState State { get; private set; } = GameState.None;
        public PlayerData Player { get; set; }
        public DungeonData Dungeon { get; set; }

        public void SetState(GameState state)
        {
            State = state;
            Logger.LogI("Game", $"状态切换 -> {state}");
            GameHUD.UpdateVisibility(state);
        }

        public bool HasSave()
        {
            return DataManager.Instance.HasSave();
        }

        public void NewGame()
        {
            Player = null;
            Dungeon = null;
            // 状态由调用方（MainPanel）管理
        }

        public void ContinueGame()
        {
            Player = DataManager.Instance.Load();
            // 不在此处SetState，由调用方（MainPanel）负责
        }

        public void SaveGame()
        {
            if (Player != null)
            {
                DataManager.Instance.Save(Player);
                Logger.LogI("GameManager", "存档成功");
            }
        }
    }
}
