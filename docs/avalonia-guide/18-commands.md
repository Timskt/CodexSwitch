# 18. 命令系统

## 18.1 ICommand 接口

```csharp
public interface ICommand
{
    event EventHandler? CanExecuteChanged;
    bool CanExecute(object? parameter);
    void Execute(object? parameter);
}
```

### 命令模式的优势

命令模式将"用户意图"与"执行逻辑"分离：
- **View** 只知道"用户点击了按钮"
- **ViewModel** 决定"点击后做什么"
- **Model** 执行实际的业务逻辑

## 18.2 CommunityToolkit.Mvvm 命令

### RelayCommand — 同步命令

```csharp
[RelayCommand]
private void Save()
{
    _service.Save(_data);
}

// 生成的代码：
// public IRelayCommand SaveCommand { get; }
```

### 带参数的命令

```csharp
[RelayCommand]
private void DeleteItem(string id)
{
    _service.Delete(id);
}

// 生成的代码：
// public IRelayCommand<string> DeleteCommand { get; }
```

### 异步命令

```csharp
[RelayCommand]
private async Task RefreshAsync(CancellationToken token)
{
    await _service.RefreshAsync(token);
}

// 生成的代码：
// public IAsyncRelayCommand RefreshCommand { get; }
// 自动支持：
// - IsRunning 属性（指示是否正在执行）
// - CancelCommand（取消正在执行的命令）
// - ExecutionTask 属性（获取执行任务）
```

### 带 CanExecute 的命令

```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { }

private bool CanSave => !string.IsNullOrEmpty(_name);

// 当 CanSave 变化时，需要通知命令
partial void OnNameChanged(string value)
{
    SaveCommand.NotifyCanExecuteChanged();
}
```

## 18.3 在 AXAML 中绑定命令

```xml
<!-- 绑定到命令 -->
<Button Command="{Binding SaveCommand}"/>

<!-- 带参数 -->
<Button Command="{Binding DeleteCommand}"
        CommandParameter="{Binding Id}"/>

<!-- 命令参数为自身 -->
<Button Command="{Binding SelectCommand}"
        CommandParameter="{Binding}"/>

<!-- 绑定到父 ViewModel 的命令 -->
<Button Command="{Binding #Root.DataContext.DeleteCommand}"
        CommandParameter="{Binding Id}"/>
```

## 18.4 命令与 CanExecute

当 `CanExecute` 返回 false 时，绑定的按钮会自动禁用：

```xml
<Button Command="{Binding SaveCommand}" Content="Save"/>
<!-- 当 CanSave 为 false 时，按钮自动变为 Disabled 状态 -->
```

### CanExecute 的缓存行为

```csharp
// CommunityToolkit.Mvvm 的 RelayCommand 会缓存 CanExecute 结果
// 需要手动调用 NotifyCanExecuteChanged() 来刷新

[RelayCommand(CanExecute = nameof(CanSubmit))]
private async Task SubmitAsync()
{
    // 提交逻辑
}

private bool CanSubmit => !string.IsNullOrEmpty(_email) && _isValid;

partial void OnEmailChanged(string value)
{
    // 当 Email 变化时，通知命令重新检查 CanExecute
    SubmitCommand.NotifyCanExecuteChanged();
}
```

## 18.5 AsyncRelayCommand 的额外属性

```xml
<!-- 显示加载状态 -->
<ProgressBar IsIndeterminate="{Binding RefreshCommand.IsRunning}"
             IsVisible="{Binding RefreshCommand.IsRunning}"/>

<!-- 取消按钮 -->
<Button Content="Cancel"
        Command="{Binding RefreshCommand.CancelCommand}"
        IsVisible="{Binding RefreshCommand.IsRunning}"/>

<!-- 禁用按钮（正在执行时） -->
<Button Content="Refresh"
        Command="{Binding RefreshCommand}"
        IsEnabled="{Binding !RefreshCommand.IsRunning}"/>
```

## 18.6 命令与事件的结合

CodexSwitch 展示了命令与事件的结合模式：

```csharp
// 在 Code-Behind 中处理 UI 特定的逻辑
private void ProviderDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    // UI 逻辑：拖拽检测
    if (sender is not Control handle ||
        handle.DataContext is not ProviderListItem item)
        return;

    _providerDrag = new ProviderDragState(item, row, e.GetPosition(this));
    e.Pointer.Capture(handle);
    e.Handled = true;
}

// 在 ViewModel 中处理业务逻辑
[RelayCommand]
private void MoveProvider(string providerId)
{
    // 业务逻辑：排序
    _providerService.MoveToIndex(providerId, _targetIndex);
}
```

---

## Deep Dive：命令的内部实现

### RelayCommand 的实现

```csharp
// 简化的 RelayCommand 实现
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

### AsyncRelayCommand 的实现

```csharp
public class AsyncRelayCommand : IAsyncRelayCommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public IRelayCommand CancelCommand { get; }

    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        CancelCommand = new RelayCommand(() => _cts?.Cancel());
    }

    public async Task ExecuteAsync(CancellationToken token)
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            await _execute(_cts.Token);
        }
        finally
        {
            _isRunning = false;
            _cts = null;
        }
    }
}
```

## Cross References

- [第 6 章 MVVM 模式实战](06-mvvm-pattern.md) — MVVM 中的命令模式
- [第 5 章 数据绑定](05-data-binding.md) — 命令绑定语法
- [第 16 章 输入处理与事件系统](16-input-events.md) — 事件与命令的关系

## Common Pitfalls

1. **忘记调用 NotifyCanExecuteChanged**: CanExecute 条件变化后不通知，按钮状态不更新
2. **异步命令不处理异常**: 未捕获的异常会导致命令卡在 IsRunning 状态
3. **命令参数类型不匹配**: 编译绑定会检查类型，运行时绑定不会

## Try It Yourself

1. 在 CodexSwitch 中找到使用 `[RelayCommand]` 的地方，查看生成的代码
2. 创建一个带取消功能的异步命令
3. 实现一个命令，其 CanExecute 依赖于多个属性
