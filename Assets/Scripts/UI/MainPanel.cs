using Game.Core;
using Game.Config;
using UnityEngine;

namespace Game.UI
{
    public class MainPanel : UIPanel
    {
        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);

            // 标题
            var title = UIBuilder.CreateText(transform, "方块与地下城", 48, UIBuilder.Gold, TextAnchor.MiddleCenter, true, "Title");
            UIBuilder.SetAnchored(title.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 180), new Vector2(600, 80));

            var subtitle = UIBuilder.CreateText(transform, "冒险，从城镇开始...", 22, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "Subtitle");
            UIBuilder.SetAnchored(subtitle.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 120), new Vector2(400, 40));

            // 按钮
            UIBuilder.CreateButton(transform, "开始冒险", 220, 50, () =>
            {
                ConfigManager.Instance.Init();
                GameManager.Instance.NewGame();
                UIManager.Instance.ReplacePanel<CreateRolePanel>();
            }, 24, "BtnNewGame").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);

            var btnContinue = UIBuilder.CreateButton(transform, "继续冒险", 220, 50, () =>
            {
                ConfigManager.Instance.Init();
                GameManager.Instance.ContinueGame();
                if (GameManager.Instance.Player != null)
                    UIManager.Instance.ReplacePanel<TownPanel>();
            }, 24, "BtnContinue");

            var btnContinueRt = btnContinue.GetComponent<RectTransform>();
            btnContinueRt.anchoredPosition = new Vector2(0, -40);

            if (!GameManager.Instance.HasSave())
            {
                btnContinue.interactable = false;
                btnContinue.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            }
        }
    }
}
