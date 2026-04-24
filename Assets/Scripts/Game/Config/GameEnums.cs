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
        Active,
        Passive,
        Buff,
        StatusAttack  // 带状态效果的攻击技能
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
