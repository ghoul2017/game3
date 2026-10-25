using UnityEngine;

namespace Game.Core
{
    public sealed class SettingsManager
    {
        public static SettingsManager Instance { get; } = new SettingsManager();
        public float MusicVolume
        {
            get; set;
        }
        public float SfxVolume
        {
            get; set;
        }

        private SettingsManager()
        {
            MusicVolume = PlayerPrefs.GetFloat("Ember.Music", 0.45f);
            SfxVolume = PlayerPrefs.GetFloat("Ember.Sfx", 0.65f);
        }

        public void ApplyAudio()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.MusicVolume = MusicVolume;
                AudioManager.Instance.SoundVolume = SfxVolume;
            }
        }

        public void Save()
        {
            PlayerPrefs.SetFloat("Ember.Music", MusicVolume);
            PlayerPrefs.SetFloat("Ember.Sfx", SfxVolume);
            PlayerPrefs.Save();
        }

        public void SetDisplay(int width, int height, bool fullscreen)
        {
            Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            PlayerPrefs.SetInt("Ember.Width", width);
            PlayerPrefs.SetInt("Ember.Height", height);
            PlayerPrefs.SetInt("Ember.Fullscreen", fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ApplyDisplay()
        {
            Screen.SetResolution(PlayerPrefs.GetInt("Ember.Width", 1600), PlayerPrefs.GetInt("Ember.Height", 900), PlayerPrefs.GetInt("Ember.Fullscreen", 0) == 1 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }
}
