# Game 层编码规则（Assets/Scripts/Game/**）

## 网格地牢系统（F131-F136）

### 数据模型
- `GridPos` — 自定义 struct（非 Vector2Int），因为 Vector2Int JSON 序列化字段名不符
- `DungeonTile` — 网格格子（TileType.Room/Wall）+ 四方向连通标记 + ContentData
- `ContentData` — 房间内容（monsterId/treasureId/goldAmount/itemId/skillId/monsterLevel）
- `DungeonData.grid[,]` — 2D 数组 [x,y]，roomTiles 列表辅助遍历

### 坐标映射
- 锚定：`pivot=(0,1)`, `anchorMin/Max=(0,1)`
- 位置：`anchoredPosition = (x * cellSize, -y * cellSize)`（y 轴向下为负）
- 对象池上限 MaxTiles=100（10x10）

### 双数据同步（迁移期）
- F131 阶段旧 rooms 和新 grid 同时维护
- 修改房间状态时两者都改：`tile.isCleared = true` + `FindMatchingRoom(...).cleared = true`
- 兼容方法：`FindRoomIndex()` / `FindMatchingRoom()` 用于旧 rooms 查询
- 迁移完成后（F136+）旧 rooms 字段将删除

### 连通性检查
- 使用 `connectUp/Down/Left/Right` 四方向布尔字段
- **不要用"邻居格子是否为 Room"判断能否移动**

### 战争迷雾（F134）
- **三态实时计算**（不持久化）: Hidden → Explored → Visible
- isExplored 持久化在 DungeonTile 上
- TileVisibility 通过 GetTileVisibility(x,y) 实时计算
- **InitVisibility 陷阱**: 在 GenerateGrid 末尾调用，会清除之前所有 isExplored 标记。起点 isExplored 由后续 SetVisibleArea 设置

### 房间事件触发（F136）
- 战斗结果在 DungeonPanel.OnShow 中延迟处理（pendingBattleTile）
- 出口确认用 pendingExitFloor 区分普通消息与出口弹窗
- 空房间事件用 pendingEvent (EmptyRoomEvent?) 区分双选项弹窗
- 五种空房事件概率: GoldBag 40% | Consumable 20% | Trap 20% | WanderingMerchant 10% | MysticPower 10%

### 神殿技能筛选
- 全技能池 → 排除已学 → 随机选一个 → 免费学习
- F146 起包含被动技能（不再排除 Passive）
- 所有技能已学时回退为恢复 HP（maxHp/2）

## 技能系统通用化（F144-F145）

### 技能 ID 三段分配
| 范围 | 用途 | 数量 |
|------|------|------|
| 1001-5006 | 职业专属技能 | 26 个 |
| 6001-6018 | 通用技能（全职业可学） | 18 个 |
| 9001+ | Boss专属技能 | 不可学习 |

- `GetAllSkills()` 过滤掉 ID >= 9000 的技能

### 标签页分类（SkillTab）
| 标签 | 过滤逻辑 |
|------|----------|
| All | 全部（id < 9000） |
| Attack | !isHeal && type == Active |
| Heal | isHeal == true |
| Buff | type == Buff |
| Control | type == StatusAttack / Debuff / DoT / Control |
| Passive | type == Passive |
| Learned | learnedSkills 包含 |

**注意**: Heal 枚举值存在但旧数据仍用 `isHeal=true + type=Active` 组合判断

### 技能池大小
SkillPoolSize = 48（容纳全部 44 个非 Boss 技能）

## 技能装备槽（F146）

### 数据模型
- `PlayerData.equippedSkills`: `List<int>`，索引=槽位，值=技能ID，0=空槽
- `maxSkillSlots = 4`
- **尾部裁剪**: 中间空槽保留 0，尾部连续 0 会被 RemoveAt 裁剪

### 多消费者安全
所有消费 equippedSkills 的地方都必须跳过 0 值：
```csharp
// 正确模式
for (int i = 0; i < count; i++)
{
    int skillId = equippedSkills[i];
    if (skillId == 0) continue;
    var skill = ConfigManager.GetSkill(skillId);
    if (skill == null) continue;
    // 使用 skill
}
```

### 装备规则
- 同一技能不可重复装备
- 只能装备已学习技能
- 点击已装备技能 = 卸下（设为0+尾部裁剪）
- 点击空槽 = 从已学未装备中选

## 战斗数值常量

| 常量 | 值 | 说明 |
|------|----|------|
| BOSS_GOLD_FLOOR_MULTIPLIER | 100 | Boss 每层金币加成 |
| BOSS_EXP_FLOOR_MULTIPLIER | 50 | Boss 每层经验加成 |
| DEFENSE_REDUCTION_RATE | 0.6f | 防御减伤系数 |
| DAMAGE_VARIANCE_RATE | 0.15f | 伤害浮动比例 |
| MIN_DAMAGE | 1 | 最小伤害 |

## BuffConfig（F145 新增）
| ID | 效果 | 数值 |
|----|------|------|
| 8 | ATK+20% | 属性修正 |
| 9 | DEF+25% | 属性修正 |
| 10 | SPD+10% | 属性修正 |

燃烧效果（EffectType.Burn）与流血/中毒共享相同 Tick 逻辑（每回合固定伤害），属于 DoT。
