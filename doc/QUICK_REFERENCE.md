# 快速参考卡片

## 启动流程
```
GameEntry.Start() 
  → UIManager.EnsureCreated()
  → UIManager.ShowPanel<MainPanel>()
```

## 状态切换
```
GameState: None → MainMenu → CreatingRole → InTown → InDungeon/InBattle
```

## 创建面板
```csharp
UIManager.Instance.ShowPanel<T>();    // 显示(栈式)
UIManager.Instance.ReplacePanel<T>(); // 替换
UIManager.Instance.Back();            // 返回
```

## 面板基类
```csharp
protected override void BindReferences() { /* 绑定组件 */ }
protected override void BindEvents()    { /* 绑定事件 */ }
public override void OnShow()           { RefreshUI(); }
private void RefreshUI()                { /* 只改属性 */ }
```

## 刷新UI规则
- ✅ `text.text = "xxx"`
- ✅ `slider.value = 0.5f`
- ✅ `btn.interactable = true`
- ❌ `new GameObject()` (禁止)
- ❌ `Destroy()` (禁止)
- ❌ `SetActive(false)` 隐藏面板 (用CanvasGroup)

## 关键路径
- 存档: `Application.persistentDataPath/save.json`
- 面板预制体: `Resources/UI/{PanelName}`
- 组件预制体: `Resources/Prefabs/{path}`

## 常用配置查询
```csharp
ConfigManager.Instance.GetProfession(prof);      // 职业
ConfigManager.Instance.GetSkill(id);             // 技能
ConfigManager.Instance.GetItem(id);               // 物品
ConfigManager.Instance.GetMonster(id);           // 怪物
ConfigManager.Instance.GetTreasure(id);          // 宝物
ConfigManager.Instance.GetBuff(id);               // Buff
```

## 玩家数据操作
```csharp
player.RecalcStats();    // 属性重算
player.AddExp(exp);      // 加经验
player.FullRestore();    // 满血满蓝
GameManager.Instance.SaveGame();  // 保存
```

## 战斗操作
```csharp
BattleManager.Instance.StartBattle(data);
BattleManager.Instance.PlayerAttack();
BattleManager.Instance.PlayerSkill(skillId);
BattleManager.Instance.MonsterTurn();
BattleManager.Instance.TryEscape();
```

## 属性ID速查
| 职业 | 战士1001 | 游侠2001 | 骑士3001 | 牧师4001 | 法师5001 |
|------|----------|----------|----------|----------|----------|
| 默认技能 | 猛击 | 精准射击 | 盾击 | 治疗术 | 火球术 |

## 宝物效果
| ID | 名称 | 效果 |
|----|------|------|
| 1 | 勇者之证 | ATK+10% |
| 2 | 守护之盾 | DEF+15% |
| 3 | 生命之泉 | HP+20% |
| 4 | 吸血之牙 | 攻击吸血10% |
| 5 | 幸运硬币 | 金币+30% |
| 6 | 经验宝石 | 经验+20% |

## Boss层数
- 第5层: 地牢守卫 (ID 2001)
- 第10层: 堕落领主 (ID 2002)
