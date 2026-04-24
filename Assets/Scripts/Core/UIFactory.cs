using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UI 控件静态工厂 — 创建各种 UI 控件
    /// </summary>
    public static class UIFactory
    {
        // ============ 控件工厂 ============

        public static GameObject CreateFullScreenBg(Transform parent, string name = "Bg")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = UIColors.DarkBg;
            UIBuilder.SetStretch(obj);

            // 添加微弱的中心渐变光效
            var glow = CreateUI(obj.transform, "CenterGlow");
            var glowImg = glow.AddComponent<Image>();
            glowImg.color = new Color(0.10f, 0.08f, 0.18f, 0.25f);
            UIBuilder.SetStretch(glow);

            return obj;
        }

        public static GameObject CreatePanel(Transform parent, float w, float h, string name = "Panel")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = UIColors.PanelBg;
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            return obj;
        }

        public static GameObject CreateBorderedPanel(Transform parent, float w, float h, string name = "BorderedPanel")
        {
            var outer = CreateUI(parent, name);
            var outerImg = outer.AddComponent<Image>();
            outerImg.color = UIColors.BorderColor;
            var outerRt = outer.GetComponent<RectTransform>();
            outerRt.sizeDelta = new Vector2(w, h);

            var inner = CreateUI(outer.transform, "Inner");
            var innerImg = inner.AddComponent<Image>();
            innerImg.color = UIColors.PanelBg;
            var innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2, 2);
            innerRt.offsetMax = new Vector2(-2, -2);

            // 角标装饰
            AddCornerDecor(outer.transform, w, h);

            return outer;
        }

        public static Text CreateText(Transform parent, string content, int fontSize = 24, Color? color = null,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false, string name = "Text")
        {
            var obj = CreateUI(parent, name);
            var text = obj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color ?? UIColors.TextWhite;
            text.alignment = alignment;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (bold) text.fontStyle = FontStyle.Bold;
            // 文字阴影效果（用描边模拟）
            text.material = null;
            return text;
        }

        /// <summary>创建带阴影效果的文字</summary>
        public static Text CreateShadowText(Transform parent, string content, int fontSize = 24,
            Color? color = null, Color? shadowColor = null,
            TextAnchor alignment = TextAnchor.MiddleLeft, bool bold = false, string name = "ShadowText")
        {
            // 先创建阴影层
            var shadowObj = CreateUI(parent, "Shadow");
            var shadow = shadowObj.AddComponent<Text>();
            shadow.text = content;
            shadow.fontSize = fontSize;
            shadow.color = shadowColor ?? new Color(0, 0, 0, 0.5f);
            shadow.alignment = alignment;
            shadow.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (bold) shadow.fontStyle = FontStyle.Bold;
            var sRt = shadowObj.GetComponent<RectTransform>();

            // 主文字层
            var mainObj = CreateUI(parent, name);
            var text = mainObj.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color ?? UIColors.TextWhite;
            text.alignment = alignment;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (bold) text.fontStyle = FontStyle.Bold;

            // 阴影偏移
            var mRt = mainObj.GetComponent<RectTransform>();
            mRt.anchorMin = sRt.anchorMin;
            mRt.anchorMax = sRt.anchorMax;
            mRt.pivot = sRt.pivot;
            mRt.anchoredPosition = sRt.anchoredPosition + new Vector2(1, -1);
            mRt.sizeDelta = sRt.sizeDelta;

            return text;
        }

        public static Button CreateButton(Transform parent, string label, float w, float h,
            System.Action onClick = null, int fontSize = 22, string name = "Btn")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = UIColors.ButtonNormal;
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            var btn = obj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.12f, 1.06f);  // 暗金倾向高亮
            colors.pressedColor = new Color(0.72f, 0.68f, 0.58f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            btn.colors = colors;

            // 底部高光线
            var highlight = CreateUI(obj.transform, "Highlight");
            var hImg = highlight.AddComponent<Image>();
            hImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.15f);
            var hRt = highlight.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 0);
            hRt.anchorMax = new Vector2(1, 0);
            hRt.pivot = new Vector2(0.5f, 0);
            hRt.offsetMin = new Vector2(4, 0);
            hRt.offsetMax = new Vector2(-4, 3);

            // 顶部暗线
            var topLine = CreateUI(obj.transform, "TopLine");
            var tImg = topLine.AddComponent<Image>();
            tImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.08f);
            var tRt = topLine.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.offsetMin = new Vector2(4, -2);
            tRt.offsetMax = new Vector2(-4, 0);

            // 按钮文字
            var childRt = CreateUI(obj.transform, "Label");
            var text = childRt.AddComponent<Text>();
            text.text = label;
            text.fontSize = fontSize;
            text.color = UIColors.Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            UIBuilder.SetStretch(childRt);

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            return btn;
        }

        /// <summary>创建重要操作的金色按钮</summary>
        public static Button CreateGoldButton(Transform parent, string label, float w, float h,
            System.Action onClick = null, int fontSize = 24, string name = "GoldBtn")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = UIColors.ButtonGold;
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            var btn = obj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.18f, 1.05f);
            colors.pressedColor = new Color(0.65f, 0.60f, 0.45f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            // 金色边框线
            var border = CreateUI(obj.transform, "Border");
            var bImg = border.AddComponent<Image>();
            bImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.35f);
            var bRt = border.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.offsetMin = new Vector2(1, 1);
            bRt.offsetMax = new Vector2(-1, -1);

            // 按钮文字
            var childRt = CreateUI(obj.transform, "Label");
            var text = childRt.AddComponent<Text>();
            text.text = label;
            text.fontSize = fontSize;
            text.color = UIColors.Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            UIBuilder.SetStretch(childRt);

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            return btn;
        }

        public static Slider CreateSlider(Transform parent, float w, float h, Color fillColor, string name = "Slider")
        {
            var obj = CreateUI(parent, name);
            var bgImg = obj.AddComponent<Image>();
            bgImg.color = UIColors.SliderBg;
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);

            var slider = obj.AddComponent<Slider>();
            slider.interactable = false;

            var fillArea = CreateUI(obj.transform, "Fill Area");
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(2, 2);
            fillAreaRt.offsetMax = new Vector2(-2, -2);

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
            img.color = UIColors.TitleBarBg;
            var barRt = bar.GetComponent<RectTransform>();
            barRt.sizeDelta = new Vector2(0, 60);
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(0.5f, 1);
            barRt.anchoredPosition = Vector2.zero;

            // 分隔线
            CreateDivider(bar.transform);

            // 左侧装饰线
            var leftDecor = CreateUI(bar.transform, "LeftDecor");
            var ldImg = leftDecor.AddComponent<Image>();
            ldImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.25f);
            var ldRt = leftDecor.GetComponent<RectTransform>();
            ldRt.anchorMin = new Vector2(0, 0.2f);
            ldRt.anchorMax = new Vector2(0, 0.8f);
            ldRt.pivot = new Vector2(0, 0.5f);
            ldRt.anchoredPosition = new Vector2(20, 0);
            ldRt.sizeDelta = new Vector2(3, 0);

            // 右侧装饰线
            var rightDecor = CreateUI(bar.transform, "RightDecor");
            var rdImg = rightDecor.AddComponent<Image>();
            rdImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.25f);
            var rdRt = rightDecor.GetComponent<RectTransform>();
            rdRt.anchorMin = new Vector2(1, 0.2f);
            rdRt.anchorMax = new Vector2(1, 0.8f);
            rdRt.pivot = new Vector2(1, 0.5f);
            rdRt.anchoredPosition = new Vector2(-20, 0);
            rdRt.sizeDelta = new Vector2(3, 0);

            // 标题文字（带阴影）
            var label = CreateUI(bar.transform, "Title");
            var text = label.AddComponent<Text>();
            text.text = title;
            text.fontSize = 28;
            text.color = UIColors.Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            UIBuilder.SetStretch(label);

            // 底部发光线
            var glow = CreateUI(bar.transform, "Glow");
            var gImg = glow.AddComponent<Image>();
            gImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.12f);
            var gRt = glow.GetComponent<RectTransform>();
            gRt.sizeDelta = new Vector2(0, 1);
            gRt.anchorMin = new Vector2(0, 0);
            gRt.anchorMax = new Vector2(1, 0);
            gRt.anchoredPosition = Vector2.zero;

            return bar;
        }

        public static GameObject CreateDivider(Transform parent, string name = "Divider")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.20f);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 1);
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
            bgImg.color = UIColors.SliderBg;
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
            handleImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.75f);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(16, h + 10);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.value = 1f;

            return slider;
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

        // ============ 装饰角标 ============

        /// <summary>在面板四角添加金色装饰角标</summary>
        private static void AddCornerDecor(Transform parent, float w, float h)
        {
            float cornerSize = 12f;
            float inset = 6f;
            Color cornerColor = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.5f);

            // 四个角的位置
            var positions = new[]
            {
                new Vector2(-w/2 + inset, h/2 - inset),  // 左上
                new Vector2(w/2 - inset, h/2 - inset),   // 右上
                new Vector2(-w/2 + inset, -h/2 + inset), // 左下
                new Vector2(w/2 - inset, -h/2 + inset)   // 右下
            };

            for (int i = 0; i < 4; i++)
            {
                var corner = CreateUI(parent, $"Corner_{i}");
                var cImg = corner.AddComponent<Image>();
                cImg.color = cornerColor;
                var cRt = corner.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0.5f, 0.5f);
                cRt.anchorMax = new Vector2(0.5f, 0.5f);
                cRt.pivot = new Vector2(0.5f, 0.5f);
                cRt.anchoredPosition = positions[i];
                cRt.sizeDelta = new Vector2(cornerSize, cornerSize);
            }
        }

        // ============ 内部工具 ============

        internal static GameObject CreateUI(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }
    }
}
