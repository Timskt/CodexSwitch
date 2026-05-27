# 10. 拖拽交互实现

CodexSwitch 的 `ProvidersPage` 实现了完整的拖拽排序功能，不依赖任何第三方拖拽库。

## 10.1 核心思路

1. 监听 Pointer 事件（按下、移动、释放）
2. 使用 `Pointer.Capture` 捕获指针
3. 通过 `TranslateTransform` 移动被拖拽的元素
4. 计算目标位置，让其他元素让路
5. 松开时动画归位并提交排序变更

## 10.2 完整实现

### 数据结构

```csharp
private sealed class ProviderDragState(ProviderListItem item, Control row, Point startPoint)
{
    public ProviderListItem Item { get; } = item;
    public Control Row { get; } = row;           // 被拖拽的 UI 元素
    public Point StartPoint { get; } = startPoint; // 按下的位置
    public Point LastPoint { get; set; } = startPoint;
    public bool IsDragging { get; set; }           // 是否已开始拖拽（超过阈值）
    public List<ProviderDragRow> Rows { get; set; } = []; // 所有可排序行
    public int OriginalIndex { get; set; }         // 原始索引
    public int TargetIndex { get; set; }           // 目标索引
    public double SlotHeight { get; set; }         // 每行高度（含间距）
}

private sealed record ProviderDragRow(
    Control Control,
    ProviderListItem Item,
    double OriginalCenterY,  // 行中心 Y 坐标
    double OriginalTopY)     // 行顶部 Y 坐标
{
    public int OriginalIndex { get; init; }
}
```

### 事件注册

```csharp
public ProvidersPage()
{
    InitializeComponent();

    // 注册右键菜单事件
    AddHandler(
        InputElement.PointerPressedEvent,
        ProviderContextHost_OnPointerPressed,
        RoutingStrategies.Bubble,
        handledEventsToo: true);  // 即使事件已处理也要接收

    AddHandler(
        InputElement.ContextRequestedEvent,
        ProviderContextHost_OnContextRequested,
        RoutingStrategies.Bubble,
        handledEventsToo: true);
}
```

### 拖拽手柄的 Pointer 事件

```xml
<!-- AXAML 中的拖拽手柄 -->
<lucide:LucideIcon Kind="GripVertical" Size="14" Classes="drag-handle"/>
```

```csharp
// 按下拖拽手柄
private void ProviderDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (sender is not Control handle ||
        handle.DataContext is not ProviderListItem item ||
        !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
        FindProviderRow(handle) is not { } row)
    {
        return;
    }

    // 记录拖拽状态
    _providerDrag = new ProviderDragState(item, row, e.GetPosition(this));

    // 捕获指针（后续所有 Pointer 事件都发送到这个控件）
    e.Pointer.Capture(handle);
    e.Handled = true;
}

// 移动
private void ProviderDragHandle_OnPointerMoved(object? sender, PointerEventArgs e)
{
    var state = _providerDrag;
    if (state is null) return;

    var currentPoint = e.GetPosition(this);
    state.LastPoint = currentPoint;

    // 超过 4px 阈值才开始拖拽（防止误触）
    if (!state.IsDragging)
    {
        if (Math.Abs(currentPoint.Y - state.StartPoint.Y) < 4)
            return;
        BeginProviderDrag(state);
        if (!state.IsDragging) return;
    }

    UpdateProviderDrag(state, currentPoint);
    e.Handled = true;
}

// 释放
private void ProviderDragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
{
    if (_providerDrag is null) return;

    _isCompletingProviderDrag = true;
    e.Pointer.Capture(null);  // 释放指针捕获
    _ = CompleteProviderDragAsync(commit: _providerDrag.IsDragging);
    e.Handled = true;
}

// 指针丢失（如窗口失焦）
private void ProviderDragHandle_OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
{
    if (_providerDrag is null || _isCompletingProviderDrag) return;
    _isCompletingProviderDrag = true;
    _ = CompleteProviderDragAsync(commit: false);  // 不提交变更
}
```

### 开始拖拽

```csharp
private void BeginProviderDrag(ProviderDragState state)
{
    var rows = GetProviderRows();
    if (rows.Count < 2) return;  // 少于 2 项不需要排序

    var originalIndex = rows.FindIndex(row =>
        string.Equals(row.Item.Id, state.Item.Id, StringComparison.OrdinalIgnoreCase));
    if (originalIndex < 0) return;

    state.IsDragging = true;
    state.Rows = rows;
    state.OriginalIndex = originalIndex;
    state.TargetIndex = originalIndex;
    state.SlotHeight = Math.Max(1,
        state.Row.Bounds.Height + state.Row.Margin.Top + state.Row.Margin.Bottom);

    // 添加拖拽样式
    state.Row.Classes.Add("dragging");
    state.Row.ZIndex = 100;  // 确保在最上层

    // 初始化所有行的 TranslateTransform
    foreach (var row in rows)
    {
        var transform = EnsureTranslateTransform(row.Control,
            animate: !ReferenceEquals(row.Control, state.Row));
        transform.X = 0;
        transform.Y = 0;
    }
}
```

### 更新拖拽位置

```csharp
private void UpdateProviderDrag(ProviderDragState state, Point currentPoint)
{
    // 1. 移动被拖拽的元素
    var draggedTransform = EnsureTranslateTransform(state.Row, animate: false);
    draggedTransform.Y = currentPoint.Y - state.StartPoint.Y;

    // 2. 计算新的目标索引
    var targetIndex = ResolveProviderDragTargetIndex(state, currentPoint.Y);
    if (targetIndex == state.TargetIndex) return;

    state.TargetIndex = targetIndex;

    // 3. 让其他元素让路
    foreach (var row in state.Rows)
    {
        if (ReferenceEquals(row.Control, state.Row)) continue;

        var offset = ResolveSiblingOffset(state, row.OriginalIndex);
        EnsureTranslateTransform(row.Control, animate: true).Y = offset;
    }
}

// 计算目标索引：被拖拽元素应该插入到哪里
private int ResolveProviderDragTargetIndex(ProviderDragState state, double pointerY)
{
    var targetIndex = 0;
    foreach (var row in state.Rows)
    {
        if (ReferenceEquals(row.Control, state.Row)) continue;

        // 如果指针在某行中心点下方，目标索引 +1
        if (pointerY > row.OriginalCenterY)
            targetIndex++;
    }
    return Math.Clamp(targetIndex, 0, state.Rows.Count - 1);
}

// 计算兄弟元素的偏移量
private static double ResolveSiblingOffset(ProviderDragState state, int siblingIndex)
{
    // 目标在原始位置之后：中间的元素上移
    if (state.TargetIndex > state.OriginalIndex &&
        siblingIndex > state.OriginalIndex &&
        siblingIndex <= state.TargetIndex)
    {
        return -state.SlotHeight;
    }

    // 目标在原始位置之前：中间的元素下移
    if (state.TargetIndex < state.OriginalIndex &&
        siblingIndex >= state.TargetIndex &&
        siblingIndex < state.OriginalIndex)
    {
        return state.SlotHeight;
    }

    return 0;
}
```

### 完成拖拽

```csharp
private async Task CompleteProviderDragAsync(bool commit)
{
    var state = _providerDrag;
    if (state is null) return;

    try
    {
        if (state.IsDragging)
        {
            // 动画归位
            EnsureTranslateTransform(state.Row, animate: true).Y = commit
                ? (state.TargetIndex - state.OriginalIndex) * state.SlotHeight
                : 0;  // 不提交则回到原位

            await Task.Delay(DragSettleDuration);  // 等待动画完成
        }

        // 提交排序变更
        if (commit && state.TargetIndex != state.OriginalIndex &&
            DataContext is MainWindowViewModel viewModel)
        {
            viewModel.MoveProvider(state.Item.Id, state.TargetIndex);
        }
    }
    finally
    {
        ResetProviderDragVisuals(state);
        _providerDrag = null;
        _isCompletingProviderDrag = false;
    }
}

private void ResetProviderDragVisuals(ProviderDragState state)
{
    foreach (var row in state.Rows)
    {
        row.Control.Classes.Remove("dragging");
        row.Control.ZIndex = 0;
        if (row.Control.RenderTransform is TranslateTransform transform)
        {
            transform.Transitions = null;  // 清除过渡
            transform.X = 0;
            transform.Y = 0;
        }
    }
}
```

## 10.3 TranslateTransform 辅助方法

```csharp
private static TranslateTransform EnsureTranslateTransform(Control control, bool animate)
{
    if (control.RenderTransform is not TranslateTransform transform)
    {
        transform = new TranslateTransform();
        control.RenderTransform = transform;
    }

    // animate=true 时添加过渡动画
    transform.Transitions = animate
        ?
        [
            new DoubleTransition
            {
                Property = TranslateTransform.YProperty,
                Duration = DragSettleDuration,  // 150ms
                Easing = new CubicEaseOut()
            }
        ]
        : null;

    return transform;
}
```

## 10.4 获取可排序行

```csharp
private List<ProviderDragRow> GetProviderRows()
{
    return this.GetVisualDescendants()
        .OfType<Control>()
        .Where(control => control.Classes.Contains("provider-list-row") &&
                          control.DataContext is ProviderListItem)
        .Select(control =>
        {
            var top = control.TranslatePoint(new Point(0, 0), this)?.Y ?? control.Bounds.Y;
            return new ProviderDragRow(
                control,
                (ProviderListItem)control.DataContext!,
                top + control.Bounds.Height / 2,  // 中心 Y
                top);                              // 顶部 Y
        })
        .OrderBy(row => row.OriginalTopY)          // 按 Y 坐标排序
        .Select((row, index) => row with { OriginalIndex = index })
        .ToList();
}
```

## 10.5 拖拽样式

```xml
<!-- ProviderCard.axaml -->
<Style Selector="ui|CsProviderCard.dragging">
    <Setter Property="Opacity" Value="0.9"/>
    <Setter Property="Background" Value="{StaticResource CsProviderCardActiveBrush}"/>
    <Setter Property="BoxShadow">
        <BoxShadow BlurRadius="12" OffsetY="4" Color="#40000000"/>
    </Setter>
</Style>
```

## 10.6 关键技术要点

| 要点 | 说明 |
|------|------|
| `Pointer.Capture` | 捕获指针，确保移动/释放事件不丢失 |
| `TranslateTransform` | 位移变换，不影响布局 |
| `ZIndex` | 拖拽元素在最上层 |
| `Classes.Add("dragging")` | 添加伪类控制样式 |
| `Transitions` | 归位时的动画效果 |
| `CubicEaseOut` | 缓动函数，先快后慢 |
| 阈值检测 | 4px 才开始拖拽，防止误触 |
| `RoutingStrategies.Bubble` | 冒泡路由，确保事件能到达父元素 |
