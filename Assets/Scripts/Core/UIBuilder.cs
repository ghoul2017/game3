using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UI 控件静态工厂 — 深色主题：黑底(#0d0d1a) + 暗金强调(#c8a24c)
    /// </summary>
    public static class UIBuilder
    {
        // ============ 配色系统 ============

        // 背景
        public static readonly Color DarkBg       = new Color(0.051f, 0.051f, 0.102f, 0.97f);   // 极深黑蓝 #0d0d1a
        public static readonly Color PanelBg      = new Color(0.102f, 0.102f, 0.180f, 0.96f);   // 面板底色 #1a1a2e
        public static readonly Color CardBg       = new Color(0.133f, 0.133f, 0.267f, 0.95f);   // 卡片背景 #222244

        // 文字
        public static readonly Color Gold         = new Color(0.784f, 0.635f, 0.298f);           // 暗金标题 #c8a24c
        public static readonly Color GoldDim      = new Color(0.627f, 0.510f, 0.239f);           // 暗金(暗)
        public static readonly Color TextWhite    = new Color(0.878f, 0.847f, 0.753f);           // 正文色 #e0d8c0
        public static readonly Color TextDim      = new Color(0.588f, 0.529f, 0.475f);           // 次要文字 #958678 (对比度≥4.5:1)

        // 按钮
        public static readonly Color ButtonNormal = new Color(0.176f, 0.176f, 0.333f, 0.95f);   // 按钮正常 #2d2d55
        public static readonly Color ButtonHover  = new Color(0.239f, 0.239f, 0.416f, 0.95f);   // 按钮悬停 #3d3d6a
        public static readonly Color ButtonPress  = new Color(0.102f, 0.102f, 0.200f, 0.95f);   // 按下更深
        public static readonly Color ButtonGold   = new Color(0.235f, 0.180f, 0.078f, 0.95f);   // 金色按钮(重要操作)
        public static readonly Color ButtonDisable = new Color(0.102f, 0.102f, 0.200f, 0.95f);  // 禁用按钮 #1a1a33

        // 边框
        public static readonly Color BorderColor  = new Color(0.392f, 0.318f, 0.149f, 0.70f);   // 暗金边框
        public static readonly Color BorderDim    = new Color(0.235f, 0.191f, 0.089f, 0.55f);   // 暗淡边框

        // 功能色（高饱和度，适配深色背景）
        public static readonly Color Red          = new Color(0.906f, 0.298f, 0.235f);           // 危险红 #e74c3c
        public static readonly Color Green        = new Color(0.345f, 0.839f, 0.553f);           // 治愈绿 #58d68d
        public static readonly Color Blue         = new Color(0.365f, 0.678f, 0.886f);           // 法力蓝 #5dade2
        public static readonly Color Purple       = new Color(0.686f, 0.478f, 0.773f);           // 魔法紫 #af7ac5
        public static readonly Color Orange       = new Color(0.941f, 0.753f, 0.251f);           // 金色图标 #f0c040

        // 状态条
        public static readonly Color HpColor      = new Color(0.906f, 0.298f, 0.235f);           // 生命红 (同Red)
        public static readonly Color MpColor      = new Color(0.365f, 0.678f, 0.886f);           // 法力蓝 (同Blue)
        public static readonly Color ExpColor     = new Color(0.941f, 0.753f, 0.251f);           // 经验金 (同Orange)

        // 房间颜色
        public static readonly Color RoomCurrent  = new Color(0.365f, 0.678f, 0.886f, 0.90f);   // 当前房间(蓝)
        public static readonly Color RoomCleared  = new Color(0.133f, 0.133f, 0.267f, 0.50f);   // 已清房间
        public static readonly Color RoomMonster  = new Color(0.906f, 0.298f, 0.235f, 0.85f);   // 怪物(红)
        public static readonly Color RoomElite    = new Color(0.686f, 0.478f, 0.773f, 0.85f);   // 精英(紫)
        public static readonly Color RoomBoss     = new Color(0.950f, 0.150f, 0.150f, 0.92f);   // BOSS(亮红)
        public static readonly Color RoomTreasure = new Color(0.941f, 0.753f, 0.251f, 0.85f);   // 宝箱(金)
        public static readonly Color RoomSpring   = new Color(0.345f, 0.839f, 0.553f, 0.85f);   // 泉水(绿)
        public static readonly Color RoomMerchant = new Color(0.750f, 0.550f, 0.150f, 0.85f);   // 商人(橙)
        public static readonly Color RoomExit     = new Color(0.365f, 0.678f, 0.886f, 0.90f);   // 出口(蓝)

        // 交替行色
        public static readonly Color RowEven      = new Color(0.133f, 0.133f, 0.267f, 0.40f);   // CardBg 变体
        public static readonly Color RowOdd       = new Color(0.102f, 0.102f, 0.180f, 0.35f);   // PanelBg 变体

        // 信息栏背景
        public static readonly Color InfoBarBg    = new Color(0.051f, 0.051f, 0.102f, 0.92f);   // DarkBg 同色
        // 标题栏背景
        public static readonly Color TitleBarBg   = new Color(0.067f, 0.055f, 0.125f, 0.95f);   // 深色标题栏
        // 滑块底色
        public static readonly Color SliderBg     = new Color(0.067f, 0.067f, 0.125f, 0.85f);   // 滑块暗色底色
        // 视口背景(ScrollRect Viewport)
        public static readonly Color ViewportBg   = new Color(0.102f, 0.102f, 0.180f, 0.96f);   // 同 PanelBg

        // ============ 装饰角标 ============

        /// <summary>在面板四角添加金色装饰角标</summary>
        private static void AddCornerDecor(Transform parent, float w, float h)
        {
            float cornerSize = 12f;
            float inset = 6f;
            Color cornerColor = new Color(Gold.r, Gold.g, Gold.b, 0.5f);

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

        // ============ 控件工厂 ============

        public static GameObject CreateFullScreenBg(Transform parent, string name = "Bg")
        {
            var obj = CreateUI(parent, name);
            var img = obj.AddComponent<Image>();
            img.color = DarkBg;
            SetStretch(obj);

            // 添加微弱的中心渐变光效
            var glow = CreateUI(obj.transform, "CenterGlow");
            var glowImg = glow.AddComponent<Image>();
            glowImg.color = new Color(0.10f, 0.08f, 0.18f, 0.25f);
            SetStretch(glow);

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
            text.color = color ?? TextWhite;
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
            text.color = color ?? TextWhite;
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
            img.color = ButtonNormal;
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
            hImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.15f);
            var hRt = highlight.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 0);
            hRt.anchorMax = new Vector2(1, 0);
            hRt.pivot = new Vector2(0.5f, 0);
            hRt.offsetMin = new Vector2(4, 0);
            hRt.offsetMax = new Vector2(-4, 3);

            // 顶部暗线
            var topLine = CreateUI(obj.transform, "TopLine");
            var tImg = topLine.AddComponent<Image>();
            tImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.08f);
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
            text.color = Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            SetStretch(childRt);

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
            img.color = ButtonGold;
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
            bImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
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
            text.color = Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            SetStretch(childRt);

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            return btn;
        }

        public static Slider CreateSlider(Transform parent, float w, float h, Color fillColor, string name = "Slider")
        {
            var obj = CreateUI(parent, name);
            var bgImg = obj.AddComponent<Image>();
            bgImg.color = SliderBg;
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
            img.color = TitleBarBg;
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
            ldImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.25f);
            var ldRt = leftDecor.GetComponent<RectTransform>();
            ldRt.anchorMin = new Vector2(0, 0.2f);
            ldRt.anchorMax = new Vector2(0, 0.8f);
            ldRt.pivot = new Vector2(0, 0.5f);
            ldRt.anchoredPosition = new Vector2(20, 0);
            ldRt.sizeDelta = new Vector2(3, 0);

            // 右侧装饰线
            var rightDecor = CreateUI(bar.transform, "RightDecor");
            var rdImg = rightDecor.AddComponent<Image>();
            rdImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.25f);
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
            text.color = Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontStyle = FontStyle.Bold;
            SetStretch(label);

            // 底部发光线
            var glow = CreateUI(bar.transform, "Glow");
            var gImg = glow.AddComponent<Image>();
            gImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.12f);
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
            img.color = new Color(Gold.r, Gold.g, Gold.b, 0.20f);
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
            bgImg.color = SliderBg;
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
            handleImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.75f);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(16, h + 10);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.value = 1f;

            return slider;
        }

        // ============ 布局辅助 ============

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

        // ============ 对象池 ============

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
            bg.color = RowEven;

            // 左侧装饰条
            var leftBar = CreateUI(obj.transform, "LeftBar");
            var lbImg = leftBar.AddComponent<Image>();
            lbImg.color = new Color(Gold.r, Gold.g, Gold.b, 0.15f);
            var lbRt = leftBar.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0, 0.15f);
            lbRt.anchorMax = new Vector2(0, 0.85f);
            lbRt.pivot = new Vector2(0, 0.5f);
            lbRt.anchoredPosition = new Vector2(2, 0);
            lbRt.sizeDelta = new Vector2(2, 0);

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

        // ============ 内部工具 ============

        private static GameObject CreateUI(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }
    }
}
