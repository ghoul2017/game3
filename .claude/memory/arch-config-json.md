---
name: ConfigManager JSON配置架构
description: F113 配置外置到Resources/Config/*.json，JsonUtility限制与fallback机制
type: project
---

# ConfigManager JSON 配置架构（F113）

**Why:** 配置从硬编码迁移到JSON外部文件，但保留了完整的硬编码fallback，理解这个双层机制对后续配置修改很重要。
**How to apply:** 修改配置时改JSON文件即可；如果JSON加载失败会自动降级到硬编码（LogW警告）。新增配置类型需要同时加包装类和fallback。

## 核心架构

### JSON 文件位置
`Assets/Resources/Config/` 下 6 个 JSON 文件：
- professions.json / skills.json / items.json / monsters.json / treasures.json / buffs.json

### 加载方式
```csharp
Resources.Load<TextAsset>("Config/xxx")  // 不带 .json 扩展名
JsonUtility.FromJson<T>(asset.text)
```

### JsonUtility 限制（重要坑点）
1. **不支持顶层数组序列化** — 必须用包装类：
   ```csharp
   [System.Serializable] public class XxxConfigList { public List<Xxx> list; }
   ```
2. **枚举序列化为整数值**（不是字符串名）— JSON中 `EffectType.Bleed` 写为 `"statusEffect": 0`

### Fallback 机制
每个 InitXxx 方法都是 JSON 优先 + 硬编码兜底：
```
LoadJson<T>() 成功 → 用JSON数据
LoadJson<T>() 失败 → LogW警告 + 使用方法内硬编码字典
```
硬编码 fallback 与 JSON 数据完全同步，是配置的"安全网"。

### 热更新支持
- `ReloadConfig()` 公共方法，内部调用 `Init()` 重新加载全部配置
- 所有公共 API（GetSkill/GetMonster/GetItem等）保持不变，调用方无需修改

### statusEffect=0 语义问题
- JSON中 `statusEffect` 缺省值为 0，对应 `EffectType.Bleed`
- 对于无状态效果的技能（如普通攻击），语义不精确但运行时安全
- 原因：BattleManager 检查 `statusDuration > 0` 才会触发状态效果，duration=0 时跳过
