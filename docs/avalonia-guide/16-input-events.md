# 16. 输入处理与事件系统

> **写给零基础的你**：事件就是"发生了什么事"。你点了鼠标——这是一个事件；你按了键盘——这也是一个事件；手指在屏幕上滑动——还是一个事件。程序需要知道这些事件，才能做出反应（比如点击按钮后执行操作）。

> **路由事件是什么？** 想象你在一个大楼里喊了一声"着火了！"。声音先传到你所在的房间（直接事件），然后传到走廊（冒泡），最后传到大楼门口。路由事件也一样——你在按钮上点击，事件会从按钮传到按钮的父容器，再传到窗口。

## 16.1 概述

输入处理是桌面应用与用户交互的核心。Avalonia 提供了一套完整的输入事件系统，涵盖指针（鼠标/触摸）事件、键盘事件、手势识别、拖放操作以及焦点管理。理解这套系统对于实现自定义交互（如拖拽排序、画布绘图、快捷键）至关重要。

在 CodexSwitch 中，输入事件系统被广泛应用于：
- **拖拽排序**：通过 `PointerPressed` / `PointerMoved` / `PointerReleased` 实现 Provider 列表的拖拽重排
- **右键菜单**：通过 `ContextRequested` 事件弹出上下文操作
- **键盘快捷键**：通过 `KeyGesture` 和 `KeyBinding` 实现全局快捷操作
- **焦点管理**：对话框打开时自动聚焦到输入框

本章将从基础到高级，全面讲解 Avalonia 的输入处理机制。

## 16.2 路由事件系统

### 16.2.1 什么是路由事件

路由事件（Routed Event）是 Avalonia 事件系统的核心概念。与普通 .NET 事件不同，路由事件可以在控件树中传播，使得父控件能够拦截或响应子控件的事件。

Avalonia 支持三种路由策略：

> **小白提示**：想象你在一个大楼里发生了事情：
> - **Direct（直接）** = 只有你知道，别人不知道（事件只在触发的控件上执行）
> - **Tunnel（隧道/预览）** = 从楼顶向下逐层通知（事件从窗口根向下传递到目标）
> - **Bubble（冒泡）** = 从你所在的位置向上逐层汇报（事件从目标向上传递到窗口）

| 策略 | 方向 | 说明 | 典型事件 | 类比 |
|------|------|------|---------|------|
| `Direct` | 仅目标控件 | 事件只在触发它的控件上执行 | `GotFocus`, `LostFocus` | 只有你知道 |
| `Tunnel` | 从根到目标 | 从窗口根向下逐级传递 | `PreviewPointerPressed`, `PreviewKeyDown` | 从楼顶向下通知 |
| `Bubble` | 从目标到根 | 从目标控件向上传递 | `PointerPressed`, `KeyDown`, `Tapped` | 从你向上汇报 |

### 16.2.2 事件传播顺序

当用户点击一个嵌套在多层容器中的按钮时，事件的传播顺序如下：

```
Window (Tunnel: PreviewPointerPressed)
  └── Grid (Tunnel: PreviewPointerPressed)
      └── StackPanel (Tunnel: PreviewPointerPressed)
          └── Button (Target: PointerPressed, 标记 Handled=true)
              └── ContentPresenter
                  └── TextBlock

完整传播顺序：
1. Window.PreviewPointerPressed   (隧道，最先触发)
2. Grid.PreviewPointerPressed     (隧道)
3. StackPanel.PreviewPointerPressed (隧道)
4. Button.PointerPressed          (目标，标记 Handled=true)
5. StackPanel.PointerPressed      (冒泡，如果已处理则跳过)
6. Grid.PointerPressed            (冒泡，如果已处理则跳过)
7. Window.PointerPressed          (冒泡，如果已处理则跳过)
```

关键规则：
- 隧道事件总是在冒泡事件之前触发
- 如果在隧道阶段标记 `Handled = true`，冒泡阶段不会触发
- 使用 `handledEventsToo: true` 可以接收已被标记为已处理的事件

### 16.2.3 注册路由事件处理器

```csharp
// 方式一：在 XAML 中直接绑定事件
<Border PointerPressed="OnPointerPressed"/>

// 方式二：在代码中注册冒泡事件
AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);

// 方式三：注册隧道事件（Preview 前缀）
AddHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);

// 方式四：接收已处理的事件（用于全局监听）
AddHandler(InputElement.PointerPressedEvent, OnAnyPointerPressed,
    RoutingStrategies.Bubble, handledEventsToo: true);
```

### 16.2.4 标记事件为已处理

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    // 处理事件
    DoSomething();

    // 标记为已处理，阻止冒泡
    e.Handled = true;
}
```

### 16.2.5 路由事件的完整生命周期

```csharp
// 完整示例：监听一个按钮点击事件在整条路径上的传播
public class EventTracingWindow : Window
{
    public EventTracingWindow()
    {
        // 隧道阶段：从窗口向下
        AddHandler(PointerPressedEvent, (s, e) =>
        {
            Debug.WriteLine($"[Tunnel] {((Control)s!).Name} 收到 PointerPressed");
        }, RoutingStrategies.Tunnel);

        // 冒泡阶段：从目标向上
        AddHandler(PointerPressedEvent, (s, e) =>
        {
            Debug.WriteLine($"[Bubble] {((Control)s!).Name} 收到 PointerPressed");
        }, RoutingStrategies.Bubble);

        // 即使事件已处理也触发
        AddHandler(PointerPressedEvent, (s, e) =>
        {
            Debug.WriteLine($"[Always] {((Control)s!).Name} 收到 PointerPressed (handled={e.Handled})");
        }, RoutingStrategies.Bubble, handledEventsToo: true);
    }
}
```

## 16.3 指针事件详解

### 16.3.1 指针事件家族

Avalonia 的指针事件统一了鼠标和触摸输入，提供一致的 API：

| 事件 | 参数类型 | 触发时机 | 用途 |
|------|---------|---------|------|
| `PointerPressed` | `PointerPressedEventArgs` | 按钮按下 | 拖拽开始、自定义交互 |
| `PointerReleased` | `PointerReleasedEventArgs` | 按钮释放 | 拖拽结束、点击完成 |
| `PointerMoved` | `PointerEventArgs` | 指针移动 | 拖拽移动、悬停效果 |
| `PointerEntered` | `PointerEventArgs` | 指针进入控件 | 悬停高亮 |
| `PointerExited` | `PointerEventArgs` | 指针离开控件 | 取消高亮 |
| `PointerCaptureLost` | `PointerCaptureLostEventArgs` | 捕获丢失 | 清理拖拽状态 |
| `PointerWheelChanged` | `PointerWheelEventArgs` | 滚轮滚动 | 自定义滚动行为 |

### 16.3.2 PointerPressed 完整用法

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    // 获取指针信息
    var point = e.GetCurrentPoint(this);

    // 检查按下的按钮
    if (point.Properties.IsLeftButtonPressed)
    {
        Debug.WriteLine("左键按下");
    }
    if (point.Properties.IsRightButtonPressed)
    {
        Debug.WriteLine("右键按下");
    }
    if (point.Properties.IsMiddleButtonPressed)
    {
        Debug.WriteLine("中键按下");
    }

    // 检测双击（ClickCount >= 2）
    if (e.ClickCount >= 2)
    {
        Debug.WriteLine("双击");
        return; // 双击时不做拖拽处理
    }

    // 获取相对于指定控件的坐标
    var position = e.GetPosition(this);
    Debug.WriteLine($"点击位置: ({position.X:F0}, {position.Y:F0})");

    // 获取相对于屏幕的坐标
    var screenPosition = e.GetPosition(null);
    Debug.WriteLine($"屏幕位置: ({screenPosition.X:F0}, {screenPosition.Y:F0})");

    // 捕获指针（用于后续的 PointerMoved 和 PointerReleased）
    e.Pointer.Capture(this);

    // 标记为已处理
    e.Handled = true;
}
```

### 16.3.3 PointerMoved 与拖拽实现

```csharp
private Point _dragStartPoint;
private bool _isDragging;

private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    _dragStartPoint = e.GetPosition(this);
    _isDragging = false;
    e.Pointer.Capture(this);
    e.Handled = true;
}

private void OnPointerMoved(object? sender, PointerEventArgs e)
{
    // 只在捕获状态下处理移动
    if (e.Pointer.Captured != this) return;

    var currentPosition = e.GetPosition(this);
    var delta = currentPosition - _dragStartPoint;

    // 设置拖拽阈值，避免误触
    if (!_isDragging && delta.Length < 4) return;

    _isDragging = true;

    // 更新拖拽元素的位置
    var transform = RenderTransform as TranslateTransform;
    if (transform == null)
    {
        transform = new TranslateTransform();
        RenderTransform = transform;
    }
    transform.X += delta.X;
    transform.Y += delta.Y;

    _dragStartPoint = currentPosition;
}
```

### 16.3.4 PointerReleased 与捕获释放

```csharp
private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
{
    // 检查释放的是哪个按钮
    var point = e.GetPosition(this);

    // 必须释放指针捕获
    e.Pointer.Capture(null);

    if (_isDragging)
    {
        // 拖拽结束，执行排序逻辑
        CommitDragReorder();
    }
    else
    {
        // 没有拖拽，视为普通点击
        ExecuteClickAction();
    }

    _isDragging = false;
    e.Handled = true;
}
```

### 16.3.5 PointerEntered 与 PointerExited

```csharp
// 悬停效果
private void OnPointerEntered(object? sender, PointerEventArgs e)
{
    // 鼠标进入时高亮
    Background = HoverBrush;
    Cursor = new Cursor(StandardCursorType.Hand);
}

private void OnPointerExited(object? sender, PointerEventArgs e)
{
    // 鼠标离开时恢复
    Background = NormalBrush;
    Cursor = new Cursor(StandardCursorType.Arrow);
}
```

在 XAML 中也可以通过样式伪类实现：

```xml
<Style Selector="Border.my-card">
    <Setter Property="Background" Value="Transparent"/>
</Style>
<Style Selector="Border.my-card:pointerover">
    <Setter Property="Background" Value="{DynamicResource HoverBrush}"/>
</Style>
```

### 16.3.6 PointerCaptureLost

指针捕获可能在以下情况丢失：
- 窗口失去焦点
- 另一个控件获取了捕获
- 系统级中断（如来电、通知）

```csharp
private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
{
    // 清理所有拖拽状态
    _isDragging = false;
    CancelDragOperation();
    ResetVisualState();
}
```

### 16.3.7 PointerWheelChanged

```csharp
private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
{
    // e.Delta.Y > 0 向上滚动，< 0 向下滚动
    // e.Delta.X 横向滚动（触控板）

    if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
    {
        // Ctrl + 滚轮 = 缩放
        _zoomLevel += e.Delta.Y * 0.1;
        _zoomLevel = Math.Clamp(_zoomLevel, 0.1, 5.0);
        ApplyZoom(_zoomLevel);
        e.Handled = true;
    }
    else
    {
        // 普通滚动
        _scrollOffset -= e.Delta.Y * 40;
        ApplyScroll(_scrollOffset);
        e.Handled = true;
    }
}
```

### 16.3.8 PointerPressed vs Tapped

| 事件 | 触发时机 | 触摸支持 | 用途 |
|------|---------|---------|------|
| `PointerPressed` | 按下瞬间 | 是 | 拖拽开始、自定义交互 |
| `PointerReleased` | 释放瞬间 | 是 | 拖拽结束 |
| `Tapped` | 完整点击（按下+释放） | 是 | 通用点击（推荐） |
| `DoubleTapped` | 快速双击 | 是 | 双击操作 |
| `RightTapped` | 右键点击 | 长按 | 上下文菜单 |

**最佳实践**：对于普通点击操作，优先使用 `Tapped` 和 `DoubleTapped`，因为它们在触摸设备上表现更好。只在需要精确控制按下/释放行为时使用 `PointerPressed` / `PointerReleased`。

## 16.4 键盘事件详解

### 16.4.1 键盘事件家族

| 事件 | 参数类型 | 触发时机 | 路由策略 |
|------|---------|---------|---------|
| `PreviewKeyDown` | `KeyEventArgs` | 按键按下（隧道） | Tunnel |
| `KeyDown` | `KeyEventArgs` | 按键按下（冒泡） | Bubble |
| `PreviewKeyUp` | `KeyEventArgs` | 按键释放（隧道） | Tunnel |
| `KeyUp` | `KeyEventArgs` | 按键释放（冒泡） | Bubble |
| `TextInput` | `TextInputEventArgs` | 文本输入 | Direct |
| `PreviewTextInput` | `TextInputEventArgs` | 文本输入（隧道） | Tunnel |

### 16.4.2 KeyDown 完整用法

```csharp
private void OnKeyDown(object? sender, KeyEventArgs e)
{
    // 检查单个按键
    switch (e.Key)
    {
        case Key.Enter:
            SubmitForm();
            e.Handled = true;
            break;

        case Key.Escape:
            CancelDialog();
            e.Handled = true;
            break;

        case Key.Delete:
        case Key.Back:
            DeleteSelectedItem();
            e.Handled = true;
            break;

        case Key.F2:
            RenameSelectedItem();
            e.Handled = true;
            break;
    }

    // 检查组合键
    if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
    {
        switch (e.Key)
        {
            case Key.S:
                SaveDocument();
                e.Handled = true;
                break;
            case Key.Z:
                Undo();
                e.Handled = true;
                break;
            case Key.Y:
                Redo();
                e.Handled = true;
                break;
            case Key.A:
                SelectAll();
                e.Handled = true;
                break;
            case Key.F:
                OpenSearch();
                e.Handled = true;
                break;
        }
    }

    // macOS 上使用 Command 键
    if (e.KeyModifiers.HasFlag(KeyModifiers.Meta))
    {
        switch (e.Key)
        {
            case Key.S:
                SaveDocument();
                e.Handled = true;
                break;
        }
    }
}
```

### 16.4.3 TextInput 事件

`TextInput` 事件在实际字符被输入时触发，适合处理文本输入：

```csharp
private void OnTextInput(object? sender, TextInputEventArgs e)
{
    // e.Text 是输入的文本（可能是一个或多个字符）
    if (e.Text is not null)
    {
        // 只允许数字输入
        if (!char.IsDigit(e.Text[0]))
        {
            e.Handled = true; // 阻止非数字输入
        }
    }
}
```

### 16.4.4 常用键值参考

```csharp
// 功能键
Key.Enter, Key.Escape, Key.Tab, Key.Space, Key.Back, Key.Delete

// 方向键
Key.Up, Key.Down, Key.Left, Key.Right

// 修饰键
Key.LeftCtrl, Key.RightCtrl, Key.LeftShift, Key.RightShift
Key.LeftAlt, Key.RightAlt, Key.LWin, Key.RWin

// 字母键
Key.A through Key.Z

// 数字键
Key.D0 through Key.D9（主键盘）
Key.NumPad0 through Key.NumPad9（数字小键盘）

// 功能键
Key.F1 through Key.F24

// 媒体键
Key.MediaPlayPause, Key.MediaStop, Key.MediaNextTrack, Key.MediaPreviousTrack
Key.VolumeUp, Key.VolumeDown, Key.VolumeMute
```

### 16.4.5 键盘事件与文本框的交互

```csharp
// 在 TextBox 中处理回车键
private void OnKeyDown(object? sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
    {
        // Enter 提交，Shift+Enter 换行
        SubmitMessage();
        e.Handled = true;
    }
}

// 阻止特定按键传递到 TextBox
private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
{
    // 在隧道阶段拦截，TextBox 不会收到这个按键
    if (e.Key == Key.Tab)
    {
        FocusNextControl();
        e.Handled = true;
    }
}
```

## 16.5 手势识别

### 16.5.1 内置手势事件

Avalonia 提供以下内置手势事件：

```xml
<!-- 单击 -->
<Border Tapped="OnTapped"/>

<!-- 双击 -->
<Border DoubleTapped="OnDoubleTapped"/>

<!-- 右键点击 -->
<Border RightTapped="OnRightTapped"/>
```

```csharp
private void OnTapped(object? sender, TappedEventArgs e)
{
    // 普通点击
    var position = e.GetPosition(this);
    ExecuteClick(position);
}

private void OnDoubleTapped(object? sender, TappedEventArgs e)
{
    // 双击 - 常用于进入编辑模式
    EnterEditMode();
    e.Handled = true; // 阻止触发单击
}
```

### 16.5.2 自定义长按检测

Avalonia 没有内置的长按事件，需要手动实现：

```csharp
public class LongPressDetector
{
    private CancellationTokenSource? _cts;
    private const int LongPressThresholdMs = 500;

    public event Action<PointerPressedEventArgs>? LongPressed;

    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var args = e;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(LongPressThresholdMs, _cts.Token);
                // 在 UI 线程触发事件
                Dispatcher.UIThread.Post(() => LongPressed?.Invoke(args));
            }
            catch (TaskCanceledException) { }
        });
    }

    public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _cts?.Cancel();
    }

    public void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        // 移动超过阈值取消长按
        _cts?.Cancel();
    }
}
```

### 16.5.3 自定义滑动检测

```csharp
public class SwipeDetector
{
    private Point _startPoint;
    private DateTime _startTime;
    private const double MinSwipeDistance = 50;
    private const int MaxSwipeDurationMs = 300;

    public event Action<SwipeDirection>? Swiped;

    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _startPoint = e.GetPosition(null);
        _startTime = DateTime.UtcNow;
    }

    public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var endPoint = e.GetPosition(null);
        var duration = DateTime.UtcNow - _startTime;

        if (duration.TotalMilliseconds > MaxSwipeDurationMs) return;

        var delta = endPoint - _startPoint;
        if (delta.Length < MinSwipeDistance) return;

        // 判断方向
        if (Math.Abs(delta.X) > Math.Abs(delta.Y))
        {
            Swiped?.Invoke(delta.X > 0 ? SwipeDirection.Right : SwipeDirection.Left);
        }
        else
        {
            Swiped?.Invoke(delta.Y > 0 ? SwipeDirection.Down : SwipeDirection.Up);
        }
    }
}

public enum SwipeDirection { Left, Right, Up, Down }
```

## 16.6 InputElement 常用属性

### 16.6.1 IsHitTestVisible

控制控件是否可以接收指针事件：

```xml
<!-- 背景遮罩层：可以接收点击（用于关闭弹出层） -->
<Border Background="#80000000" IsHitTestVisible="True"/>

<!-- 装饰性元素：不接收点击，事件穿透到下层 -->
<Border Background="Transparent" IsHitTestVisible="False"/>

<!-- 禁用整个区域的输入 -->
<Panel IsHitTestVisible="{Binding IsInteractive}">
    <!-- 子元素 -->
</Panel>
```

```csharp
// 禁用输入但保持可见（不同于 IsEnabled）
myControl.IsHitTestVisible = false;

// 区别：
// IsHitTestVisible = false: 控件可见但不接收输入，样式不变
// IsEnabled = false: 控件可见但不接收输入，样式变为灰色
```

### 16.6.2 Focusable 与 IsTabStop

```xml
<!-- 可以接收焦点的控件 -->
<TextBox Focusable="True" TabIndex="0"/>

<!-- 不参与 Tab 导航但可以被代码聚焦 -->
<Border Focusable="True" IsTabStop="False" TabIndex="-1"/>
```

```csharp
// Focusable: 控件是否可以接收焦点
myControl.Focusable = true;

// IsTabStop: 控件是否在 Tab 导航序列中
myControl.IsTabStop = true;

// TabIndex: Tab 导航顺序（-1 表示不参与，0+ 表示顺序）
myControl.TabIndex = 0;
```

### 16.6.3 IsEnabled

```xml
<!-- 禁用控件 -->
<Button Content="Submit" IsEnabled="{Binding CanSubmit}"/>

<!-- 禁用整个区域 -->
<StackPanel IsEnabled="{Binding IsFormEditable}">
    <TextBox Text="{Binding Name}"/>
    <TextBox Text="{Binding Email}"/>
</StackPanel>
```

```csharp
// IsEnabled 影响所有子控件
// 禁用时：控件变为灰色，不接收输入，不触发命令
myControl.IsEnabled = false;

// 与命令的 CanExecute 联动
// 当 Command.CanExecute 返回 false 时，Button 自动设置 IsEnabled = false
```

## 16.7 焦点管理

### 16.7.1 FocusManager

```csharp
// 获取当前焦点元素
var focused = FocusManager.GetFocusManager(this)?.GetFocusedElement();

// 程序化聚焦
myControl.Focus();

// 聚焦并选中文本（TextBox）
myTextBox.Focus();
myTextBox.SelectAll();

// 尝试聚焦（如果不可聚焦返回 false）
bool success = myControl.Focus();
```

### 16.7.2 焦点事件

```csharp
// GotFocus 和 LostFocus 是直接事件（Direct），不会冒泡
myControl.GotFocus += (s, e) =>
{
    Debug.WriteLine("获得焦点");
    // 添加焦点指示样式
    Classes.Add("focused");
};

myControl.LostFocus += (s, e) =>
{
    Debug.WriteLine("失去焦点");
    // 移除焦点指示样式
    Classes.Remove("focused");
};
```

在 XAML 中通过样式伪类处理焦点状态：

```xml
<Style Selector="TextBox">
    <Setter Property="BorderBrush" Value="Gray"/>
</Style>
<Style Selector="TextBox:focus">
    <Setter Property="BorderBrush" Value="Blue"/>
</Style>
```

### 16.7.3 焦点导航

```csharp
// Tab 导航方向
// Tab = 下一个，Shift+Tab = 上一个
// 方向键可以在可聚焦元素间导航

// 自定义焦点导航
protected override void OnKeyDown(KeyEventArgs e)
{
    if (e.Key == Key.Tab)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            FocusManager.GetFocusManager(this)?.MoveFocus(NavigationDirection.Previous);
        else
            FocusManager.GetFocusManager(this)?.MoveFocus(NavigationDirection.Next);
        e.Handled = true;
        return;
    }
    base.OnKeyDown(e);
}
```

### 16.7.4 焦点捕获

```csharp
// 在对话框中锁定焦点
private void OpenDialog()
{
    _dialog.IsVisible = true;
    _dialog.Focus(); // 对话框获得焦点

    // 焦点陷阱：Tab 导航只在对话框内循环
    // 这通常通过对话框的 Template 实现
}
```

## 16.8 键盘快捷键（KeyGesture 与 KeyBinding）

### 16.8.1 在 AXAML 中定义快捷键

```xml
<Window>
    <Window.KeyBindings>
        <!-- Ctrl+S 保存 -->
        <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}"/>

        <!-- Ctrl+N 新建 -->
        <KeyBinding Gesture="Ctrl+N" Command="{Binding NewCommand}"/>

        <!-- Ctrl+Shift+N 新建窗口 -->
        <KeyBinding Gesture="Ctrl+Shift+N" Command="{Binding NewWindowCommand}"/>

        <!-- Delete 删除 -->
        <KeyBinding Gesture="Delete" Command="{Binding DeleteCommand}"/>

        <!-- F2 重命名 -->
        <KeyBinding Gesture="F2" Command="{Binding RenameCommand}"/>

        <!-- Escape 取消 -->
        <KeyBinding Gesture="Escape" Command="{Binding CancelCommand}"/>

        <!-- Ctrl+Q 退出 -->
        <KeyBinding Gesture="Ctrl+Q" Command="{Binding ExitCommand}"/>
    </Window.KeyBindings>
</Window>
```

### 16.8.2 在代码中创建 KeyBinding

```csharp
var keyBinding = new KeyBinding
{
    Gesture = new KeyGesture(Key.S, KeyModifiers.Control),
    Command = SaveCommand
};
KeyBindings.Add(keyBinding);
```

### 16.8.3 动态快捷键

```csharp
// 根据上下文动态设置快捷键
private void UpdateKeyBindings()
{
    KeyBindings.Clear();

    if (CurrentPage == "Editor")
    {
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.S, KeyModifiers.Control),
            Command = SaveDocumentCommand
        });
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Z, KeyModifiers.Control),
            Command = UndoCommand
        });
    }
    else if (CurrentPage == "List")
    {
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Delete),
            Command = DeleteSelectedCommand
        });
    }
}
```

### 16.8.4 跨平台快捷键适配

```csharp
// macOS 使用 Command 键，Windows/Linux 使用 Ctrl 键
public static KeyGesture PlatformKeyGesture(Key key)
{
    var modifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
    return new KeyGesture(key, modifier);
}

// 使用
KeyBindings.Add(new KeyBinding
{
    Gesture = PlatformKeyGesture(Key.S),
    Command = SaveCommand
});
```

## 16.9 拖放 (Drag and Drop)

### 16.9.1 发起拖放

```csharp
private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    var data = new DataObject();
    data.Set("text/plain", "要传递的文本");
    data.Set("application/x-myapp-item", itemId); // 自定义格式

    var result = await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy | DragDropEffects.Move);

    // result 是最终的拖放效果
    switch (result)
    {
        case DragDropEffects.Copy:
            Debug.WriteLine("复制完成");
            break;
        case DragDropEffects.Move:
            Debug.WriteLine("移动完成");
            break;
        case DragDropEffects.None:
            Debug.WriteLine("拖放取消");
            break;
    }
}
```

### 16.9.2 接收拖放

```xml
<Border DragDrop.AllowDrop="True"
        DragDrop.Drop="OnDrop"
        DragDrop.DragEnter="OnDragEnter"
        DragDrop.DragLeave="OnDragLeave"
        DragDrop.DragOver="OnDragOver">
    <!-- 内容 -->
</Border>
```

```csharp
private void OnDragEnter(object? sender, DragEventArgs e)
{
    // 检查是否接受此数据
    if (e.Data.Contains("text/plain"))
    {
        e.DragEffects = DragDropEffects.Copy;
        // 添加视觉反馈
        Classes.Add("drag-over");
    }
    else
    {
        e.DragEffects = DragDropEffects.None;
    }
}

private void OnDragOver(object? sender, DragEventArgs e)
{
    // DragOver 持续触发，可以更新拖放位置指示
    e.DragEffects = e.Data.Contains("text/plain")
        ? DragDropEffects.Copy
        : DragDropEffects.None;
}

private void OnDragLeave(object? sender, DragEventArgs e)
{
    // 移除视觉反馈
    Classes.Remove("drag-over");
}

private void OnDrop(object? sender, DragEventArgs e)
{
    Classes.Remove("drag-over");

    if (e.Data.Contains("text/plain"))
    {
        var text = e.Data.GetText();
        ProcessDroppedText(text);
    }
}
```

## 16.10 CodexSwitch 实战：拖拽排序

### 16.10.1 XAML 定义

在 CodexSwitch 的 `ProvidersPage.axaml` 中，拖拽手柄通过指针事件实现：

```xml
<Border Classes="provider-drag-handle"
        PointerPressed="ProviderDragHandle_OnPointerPressed"
        PointerMoved="ProviderDragHandle_OnPointerMoved"
        PointerReleased="ProviderDragHandle_OnPointerReleased"
        PointerCaptureLost="ProviderDragHandle_OnPointerCaptureLost">
    <lucide:LucideIcon Kind="GripVertical" Size="18" StrokeWidth="2"/>
</Border>
```

### 16.10.2 样式配合

```xml
<!-- 拖拽手柄的基础样式 -->
<Style Selector="Border.provider-drag-handle">
    <Setter Property="Width" Value="22"/>
    <Setter Property="MinHeight" Value="46"/>
    <Setter Property="Background" Value="Transparent"/>
</Style>

<!-- 拖拽手柄悬停时图标变色 -->
<Style Selector="Border.provider-drag-handle:pointerover lucide|LucideIcon">
    <Setter Property="Foreground" Value="{DynamicResource CodexSwitch.ForegroundBrush}"/>
</Style>

<!-- 拖拽中的行半透明 -->
<Style Selector="Grid.provider-list-row.dragging">
    <Setter Property="Opacity" Value="0.92"/>
</Style>
```

### 16.10.3 Code-Behind 处理

```csharp
// 伪代码：CodexSwitch 拖拽实现的核心逻辑
private ProviderDragState? _providerDrag;

private void ProviderDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (sender is not Control handle) return;

    // 获取数据上下文
    var item = handle.DataContext as ProviderListItem;
    if (item == null) return;

    // 初始化拖拽状态
    _providerDrag = new ProviderDragState(item, e.GetPosition(this));

    // 捕获指针
    e.Pointer.Capture(handle);
    e.Handled = true;
}

private void ProviderDragHandle_OnPointerMoved(object? sender, PointerEventArgs e)
{
    if (_providerDrag == null || e.Pointer.Captured == null) return;

    var currentPosition = e.GetPosition(this);
    var delta = currentPosition - _providerDrag.StartPoint;

    // 拖拽阈值检测
    if (!_providerDrag.IsActive && Math.Abs(delta.Y) < 4) return;
    _providerDrag.IsActive = true;

    // 更新视觉反馈（移动行位置）
    UpdateDragVisual(delta.Y);

    // 计算目标位置（用于排序）
    CalculateDropTarget(currentPosition);
}

private void ProviderDragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
{
    if (_providerDrag?.IsActive == true)
    {
        // 执行排序
        CommitProviderReorder();
    }

    // 清理状态
    _providerDrag = null;
    e.Pointer.Capture(null);
    e.Handled = true;
}

private void ProviderDragHandle_OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
{
    // 捕获丢失时清理状态
    _providerDrag = null;
    ResetDragVisual();
}
```

### 16.10.4 右键菜单实现

CodexSwitch 还展示了通过代码创建上下文菜单的模式：

```csharp
// 程序化创建上下文菜单
public static void OpenFor(Control target, MainWindowViewModel viewModel, ProviderListItem item)
{
    var menu = new ContextMenu();

    var editItem = new MenuItem
    {
        Header = I18nService.Current.Translate("providers.edit")
    };
    editItem.Click += (_, _) => viewModel.EditProvider(item.Id);
    menu.Items.Add(editItem);

    var deleteItem = new MenuItem
    {
        Header = I18nService.Current.Translate("providers.delete")
    };
    deleteItem.Click += (_, _) => viewModel.DeleteProvider(item.Id);
    menu.Items.Add(deleteItem);

    menu.Open(target);
}
```

## 16.11 常见组件用法大全

### 16.11.1 Cursor 光标设置

```xml
<!-- 设置光标样式 -->
<Border Cursor="Hand"/>
<Border Cursor="Cross"/>
<Border Cursor="IBeam"/>
<Border Cursor="SizeAll"/>
<Border Cursor="SizeNS"/>
<Border Cursor="SizeWE"/>
```

```csharp
// 动态设置光标
myControl.Cursor = new Cursor(StandardCursorType.Hand);
myControl.Cursor = new Cursor(StandardCursorType.Wait);

// 自定义光标
using var stream = AssetLoader.Open(new Uri("avares://MyApp/Assets/cursor.cur"));
myControl.Cursor = new Cursor(stream);
```

### 16.11.2 GestureRecognizer（实验性）

```csharp
// 旋转手势
var rotateGesture = new RotateGestureRecognizer();
rotateGesture.RotationChanged += (s, e) =>
{
    _rotationAngle += e.Delta;
};
myControl.GestureRecognizers.Add(rotateGesture);

// 缩放手势
var pinchGesture = new PinchGestureRecognizer();
pinchGesture.PinchChanged += (s, e) =>
{
    _scale *= e.Scale;
};
myControl.GestureRecognizers.Add(pinchGesture);

// 拖拽手势
var dragGesture = new DragGestureRecognizer();
dragGesture.DragStarting += (s, e) =>
{
    e.Data.Set("text/plain", draggedItem.Text);
    e.DragEffects = DragDropEffects.Move;
};
myControl.GestureRecognizers.Add(dragGesture);
```

## 16.12 最佳实践

### 什么时候用什么事件

| 场景 | 推荐事件 | 原因 |
|------|---------|------|
| 普通按钮点击 | `Tapped` 或 `Command` | 触摸友好，MVVM 友好 |
| 双击操作 | `DoubleTapped` | 内置双击检测 |
| 右键菜单 | `RightTapped` 或 `ContextRequested` | 标准右键行为 |
| 拖拽排序 | `PointerPressed/Moved/Released` | 需要精确控制按下/移动/释放 |
| 拖放文件 | `DragDrop.DoDragDrop` | 系统级拖放 |
| 悬停效果 | `PointerEntered/Exited` 或 `:pointerover` 伪类 | 样式方式更简洁 |
| 全局快捷键 | `KeyBinding` | 声明式，MVVM 友好 |
| 表单提交 | `KeyDown` (Enter) | 需要拦截默认行为 |
| 自定义滚动 | `PointerWheelChanged` | 需要自定义滚动逻辑 |

### 事件处理原则

1. **优先使用 Command**：按钮点击等标准交互尽量使用 Command 绑定
2. **使用路由策略**：在父容器上统一处理子元素的事件
3. **及时释放捕获**：在 `PointerReleased` 和 `PointerCaptureLost` 中清理状态
4. **标记 Handled**：处理过的事件标记 `e.Handled = true` 防止重复处理
5. **设置拖拽阈值**：避免 4px 以内的微小移动被误判为拖拽

## 16.13 Deep Dive：指针捕获机制

### 16.13.1 为什么需要指针捕获

在拖拽操作中，用户可能会将鼠标移出被拖拽元素的边界。如果没有捕获，`PointerMoved` 事件会停止触发（因为鼠标不在元素上了），拖拽就会中断。指针捕获确保所有后续的指针事件都发送到捕获者，无论鼠标在哪里。

```
没有捕获：
  鼠标在元素内 → PointerMoved 触发
  鼠标移出元素 → PointerMoved 停止触发 → 拖拽中断

有捕获：
  鼠标在元素内 → PointerMoved 触发
  鼠标移出元素 → PointerMoved 仍然触发 → 拖拽正常
```

### 16.13.2 捕获的生命周期

```csharp
// 1. 获取捕获
e.Pointer.Capture(this);

// 2. 捕获后所有指针事件都发送到 this
// 即使鼠标在窗口外，只要鼠标按钮按着，事件仍然触发

// 3. 释放捕获
e.Pointer.Capture(null);

// 4. 捕获丢失（被动释放）
// 窗口失焦、另一个控件获取捕获等
private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
{
    // 必须在这里清理状态
}
```

### 16.13.3 多指针支持

```csharp
// 每个指针（鼠标/触摸点）有独立的 PointerId
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    var pointerId = e.Pointer.Id;
    // 可以同时跟踪多个触摸点
    _activePointers[pointerId] = e.GetPosition(this);
}
```

## 16.14 Cross References

- [第 10 章 动画与过渡效果](10-animation-transitions.md) -- 指针交互触发的动画
- [第 13 章 拖拽交互实现](13-drag-drop.md) -- 完整的拖拽排序实现
- [第 17 章 对话框与弹出层](17-dialogs-popups.md) -- 右键菜单和弹出层
- [第 18 章 命令系统](18-commands.md) -- 事件与命令的结合模式
- [第 7 章 样式与主题系统](07-styling-theming.md) -- `:pointerover` 和 `:focus` 伪类
- [第 20 章 跨平台适配](20-cross-platform.md) -- macOS Command 键适配

## 16.15 Common Pitfalls

1. **忘记释放指针捕获**：如果 `PointerPressed` 中调用了 `Capture(this)` 但没有在 `PointerReleased` 中调用 `Capture(null)`，其他控件将永远无法接收指针事件。始终在 `PointerReleased` 和 `PointerCaptureLost` 中释放捕获。

2. **忘记标记 Handled**：如果不标记 `e.Handled = true`，事件会继续冒泡到父控件，可能导致重复处理或意外行为。

3. **在隧道事件中修改数据**：隧道事件在冒泡事件之前触发，如果在隧道中修改了共享数据，冒泡阶段看到的可能是修改后的值。

4. **忽略拖拽阈值**：没有设置最小移动距离阈值会导致轻微的手抖被误判为拖拽，建议设置 4-8 像素的阈值。

5. **在 PointerMoved 中做耗时操作**：`PointerMoved` 在鼠标移动时高频触发（通常 60-120 次/秒），在其中做耗时操作会导致卡顿。应使用节流或异步处理。

6. **混淆 Tapped 和 PointerPressed**：`Tapped` 在按下+释放后触发，`PointerPressed` 在按下瞬间触发。对于需要在按下时就开始的操作（如拖拽），必须使用 `PointerPressed`。

7. **不处理键盘修饰键的平台差异**：macOS 使用 Command 键作为主修饰键，Windows/Linux 使用 Ctrl 键。硬编码 `KeyModifiers.Control` 会导致 macOS 上快捷键不工作。

8. **焦点事件不会冒泡**：`GotFocus` 和 `LostFocus` 是直接事件（Direct），不会冒泡。如果需要在父容器上监听焦点变化，需要使用 `AddHandler` 并设置 `handledEventsToo: true`。

9. **TextInput 与 KeyDown 的区别**：`KeyDown` 按键时触发，`TextInput` 在实际字符产生时触发。组合键（如 Ctrl+C）不会触发 `TextInput`，但会触发 `KeyDown`。

10. **拖放事件需要 AllowDrop**：在 AXAML 中必须设置 `DragDrop.AllowDrop="True"` 才能接收拖放事件，否则 `DragEnter` / `DragOver` / `Drop` 不会触发。

## 16.16 Try It Yourself

1. **基础练习**：创建一个 Border，当鼠标悬停时改变背景色，按下时加深，释放时恢复悬停色。使用 `PointerEntered`、`PointerExited`、`PointerPressed`、`PointerReleased` 四个事件。

2. **键盘练习**：创建一个简单的文本框，实现以下快捷键：Ctrl+Enter 提交、Escape 清空、Ctrl+A 全选。注意处理 macOS 的 Command 键。

3. **拖拽练习**：创建一个可拖拽的方块，使用 `PointerPressed` / `PointerMoved` / `PointerReleased` 和指针捕获实现。确保设置拖拽阈值（4px）。

4. **右键菜单练习**：为一个列表项添加右键菜单，包含"编辑"和"删除"两个选项。使用 `ContextRequested` 事件。

5. **拖放练习**：实现两个面板之间的拖放，支持 `DragDrop.DoDragDrop` 发起和 `DragDrop.AllowDrop` 接收。

6. **手势练习**：实现一个简单的画布，使用 `PointerMoved` 绘制自由线条，使用 `PointerWheelChanged` 实现缩放。

7. **焦点练习**：创建一个对话框，打开时自动聚焦到第一个输入框，Tab 键只在对话框内的控件间循环。

8. **综合练习**：在 CodexSwitch 的 `ProvidersPage` 中，研究拖拽排序的完整实现，然后为其添加双击编辑功能（`DoubleTapped` 事件触发编辑命令）。
