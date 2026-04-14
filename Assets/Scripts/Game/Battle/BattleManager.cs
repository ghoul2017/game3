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

        public void StartBattle(BattleData battleData)
        {
            CurrentBattle = battleData;
            Debug.Log($"[战斗] 开始战斗 怪物={battleData.monster.name} HP={battleData.monster.hp}");
            CurrentBattle.isPlayerTurn = true;
            CurrentBattle.isFinished = false;
            CurrentBattle.isVictory = false;
            CurrentBattle.playerBuffAttack = 0;
            CurrentBattle.playerBuffDefense = 0;
            CurrentBattle.playerBuffTurns = 0;
            CurrentBattle.log.Clear();
            CurrentBattle.log.Add($"遭遇了 {battleData.monster.name}！");
        }

        public string PlayerAttack()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            if (b == null || b.isFinished) return "";

            int totalAtk = p.attack + b.playerBuffAttack;
            int dmg = CalcDamage(totalAtk, b.monster.defense + b.monster.buffDefense);
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
            if (p.mp < skill.mpCost)
            {
                string errMsg = "法力不足！";
                b.log.Add(errMsg);
                return errMsg;
            }

            p.mp -= skill.mpCost;
            string msg;

            if (skill.type == SkillType.Passive)
            {
                msg = $"{skill.name} 是被动技能，无法主动使用";
                b.log.Add(msg);
                return msg;
            }

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
                        if (buffCfg.effectType == 0 || buffCfg.effectType == 2)
                            b.playerBuffAttack = p.attack * buffCfg.effectValue / 100;
                        if (buffCfg.effectType == 1 || buffCfg.effectType == 2)
                            b.playerBuffDefense = p.defense * buffCfg.effectValue / 100;
                        b.playerBuffTurns = skill.buffTurns;
                        msg = $"你使用了 {skill.name}！攻击+{b.playerBuffAttack} 防御+{b.playerBuffDefense}，持续{skill.buffTurns}回合";
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

            // 主动攻击技能
            int totalAtk = p.attack + b.playerBuffAttack;
            int dmg = CalcDamage((int)(totalAtk * skill.power / 100f), b.monster.defense + b.monster.buffDefense);
            b.monster.hp -= dmg;
            msg = $"你使用 {skill.name}，造成 {dmg} 点伤害";

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

            var m = b.monster;
            int monsterAtk = m.attack + m.buffAttack;
            int playerDef = p.defense + b.playerBuffDefense;

            // 怪物有技能时，根据概率使用
            if (m.skillIds.Count > 0 && Random.value < GameConstants.MONSTER_SKILL_CHANCE)
            {
                int sid = m.skillIds[Random.Range(0, m.skillIds.Count)];
                var skill = ConfigManager.Instance.GetSkill(sid);
                if (skill != null)
                {
                    int dmg = CalcDamage((int)(monsterAtk * skill.power / 100f), playerDef);
                    p.hp -= dmg;
                    if (p.hp < 0) p.hp = 0;
                    string msg = $"{m.name} 使用 {skill.name}，对你造成 {dmg} 点伤害";
                    b.log.Add(msg);

                    DecayBuffs(b);

                    if (p.hp <= 0) return msg + "\n" + OnDefeat();
                    return msg;
                }
            }

            // 普通攻击
            int normalDmg = CalcDamage(monsterAtk, playerDef);
            p.hp -= normalDmg;
            if (p.hp < 0) p.hp = 0;
            string normalMsg = $"{m.name} 攻击了你，造成 {normalDmg} 点伤害";
            b.log.Add(normalMsg);

            DecayBuffs(b);

            if (p.hp <= 0) return normalMsg + "\n" + OnDefeat();
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
                    b.log.Add("你的增益效果消失了");
                }
            }
        }

        public string TryEscape()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            if (b == null || b.isFinished) return "";

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
            b.isFinished = true;
            b.isVictory = false;
            b.playerBuffAttack = 0;
            b.playerBuffDefense = 0;
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
    }
}
