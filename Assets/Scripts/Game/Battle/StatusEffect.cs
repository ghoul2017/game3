using Game.Config;

namespace Game.Battle
{
    /// <summary>
    /// 状态效果类型枚举
    /// </summary>
    public enum EffectType
    {
        Bleed,      // 流血：每回合损失HP
        Poison,     // 中毒：每回合损失HP
        Burn,       // 燃烧：每回合损失HP（基于施放者攻击力）
        Stun,       // 眩晕：跳过行动回合
        Silence,    // 沉默：无法使用技能
        Vulnerable, // 易伤：受到伤害增加
        BuffATK,    // 攻击增益
        BuffDEF,    // 防御增益
        BuffSpeed   // 速度增益
    }

    /// <summary>
    /// 状态效果数据类，表示施加在战斗单位上的持续效果
    /// </summary>
    [System.Serializable]
    public class StatusEffect
    {
        /// <summary>效果类型</summary>
        public EffectType type;

        /// <summary>效果数值（DoT为每回合伤害，Buff为加成值，Vulnerable为伤害增幅百分比）</summary>
        public int value;

        /// <summary>剩余持续回合</summary>
        public int duration;

        /// <summary>来源技能ID</summary>
        public int sourceId;

        public StatusEffect(EffectType type, int value, int duration, int sourceId = 0)
        {
            this.type = type;
            this.value = value;
            this.duration = duration;
            this.sourceId = sourceId;
        }

        /// <summary>获取效果类型的中文名称</summary>
        public string GetName()
        {
            return type switch
            {
                EffectType.Bleed => "流血",
                EffectType.Poison => "中毒",
                EffectType.Burn => "燃烧",
                EffectType.Stun => "眩晕",
                EffectType.Silence => "沉默",
                EffectType.Vulnerable => "易伤",
                EffectType.BuffATK => "攻击增益",
                EffectType.BuffDEF => "防御增益",
                EffectType.BuffSpeed => "速度增益",
                _ => type.ToString()
            };
        }
    }
}
