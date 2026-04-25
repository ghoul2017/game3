# 战斗系统编码规则（Assets/Scripts/Game/Battle/**）

## 统一回合流程（F140）

### 四阶段结构
```
RoundStart  → 先手方Action → 后手方Action → RoundEnd
(Tick双方     (根据isPlayerTurn     (另一方        (DecayBuffs
 +判定先手)    决定先后)             的Action)     旧buff衰减)
```

**核心原则**: TickEffects 只在 RoundStart 执行一次（F140 修复前每个 Action 各 Tick 一次导致双重 Tick）

### 两种调用路径

**手动操作**（BattlePanel）:
```
UI按钮 → ExecuteRound(lambda) → RoundStart → 过渡动画(0.2s) → ExecuteRoundAfterTransition
  → isPlayerTurn ? playerAction() → MonsterAction() : MonsterAction() → playerAction()
  → RoundEnd → RefreshUI
```

**自动战斗**（AutoBattleCoroutine）:
```
while 循环: RoundStart → playerAction → MonsterAction → RoundEnd → RefreshUI → Wait(0.6s)
```

### 兼容包装器（旧接口保留）
| 旧方法 | 内部实现 |
|--------|---------|
| PlayerAttack() | RoundStart() + PlayerActionAttack() |
| PlayerSkill(id) | RoundStart() + PlayerActionSkill(id) |
| PlayerUseItem(id) | RoundStart() + PlayerActionUseItem(id) |
| MonsterTurn() | MonsterAction()（无 RoundStart） |

新代码优先使用 ExecuteRound 流程。

### 逃跑独立路径
```
TryEscape(): RoundStart() → ActionEscape()
  → 失败: MonsterAction()（惩罚机制）
  → 成功: 标记 isFinished
```
逃跑不走 ExecuteRound（失败不需要 RoundEnd）。

### Stun/Silence 检查
每个 Action 方法内部自行检查：
- **Stun** → 跳过行动（所有 Action 都检查）
- **Silence** → 仅 PlayerActionSkill 检查

## 伤害公式（F150 重调）

```
baseDmg = max(1, atk - (int)(def * DEFENSE_REDUCTION_RATE))     // 乘法防御
variance = max(1, (int)(baseDmg * DAMAGE_VARIANCE_RATE))         // 伤害浮动
result  = max(1, baseDmg + Random.Range(-variance, variance+1))  // 最终伤害
```

### 关键常量（GameConstants）
| 常量 | 值 | 说明 |
|------|----|------|
| DEFENSE_REDUCTION_RATE | 0.6f | 防御减伤系数（乘法模型） |
| DAMAGE_VARIANCE_RATE | 0.15f | 伤害浮动比例 |
| MIN_DAMAGE | 1 | 最小伤害 |

- 不要引入旧除法模型（DEFENSE_DIVISOR 已删除）
- 调整平衡时只修改 DEFENSE_REDUCTION_RATE

## 速度系统（F130）

### 速度公式
`speed = agility + speedBase + buffSpeed`

- 游侠 speedBase=12，其他职业=10
- 游侠最高敏捷(12) + 最高speedBase(12) = 天然先手

### 回合顺序判定
`DetermineTurnOrder()` 在 StartBattle 和每轮 Buff 衰减后调用
- `isPlayerTurn=true` → 玩家先手
- 结果存入 isPlayerTurn

### 速度闪避
```
速度差 = 攻击方速度 - 防守方速度
if 速度差 < SPEED_DODGE_THRESHOLD(5): 不触发闪避
else: 闪避率 = min(速度差 × SPEED_DODGE_RATE_PER_POINT(2) / 100, 1.0)
```

**注意**: TrySpeedDodge 的 dodgeChance 需 Mathf.Min 钳制到 1.0（已知待修复）

### 速度 Buff
| 技能 | 效果 | Buff ID |
|------|------|---------|
| 加速术 | 速度+10，3回合 | 6 |
| 减速术 | 敌人速度-10，3回合 | 7 |

## Boss 战规则（F143）

### 五层 Boss
| 层 | 名称 | HP | 特点 |
|----|------|----|------|
| 5 | 暗影领主 | 300 | 高攻+流血 |
| 10 | 堕落骑士 | 550 | 高防+眩晕+防御AI |
| 15 | 深渊法师 | 700 | debuff+沉默+易伤 |
| 20 | 混沌巨兽 | 1000 | 高血+多阶段 |
| 25 | 暗黑龙王 | 1500 | 三阶段+全技能 |

- Boss 属性约同层普通怪 2.5 倍
- 使用 `MonsterAIPattern.Boss`（三阶段策略，阈值 60%/30%）
- `GetBossByFloor`: 先精确匹配 minFloor==floor，再用 minFloor<=floor 兜底

### Boss 战特殊流程
1. **警告弹窗**: 复用 DungeonPanel MsgOverlay（pendingBossConfig/pendingBossTile）
2. **逃跑禁用**: SetActionButtons 中 `btnEscape.interactable = false` + 回合切换时再次检查
3. **专属 BGM**: BossWarning 音效 + BossBGM
4. **击杀奖励**: 额外 floor*100 金币 + floor*50 经验 + 必掉装备 + 随机宝物

### 金币奖励双路径
- **普通/精英怪**: `goldReward = monsterCfg.goldReward * floor * rewardMul`（DungeonPanel 中）
- **Boss**: 不在 DungeonPanel 乘 floor，在 OnVictory 中独立加成 `floor * BOSS_GOLD_FLOOR_MULTIPLIER`
- 两个路径不相通，不要合并

## 战斗视觉效果（F153）

### 伤害浮动数字
- 日志驱动：RefreshUI 中解析 BattleData.log 新增条目
- 去重：`lastProcessedLogCount` 记录处理位置
- 对象池：8 个 FloatingText 预创建，循环复用

### Boss 屏幕震动
- 信号：`BattleData.bossSkillTriggered`（一次性标记，RefreshUI 消费后重置）
- 实现：操作 RectTransform.localPosition，0.1s x 3 次 ±5px
- 新旧震动互斥（独立 shakeCoroutine 管理）

## BattleController 轻量控制器（F170）

- **非 MonoBehaviour 单例**: 纯 C# 类，不需要挂载 GameObject
- **零 UI 引用**: 不引用 BattlePanel 或任何 UI 类
- **两个事件**: `OnRoundComplete(RoundResult)` 和 `OnBattleEnd(BattleResult)`
- **三个入口**: `ExecuteAttack()` / `ExecuteSkill(int)` / `ExecuteItem(int)`
- `logCountAtRoundStart` 偏移量提取本回合新增日志，不修改 BattleManager 日志写入逻辑
- 新的战斗触发点优先使用 BattleController.ExecuteXxx()，不再直接调用 BattleManager 回合方法

## 地牢内联战斗（F171）

DungeonPanel 订阅 BattleController 事件，跳过 BattlePanel 中转：
- **两次点击模式**: 第一次点击触发战斗开始，第二次点击执行回合行动
- isRoundAnimating 标记防连点，动画期间忽略输入
- 被动技能回退到普通攻击（不报错），MP 不足提示但不消耗回合
- 未来新增战斗场景可复用：订阅 BattleController 事件 + 自定义动画/结算

## 怪物 AI 系统（F141+F147）

### 五种 AI 模式
| 模式 | 策略 | 代表怪物 |
|------|------|----------|
| Aggressive | 70%最高伤技能 / 30%普攻 | 骷髅兵、蝙蝠、暗影狼、暗黑法师 |
| Defensive | HP<50%偏治疗；HP>=50%均衡 | 石像鬼、重甲兵 |
| Debuffer | 玩家无debuff时70%上debuff | 毒蛇、暗影法师 |
| Balanced | 50%普攻 / 50%随机技能 | 史莱姆、哥布林 |
| Boss | HP>60%走Aggressive；30-60%混合；<30%全技能+伤害+20% | 五层Boss |

### mp 字段陷阱
- `BattleManager.MonsterAction()` 用 `m.mp > 0` 判断能否用技能
- **MonsterConfig.mp 是 int，默认值 0**：有 skillIds 但未设 mp → AI 永远判定无 MP → 技能永不释放
- **规则**: 有 skillIds 的怪物必须显式设置 mp 字段

### 楼层分布
- `GetMonstersByFloor(floor)`: `minFloor <= floor && (maxFloor == 0 || maxFloor >= floor) && !isBoss`
- `maxFloor=0` 表示无上限（20+ 层怪物）

## 已知待修复项
- `TrySpeedDodge()` dodgeChance 无 Mathf.Min 钳制
- `new StatusEffect(...)` 仅取名称时产生不必要堆分配（改为 StatusEffect.GetName 静态方法）
- `BattlePanel.BindReferences()` rewardOverlay Find 无 null 保护
- `OnSkillMenu/OnItemMenu` 缺少 `skillPanel != null` 检查
