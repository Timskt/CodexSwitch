# 13. 拖拽交互实现

## 13.1 核心思路

1. 监听 Pointer 事件（按下、移动、释放）
2. 使用 `Pointer.Capture` 捕获指针
3. 通过 `TranslateTransform` 移动被拖拽的元素
4. 计算目标位置，让其他元素让路
5. 松开时动画归位并提交排序变更

## 13.2 数据结构

```csharp
private sealed class ProviderDragState(ProviderListItem item, Control row, Point startPoint)
{
    public ProviderListItem Item { get; } = item;
    public Control Row { get; } = row;
    public Point StartPoint { get; } = startPoint;
    public bool IsDragging { get; set; }
    public List<ProviderDragRow> Rows { get; set; } = [];
    public int OriginalIndex { get; set; }
    public int TargetIndex { get; set; }
    public double SlotHeight { get; set; }
}
```

## 13.3 Pointer 事件

```csharp
// 按下
private void ProviderDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    _providerDrag = new ProviderDragState(item, row, e.GetPosition(this));
    e.Pointer.Capture(handle);  // 捕获指针
}

// 移动
private void ProviderDragHandle_OnPointerMoved(object? sender, PointerEventArgs e)
{
    var currentPoint = e.GetPosition(this);

    // 超过 4px 阈值才开始拖拽
    if (!state.IsDragging)
    {
        if (Math.Abs(currentPoint.Y - state.StartPoint.Y) < 4)
            return;
        BeginProviderDrag(state);
    }

    UpdateProviderDrag(state, currentPoint);
}

// 释放
private void ProviderDragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
{
    e.Pointer.Capture(null);  // 释放指针
    _ = CompleteProviderDragAsync(commit: _providerDrag.IsDragging);
}
```

## 13.4 拖拽逻辑

```csharp
private void UpdateProviderDrag(ProviderDragState state, Point currentPoint)
{
    // 移动被拖拽元素
    EnsureTranslateTransform(state.Row, animate: false).Y = currentPoint.Y - state.StartPoint.Y;

    // 计算目标索引
    var targetIndex = ResolveProviderDragTargetIndex(state, currentPoint.Y);

    // 让其他元素让路
    foreach (var row in state.Rows)
    {
        if (ReferenceEquals(row.Control, state.Row)) continue;
        var offset = ResolveSiblingOffset(state, row.OriginalIndex);
        EnsureTranslateTransform(row.Control, animate: true).Y = offset;
    }
}
```

## 13.5 完成拖拽

```csharp
private async Task CompleteProviderDragAsync(bool commit)
{
    // 动画归位
    EnsureTranslateTransform(state.Row, animate: true).Y = commit
        ? (state.TargetIndex - state.OriginalIndex) * state.SlotHeight
        : 0;

    await Task.Delay(DragSettleDuration);  // 等待动画

    // 提交排序变更
    if (commit && state.TargetIndex != state.OriginalIndex)
        viewModel.MoveProvider(state.Item.Id, state.TargetIndex);

    ResetProviderDragVisuals(state);
}
```

## 13.6 关键技术要点

| 要点 | 说明 |
|------|------|
| `Pointer.Capture` | 捕获指针，确保事件不丢失 |
| `TranslateTransform` | 位移变换，不影响布局 |
| `ZIndex` | 拖拽元素在最上层 |
| `Classes.Add("dragging")` | 添加伪类控制样式 |
| `Transitions` | 归位时的动画效果 |
| 阈值检测 | 4px 才开始拖拽，防止误触 |
