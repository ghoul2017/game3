using UnityEngine;
using Game.UI;

namespace Game.Core
{
    /// <summary>
    /// 游戏启动入口，挂载到场景 GameObject 上
    /// </summary>
    public class GameEntry : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("[GameEntry] 初始化音频系统");
            AudioManager.EnsureCreated();
        }

        private void Start()
        {
            Debug.Log("[GameEntry] 方块与地下城 启动");
            UIManager.EnsureCreated();
            GameHUD.Create();  // 创建 HUD（初始隐藏）
            UIManager.Instance.ShowPanel<MainPanel>();
        }
    }
}
