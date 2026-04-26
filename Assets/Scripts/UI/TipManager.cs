using System.Collections;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 浮动信息提示系统 — HoverTip(鼠标跟随) + AutoTip(定时淡出)
    /// 对象池8个Tip，多个可共存，挂载到 Tip Canvas 上
    /// </summary>
    public class TipManager : MonoBehaviour
    {
        private static TipManager instance;
        public static TipManager Instance => instance;

        // 对象池
        private const int POOL_SIZE = 8;
        private readonly TipItem[] pool = new TipItem[POOL_SIZE];

        // HoverTip 索引（同时只有一个 HoverTip）
        private int hoverTipIndex = -1;

        // Canvas 的 RectTransform，用于坐标转换
        private RectTransform canvasRt;

        // 结构体：每个池对象的数据
        private struct TipItem
        {
            public GameObject obj;
            public RectTransform rt;
            public Text text;
            public CanvasGroup canvasGroup;
            public Coroutine fadeCoroutine;
            public bool inUse;
            public TipKind kind;
        }

        private enum TipKind
        {
            None,
            Hover,
            Auto
        }

        // ============ 生命周期 ============

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            // 获取所在 Canvas 的 RectTransform，用于屏幕坐标转换
            canvasRt = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (canvasRt == null) canvasRt = GetComponent<RectTransform>();
            InitPool();
        }

        private void Update()
        {
            // HoverTip 固定位置，不跟随鼠标
        }

        // ============ 初始化 ============

        private void InitPool()
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                var obj = UIFactory.CreateUI(transform, $"Tip_{i}");
                var rt = obj.GetComponent<RectTransform>();

                // 背景：CardBg + BorderColor 边框
                var bg = UIFactory.CreateUI(obj.transform, "Bg");
                var bgImg = bg.AddComponent<Image>();
                bgImg.color = UIColors.CardBg;
                var bgRt = bg.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;

                // 边框（1px）
                var border = UIFactory.CreateUI(obj.transform, "Border");
                var borderImg = border.AddComponent<Image>();
                borderImg.color = UIColors.BorderColor;
                var borderRt = border.GetComponent<RectTransform>();
                borderRt.anchorMin = Vector2.zero;
                borderRt.anchorMax = Vector2.one;
                borderRt.offsetMin = new Vector2(-1, -1);
                borderRt.offsetMax = new Vector2(1, 1);

                // 背景放在边框后面
                bg.transform.SetAsFirstSibling();

                // 文字
                var text = UIFactory.CreateText(obj.transform, "", 20, UIColors.TextWhite,
                    TextAnchor.MiddleLeft, false, "Text");
                var textRt = text.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(10, 6);
                textRt.offsetMax = new Vector2(-10, -6);
                text.horizontalOverflow = HorizontalWrapMode.Overflow;

                // CanvasGroup
                var cg = obj.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.blocksRaycasts = false;

                obj.SetActive(false);

                pool[i] = new TipItem
                {
                    obj = obj,
                    rt = rt,
                    text = text,
                    canvasGroup = cg,
                    fadeCoroutine = null,
                    inUse = false,
                    kind = TipKind.None
                };
            }
        }

        // ============ 公共 API ============

        /// <summary>
        /// 显示 HoverTip — 固定在锚定坐标，不跟随鼠标
        /// </summary>
        /// <param name="content">提示内容</param>
        /// <param name="anchoredPos">Tip Canvas 中的锚定坐标</param>
        public void ShowHoverTipAt(string content, Vector2 anchoredPos)
        {
            // 如果已有 HoverTip，直接更新内容
            if (hoverTipIndex >= 0)
            {
                var item = pool[hoverTipIndex];
                item.text.text = content;
                ResizeToFit(item);
                item.rt.anchoredPosition = anchoredPos;
                pool[hoverTipIndex] = item;
                return;
            }

            // 找空闲槽位
            int idx = FindFreeSlot();
            if (idx < 0) return;

            hoverTipIndex = idx;
            var tip = pool[idx];
            tip.inUse = true;
            tip.kind = TipKind.Hover;
            tip.text.text = content;
            tip.canvasGroup.alpha = 1f;
            tip.rt.anchoredPosition = anchoredPos;
            tip.obj.SetActive(true);

            ResizeToFit(tip);
            pool[idx] = tip;
        }

        /// <summary>
        /// 隐藏 HoverTip — 立即关闭
        /// </summary>
        public void HideHoverTip()
        {
            if (hoverTipIndex < 0) return;

            ReleaseSlot(hoverTipIndex);
            hoverTipIndex = -1;
        }

        /// <summary>
        /// 显示 AutoTip — 定时淡出
        /// </summary>
        /// <param name="content">提示内容</param>
        /// <param name="anchoredPos">在 Tip Canvas 中的锚定坐标</param>
        /// <param name="duration">显示时长（秒），默认2.5</param>
        public void ShowAutoTip(string content, Vector2 anchoredPos, float duration = 2.5f)
        {
            int idx = FindFreeSlot();
            if (idx < 0) return; // 池满，静默丢弃

            var tip = pool[idx];
            tip.inUse = true;
            tip.kind = TipKind.Auto;
            tip.text.text = content;
            tip.canvasGroup.alpha = 1f;
            tip.rt.anchoredPosition = anchoredPos;
            tip.obj.SetActive(true);

            ResizeToFit(tip);
            pool[idx] = tip;

            // 启动定时淡出协程
            if (tip.fadeCoroutine != null)
                StopCoroutine(tip.fadeCoroutine);
            tip.fadeCoroutine = StartCoroutine(AutoFadeCoroutine(idx, duration));
            pool[idx] = tip;
        }

        // ============ 内部方法 ============

        /// <summary>
        /// AutoTip 淡出协程：等待 duration - fadeTime 后开始 0.3s 淡出
        /// </summary>
        private IEnumerator AutoFadeCoroutine(int idx, float duration)
        {
            const float fadeTime = 0.3f;
            float waitTime = Mathf.Max(0f, duration - fadeTime);

            // 等待（减去淡出时间）
            yield return new WaitForSeconds(waitTime);

            // 淡出
            var tip = pool[idx];
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeTime);
                tip.canvasGroup.alpha = 1f - t;
                yield return null;
            }

            // 完成，归还池
            ReleaseSlot(idx);
        }

        /// <summary>
        /// 根据文字内容自适应 Tip 尺寸
        /// </summary>
        private void ResizeToFit(TipItem tip)
        {
            // 强制刷新文字布局
            var generator = tip.text.cachedTextGenerator;
            var settings = tip.text.GetGenerationSettings(tip.text.rectTransform.rect.size);
            generator.Populate(tip.text.text, settings);

            float preferredWidth = tip.text.preferredWidth;
            float preferredHeight = tip.text.preferredHeight;

            // 加上内边距
            tip.rt.sizeDelta = new Vector2(preferredWidth + 20f, preferredHeight + 12f);
        }

        /// <summary>
        /// 找到空闲池槽位，返回索引；全满返回 -1
        /// </summary>
        private int FindFreeSlot()
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                if (!pool[i].inUse) return i;
            }
            return -1;
        }

        /// <summary>
        /// 归还池槽位：停协程、隐藏、重置状态
        /// </summary>
        private void ReleaseSlot(int idx)
        {
            var tip = pool[idx];

            // 精准停止淡出协程
            if (tip.fadeCoroutine != null)
            {
                StopCoroutine(tip.fadeCoroutine);
                tip.fadeCoroutine = null;
            }

            tip.canvasGroup.alpha = 0f;
            tip.obj.SetActive(false);
            tip.inUse = false;
            tip.kind = TipKind.None;
            tip.text.text = "";
            pool[idx] = tip;
        }
    }
}
