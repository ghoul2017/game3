namespace Game.Config
{
    public enum Profession
    {
        Warrior,  // 战士
        Ranger,   // 游侠
        Knight,   // 骑士
        Priest,   // 牧师
        Mage      // 法师
    }

    public enum ItemType
    {
        Weapon,
        Armor,
        Accessory,
        Consumable,
        Treasure
    }

    public enum SkillType
    {
        Active,        // 攻击技能
        Passive,       // 被动技能
        Buff,          // 增益技能
        StatusAttack,  // 带状态效果的攻击技能（控制类）
        Heal,          // 治疗技能
        Debuff,        // 减益技能（降攻/降防/减速）
        DoT,           // 持续伤害技能（流血/中毒/燃烧）
        Control        // 控制技能（眩晕/冰冻/恐惧）
    }

    /// <summary>
    /// 技能学习面板的标签页分类
    /// </summary>
    public enum SkillTab
    {
        All,       // 全部
        Attack,    // 攻击
        Heal,      // 治疗
        Buff,      // Buff
        Control,   // 控制
        Passive,   // 被动
        Learned    // 已学
    }

    public enum DungeonEventType
    {
        Empty,
        Monster,
        Elite,
        Boss,
        Treasure,
        Spring,
        Merchant,
        Exit,
        Shrine
    }

    /// <summary>
    /// 怪物AI行为模式
    /// </summary>
    public enum MonsterAIPattern
    {
        Aggressive,  // 狂暴：偏好高伤技能
        Defensive,   // 防御：HP低时优先治疗/防御
        Debuffer,    // 控制：优先上debuff
        Balanced,    // 均衡：随机合理决策
        Boss         // 多阶段：根据HP切换策略
    }

    /// <summary>
    /// 装备特殊效果类型
    /// </summary>
    public enum EquipEffectType
    {
        None,              // 无特殊效果
        CritChance,        // 暴击率
        Dodge,             // 闪避
        Lifesteal,         // 吸血（造成伤害时恢复 effectValue% 的生命）
        Thorns,            // 反伤（受到攻击时反弹 effectValue% 受到的伤害）
        SkillCostReduce,   // 技能减耗（技能消耗减少 effectValue%）
        BossDamageBonus,   // Boss增伤（对Boss伤害增加 effectValue%）
        DamageVariance,    // 伤害波动（伤害在 effectValue% 范围内随机浮动）
        AutoRevive         // 自动复活（死亡时复活一次，恢复 effectValue% HP，战斗中生效后销毁）
    }

    /// <summary>
    /// 地牢网格瓦片类型
    /// </summary>
    public enum TileType
    {
        Room,  // 可通行房间
        Wall   // 不可通行墙壁
    }

    /// <summary>
    /// 战争迷雾可见性状态
    /// </summary>
    public enum TileVisibility
    {
        Hidden,   // 未探索：全黑
        Explored, // 已去过但不在当前视野：半透明
        Visible   // 当前房间及四方向相邻：完全可见
    }
}
