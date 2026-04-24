using System.Collections.Generic;
using Game.Config;
using Game.Core;
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

        // ---- 被动技能辅助方法 ----

        private bool HasPassive(int skillId)
        {
            var p = GameManager.Instance.Player;
            return p.equippedSkills.Contains(skillId);
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

        /// <summary>复活：死亡时复活一次，恢复30%HP</summary>
        private bool TryRevive()
        {
            if (!HasPassive(4004) || reviveUsed) return false;
            var p = GameManager.Instance.Player;
            reviveUsed = true;
            int reviveHp = Mathf.Max(1, p.maxHp * 30 / 100);
            p.hp = reviveHp;
            CurrentBattle.log.Add($"【复活】被动技能触发！恢复 {reviveHp} 生命");
            return true;
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
            Debug.Log($"[战斗] 开始战斗 怪物={battleData.monster.name} HP={battleData.monster.hp}");
            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Attack);
            CurrentBattle.isFinished = false;
            CurrentBattle.isVictory = false;
            CurrentBattle.playerBuffAttack = 0;
            CurrentBattle.playerBuffDefense = 0;
            CurrentBattle.playerBuffSpeed = 0;
            CurrentBattle.playerBuffTurns = 0;
            CurrentBattle.playerEffects.Clear();
            CurrentBattle.monsterEffects.Clear();
            CurrentBattle.log.Clear();
            CurrentBattle.log.Add($"遭遇了 {battleData.monster.name}！");
            reviveUsed = false;

            // 根据速度判定先手
            DetermineTurnOrder();
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

        /// <summary>获取玩家有效速度 = 基础速度 + 速度buff</summary>
        public int GetEffectivePlayerSpeed()
        {
            var p = GameManager.Instance.Player;
            return p.speed + (CurrentBattle?.playerBuffSpeed ?? 0);
        }

        /// <summary>获取怪物有效速度 = 基础速度 + 速度buff</summary>
        public int GetEffectiveMonsterSpeed()
        {
            var m = CurrentBattle?.monster;
            return m != null ? m.speed + m.buffSpeed : 0;
        }

        public string PlayerAttack()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            if (b == null || b.isFinished) return "";

            // 回合开始：Tick状态效果
            TickEffects(true);

            // 检查玩家是否死亡（DoT致死）
            if (p.hp <= 0)
            {
                if (TryRevive()) { /* 复活成功，继续行动 */ }
                else { return OnDefeat(); }
            }

            // 检查眩晕：跳过行动（TickEffects自然管理duration，不手动移除）
            if (HasEffect(true, EffectType.Stun))
            {
                b.log.Add("你因眩晕无法行动！");
                return "你因眩晕无法行动！";
            }

            // 魔力涌动：每回合回蓝
            ApplyManaSurge();

            // 攻击音效
            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Attack);

            // 速度差闪避判定：怪物闪避
            if (TrySpeedDodge(true))
            {
                string dodgeMsg = $"{b.monster.name}闪避了攻击！";
                b.log.Add(dodgeMsg);
                return dodgeMsg;
            }

            // 狂战之怒：HP越低攻击越高
            int totalAtk = p.attack + b.playerBuffAttack + GetRageBonus(p.attack);
            int rawDmg = CalcDamage(totalAtk, b.monster.defense + b.monster.buffDefense);
            float monsterVuln = GetVulnerableMultiplier(false); // 怪物易伤加成
            int dmg = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(rawDmg * monsterVuln));
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

            b.log.Add(msg);

            if (b.monster.hp <= 0)
            {
                b.monster.hp = 0;
                return msg + "\n" + OnVictory();
            }
            return msg;
        }

        public string PlayerSkill(int skillId)
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            var skill = ConfigManager.Instance.GetSkill(skillId);
            if (b == null || b.isFinished || skill == null) return "";

            // 回合开始：Tick状态效果
            TickEffects(true);

            // 检查玩家是否死亡（DoT致死）
            if (p.hp <= 0)
            {
                if (TryRevive()) { /* 复活成功 */ }
                else { return OnDefeat(); }
            }

            // 检查眩晕：跳过行动（TickEffects自然管理duration，不手动移除）
            if (HasEffect(true, EffectType.Stun))
            {
                b.log.Add("你因眩晕无法行动！");
                return "你因眩晕无法行动！";
            }

            // 检查沉默：无法使用技能
            if (HasEffect(true, EffectType.Silence))
            {
                string silenceMsg = "你处于沉默状态，无法使用技能！";
                b.log.Add(silenceMsg);
                return silenceMsg;
            }

            if (p.mp < skill.mpCost)
            {
                string errMsg = "法力不足！";
                b.log.Add(errMsg);
                return errMsg;
            }

            // 魔力涌动：每回合回蓝
            ApplyManaSurge();

            p.mp -= skill.mpCost;
            string msg;

            if (skill.type == SkillType.Passive)
            {
                msg = $"{skill.name} 是被动技能，无法主动使用";
                b.log.Add(msg);
                return msg;
            }

            // 技能音效（仅在可使用的主动技能时播放）
            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Skill);

            if (skill.type == SkillType.Buff)
            {
                // 对自己施加buff
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
                        // 速度buff改变后立即重新判定回合顺序
                        if (b.playerBuffSpeed != 0)
                            DetermineTurnOrder();
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

            // 治疗技能：恢复自身HP
            if (skill.isHeal)
            {
                int healAmount = (int)(p.attack * skill.power / 100f) + p.level * 5;
                int actualHeal = Mathf.Min(healAmount, p.maxHp - p.hp);
                p.hp += actualHeal;
                msg = $"你使用 {skill.name}，恢复 {actualHeal} 点生命";
                b.log.Add(msg);
                // 治疗也触发怪物回合
                return msg;
            }

            // 主动攻击技能 / 状态攻击技能
            int totalAtk = p.attack + b.playerBuffAttack + GetRageBonus(p.attack);
            int skillRawDmg = CalcDamage((int)(totalAtk * skill.power / 100f), b.monster.defense + b.monster.buffDefense);
            float skillVuln = GetVulnerableMultiplier(false); // 怪物易伤加成
            int dmg = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(skillRawDmg * skillVuln));
            b.monster.hp -= dmg;
            msg = $"你使用 {skill.name}，造成 {dmg} 点伤害";

            // Debuff效果：降低敌人攻击/防御/速度
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
                    // 速度debuff后立即重新判定回合顺序
                    DetermineTurnOrder();
                }
            }

            // 状态攻击技能：附加状态效果
            if (skill.type == SkillType.StatusAttack && skill.statusDuration > 0)
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

            b.log.Add(msg);

            if (b.monster.hp <= 0)
            {
                b.monster.hp = 0;
                return msg + "\n" + OnVictory();
            }
            return msg;
        }

        public string PlayerUseItem(int itemId)
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            var item = ConfigManager.Instance.GetItem(itemId);
            if (b == null || b.isFinished || item == null) return "";

            // 回合开始：Tick状态效果
            TickEffects(true);

            // 检查玩家是否死亡（DoT致死）
            if (p.hp <= 0)
            {
                if (TryRevive()) { /* 复活成功 */ }
                else { return OnDefeat(); }
            }

            // 检查眩晕：跳过行动（TickEffects自然管理duration，不手动移除）
            if (HasEffect(true, EffectType.Stun))
            {
                b.log.Add("你因眩晕无法行动！");
                return "你因眩晕无法行动！";
            }

            // 魔力涌动：每回合回蓝
            ApplyManaSurge();

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

            p.bagItems.Remove(itemId);
            return $"使用了 {item.name}";
        }

        public string MonsterTurn()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            if (b == null || b.isFinished) return "";

            // 回合开始：Tick怪物状态效果
            TickEffects(false);

            // 检查怪物是否死亡（DoT致死）
            if (b.monster.hp <= 0)
            {
                b.monster.hp = 0;
                return OnVictory();
            }

            // 检查怪物眩晕：跳过行动
            if (HasEffect(false, EffectType.Stun))
            {
                string stunMsg = $"{b.monster.name}因眩晕无法行动！";
                b.log.Add(stunMsg);
                // TickEffects自然管理duration，不手动RemoveEffect
                DecayBuffs(b);
                return stunMsg;
            }

            var m = b.monster;
            int monsterAtk = m.attack + m.buffAttack;
            int playerDef = p.defense + b.playerBuffDefense;
            float dmgMultiplier = GetSteadfastMultiplier(); // 坚守减伤
            float vulnerableMult = GetVulnerableMultiplier(true); // 玩家易伤加成
            dmgMultiplier *= vulnerableMult;

            // 受击音效
            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Hit);

            // 速度差闪避判定：玩家闪避
            if (TrySpeedDodge(false))
            {
                string dodgeMsg = "你闪避了攻击！";
                b.log.Add(dodgeMsg);
                DecayBuffs(b);
                return dodgeMsg;
            }

            // 怪物有技能时，根据概率使用
            if (m.skillIds.Count > 0 && Random.value < GameConstants.MONSTER_SKILL_CHANCE)
            {
                int sid = m.skillIds[Random.Range(0, m.skillIds.Count)];
                var skill = ConfigManager.Instance.GetSkill(sid);
                if (skill != null)
                {
                    int rawDmg = CalcDamage((int)(monsterAtk * skill.power / 100f), playerDef);
                    int dmg = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(rawDmg * dmgMultiplier));
                    p.hp -= dmg;
                    if (p.hp < 0) p.hp = 0;
                    string msg = $"{m.name} 使用 {skill.name}，对你造成 {dmg} 点伤害";
                    if (dmgMultiplier < 1f) msg += "（坚守减伤）";

                    // 状态攻击技能：怪物对玩家附加状态效果
                    if (skill.type == SkillType.StatusAttack && skill.statusDuration > 0)
                    {
                        int effectValue = CalcStatusEffectValue(skill, monsterAtk);
                        ApplyEffect(true, skill.statusEffect, effectValue, skill.statusDuration, skill.id);
                        msg += $"，附加{new StatusEffect(skill.statusEffect, effectValue, skill.statusDuration).GetName()}效果";
                    }

                    b.log.Add(msg);

                    DecayBuffs(b);

                    if (p.hp <= 0)
                    {
                        if (TryRevive()) return msg; // 复活
                        return msg + "\n" + OnDefeat();
                    }
                    return msg;
                }
            }

            // 普通攻击
            int rawNormalDmg = CalcDamage(monsterAtk, playerDef);
            int normalDmg = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(rawNormalDmg * dmgMultiplier));
            p.hp -= normalDmg;
            if (p.hp < 0) p.hp = 0;
            string normalMsg = $"{m.name} 攻击了你，造成 {normalDmg} 点伤害";
            if (dmgMultiplier < 1f) normalMsg += "（坚守减伤）";
            b.log.Add(normalMsg);

            DecayBuffs(b);

            if (p.hp <= 0)
            {
                if (TryRevive()) return normalMsg; // 复活
                return normalMsg + "\n" + OnDefeat();
            }
            return normalMsg;
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
                    // buff消失后重新判定回合顺序
                    DetermineTurnOrder();
                }
            }
        }

        public string TryEscape()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            if (b == null || b.isFinished) return "";

            // 回合开始：Tick状态效果
            TickEffects(true);

            // 检查玩家是否死亡（DoT致死）
            if (p.hp <= 0)
            {
                if (TryRevive()) { /* 复活成功 */ }
                else { return OnDefeat(); }
            }

            // 检查眩晕：无法逃跑（TickEffects自然管理duration，不手动移除）
            if (HasEffect(true, EffectType.Stun))
            {
                b.log.Add("你因眩晕无法行动！");
                return "你因眩晕无法行动！";
            }

            // 魔力涌动：每回合回蓝
            ApplyManaSurge();

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
            return failMsg + "\n" + MonsterTurn();
        }

        private string OnVictory()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            var m = b.monster;

            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Victory);

            b.isFinished = true;
            b.isVictory = true;
            if (b.room != null) b.room.cleared = true;

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

            p.AddExp(expReward);
            p.gold += goldReward;

            // 存储奖励数据
            b.rewardExp = expReward;
            b.rewardGold = goldReward;
            b.rewardItems.Clear();

            Debug.Log($"[战斗] 胜利！怪物={m.name} 经验={expReward} 金币={goldReward}");
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

            b.log.Add(msg);
            return msg;
        }

        private string OnDefeat()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;

            Core.AudioManager.Instance?.PlaySFX(Core.AudioClipType.Battle_Defeat);

            b.isFinished = true;
            b.isVictory = false;
            b.playerBuffAttack = 0;
            b.playerBuffDefense = 0;
            b.playerBuffSpeed = 0;
            b.playerBuffTurns = 0;

            // 失败惩罚：损失金币
            int lostGold = (int)(p.gold * GameConstants.DEFEAT_GOLD_LOSS_RATE);
            Debug.Log($"[战斗] 失败！损失金币={lostGold}");
            p.gold -= lostGold;
            p.dungeonFloor = Mathf.Max(1, p.dungeonFloor - 1);
            p.FullRestore();

            string msg = $"你被击败了！损失 {lostGold} 金币，返回城镇";
            b.log.Add(msg);
            return msg;
        }

        private int CalcDamage(int atk, int def)
        {
            int baseDmg = Mathf.Max(GameConstants.MIN_DAMAGE, atk - def / GameConstants.DEFENSE_DIVISOR);
            int variance = Mathf.Max(GameConstants.MIN_DAMAGE, (int)(baseDmg * GameConstants.DAMAGE_VARIANCE_RATE));
            return Mathf.Max(GameConstants.MIN_DAMAGE, baseDmg + Random.Range(-variance, variance + 1));
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

                // DoT效果：流血和中毒
                if (effect.type == EffectType.Bleed || effect.type == EffectType.Poison)
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
                    string dotName = effect.type == EffectType.Bleed ? "流血" : "中毒";
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
                return baseVal + bonus;
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

        /// <summary>获取有效速度 = 基础 + 旧buff + 状态效果修正</summary>
        public int GetEffectiveSPD(bool isPlayer)
        {
            if (isPlayer)
            {
                return GetEffectivePlayerSpeed() + GetEffectModifier(isPlayer, EffectType.BuffSpeed);
            }
            else
            {
                return GetEffectiveMonsterSpeed() + GetEffectModifier(isPlayer, EffectType.BuffSpeed);
            }
        }

        /// <summary>汇总指定类型Buff修正值</summary>
        private int GetEffectModifier(bool isPlayer, EffectType type)
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
            if (skill.statusEffect == EffectType.Bleed || skill.statusEffect == EffectType.Poison)
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
