using Game.Core;
using Game.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class MainPanel : UIPanel
    {
        // ---- 缓存引用 ----
        private Text titleText;
        private Text subtitleText;
        private Button btnNewGame;
        private Button btnContinue;
        private Image btnContinueBg;
        private Text btnContinueLabel;

        protected override void BindReferences()
        {
            titleText = FindComponent<Text>("Title");
            subtitleText = FindComponent<Text>("Subtitle");
            btnNewGame = FindComponent<Button>("BtnNewGame");
            btnContinue = FindComponent<Button>("BtnContinue");
            btnContinueBg = FindComponent<Image>("BtnContinue");
            btnContinueLabel = FindComponent<Text>("BtnContinue/Label");
        }

        protected override void BindEvents()
        {
            btnNewGame.onClick.AddListener(() =>
            {
                Debug.Log("[操作] 点击 开始冒险");
                ConfigManager.Instance.Init();
                GameManager.Instance.NewGame();
                UIManager.Instance.ReplacePanel<CreateRolePanel>();
            });

            btnContinue.onClick.AddListener(() =>
            {
                Debug.Log("[操作] 点击 继续冒险");
                ConfigManager.Instance.Init();
                GameManager.Instance.ContinueGame();
                if (GameManager.Instance.Player != null)
                    UIManager.Instance.ReplacePanel<TownPanel>();
                else
                    Debug.LogWarning("[操作] 继续冒险失败：无存档");
            });

            var btnSettings = FindComponent<Button>("BtnSettings");
            btnSettings.onClick.AddListener(() =>
            {
                Debug.Log("[操作] 点击 设置");
                UIManager.Instance.ShowPanel<SettingsPanel>();
            });

            var btnQuit = FindComponent<Button>("BtnQuit");
            btnQuit.onClick.AddListener(() =>
            {
                Debug.Log("[操作] 点击 退出游戏");
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            });
        }

        public override void OnShow()
        {
            // 每次显示时检查存档状态
            if (!GameManager.Instance.HasSave())
            {
                btnContinue.interactable = false;
                btnContinueBg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            }
            else
            {
                btnContinue.interactable = true;
                btnContinueBg.color = Color.white;
            }
        }
    }
}
