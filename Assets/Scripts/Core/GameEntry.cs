using System;
using System.IO;
using Game.UI;
using UnityEngine;

namespace Game.Core
{
    public sealed class GameEntry : MonoBehaviour
    {
        private string startupError;

        private void Start()
        {
            try
            {
                Application.targetFrameRate = 60;
                AudioManager.EnsureCreated();
                SettingsManager.Instance.ApplyAudio();
                SettingsManager.Instance.ApplyDisplay();
                GameConfig config = new GameConfig();
                SaveStore store = new SaveStore(Path.Combine(Application.persistentDataPath, "adventure-v2.json"));
                AdventureSession session = new AdventureSession(config, store);
                gameObject.AddComponent<AdventureView>().Initialize(session);
                Debug.Log("[启动] 方块与地下城界面初始化完成。");
            }
            catch (Exception exception)
            {
                startupError = $"游戏无法启动：{exception.Message}";
                Debug.LogError($"[启动] {exception}");
            }
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(startupError))
            {
                GUIStyle style = new GUIStyle(GUI.skin.label) { font = SlateUI.Font, fontSize = 24, wordWrap = true };
                GUI.Label(new Rect(40, 40, Screen.width - 80, Screen.height - 80), startupError, style);
            }
        }
    }
}
