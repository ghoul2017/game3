# 方块与地下城

从余烬镇出发，探索十层地下城，带着成长与战利品归来。五个职业有不同的战斗方式，地图以房间为单位逐步揭示，战斗直接发生在地图中。

## 运行

使用 **Unity 2022.3.62f3c1** 打开本工程，与 toc 当前版本一致。打开 `Assets/Scenes/SampleScene.unity`，点击 Play。首次打开需要等待包解析与 URP 资源导入。

本机已构建的 Windows 程序位于 `Builds/Windows/EmberDungeon.exe`。构建产物不进入 Git，换机后使用菜单「方块与地下城 → 构建 Windows」重新生成。

## 怎么玩

1. 在标题界面创建旅人，选择战士、游侠、骑士、牧师或法师。
2. 点击小镇中的旅馆建筑免费恢复、集市购买装备与药水、公会学习五个职业的技能。职业只决定初始能力与成长，技能可以跨职业搭配。
3. 点击城门直接进入第一层。地下城由大小不一、直接共边的区块组成，虚空没有通道，墙块和屏障形成障碍；点击显露的房间直接交互，旅商点击后显示三选一报价，只能购买一份，也可放弃交易。
4. 点击敌人区块直接普通攻击；先选择下方技能，再点敌人施放伤害技能，点左侧角色区块使用治疗、护盾或防御。选择技能本身不推进回合，右键恢复普通攻击。查看右侧敌人意图，速度决定先后手。背包药水占用一轮行动，普通战斗可以撤退，首领战需要战胜敌人。
5. 资源不足时返回城镇。小怪、精英和泉水会恢复，地图、宝箱、神龛和首领进度保留。返城后再次从第一层出发，保留原有地图与一次性收获。
6. 击败第五层首领解除中途封印，击败第十层首领完成冒险。

新生成楼层使用42×30大地图，从中央入口开始。区块按紧凑尺寸与大小权重组合，墙／屏障以横竖细条为主；每层按空间占地率生成，数量不固定。具体尺寸与权重见 `doc/现行设计.md`。已有存档的楼层保留原有探索结果。每次有效操作自动保存；战斗中退出也能恢复相同局面。保存失败会显示红色错误与重试按钮。新存档位于 Unity `Application.persistentDataPath` 下的 `adventure-v2.json`，独立于旧版 `save.json`。新建冒险替换已有新版本存档前，会要求确认。

地图支持滚轮围绕鼠标缩放、左键或中键拖动，点击顶部「全图」查看整层。墙块比较物攻、屏障比较法攻，悬停查看破坏条件；达标后战斗外点击破坏，返城不恢复。

背包常驻右侧：左键使用药水或装备物品，城镇中右键出售一个。物品、技能、装备槽、属性和地图目标可悬停查看详细信息。物品列表滚动只影响背包。保存结构已升为3，文件名仍为 adventure-v2.json；读取旧结构2时仅在内存升级，旧通道以已破坏墙块保留邻接。

## 工程与配置

当前规则见 `doc/现行设计.md`，原始需求保留于 `doc/项目设计.txt`。旧 `doc/feature_list.json` 和 `doc/progress.txt` 只用于追溯开发历史，里面的完成标记不代表现行实现和验证结果。

本轮实际检查、结果与限制见 `doc/验证结果.md`。场景素材与生成提示词见 `doc/美术素材说明.md`。

`Assets/Scripts/Game` 管理权威冒险状态、地图生成、交易、技能与战斗；`Core` 管理启动、存档、音频和显示设置；`UI` 使用 Unity UGUI 呈现状态并调用业务入口，WorldBoard 保留场景与目标节点，回合变化只刷新其状态。业务修改统一经过 `AdventureSession`，成功操作后保存，界面按状态变更刷新。

`Assets/Resources/Config` 包含职业、技能、物品、怪物、宝物和楼层 JSON。配置使用中文正文，编号用于引用，类型值对应 `GameConfig.cs` 的枚举。菜单「方块与地下城 → 校验配置」检查配置和引用。修改数值后还需运行相关行为测试。

## 验证与构建

Unity Test Runner 的 EditMode 覆盖随机地图、返城刷新、完整存读档、战斗奖励、交易与成长、首领门禁和五职业通关模拟。PlayMode 覆盖真实 UI 射线点击、跨职业技能选择与目标施放、连续攻击节点保留、商人区块直接交易、全部技能栏、弹窗遮挡及运行画面截图。另通过逐帧输入完整鼠标按下、移动、松开，验证轻微移动仍算点击、明显拖动不会触发操作，以及连续攻击、技能和药水使用。

命令行示例（把 Unity 路径替换为本机安装位置）：

```powershell
& $unity -batchmode -projectPath $project -runTests -testPlatform EditMode -testResults "$project/Logs/edit-results.xml" -logFile "$project/Logs/edit-tests.log"
& $unity -batchmode -projectPath $project -runTests -testPlatform PlayMode -testResults "$project/Logs/play-results.xml" -logFile "$project/Logs/play-tests.log"
& $unity -batchmode -quit -projectPath $project -executeMethod GameBuild.Windows -logFile "$project/Logs/windows-build.log"
```

同一工程的 Unity 任务顺序执行，交互编辑器占用工程时使用编辑器内菜单和 Test Runner。测试使用独立临时存档，不覆盖玩家冒险。运行截图位于 `Logs/VisualQA`，属于可重新生成的本机验证产物。
