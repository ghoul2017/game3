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
        Buff
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
    /// 地牢网格瓦片类型
    /// </summary>
    public enum TileType
    {
        Room,  // 可通行房间
        Wall   // 不可通行墙壁
    }
}
