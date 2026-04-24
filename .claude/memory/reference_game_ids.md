---
name: 游戏配置ID与枚举速查
description: 职业属性、技能/物品/怪物/宝物ID范围、枚举定义、游戏常量 — 来自TECHNICAL_DOCUMENTATION
type: reference
---

## 枚举速查

### Profession（职业）
Warrior=战士, Ranger=游侠, Knight=骑士, Priest=牧师, Mage=法师

### ItemType（物品类型）
Weapon=武器, Armor=防具, Accessory=饰品, Consumable=消耗品, Treasure=宝物/遗物

### SkillType（技能类型）
Active=主动攻击, Passive=被动, Buff=增益, Heal=治疗（isHeal字段）

### DungeonEventType（地牢事件）
Empty=空房, Monster=普通怪, Elite=精英怪, Boss=Boss战, Treasure=宝箱, Spring=泉水, Merchant=商人, Exit=出口

## 职业起始属性

| 职业 | STR | AGI | INT | VIT |
|------|-----|-----|-----|-----|
| 战士 | 12  | 6   | 4   | 10  |
| 游侠 | 8   | 12  | 6   | 6   |
| 骑士 | 8   | 4   | 4   | 14  |
| 牧师 | 4   | 6   | 12  | 8   |
| 法师 | 4   | 6   | 14  | 6   |

## 技能ID

| 职业 | ID范围 | 示例 |
|------|--------|------|
| 战士 | 1001-1004 | 猛击/战吼/旋风斩/狂战之怒(被动) |
| 游侠 | 2001-2004 | 精准射击/闪避姿态 |
| 骑士 | 3001-3004 | 盾击(debuff)/铁壁/嘲讽(debuff)/坚守(被动) |
| 牧师 | 4001-4004 | 治疗术(isHeal)/祝福/复活(被动) |
| 法师 | 5001-5004 | 火球术/冰甲/魔力涌动(被动) |

## 物品ID

| 类型 | ID范围 | 示例 |
|------|--------|------|
| 武器 | 1001-1007 | 铁剑/短弓/法杖 |
| 防具 | 2001-2004 | 皮甲/锁子甲/板甲 |
| 饰品 | 3001-3003 | 力量戒指/敏捷护符 |
| 消耗品 | 4001-4005 | 生命药水/法力药水 |

## 怪物ID

| ID | 名称 | 层数 | 类型 |
|----|------|------|------|
| 1001 | 史莱姆 | 1 | 普通 |
| 1002 | 骷髅兵 | 1 | 普通 |
| 1003 | 蝙蝠 | 1 | 普通 |
| 1004 | 哥布林 | 2 | 普通 |
| 1005 | 暗影狼 | 3 | 普通 |
| 2001 | 地牢守卫 | 5 | Boss |
| 2002 | 堕落领主 | 10 | Boss |

## 宝物ID

| ID | 名称 | 效果 |
|----|------|------|
| 1 | 勇者之证 | ATK+10% |
| 2 | 守护之盾 | DEF+15% |
| 3 | 生命之泉 | HP+20% |
| 4 | 吸血之牙 | 攻击吸血10% |
| 5 | 幸运硬币 | 金币+30% |
| 6 | 经验宝石 | 经验+20% |

## 游戏常量 (GameConstants)

### 战斗
- MONSTER_SKILL_CHANCE=0.3f, ESCAPE_CHANCE=0.5f
- DEFEAT_GOLD_LOSS_RATE=0.1f, DAMAGE_VARIANCE_RATE=0.1f
- MIN_DAMAGE=1, DEFENSE_DIVISOR=2

### 地牢
- TREASURE_CHANCE=0.4f, CONSUMABLE_DROP_CHANCE=0.3f
- MIN_ROOMS=5, MAX_ROOMS=15

### 楼层缩放
- 每层怪物属性 +15%（HP/攻击/防御）
- 精英怪：1.5倍属性 + 2倍经验金币
- 10层Boss通关 → 无尽模式

## 存档路径
`Path.Combine(Application.persistentDataPath, "save.json")`
