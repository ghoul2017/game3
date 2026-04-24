---
name: UI面板交互行为规范
description: 每个面板的功能清单和验证要点，用于功能验证和回归测试
type: reference
---

## MainPanel（主菜单）
- 开始冒险 → CreateRolePanel
- 继续冒险（有存档） → TownPanel；无存档时灰色不可点
- 设置 → SettingsPanel；退出游戏

## CreateRolePanel（创建角色）
- 5职业按钮切换，详情面板实时更新
- 空名默认"冒险者"
- 开始游戏 → 创建角色+学默认技能+进城镇

## TownPanel（城镇）
- 信息栏：名称/等级/职业/HP/MP/金币/层数
- 进入地牢 → DungeonPanel（ReplacePanel）
- 休息恢复 → 满HP/MP + 自动存档
- 商店/技能学习/角色信息 → ShowPanel（可Back）
- 存档退出 → 保存+回主菜单

## DungeonPanel（地牢）
- 网格地图：不同类型不同图标（怪/精/王/宝/泉/商/梯）
- 当前房间金色高亮，已清房间灰色
- 只有当前房间和下一房间可点击
- 怪物房 → BattlePanel；宝箱 → 金币+可能宝物/消耗品
- 泉水 → 恢复1/3 HP/MP；商人 → ShopPanel
- 出口 → 下一层（10层Boss后通关/无尽）
- 右侧状态面板：HP/MP/EXP条+金币/攻防
- 底部技能快捷栏：4技能槽+MP消耗

## BattlePanel（战斗）
- 怪物区：图标(怪/王)+名称+HP条+攻防
- 4操作按钮：攻击/技能/物品/逃跑
- 技能子面板：8行，MP不足灰色不可用
- 物品子面板：8行消耗品，按ID分组显示数量
- 自动战斗按钮：0.6s间隔自动执行
- 胜利奖励面板：显示经验/金币/物品+继续按钮
- 失败 → 返回城镇（满血复活）
- 战斗日志：最近6条
- 玩家信息条：HP/MP/攻击/防御/Buff状态

## BagPanel（背包）
- 物品列表按ID分组：[类型] 名称 x数量 属性 [需Lv.X]
- 武器/防具/饰品 → 装备按钮（已装备绿色，等级不足灰色）
- 消耗品 → 使用按钮
- 所有物品 → 卖X金按钮（售价=原价/2）
- 底部信息栏：容量 X/N + 金币

## ShopPanel（商店）
- 商品列表8行一页，翻页按钮
- 显示物品属性+等级要求 [需Lv.X]
- 购买条件：金币足够+背包未满+等级满足
- 金币不足/背包满/等级不够 → 灰色不可交互

## SkillLearnPanel（技能学习）
- 当前职业所有技能：名称/类型/威力/MP消耗/描述/前置
- 已学 → 绿色"已学"；可学 → "X金"按钮
- 前置不满足/等级不足/金币不足 → 灰色
- 学习后自动装备（有空位）
- 已装备区：名称×(红，卸下) / 名称+(绿，装备)

## RoleInfoPanel（角色信息）
- 属性面板：力量/敏捷/智力/体力/HP/MP/攻击/防御/暴击/闪避
- 装备面板：武器/防具/饰品（未装备显示"空"灰色）
- 技能面板：[类型] 名称 - 描述
- 经验条：EXP进度+当前/所需
- 宝物面板：已获得宝物名称+描述

## SettingsPanel（设置）
- 音乐/音效滑块(0-100)、全屏切换
- 分辨率选择（< > 按钮）、语言选择
- 返回时保存到 PlayerPrefs

## 导航流程
```
MainPanel → CreateRolePanel → TownPanel
MainPanel → SettingsPanel (ShowPanel, Back)
MainPanel → (ContinueGame) → TownPanel

TownPanel → DungeonPanel (ReplacePanel)
TownPanel → ShopPanel/SkillLearnPanel/RoleInfoPanel (ShowPanel, Back)
TownPanel → MainPanel (ReplacePanel, 存档退出)

DungeonPanel → BattlePanel (ShowPanel, 战斗结束Back)
DungeonPanel → ShopPanel/BagPanel/RoleInfoPanel (ShowPanel, Back)
DungeonPanel → TownPanel (ReplacePanel, 返回城镇)

BattlePanel → DungeonPanel (胜利Back)
BattlePanel → TownPanel (失败ReplacePanel)
```
