---
name: 音频系统架构
description: AudioManager单例设计、AudioSource池、SFX触发规则、BGM Crossfade、场景音乐流转、初始化调用链
type: project
---

# 音频系统架构（F129 + F152）

## 1. MonoBehaviour 单例而非纯C#单例

**Why:** AudioSource 必须挂载在 GameObject 上，AudioClip.Create() / AudioSource.Play() 都是 Unity 引擎 API，纯 C# 类无法调用。因此 AudioManager 采用 MonoBehaviour + DontDestroyOnLoad 模式。

**How to apply:** 需要 AddComponent、Coroutine、AudioSource 等 Unity 引擎功能的 Manager 才用 MonoBehaviour 单例；纯数据/逻辑管理的 Manager 用普通 C# 单例。

## 2. AudioSource 轮播池（5通道）

**Why:** 动态创建/销毁 AudioSource 会触发 GC 和频繁 AddComponent/Destroy。预分配 5 个 AudioSource 组成轮播池（Round-Robin），每次 PlaySFX() 取下一个通道播放。通道数 5 可在连续快速点击时，前一个短音效（如 UI_Click 0.08s）尚未结束时不被截断。

**How to apply:** 如需增加音效通道，修改 sfxSources 数组大小即可。轮播逻辑 sfxIndex = (sfxIndex + 1) % sfxSources.Length 无需额外修改。

## 3. 音效触发时序规则

**Why:** 技能音效（Battle_Skill）必须放在被动技能检查和法力检查之后。被动技能无需音效，法力不足时技能未实际释放不应有音效反馈。

**How to apply:** 在 BattleManager 中，PlaySFX 调用应紧邻实际技能生效代码之前：被动判断 → 法力判断 → 播放音效 → 执行技能效果。

## 4. 初始化空引用防护

**Why:** SettingsPanel 的滑块回调可能在 AudioManager 尚未初始化时触发（Unity 序列化字段默认值回写触发 OnValueChanged）。滑块回调中需加 AudioManager.Instance null 检查。

**How to apply:** 任何在 Awake()/Start() 阶段可能被触发的 AudioManager 引用都需加空检查。

## 5. 音效调用链

GameEntry.Awake → AudioManager.EnsureCreated → Awake(生成clip+缓存) → 外部调用 PlaySFX(type) → 从字典取clip → 轮播AudioSource池播放

程序化音效类型：Click(正弦波) / PanelOpen(上升扫频) / PanelClose(下降扫频) / Attack(白噪声) / Skill(上升扫频) / Hit(白噪声) / Victory(上行琶音) / Defeat(下行琶音) / Move(白噪声) / Discover(琶音) / Dodge(下降扫频) / BossWarning(上行琶音) / TreasureOpen(上行琶音) / HealWater(上升扫频) / MagicLearn(琶音)

## 6. BGM 系统与 Crossfade（F152）

**Why:** 游戏有三种场景 BGM（城镇/地牢/战斗），需要平滑切换。CrossfadeMusic 使用半时淡出+半时淡入策略（各占 duration 的 50%），通过 `Time.unscaledDeltaTime` 确保不受 `Time.timeScale`（暂停/慢动作）影响。

**How to apply:** 切换 BGM 统一调用 `CrossfadeMusic(AudioClipType, duration)`，不要直接用 `PlayMusic()`（PlayMusic 无淡入淡出，仅用于首次启动场景）。`StopMusic()` 用于战斗结束等需要立即静音的场景。

**陷阱 — 同曲跳过检测：** CrossfadeMusic 内置 `clip == current && isPlaying` 判断，避免重复触发淡入淡出导致音量异常。PlayMusic 也有相同的同曲保护。

## 7. 场景 BGM 流转路径（F152）

```
城镇 → DungeonPanel.OnShow() → CrossfadeMusic(BGM_Dungeon)
地牢 → 进入战斗 → BattleManager.StartBattle() → CrossfadeMusic(BGM_Battle 或 BossBGM)
战斗结束 → OnVictory/OnDefeat → StopMusic()
返回地牢 → DungeonPanel.OnShow() → CrossfadeMusic(BGM_Dungeon)
返回城镇 → TownPanel.OnShow() → CrossfadeMusic(BGM_Peaceful)
```

**How to apply:** BGM 切换入口分散在各面板的 OnShow() 和 BattleManager 的生命周期中。新增场景/面板需要 BGM 时，在 OnShow() 中调用 CrossfadeMusic，无需关心其他面板的 BGM 状态。

## 8. 音乐通道设计

- SFX 通道：5 个 AudioSource，轮播播放短音效，loop=false
- Music 通道：1 个 AudioSource，循环播放 BGM，loop=true
- 音量分离：SoundVolume 控制 SFX 通道，MusicVolume 控制 Music 通道，SettingsPanel 滑块双向绑定
