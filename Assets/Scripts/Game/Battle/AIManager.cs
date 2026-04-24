using System.Collections.Generic;
using Game.Config;
using Game.Data;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 怪物AI行动类型
    /// </summary>
    public enum AIMonsterAction
    {
        NormalAttack,  // 普通攻击
        UseSkill,      // 使用技能
    }

    /// <summary>
    /// 怪物AI决策结果
    /// </summary>
    public class AIDecision
    {
        public AIMonsterAction action;
        public int skillId;          // 使用技能时有效
        public float damageBonus;    // 额外伤害倍率（Boss低血量时+20%）
        public string hint;          // AI提示文本（可选战斗日志显示）

        public AIDecision(AIMonsterAction action, int skillId = 0, float damageBonus = 1f, string hint = "")
        {
            this.action = action;
            this.skillId = skillId;
            this.damageBonus = damageBonus;
            this.hint = hint;
        }
    }

    /// <summary>
    /// 怪物AI管理器：根据怪物行为模式决策行动
    /// </summary>
    public class AIManager
    {
        private static AIManager instance;
        public static AIManager Instance => instance ??= new AIManager();

        /// <summary>
        /// 根据怪物AI模式和当前战斗状态决策行动
        /// </summary>
        /// <param name="monster">当前怪物</param>
        /// <param name="battle">战斗数据</param>
        /// <param name="hasMonsterSkillMp">怪物是否有足够MP使用技能（由外部判定）</param>
        /// <returns>AI决策结果</returns>
        public AIDecision DecideAction(BattleMonster monster, BattleData battle, bool hasMonsterSkillMp = true)
        {
            if (monster == null || battle == null)
                return new AIDecision(AIMonsterAction.NormalAttack);

            // 无技能时只能普攻
            if (monster.skillIds == null || monster.skillIds.Count == 0)
                return new AIDecision(AIMonsterAction.NormalAttack);

            // MP不足时排除技能选项
            if (!hasMonsterSkillMp)
                return new AIDecision(AIMonsterAction.NormalAttack);

            float hpRatio = monster.maxHp > 0 ? (float)monster.hp / monster.maxHp : 1f;

            return monster.aiPattern switch
            {
                MonsterAIPattern.Aggressive => DecideAggressive(monster, battle),
                MonsterAIPattern.Defensive  => DecideDefensive(monster, battle, hpRatio),
                MonsterAIPattern.Debuffer   => DecideDebuffer(monster, battle),
                MonsterAIPattern.Boss       => DecideBoss(monster, battle, hpRatio),
                _                           => DecideBalanced(monster, battle),
            };
        }

        // ==================== Aggressive：偏好高伤技能 ====================

        /// <summary>
        /// 狂暴模式：MP>0且高伤技能可用时70%用技能/30%普攻
        /// </summary>
        private AIDecision DecideAggressive(BattleMonster monster, BattleData battle)
        {
            var bestSkill = FindHighestDamageSkill(monster, battle);
            if (bestSkill != null)
            {
                if (Random.value < 0.7f)
                    return new AIDecision(AIMonsterAction.UseSkill, bestSkill.id);
            }
            return new AIDecision(AIMonsterAction.NormalAttack);
        }

        // ==================== Defensive：HP低时优先治疗/防御 ====================

        /// <summary>
        /// 防御模式：HP&lt;50%时40%治疗/防御buff/30%普攻/30%其他；HP>=50%走均衡
        /// </summary>
        private AIDecision DecideDefensive(BattleMonster monster, BattleData battle, float hpRatio)
        {
            if (hpRatio < 0.5f)
            {
                float roll = Random.value;
                // 40%概率尝试治疗或防御buff
                if (roll < 0.4f)
                {
                    var healOrBuff = FindHealOrBuffSkill(monster, battle);
                    if (healOrBuff != null)
                        return new AIDecision(AIMonsterAction.UseSkill, healOrBuff.id);
                }
                // 30%普攻，30%其他技能
                if (roll < 0.7f)
                    return new AIDecision(AIMonsterAction.NormalAttack);

                // 其他可用技能
                var anySkill = PickRandomSkill(monster, battle);
                if (anySkill != null)
                    return new AIDecision(AIMonsterAction.UseSkill, anySkill.id);
                return new AIDecision(AIMonsterAction.NormalAttack);
            }

            // HP>=50%时走均衡策略
            return DecideBalanced(monster, battle);
        }

        // ==================== Debuffer：优先上debuff ====================

        /// <summary>
        /// 控制模式：目标无debuff→70%上debuff/30%普攻；已有debuff→60%普攻/40%技能
        /// </summary>
        private AIDecision DecideDebuffer(BattleMonster monster, BattleData battle)
        {
            bool playerHasDebuff = HasPlayerDebuff(battle);
            var debuffSkill = FindDebuffSkill(monster, battle);

            if (!playerHasDebuff && debuffSkill != null)
            {
                // 目标无debuff，70%上debuff
                if (Random.value < 0.7f)
                    return new AIDecision(AIMonsterAction.UseSkill, debuffSkill.id);
                return new AIDecision(AIMonsterAction.NormalAttack);
            }

            // 已有debuff或无debuff技能：60%普攻 / 40%技能
            if (Random.value < 0.6f)
                return new AIDecision(AIMonsterAction.NormalAttack);

            var anySkill = PickRandomSkill(monster, battle);
            if (anySkill != null)
                return new AIDecision(AIMonsterAction.UseSkill, anySkill.id);
            return new AIDecision(AIMonsterAction.NormalAttack);
        }

        // ==================== Balanced：均匀分布 ====================

        /// <summary>
        /// 均衡模式：50%普攻 / 50%技能
        /// </summary>
        private AIDecision DecideBalanced(BattleMonster monster, BattleData battle)
        {
            if (Random.value < 0.5f)
            {
                var skill = PickRandomSkill(monster, battle);
                if (skill != null)
                    return new AIDecision(AIMonsterAction.UseSkill, skill.id);
            }
            return new AIDecision(AIMonsterAction.NormalAttack);
        }

        // ==================== Boss：多阶段 ====================

        /// <summary>
        /// Boss模式：HP>60%走Aggressive，HP30-60%走Debuffer+Defensive混合，HP&lt;30%全技能+伤害+20%
        /// </summary>
        private AIDecision DecideBoss(BattleMonster monster, BattleData battle, float hpRatio)
        {
            // HP > 60%：Aggressive模式
            if (hpRatio > 0.6f)
            {
                return new AIDecision(AIMonsterAction.NormalAttack, hint: $"{monster.name}愤怒地咆哮！");
            }

            // HP 30%-60%：Debuffer + Defensive混合
            if (hpRatio > 0.3f)
            {
                // 交替使用debuffer和defensive策略
                if (Random.value < 0.5f)
                {
                    var decision = DecideDebuffer(monster, battle);
                    return decision;
                }
                else
                {
                    var decision = DecideDefensive(monster, battle, hpRatio);
                    return decision;
                }
            }

            // HP < 30%：全技能可用 + 伤害+20% + 优先最高伤技能
            var bestSkill = FindHighestDamageSkill(monster, battle);
            if (bestSkill != null)
            {
                return new AIDecision(AIMonsterAction.UseSkill, bestSkill.id, 1.2f,
                    $"{monster.name}进入狂暴状态，所有伤害提升！");
            }

            // 无技能则普攻+20%
            return new AIDecision(AIMonsterAction.NormalAttack, damageBonus: 1.2f,
                $"{monster.name}进入狂暴状态，所有伤害提升！");
        }

        // ==================== 辅助方法 ====================

        /// <summary>找到最高伤害的可用技能</summary>
        private SkillConfig FindHighestDamageSkill(BattleMonster monster, BattleData battle)
        {
            SkillConfig best = null;
            int bestPower = 0;
            foreach (int sid in monster.skillIds)
            {
                var skill = ConfigManager.Instance.GetSkill(sid);
                if (skill == null) continue;
                // 排除重复debuff：已有同类型debuff时不选
                if (IsDuplicateDebuff(skill, battle)) continue;
                if (skill.power > bestPower)
                {
                    bestPower = skill.power;
                    best = skill;
                }
            }
            return best;
        }

        /// <summary>找到治疗或防御buff技能</summary>
        private SkillConfig FindHealOrBuffSkill(BattleMonster monster, BattleData battle)
        {
            foreach (int sid in monster.skillIds)
            {
                var skill = ConfigManager.Instance.GetSkill(sid);
                if (skill == null) continue;
                if (skill.isHeal || skill.type == SkillType.Buff)
                    return skill;
            }
            return null;
        }

        /// <summary>找到debuff/状态攻击技能（排除重复debuff）</summary>
        private SkillConfig FindDebuffSkill(BattleMonster monster, BattleData battle)
        {
            foreach (int sid in monster.skillIds)
            {
                var skill = ConfigManager.Instance.GetSkill(sid);
                if (skill == null) continue;
                // 检查是否有debuff或状态攻击效果
                if (IsDebuffSkill(skill) && !IsDuplicateDebuff(skill, battle))
                    return skill;
            }
            return null;
        }

        /// <summary>随机选择一个可用技能</summary>
        private SkillConfig PickRandomSkill(BattleMonster monster, BattleData battle)
        {
            var available = new List<SkillConfig>();
            foreach (int sid in monster.skillIds)
            {
                var skill = ConfigManager.Instance.GetSkill(sid);
                if (skill != null && !IsDuplicateDebuff(skill, battle))
                    available.Add(skill);
            }
            if (available.Count == 0) return null;
            return available[Random.Range(0, available.Count)];
        }

        /// <summary>检查技能是否是debuff/状态攻击类</summary>
        private bool IsDebuffSkill(SkillConfig skill)
        {
            return skill.debuffAttack > 0 || skill.debuffDefense > 0 || skill.debuffSpeed > 0
                || skill.type == SkillType.StatusAttack;
        }

        /// <summary>检查玩家是否已有同类debuff（避免重复施加）</summary>
        private bool IsDuplicateDebuff(SkillConfig skill, BattleData battle)
        {
            // 状态攻击技能：检查玩家是否已有该状态效果
            if (skill.type == SkillType.StatusAttack && skill.statusDuration > 0)
            {
                bool hasEffect = battle.playerEffects.Exists(e => e.type == skill.statusEffect);
                return hasEffect;
            }
            return false;
        }

        /// <summary>检查玩家身上是否有debuff类状态效果</summary>
        private bool HasPlayerDebuff(BattleData battle)
        {
            return battle.playerEffects.Exists(e =>
                e.type == EffectType.Bleed || e.type == EffectType.Poison
                || e.type == EffectType.Stun || e.type == EffectType.Silence
                || e.type == EffectType.Vulnerable);
        }
    }
}
