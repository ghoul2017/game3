using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UI 管理器单例，管理面板栈和 Canvas
    /// </summary>
    public class UIManager
    {
        private static UIManager instance;
        public static UIManager Instance => instance;

        private readonly Dictionary<string, UIPanel> panelCache = new Dictionary<string, UIPanel>();
        private readonly Stack<UIPanel> panelStack = new Stack<UIPanel>();
        private Transform canvasTransform;

        public static UIManager EnsureCreated()
        {
            if (instance != null) return instance;

            instance = new UIManager();
            instance.InitCanvas();
            return instance;
        }

        private void InitCanvas()
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasTransform = canvasObj.transform;
            Object.DontDestroyOnLoad(canvasObj);

            // 自动创建 EventSystem
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Object.DontDestroyOnLoad(esObj);
            }
        }

        public T ShowPanel<T>() where T : UIPanel
        {
            // 隐藏栈顶
            if (panelStack.Count > 0)
            {
                var top = panelStack.Peek();
                top.Hide();
            }

            var panel = GetOrCreatePanel<T>();
            panel.Show();
            panelStack.Push(panel);
            return panel;
        }

        public void Back()
        {
            if (panelStack.Count == 0) return;

            var current = panelStack.Pop();
            current.Hide();

            if (panelStack.Count > 0)
            {
                var prev = panelStack.Peek();
                prev.Show();
            }
        }

        public T ReplacePanel<T>() where T : UIPanel
        {
            if (panelStack.Count > 0)
            {
                var old = panelStack.Pop();
                old.Hide();
            }
            return ShowPanel<T>();
        }

        public void ClearAll()
        {
            while (panelStack.Count > 0)
            {
                var panel = panelStack.Pop();
                panel.Hide();
            }
        }

        private T GetOrCreatePanel<T>() where T : UIPanel
        {
            string typeName = typeof(T).Name;

            if (panelCache.TryGetValue(typeName, out var cached))
                return (T)cached;

            // 尝试从 Resources/UI 加载
            var prefab = Resources.Load<GameObject>($"UI/{typeName}");
            GameObject panelObj;
            if (prefab != null)
            {
                panelObj = Object.Instantiate(prefab, canvasTransform, false);
            }
            else
            {
                // 纯代码创建
                panelObj = new GameObject(typeName);
                panelObj.transform.SetParent(canvasTransform, false);
            }

            var panel = panelObj.GetOrAddComponent<T>();
            panelCache[typeName] = panel;
            return panel;
        }

        public T GetPanel<T>() where T : UIPanel
        {
            string typeName = typeof(T).Name;
            panelCache.TryGetValue(typeName, out var panel);
            return (T)panel;
        }
    }

    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
    }
}
