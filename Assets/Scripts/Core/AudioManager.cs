using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 音效分类枚举
    /// </summary>
    public enum AudioClipType
    {
        Battle_Attack,
        Battle_Skill,
        Battle_Hit,
        Battle_Victory,
        Battle_Defeat,
        Battle_Dodge,          // 闪避成功
        UI_Click,
        UI_PanelOpen,
        UI_PanelClose,
        Dungeon_Move,
        Dungeon_Discover,
        Dungeon_TreasureOpen,  // 宝箱打开
        Dungeon_HealWater,     // 泉水恢复
        Dungeon_MagicLearn,    // 神龛学习
        Battle_BossWarning,    // Boss战前警告
        Battle_BossBGM,        // Boss战专属BGM
        BGM_Peaceful,          // 城镇背景音乐
        BGM_Dungeon,           // 地牢背景音乐
        BGM_Battle             // 普通战斗背景音乐
    }

    /// <summary>
    /// 音频管理器单例（MonoBehaviour），管理音效/音乐播放。
    /// 音效使用程序化生成（正弦波/白噪声等基础波形），无需外部音频资源。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance => instance;

        // AudioSource 池：SFX 通道 x5（轮播避免重叠截断）+ Music 通道 x1
        private AudioSource[] sfxSources;
        private AudioSource musicSource;
        private int sfxIndex;

        // 音量属性（0-1）
        private float soundVolume = 1f;
        private float musicVolume = 1f;

        public float SoundVolume
        {
            get => soundVolume;
            set
            {
                soundVolume = Mathf.Clamp01(value);
                if (sfxSources != null)
                {
                    foreach (var src in sfxSources)
                        src.volume = soundVolume;
                }
            }
        }

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (musicSource != null)
                    musicSource.volume = musicVolume;
            }
        }

        // AudioClip 缓存字典
        private Dictionary<AudioClipType, AudioClip> clipCache = new Dictionary<AudioClipType, AudioClip>();

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 创建 AudioSource 池
            sfxSources = new AudioSource[5];
            for (int i = 0; i < 5; i++)
            {
                sfxSources[i] = gameObject.AddComponent<AudioSource>();
                sfxSources[i].playOnAwake = false;
                sfxSources[i].loop = false;
                sfxSources[i].volume = soundVolume;
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;

            // 程序化生成所有音效并缓存
            GenerateAllClips();

            // 从 SettingsManager 加载音量
            var sm = SettingsManager.Instance;
            sm.Load();
            SoundVolume = sm.SfxVolume;
            MusicVolume = sm.MusicVolume;

            Debug.Log("[AudioManager] 初始化完成 — SFX通道=5 Music通道=1 音效数=" + clipCache.Count);
        }

        /// <summary>
        /// 确保 AudioManager 已创建（外部调用入口）
        /// </summary>
        public static void EnsureCreated()
        {
            if (instance != null) return;
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }

        /// <summary>
        /// 播放指定类型的音效（轮播 AudioSource 池避免截断）
        /// </summary>
        public void PlaySFX(AudioClipType type)
        {
            if (sfxSources == null || !clipCache.TryGetValue(type, out var clip))
                return;
            var src = sfxSources[sfxIndex];
            src.clip = clip;
            src.Play();
            sfxIndex = (sfxIndex + 1) % sfxSources.Length;
        }

        /// <summary>
        /// 播放音乐（循环）
        /// </summary>
        public void PlayMusic(AudioClipType type)
        {
            if (musicSource == null || !clipCache.TryGetValue(type, out var clip))
                return;
            if (musicSource.clip == clip && musicSource.isPlaying)
                return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        /// <summary>
        /// 停止音乐
        /// </summary>
        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        /// <summary>
        /// 淡入淡出切换背景音乐
        /// </summary>
        /// <param name="type">目标音乐类型</param>
        /// <param name="duration">淡入淡出时长（秒）</param>
        public void CrossfadeMusic(AudioClipType type, float duration = 1.0f)
        {
            if (musicSource == null || !clipCache.TryGetValue(type, out var clip)) return;
            // 如果正在播放相同的音乐，跳过
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            StartCoroutine(CrossfadeCoroutine(clip, duration));
        }

        private System.Collections.IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration)
        {
            float halfDur = duration * 0.5f;

            // 淡出当前音乐
            if (musicSource.isPlaying)
            {
                float startVol = musicSource.volume;
                float elapsed = 0f;
                while (elapsed < halfDur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDur);
                    yield return null;
                }
                musicSource.Stop();
                musicSource.volume = 0f;
            }

            // 淡入新音乐
            musicSource.clip = newClip;
            musicSource.Play();
            float elapsed2 = 0f;
            while (elapsed2 < halfDur)
            {
                elapsed2 += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed2 / halfDur);
                yield return null;
            }
            musicSource.volume = musicVolume;
        }

        #region 程序化音效生成

        /// <summary>
        /// 程序化生成全部音效 AudioClip 并缓存到字典
        /// </summary>
        private void GenerateAllClips()
        {
            clipCache[AudioClipType.UI_Click] = GenerateTone(440f, 0.08f, 0.3f);
            clipCache[AudioClipType.UI_PanelOpen] = GenerateSweep(300f, 600f, 0.15f, 0.3f);
            clipCache[AudioClipType.UI_PanelClose] = GenerateSweep(600f, 300f, 0.12f, 0.25f);
            clipCache[AudioClipType.Battle_Attack] = GenerateNoiseBurst(0.15f, 0.5f);
            clipCache[AudioClipType.Battle_Skill] = GenerateSweep(400f, 800f, 0.3f, 0.4f);
            clipCache[AudioClipType.Battle_Hit] = GenerateNoiseBurst(0.08f, 0.6f);
            clipCache[AudioClipType.Battle_Victory] = GenerateArpeggio(new float[] { 523f, 659f, 784f, 1047f }, 0.15f, 0.5f);
            clipCache[AudioClipType.Battle_Defeat] = GenerateArpeggio(new float[] { 400f, 350f, 300f, 200f }, 0.2f, 0.4f);
            clipCache[AudioClipType.Dungeon_Move] = GenerateNoiseBurst(0.06f, 0.2f);
            clipCache[AudioClipType.Dungeon_Discover] = GenerateArpeggio(new float[] { 600f, 800f }, 0.12f, 0.35f);
            clipCache[AudioClipType.Battle_Dodge] = GenerateSweep(800f, 400f, 0.1f, 0.25f);
            clipCache[AudioClipType.Battle_BossWarning] = GenerateArpeggio(new float[] { 200f, 250f, 300f, 400f }, 0.25f, 0.6f);
            clipCache[AudioClipType.Battle_BossBGM] = GenerateBossBGM();
            clipCache[AudioClipType.Dungeon_TreasureOpen] = GenerateArpeggio(new float[] { 523f, 659f, 784f, 1047f, 1319f }, 0.08f, 0.35f);
            clipCache[AudioClipType.Dungeon_HealWater] = GenerateSweep(400f, 900f, 0.4f, 0.3f);
            clipCache[AudioClipType.Dungeon_MagicLearn] = GenerateArpeggio(new float[] { 440f, 554f, 659f, 880f }, 0.15f, 0.4f);
            clipCache[AudioClipType.BGM_Peaceful] = GeneratePeacefulBGM();
            clipCache[AudioClipType.BGM_Dungeon] = GenerateDungeonBGM();
            clipCache[AudioClipType.BGM_Battle] = GenerateBattleBGM();
        }

        /// <summary>
        /// 生成纯音（正弦波），带简单 ADSR 包络
        /// </summary>
        private AudioClip GenerateTone(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f;
                float attack = 0.01f;
                float release = 0.02f;
                if (t < attack) envelope = t / attack;
                else if (t > duration - release) envelope = (duration - t) / release;
                data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * volume * envelope;
            }
            var clip = AudioClip.Create($"Tone_{frequency}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 生成频率扫描音效（上升/下降滑音）
        /// </summary>
        private AudioClip GenerateSweep(float startFreq, float endFreq, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(startFreq, endFreq, t / duration);
                float envelope = Mathf.Sin(Mathf.PI * t / duration);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * volume * envelope;
            }
            var clip = AudioClip.Create($"Sweep_{startFreq}_{endFreq}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 生成白噪声爆炸音效
        /// </summary>
        private AudioClip GenerateNoiseBurst(float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f;
                float release = duration * 0.3f;
                if (t > duration - release) envelope = (duration - t) / release;
                data[i] = (Random.value * 2f - 1f) * volume * envelope * 0.5f;
            }
            var clip = AudioClip.Create($"Noise_{duration}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 生成琶音（连续多个音符的和弦效果）
        /// </summary>
        private AudioClip GenerateArpeggio(float[] frequencies, float noteDuration, float volume)
        {
            int sampleRate = 44100;
            int noteSamples = Mathf.CeilToInt(sampleRate * noteDuration);
            int totalSamples = noteSamples * frequencies.Length;
            var data = new float[totalSamples];
            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    int idx = n * noteSamples + i;
                    float t = (float)i / sampleRate;
                    float envelope = Mathf.Sin(Mathf.PI * (float)i / noteSamples);
                    data[idx] = Mathf.Sin(2 * Mathf.PI * frequencies[n] * t) * volume * envelope * 0.6f;
                }
            }
            var clip = AudioClip.Create("Arpeggio", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 生成Boss战BGM：低沉紧张的循环旋律
        /// </summary>
        private AudioClip GenerateBossBGM()
        {
            int sampleRate = 44100;
            float duration = 2.0f; // 2秒循环
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            // 低频基础音 + 中频旋律
            float[] bassNotes = { 110f, 110f, 130.81f, 110f, 98f, 98f, 110f, 130.81f };
            float[] melodyNotes = { 220f, 261.63f, 293.66f, 261.63f, 220f, 196f, 220f, 261.63f };
            int noteSamples = samples / bassNotes.Length;
            for (int n = 0; n < bassNotes.Length; n++)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    int idx = n * noteSamples + i;
                    if (idx >= samples) break;
                    float t = (float)i / sampleRate;
                    float envelope = Mathf.Sin(Mathf.PI * (float)i / noteSamples);
                    // 低音层
                    float bass = Mathf.Sin(2 * Mathf.PI * bassNotes[n] * t) * 0.3f * envelope;
                    // 旋律层（方波近似）
                    float melody = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * melodyNotes[n] * t)) * 0.15f * envelope;
                    data[idx] = (bass + melody) * 0.5f;
                }
            }
            var clip = AudioClip.Create("BossBGM", samples, 1, sampleRate, true);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 生成城镇背景音乐：轻柔舒缓的和弦循环
        /// </summary>
        private AudioClip GeneratePeacefulBGM()
        {
            int sampleRate = 44100;
            float duration = 4.0f; // 4秒循环
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            // C大调和弦进行: C-Am-F-G
            float[][] chordNotes = {
                new float[] { 261.63f, 329.63f, 392f },     // C大三和弦
                new float[] { 220f, 261.63f, 329.63f },     // Am小三和弦
                new float[] { 174.61f, 220f, 261.63f },     // F大三和弦
                new float[] { 196f, 246.94f, 293.66f }      // G大三和弦
            };
            int chordSamples = samples / chordNotes.Length;
            for (int c = 0; c < chordNotes.Length; c++)
            {
                for (int i = 0; i < chordSamples; i++)
                {
                    int idx = c * chordSamples + i;
                    if (idx >= samples) break;
                    float t = (float)i / sampleRate;
                    float envelope = Mathf.Sin(Mathf.PI * (float)i / chordSamples);
                    float val = 0f;
                    foreach (var freq in chordNotes[c])
                    {
                        val += Mathf.Sin(2 * Mathf.PI * freq * t) * 0.15f;
                    }
                    data[idx] = val * envelope * 0.4f;
                }
            }
            var clip = AudioClip.Create("PeacefulBGM", samples, 1, sampleRate, true);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 生成地牢背景音乐：低沉紧张的环境音循环
        /// </summary>
        private AudioClip GenerateDungeonBGM()
        {
            int sampleRate = 44100;
            float duration = 3.0f; // 3秒循环
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            // 低频无人机音 + 缓慢脉动
            float[] bassNotes = { 55f, 55f, 65.41f, 55f, 49f, 55f };
            int noteSamples = samples / bassNotes.Length;
            for (int n = 0; n < bassNotes.Length; n++)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    int idx = n * noteSamples + i;
                    if (idx >= samples) break;
                    float t = (float)i / sampleRate;
                    float envelope = Mathf.Sin(Mathf.PI * (float)i / noteSamples);
                    // 低频正弦波 + 轻微失谐的泛音
                    float bass = Mathf.Sin(2 * Mathf.PI * bassNotes[n] * t) * 0.25f;
                    float overtone = Mathf.Sin(2 * Mathf.PI * bassNotes[n] * 1.5f * t) * 0.08f;
                    // 低通滤波模拟：衰减高频
                    float noise = (Mathf.Sin(2 * Mathf.PI * 120f * t + Mathf.Sin(2 * Mathf.PI * 0.3f * t) * 2f) * 0.04f);
                    data[idx] = (bass + overtone + noise) * envelope;
                }
            }
            var clip = AudioClip.Create("DungeonBGM", samples, 1, sampleRate, true);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 生成普通战斗背景音乐：节奏紧张的战斗旋律
        /// </summary>
        private AudioClip GenerateBattleBGM()
        {
            int sampleRate = 44100;
            float duration = 3.0f; // 3秒循环
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            // 快节奏音符：小调进行
            float[] melodyNotes = { 196f, 220f, 233.08f, 220f, 196f, 174.61f, 196f, 233.08f };
            float[] bassLine = { 98f, 98f, 116.54f, 116.54f, 87.31f, 87.31f, 98f, 98f };
            int noteSamples = samples / melodyNotes.Length;
            for (int n = 0; n < melodyNotes.Length; n++)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    int idx = n * noteSamples + i;
                    if (idx >= samples) break;
                    float t = (float)i / sampleRate;
                    float envelope = Mathf.Sin(Mathf.PI * (float)i / noteSamples);
                    // 旋律层（方波近似，增加紧张感）
                    float melody = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * melodyNotes[n] * t)) * 0.1f * envelope;
                    // 低音层
                    float bass = Mathf.Sin(2 * Mathf.PI * bassLine[n] * t) * 0.2f * envelope;
                    // 打击层：每个音符开始时一个短促噪声
                    float perc = 0f;
                    if (i < sampleRate * 0.03f)
                        perc = (Random.value * 2f - 1f) * 0.08f * (1f - i / (sampleRate * 0.03f));
                    data[idx] = (melody + bass + perc) * 0.5f;
                }
            }
            var clip = AudioClip.Create("BattleBGM", samples, 1, sampleRate, true);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion
    }
}
