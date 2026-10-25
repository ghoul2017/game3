using UnityEngine;

namespace Game.Core
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance
        {
            get; private set;
        }
        private AudioSource music;
        private AudioSource effects;
        private AudioClip click;

        public float SoundVolume
        {
            get => effects.volume;
            set => effects.volume = Mathf.Clamp01(value) * 0.3f;
        }

        public float MusicVolume
        {
            get => music.volume;
            set => music.volume = Mathf.Clamp01(value) * 0.22f;
        }

        public static void EnsureCreated()
        {
            if (Instance == null)
            {
                new GameObject("声音").AddComponent<AudioManager>();
            }
        }

        private void Awake()
        {
            Instance = this;
            music = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            effects.playOnAwake = false;
            music.loop = true;
            music.clip = CreateAmbience();
            click = CreateClick();
            music.Play();
        }

        public void Click()
        {
            effects.PlayOneShot(click);
        }

        private static AudioClip CreateClick()
        {
            const int rate = 22050;
            float[] samples = new float[rate / 15];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)rate;
                float envelope = Mathf.Exp(-time * 75f) * Mathf.Min(1, time * 900);
                samples[i] = Mathf.Sin(time * 2 * Mathf.PI * 660) * envelope * 0.4f;
            }
            AudioClip clip = AudioClip.Create("石板轻触", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateAmbience()
        {
            const int rate = 22050;
            float[] samples = new float[rate * 16];
            float[] notes = { 130.8128f, 155.5635f, 195.9977f, 174.6141f };
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)rate;
                float amplitude = Mathf.Pow(Mathf.Sin(time / 16 * Mathf.PI), 2);
                float note = notes[Mathf.Min(3, (int)(time / 4))];
                float local = time % 4;
                float bell = Mathf.Sin(time * note * 2 * Mathf.PI) * Mathf.Sin(local / 4 * Mathf.PI) * 0.12f;
                float pad = Mathf.Sin(time * 65.4064f * 2 * Mathf.PI) * 0.07f;
                samples[i] = (bell + pad) * amplitude;
            }
            AudioClip clip = AudioClip.Create("余烬低语", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (music != null && music.clip != null)
            {
                Destroy(music.clip);
            }
            if (click != null)
            {
                Destroy(click);
            }
        }
    }
}
