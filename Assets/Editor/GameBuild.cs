using System;
using System.IO;
using Game;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameBuild
{
    [MenuItem("方块与地下城/校验配置")]
    public static void ValidateConfig()
    {
        new GameConfig();
        Debug.Log("[构建] 全部配置与引用校验通过。");
    }

    [MenuItem("方块与地下城/构建 Windows")]
    public static void Windows()
    {
        ValidateConfig();
        PlayerSettings.companyName = "EmberTown";
        PlayerSettings.productName = "方块与地下城";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        AssetDatabase.ForceReserializeAssets(new[] { "Assets/Scenes/SampleScene.unity", "Assets/Settings/UniversalRP.asset", "Assets/Settings/Renderer2D.asset" });
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Builds/Windows");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = "Builds/Windows/EmberDungeon.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"[构建] Windows构建失败：{report.summary.result}");
        }
        Debug.Log($"[构建] Windows构建成功，{report.summary.totalSize}字节。");
    }
}
