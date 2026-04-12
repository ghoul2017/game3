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
            CurrentBattle.isPlayerTurn = true;
            CurrentBattle.isFinished = false;
            CurrentBattle.isVictory = false;
            CurrentBattle.log.Clear();
            CurrentBattle.log.Add($"遭遇了 {battleData.monster.name}！");
        }

        public string PlayerAttack()
        {
            var b = CurrentBattle;
            var p = GameManager.Instance.Player;
            if (b == null || b.isFinished) return "";

            int dmg = CalcDamage(p.attack, b.monster.defense + b.monster.buffDefense);
            b.monster.hp -= dmg;
            string msg = $"你攻击了 {b.monster.name}，造成 {dmg} 点伤害";
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
                string msg = "法力不足！";
                b.log.Add(msg);
                return msg;
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
                msg = $"你使用了 {skill.name}！";
                b.log.Add(msg);
                return msg;
            }

            // 主动攻击技能
            int dmg = CalcDamage((int)(p.attack * skill.power / 100f), b.monster.defense + b.monster.buffDefense);
            b.monster.hp -= dmg;
            msg = $"你使用 {skill.name}，造成 {dmg} 点伤害";
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
            int totalAtk = m.attack + m.buffAttack;

            // 怪物有技能时，30%概率使用
            if (m.skillIds.Count > 0 && Random.value < 0.3f)
            {
                int sid = m.skillIds[Random.Range(0, m.skillIds.Count)];
                var skill = ConfigManager.Instance.GetSkill(sid);
                if (skill != null)
                {
                    int dmg = CalcDamage((int)(totalAtk * skill.power / 100f), p.defense);
                    p.hp -= dmg;
                    if (p.hp < 0) p.hp = 0;
                    string msg = $"{m.name} 使用 {skill.name}，对你造成 {dmg} 点伤害";
                    b.log.Add(msg);
                    if (p.hp <= 0) return msg + "\n" + OnDefeat();
                    return msg;
                }
            }

            // 普通攻击
            int normalDmg = CalcDamage(totalAtk, p.defense);
            p.hp -= normalDmg;
            if (p.hp < 0) p.hp = 0;
            string normalMsg = $"{m.name} 攻击了你，造成 {normalDmg} 点伤害";
            b.log.Add(normalMsg);

            // 处理怪物buff衰减
            if (m.buffTurns > 0)
            {
                m.buffTurns--;
                if (m.buffTurns <= 0)
                {
                    m.buffAttack = 0;
                    m.buffDefense = 0;
                }
            }

            if (p.hp <= 0) return normalMsg + "\n" + OnDefeat();
            return normalMsg;
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

            if (Random.value < 0.5f)
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

            p.AddExp(m.expReward);
            p.gold += m.goldReward;

            string msg = $"击败了 {m.name}！获得 {m.expReward} 经验，{m.goldReward} 金币";

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

            // 失败惩罚：损失10%金币
            int lostGold = p.gold / 10;
            p.gold -= lostGold;
            p.dungeonFloor = Mathf.Max(1, p.dungeonFloor - 1);
            p.FullRestore();

            string msg = $"你被击败了！损失 {lostGold} 金币，返回城镇";
            b.log.Add(msg);
            return msg;
        }

        private int CalcDamage(int atk, int def)
        {
            int baseDmg = Mathf.Max(1, atk - def / 2);
            int variance = Mathf.Max(1, baseDmg / 10);
            return Mathf.Max(1, baseDmg + Random.Range(-variance, variance + 1));
        }
    }
}
