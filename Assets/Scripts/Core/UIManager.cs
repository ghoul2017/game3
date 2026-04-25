using System;
using System.Collections.Generic;
using Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// UI 管理器单例，管理面板栈和三层 Canvas（HUD/Window/Tip）
    /// </summary>
    public class UIManager
    {
        private static UIManager instance;
        public static UIManager Instance => instance;

        private readonly Dictionary<string, UIPanel> panelCache = new Dictionary<string, UIPanel>();
        private readonly Stack<UIPanel> panelStack = new Stack<UIPanel>();

        // 三层 Canvas
        private Canvas canvasHUD;
        private Canvas canvasWindow;
        private Canvas canvasTip;

        /// <summary>HUD 层 Canvas（sortingOrder=5），永久存在的固定布局</summary>
        public Canvas CanvasHUD => canvasHUD;
        /// <summary>Window 层 Canvas（sortingOrder=10），非全屏可拖动窗口</summary>
        public Canvas CanvasWindow => canvasWindow;
        /// <summary>Tip 层 Canvas（sortingOrder=20），浮动信息</summary>
        public Canvas CanvasTip => canvasTip;

        // 当前打开的窗口面板
        private UIPanel currentWindow;

        public static UIManager EnsureCreated()
        {
            if (instance != null) return instance;

            instance = new UIManager();
            instance.InitCanvas();
            return instance;
        }

        private void InitCanvas()
        {
            // 创建三层 Canvas
            canvasHUD = CreateLayerCanvas("HUD", 5);
            canvasWindow = CreateLayerCanvas("Window", 10);
            canvasTip = CreateLayerCanvas("Tip", 20);

            // 自动创建 EventSystem
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Object.DontDestroyOnLoad(esObj);
            }
        }

        /// <summary>
        /// 创建一个层 Canvas，配备 CanvasScaler 和 GraphicRaycaster
        /// </summary>
        private Canvas CreateLayerCanvas(string name, int sortingOrder)
        {
            var canvasObj = new GameObject($"Canvas_{name}");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Object.DontDestroyOnLoad(canvasObj);
            return canvas;
        }

        // ---- 新 API：Window 窗口 ----

        /// <summary>
        /// 显示窗口面板到 Window Canvas。
        /// 先关闭已有窗口，再实例化面板并调用 OnInit/OnShow。
        /// </summary>
        public T ShowWindow<T>() where T : UIPanel
        {
            // 关闭已有窗口（调用 OnWindowClose 并销毁）
            if (currentWindow != null)
            {
                CloseWindowInternal();
            }

            var panel = GetOrCreatePanel<T>(canvasWindow.transform);
            panel.Show();
            currentWindow = panel;
            Debug.Log($"[UI] ShowWindow<{typeof(T).Name}>");
            return panel;
        }

        /// <summary>
        /// 关闭当前 Window 层面板。
        /// 对 WindowBase 子类：调用 OnWindowClose 后销毁 GameObject 并从缓存移除。
        /// </summary>
        public void CloseWindow()
        {
            if (currentWindow == null) return;

            Debug.Log($"[UI] CloseWindow 关闭={currentWindow.GetType().Name}");
            CloseWindowInternal();
        }

        /// <summary>内部关闭窗口：调用 OnWindowClose → 销毁 GameObject → 清缓存</summary>
        private void CloseWindowInternal()
        {
            if (currentWindow == null) return;

            // WindowBase 子类调用 OnWindowClose 回调
            var windowBase = currentWindow as WindowBase;
            if (windowBase != null)
            {
                windowBase.InvokeWindowClose();
                string typeName = currentWindow.GetType().Name;
                panelCache.Remove(typeName);
                Object.Destroy(currentWindow.gameObject);
            }
            else
            {
                currentWindow.Hide();
            }
            currentWindow = null;
        }

        /// <summary>获取 HUD 层 Canvas（供外部添加永久 HUD 元素）</summary>
        public Canvas GetHUDCanvas() => canvasHUD;

        /// <summary>获取 Tip 层 Canvas（供外部添加浮动提示）</summary>
        public Canvas GetTipCanvas() => canvasTip;

        // ---- 旧 API（兼容保留） ----

        /// <summary>
        /// 显示面板到 HUD Canvas 并压栈管理
        /// </summary>
        [Obsolete("建议使用 ShowWindow<T>() 替代，旧接口保留兼容")]
        public T ShowPanel<T>() where T : UIPanel
        {
            string panelName = typeof(T).Name;
            Debug.Log($"[UI] ShowPanel<{panelName}> 栈深度={panelStack.Count}");
            // 隐藏栈顶
            if (panelStack.Count > 0)
            {
                var top = panelStack.Peek();
                top.Hide();
            }

            var panel = GetOrCreatePanel<T>(canvasHUD.transform);
            panel.Show();
            panelStack.Push(panel);
            return panel;
        }

        /// <summary>
        /// 返回上一级面板
        /// </summary>
        [Obsolete("建议使用 CloseWindow() 替代，旧接口保留兼容")]
        public void Back()
        {
            if (panelStack.Count == 0) return;

            var current = panelStack.Pop();
            Debug.Log($"[UI] Back 关闭={current.GetType().Name} 栈深度={panelStack.Count}");
            current.Hide();

            if (panelStack.Count > 0)
            {
                var prev = panelStack.Peek();
                prev.Show();
            }
        }

        /// <summary>
        /// 替换当前面板
        /// </summary>
        [Obsolete("建议使用 ShowWindow<T>() 替代，旧接口保留兼容")]
        public T ReplacePanel<T>() where T : UIPanel
        {
            string panelName = typeof(T).Name;
            if (panelStack.Count > 0)
            {
                var old = panelStack.Pop();
                Debug.Log($"[UI] ReplacePanel<{panelName}> 替换={old.GetType().Name}");
                old.Hide();
            }
            else
            {
                Debug.Log($"[UI] ReplacePanel<{panelName}> (空栈)");
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

            if (currentWindow != null)
            {
                CloseWindowInternal();
            }
        }

        private T GetOrCreatePanel<T>(Transform parent) where T : UIPanel
        {
            string typeName = typeof(T).Name;

            if (panelCache.TryGetValue(typeName, out var cached))
            {
                // 确保面板挂载到正确的 Canvas 下
                if (cached.transform.parent != parent)
                    cached.transform.SetParent(parent, false);
                return (T)cached;
            }

            // 从 Resources/UI/{TypeName} 加载预制体
            var prefab = PrefabLoader.LoadPanelPrefab(typeName);
            GameObject panelObj;
            if (prefab != null)
            {
                panelObj = PrefabLoader.Instantiate(prefab, parent, typeName);
            }
            else
            {
                // 无预制体时回退到 CreateVisuals
                panelObj = CreatePanelFromVisuals<T>(typeName, parent);
            }

            var panel = panelObj.GetOrAddComponent<T>();
            panelCache[typeName] = panel;
            return panel;
        }

        private GameObject CreatePanelFromVisuals<T>(string typeName, Transform parent) where T : UIPanel
        {
            var panelObj = new GameObject(typeName);
            panelObj.AddComponent<RectTransform>();
            panelObj.transform.SetParent(parent, false);
            var cvMethod = typeof(T).GetMethod("CreateVisuals",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (cvMethod != null)
                cvMethod.Invoke(null, new object[] { panelObj.transform });
            return panelObj;
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
