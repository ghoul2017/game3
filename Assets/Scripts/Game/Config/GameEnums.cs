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
        Exit
    }
}
