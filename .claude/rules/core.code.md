# Core 框架层编码规则

**路径**: `Assets/Scripts/Core/**`

## 单例模式

所有 Manager 类必须使用线程安全的单例模式：

```csharp
public class XXXManager : MonoBehaviour
{
    private static XXXManager _instance;
    public static XXXManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<XXXManager>();
                if (_instance == null)
                {
                    var go = new GameObject(nameof(XXXManager));
                    _instance = go.AddComponent<XXXManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
}
```

## UIPanel 生命周期

所有面板继承 `UIPanel` 基类，必须重写：

- `OnInit()` - 创建控件、绑定事件、缓存引用
- `OnShow()` - 刷新数据、播放动画
- `OnHide()` - 暂停动画、隐藏特效
- `OnDestroyed()` - 释放资源、取消事件绑定

## UIManager 栈管理

- 使用 `ShowPanel<T>()` 压栈
- 使用 `ReplacePanel<T>()` 替换栈顶
- 使用 `PopPanel()` 弹出当前面板
- 返回键行为：自动调用 `PopPanel()`

## 禁止操作

- **禁止** 在非 Manager 类中使用 `FindObjectOfType` 频繁调用
- **禁止** 在 Update 中分配内存（避免 GC spike）
- **禁止** 使用 `Object.DestroyImmediate`
- **禁止** 直接访问其他面板的私有字段

## 命名空间

所有 Core 目录下的类使用 `Game.Core` 命名空间。
