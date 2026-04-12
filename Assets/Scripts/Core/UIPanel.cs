using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UI 面板基类，提供生命周期钩子和淡入淡出动画
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private bool isInitialized;

        public virtual void OnInit() { }
        public virtual void OnShow() { }
        public virtual void OnHide() { }
        public virtual void OnDestroyed() { }

        public void Show()
        {
            gameObject.SetActive(true);
            EnsureCanvasGroup();
            EnsureRectTransform();

            if (!isInitialized)
            {
                isInitialized = true;
                OnInit();
            }

            canvasGroup.interactable = false;
            StartCoroutine(FadeCoroutine(0f, 1f, 0.2f, () =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }));
            OnShow();
        }

        public void Hide()
        {
            EnsureCanvasGroup();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            StartCoroutine(FadeCoroutine(1f, 0f, 0.2f, () =>
            {
                gameObject.SetActive(false);
            }));
            OnHide();
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        protected void EnsureRectTransform()
        {
            var rt = transform as RectTransform;
            if (rt != null && rt.parent != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private IEnumerator FadeCoroutine(float from, float to, float duration, Action onComplete = null)
        {
            canvasGroup.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
            onComplete?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            OnDestroyed();
        }
    }
}
