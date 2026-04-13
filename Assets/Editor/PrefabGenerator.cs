using Game.Core;
using Game.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor 工具：将代码构建的 UI 面板生成为预制体。
/// 使用方法：Unity 菜单 Tools > Generate UI Prefabs
/// 预制体保存到 Assets/Resources/UI/ 目录。
/// </summary>
public static class PrefabGenerator
{
    private const string OutputPath = "Assets/Resources/UI";

    [MenuItem("Tools/Generate UI Prefabs")]
    public static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");

        int count = 0;

        // 确保 UIManager 已初始化（Canvas 存在）
        UIManager.EnsureCreated();
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[PrefabGenerator] Canvas 不存在，无法生成预制体");
            return;
        }

        // 需要生成预制体的面板列表
        var panelTypes = new System.Type[]
        {
            typeof(SettingsPanel),
            // 后续面板在此添加
        };

        foreach (var type in panelTypes)
        {
            if (GeneratePrefab(type, canvas.transform))
                count++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[PrefabGenerator] 完成！生成 {count} 个预制体到 {OutputPath}/");
    }

    private static bool GeneratePrefab(System.Type panelType, Transform parent)
    {
        string typeName = panelType.Name;
        string prefabPath = $"{OutputPath}/{typeName}.prefab";

        // 创建临时 GameObject
        var tempObj = new GameObject(typeName);
        tempObj.transform.SetParent(parent, false);

        // 添加面板组件并触发 OnInit
        var panel = (UIPanel)tempObj.AddComponent(panelType);
        panel.InvokeInit();

        // 保存为预制体
        var prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, prefabPath);
        if (prefab != null)
        {
            Debug.Log($"[PrefabGenerator] 已生成: {prefabPath}");
        }
        else
        {
            Debug.LogError($"[PrefabGenerator] 生成失败: {prefabPath}");
        }

        // 清理临时对象
        Object.DestroyImmediate(tempObj);

        return prefab != null;
    }
}
