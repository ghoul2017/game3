using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UI 面板基类，提供生命周期钩子。
    /// 预制体模式：Override BindReferences() + BindEvents()，OnInit 自动调用。
    /// 旧代码模式：直接 Override OnInit() 创建 UI（迁移后删除）。
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private bool isInitialized;
        private Coroutine transitionCoroutine;

        // ---- 生命周期 ----

        /// <summary>首次显示时调用。默认调用 BindReferences + BindEvents。</summary>
        private static Font runtimeFont;

        public virtual void OnInit()
        {
            BindReferences();
            BindEvents();
            FixFonts();
        }

        /// <summary>
        /// 修复预制体中字体引用：Resources.GetBuiltinResource 生成的字体在运行时可能无法渲染中文，
        /// 用 Font.CreateDynamicFontFromOSFont 替换确保所有 Text 正常显示。
        /// </summary>
        private void FixFonts()
        {
            if (runtimeFont == null)
                runtimeFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            foreach (var text in GetComponentsInChildren<Text>(true))
                text.font = runtimeFont;
        }

        /// <summary>每次显示时调用。</summary>
        public virtual void OnShow() { }

        /// <summary>每次隐藏时调用。</summary>
        public virtual void OnHide() { }

        /// <summary>销毁时调用。</summary>
        public virtual void OnDestroyed() { }

        // ---- 面板过渡动画协程 ----

        /// <summary>淡入协程：alpha 0→1，持续0.2s</summary>
        private IEnumerator FadeInCoroutine()
        {
            float duration = 0.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = t;
                yield return null;
            }
            canvasGroup.alpha = 1f;
            transitionCoroutine = null;
        }

        /// <summary>停止面板过渡动画（供子类调用）</summary>
        protected void StopTransition()
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }
        }

        // ---- 预制体模式虚方法 ----

        /// <summary>
        /// 预制体模式：从预制体层级中查找并缓存组件引用。
        /// 示例：musicSlider = transform.Find("MusicSlider").GetComponent&lt;Slider&gt;();
        /// </summary>
        protected virtual void BindReferences() { }

        /// <summary>
        /// 预制体模式：绑定事件回调（按钮点击、滑块变化等）。
        /// 在 BindReferences 之后调用，此时所有引用已缓存。
        /// </summary>
        protected virtual void BindEvents() { }

        // ---- 公开 API ----

        /// <summary>
        /// Editor 用：强制触发 OnInit（用于 PrefabGenerator 生成预制体）
        /// 运行时不要调用，由 Show() 自动管理。
        /// </summary>
        public void InvokeInit()
        {
            if (!isInitialized)
            {
                isInitialized = true;
                OnInit();
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            EnsureCanvasGroup();
            EnsureRectTransform();

            if (!isInitialized)
            {
                isInitialized = true;
                Debug.Log($"[Panel] OnInit -> {GetType().Name}");
                OnInit();
            }

            Debug.Log($"[Panel] Show -> {GetType().Name} 子物体数={transform.childCount}");

            // 停止旧的过渡动画（不使用StopAllCoroutines，避免中断子类协程）
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            // 面板过渡动画：alpha 0→1 淡入
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            transitionCoroutine = StartCoroutine(FadeInCoroutine());

            // 播放面板打开音效
            AudioManager.Instance?.PlaySFX(AudioClipType.UI_PanelOpen);

            OnShow();
        }

        public void Hide()
        {
            EnsureCanvasGroup();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 停止旧的过渡动画
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            // 面板过渡动画：alpha 1→0 淡出
            canvasGroup.alpha = 0f;

            // 播放面板关闭音效
            AudioManager.Instance?.PlaySFX(AudioClipType.UI_PanelClose);

            OnHide();
        }

        // ---- 内部辅助 ----

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        protected void EnsureRectTransform()
        {
            var rt = transform as RectTransform;
            if (rt == null)
            {
                // 预制体可能以 Transform 保存，需要升级为 RectTransform
                gameObject.AddComponent<RectTransform>();
                rt = transform as RectTransform;
            }
            if (rt != null && rt.parent != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        protected virtual void OnDestroy()
        {
            OnDestroyed();
        }

        /// <summary>
        /// 播放 UI 点击音效（按钮点击时统一调用）
        /// </summary>
        protected void PlayUIClick()
        {
            AudioManager.Instance?.PlaySFX(AudioClipType.UI_Click);
        }

        /// <summary>
        /// 辅助方法：安全查找子物体上的组件。
        /// path 用 "/" 分隔层级，如 "MapPanel/Inner/StatusPanel"
        /// </summary>
        protected T FindComponent<T>(string path) where T : Component
        {
            var t = transform.Find(path);
            if (t != null)
            {
                var comp = t.GetComponent<T>();
                if (comp != null) return comp;
            }
            Debug.LogWarning($"[UIPanel] FindComponent<{typeof(T).Name}> 失败: {GetType().Name}/{path}");
            return null;
        }

        /// <summary>
        /// 辅助方法：安全查找子物体的 Transform
        /// </summary>
        protected Transform FindChild(string path)
        {
            var t = transform.Find(path);
            if (t == null)
                Debug.LogWarning($"[UIPanel] FindChild 失败: {GetType().Name}/{path}");
            return t;
        }
    }
}
