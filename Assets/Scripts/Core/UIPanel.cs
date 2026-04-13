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

            // 停止之前的淡出协程，防止冲突
            StopAllCoroutines();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            OnShow();
        }

        public void Hide()
        {
            EnsureCanvasGroup();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // 停止之前的淡入协程，防止冲突
            StopAllCoroutines();
            canvasGroup.alpha = 0f;
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

        protected virtual void OnDestroy()
        {
            OnDestroyed();
        }
    }
}
