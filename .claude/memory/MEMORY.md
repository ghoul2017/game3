# Memory Index

## 项目设计与参考
- [项目设计](game_design.md) — 方块与地下城核心设计：职业/养成/技术决策
- [方块地牢2拆解](square_dungeon2_analysis.md) — 参考游戏的核心机制和数值设计
- [参考项目代码风格](ref_project_style.md) — 4个参考项目的共同架构模式和编码习惯
- [UI使用预制体](feedback_prefab_ui.md) — 从F089起新UI面板改用预制体而非纯代码构建

## 核心框架
- [Logger日志系统架构](arch-logger-system.md) — F112 四级日志+Conditional条件编译设计决策
- [ConfigManager JSON配置架构](arch-config-json.md) — F113 JsonUtility限制+包装类+硬编码fallback
- [音频系统架构](arch-audio-system.md) — AudioManager单例/SFX轮播池/BGM Crossfade
- [UIManager三层Canvas架构](arch-ui-three-canvas.md) — F161 HUD/Window/Tip三层Canvas+panelCache跨层互斥
- [GameHUD框架架构](arch-game-hud.md) — F162-F168+F176 永久HUD+ClearAll+SetState模式+旧面板废弃
- [WindowBase可拖动窗口架构](arch-window-base.md) — F163 标题栏拖拽+Destroy非缓存+单窗口互斥
- [TipManager浮动信息提示架构](arch-tip-manager.md) — F164 struct池+HoverTip+AutoTip淡出

## 战斗系统
- [统一回合流程架构](arch-unified-round.md) — F140 四阶段回合+兼容包装器+逃跑独立路径
- [战斗速度系统](arch-battle-speed.md) — F130 速度公式+回合顺序+速度闪避+加速减速Buff
- [伤害公式与金币奖励机制](arch-damage-formula.md) — F150 乘法防御0.6+伤害浮动0.15+Boss金币双路径
- [状态效果系统架构](arch-status-effect.md) — F138 DoT/控制/属性修正三类+叠加规则+Tick时序
- [状态效果技能架构](arch-status-effect-skills.md) — F139 StatusAttack设计+数值分发+眩晕陷阱
- [怪物AI模式架构](arch-monster-ai.md) — F141+F147 五种AI+Boss三阶段+mp字段陷阱
- [Boss战系统架构](arch-boss-battle.md) — F143 五层Boss+警告弹窗+逃跑禁用+专属BGM+必掉装备
- [战斗视觉效果架构](arch-battle-visual-effects.md) — F153 伤害浮动数字+Boss震动+面板过渡
- [BattleController轻量战斗控制器架构](arch-battle-controller.md) — F170 事件驱动+Execute编排+日志提取
- [地牢内联战斗架构](arch-inline-battle.md) — F171 两次点击+防连点+被动技能回退+MP不足提示

## 地牢系统
- [网格地牢架构与迁移计划](arch-dungeon-grid.md) — F131-F136 数据模型+生成算法+坐标映射+迁移策略
- [网格地图UI架构](arch-grid-map-ui.md) — F133 ScrollRect+CurrentOutline+滚轮缩放+房间着色
- [战争迷雾系统架构](arch-fog-of-war.md) — F134 三态实时计算+InitVisibility时序陷阱
- [玩家网格移动架构](arch-player-grid-movement.md) — F135 连通性检查+isMoving锁+键盘三重守卫
- [网格房间事件触发架构](arch-room-event-trigger.md) — F136-F137 战斗回传/出口确认/空房间随机事件

## 技能系统
- [技能系统通用化与标签页架构](arch-skill-universal.md) — F144-F145 全职业通用+ID三段分配+控制标签页
- [技能系统改造](arch-skill-equip-slots.md) — F160 取消装备槽+learnedSkills即可用+容量6+槽满退金币

## UI与配置速查
- [深色主题UI配色规范](biz-dark-theme.md) — F128配色+F111 UIColors/UIFactory/UIPool/UIBuilder模块
- [配置ID与枚举速查](reference_game_ids.md) — 职业属性/技能/物品/怪物/宝物ID+枚举+常量
- [常见问题排查](reference_troubleshooting.md) — UI文字/面板错乱/属性计算等问题修复方法
- [UI面板交互行为](reference_ui_behavior.md) — 10个面板功能清单+验证要点+导航流程

## 已知问题与陷阱
- [战斗系统待修复警告](debug-battle-warnings.md) — dodgeChance无上限/StatusEffect堆分配/null保护缺失
- [UIPanel协程动画时序陷阱](debug-panel-coroutine-trap.md) — 只停transitionCoroutine/FadeOut中断OnHide
