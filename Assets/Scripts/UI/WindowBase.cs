using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 可拖动非全屏窗口基类。
    /// 默认 700x500 居中，标题栏(40px 可拖拽) + 关闭按钮 + 内容区。
    /// 最多 1 个窗口同时打开（UIManager 管理）。
    /// </summary>
    public abstract class WindowBase : UIPanel, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        // ============ 布局常量 ============
        protected const float DEFAULT_WIDTH = 700f;
        protected const float DEFAULT_HEIGHT = 500f;
        protected const float TITLE_BAR_HEIGHT = 40f;

        // ============ 组件引用 ============
        private RectTransform windowContainer;
        private RectTransform titleBarRect;
        private Text titleText;
        private Button closeButton;
        private RectTransform contentContainer;

        // 拖拽状态
        private bool isDragging;
        private Vector2 dragStartPointerPos;
        private Vector2 dragStartWindowPos;

        // ============ 子类虚方法 ============

        /// <summary>返回窗口标题文字</summary>
        protected virtual string GetWindowTitle() => "窗口";

        /// <summary>子类在内容区构建 UI 控件（在 BindReferences 中调用）</summary>
        protected abstract void BuildContent(Transform contentParent);

        /// <summary>窗口打开后回调（OnShow 中调用）</summary>
        protected virtual void OnWindowOpen() { }

        /// <summary>窗口关闭前回调（CloseWindow 中调用）</summary>
        protected virtual void OnWindowClose() { }

        /// <summary>获取窗口宽度，子类可覆盖</summary>
        protected virtual float GetWindowWidth() => DEFAULT_WIDTH;

        /// <summary>获取窗口高度，子类可覆盖</summary>
        protected virtual float GetWindowHeight() => DEFAULT_HEIGHT;

        // ============ 生命周期 ============

        protected override void BindReferences()
        {
            BuildWindowStructure();
        }

        protected override void BindEvents()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        public override void OnShow()
        {
            RefreshUI();
            OnWindowOpen();
        }

        // ============ 窗口结构构建 ============

        private void BuildWindowStructure()
        {
            float w = GetWindowWidth();
            float h = GetWindowHeight();

            // 根节点居中定位
            var root = (RectTransform)transform;
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(w, h);
            root.anchoredPosition = Vector2.zero;

            // 外层容器（带边框的面板）
            var containerObj = UIFactory.CreateBorderedPanel(root, w, h, "WindowContainer");
            var containerRt = containerObj.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = Vector2.zero;
            windowContainer = containerRt;

            // 标题栏（40px，顶部锚定，可拖拽区域）
            BuildTitleBar(containerObj.transform, w);

            // 内容区（标题栏以下全部空间）
            BuildContentArea(containerObj.transform);

            // 子类构建具体内容
            BuildContent(contentContainer);
        }

        /// <summary>
        /// 构建标题栏：背景 + 标题文字 + 关闭按钮
        /// </summary>
        private void BuildTitleBar(Transform parent, float windowWidth)
        {
            var barObj = UIFactory.CreateUI(parent, "TitleBar");
            var barImg = barObj.AddComponent<Image>();
            barImg.color = UIColors.TitleBarBg;
            var barRt = barObj.GetComponent<RectTransform>();
            // 顶部锚定，宽度填满，高度 40px
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(0f, TITLE_BAR_HEIGHT);
            barRt.anchoredPosition = Vector2.zero;
            titleBarRect = barRt;

            // 标题栏拖拽区域（整个标题栏 Image 作为 raycast 目标）
            // Image 组件已有，确保 raycastTarget=true

            // 标题文字
            var titleObj = UIFactory.CreateUI(barObj.transform, "TitleText");
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0f);
            titleRt.anchorMax = new Vector2(0.85f, 1f);
            titleRt.offsetMin = new Vector2(12, 0);
            titleRt.offsetMax = new Vector2(-4, 0);
            titleText = titleObj.AddComponent<Text>();
            titleText.text = GetWindowTitle();
            titleText.fontSize = 22;
            titleText.color = UIColors.Gold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontStyle = FontStyle.Bold;

            // 关闭按钮（右上角）
            var closeObj = UIFactory.CreateUI(barObj.transform, "BtnClose");
            var closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.sizeDelta = new Vector2(30, 30);
            closeRt.anchoredPosition = new Vector2(-6, 0);
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = UIColors.ButtonNormal;
            closeButton = closeObj.AddComponent<Button>();
            var btnColors = closeButton.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = new Color(1.2f, 1.0f, 1.0f);
            btnColors.pressedColor = new Color(0.7f, 0.6f, 0.6f);
            btnColors.fadeDuration = 0.1f;
            closeButton.colors = btnColors;

            // 关闭按钮文字 "X"
            var closeLabel = UIFactory.CreateUI(closeObj.transform, "Label");
            var closeLabelRt = closeLabel.GetComponent<RectTransform>();
            closeLabelRt.anchorMin = Vector2.zero;
            closeLabelRt.anchorMax = Vector2.one;
            closeLabelRt.offsetMin = Vector2.zero;
            closeLabelRt.offsetMax = Vector2.zero;
            var closeLabelText = closeLabel.AddComponent<Text>();
            closeLabelText.text = "X";
            closeLabelText.fontSize = 18;
            closeLabelText.color = UIColors.Red;
            closeLabelText.alignment = TextAnchor.MiddleCenter;
            closeLabelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            closeLabelText.fontStyle = FontStyle.Bold;

            // 标题栏底部金色分隔线
            var divider = UIFactory.CreateUI(barObj.transform, "Divider");
            var divImg = divider.AddComponent<Image>();
            divImg.color = new Color(UIColors.Gold.r, UIColors.Gold.g, UIColors.Gold.b, 0.25f);
            var divRt = divider.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0f, 0f);
            divRt.anchorMax = new Vector2(1f, 0f);
            divRt.pivot = new Vector2(0.5f, 0f);
            divRt.sizeDelta = new Vector2(0f, 1f);
            divRt.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 构建内容区：标题栏以下全部空间
        /// </summary>
        private void BuildContentArea(Transform parent)
        {
            var contentObj = UIFactory.CreateUI(parent, "Content");
            var contentRt = contentObj.GetComponent<RectTransform>();
            // 拉伸填满，顶部留出标题栏高度
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(2, 2);
            contentRt.offsetMax = new Vector2(-2, -TITLE_BAR_HEIGHT);
            contentContainer = contentRt;
        }

        // ============ 拖拽实现 ============

        /// <summary>判断拖拽事件是否发生在标题栏区域内</summary>
        private bool IsPointerOverTitleBar(PointerEventData eventData)
        {
            if (titleBarRect == null) return false;

            // 将屏幕坐标转换为标题栏局部坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                titleBarRect, eventData.position, eventData.pressEventCamera, out var localPos);

            return titleBarRect.rect.Contains(localPos);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 仅标题栏区域可拖拽
            if (!IsPointerOverTitleBar(eventData)) return;

            isDragging = true;
            dragStartPointerPos = eventData.position;
            dragStartWindowPos = ((RectTransform)transform).anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            var delta = eventData.position - dragStartPointerPos;
            var rt = (RectTransform)transform;
            rt.anchoredPosition = dragStartWindowPos + delta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        // ============ 关闭按钮 ============

        private void OnCloseButtonClicked()
        {
            PlayUIClick();
            UIManager.Instance?.CloseWindow();
        }

        // ============ 刷新 ============

        /// <summary>
        /// 刷新 UI（更新标题文字等）。子类可 override 扩展。
        /// </summary>
        protected virtual void RefreshUI()
        {
            if (titleText != null)
                titleText.text = GetWindowTitle();
        }

        // ============ 公开方法 ============

        /// <summary>获取内容区容器，供子类或外部使用</summary>
        public RectTransform ContentContainer => contentContainer;

        /// <summary>供 UIManager 调用的关闭回调</summary>
        public void InvokeWindowClose()
        {
            OnWindowClose();
        }
    }
}
