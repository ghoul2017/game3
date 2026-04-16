# UI 面板编码规则

**路径**: `Assets/Scripts/UI/**`

## OnInit() 规则

1. **创建所有 UI 控件**并缓存到字段
2. **绑定所有事件**到方法
3. **初始化对象池**（如有动态列表）

```csharp
protected override void OnInit()
{
    // 缓存引用
    _titleText = transform.Find("Title").GetComponent<Text>();
    _closeBtn = transform.Find("CloseBtn").GetComponent<Button>();

    // 绑定事件
    _closeBtn.onClick.AddListener(OnCloseClick);

    // 初始化对象池
    InitItemPool();
}
```

## RefreshUI() 规则

1. **只修改属性**：`.text`、`.value`、`.color`、`.interactable`
2. **零销毁零创建**
3. **使用对象池**处理动态列表

```csharp
private void RefreshUI()
{
    _titleText.text = _currentTitle;
    _levelText.text = $"Lv.{_player.Level}";

    // 动态列表：显隐 + 原地更新
    for (int i = 0; i < _items.Count; i++)
    {
        var item = _itemPool[i];
        bool active = i < _dataList.Count;
        item.SetActive(active);
        if (active) item.SetText(_dataList[i].Name);
    }
}
```

## 对象池模式

预创建固定数量的行对象，通过 `SetActive` 控制显隐：

```csharp
private List<GameObject> _itemPool = new();

private void InitItemPool(int count)
{
    for (int i = 0; i < count; i++)
    {
        var item = Instantiate(_itemTemplate, _contentRoot);
        item.SetActive(false);
        _itemPool.Add(item);
    }
}
```

## 禁止操作

- **绝对禁止** `Object.DestroyImmediate` — 导致文字消失、层级错乱
- **禁止** 在 `RefreshUI` 中 `Instantiate` 或 `Destroy`
- **禁止** 使用 `SetActive(false)` 隐藏面板 — 使用 `CanvasGroup.alpha = 0`
- **禁止** 在 Update 中每帧创建字符串

## 隐藏面板

使用 `CanvasGroup` 而非 `SetActive`：

```csharp
private CanvasGroup _canvasGroup;

protected override void OnHide()
{
    _canvasGroup.alpha = 0;
    _canvasGroup.interactable = false;
    _canvasGroup.blocksRaycasts = false;
}
```

## 命名空间

所有 UI 目录下的类使用 `Game.UI` 命名空间。
