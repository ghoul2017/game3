namespace Game.Config
{
    /// <summary>
    /// 游戏常量定义 - 集中管理魔法数字和配置值
    /// </summary>
    public static class GameConstants
    {
        // ==================== 战斗系统 ====================
        
        /// <summary>怪物使用技能的触发概率</summary>
        public const float MONSTER_SKILL_CHANCE = 0.3f;
        
        /// <summary>普通逃跑成功率</summary>
        public const float ESCAPE_CHANCE = 0.5f;
        
        /// <summary>失败时损失金币的比例 (1/10 = 10%)</summary>
        public const float DEFEAT_GOLD_LOSS_RATE = 0.1f;
        
        /// <summary>伤害浮动比例</summary>
        public const float DAMAGE_VARIANCE_RATE = 0.1f;
        
        /// <summary>最小伤害值</summary>
        public const int MIN_DAMAGE = 1;
        
        /// <summary>防御减伤系数 (除以2)</summary>
        public const int DEFENSE_DIVISOR = 2;

        /// <summary>速度差闪避阈值：速度差>=此值时触发闪避</summary>
        public const int SPEED_DODGE_THRESHOLD = 5;

        /// <summary>速度差闪避率系数：闪避率 = (speedDiff * 此值)%</summary>
        public const int SPEED_DODGE_RATE_PER_POINT = 2;

        // ==================== 地牢系统 ====================
        
        /// <summary>宝箱触发概率</summary>
        public const float TREASURE_CHANCE = 0.4f;
        
        /// <summary>消耗品掉落概率</summary>
        public const float CONSUMABLE_DROP_CHANCE = 0.3f;
        
        /// <summary>事件触发概率</summary>
        public const float EVENT_CHANCE = 0.2f;
        
        /// <summary>房间最小数量</summary>
        public const int MIN_ROOMS = 5;
        
        /// <summary>房间最大数量</summary>
        public const int MAX_ROOMS = 15;
        
        /// <summary>Boss房间位置比例</summary>
        public const float BOSS_ROOM_POSITION = 0.8f;

        // ==================== 物品系统 ====================
        
        /// <summary>物品掉落基础概率</summary>
        public const float ITEM_DROP_BASE_CHANCE = 0.3f;

        // ==================== 宝物效果 ====================
        
        /// <summary>宝物ID: 吸血之牙</summary>
        public const int TREASURE_ID_LIFESTEAL = 4;
        
        /// <summary>宝物ID: 幸运硬币</summary>
        public const int TREASURE_ID_LUCKY_COIN = 5;
        
        /// <summary>宝物ID: 经验宝石</summary>
        public const int TREASURE_ID_EXP_GEM = 6;

        // ==================== UI系统 ====================

        /// <summary>战斗日志最大行数</summary>
        public const int MAX_BATTLE_LOG_LINES = 6;

        /// <summary>技能栏最大数量</summary>
        public const int MAX_SKILL_SLOTS = 4;

        /// <summary>宝物栏最大数量</summary>
        public const int MAX_TREASURE_SLOTS = 6;

        /// <summary>装备栏最大数量</summary>
        public const int MAX_EQUIP_SLOTS = 3;

        // ==================== Boss系统 ====================

        /// <summary>Boss战额外金币倍率：floor * 此值</summary>
        public const int BOSS_GOLD_FLOOR_MULTIPLIER = 100;

        /// <summary>Boss战额外经验倍率：floor * 此值</summary>
        public const int BOSS_EXP_FLOOR_MULTIPLIER = 50;
    }
}
