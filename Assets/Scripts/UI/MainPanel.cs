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

            // 顶部装饰线
            var topLine = new GameObject("TopDecor");
            topLine.transform.SetParent(parent, false);
            topLine.AddComponent<RectTransform>();
            var tImg = topLine.AddComponent<Image>();
            tImg.color = new Color(UIBuilder.Gold.r, UIBuilder.Gold.g, UIBuilder.Gold.b, 0.15f);
            var tRt = topLine.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.15f, 1);
            tRt.anchorMax = new Vector2(0.85f, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -90);
            tRt.sizeDelta = new Vector2(0, 1);

            // 标题（大号带阴影）
            var title = UIBuilder.CreateShadowText(parent, "方块与地下城", 50, UIBuilder.Gold,
                new Color(0, 0, 0, 0.4f), TextAnchor.MiddleCenter, true, "Title");
            UIBuilder.SetAnchored(title.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 190), new Vector2(650, 70));

            // 副标题
            var subtitle = UIBuilder.CreateText(parent, "冒 险 ， 从 城 镇 开 始 ...", 19, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "Subtitle");
            UIBuilder.SetAnchored(subtitle.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 130), new Vector2(480, 32));

            // 底部装饰线
            var botLine = new GameObject("BotDecor");
            botLine.transform.SetParent(parent, false);
            botLine.AddComponent<RectTransform>();
            var bImg = botLine.AddComponent<Image>();
            bImg.color = new Color(UIBuilder.Gold.r, UIBuilder.Gold.g, UIBuilder.Gold.b, 0.10f);
            var bRt = botLine.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.25f, 0.5f);
            bRt.anchorMax = new Vector2(0.75f, 0.5f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0, 100);
            bRt.sizeDelta = new Vector2(0, 1);

            // 主按钮区 — 4 个按钮居中排列
            float btnW = 280;
            float btnH = 50;
            float gap = 60;

            var (btnNew, _) = UIBuilder.CreateButtonWithRef(parent, "开 始 冒 险", btnW, btnH, null, 22, "BtnNewGame");
            UIBuilder.SetAnchored(btnNew.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 50), new Vector2(btnW, btnH));

            var (btnCont, _) = UIBuilder.CreateButtonWithRef(parent, "继 续 冒 险", btnW, btnH, null, 22, "BtnContinue");
            UIBuilder.SetAnchored(btnCont.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 50 - gap), new Vector2(btnW, btnH));

            var (btnSet, _) = UIBuilder.CreateButtonWithRef(parent, "设       置", btnW, btnH, null, 20, "BtnSettings");
            UIBuilder.SetAnchored(btnSet.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 50 - gap * 2), new Vector2(btnW, btnH));

            var (btnQuit, _) = UIBuilder.CreateButtonWithRef(parent, "退 出 游 戏", btnW, btnH, null, 20, "BtnQuit");
            UIBuilder.SetAnchored(btnQuit.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 50 - gap * 3), new Vector2(btnW, btnH));

            // 底部版本号
            var ver = UIBuilder.CreateText(parent, "v1.2.0", 13, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "Version");
            UIBuilder.SetAnchored(ver.gameObject,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 28), new Vector2(180, 22));
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
                btnContinueBg.color = UIBuilder.ButtonDisable;
            }
            else
            {
                btnContinue.interactable = true;
                btnContinueBg.color = Color.white;
            }
        }
    }
}
