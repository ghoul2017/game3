---
name: UI架构重设计
description: UI 面板从销毁-重建模式改为缓存引用+原地更新的完整技术方案
type: project
---

# UI 架构重设计

## Why

所有面板的 RefreshUI() 使用 Object.DestroyImmediate 删除所有子物体再重建，导致：
- 文字频繁消失/闪烁
- GC 压力大
- 按钮事件丢失
- 用户说"还是有问题，这是系统性问题"

## How to apply

### 核心原则
1. OnInit() 中创建所有 UI 控件并**缓存到字段**
2. RefreshUI() 只改 `.text`/`.value`/`.color`，**零销毁零创建**
3. 动态列表用对象池：预创建行，SetActive 显隐
4. 绝不使用 Object.DestroyImmediate
5. Hide 用 CanvasGroup (alpha=0) 不用 SetActive(false)

### feature_list.json 中的 feature 顺序
F008(UIBuilder池) → F009(UIPanel基类) → F012(TownPanel) → F011(CreateRolePanel) → F016(RoleInfoPanel) → F017(ShopPanel) → F015(BagPanel) → F018(SkillLearnPanel) → F014(BattlePanel) → F013(DungeonPanel) → F080(Buff系统) → F062(宝物效果) → F081(战斗物品选择)

### 每个面板重写模式
```csharp
public class XxxPanel : UIPanel
{
    // 缓存字段
    private Text someText;
    private Button someBtn;

    public override void OnInit()
    {
        UIBuilder.CreateFullScreenBg(transform);
        // 创建所有控件，缓存引用
        someText = UIBuilder.CreateText(transform, "", ...);
        var (btn, label) = UIBuilder.CreateButtonWithRef(transform, "", w, h, OnClick, ...);
        someBtn = btn;
    }

    public override void OnShow()
    {
        RefreshUI(); // 只改属性
    }

    private void RefreshUI()
    {
        someText.text = "新内容"; // 原地更新，不销毁不创建
    }
}
```
