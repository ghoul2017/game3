using UnityEngine;
using Game.UI;

namespace Game.Core
{
    /// <summary>
    /// 游戏启动入口，挂载到场景 GameObject 上
    /// </summary>
    public class GameEntry : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("[GameEntry] 方块与地下城 启动");
            UIManager.EnsureCreated();
            UIManager.Instance.ShowPanel<MainPanel>();
        }
    }
}
