# Battle 战斗系统编码规则

**路径**: `Assets/Scripts/Game/Battle/**`

## 回合制流程

战斗按以下顺序执行：

1. **开始回合** → 玩家选择行动
2. **执行玩家行动** → 扣除 MP、应用伤害、触发特效
3. **检查战斗结束** → 怪物死亡则结束
4. **怪物行动** → AI 选择技能、应用伤害
5. **检查战斗结束** → 玩家死亡则结束
6. **处理 Buff** → 回合结束触发、减少持续时间
7. **下一回合**

```csharp
public void NextTurn()
{
    _currentTurn++;
    ProcessBuffTick();
    if (_monster.IsDead) EndBattle(true);
    else if (_player.IsDead) EndBattle(false);
}
```

## Buff 处理

Buff 数据结构：

```csharp
public class BuffData
{
    public int BuffId;
    public int Duration;      // 剩余回合数
    public BuffType Type;     // 增益/减益
    public int Value;         // 数值
}
```

Buff 处理时机：
- **回合开始**：触发持续伤害/恢复
- **回合结束**：减少持续时间，移除过期 Buff

## 伤害计算

```csharp
public int CalculateDamage(int baseDamage, int attack, int defense)
{
    int damage = baseDamage + attack - defense;
    // 应用 Buff 修正
    var atkBuff = GetBuffValue(BuffType.AtkUp);
    var defDebuff = GetBuffValue(BuffType.DefDown);
    damage = damage * (100 + atkBuff) / 100;
    damage = damage * (100 + defDebuff) / 100;
    return Math.Max(1, damage);  // 最小伤害 1
}
```

## 禁止操作

- **禁止** 在战斗逻辑中使用协程等待 — 使用状态机或回调
- **禁止** 直接修改玩家数据 — 通过 `PlayerData` 实例
- **禁止** 硬编码怪物属性 — 从 Config 加载

## 命名空间

所有 Battle 目录下的类使用 `Game.Battle` 命名空间。
