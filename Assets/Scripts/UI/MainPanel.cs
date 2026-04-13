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

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);

            // 标题
            var title = UIBuilder.CreateText(parent, "方块与地下城", 48, UIBuilder.Gold, TextAnchor.MiddleCenter, true, "Title");
            UIBuilder.SetAnchored(title.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 180), new Vector2(600, 80));

            var subtitle = UIBuilder.CreateText(parent, "冒险，从城镇开始...", 22, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "Subtitle");
            UIBuilder.SetAnchored(subtitle.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 120), new Vector2(400, 40));

            // 按钮组 — 4 个按钮等距排列
            var (btnNew, _) = UIBuilder.CreateButtonWithRef(parent, "开始冒险", 220, 50, null, 24, "BtnNewGame");
            UIBuilder.SetAnchored(btnNew.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 60), new Vector2(220, 50));

            var (btnCont, _) = UIBuilder.CreateButtonWithRef(parent, "继续冒险", 220, 50, null, 24, "BtnContinue");
            UIBuilder.SetAnchored(btnCont.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 0), new Vector2(220, 50));

            var (btnSet, _) = UIBuilder.CreateButtonWithRef(parent, "设    置", 220, 50, null, 24, "BtnSettings");
            UIBuilder.SetAnchored(btnSet.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -60), new Vector2(220, 50));

            var (btnQuit, _) = UIBuilder.CreateButtonWithRef(parent, "退出游戏", 220, 50, null, 24, "BtnQuit");
            UIBuilder.SetAnchored(btnQuit.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -120), new Vector2(220, 50));
        }

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
