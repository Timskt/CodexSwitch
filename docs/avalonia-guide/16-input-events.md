# 16. 输入处理与事件系统

## 16.1 路由事件

Avalonia 的事件系统支持三种路由策略：

| 策略 | 说明 | 示例 |
|------|------|------|
| `Direct` | 只在目标控件上触发 | `GotFocus` |
| `Tunnel` | 从根向下传播 | `PreviewPointerPressed` |
| `Bubble` | 从目标向上传播 | `PointerPressed` |

```csharp
// 冒泡事件：从子元素向上传播
AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);

// 隧道事件：从父元素向下传播
AddHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);

// 同时处理已标记为 handled 的事件
AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
```

### 事件路由的工作原理

```
Window (Tunnel: PreviewPointerPressed)
  └── Grid (Tunnel: PreviewPointerPressed)
      └── Button (Direct: PointerPressed, 标记 Handled=true)
          └── ContentPresenter
              └── TextBlock

事件传播顺序：
1. Window.PreviewPointerPressed (隧道)
2. Grid.PreviewPointerPressed (隧道)
3. Button.PointerPressed (直接，标记为已处理)
4. Grid.PointerPressed (冒泡，跳过因为已处理)
5. Window.PointerPressed (冒泡，跳过因为已处理)

如果使用 handledEventsToo: true：
4. Grid.PointerPressed (冒泡，仍然触发)
5. Window.PointerPressed (冒泡，仍然触发)
```

## 16.2 指针事件

```csharp
// 指针按下
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    var point = e.GetCurrentPoint(this);

    if (point.Properties.IsLeftButtonPressed) { }
    if (point.Properties.IsRightButtonPressed) { }
    if (point.Properties.IsMiddleButtonPressed) { }

    if (e.ClickCount >= 2) { /* 双击 */ }

    // 获取相对位置
    var position = e.GetPosition(this);

    // 捕获指针
    e.Pointer.Capture(this);
    e.Handled = true;
}

// 指针移动
private void OnPointerMoved(object? sender, PointerEventArgs e)
{
    var position = e.GetPosition(this);
}

// 指针释放
private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
{
    e.Pointer.Capture(null);  // 释放捕获
}

// 指针进入/离开
private void OnPointerEntered(object? sender, PointerEventArgs e) { }
private void OnPointerExited(object? sender, PointerEventArgs e) { }

// 指针捕获丢失
private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) { }
```

### PointerPressed vs Tapped

| 事件 | 触发时机 | 用途 |
|------|---------|------|
| `PointerPressed` | 按下时 | 拖拽、自定义交互 |
| `PointerReleased` | 释放时 | 点击完成 |
| `Tapped` | 完整点击（按下+释放） | 通用点击 |
| `DoubleTapped` | 双击 | 双击操作 |
| `RightTapped` | 右键点击 | 上下文菜单 |

## 16.3 键盘事件

```csharp
// 按键按下
private void OnKeyDown(object? sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter) { }
    if (e.Key == Key.Escape) { }

    // 组合键
    if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.S) { }

    e.Handled = true;
}

// 按键释放
private void OnKeyUp(object? sender, KeyEventArgs e) { }
```

### 常用键值

```csharp
Key.Enter      // 回车
Key.Escape     // ESC
Key.Tab        // Tab
Key.Space      // 空格
Key.Back       // 退格
Key.Delete     // 删除
Key.Up         // 上箭头
Key.Down       // 下箭头
Key.Left       // 左箭头
Key.Right      // 右箭头
Key.A - Key.Z  // 字母键
Key.D0 - Key.D9 // 数字键
Key.F1 - Key.F12 // 功能键
```

## 16.4 焦点管理

```csharp
// 获取焦点
myControl.Focus();

// 焦点事件
myControl.GotFocus += (s, e) => { };
myControl.LostFocus += (s, e) => { };

// 焦点相关属性
myControl.Focusable = true;      // 是否可聚焦
myControl.TabIndex = 0;          // Tab 键顺序
myControl.IsTabStop = true;      // 是否在 Tab 序列中
```

### 焦点导航

```csharp
// 程序化焦点导航
var next = KeyboardNavigationHandler.GetNext(myControl, NavigationDirection.Next);
next?.Focus();
```

## 16.5 拖放 (Drag and Drop)

```csharp
// 开始拖放
private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    var data = new DataObject();
    data.Set("text/plain", "Hello");

    var result = await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
}

// 接收拖放
private void OnDragEnter(object? sender, DragEventArgs e)
{
    e.DragEffects = e.Data.Contains("text/plain")
        ? DragDropEffects.Copy
        : DragDropEffects.None;
}

private void OnDragOver(object? sender, DragEventArgs e)
{
    e.DragEffects = DragDropEffects.Copy;
}

private void OnDrop(object? sender, DragEventArgs e)
{
    var text = e.Data.Get("text/plain");
    // 处理拖放数据
}
```

## 16.6 右键菜单

```csharp
// 通过事件打开
AddHandler(InputElement.ContextRequestedEvent, OnContextRequested, RoutingStrategies.Bubble);

private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
{
    var menu = new MenuFlyout();
    menu.Items.Add(new MenuItem { Header = "Edit" });
    menu.Items.Add(new MenuItem { Header = "Delete" });
    menu.ShowAt(this, true);
    e.Handled = true;
}
```

CodexSwitch 的 `CsProviderContextMenu` 展示了更复杂的上下文菜单实现：

```csharp
// CsProviderContextMenu.cs
public static void OpenFor(Control target, MainWindowViewModel viewModel, ProviderListItem item)
{
    var menu = new ContextMenu();

    // 添加菜单项
    var editItem = new MenuItem { Header = I18nService.Current.Translate("providers.edit") };
    editItem.Click += (_, _) => viewModel.EditProvider(item.Id);
    menu.Items.Add(editItem);

    var deleteItem = new MenuItem { Header = I18nService.Current.Translate("providers.delete") };
    deleteItem.Click += (_, _) => viewModel.DeleteProvider(item.Id);
    menu.Items.Add(deleteItem);

    // 显示菜单
    menu.Open(target);
}
```

---

## Deep Dive：指针捕获机制

指针捕获 (Pointer Capture) 是拖拽和自定义交互的关键：

```csharp
// 捕获指针
e.Pointer.Capture(handle);

// 之后所有的 Pointer 事件都会发送到 handle，即使鼠标移出了 handle 的边界
// 这对于拖拽操作至关重要

// 释放捕获
e.Pointer.Capture(null);

// 捕获丢失（如窗口失焦）
private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
{
    // 清理拖拽状态
}
```

### 为什么需要捕获？

在拖拽操作中，用户可能会将鼠标移出被拖拽元素的边界。如果没有捕获，`PointerMoved` 事件会停止触发，拖拽就会中断。捕获确保事件持续发送到正确的控件。

## Cross References

- [第 13 章 拖拽交互实现](13-drag-drop.md) — 完整的拖拽排序实现
- [第 17 章 对话框与弹出层](17-dialogs-popups.md) — 右键菜单和弹出层
- [第 10 章 动画与过渡效果](10-animation-transitions.md) — 指针交互的动画

## Common Pitfalls

1. **忘记释放指针捕获**: 如果捕获后不释放，其他控件将无法接收指针事件
2. **忘记标记 Handled**: 如果不标记 `e.Handled = true`，事件会继续冒泡
3. **在 Tunnel 事件中修改数据**: 隧道事件在冒泡事件之前触发，修改可能被覆盖

## Try It Yourself

1. 在 CodexSwitch 的 `ProvidersPage.axaml.cs` 中，找到拖拽手柄的 Pointer 事件处理
2. 尝试修改拖拽阈值（当前是 4px），观察体验变化
3. 添加一个 `DoubleTapped` 事件处理，实现双击编辑功能
