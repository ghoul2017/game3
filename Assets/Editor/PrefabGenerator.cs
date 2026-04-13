using Game.Core;
using Game.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor 工具：将代码构建的 UI 面板生成为预制体。
/// 使用方法：Unity 菜单 Tools > Generate UI Prefabs
/// 预制体保存到 Assets/Resources/UI/ 目录。
/// 面板通过 CreateVisuals 静态方法提供视觉层级。
/// </summary>
public static class PrefabGenerator
{
    private const string OutputPath = "Assets/Resources/UI";

    /// <summary>面板的 CreateVisuals 委托</summary>
    private static readonly System.Type[] PanelTypes =
    {
        typeof(MainPanel),
        typeof(CreateRolePanel),
        typeof(TownPanel),
        typeof(DungeonPanel),
        typeof(BattlePanel),
        typeof(BagPanel),
        typeof(RoleInfoPanel),
        typeof(ShopPanel),
        typeof(SkillLearnPanel),
        typeof(SettingsPanel)
    };

    [MenuItem("Tools/Generate UI Prefabs")]
    public static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");

        int count = 0;
        foreach (var type in PanelTypes)
        {
            if (GeneratePrefab(type))
                count++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[PrefabGenerator] 完成！生成 {count} 个预制体到 {OutputPath}/");
    }

    private static bool GeneratePrefab(System.Type panelType)
    {
        string typeName = panelType.Name;
        string prefabPath = $"{OutputPath}/{typeName}.prefab";

        // 创建临时 GameObject（添加 RectTransform 以兼容 Canvas）
        var tempObj = new GameObject(typeName);
        tempObj.AddComponent<RectTransform>();

        // 调用面板的 CreateVisuals 静态方法
        var method = panelType.GetMethod("CreateVisuals",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method != null)
        {
            method.Invoke(null, new object[] { tempObj.transform });
        }
        else
        {
            // 回退：添加组件并触发 OnInit（旧模式面板）
            UIManager.EnsureCreated();
            tempObj.transform.SetParent(Object.FindObjectOfType<Canvas>().transform, false);
            var panel = (UIPanel)tempObj.AddComponent(panelType);
            panel.InvokeInit();
        }

        // 保存为预制体
        var prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, prefabPath);
        if (prefab != null)
            Debug.Log($"[PrefabGenerator] 已生成: {prefabPath}");
        else
            Debug.LogError($"[PrefabGenerator] 生成失败: {prefabPath}");

        Object.DestroyImmediate(tempObj);
        return prefab != null;
    }
}
