using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class SettingsPanel : UIPanel
    {
        // --- 缓存引用 ---
        private Slider musicSlider;
        private Text musicValueText;
        private Slider sfxSlider;
        private Text sfxValueText;
        private Text fullscreenLabel;
        private Text resText;
        private Text langText;

        private int resIndex;
        private int langIndex;

        /// <summary>仅供 PrefabGenerator 调用</summary>
        public static void CreateVisuals(Transform parent)
        {
            UIBuilder.CreateFullScreenBg(parent);
            UIBuilder.CreateTitleBar(parent, "设    置");

            float startY = 180f;
            float rowH = 65f;
            float labelX = -240f;
            float sliderX = 80f;
            float labelW = 160f;

            // ===== 音乐音量 =====
            CreateVisualLabel(parent, "音乐音量", labelX, startY, labelW);
            var ms = UIBuilder.CreateInteractiveSlider(parent, 300, 22, UIBuilder.Blue, "MusicSlider");
            UIBuilder.SetAnchored(ms.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(300, 22));
            var mv = UIBuilder.CreateText(parent, "100", 18, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "MusicValue");
            UIBuilder.SetAnchored(mv.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX + 195, startY), new Vector2(60, 30));

            startY -= rowH;

            // ===== 音效音量 =====
            CreateVisualLabel(parent, "音效音量", labelX, startY, labelW);
            var ss = UIBuilder.CreateInteractiveSlider(parent, 300, 22, UIBuilder.Green, "SfxSlider");
            UIBuilder.SetAnchored(ss.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(300, 22));
            var sv = UIBuilder.CreateText(parent, "100", 18, UIBuilder.TextDim, TextAnchor.MiddleCenter, false, "SfxValue");
            UIBuilder.SetAnchored(sv.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX + 195, startY), new Vector2(60, 30));

            startY -= rowH;

            // ===== 全屏模式 =====
            CreateVisualLabel(parent, "全屏模式", labelX, startY, labelW);
            var (fsBtn, fsLbl) = UIBuilder.CreateButtonWithRef(parent, "全屏", 160, 38, null, 20, "BtnFullscreen");
            UIBuilder.SetAnchored(fsBtn.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(160, 38));

            startY -= rowH;

            // ===== 分辨率 =====
            CreateVisualLabel(parent, "分 辨 率", labelX, startY, labelW);
            var (btnResPrev, _) = UIBuilder.CreateButtonWithRef(parent, "<", 40, 38, null, 20, "BtnResPrev");
            UIBuilder.SetAnchored(btnResPrev.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX - 90, startY), new Vector2(40, 38));
            var rt = UIBuilder.CreateText(parent, "1920x1080", 20, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "ResText");
            UIBuilder.SetAnchored(rt.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(140, 30));
            var (btnResNext, _) = UIBuilder.CreateButtonWithRef(parent, ">", 40, 38, null, 20, "BtnResNext");
            UIBuilder.SetAnchored(btnResNext.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX + 90, startY), new Vector2(40, 38));

            startY -= rowH;

            // ===== 语言 =====
            CreateVisualLabel(parent, "语    言", labelX, startY, labelW);
            var (btnLangPrev, _) = UIBuilder.CreateButtonWithRef(parent, "<", 40, 38, null, 20, "BtnLangPrev");
            UIBuilder.SetAnchored(btnLangPrev.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX - 70, startY), new Vector2(40, 38));
            var lt = UIBuilder.CreateText(parent, "中文", 20, UIBuilder.TextWhite, TextAnchor.MiddleCenter, false, "LangText");
            UIBuilder.SetAnchored(lt.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX, startY), new Vector2(100, 30));
            var (btnLangNext, _) = UIBuilder.CreateButtonWithRef(parent, ">", 40, 38, null, 20, "BtnLangNext");
            UIBuilder.SetAnchored(btnLangNext.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(sliderX + 70, startY), new Vector2(40, 38));

            // ===== 返回按钮 =====
            var (btnBack, _) = UIBuilder.CreateButtonWithRef(parent, "返    回", 200, 48, null, 24, "BtnBack");
            UIBuilder.SetAnchored(btnBack.gameObject,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 60), new Vector2(200, 48));
        }

        private static void CreateVisualLabel(Transform parent, string text, float x, float y, float w)
        {
            var label = UIBuilder.CreateText(parent, text, 22, UIBuilder.TextWhite, TextAnchor.MiddleRight, false, text + "Label");
            UIBuilder.SetAnchored(label.gameObject,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, y), new Vector2(w, 40));
        }

        protected override void BindReferences()
        {
            musicSlider = FindComponent<Slider>("MusicSlider");
            musicValueText = FindComponent<Text>("MusicValue");
            sfxSlider = FindComponent<Slider>("SfxSlider");
            sfxValueText = FindComponent<Text>("SfxValue");
            fullscreenLabel = FindComponent<Text>("BtnFullscreen/Label");
            resText = FindComponent<Text>("ResText");
            langText = FindComponent<Text>("LangText");
        }

        protected override void BindEvents()
        {
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            FindComponent<Button>("BtnFullscreen").onClick.AddListener(OnFullscreenToggle);
            FindComponent<Button>("BtnResPrev").onClick.AddListener(() => CycleResolution(-1));
            FindComponent<Button>("BtnResNext").onClick.AddListener(() => CycleResolution(1));
            FindComponent<Button>("BtnLangPrev").onClick.AddListener(() => CycleLanguage(-1));
            FindComponent<Button>("BtnLangNext").onClick.AddListener(() => CycleLanguage(1));
            FindComponent<Button>("BtnBack").onClick.AddListener(() =>
            {
                SettingsManager.Instance.Save();
                UIManager.Instance.Back();
            });
        }

        public override void OnShow()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var sm = SettingsManager.Instance;
            sm.Load();

            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);

            musicSlider.value = sm.MusicVolume;
            sfxSlider.value = sm.SfxVolume;
            musicValueText.text = Mathf.RoundToInt(sm.MusicVolume * 100).ToString();
            sfxValueText.text = Mathf.RoundToInt(sm.SfxVolume * 100).ToString();

            fullscreenLabel.text = sm.Fullscreen ? "全屏" : "窗口";

            resIndex = sm.ResIndex;
            resText.text = SettingsManager.Resolutions[resIndex];

            langIndex = sm.LangIndex;
            langText.text = SettingsManager.Languages[langIndex];

            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        // --- 回调 ---
        private void OnMusicChanged(float v)
        {
            musicValueText.text = Mathf.RoundToInt(v * 100).ToString();
            SettingsManager.Instance.MusicVolume = v;
        }

        private void OnSfxChanged(float v)
        {
            sfxValueText.text = Mathf.RoundToInt(v * 100).ToString();
            SettingsManager.Instance.SfxVolume = v;
        }

        private void OnFullscreenToggle()
        {
            var sm = SettingsManager.Instance;
            sm.Fullscreen = !sm.Fullscreen;
            fullscreenLabel.text = sm.Fullscreen ? "全屏" : "窗口";
            sm.ApplyFullscreen();
        }

        private void CycleResolution(int dir)
        {
            var sm = SettingsManager.Instance;
            resIndex = (resIndex + dir + SettingsManager.Resolutions.Length) % SettingsManager.Resolutions.Length;
            sm.ResIndex = resIndex;
            resText.text = SettingsManager.Resolutions[resIndex];
            sm.ApplyResolution();
        }

        private void CycleLanguage(int dir)
        {
            var sm = SettingsManager.Instance;
            langIndex = (langIndex + dir + SettingsManager.Languages.Length) % SettingsManager.Languages.Length;
            sm.LangIndex = langIndex;
            langText.text = SettingsManager.Languages[langIndex];
        }
    }
}
