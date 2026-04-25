using Game.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 战斗日志颜色标记工具类 — 从 BattlePanel.ColorizeLog 提取
    /// 规则：伤害/DoT→红色，治疗/复活→绿色，状态施加/消失→橙色，闪避/增益→蓝色，其他→白色
    /// </summary>
    public static class BattleLogFormatter
    {
        /// <summary>将 Color 转换为富文本用的 hex 字符串 "#rrggbb"</summary>
        public static string ColorToHex(Color c)
        {
            int r = Mathf.RoundToInt(c.r * 255f);
            int g = Mathf.RoundToInt(c.g * 255f);
            int b = Mathf.RoundToInt(c.b * 255f);
            return $"#{r:x2}{g:x2}{b:x2}";
        }

        /// <summary>
        /// 将战斗日志文本标记为富文本颜色。
        /// 规则：伤害→红色，治疗→绿色，状态施加→橙色，闪避→蓝色，普通→白色
        /// </summary>
        public static string ColorizeLog(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            // 闪避（优先级高）
            if (raw.Contains("闪避"))
                return $"<color={ColorToHex(UIColors.Blue)}>{raw}</color>";

            // 伤害类
            if (raw.Contains("造成") && raw.Contains("伤害"))
                return $"<color={ColorToHex(UIColors.Red)}>{raw}</color>";

            // DoT伤害（流血/中毒/燃烧损失HP）
            if (raw.Contains("损失") && raw.Contains("HP"))
                return $"<color={ColorToHex(UIColors.Red)}>{raw}</color>";

            // 治疗类
            if (raw.Contains("恢复") && (raw.Contains("生命") || raw.Contains("法力") || raw.Contains("HP")))
                return $"<color={ColorToHex(UIColors.Green)}>{raw}</color>";

            // 状态施加/效果
            if (raw.Contains("受到") && raw.Contains("效果"))
                return $"<color={ColorToHex(UIColors.Orange)}>{raw}</color>";
            if (raw.Contains("效果消失"))
                return $"<color={ColorToHex(UIColors.Orange)}>{raw}</color>";
            if (raw.Contains("眩晕") || raw.Contains("沉默") || raw.Contains("无法行动"))
                return $"<color={ColorToHex(UIColors.Orange)}>{raw}</color>";

            // 增益/Buff
            if (raw.Contains("增益") || raw.Contains("攻击+") || raw.Contains("防御+"))
                return $"<color={ColorToHex(UIColors.Blue)}>{raw}</color>";

            // 速度判定
            if (raw.Contains("[速度]"))
                return $"<color={ColorToHex(UIColors.TextWhite)}>{raw}</color>";

            // 复活
            if (raw.Contains("复活"))
                return $"<color={ColorToHex(UIColors.Green)}>{raw}</color>";

            // 默认：普通信息白色
            return $"<color={ColorToHex(UIColors.TextWhite)}>{raw}</color>";
        }
    }
}
