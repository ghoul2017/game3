using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 预制体加载工具类，统一管理预制体的加载和缓存。
    /// 面板预制体路径：Resources/UI/{PanelClassName}
    /// 组件预制体路径：Resources/Prefabs/{path}
    /// </summary>
    public static class PrefabLoader
    {
        private static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();

        private const string PanelPathPrefix = "UI/";
        private const string WidgetPathPrefix = "Prefabs/";

        /// <summary>
        /// 加载面板预制体（从 Resources/UI/{panelTypeName}）
        /// </summary>
        public static GameObject LoadPanelPrefab(string panelTypeName)
        {
            return Load(PanelPathPrefix + panelTypeName);
        }

        /// <summary>
        /// 加载组件预制体（从 Resources/Prefabs/{widgetPath}）
        /// </summary>
        public static GameObject LoadWidget(string widgetPath)
        {
            return Load(WidgetPathPrefix + widgetPath);
        }

        /// <summary>
        /// 实例化预制体并挂载到父节点
        /// </summary>
        public static GameObject Instantiate(GameObject prefab, Transform parent, string name = null)
        {
            var go = Object.Instantiate(prefab, parent, false);
            go.name = string.IsNullOrEmpty(name) ? prefab.name : name;
            return go;
        }

        /// <summary>
        /// 加载并实例化面板预制体。如果预制体不存在返回 null。
        /// </summary>
        public static GameObject InstantiatePanel(string panelTypeName, Transform parent)
        {
            var prefab = LoadPanelPrefab(panelTypeName);
            if (prefab == null) return null;
            return Instantiate(prefab, parent, panelTypeName);
        }

        /// <summary>
        /// 加载并实例化组件预制体。如果预制体不存在返回 null。
        /// </summary>
        public static GameObject InstantiateWidget(string widgetPath, Transform parent, string name = null)
        {
            var prefab = LoadWidget(widgetPath);
            if (prefab == null) return null;
            return Instantiate(prefab, parent, name);
        }

        /// <summary>
        /// 清除缓存（场景切换或内存回收时调用）
        /// </summary>
        public static void ClearCache()
        {
            Cache.Clear();
        }

        private static GameObject Load(string resourcePath)
        {
            if (Cache.TryGetValue(resourcePath, out var cached))
                return cached;

            var prefab = Resources.Load<GameObject>(resourcePath);
            Cache[resourcePath] = prefab;

            if (prefab == null)
                Logger.LogW("PrefabLoader", $"预制体未找到: Resources/{resourcePath}");

            return prefab;
        }
    }
}
