using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 设置面板：音效/音乐滑块、全屏切换、分辨率选择、语言选择、返回按钮
    /// 数据持久化使用 PlayerPrefs（F092 将封装为 SettingsManager）
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        // --- 缓存引用 ---
        private Slider musicSlider;
        private Text musicValueText;
        private Slider sfxSlider;
        private Text sfxValueText;
        private Button fullscreenBtn;
        private Text fullscreenLabel;
        private Text resText;
        private Text langText;

        // --- 选项数据 ---
        private static readonly string[] Resolutions = { "1920x1080", "1600x900", "1280x720" };
        private static readonly string[] Languages = { "中文", "English" };
        private int resIndex;
        private int langIndex;

        // --- PlayerPrefs 键 ---
        private const string KeyMusic = "Settings_MusicVolume";
        private const string KeySfx = "Settings_SfxVolume";
        private const string KeyFullscreen = "Settings_Fullscreen";
        private const string KeyResIndex = "Settings_ResIndex";
        private const string KeyLangIndex = "Settings_LangIndex";

        public override void OnInit()
        {
            UIBuilder.CreateFullScreenBg(transform);
            UIBuilder.CreateTitleBar(transform, "设    置");

            float startY = 180f;
            float rowH = 65f;
            float labelX = -240f;
            float sliderX = 80f;
            float labelW = 160f;

            // ===== 音乐音量 =====
            CreateSettingLabel("音乐音量", labelX, startY, labelW);
            musicSlider = UIBuilder.CreateInteractiveSlider(transform, 300, 22, UIBuilder.Blue, "MusicSlider");
            UIBuilder.SetAnchored(musicSlider.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(300, 22));
            musicValueText = UIBuilder.CreateText(transform, "100", 18, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "MusicValue");
            UIBuilder.SetAnchored(musicValueText.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX + 195, startY), new Vector2(60, 30));
            musicSlider.onValueChanged.AddListener(OnMusicChanged);

            startY -= rowH;

            // ===== 音效音量 =====
            CreateSettingLabel("音效音量", labelX, startY, labelW);
            sfxSlider = UIBuilder.CreateInteractiveSlider(transform, 300, 22, UIBuilder.Green, "SfxSlider");
            UIBuilder.SetAnchored(sfxSlider.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(300, 22));
            sfxValueText = UIBuilder.CreateText(transform, "100", 18, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "SfxValue");
            UIBuilder.SetAnchored(sfxValueText.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX + 195, startY), new Vector2(60, 30));
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);

            startY -= rowH;

            // ===== 全屏模式 =====
            CreateSettingLabel("全屏模式", labelX, startY, labelW);
            var (fsBtn, fsLabel) = UIBuilder.CreateButtonWithRef(transform, "全屏", 160, 38, OnFullscreenToggle, 20, "BtnFullscreen");
            fullscreenBtn = fsBtn;
            fullscreenLabel = fsLabel;
            UIBuilder.SetAnchored(fsBtn.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(160, 38));

            startY -= rowH;

            // ===== 分辨率 =====
            CreateSettingLabel("分 辨 率", labelX, startY, labelW);
            var (btnResPrev, _) = UIBuilder.CreateButtonWithRef(transform, "<", 40, 38,
                () => CycleOption(-1, ref resIndex, Resolutions, resText, KeyResIndex), 20, "BtnResPrev");
            UIBuilder.SetAnchored(btnResPrev.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX - 90, startY), new Vector2(40, 38));
            resText = UIBuilder.CreateText(transform, "1920x1080", 20, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "ResText");
            UIBuilder.SetAnchored(resText.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(140, 30));
            var (btnResNext, _) = UIBuilder.CreateButtonWithRef(transform, ">", 40, 38,
                () => CycleOption(1, ref resIndex, Resolutions, resText, KeyResIndex), 20, "BtnResNext");
            UIBuilder.SetAnchored(btnResNext.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX + 90, startY), new Vector2(40, 38));

            startY -= rowH;

            // ===== 语言 =====
            CreateSettingLabel("语    言", labelX, startY, labelW);
            var (btnLangPrev, _) = UIBuilder.CreateButtonWithRef(transform, "<", 40, 38,
                () => CycleOption(-1, ref langIndex, Languages, langText, KeyLangIndex), 20, "BtnLangPrev");
            UIBuilder.SetAnchored(btnLangPrev.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX - 70, startY), new Vector2(40, 38));
            langText = UIBuilder.CreateText(transform, "中文", 20, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "LangText");
            UIBuilder.SetAnchored(langText.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(100, 30));
            var (btnLangNext, _) = UIBuilder.CreateButtonWithRef(transform, ">", 40, 38,
                () => CycleOption(1, ref langIndex, Languages, langText, KeyLangIndex), 20, "BtnLangNext");
            UIBuilder.SetAnchored(btnLangNext.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX + 70, startY), new Vector2(40, 38));

            // ===== 返回按钮 =====
            var (btnBack, _) = UIBuilder.CreateButtonWithRef(transform, "返    回", 200, 48,
                () =>
                {
                    PlayerPrefs.Save();
                    UIManager.Instance.Back();
                }, 24, "BtnBack");
            UIBuilder.SetAnchored(btnBack.gameObject,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 60), new Vector2(200, 48));
        }

        public override void OnShow()
        {
            LoadSettings();
        }

        // --- 加载设置 ---
        private void LoadSettings()
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);

            musicSlider.value = PlayerPrefs.GetFloat(KeyMusic, 1f);
            sfxSlider.value = PlayerPrefs.GetFloat(KeySfx, 1f);
            musicValueText.text = Mathf.RoundToInt(musicSlider.value * 100).ToString();
            sfxValueText.text = Mathf.RoundToInt(sfxSlider.value * 100).ToString();

            bool fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            fullscreenLabel.text = fullscreen ? "全屏" : "窗口";

            resIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyResIndex, 0), 0, Resolutions.Length - 1);
            resText.text = Resolutions[resIndex];

            langIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyLangIndex, 0), 0, Languages.Length - 1);
            langText.text = Languages[langIndex];

            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        // --- 回调 ---
        private void OnMusicChanged(float v)
        {
            musicValueText.text = Mathf.RoundToInt(v * 100).ToString();
            PlayerPrefs.SetFloat(KeyMusic, v);
        }

        private void OnSfxChanged(float v)
        {
            sfxValueText.text = Mathf.RoundToInt(v * 100).ToString();
            PlayerPrefs.SetFloat(KeySfx, v);
        }

        private void OnFullscreenToggle()
        {
            bool fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            fullscreen = !fullscreen;
            PlayerPrefs.SetInt(KeyFullscreen, fullscreen ? 1 : 0);
            fullscreenLabel.text = fullscreen ? "全屏" : "窗口";
            Screen.fullScreen = fullscreen;
        }

        private void CycleOption(int dir, ref int index, string[] options, Text display, string prefsKey)
        {
            index = (index + dir + options.Length) % options.Length;
            display.text = options[index];
            PlayerPrefs.SetInt(prefsKey, index);

            if (prefsKey == KeyResIndex)
                ApplyResolution();
        }

        private void ApplyResolution()
        {
            var parts = Resolutions[resIndex].Split('x');
            int w = int.Parse(parts[0]);
            int h = int.Parse(parts[1]);
            bool fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            Screen.SetResolution(w, h, fullscreen);
        }

        // --- 辅助 ---
        private void CreateSettingLabel(string text, float x, float y, float w)
        {
            var label = UIBuilder.CreateText(transform, text, 22, UIBuilder.TextWhite, TextAnchor.MiddleRight, false, text + "Label");
            UIBuilder.SetAnchored(label.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y), new Vector2(w, 40));
        }
    }
}
