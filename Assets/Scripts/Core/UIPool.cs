using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UI 对象池 — 预创建行，SetActive 显隐，原地更新文字
    /// </summary>
    public static class UIPool
    {
        /// <summary>
        /// 对象池行结构体
        /// </summary>
        public struct PoolRow
        {
            public GameObject obj;
            public RectTransform rt;
            public Image bg;
            public Text nameText;
            public Button actionBtn;
            public Text actionBtnLabel;
            public Button secondBtn;
            public Text secondBtnLabel;
        }

        /// <summary>
        /// 创建对象池行列表，预创建 count 行，每行含 nameText + actionBtn + secondBtn
        /// </summary>
        public static List<PoolRow> CreatePoolRows(Transform parent, int count, float rowHeight, float startY, float rowWidth)
        {
            var rows = new List<PoolRow>();
            for (int i = 0; i < count; i++)
            {
                var row = CreatePoolRow(parent, rowHeight, startY - i * (rowHeight + 4), rowWidth);
                row.obj.SetActive(false);
                rows.Add(row);
            }
            return rows;
        }

        private static PoolRow CreatePoolRow(Transform parent, float height, float y, float width)
        {
            var obj = UIFactory.CreateUI(parent, "PoolRow");
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0, y);

            var bg = obj.AddComponent<Image>();
            bg.color = UIColors.RowEven;

            // 左侧装饰条
            var leftBar = UIFactory.CreateUI(obj.transform, "LeftBar");
            var lbImg = leftBar.AddComponent<Image>();
            lbImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.15f);
            var lbRt = leftBar.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0, 0.15f);
            lbRt.anchorMax = new Vector2(0, 0.85f);
            lbRt.pivot = new Vector2(0, 0.5f);
            lbRt.anchoredPosition = new Vector2(2, 0);
            lbRt.sizeDelta = new Vector2(2, 0);

            var nameText = UIFactory.CreateText(obj.transform, "", 16, UIColors.TextWhite, TextAnchor.MiddleLeft, name: "NameText");
            UIBuilder.SetAnchored(nameText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-140, 0), new Vector2(width - 280, height - 6));

            var (actionBtn, actionBtnLabel) = UIFactory.CreateButtonWithRef(obj.transform, "", 90, (int)(height - 8),
                null, 14, "ActionBtn");
            actionBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(width / 2 - 100, 0);

            var (secondBtn, secondBtnLabel) = UIFactory.CreateButtonWithRef(obj.transform, "", 90, (int)(height - 8),
                null, 14, "SecondBtn");
            secondBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(width / 2 - 10, 0);

            return new PoolRow
            {
                obj = obj, rt = rt, bg = bg,
                nameText = nameText,
                actionBtn = actionBtn, actionBtnLabel = actionBtnLabel,
                secondBtn = secondBtn, secondBtnLabel = secondBtnLabel
            };
        }
    }
}
