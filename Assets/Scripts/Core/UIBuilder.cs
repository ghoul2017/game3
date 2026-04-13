using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UI 控件静态工厂，纯代码构建 UI
    /// </summary>
    public static class UIBuilder
    {
        // RPG 暗色配色
        public static readonly Color DarkBg = new Color(0.08f, 0.08f, 0.14f, 0.95f);
        public static readonly Color PanelBg = new Color(0.12f, 0.12f, 0.20f, 0.95f);
        public static readonly Color Gold = new Color(0.886f, 0.718f, 0.078f);
        public static readonly Color TextWhite = new Color(0.9f, 0.9f, 0.9f);
        public static readonly Color TextDim = new Color(0.6f, 0.6f, 0.6f);
        public static readonly Color ButtonNormal = new Color(0.15f, 0.12f, 0.25f, 0.9f);
        public static readonly Color ButtonHover = new Color(0.25f, 0.20f, 0.35f, 0.9f);
        public static readonly Color Red = new Color(0.85f, 0.2f, 0.2f);
        public static readonly Color Green = new Color(0.2f, 0.75f, 0.3f);
        public static readonly Color Blue = new Color(0.2f, 0.5f, 0.85f);
        public static readonly Color BorderColor = new Color(0.4f, 0.35f, 0.2f, 0.8f);
        public static readonly Color HpColor = new Color(0.8f, 0.15f, 0.15f);
        public static readonly Color MpColor = new Color(0.15f, 0.4f, 0.8f);
        public static readonly Color ExpColor = new Color(0.15f, 0.7f, 0.3f);

        public static GameObject CreateFullScreenBg(Transform parent, string name = "Bg")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = DarkBg;
            SetStretch(obj);
            return obj;
        }

        public static GameObject CreatePanel(Transform parent, float w, float h, string name = "Panel")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = PanelBg;
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            return obj;
        }

        public static GameObject CreateBorderedPanel(Transform parent, float w, float h, string name = "BorderedPanel")
        {
            var outer = CreateUI(parent, name);
            var outerImg = outer.AddComponent<Image>();
            outerImg.color = BorderColor;
            var outerRt = outer.GetComponent<RectTransform>();
            outerRt.sizeDelta = new Vector2(w, h);

            var inner = CreateUI(outer.transform, "Inner");
            var innerImg = inner.AddComponent<Image>();
            innerImg.color = PanelBg;
            var innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2, 2);
            innerRt.offsetMax = new Vector2(-2, -2);

            return outer;
        }

        public static Text CreateText(Transform parent, string content, int fontSize = 24, Color? color = null,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false, string name = "Text")
        {
            var obj = CreateUI(parent, name);
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color ?? TextWhite;
            text.alignment = alignment;
            text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            if (bold) text.fontStyle = FontStyle.Bold;
            return text;
        }

        public static Button CreateButton(Transform parent, string label, float w, float h,
            System.Action onClick = null, int fontSize = 22, string name = "Btn")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = ButtonNormal;
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            var btn = obj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;

            var childRt = CreateUI(obj.transform, "Label");
            var text = childRt.AddComponent<Text>();
            text.text = label;
            text.fontSize = fontSize;
            text.color = Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            SetStretch(childRt);

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            return btn;
        }

        public static Slider CreateSlider(Transform parent, float w, float h, Color fillColor, string name = "Slider")
        {
            var obj = CreateUI(parent, name);
            var bgImg = obj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            var slider = obj.AddComponent<Slider>();
            slider.interactable = false;

            var fillArea = CreateUI(obj.transform, "Fill Area");
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            var fill = CreateUI(fillArea.transform, "Fill");
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;
            slider.value = 1f;

            return slider;
        }

        public static GameObject CreateTitleBar(Transform parent, string title, string name = "TitleBar")
        {
            var bar = CreateUI(parent, name);
            var img = bar.AddComponent<Image>();
            img.color = new Color(0.1f, 0.08f, 0.16f, 0.9f);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.sizeDelta = new Vector2(0, 60);
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1);
            barRt.anchoredPosition = Vector2.zero;

            CreateDivider(bar.transform);

            var label = CreateUI(bar.transform, "Title");
            var text = label.AddComponent<Text>();
            text.text = title;
            text.fontSize = 28;
            text.color = Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
            text.fontStyle = FontStyle.Bold;
            SetStretch(label);

            return bar;
        }

        public static GameObject CreateDivider(Transform parent, string name = "Divider")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = new Color(Gold.r, Gold.g, Gold.b, 0.3f);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 2);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.anchoredPosition = Vector2.zero;
            return obj;
        }

        /// <summary>
        /// 创建可交互滑块（带拖拽手柄）
        /// </summary>
        public static Slider CreateInteractiveSlider(Transform parent, float w, float h, Color fillColor, string name = "InteractiveSlider")
        {
            var obj = CreateUI(parent, name);
            var bgImg = obj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            var slider = obj.AddComponent<Slider>();
            slider.interactable = true;

            // Fill Area
            var fillArea = CreateUI(obj.transform, "Fill Area");
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            var fill = CreateUI(fillArea.transform, "Fill");
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            // Handle
            var handleArea = CreateUI(obj.transform, "Handle Slide Area");
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = Vector2.zero;
            handleAreaRt.offsetMax = Vector2.zero;

            var handle = CreateUI(handleArea.transform, "Handle");
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(20, h + 8);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.value = 1f;

            return slider;
        }

        // --- 布局辅助 ---

        public static void SetStretch(GameObject obj)
        {
            var rt = obj.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void SetAnchored(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var rt = obj.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        /// <summary>
        /// 创建按钮并返回引用元组，方便缓存
        /// </summary>
        public static (Button btn, Text label) CreateButtonWithRef(Transform parent, string label, float w, float h,
            System.Action onClick = null, int fontSize = 22, string name = "Btn")
        {
            var btn = CreateButton(parent, label, w, h, onClick, fontSize, name);
            var labelComp = btn.transform.Find("Label").GetComponent<Text>();
            return (btn, labelComp);
        }

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
            var obj = CreateUI(parent, "PoolRow");
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0, y);

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.16f, 0.5f);

            var nameText = CreateText(obj.transform, "", 16, TextWhite, TextAnchor.MiddleLeft, name: "NameText");
            SetAnchored(nameText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-140, 0), new Vector2(width - 280, height - 6));

            var (actionBtn, actionBtnLabel) = CreateButtonWithRef(obj.transform, "", 90, (int)(height - 8),
                null, 14, "ActionBtn");
            actionBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(width / 2 - 100, 0);

            var (secondBtn, secondBtnLabel) = CreateButtonWithRef(obj.transform, "", 90, (int)(height - 8),
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

        private static GameObject CreateUI(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }
    }
}
