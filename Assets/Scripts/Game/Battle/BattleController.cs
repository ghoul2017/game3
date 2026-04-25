using System;
using System.Collections.Generic;
using Game.Config;
using Game.Core;
using Game.Data;

namespace Game.Battle
{
    /// <summary>
    /// 单轮回合结果数据
    /// </summary>
    public class RoundResult
    {
        /// <summary>是否玩家先手</summary>
        public bool isPlayerTurn;

        /// <summary>玩家行动日志（可能有多个条目）</summary>
        public List<string> playerActionLogs = new List<string>();

        /// <summary>怪物行动日志</summary>
        public List<string> monsterActionLogs = new List<string>();

        /// <summary>本回合新增的完整战斗日志条目</summary>
        public List<string> roundLogs = new List<string>();

        /// <summary>战斗是否在本回合结束</summary>
        public bool battleEnded;

        /// <summary>如果战斗结束，是否胜利</summary>
        public bool isVictory;

        /// <summary>玩家当前HP</summary>
        public int playerHp;

        /// <summary>玩家当前MP</summary>
        public int playerMp;

        /// <summary>玩家最大HP</summary>
        public int playerMaxHp;

        /// <summary>玩家最大MP</summary>
        public int playerMaxMp;

        /// <summary>怪物当前HP</summary>
        public int monsterHp;

        /// <summary>怪物最大HP</summary>
        public int monsterMaxHp;
    }

    /// <summary>
    /// 战斗结束结果数据
    /// </summary>
    public class BattleResult
    {
        /// <summary>是否胜利</summary>
        public bool isVictory;

        /// <summary>获得经验</summary>
        public int expReward;

        /// <summary>获得金币</summary>
        public int goldReward;

        /// <summary>获得物品名称列表</summary>
        public List<string> rewardItems = new List<string>();

        /// <summary>是否Boss战</summary>
        public bool isBossBattle;

        /// <summary>Boss击杀标识</summary>
        public string bossKillTitle;

        /// <summary>失败时损失金币</summary>
        public int lostGold;
    }

    /// <summary>
    /// 轻量战斗控制器：非MonoBehaviour单例 + 事件驱动
    /// 封装完整回合流程，不引用任何UI类
    /// </summary>
    public class BattleController
    {
        private static BattleController instance;
        public static BattleController Instance => instance ??= new BattleController();

        /// <summary>每回合完成时触发（无论战斗是否结束）</summary>
        public event Action<RoundResult> OnRoundComplete;

        /// <summary>战斗结束时触发（胜利或失败）</summary>
        public event Action<BattleResult> OnBattleEnd;

        /// <summary>记录每回合开始时的日志数量，用于提取本回合新增日志</summary>
        private int logCountAtRoundStart;

        private BattleController() { }

        /// <summary>
        /// 执行攻击回合：RoundStart -> 先手Action -> 后手Action -> RoundEnd -> 触发事件
        /// </summary>
        /// <returns>回合结果，战斗未开始时返回null</returns>
        public RoundResult ExecuteAttack()
        {
            return ExecuteRound(() =>
            {
                BattleManager.Instance.PlayerActionAttack();
            });
        }

        /// <summary>
        /// 执行技能回合：RoundStart -> 先手Action(技能) -> 后手Action -> RoundEnd -> 触发事件
        /// </summary>
        /// <param name="skillId">要使用的技能ID</param>
        /// <returns>回合结果，战斗未开始时返回null</returns>
        public RoundResult ExecuteSkill(int skillId)
        {
            return ExecuteRound(() =>
            {
                BattleManager.Instance.PlayerActionSkill(skillId);
            });
        }

        /// <summary>
        /// 执行物品使用回合：RoundStart -> 先手Action(物品) -> 后手Action -> RoundEnd -> 触发事件
        /// </summary>
        /// <param name="itemId">要使用的物品ID</param>
        /// <returns>回合结果，战斗未开始时返回null</returns>
        public RoundResult ExecuteItem(int itemId)
        {
            return ExecuteRound(() =>
            {
                BattleManager.Instance.PlayerActionUseItem(itemId);
            });
        }

        /// <summary>
        /// 执行逃跑：RoundStart -> 逃跑判定 -> 失败时怪物行动
        /// 逃跑不走标准RoundEnd，使用独立流程
        /// </summary>
        /// <returns>回合结果</returns>
        public RoundResult ExecuteEscape()
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return null;

            // 记录回合开始前的日志数量
            logCountAtRoundStart = b.log.Count;

            // 1. 回合开始：Tick双方状态效果 + 判定先手
            BattleManager.Instance.RoundStart();
            if (b.isFinished)
            {
                return BuildAndFireResult(b);
            }

            // 2. 逃跑判定
            BattleManager.Instance.ActionEscape();

            // 3. 逃跑失败时怪物行动（惩罚）
            if (!b.isFinished)
            {
                BattleManager.Instance.MonsterAction();
            }

            // 4. 回合结束（逃跑成功时不需要RoundEnd，但isFinished=true不影响）
            if (!b.isFinished)
            {
                BattleManager.Instance.RoundEnd();
            }

            return BuildAndFireResult(b);
        }

        /// <summary>
        /// 通用回合执行：RoundStart -> 先手Action -> 后手Action -> RoundEnd
        /// </summary>
        private RoundResult ExecuteRound(Action playerAction)
        {
            var b = BattleManager.Instance.CurrentBattle;
            if (b == null || b.isFinished) return null;

            // 记录回合开始前的日志数量
            logCountAtRoundStart = b.log.Count;

            // 1. 回合开始：Tick双方状态效果 + 判定先手
            BattleManager.Instance.RoundStart();
            if (b.isFinished)
            {
                return BuildAndFireResult(b);
            }

            // 2. 根据先手顺序执行行动
            if (b.isPlayerTurn)
            {
                // 玩家先手：玩家行动 -> 怪物行动
                playerAction();
                if (!b.isFinished) BattleManager.Instance.MonsterAction();
            }
            else
            {
                // 怪物先手：怪物行动 -> 玩家行动
                BattleManager.Instance.MonsterAction();
                if (!b.isFinished) playerAction();
            }

            // 3. 回合结束：衰减buff
            if (!b.isFinished) BattleManager.Instance.RoundEnd();

            return BuildAndFireResult(b);
        }

        /// <summary>
        /// 构建回合结果数据并触发事件
        /// </summary>
        private RoundResult BuildAndFireResult(BattleData b)
        {
            var p = GameManager.Instance.Player;
            var m = b.monster;

            var result = new RoundResult
            {
                isPlayerTurn = b.isPlayerTurn,
                battleEnded = b.isFinished,
                isVictory = b.isVictory,
                playerHp = p.hp,
                playerMp = p.mp,
                playerMaxHp = p.maxHp,
                playerMaxMp = p.maxMp,
                monsterHp = m.hp,
                monsterMaxHp = m.maxHp,
            };

            // 提取本回合新增的日志条目
            int totalLogs = b.log.Count;
            for (int i = logCountAtRoundStart; i < totalLogs; i++)
            {
                string entry = b.log[i];
                result.roundLogs.Add(entry);

                // 简单分类：包含"你"开头的且不是怪物行动的归入玩家行动日志
                // 怪物行动日志包含怪物名或"对你"/"攻击了你"
                if (IsPlayerActionLog(entry, m.name))
                    result.playerActionLogs.Add(entry);
                else
                    result.monsterActionLogs.Add(entry);
            }

            // 触发回合完成事件
            OnRoundComplete?.Invoke(result);

            // 战斗结束时触发战斗结束事件
            if (b.isFinished)
            {
                var battleResult = BuildBattleResult(b, p);
                OnBattleEnd?.Invoke(battleResult);
            }

            return result;
        }

        /// <summary>
        /// 构建战斗结束结果数据
        /// </summary>
        private BattleResult BuildBattleResult(BattleData b, PlayerData p)
        {
            var result = new BattleResult
            {
                isVictory = b.isVictory,
                expReward = b.rewardExp,
                goldReward = b.rewardGold,
                isBossBattle = b.isBossBattle,
                bossKillTitle = b.bossKillTitle,
            };

            // 复制奖励物品列表
            foreach (var itemName in b.rewardItems)
                result.rewardItems.Add(itemName);

            // 失败时计算损失金币
            if (!b.isVictory)
            {
                result.lostGold = (int)(p.gold * GameConstants.DEFEAT_GOLD_LOSS_RATE);
            }

            return result;
        }

        /// <summary>
        /// 判断日志条目是否属于玩家行动（用于分类）
        /// 规则：以"你"开头且不含怪物名的行动属于玩家行动
        /// </summary>
        private bool IsPlayerActionLog(string entry, string monsterName)
        {
            if (string.IsNullOrEmpty(entry)) return false;
            // 怪物行动特征：包含怪物名+"攻击了"、怪物名+"使用"、包含"对你造成"
            if (entry.Contains(monsterName + " 攻击了") ||
                entry.Contains(monsterName + " 使用") ||
                entry.Contains("对你造成"))
                return false;
            // 怪物自身状态：怪物名+因/受到/的...效果
            if (entry.Contains(monsterName + "因") ||
                entry.Contains(monsterName + "受到"))
                return false;
            // 玩家行动特征：以"你"开头
            if (entry.StartsWith("你"))
                return true;
            // 回合标记/速度信息/系统消息属于玩家
            if (entry.StartsWith("[速度]") || entry.StartsWith("[回合]"))
                return true;
            // 默认归入怪物行动日志
            return false;
        }
    }
}
