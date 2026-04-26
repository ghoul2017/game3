using System.Collections.Generic;
using Game.Config;
using Game.Core;
using Logger = Game.Core.Logger;
using Game.Data;
using UnityEngine;

namespace Game.Battle
{
    public class BattleManager
    {
        private static BattleManager instance;
        public static BattleManager Instance => instance ??= new BattleManager();

        public BattleData CurrentBattle { get; private set; }

        // 被动技能：复活已使用标记
        private bool reviveUsed;

        // 装备特殊效果：凤凰羽毛复活已使用标记
        private bool equipReviveUsed;

        // ---- 装备特殊效果辅助方法 ----

        /// <summary>获取所有装备中指定效果类型的总数值</summary>
        private int GetEquipEffectTotal(EquipEffectType effectType)
        {
            var p = GameManager.Instance.Player;
            int total = 0;
            var weapon = ConfigManager.Instance.GetItem(p.weaponId);
            var armor = ConfigManager.Instance.GetItem(p.armorId);
            var accessory = ConfigManager.Instance.GetItem(p.accessoryId);
            if (weapon != null && weapon.equipEffectType == effectType) total += weapon.equipEffectValue;
            if (armor != null && armor.equipEffectType == effectType) total += armor.equipEffectValue;
            if (accessory != null && accessory.equipEffectType == effectType) total += accessory.equipEffectValue;
            return total;
        }

        /// <summary>装备吸血效果：造成伤害时恢复 effectValue% 的生命</summary>
        private void ApplyEquipLifesteal(int dmg)
        {
            int lifestealRate = GetEquipEffectTotal(EquipEffectType.Lifesteal);
            if (lifestealRate <= 0 || dmg <= 0) return;
            var p = GameManager.Instance.Player;
            int heal = Mathf.Max(1, dmg * lifestealRate / 100);
            p.hp = Mathf.Min(p.hp + heal, p.maxHp);
            CurrentBattle.log.Add($"【吸血】装备效果恢复 {heal} 生命");
        }

        /// <summary>装备Boss增伤倍率</summary>
        private float GetEquipBossDamageBonus()
        {
            if (CurrentBattle == null || !CurrentBattle.isBossBattle) return 1f;
            int bonus = GetEquipEffectTotal(EquipEffectType.BossDamageBonus);
            return bonus > 0 ? 1f + bonus / 100f : 1f;
        }

        /// <summary>装备伤害波动倍率（替换基础伤害浮动）</summary>
        private float GetEquipDamageVarianceRate()
        {
            int variance = GetEquipEffectTotal(EquipEffectType.DamageVariance);
            return variance > 0 ? variance / 100f : 0f;
        }

        /// <summary>装备反伤效果：受到攻击时反弹 effectValue% 受到的伤害</summary>
        private void ApplyEquipThorns(int dmgReceived)
        {
            int thornsRate = GetEquipEffectTotal(EquipEffectType.Thorns);
            if (thornsRate <= 0 || dmgReceived <= 0) return;
            var m = CurrentBattle?.monster;
            if (m == null || m.hp <= 0) return;
            int thornsDmg = Mathf.Max(1, dmgReceived * thornsRate / 100);
            m.hp = Mathf.Max(0, m.hp - thornsDmg);
            CurrentBattle.log.Add($"【反伤】装备效果反弹 {thornsDmg} 伤害");
        }

        /// <summary>装备技能减耗比例（返回 0-100 的百分比）</summary>
        private int GetEquipSkillCostReduce()
        {
            return GetEquipEffectTotal(EquipEffectType.SkillCostReduce);
        }

        /// <summary>装备自动复活：凤凰羽毛效果，死亡时复活一次恢复 effectValue% HP</summary>
        private bool TryEquipRevive()
        {
            if (equipReviveUsed) return false;
            int revivePercent = GetEquipEffectTotal(EquipEffectType.AutoRevive);
            if (revivePercent <= 0) return false;
            equipReviveUsed = true;
            var p = GameManager.Instance.Player;
            int reviveHp = Mathf.Max(1, p.maxHp * revivePercent / 100);
            p.hp = reviveHp;
            CurrentBattle.log.Add($"【凤凰羽毛】装备效果触发！复活并恢复 {reviveHp} 生命");
            // 复活后销毁装备
            if (p.accessoryId != 0)
            {
                var acc = ConfigManager.Instance.GetItem(p.accessoryId);
                if (acc != null && acc.equipEffectType == EquipEffectType.AutoRevive)
                {
                    CurrentBattle.log.Add($"【凤凰羽毛】{acc.name} 已消耗销毁");
                    p.accessoryId = 0;
                    p.RecalcStats();
                }
            }
            return true;
        }

        /// <summary>计算装备减耗后的实际技能消耗</summary>
        public int GetActualSkillCost(int baseCost)
        {
            int reduce = GetEquipSkillCostReduce();
            // 宝物：贤者之心（技能消耗-20%，effectType=6）
            reduce += GetTreasureEffectTotal(6);
            if (reduce <= 0) return baseCost;
            return Mathf.Max(1, baseCost * (100 - reduce) / 100);
        }

        // ---- 宝物效果辅助方法 ----

        /// <summary>获取指定effectType的宝物效果总值</summary>
        private int GetTreasureEffectTotal(int effectType)
        {
            var p = GameManager.Instance.Player;
            int total = 0;
            foreach (var tid in p.treasures)
            {
                var tc = ConfigManager.Instance.GetTreasure(tid);
                if (tc != null && tc.effectType == effectType)
                    total += tc.effectValue;
            }
            return total;
        }

        /// <summary>检查玩家是否拥有指定effectType的宝物</summary>
        private bool HasTreasureEffect(int effectType)
        {
            return GetTreasureEffectTotal(effectType) > 0;
        }

        /// <summary>战斗开始时应用宝物效果（守护结界护盾）</summary>
        private void ApplyTreasureBattleStartEffects()
        {
            var p = GameManager.Instance.Player;
            var b = CurrentBattle;
            if (b == null) return;

            // 守护结界（effectType=7）：战斗开始获得 effectValue% 最大HP的护盾
            int shieldPercent = GetTreasureEffectTotal(7);
            if (shieldPercent > 0)
            {
                int shield = Mathf.Max(1, p.maxHp * shieldPercent / 100);
                b.shieldAmount += shield;
                b.log.Add($"【守护结界】宝物效果！获得 {shield} 点护盾");
            }
        }

        /// <summary>护盾吸收伤害，返回实际扣除的HP量</summary>
        private int ApplyShieldAbsorb(int dmg)
        {
            var b = CurrentBattle;
            if (b == null || b.shieldAmount <= 0 || dmg <= 0) return dmg;

            int absorbed = Mathf.Min(b.shieldAmount, dmg);
            b.shieldAmount -= absorbed;
            int remaining = dmg - absorbed;
            if (absorbed > 0)
                b.log.Add($"【护盾】吸收了 {absorbed} 点伤害（剩余护盾 {b.shieldAmount}）");
            return remaining;
        }

        /// <summary>贪婪之戒检查：掉落翻倍但受伤+15%</summary>
        private bool HasGreedRing()
        {
            return HasTreasureEffect(9);
        }

        /// <summary>贪婪之戒受伤加成倍率</summary>
        private float GetGreedDamageMultiplier()
        {
            int value = GetTreasureEffectTotal(9);
            return value > 0 ? 1f + value / 100f : 1f;
        }

        // ---- 被动技能辅助方法 ----

        private bool HasPassive(int skillId)
        {
            var p = GameManager.Instance.Player;
            return p.learnedSkills.Contains(skillId);
        }

        /// <summary>狂战之怒：HP越低攻击越高，最高+50%</summary>
        private int GetRageBonus(int baseAtk)
        {
            if (!HasPassive(1004)) return 0;
            var p = GameManager.Instance.Player;
            float hpRatio = p.maxHp > 0 ? (float)p.hp / p.maxHp : 1f;
            // 缺失HP比例的一半作为攻击加成，最高50%
            float bonus = (1f - hpRatio) * 0.5f;
            return (int)(baseAtk * bonus);
        }

        /// <summary>坚守：HP低于30%时减伤30%</summary>
        private float GetSteadfastMultiplier()
        {
            if (!HasPassive(3004)) return 1f;
            var p = GameManager.Instance.Player;
            float hpRatio = p.maxHp > 0 ? (float)p.hp / p.maxHp : 1f;
            return hpRatio < 0.3f ? 0.7f : 1f; // 受到70%伤害
        }

        /// <summary>魔力涌动：每回合恢复10%最大法力</summary>
        private void ApplyManaSurge()
        {
            if (!HasPassive(5004)) return;
            var p = GameManager.Instance.Player;
            int restore = Mathf.Max(1, p.maxMp / 10);
            p.mp = Mathf.Min(p.mp + restore, p.maxMp);
        }

        /// <summary>复活：被动技能或装备凤凰羽毛，优先被动技能</summary>
        private bool TryRevive()
        {
            // 优先被动技能复活
            if (HasPassive(4004) && !reviveUsed)
            {
                reviveUsed = true;
                var p = GameManager.Instance.Player;
                int reviveHp = Mathf.Max(1, p.maxHp * 30 / 100);
                p.hp = reviveHp;
                CurrentBattle.log.Add($"【复活】被动技能触发！恢复 {reviveHp} 生命");
                return true;
            }
            // 其次装备复活（凤凰羽毛）
            return TryEquipRevive();
        }

        /// <summary>宝物：荆棘之甲（受击10%概率反伤50%，effectType=8）</summary>
        private void ApplyTreasureThorns(int dmgReceived)
        {
            int thornsPercent = GetTreasureEffectTotal(8);
            if (thornsPercent <= 0 || dmgReceived <= 0) return;

            // 10%概率触发
            if (Random.value >= 0.10f) return;

            var m = CurrentBattle?.monster;
            if (m == null || m.hp <= 0) return;

            int thornsDmg = Mathf.Max(GameConstants.MIN_DAMAGE, dmgReceived * thornsPercent / 100);
            m.hp = Mathf.Max(0, m.hp - thornsDmg);
            CurrentBattle.log.Add($"【荆棘之甲】宝物效果触发！反弹 {thornsDmg} 伤害");
        }

        /// <summary>
        /// 速度差闪避判定：速度差>=5时，高速度方获得(speedDiff*2)%闪避率
        /// 返回 true 表示闪避成功
        /// </summary>
        private bool TrySpeedDodge(bool isPlayerAttacking)
        {
            int playerSpeed = GetEffectivePlayerSpeed();
            int monsterSpeed = GetEffectiveMonsterSpeed();
            int speedDiff = isPlayerAttacking
                ? playerSpeed - monsterSpeed
                : monsterSpeed - playerSpeed;

            if (speedDiff < GameConstants.SPEED_DODGE_THRESHOLD) return false;

            float dodgeChance = speedDiff * GameConstants.SPEED_DODGE_RATE_PER_POINT / 100f;
            if (Random.value < dodgeChance) return true;
            return false;
        }

        public void StartBattle(BattleData battleData)
        {
            CurrentBattle = battleData;
            Logger.LogI("战斗", $"开始战斗 怪物={battleData.monster.name} HP={battleData.monster.hp}");
            CurrentBattle.isFinished = false;
            CurrentBattle.isVictory = false;
            CurrentBattle.playerBuffAttack = 0;
            CurrentBattle.playerBuffDefense = 0;
            CurrentBattle.playerBuffSpeed = 0;
            CurrentBattle.playerBuffTurns = 0;
            CurrentBattle.playerEffects.Clear();
            CurrentBattle.monsterEffects.Clear();
            CurrentBattle.log.Clear();
            CurrentBattle.shieldAmount = 0;
            reviveUsed = false;
            equipReviveUsed = false;

            // Boss战特殊处理
            bool isBoss = battleData.monster.isBoss;
            CurrentBattle.isBossBattle = isBoss;
            CurrentBattle.bossDefeated = false;
            CurrentBattle.bossKillTitle = "";

            if (isBoss)
            {
                // Boss战前警告
                CurrentBattle.log.Add("遭遇了 " + battleData.monster.name + "！");
                CurrentBattle.log.Add("强大的敌人出现了！");
                // 播放Boss警告音效和BGM
                Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_BossWarning);
                Core.AudioManager.Instance?.CrossfadeMusic(Core.AudioClipType.Battle_BossBGM, 1.0f);
            }
            else
            {
                CurrentBattle.log.Add($"遭遇了 {battleData.monster.name}！");
                Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Attack);
                Core.AudioManager.Instance?.CrossfadeMusic(Core.AudioClipType.BGM_Battle, 0.5f);
            }

            // 根据速度判定先手
            DetermineTurnOrder();

            // 宝物：守护结界（开局护盾，effectType=7）
            ApplyTreasureBattleStartEffects();
        }

        /// <summary>根据双方有效速度判定回合归属</summary>
        public void DetermineTurnOrder()
        {
            var b = CurrentBattle;
            if (b == null) return;
            int playerSpeed = GetEffectivePlayerSpeed();
            int monsterSpeed = GetEffectiveMonsterSpeed();
            b.isPlayerTurn = playerSpeed >= monsterSpeed;
            string turnMsg = b.isPlayerTurn ? "你的回合" : "敌人回合";
            b.log.Add($"[速度] 你:{playerSpeed} 敌方:{monsterSpeed} — {turnMsg}");
        }

        /// <summary>获取玩家有效速度 = 基础速度 + 速度buff + 状态效果修正</summary>
        public int GetEffectivePlayerSpeed()
        {
            var p = GameManager.Instance.Player;
            int baseSpeed = p.speed + (CurrentBattle?.playerBuffSpeed ?? 0);
            return baseSpeed + GetEffectModifier(true, EffectType.BuffSpeed);
        }

        /// <summary>获取怪物有效速度 = 基础速度 + 速度buff + 状态效果修正</summary>
        public int GetEffectiveMonsterSpeed()
        {
            var m = CurrentBattle?.monster;
            if (m == null) return 0;
            return m.speed + m.buffSpeed + GetEffectModifier(false, EffectType.BuffSpeed);
        }

        // ==================== 统一回合流程 ====================

        /// <summary>
        /// 回合开始阶段：Tick双方状态效果 -> DetermineTurnOrder
        /// 由UI层在每个回合开始时调用一次
        /// </summary>
        public void RoundStart()
        {
            var b = CurrentBattle;
            if (b == null || b.isFinished) return;

            // Tick双方状态效果（DoT伤害、duration递减、过期移除）
            TickEffects(true);
            TickEffects(false);

            // 检查双方是否因DoT致死
            var p = GameManager.Instance.Player;
            if (p.hp <= 0)
            {
                if (!TryRevive()) { OnDefeat(); return; }
            }
            if (b.monster.hp <= 0)
            {
                b.monster.hp = 0;
                OnVictory();
                return;
            }

            // 魔力涌动：每回合回蓝
            ApplyManaSurge();

            // 根据双方有效速度判定先手
            DetermineTurnOrder();
        }

        /// <summary>
        /// 回合结束阶段：衰减双方buff，重新判定回合顺序
        /// </summary>
        public void RoundEnd()
        {
            var b = CurrentBattle;
            if (b == null || b.isFinished) return;
            DecayBuffs(b);
        }

        /// <summary>检查玩家是否被眩晕</summary>
        public bool IsPlayerStunned()
        {
            return CurrentBattle != null && HasEffect(true, EffectType.Stun);
        }

        /// <summary>检查玩家是否被沉默</summary>
        public bool IsPlayerSilenced()
        {
            return CurrentBattle != null && HasEffect(true, EffectType.Silence);
        }

        /// <summary>检查怪物是否被眩晕</summary>
        public bool IsMonsterStunned()
        {
            return CurrentBattle != null && HasEffect(false, EffectType.Stun);
        }

        /// <summary>
        /// 执行玩家攻击行动（不含TickEffects和DetermineTurnOrder）
        /// </summary>
        public string PlayerActionAttack()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            if (b == null || b.isFinished) return "";

            // 检查玩家是否死亡
            if (p.hp <= 0)
            {
                if (TryRevive()) { /* 复活成功，继续行动 */ }
                else { return OnDefeat(); }
            }

            // 眩晕：跳过行动
            if (HasEffect(true, EffectType.Stun))
            {
                b.log.Add("你因眩晕无法行动！");
                return "你因眩晕无法行动！";
            }

            // 攻击音效
            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Attack);

            // 速度差闪避判定：怪物闪避
            if (TrySpeedDodge(true))
            {
                Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Dodge);
                string dodgeMsg = $"{b.monster.name}闪避了攻击！";
                b.log.Add(dodgeMsg);
                return dodgeMsg;
            }

            // 狂战之怒：HP越低攻击越高
            int totalAtk = GetEffectiveATK(true);
            int rawDmg = CalcPlayerDamage(totalAtk, GetEffectiveDEF(false));
            float monsterVuln = GetVulnerableMultiplier(false);
            float bossBonus = GetEquipBossDamageBonus();
            int dmg = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(rawDmg * monsterVuln * bossBonus));
            b.monster.hp -= dmg;
            string msg = $"你攻击了 {b.monster.name}，造成 {dmg} 点伤害";

            // 吸血之牙宝物效果：攻击恢复 effectValue% 伤害值的生命
            if (p.treasures.Contains(GameConstants.TREASURE_ID_LIFESTEAL))
            {
                var tc = ConfigManager.Instance.GetTreasure(GameConstants.TREASURE_ID_LIFESTEAL);
                if (tc != null)
                {
                    int lifesteal = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(dmg * tc.effectValue / 100f));
                    p.hp = Mathf.Min(p.hp + lifesteal, p.maxHp);
                    msg += $"，吸血恢复 {lifesteal} 生命";
                }
            }

            // 装备吸血效果
            ApplyEquipLifesteal(dmg);

            b.log.Add(msg);

            if (b.monster.hp <= 0)
            {
                b.monster.hp = 0;
                return msg + "\n" + OnVictory();
            }
            return msg;
        }

        // 兼容旧调用
        public string PlayerAttack()
        {
            var b = CurrentBattle;
            if (b == null || b.isFinished) return "";
            RoundStart();
            if (b.isFinished) return "";
            return PlayerActionAttack();
        }

        /// <summary>
        /// 执行玩家技能行动（不含TickEffects和DetermineTurnOrder）
        /// </summary>
        public string PlayerActionSkill(int skillId)
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            var skill = ConfigManager.Instance.GetSkill(skillId);
            if (b == null || b.isFinished || skill == null) return "";

            // 检查玩家是否死亡
            if (p.hp <= 0)
            {
                if (TryRevive()) { /* 复活成功 */ }
                else { return OnDefeat(); }
            }

            // 眩晕：跳过行动
            if (HasEffect(true, EffectType.Stun))
            {
                b.log.Add("你因眩晕无法行动！");
                return "你因眩晕无法行动！";
            }

            // 沉默：无法使用技能
            if (HasEffect(true, EffectType.Silence))
            {
                string silenceMsg = "你处于沉默状态，无法使用技能！";
                b.log.Add(silenceMsg);
                return silenceMsg;
            }

            if (p.mp < GetActualSkillCost(skill.mpCost))
            {
                string errMsg = "法力不足！";
                b.log.Add(errMsg);
                return errMsg;
            }

            p.mp -= GetActualSkillCost(skill.mpCost);
            string msg;

            if (skill.type == SkillType.Passive)
            {
                msg = $"{skill.name} 是被动技能，无法主动使用";
                b.log.Add(msg);
                return msg;
            }

            // 技能音效
            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Skill);

            if (skill.type == SkillType.Buff)
            {
                if (skill.buffId > 0)
                {
                    var buffCfg = ConfigManager.Instance.GetBuff(skill.buffId);
                    if (buffCfg != null)
                    {
                        b.playerBuffAttack = 0;
                        b.playerBuffDefense = 0;
                        b.playerBuffSpeed = 0;
                        if (buffCfg.effectType == 0 || buffCfg.effectType == 2)
                            b.playerBuffAttack = p.attack * buffCfg.effectValue / 100;
                        if (buffCfg.effectType == 1 || buffCfg.effectType == 2)
                            b.playerBuffDefense = p.defense * buffCfg.effectValue / 100;
                        if (buffCfg.effectType == 3)
                            b.playerBuffSpeed = buffCfg.effectValue;
                        b.playerBuffTurns = skill.buffTurns;
                        string speedInfo = b.playerBuffSpeed > 0 ? $" 速度+{b.playerBuffSpeed}" : "";
                        msg = $"你使用了 {skill.name}！攻击+{b.playerBuffAttack} 防御+{b.playerBuffDefense}{speedInfo}，持续{skill.buffTurns}回合";
                    }
                    else
                    {
                        msg = $"你使用了 {skill.name}！";
                    }
                }
                else
                {
                    msg = $"你使用了 {skill.name}！";
                }
                b.log.Add(msg);
                return msg;
            }

            // 治疗技能
            if (skill.isHeal)
            {
                int healAmount = (int)(p.attack * skill.power / 100f) + p.level * 5;
                int actualHeal = Mathf.Min(healAmount, p.maxHp - p.hp);
                p.hp += actualHeal;
                msg = $"你使用 {skill.name}，恢复 {actualHeal} 点生命";
                b.log.Add(msg);
                return msg;
            }

            // 纯减益技能：不造成伤害，只施加减益效果
            if (skill.type == SkillType.Debuff)
            {
                msg = $"你使用了 {skill.name}";
                if (skill.debuffAttack > 0 || skill.debuffDefense > 0 || skill.debuffSpeed > 0)
                {
                    var m = b.monster;
                    if (skill.debuffAttack > 0)
                    {
                        int reduce = m.attack * skill.debuffAttack / 100;
                        m.buffAttack -= reduce;
                        m.buffTurns = Mathf.Max(m.buffTurns, skill.debuffTurns);
                        msg += $"，敌人攻击-{reduce}";
                    }
                    if (skill.debuffDefense > 0)
                    {
                        int reduce = m.defense * skill.debuffDefense / 100;
                        m.buffDefense -= reduce;
                        m.buffTurns = Mathf.Max(m.buffTurns, skill.debuffTurns);
                        msg += $"，敌人防御-{reduce}";
                    }
                    if (skill.debuffSpeed > 0)
                    {
                        m.buffSpeed -= skill.debuffSpeed;
                        m.buffTurns = Mathf.Max(m.buffTurns, skill.debuffTurns);
                        msg += $"，敌人速度-{skill.debuffSpeed}";
                    }
                }
                b.log.Add(msg);
                return msg;
            }

            // 主动攻击技能 / 状态攻击技能 / DoT技能 / 控制技能
            int totalAtk = GetEffectiveATK(true);
            int skillRawDmg = CalcPlayerDamage((int)(totalAtk * skill.power / 100f), GetEffectiveDEF(false));
            float skillVuln = GetVulnerableMultiplier(false);
            float skillBossBonus = GetEquipBossDamageBonus();
            int dmg = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(skillRawDmg * skillVuln * skillBossBonus));
            b.monster.hp -= dmg;
            msg = $"你使用 {skill.name}，造成 {dmg} 点伤害";

            // Debuff效果
            if (skill.debuffAttack > 0 || skill.debuffDefense > 0 || skill.debuffSpeed > 0)
            {
                var m = b.monster;
                if (skill.debuffAttack > 0)
                {
                    int reduce = m.attack * skill.debuffAttack / 100;
                    m.buffAttack -= reduce;
                    m.buffTurns = Mathf.Max(m.buffTurns, skill.debuffTurns);
                    msg += $"，敌人攻击-{reduce}";
                }
                if (skill.debuffDefense > 0)
                {
                    int reduce = m.defense * skill.debuffDefense / 100;
                    m.buffDefense -= reduce;
                    m.buffTurns = Mathf.Max(m.buffTurns, skill.debuffTurns);
                    msg += $"，敌人防御-{reduce}";
                }
                if (skill.debuffSpeed > 0)
                {
                    m.buffSpeed -= skill.debuffSpeed;
                    m.buffTurns = Mathf.Max(m.buffTurns, skill.debuffTurns);
                    msg += $"，敌人速度-{skill.debuffSpeed}";
                }
            }

            // 状态攻击/DoT/控制技能：附加状态效果
            if ((skill.type == SkillType.StatusAttack || skill.type == SkillType.DoT || skill.type == SkillType.Control) && skill.statusDuration > 0)
            {
                int effectValue = CalcStatusEffectValue(skill, totalAtk);
                ApplyEffect(false, skill.statusEffect, effectValue, skill.statusDuration, skill.id);
                msg += $"，附加{new StatusEffect(skill.statusEffect, effectValue, skill.statusDuration).GetName()}效果";
            }

            // 吸血之牙宝物效果
            if (p.treasures.Contains(GameConstants.TREASURE_ID_LIFESTEAL))
            {
                var tc = ConfigManager.Instance.GetTreasure(GameConstants.TREASURE_ID_LIFESTEAL);
                if (tc != null)
                {
                    int lifesteal = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(dmg * tc.effectValue / 100f));
                    p.hp = Mathf.Min(p.hp + lifesteal, p.maxHp);
                    msg += $"，吸血恢复 {lifesteal} 生命";
                }
            }

            // 装备吸血效果
            ApplyEquipLifesteal(dmg);

            // 被动技能：法力汲取（技能造成伤害时回复伤害值10%的MP）
            if (HasPassive(6017) && dmg > 0)
            {
                int mpRestore = Mathf.Max(1, dmg / 10);
                int actualMpRestore = Mathf.Min(mpRestore, p.maxMp - p.mp);
                if (actualMpRestore > 0)
                {
                    p.mp += actualMpRestore;
                    msg += $"，汲取 {actualMpRestore} 法力";
                }
            }

            b.log.Add(msg);

            if (b.monster.hp <= 0)
            {
                b.monster.hp = 0;
                return msg + "\n" + OnVictory();
            }
            return msg;
        }

        // 兼容旧调用
        public string PlayerSkill(int skillId)
        {
            var b = CurrentBattle;
            if (b == null || b.isFinished) return "";
            RoundStart();
            if (b.isFinished) return "";
            return PlayerActionSkill(skillId);
        }

        /// <summary>
        /// 执行玩家使用物品行动（不含TickEffects和DetermineTurnOrder）
        /// </summary>
        public string PlayerActionUseItem(int itemId)
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (b == null || b.isFinished || item == null) return "";

            // 检查玩家是否死亡
            if (p.hp <= 0)
            {
                if (TryRevive()) { /* 复活成功 */ }
                else { return OnDefeat(); }
            }

            // 眩晕：跳过行动
            if (HasEffect(true, EffectType.Stun))
            {
                b.log.Add("你因眩晕无法行动！");
                return "你因眩晕无法行动！";
            }

            if (item.restoreHp > 0)
            {
                int heal = Mathf.Min(item.restoreHp, p.maxHp - p.hp);
                p.hp += heal;
                string msg = $"使用 {item.name}，恢复 {heal} 点生命";
                b.log.Add(msg);
            }
            if (item.restoreMp > 0)
            {
                int heal = Mathf.Min(item.restoreMp, p.maxMp - p.mp);
                p.mp += heal;
                string msg = $"使用 {item.name}，恢复 {heal} 点法力";
                b.log.Add(msg);
            }

            // 消耗品特殊效果
            switch (item.consumableEffect)
            {
                case ConsumableEffectType.CurePoison:
                    if (HasEffect(true, EffectType.Poison))
                    {
                        RemoveEffect(true, EffectType.Poison);
                        b.log.Add("【解毒药】中毒状态已移除！");
                    }
                    else
                    {
                        b.log.Add("使用了解毒药，但你并未中毒");
                    }
                    break;

                case ConsumableEffectType.AtkBuff:
                    p.atkBuffBattles += item.consumableEffectValue;
                    b.log.Add($"【兴奋剂】攻击力+20%，持续 {p.atkBuffBattles} 场战斗");
                    break;

                case ConsumableEffectType.Shield:
                    p.hasShieldScroll = true;
                    b.log.Add("【护盾卷轴】下次受伤减伤50%已就绪");
                    break;

                case ConsumableEffectType.ExpBoost:
                    p.hasExpBoost = true;
                    b.log.Add("【经验书】下一场战斗经验+50%已就绪");
                    break;

                case ConsumableEffectType.Bomb:
                    {
                        int bombDmg = item.consumableEffectValue > 0 ? item.consumableEffectValue : 50;
                        b.monster.hp = Mathf.Max(0, b.monster.hp - bombDmg);
                        b.log.Add($"【炸弹】对 {b.monster.name} 造成 {bombDmg} 点无视防御伤害！");
                        if (b.monster.hp <= 0)
                        {
                            b.monster.hp = 0;
                            p.bagItems.Remove(itemId);
                            return $"使用了炸弹！\n" + OnVictory();
                        }
                    }
                    break;

                // Teleport 回城卷轴：战斗中无法使用，由UI层处理
            }

            p.bagItems.Remove(itemId);
            return $"使用了 {item.name}";
        }

        // 兼容旧调用
        public string PlayerUseItem(int itemId)
        {
            var b = CurrentBattle;
            if (b == null || b.isFinished) return "";
            RoundStart();
            if (b.isFinished) return "";
            return PlayerActionUseItem(itemId);
        }

        /// <summary>
        /// 执行怪物行动（不含TickEffects）
        /// </summary>
        public string MonsterAction()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            if (b == null || b.isFinished) return "";

            // 眩晕：跳过行动
            if (HasEffect(false, EffectType.Stun))
            {
                string stunMsg = $"{b.monster.name}因眩晕无法行动！";
                b.log.Add(stunMsg);
                return stunMsg;
            }

            var m = b.monster;
            int monsterAtk = GetEffectiveATK(false);
            int playerDef = GetEffectiveDEF(true);
            float dmgMultiplier = GetSteadfastMultiplier();
            float vulnerableMult = GetVulnerableMultiplier(true);
            dmgMultiplier *= vulnerableMult;

            // 贪婪之戒：受伤+15%
            dmgMultiplier *= GetGreedDamageMultiplier();

            // 受击音效
            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Hit);

            // 速度差闪避判定：玩家闪避
            if (TrySpeedDodge(false))
            {
                Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Dodge);
                string dodgeMsg = "你闪避了攻击！";
                b.log.Add(dodgeMsg);
                return dodgeMsg;
            }

            // 使用AI系统决策怪物行动
            var decision = AIManager.Instance.DecideAction(m, b, m.mp > 0);

            // AI提示（可选战斗日志）
            if (!string.IsNullOrEmpty(decision.hint))
                b.log.Add(decision.hint);

            if (decision.action == AIMonsterAction.UseSkill)
            {
                var skill = ConfigManager.Instance.GetSkill(decision.skillId);
                if (skill != null)
                {
                    // Boss使用技能标记（UI层触发屏幕震动）
                    if (b.isBossBattle)
                        b.bossSkillTriggered = true;
                    int rawDmg = CalcDamage((int)(monsterAtk * skill.power / 100f), playerDef);
                    int dmg = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(rawDmg * dmgMultiplier * decision.damageBonus));

                    // 护盾卷轴减伤50%
                    if (p.hasShieldScroll)
                    {
                        dmg = Mathf.Max(GameConstants.MIN_DAMAGE, dmg / 2);
                        p.hasShieldScroll = false;
                    }

                    // 护盾吸收
                    dmg = ApplyShieldAbsorb(dmg);

                    p.hp -= dmg;
                    if (p.hp < 0) p.hp = 0;
                    string msg = $"{m.name} 使用 {skill.name}，对你造成 {dmg} 点伤害";
                    if (dmgMultiplier < 1f) msg += "（坚守减伤）";
                    if (decision.damageBonus > 1f) msg += "（狂暴加成）";

                    // 状态攻击/DoT/控制技能：怪物对玩家附加状态效果
                    if ((skill.type == SkillType.StatusAttack || skill.type == SkillType.DoT || skill.type == SkillType.Control) && skill.statusDuration > 0)
                    {
                        int effectValue = CalcStatusEffectValue(skill, monsterAtk);
                        ApplyEffect(true, skill.statusEffect, effectValue, skill.statusDuration, skill.id);
                        msg += $"，附加{new StatusEffect(skill.statusEffect, effectValue, skill.statusDuration).GetName()}效果";
                    }

                    b.log.Add(msg);

                    // 装备反伤效果
                    ApplyEquipThorns(dmg);

                    // 宝物：荆棘之甲（受击10%概率反伤50%，effectType=8）
                    ApplyTreasureThorns(dmg);

                    if (p.hp <= 0)
                    {
                        if (TryRevive()) return msg;
                        return msg + "\n" + OnDefeat();
                    }
                    // 反伤可能导致怪物死亡
                    if (m.hp <= 0)
                    {
                        m.hp = 0;
                        return msg + "\n" + OnVictory();
                    }
                    return msg;
                }
            }

            // 普通攻击
            int rawNormalDmg = CalcDamage(monsterAtk, playerDef);
            int normalDmg = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(rawNormalDmg * dmgMultiplier * decision.damageBonus));

            // 护盾卷轴减伤50%
            if (p.hasShieldScroll)
            {
                normalDmg = Mathf.Max(GameConstants.MIN_DAMAGE, normalDmg / 2);
                p.hasShieldScroll = false;
            }

            // 护盾吸收
            normalDmg = ApplyShieldAbsorb(normalDmg);

            p.hp -= normalDmg;
            if (p.hp < 0) p.hp = 0;
            string normalMsg = $"{m.name} 攻击了你，造成 {normalDmg} 点伤害";
            if (dmgMultiplier < 1f) normalMsg += "（坚守减伤）";
            if (decision.damageBonus > 1f) normalMsg += "（狂暴加成）";
            b.log.Add(normalMsg);

            // 装备反伤效果
            ApplyEquipThorns(normalDmg);

            // 宝物：荆棘之甲（受击10%概率反伤50%，effectType=8）
            ApplyTreasureThorns(normalDmg);

            if (p.hp <= 0)
            {
                if (TryRevive()) return normalMsg;
                return normalMsg + "\n" + OnDefeat();
            }
            // 反伤可能导致怪物死亡
            if (m.hp <= 0)
            {
                m.hp = 0;
                return normalMsg + "\n" + OnVictory();
            }
            return normalMsg;
        }

        // 兼容旧调用：MonsterTurn 仍可单独使用，内部调用 MonsterAction
        public string MonsterTurn()
        {
            var b = CurrentBattle;
            if (b == null || b.isFinished) return "";
            return MonsterAction();
        }

        private void DecayBuffs(BattleData b)
        {
            var m = b.monster;
            // 怪物buff衰减
            if (m.buffTurns > 0)
            {
                m.buffTurns--;
                if (m.buffTurns <= 0)
                {
                    m.buffAttack = 0;
                    m.buffDefense = 0;
                    m.buffSpeed = 0;
                }
            }
            // 玩家buff衰减
            if (b.playerBuffTurns > 0)
            {
                b.playerBuffTurns--;
                if (b.playerBuffTurns <= 0)
                {
                    b.playerBuffAttack = 0;
                    b.playerBuffDefense = 0;
                    b.playerBuffSpeed = 0;
                    b.log.Add("你的增益效果消失了");
                }
            }
        }

        /// <summary>
        /// 执行逃跑行动（不含TickEffects）
        /// </summary>
        public string ActionEscape()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            if (b == null || b.isFinished) return "";

            // 检查玩家是否死亡
            if (p.hp <= 0)
            {
                if (TryRevive()) { /* 复活成功 */ }
                else { return OnDefeat(); }
            }

            // 眩晕：无法逃跑
            if (HasEffect(true, EffectType.Stun))
            {
                b.log.Add("你因眩晕无法行动！");
                return "你因眩晕无法行动！";
            }

            // 50%逃跑概率，Boss不可逃跑
            var m = b.monster;
            var cfg = ConfigManager.Instance.GetMonster(m.id);
            if (cfg != null && cfg.isBoss)
            {
                string msg = "无法从Boss战中逃跑！";
                b.log.Add(msg);
                return msg;
            }

            if (Random.value < GameConstants.ESCAPE_CHANCE)
            {
                b.isFinished = true;
                string msg = "逃跑成功！";
                b.log.Add(msg);
                return msg;
            }
            string failMsg = "逃跑失败！";
            b.log.Add(failMsg);
            return failMsg;
        }

        public string TryEscape()
        {
            var b = CurrentBattle;
            if (b == null || b.isFinished) return "";
            RoundStart();
            if (b.isFinished) return "";

            string result = ActionEscape();
            if (!b.isFinished)
            {
                // 逃跑失败，怪物行动
                MonsterAction();
            }
            return result;
        }

        private string OnVictory()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            var m = b.monster;

            // Boss战停止专属BGM
            if (b.isBossBattle)
                Core.AudioManager.Instance?.StopMusic();

            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Victory);

            b.isFinished = true;
            b.isVictory = true;
            if (b.room != null) b.room.cleared = true;

            // 被动技能：击杀回蓝（击杀怪物时回复20MP）
            if (HasPassive(6018))
            {
                int mpRestore = Mathf.Min(20, p.maxMp - p.mp);
                if (mpRestore > 0)
                {
                    p.mp += mpRestore;
                    b.log.Add($"【击杀回蓝】回复 {mpRestore} 法力");
                }
            }

            // 宝物加成：幸运硬币(id=5)金币+30%，经验宝石(id=6)经验+20%
            int goldReward = m.goldReward;
            int expReward = m.expReward;

            if (p.treasures.Contains(GameConstants.TREASURE_ID_LUCKY_COIN))
            {
                var luckyCoin = ConfigManager.Instance.GetTreasure(GameConstants.TREASURE_ID_LUCKY_COIN);
                if (luckyCoin != null)
                    goldReward = (int)(goldReward * (1 + luckyCoin.effectValue / 100f));
            }

            if (p.treasures.Contains(GameConstants.TREASURE_ID_EXP_GEM))
            {
                var expGem = ConfigManager.Instance.GetTreasure(GameConstants.TREASURE_ID_EXP_GEM);
                if (expGem != null)
                    expReward = (int)(expReward * (1 + expGem.effectValue / 100f));
            }

            // 消耗品：经验书效果（下一场战斗经验+50%）
            if (p.hasExpBoost)
            {
                expReward = (int)(expReward * 1.5f);
                p.hasExpBoost = false;
                b.log.Add("【经验书】经验加成50%已生效！");
            }

            // 宝物：贪婪之戒（掉落翻倍，effectType=9）
            if (HasGreedRing())
            {
                goldReward *= 2;
                b.log.Add("【贪婪之戒】金币掉落翻倍！");
            }

            // Boss掉落加成：额外 floor*100 金币 + floor*50 经验
            if (b.isBossBattle)
            {
                int floor = GameManager.Instance.Dungeon?.floor ?? 1;
                goldReward += floor * GameConstants.BOSS_GOLD_FLOOR_MULTIPLIER;
                expReward += floor * GameConstants.BOSS_EXP_FLOOR_MULTIPLIER;
            }

            p.AddExp(expReward);
            p.gold += goldReward;

            // 消耗品：兴奋剂战斗计数递减
            if (p.atkBuffBattles > 0)
            {
                p.atkBuffBattles--;
                if (p.atkBuffBattles <= 0)
                    b.log.Add("兴奋剂效果消失了");
            }

            // 存储奖励数据
            b.rewardExp = expReward;
            b.rewardGold = goldReward;
            b.rewardItems.Clear();

            Logger.LogI("战斗", $"胜利！怪物={m.name} 经验={expReward} 金币={goldReward}");
            string msg = $"击败了 {m.name}！获得 {expReward} 经验，{goldReward} 金币";

            // 掉落物品
            foreach (var drop in m.drops)
            {
                if (Random.value < drop.chance && p.bagItems.Count < p.maxBagSize)
                {
                    var item = ConfigManager.Instance.GetItem(drop.itemId);
                    if (item != null)
                    {
                        p.bagItems.Add(drop.itemId);
                        b.rewardItems.Add(item.name);
                        msg += $"\n获得物品：{item.name}";
                    }
                }
            }

            // Boss额外掉落：1个随机宝物
            if (b.isBossBattle)
            {
                var allTreasures = ConfigManager.Instance.GetAllTreasures();
                var available = allTreasures.FindAll(t => !p.treasures.Contains(t.id));
                if (available.Count > 0)
                {
                    var treasure = available[Random.Range(0, available.Count)];
                    p.treasures.Add(treasure.id);
                    p.RecalcStats();
                    b.rewardItems.Add($"宝物：{treasure.name}");
                    msg += $"\n获得宝物：{treasure.name} — {treasure.desc}";
                }

                // 记录Boss击杀到PlayerData
                if (!p.defeatedBosses.Contains(m.id))
                {
                    p.defeatedBosses.Add(m.id);
                    Logger.LogI("战斗", $"记录Boss击杀：{m.name}(id={m.id})");
                }

                b.bossDefeated = true;
                b.bossKillTitle = $"击败了 {m.name}！";
            }

            b.log.Add(msg);
            return msg;
        }

        private string OnDefeat()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;

            // Boss战停止专属BGM
            if (b.isBossBattle)
                Core.AudioManager.Instance?.StopMusic();

            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Defeat);

            b.isFinished = true;
            b.isVictory = false;
            b.playerBuffAttack = 0;
            b.playerBuffDefense = 0;
            b.playerBuffSpeed = 0;
            b.playerBuffTurns = 0;

            // 失败惩罚：损失金币
            int lostGold = (int)(p.gold * GameConstants.DEFEAT_GOLD_LOSS_RATE);
            Logger.LogI("战斗", $"失败！损失金币={lostGold}");
            p.gold -= lostGold;
            p.dungeonFloor = Mathf.Max(1, p.dungeonFloor - 1);
            p.FullRestore();

            string msg = $"你被击败了！损失 {lostGold} 金币，返回城镇";
            b.log.Add(msg);
            return msg;
        }

        private int CalcDamage(int atk, int def)
        {
            int baseDmg = Mathf.Max(GameConstants.MIN_DAMAGE, atk - (int)(def * GameConstants.DEFENSE_REDUCTION_RATE));
            int variance = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(baseDmg * GameConstants.DAMAGE_VARIANCE_RATE));
            return Mathf.Max(GameConstants.MIN_DAMAGE, baseDmg + Random.Range(-variance, variance + 1));
        }

        /// <summary>计算玩家伤害（含装备伤害波动效果）</summary>
        private int CalcPlayerDamage(int atk, int def)
        {
            int baseDmg = Mathf.Max(GameConstants.MIN_DAMAGE, atk - (int)(def * GameConstants.DEFENSE_REDUCTION_RATE));
            float varianceRate = GetEquipDamageVarianceRate();
            if (varianceRate > 0f)
            {
                int variance = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(baseDmg * varianceRate));
                return Mathf.Max(GameConstants.MIN_DAMAGE, baseDmg + Random.Range(-variance, variance + 1));
            }
            int normalVariance = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(baseDmg * GameConstants.DAMAGE_VARIANCE_RATE));
            return Mathf.Max(GameConstants.MIN_DAMAGE, baseDmg + Random.Range(-normalVariance, normalVariance + 1));
        }

        // ==================== 状态效果系统 ====================

        /// <summary>
        /// 施加状态效果。叠加规则：同类型效果刷新duration取max
        /// </summary>
        /// <param name="isPlayer">true=玩家，false=怪物</param>
        public void ApplyEffect(bool isPlayer, EffectType type, int value, int duration, int sourceId = 0)
        {
            var effects = isPlayer ? CurrentBattle.playerEffects : CurrentBattle.monsterEffects;
            var existing = effects.Find(e => e.type == type);
            if (existing != null)
            {
                // 叠加规则：同类型刷新duration取max，数值取最新
                existing.duration = Mathf.Max(existing.duration, duration);
                existing.value = value;
                existing.sourceId = sourceId;
            }
            else
            {
                effects.Add(new StatusEffect(type, value, duration, sourceId));
            }

            string targetName = isPlayer ? "你" : CurrentBattle.monster.name;
            CurrentBattle.log.Add($"{targetName}受到{new StatusEffect(type, value, duration).GetName()}效果({value}点/持续{duration}回合)");
        }

        /// <summary>
        /// 移除指定类型的状态效果
        /// </summary>
        public void RemoveEffect(bool isPlayer, EffectType type)
        {
            var effects = isPlayer ? CurrentBattle.playerEffects : CurrentBattle.monsterEffects;
            var effect = effects.Find(e => e.type == type);
            if (effect != null)
            {
                string targetName = isPlayer ? "你" : CurrentBattle.monster.name;
                CurrentBattle.log.Add($"{targetName}的{effect.GetName()}效果消失了");
                effects.Remove(effect);
            }
        }

        /// <summary>
        /// 每回合Tick所有状态效果：触发DoT伤害、duration--、过期移除
        /// </summary>
        public void TickEffects(bool isPlayer)
        {
            var effects = isPlayer ? CurrentBattle.playerEffects : CurrentBattle.monsterEffects;
            string targetName = isPlayer ? "你" : CurrentBattle.monster.name;

            // 反向遍历，安全删除
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];

                // DoT效果：流血、中毒和燃烧
                if (effect.type == EffectType.Bleed || effect.type == EffectType.Poison || effect.type == EffectType.Burn)
                {
                    int dotDmg = Mathf.Max(GameConstants.MIN_DAMAGE, effect.value);
                    if (isPlayer)
                    {
                        var p = GameManager.Instance.Player;
                        p.hp = Mathf.Max(0, p.hp - dotDmg);
                    }
                    else
                    {
                        CurrentBattle.monster.hp = Mathf.Max(0, CurrentBattle.monster.hp - dotDmg);
                    }
                    string dotName = effect.type == EffectType.Bleed ? "流血" : effect.type == EffectType.Burn ? "燃烧" : "中毒";
                    CurrentBattle.log.Add($"{targetName}因{dotName}损失{dotDmg}HP");
                }

                // duration递减
                effect.duration--;

                // 过期移除
                if (effect.duration <= 0)
                {
                    CurrentBattle.log.Add($"{targetName}的{effect.GetName()}效果消失了");
                    effects.RemoveAt(i);
                }
            }
        }

        /// <summary>检查目标是否有指定状态效果</summary>
        public bool HasEffect(bool isPlayer, EffectType type)
        {
            var effects = isPlayer ? CurrentBattle.playerEffects : CurrentBattle.monsterEffects;
            return effects.Exists(e => e.type == type);
        }

        /// <summary>获取玩家有效攻击力 = 基础 + 旧buff + 状态效果修正</summary>
        public int GetEffectiveATK(bool isPlayer)
        {
            if (isPlayer)
            {
                var p = GameManager.Instance.Player;
                int baseVal = p.attack + (CurrentBattle?.playerBuffAttack ?? 0) + GetRageBonus(p.attack);
                int bonus = GetEffectModifier(isPlayer, EffectType.BuffATK);
                // 兴奋剂：ATK+20%
                int atkBuff = 0;
                if (p.atkBuffBattles > 0)
                    atkBuff = (int)(baseVal * 0.20f);
                return baseVal + bonus + atkBuff;
            }
            else
            {
                var m = CurrentBattle?.monster;
                if (m == null) return 0;
                return m.attack + m.buffAttack + GetEffectModifier(isPlayer, EffectType.BuffATK);
            }
        }

        /// <summary>获取有效防御力 = 基础 + 旧buff + 状态效果修正</summary>
        public int GetEffectiveDEF(bool isPlayer)
        {
            if (isPlayer)
            {
                var p = GameManager.Instance.Player;
                return p.defense + (CurrentBattle?.playerBuffDefense ?? 0) + GetEffectModifier(isPlayer, EffectType.BuffDEF);
            }
            else
            {
                var m = CurrentBattle?.monster;
                if (m == null) return 0;
                return m.defense + m.buffDefense + GetEffectModifier(isPlayer, EffectType.BuffDEF);
            }
        }

        /// <summary>获取有效速度（委托给GetEffectivePlayerSpeed/GetEffectiveMonsterSpeed，已包含状态效果修正）</summary>
        public int GetEffectiveSPD(bool isPlayer)
        {
            return isPlayer ? GetEffectivePlayerSpeed() : GetEffectiveMonsterSpeed();
        }

        /// <summary>汇总指定类型Buff修正值</summary>
        public int GetEffectModifier(bool isPlayer, EffectType type)
        {
            var effects = isPlayer ? CurrentBattle.playerEffects : CurrentBattle.monsterEffects;
            int total = 0;
            foreach (var e in effects)
            {
                if (e.type == type) total += e.value;
            }
            return total;
        }

        /// <summary>
        /// 获取Vulnerable（易伤）加成倍率
        /// </summary>
        public float GetVulnerableMultiplier(bool isPlayer)
        {
            if (HasEffect(isPlayer, EffectType.Vulnerable))
            {
                int vulnValue = GetEffectModifier(isPlayer, EffectType.Vulnerable);
                return 1f + vulnValue / 100f;
            }
            return 1f;
        }

        /// <summary>
        /// 根据技能配置计算状态效果的数值
        /// 流血/中毒：value = ATK * statusPowerRate
        /// 易伤：value = (int)(statusPowerRate * 100)，即30表示受伤+30%
        /// 眩晕/沉默：value = 0（无数值，只需duration）
        /// </summary>
        private int CalcStatusEffectValue(SkillConfig skill, int attackerAtk)
        {
            if (skill.statusEffect == EffectType.Bleed || skill.statusEffect == EffectType.Poison || skill.statusEffect == EffectType.Burn)
            {
                // DoT效果：每回合伤害 = 攻击力 * 倍率
                return Mathf.Max(GameConstants.MIN_DAMAGE, (int)(attackerAtk * skill.statusPowerRate));
            }
            if (skill.statusEffect == EffectType.Vulnerable)
            {
                // 易伤：statusPowerRate 0.3 = 30%受伤增加
                return (int)(skill.statusPowerRate * 100);
            }
            // 眩晕/沉默等无数值效果
            return 0;
        }
    }
}
