# 13. 拖拽交互实现

> **写给零基础的你**：拖拽就是"按住鼠标拖来拖去"的操作。比如你在桌面上拖动文件到文件夹里，或者在列表里拖动项目来调整顺序。这在桌面应用里非常常见，本章教你怎么做。

## 13.1 概述

拖拽是桌面应用中最自然的交互方式之一。学完本章，你将能够：

- 掌握 Avalonia 的 Pointer 事件系统和指针捕获机制
- 实现完整的拖拽排序（列表项重排）
- 理解 TranslateTransform 在拖拽中的应用
- 实现拖拽视觉反馈（阴影、动画、放置区域高亮）
- 使用 Avalonia 内置的 DragDrop API 实现跨控件拖拽
- 理解 DataObject 和数据格式
- 实现键盘辅助的拖拽操作

CodexSwitch 的 Provider 列表拖拽排序是一个完整的拖拽交互案例。本章将从零开始拆解这个实现，讲解每个设计决策的原因。

## 13.2 核心概念

### 13.2.1 拖拽的五个阶段

> **小白提示**：拖拽就像"搬家具"。你先用手抓住（按下），然后拖着走（移动），最后放到新位置（释放）。程序也需要这三个步骤，还要处理"拖到哪里"、"其他东西让不让路"等问题。

```
按下 (PointerPressed)        ← 你用手抓住家具
    -> 创建拖拽状态，捕获指针
    -> 移动 (PointerMoved)   ← 你拖着家具走
        -> 超过阈值后开始拖拽（不是随便动一下就算拖拽）
        -> TranslateTransform 移动被拖拽元素
        -> 计算目标索引，其他元素让路
    -> 释放 (PointerReleased) ← 你把家具放到新位置
        -> 释放指针捕获
        -> 动画归位
        -> 提交排序变更
```

### 13.2.2 Pointer 事件系统

> **小白提示**：为什么叫 Pointer 而不是 Mouse？因为 Pointer 是"指针"的统称，包括鼠标、触控笔、手指触摸。用 Pointer 一套代码就能支持所有输入方式。

Avalonia 使用 Pointer 事件（而非 Mouse 事件），支持鼠标、触控笔和触摸屏：

| 事件 | 触发时机 | 说明 |
|------|---------|------|
| `PointerPressed` | 按下按钮 | 创建拖拽状态 |
| `PointerMoved` | 移动指针 | 更新拖拽位置 |
| `PointerReleased` | 释放按钮 | 完成拖拽 |
| `PointerCaptureLost` | 捕获丢失 | 取消拖拽 |
| `PointerEntered` | 指针进入控件 | 放置区域高亮 |
| `PointerExited` | 指针离开控件 | 取消高亮 |

### 13.2.3 指针捕获 (Pointer.Capture)

```
无捕获：PointerMoved 发送给指针下方的控件
有捕获：PointerMoved 始终发送给捕获控件

按下句柄 -> Capture(handle)
    -> 移动事件始终发送给 handle
    -> 释放事件始终发送给 handle
Capture(null) -> 恢复正常路由
```

捕获确保拖拽过程中即使鼠标快速移动到其他控件上方，事件仍然正确路由。

## 13.3 进阶用法

### 13.3.1 数据结构设计

```csharp
// 拖拽运行时状态
private sealed class ProviderDragState(ProviderListItem item, Control row, Point startPoint)
{
    public ProviderListItem Item { get; } = item;
    public Control Row { get; } = row;
    public Point StartPoint { get; } = startPoint;
    public Point LastPoint { get; set; } = startPoint;
    public bool IsDragging { get; set; }
    public List<ProviderDragRow> Rows { get; set; } = [];
    public int OriginalIndex { get; set; }
    public int TargetIndex { get; set; }
    public double SlotHeight { get; set; }
}

// 每个行在拖拽开始时的位置快照
private sealed record ProviderDragRow(
    Control Control, ProviderListItem Item,
    double OriginalCenterY, double OriginalTopY)
{
    public int OriginalIndex { get; init; }
}
```

`ProviderDragState` 是可变的拖拽状态，`ProviderDragRow` 是不可变的位置快照（使用 `record`），`with` 表达式用于设置 `OriginalIndex`。

### 13.3.2 注册 Pointer 事件

```csharp
public ProvidersPage()
{
    InitializeComponent();

    // 在冒泡阶段监听，包括已标记 handled 的事件
    AddHandler(
        InputElement.PointerPressedEvent,
        ProviderContextHost_OnPointerPressed,
        RoutingStrategies.Bubble,
        handledEventsToo: true);

    AddHandler(
        InputElement.ContextRequestedEvent,
        ProviderContextHost_OnContextRequested,
        RoutingStrategies.Bubble,
        handledEventsToo: true);
}
```

`handledEventsToo: true` 确保即使子控件标记了 `e.Handled = true`，父容器仍能收到事件。

### 13.3.3 按下：创建拖拽状态

```csharp
private void ProviderDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (sender is not Control handle ||
        handle.DataContext is not ProviderListItem item ||
        !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
        FindProviderRow(handle) is not { } row)
    {
        return;
    }

    _providerDrag = new ProviderDragState(item, row, e.GetPosition(this));
    e.Pointer.Capture(handle);  // 捕获指针
    e.Handled = true;
}
```

`e.Pointer.Capture(handle)` 将指针事件路由到拖拽句柄控件。

### 13.3.4 移动：阈值检测 + 拖拽更新

```csharp
private void ProviderDragHandle_OnPointerMoved(object? sender, PointerEventArgs e)
{
    var state = _providerDrag;
    if (state is null) return;

    var currentPoint = e.GetPosition(this);
    state.LastPoint = currentPoint;

    if (!state.IsDragging)
    {
        // 4px 阈值防止误触
        if (Math.Abs(currentPoint.Y - state.StartPoint.Y) < 4)
            return;

        BeginProviderDrag(state);
        if (!state.IsDragging) return;
    }

    UpdateProviderDrag(state, currentPoint);
    e.Handled = true;
}
```

4px 阈值是关键设计——用户点击时鼠标可能有微小抖动，阈值防止普通点击被误判为拖拽。

### 13.3.5 释放：归位动画 + 提交变更

```csharp
private void ProviderDragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
{
    if (_providerDrag is null) return;

    _isCompletingProviderDrag = true;
    e.Pointer.Capture(null);  // 释放指针
    _ = CompleteProviderDragAsync(commit: _providerDrag.IsDragging);
    e.Handled = true;
}
```

### 13.3.6 捕获丢失处理

```csharp
private void ProviderDragHandle_OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
{
    if (_providerDrag is null || _isCompletingProviderDrag)
        return;

    _isCompletingProviderDrag = true;
    _ = CompleteProviderDragAsync(commit: false);  // 不提交变更
}
```

指针捕获可能因系统原因丢失（如弹出对话框），此时应取消拖拽而不提交排序。

## 13.4 组件详解大全

### 13.4.1 Avalonia 内置 DragDrop API

除了手动实现拖拽（如 CodexSwitch 的 Provider 列表），Avalonia 还提供内置的 DragDrop API，适合跨控件或跨应用拖拽。

**发起拖拽**

```csharp
private async void OnMouseButtonDown(object? sender, PointerPressedEventArgs e)
{
    var data = new DataObject();
    data.Set("my-format", "Hello from drag source");

    var result = await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy | DragDropEffects.Move);

    switch (result)
    {
        case DragDropEffects.Copy:
            Debug.WriteLine("Copied");
            break;
        case DragDropEffects.Move:
            Debug.WriteLine("Moved");
            break;
        case DragDropEffects.None:
            Debug.WriteLine("Cancelled");
            break;
    }
}
```

**接收拖拽**

```xml
<Border DragDrop.AllowDrop="True"
        DragDrop.Drop="OnDrop"
        DragDrop.DragEnter="OnDragEnter"
        DragDrop.DragLeave="OnDragLeave"
        DragDrop.DragOver="OnDragOver">
    <TextBlock Text="Drop here"/>
</Border>
```

```csharp
private void OnDragEnter(object? sender, DragEventArgs e)
{
    // 检查是否接受此数据格式
    if (e.Data.Contains("my-format"))
    {
        e.DragEffects = DragDropEffects.Copy;
        // 高亮放置区域
        ((Control)sender!).Classes.Add("drag-over");
    }
    else
    {
        e.DragEffects = DragDropEffects.None;
    }
}

private void OnDragLeave(object? sender, DragEventArgs e)
{
    ((Control)sender!).Classes.Remove("drag-over");
}

private void OnDragOver(object? sender, DragEventArgs e)
{
    // 持续检查并更新效果
    if (!e.Data.Contains("my-format"))
        e.DragEffects = DragDropEffects.None;
}

private void OnDrop(object? sender, DragEventArgs e)
{
    ((Control)sender!).Classes.Remove("drag-over");

    if (e.Data.Contains("my-format"))
    {
        var value = e.Data.Get("my-format") as string;
        // 处理放置的数据
    }
}
```

### 13.4.2 DataObject 和数据格式

`DataObject` 是拖拽数据的容器，支持多种格式：

```csharp
var data = new DataObject();

// 自定义格式
data.Set("my-app/provider-id", providerId);

// 标准格式
data.Set(DataFormats.Text, "Hello");
data.Set(DataFormats.Files, new[] { "/path/to/file" });

// 检查格式
if (data.Contains("my-app/provider-id"))
{
    var id = data.Get("my-app/provider-id") as string;
}
```

**标准数据格式**

| 格式 | 说明 |
|------|------|
| `DataFormats.Text` | 纯文本 |
| `DataFormats.Files` | 文件路径列表 |
| `DataFormats.Html` | HTML 文本 |

### 13.4.3 DragDropEffects

```csharp
[Flags]
public enum DragDropEffects
{
    None = 0,
    Copy = 1,     // 复制
    Move = 2,     // 移动
    Link = 4,     // 链接
    Scroll = -2147483648  // 滚动
}
```

在 `DragOver` 事件中设置 `e.DragEffects` 告诉源控件当前允许的效果。

### 13.4.4 跨进程拖拽

Avalonia 的 DragDrop API 支持跨进程拖拽（如从文件管理器拖拽文件到应用）：

```csharp
private void OnDrop(object? sender, DragEventArgs e)
{
    if (e.Data.Contains(DataFormats.Files))
    {
        var files = e.Data.GetFiles();
        foreach (var file in files)
        {
            Console.WriteLine($"Dropped file: {file.Path}");
        }
    }
}
```

### 13.4.5 拖拽视觉反馈

**拖拽图标**

```csharp
var data = new DataObject();
data.Set("my-format", myData);

// 设置自定义拖拽图标
var result = await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy,
    dragIcon: CreateDragIcon());
```

**放置区域高亮**

```xml
<Style Selector="Border.drop-zone.drag-over">
    <Setter Property="BorderBrush" Value="{StaticResource CsPrimaryBrush}"/>
    <Setter Property="BorderThickness" Value="2"/>
    <Setter Property="Background" Value="{StaticResource CsPrimaryHoverBrush}"/>
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.15"/>
            <BrushTransition Property="Background" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>
```

## 13.5 CodexSwitch 实战

### 13.5.1 拖拽核心逻辑

**开始拖拽**

```csharp
private void BeginProviderDrag(ProviderDragState state)
{
    var rows = GetProviderRows();
    if (rows.Count < 2) return;

    var originalIndex = rows.FindIndex(row =>
        string.Equals(row.Item.Id, state.Item.Id, StringComparison.OrdinalIgnoreCase));
    if (originalIndex < 0) return;

    state.IsDragging = true;
    state.Rows = rows;
    state.OriginalIndex = originalIndex;
    state.TargetIndex = originalIndex;
    state.SlotHeight = Math.Max(1,
        state.Row.Bounds.Height + state.Row.Margin.Top + state.Row.Margin.Bottom);

    // 添加 dragging 样式类，提升层级
    state.Row.Classes.Add("dragging");
    state.Row.ZIndex = 100;

    // 为所有行初始化 TranslateTransform
    foreach (var row in rows)
    {
        var transform = EnsureTranslateTransform(row.Control,
            animate: !ReferenceEquals(row.Control, state.Row));
        transform.X = 0;
        transform.Y = 0;
    }
}
```

**更新拖拽**

```csharp
private void UpdateProviderDrag(ProviderDragState state, Point currentPoint)
{
    // 移动被拖拽元素（无动画，跟随指针）
    var draggedTransform = EnsureTranslateTransform(state.Row, animate: false);
    draggedTransform.Y = currentPoint.Y - state.StartPoint.Y;

    // 计算目标位置
    var targetIndex = ResolveProviderDragTargetIndex(state, currentPoint.Y);
    if (targetIndex == state.TargetIndex) return;

    state.TargetIndex = targetIndex;

    // 让其他元素让路（带动画）
    foreach (var row in state.Rows)
    {
        if (ReferenceEquals(row.Control, state.Row)) continue;

        var offset = ResolveSiblingOffset(state, row.OriginalIndex);
        EnsureTranslateTransform(row.Control, animate: true).Y = offset;
    }
}
```

**目标索引计算**

```csharp
private int ResolveProviderDragTargetIndex(ProviderDragState state, double pointerY)
{
    var targetIndex = 0;
    foreach (var row in state.Rows)
    {
        if (ReferenceEquals(row.Control, state.Row)) continue;

        // 指针超过某行中心点时，目标索引加一
        if (pointerY > row.OriginalCenterY)
            targetIndex++;
    }
    return Math.Clamp(targetIndex, 0, state.Rows.Count - 1);
}
```

**兄弟元素偏移计算**

```csharp
private static double ResolveSiblingOffset(ProviderDragState state, int siblingIndex)
{
    // 被拖拽元素向下移动：原位置下方、目标位置上方的元素上移一个槽位
    if (state.TargetIndex > state.OriginalIndex &&
        siblingIndex > state.OriginalIndex &&
        siblingIndex <= state.TargetIndex)
    {
        return -state.SlotHeight;
    }

    // 被拖拽元素向上移动：原位置上方、目标位置下方的元素下移一个槽位
    if (state.TargetIndex < state.OriginalIndex &&
        siblingIndex >= state.TargetIndex &&
        siblingIndex < state.OriginalIndex)
    {
        return state.SlotHeight;
    }

    return 0;
}
```

### 13.5.2 归位动画与提交

```csharp
private async Task CompleteProviderDragAsync(bool commit)
{
    var state = _providerDrag;
    if (state is null) return;

    try
    {
        if (state.IsDragging)
        {
            // 动画归位到目标位置或原位
            EnsureTranslateTransform(state.Row, animate: true).Y = commit
                ? (state.TargetIndex - state.OriginalIndex) * state.SlotHeight
                : 0;
            await Task.Delay(DragSettleDuration);  // 等待 150ms 动画完成
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
```

### 13.5.3 TranslateTransform 管理

```csharp
private static TranslateTransform EnsureTranslateTransform(Control control, bool animate)
{
    if (control.RenderTransform is not TranslateTransform transform)
    {
        transform = new TranslateTransform();
        control.RenderTransform = transform;
    }

    transform.Transitions = animate
        ?
        [
            new DoubleTransition
            {
                Property = TranslateTransform.YProperty,
                Duration = DragSettleDuration,
                Easing = new CubicEaseOut()
            }
        ]
        : null;
    return transform;
}
```

被拖拽的元素不使用 Transition（`animate: false`），因为需要立即跟随指针。其他行的让路动画使用 Transition（`animate: true`），因为它们应该平滑地移动到新位置。

### 13.5.4 视觉树遍历：发现所有行

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
                top);
        })
        .OrderBy(row => row.OriginalTopY)
        .Select((row, index) => row with { OriginalIndex = index })
        .ToList();
}
```

`GetVisualDescendants()` 遍历整个视觉树，通过 CSS 类名 `provider-list-row` 识别行控件。`TranslatePoint` 将行的局部坐标转换为 `ProvidersPage` 的坐标系。

### 13.5.5 清理视觉状态

```csharp
private void ResetProviderDragVisuals(ProviderDragState state)
{
    foreach (var row in state.Rows.Count == 0
        ? [new ProviderDragRow(state.Row, state.Item, 0, 0)]
        : state.Rows)
    {
        row.Control.Classes.Remove("dragging");
        row.Control.ZIndex = 0;
        if (row.Control.RenderTransform is TranslateTransform transform)
        {
            transform.Transitions = null;  // 先清除过渡
            transform.X = 0;              // 再重置位置
            transform.Y = 0;
        }
    }
}
```

清理时先将 `Transitions` 设为 `null`，再重置 `X/Y` 为 0。否则重置操作会触发 150ms 的过渡动画。

## 13.6 举一反三

### 13.6.1 手动拖拽 vs DragDrop API

| 特性 | 手动拖拽 | DragDrop API |
|------|---------|-------------|
| 复杂度 | 高（需要自行处理所有状态） | 低（API 封装） |
| 灵活性 | 高（完全控制） | 中（受 API 限制） |
| 视觉反馈 | 完全自定义 | 部分自定义 |
| 跨控件 | 需要自行实现 | 内置支持 |
| 跨进程 | 不支持 | 支持 |
| 适用场景 | 列表排序、自定义交互 | 文件拖入、跨控件拖拽 |

### 13.6.2 与 HTML5 Drag and Drop 的对比

| HTML5 | Avalonia |
|-------|----------|
| `dragstart` | `DragDrop.DoDragDrop` |
| `dragenter` | `DragDrop.DragEnter` |
| `dragover` | `DragDrop.DragOver` |
| `dragleave` | `DragDrop.DragLeave` |
| `drop` | `DragDrop.Drop` |
| `dataTransfer` | `DataObject` |
| `effectAllowed` | `DragDropEffects` |

## 13.7 最佳实践与设计模式

### 13.7.1 阈值检测

总是使用阈值防止误触：

```csharp
if (Math.Abs(currentPoint.Y - state.StartPoint.Y) < 4)
    return;  // 移动不超过 4px，不开始拖拽
```

### 13.7.2 使用 RenderTransform 而非 Margin

```csharp
// 好：RenderTransform 不触发布局
transform.Y = currentPoint.Y - state.StartPoint.Y;

// 差：Margin 每帧触发布局计算
row.Margin = new Thickness(0, offset, 0, 0);
```

### 13.7.3 区分动画和非动画

```csharp
// 被拖拽元素：无动画（立即跟随指针）
EnsureTranslateTransform(state.Row, animate: false).Y = offset;

// 其他元素：有动画（平滑让路）
EnsureTranslateTransform(row.Control, animate: true).Y = offset;
```

## Deep Dive：输入路由与指针捕获

### 路由策略

Avalonia 的事件路由有三种策略：

| 策略 | 方向 | 用途 |
|------|------|------|
| `Direct` | 直接发送给目标 | 属性变更 |
| `Tunnel` | 从根到目标（Preview 前缀） | 预处理 |
| `Bubble` | 从目标到根 | 冒泡处理 |

拖拽使用 `Bubble` 策略：事件从拖拽句柄向上冒泡到 `ProvidersPage`。

### TranslateTransform vs Margin

拖拽使用 `TranslateTransform` 而非修改 `Margin`：

```
TranslateTransform（RenderTransform）：
    -> 不影响 Measure/Arrange
    -> 只影响最终绘制位置
    -> 性能好

Margin（Layout 属性）：
    -> 触发完整的 Measure/Arrange 流程
    -> 每帧重新计算布局
    -> 性能差
```

## Cross References

- **输入事件**：Pointer 事件系统的完整说明，参见 [第 16 章](16-input-events.md)
- **视觉/逻辑树**：`GetVisualDescendants` 和 `GetVisualAncestors`，参见 [第 23 章](23-visual-logical-tree.md)
- **动画**：拖拽归位使用的 `DoubleTransition`，参见 [第 10 章](10-animation-transitions.md)
- **数据模板**：`DataTemplate` 生成的行控件与拖拽句柄的关系，参见 [第 8 章](08-data-templates.md)
- **自定义控件**：拖拽句柄控件的创建，参见 [第 9 章](09-custom-controls.md)

## Common Pitfalls

### 陷阱 1：忘记释放指针捕获

```csharp
// 错误：PointerReleased 中未释放捕获
// 后果：后续所有 Pointer 事件都被拖拽句柄拦截

// 正确：
e.Pointer.Capture(null);
```

### 陷阱 2：没有阈值检测

```csharp
// 错误：按下即开始拖拽，正常点击也会触发

// 正确：等待超过阈值
if (Math.Abs(currentPoint.Y - state.StartPoint.Y) < 4)
    return;
```

### 陷阱 3：TranslatePoint 返回 null

`TranslatePoint` 在控件未附加到视觉树时返回 `null`。使用 `?? control.Bounds.Y` 回退。

### 陷阱 4：拖拽时触发右键菜单

通过 `_providerDrag is not null` 检查确保拖拽进行中不打开菜单：

```csharp
private void OpenProviderContextMenu(Control? source, Action markHandled)
{
    if (_providerDrag is not null || ...)  // 拖拽中忽略
        return;
    // ...
}
```

### 陷阱 5：异步归位期间状态不一致

`CompleteProviderDragAsync` 使用 `_isCompletingProviderDrag` 标志防止 `PointerCaptureLost` 触发重复的归位逻辑。`finally` 块确保无论成功失败都清理状态。

### 陷阱 6：ResetProviderDragVisuals 中的 Transitions 残留

清理时必须先将 `Transitions` 设为 `null`，再重置 `X/Y` 为 0。否则重置操作会触发 150ms 的过渡动画。

### 陷阱 7：DragDrop.DoDragDrop 是异步的

```csharp
// DoDragDrop 会等待拖拽完成后才返回
var result = await DragDrop.DoDragDrop(e, data, effects);
// 此处执行时拖拽已完成
```

### 陷阱 8：DragEnter 和 DragOver 的区别

`DragEnter` 在指针首次进入控件时触发一次，`DragOver` 在指针停留在控件上方时持续触发。放置区域高亮应在 `DragEnter` 中设置，在 `DragLeave` 中移除。

### 陷阱 9：忘记处理 PointerCaptureLost

```csharp
// 错误：不处理捕获丢失
// 后果：系统弹出对话框时，拖拽状态残留

// 正确：
private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
{
    if (_dragState is not null)
        CancelDrag();
}
```

### 陷阱 10：在 PointerMoved 中频繁分配对象

```csharp
// 错误：每帧创建新对象
private void OnPointerMoved(object? sender, PointerEventArgs e)
{
    var state = new DragState();  // 每帧分配
    // ...
}

// 正确：复用状态对象
private void OnPointerMoved(object? sender, PointerEventArgs e)
{
    _dragState.Update(e.GetPosition(this));
}
```

### 陷阱 11：GetVisualDescendants 性能问题

对于大列表，`GetVisualDescendants()` 每次遍历整个视觉树可能成为瓶颈。考虑在拖拽开始时缓存行列表。

### 陷阱 12：ZIndex 在不同平台的行为差异

`ZIndex` 只在同一容器的兄弟元素之间有效。如果被拖拽元素的父容器与其他元素的父容器不同，`ZIndex` 可能不生效。

## Try It Yourself

### 练习 1：添加拖拽视觉反馈

在 `BeginProviderDrag` 中为被拖拽行添加阴影效果：

```xml
<Style Selector="Border.provider-list-row.dragging">
    <Setter Property="BoxShadow" Value="0 4 12 0 #40000000"/>
    <Setter Property="Opacity" Value="0.9"/>
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>
```

### 练习 2：实现水平拖拽

修改拖拽逻辑，同时支持水平方向移动。在 `UpdateProviderDrag` 中增加 X 轴的 `TranslateTransform.X` 更新。

### 练习 3：实现跨列表拖拽

创建两个列表，实现元素从列表 A 拖到列表 B。提示：需要在 `PointerMoved` 中检测指针是否进入另一个列表区域。

### 练习 4：使用 DragDrop API 实现文件拖入

创建一个文件拖入区域：

```xml
<Border DragDrop.AllowDrop="True"
        DragDrop.Drop="OnFileDrop"
        DragDrop.DragEnter="OnFileDragEnter"
        DragDrop.DragLeave="OnFileDragLeave"
        Classes="drop-zone">
    <TextBlock Text="拖放文件到此处"/>
</Border>
```

### 练习 5：优化大列表性能

当列表有 100+ 项时，`GetProviderRows` 每次遍历视觉树可能成为性能瓶颈。尝试缓存行列表，只在拖拽开始时重建。

### 练习 6：添加键盘辅助

在拖拽过程中，允许使用上/下箭头键微调位置，Enter 键确认，Escape 键取消：

```csharp
protected override void OnKeyDown(KeyEventArgs e)
{
    if (_providerDrag is { IsDragging: true })
    {
        switch (e.Key)
        {
            case Key.Escape:
                _ = CompleteProviderDragAsync(commit: false);
                e.Handled = true;
                break;
            case Key.Enter:
                _ = CompleteProviderDragAsync(commit: true);
                e.Handled = true;
                break;
            case Key.Up:
                MoveTargetIndex(-1);
                e.Handled = true;
                break;
            case Key.Down:
                MoveTargetIndex(1);
                e.Handled = true;
                break;
        }
    }
}
```

### 练习 7：实现自定义拖拽预览

在拖拽过程中显示一个半透明的缩略图跟随鼠标：

```csharp
private void ShowDragPreview(Control source, Point position)
{
    var preview = new Border
    {
        Width = source.Bounds.Width,
        Height = source.Bounds.Height,
        Opacity = 0.7,
        Background = Brushes.White,
        // ... 复制源控件的视觉内容
    };
    Canvas.SetLeft(preview, position.X - source.Bounds.Width / 2);
    Canvas.SetTop(preview, position.Y - source.Bounds.Height / 2);
    _overlay.Children.Add(preview);
}
```

### 练习 8：实现触摸拖拽

Avalonia 的 Pointer 事件天然支持触摸。测试拖拽排序在触摸屏上的行为：
- 长按 500ms 开始拖拽（而非按下即准备）
- 拖拽过程中显示放置预览
- 释放时有振动反馈（如果平台支持）
