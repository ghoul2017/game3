using UnityEngine;
using Game.Data;

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
            Debug.Log($"[GameManager] 状态切换 -> {state}");
        }

        public bool HasSave()
        {
            return DataManager.Instance.HasSave();
        }

        public void NewGame()
        {
            Player = null;
            Dungeon = null;
            SetState(GameState.CreatingRole);
        }

        public void ContinueGame()
        {
            Player = DataManager.Instance.Load();
            if (Player != null)
            {
                SetState(GameState.InTown);
            }
        }

        public void SaveGame()
        {
            if (Player != null)
            {
                DataManager.Instance.Save(Player);
                Debug.Log("[GameManager] 存档成功");
            }
        }
    }
}
