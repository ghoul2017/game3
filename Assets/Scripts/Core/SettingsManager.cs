using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 设置管理器单例，统一管理用户偏好设置的读写和运行时应用。
    /// 数据持久化使用 PlayerPrefs。
    /// </summary>
    public class SettingsManager
    {
        private static SettingsManager instance;
        public static SettingsManager Instance => instance ??= new SettingsManager();

        // --- 设置数据 ---
        public float MusicVolume { get; set; } = 1f;
        public float SfxVolume { get; set; } = 1f;
        public bool Fullscreen { get; set; } = true;
        public int ResIndex { get; set; }
        public int LangIndex { get; set; }

        // --- 选项列表 ---
        public static readonly string[] Resolutions = { "1920x1080", "1600x900", "1280x720" };
        public static readonly string[] Languages = { "中文", "English" };

        // --- PlayerPrefs 键 ---
        private const string KeyMusic = "Settings_MusicVolume";
        private const string KeySfx = "Settings_SfxVolume";
        private const string KeyFullscreen = "Settings_Fullscreen";
        private const string KeyResIndex = "Settings_ResIndex";
        private const string KeyLangIndex = "Settings_LangIndex";

        /// <summary>从 PlayerPrefs 加载设置</summary>
        public void Load()
        {
            MusicVolume = PlayerPrefs.GetFloat(KeyMusic, 1f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfx, 1f);
            Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            ResIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyResIndex, 0), 0, Resolutions.Length - 1);
            LangIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyLangIndex, 0), 0, Languages.Length - 1);
        }

        /// <summary>保存当前设置到 PlayerPrefs</summary>
        public void Save()
        {
            PlayerPrefs.SetFloat(KeyMusic, MusicVolume);
            PlayerPrefs.SetFloat(KeySfx, SfxVolume);
            PlayerPrefs.SetInt(KeyFullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(KeyResIndex, ResIndex);
            PlayerPrefs.SetInt(KeyLangIndex, LangIndex);
            PlayerPrefs.Save();
        }

        /// <summary>应用全屏设置到 Screen</summary>
        public void ApplyFullscreen()
        {
            Screen.fullScreen = Fullscreen;
        }

        /// <summary>应用分辨率设置到 Screen</summary>
        public void ApplyResolution()
        {
            var parts = Resolutions[ResIndex].Split('x');
            int w = int.Parse(parts[0]);
            int h = int.Parse(parts[1]);
            Screen.SetResolution(w, h, Fullscreen);
        }

        /// <summary>加载并应用全部设置</summary>
        public void LoadAndApply()
        {
            Load();
            ApplyFullscreen();
            ApplyResolution();
        }
    }
}
