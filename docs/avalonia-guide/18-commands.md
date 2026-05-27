# 18. 命令系统

> **写给零基础的你**：命令（Command）就是"告诉程序去做某件事"的指令。比如点击"保存"按钮——按钮是界面，保存是逻辑。命令把这两者连接起来，但又让它们互不依赖。就像遥控器和电视机：你按遥控器的按钮（命令），电视就换台（执行逻辑），但遥控器不需要知道电视内部怎么工作的。

## 18.1 概述

命令系统是 MVVM 架构的核心支柱之一。它将"用户意图"（如点击按钮）与"执行逻辑"（如保存数据）分离，使得 UI 层无需知道业务逻辑的实现细节，业务逻辑也无需依赖 UI 框架。

在 CodexSwitch 中，命令系统无处不在：
- **导航命令**：`ShowHomeCommand`、`ShowProvidersCommand` 切换页面
- **操作命令**：`AddProviderCommand`、`SaveProviderCommand` 管理 Provider
- **异步命令**：`RefreshUsageCommand` 异步查询使用数据，支持取消
- **带条件的命令**：`SaveCommand` 根据表单验证状态自动启用/禁用

本章将全面讲解 Avalonia 中命令系统的工作原理、CommunityToolkit.Mvvm 的命令实现，以及在各种场景下的最佳实践。

## 18.2 ICommand 接口

### 18.2.1 接口定义

> **小白提示：什么是接口（Interface）？**  接口就像"合同"。它规定了"你必须实现哪些方法"，但不告诉你怎么实现。就像餐厅招聘厨师，合同上写"会做中餐、会做西餐"，但不规定你怎么做。ICommand 接口规定了"你必须有 CanExecute 和 Execute 两个方法"。

```csharp
public interface ICommand
{
    // 当 CanExecute 状态可能变化时触发（通知按钮更新状态）
    event EventHandler? CanExecuteChanged;

    // 命令是否可以执行（返回 true 表示可以执行，按钮高亮；false 表示不能执行，按钮变灰）
    bool CanExecute(object? parameter);

    // 执行命令（用户点击按钮时实际执行的操作）
    void Execute(object? parameter);
}
```

### 18.2.2 命令的工作流程

```
用户点击按钮
    ↓
Button 检查 Command.CanExecute(parameter)（我能执行吗？）
    ↓
├── true → 调用 Command.Execute(parameter)（执行操作）
└── false → 按钮保持禁用状态（变灰，点不了）
```

> **小白提示**：CanExecute 就像"门禁系统"。你走到门口（点击按钮），门禁先检查你有没有权限（CanExecute），有权限才开门（执行 Execute），没权限就不开门（按钮禁用）。

### 18.2.3 手动实现 ICommand

```csharp
public class SimpleCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public SimpleCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

// 使用
var saveCommand = new SimpleCommand(
    execute: _ => SaveData(),
    canExecute: _ => !string.IsNullOrEmpty(Name)
);
```

### 18.2.4 CanExecute 的缓存机制

WPF/Avalonia 的命令绑定框架会对 `CanExecute` 的结果进行缓存：

1. 首次绑定时调用 `CanExecute` 获取结果
2. 当 `CanExecuteChanged` 触发时重新调用 `CanExecute`
3. 按钮的 `IsEnabled` 属性根据 `CanExecute` 的结果自动设置

这意味着：如果你改变了 CanExecute 的条件但没有触发 `CanExecuteChanged`，按钮状态不会更新。

## 18.3 CommunityToolkit.Mvvm 命令

### 18.3.1 RelayCommand -- 同步命令

`[RelayCommand]` 特性是 CommunityToolkit.Mvvm 提供的源代码生成器，它会自动为标记的方法生成对应的命令属性。

```csharp
[RelayCommand]
private void Save()
{
    _service.Save(_data);
}

// 源代码生成器自动生成以下代码：
// [GeneratedCode]
// private IRelayCommand? _saveCommand;
// public IRelayCommand SaveCommand => _saveCommand ??= new RelayCommand(Save);
```

### 18.3.2 带参数的命令

```csharp
// 方式一：方法参数
[RelayCommand]
private void DeleteItem(string id)
{
    _service.Delete(id);
}

// 生成：public IRelayCommand<string> DeleteItemCommand { get; }

// 方式二：接受 object? 参数
[RelayCommand]
private void ProcessItem(object? parameter)
{
    if (parameter is Item item)
        Process(item);
}

// 生成：public IRelayCommand ProcessItemCommand { get; }
```

在 XAML 中传递参数：

```xml
<!-- 方式一：绑定到 DataContext -->
<Button Command="{Binding DeleteItemCommand}"
        CommandParameter="{Binding Id}"/>

<!-- 方式二：传递自身 -->
<Button Command="{Binding SelectCommand}"
        CommandParameter="{Binding}"/>

<!-- 方式三：传递静态值 -->
<Button Command="{Binding NavigateCommand}"
        CommandParameter="settings"/>

<!-- 方式四：传递到父 ViewModel 的命令 -->
<Button Command="{Binding #Root.DataContext.DeleteItemCommand}"
        CommandParameter="{Binding Id}"/>
```

### 18.3.3 异步命令

```csharp
[RelayCommand]
private async Task RefreshAsync(CancellationToken token)
{
    await _service.RefreshAsync(token);
}

// 生成：public IAsyncRelayCommand RefreshCommand { get; }
// IAsyncRelayCommand 自动提供：
// - IsRunning 属性（bool，指示是否正在执行）
// - ExecutionTask 属性（Task，获取当前执行任务）
// - CancelCommand（IRelayCommand，取消正在执行的命令）
```

### 18.3.4 带 CanExecute 的命令

```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save()
{
    _service.Save(_data);
}

private bool CanSave => !string.IsNullOrEmpty(_name) && _isFormValid;

// 当条件变化时，必须通知命令
partial void OnNameChanged(string value)
{
    SaveCommand.NotifyCanExecuteChanged();
}

partial void OnIsFormValidChanged(bool value)
{
    SaveCommand.NotifyCanExecuteChanged();
}
```

### 18.3.5 异步命令 + CanExecute

```csharp
[RelayCommand(CanExecute = nameof(CanSubmit))]
private async Task SubmitAsync(CancellationToken token)
{
    IsSubmitting = true;
    try
    {
        await _apiClient.SubmitAsync(_formData, token);
        ShowSuccess("Submit succeeded");
    }
    catch (OperationCanceledException)
    {
        ShowInfo("Submit cancelled");
    }
    catch (Exception ex)
    {
        ShowError($"Submit failed: {ex.Message}");
    }
    finally
    {
        IsSubmitting = false;
    }
}

private bool CanSubmit => !string.IsNullOrEmpty(_email)
    && _email.Contains('@')
    && !_isSubmitting;

// 注意：IsRunning 期间 CanExecute 会自动返回 false
// 但如果你有自己的 _isSubmitting 字段，需要手动通知
```

### 18.3.6 命令的 AllowConcurrentExecutions

```csharp
// 默认情况下，异步命令不允许并发执行
// 如果需要并发，设置 AllowConcurrentExecutions
[RelayCommand(AllowConcurrentExecutions = true)]
private async Task DownloadFileAsync(string url, CancellationToken token)
{
    await _httpClient.DownloadAsync(url, token);
}
```

## 18.4 AsyncRelayCommand 详解

### 18.4.1 异步命令的生命周期

```
创建 → 空闲 (IsRunning=false)
  ↓ ExecuteAsync()
执行中 (IsRunning=true)
  ↓ 完成/取消/异常
空闲 (IsRunning=false)
```

### 18.4.2 取消支持

```csharp
[RelayCommand]
private async Task LoadDataAsync(CancellationToken token)
{
    var data = await _httpClient.GetAsync("/api/data", token);
    ProcessData(data);
}
```

```xml
<!-- 进度指示 -->
<ProgressBar IsIndeterminate="{Binding LoadDataCommand.IsRunning}"
             IsVisible="{Binding LoadDataCommand.IsRunning}"/>

<!-- 取消按钮 -->
<Button Content="Cancel"
        Command="{Binding LoadDataCommand.CancelCommand}"
        IsVisible="{Binding LoadDataCommand.IsRunning}"/>

<!-- 加载按钮（执行中禁用） -->
<Button Content="Load Data"
        Command="{Binding LoadDataCommand}"/>
```

### 18.4.3 错误处理

```csharp
[RelayCommand]
private async Task FetchDataAsync(CancellationToken token)
{
    try
    {
        var result = await _api.FetchAsync(token);
        Data = result;
    }
    catch (OperationCanceledException)
    {
        StatusMessage = "Fetch cancelled";
    }
    catch (HttpRequestException ex)
    {
        StatusMessage = $"Network error: {ex.Message}";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Unexpected error: {ex.Message}";
        _logger.LogError(ex, "FetchData failed");
    }
}
```

### 18.4.4 等待命令完成

```csharp
// 在某些场景下需要等待命令执行完成
await SaveCommand.ExecuteAsync(null);

// 或者通过 ExecutionTask
if (SaveCommand.ExecutionTask is Task task)
{
    await task;
}

// 在 OnNavigatedFrom 中等待
public async Task OnNavigatedFromAsync()
{
    if (SaveCommand.IsRunning)
    {
        await SaveCommand.ExecutionTask!;
    }
}
```

### 18.4.5 进度报告

```csharp
[RelayCommand]
private async Task ImportDataAsync(CancellationToken token)
{
    var progress = new Progress<int>(percent =>
    {
        ImportProgress = percent;
        StatusMessage = $"Importing... {percent}%";
    });

    await Task.Run(async () =>
    {
        var items = await LoadItemsAsync(token);
        for (int i = 0; i < items.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            await ProcessItemAsync(items[i], token);
            ((IProgress<int>)progress).Report((i + 1) * 100 / items.Count);
        }
    }, token);
}
```

```xml
<!-- 进度 UI -->
<StackPanel IsVisible="{Binding ImportDataCommand.IsRunning}">
    <ProgressBar Value="{Binding ImportProgress}" Maximum="100"/>
    <TextBlock Text="{Binding StatusMessage}"/>
    <Button Content="Cancel"
            Command="{Binding ImportDataCommand.CancelCommand}"/>
</StackPanel>
```

## 18.5 命令在 UI 中的使用

### 18.5.1 Button 命令绑定

```xml
<!-- 基本绑定 -->
<Button Command="{Binding SaveCommand}" Content="Save"/>

<!-- 带参数 -->
<Button Command="{Binding DeleteCommand}"
        CommandParameter="{Binding Id}"/>

<!-- IsEnabled 由 CanExecute 自动控制 -->
<Button Command="{Binding SaveCommand}"/>
<!-- 等效于 -->
<Button Command="{Binding SaveCommand}"
        IsEnabled="{Binding SaveCommand.CanExecute(null)}"/>
```

### 18.5.2 MenuItem 命令绑定

```xml
<MenuItem Header="Save" Command="{Binding SaveCommand}"
          InputGesture="Ctrl+S"/>

<MenuItem Header="Delete" Command="{Binding DeleteCommand}"
          CommandParameter="{Binding SelectedItem.Id}">
    <MenuItem.Icon>
        <PathIcon Data="{StaticResource DeleteIcon}"/>
    </MenuItem.Icon>
</MenuItem>

<!-- 子菜单中的命令 -->
<MenuItem Header="Sort By">
    <MenuItem Header="Name" Command="{Binding SortCommand}"
              CommandParameter="Name"/>
    <MenuItem Header="Date" Command="{Binding SortCommand}"
              CommandParameter="Date"/>
    <MenuItem Header="Size" Command="{Binding SortCommand}"
              CommandParameter="Size"/>
</MenuItem>
```

### 18.5.3 ContextMenu 命令绑定

```xml
<Border>
    <Border.ContextMenu>
        <ContextMenu>
            <MenuItem Header="Edit" Command="{Binding EditCommand}"
                      CommandParameter="{Binding}"/>
            <MenuItem Header="Duplicate" Command="{Binding DuplicateCommand}"
                      CommandParameter="{Binding}"/>
            <Separator/>
            <MenuItem Header="Delete" Command="{Binding DeleteCommand}"
                      CommandParameter="{Binding}"/>
        </ContextMenu>
    </Border.ContextMenu>
</Border>
```

### 18.5.4 KeyBinding 与命令

```xml
<Window>
    <Window.KeyBindings>
        <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}"/>
        <KeyBinding Gesture="Ctrl+N" Command="{Binding NewCommand}"/>
        <KeyBinding Gesture="Delete" Command="{Binding DeleteCommand}"/>
        <KeyBinding Gesture="F5" Command="{Binding RefreshCommand}"/>
        <KeyBinding Gesture="Escape" Command="{Binding CancelCommand}"/>
    </Window.KeyBindings>
</Window>
```

### 18.5.5 ListBox/ItemsControl 中的命令

```xml
<ItemsControl ItemsSource="{Binding Items}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border>
                <!-- 列表项中的命令绑定到父 ViewModel -->
                <StackPanel Orientation="Horizontal">
                    <Button Content="Edit"
                            Command="{Binding #Root.DataContext.EditCommand}"
                            CommandParameter="{Binding Id}"/>
                    <Button Content="Delete"
                            Command="{Binding #Root.DataContext.DeleteCommand}"
                            CommandParameter="{Binding Id}"/>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 18.5.6 Flyout 中的命令

```xml
<Button Content="Actions">
    <Button.Flyout>
        <MenuFlyout>
            <MenuItem Header="Edit"
                      Command="{Binding EditCommand}"
                      CommandParameter="{Binding SelectedItem}"/>
            <MenuItem Header="Delete"
                      Command="{Binding DeleteCommand}"
                      CommandParameter="{Binding SelectedItem.Id}"/>
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

## 18.6 命令参数传递模式

### 18.6.1 CommandParameter 绑定

```xml
<!-- 绑定到当前数据项 -->
<Button Command="{Binding SelectCommand}"
        CommandParameter="{Binding}"/>

<!-- 绑定到属性 -->
<Button Command="{Binding NavigateCommand}"
        CommandParameter="{Binding PageName}"/>

<!-- 绑定到父元素的 DataContext -->
<Button Command="{Binding #ListView.DataContext.SelectCommand}"
        CommandParameter="{Binding}"/>

<!-- 静态字符串参数 -->
<Button Command="{Binding FilterCommand}"
        CommandParameter="all"/>
```

### 18.6.2 事件参数转换为命令参数

有时需要将事件参数（如鼠标位置）传递给命令。这通常通过 Code-Behind 桥接：

```csharp
// Code-Behind
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (sender is Control control && control.DataContext is ItemViewModel item)
    {
        var position = e.GetPosition(this);
        ViewModel.DragStartCommand.Execute(new DragStartInfo(item, position));
    }
}
```

### 18.6.3 复合参数

```csharp
// 当需要传递多个参数时，使用记录类型
public record DeleteConfirmation(string ItemId, bool Permanent);

[RelayCommand]
private void ConfirmDelete(DeleteConfirmation confirmation)
{
    if (confirmation.Permanent)
        _service.PermanentDelete(confirmation.ItemId);
    else
        _service.SoftDelete(confirmation.ItemId);
}
```

## 18.7 命令与 MVVM 模式

### 18.7.1 CodexSwitch 中的命令模式

在 CodexSwitch 的 `MainWindowViewModel` 中，命令被大量使用：

```csharp
// 导航命令
[RelayCommand]
private void ShowHome() => CurrentPage = "Home";

[RelayCommand]
private void ShowProviders() => CurrentPage = "Providers";

[RelayCommand]
private void ShowUsage() => CurrentPage = "Usage";

// 带状态管理的命令
[RelayCommand]
private void AddProvider()
{
    _editingProviderId = null;
    ProviderDialogTitle = "Add Provider";
    ResetProviderFields();
    IsProviderDialogOpen = true;
}

[RelayCommand]
private void EditProvider(string providerId)
{
    var provider = _config.Providers.FirstOrDefault(p => p.Id == providerId);
    if (provider == null) return;

    _editingProviderId = providerId;
    ProviderDialogTitle = "Edit Provider";
    LoadProviderFields(provider);
    IsProviderDialogOpen = true;
}

// 异步命令
[RelayCommand]
private async Task RefreshUsageAsync(CancellationToken token)
{
    IsUsageRefreshing = true;
    try
    {
        var data = await _usageReader.ReadAsync(token);
        UpdateUsageDashboard(data);
    }
    finally
    {
        IsUsageRefreshing = false;
    }
}
```

### 18.7.2 命令的组织原则

```csharp
public partial class MainWindowViewModel : ViewModelBase
{
    // 1. 按功能分组
    // === Navigation Commands ===
    [RelayCommand] private void ShowHome() { }
    [RelayCommand] private void ShowProviders() { }
    [RelayCommand] private void ShowSettings() { }

    // === Provider CRUD Commands ===
    [RelayCommand] private void AddProvider() { }
    [RelayCommand] private void EditProvider(string id) { }
    [RelayCommand(CanExecute = nameof(CanSaveProvider))]
    private void SaveProvider() { }
    [RelayCommand] private void DeleteProvider(string id) { }

    // === Async Operations ===
    [RelayCommand] private async Task RefreshDataAsync(CancellationToken t) { }
    [RelayCommand] private async Task ExportDataAsync(CancellationToken t) { }

    // === Dialog Commands ===
    [RelayCommand] private void CancelDialog() { }
    [RelayCommand] private void ConfirmDelete() { }
}
```

## 18.8 命令的单元测试

### 18.8.1 测试 RelayCommand

```csharp
[Fact]
public void SaveCommand_SavesData()
{
    // Arrange
    var mockService = new Mock<IDataService>();
    var vm = new MainViewModel(mockService.Object);
    vm.Name = "Test";

    // Act
    vm.SaveCommand.Execute(null);

    // Assert
    mockService.Verify(s => s.Save(It.IsAny<Data>()), Times.Once);
}
```

### 18.8.2 测试 CanExecute

```csharp
[Fact]
public void SaveCommand_WhenNameEmpty_CannotExecute()
{
    // Arrange
    var vm = new MainViewModel();
    vm.Name = "";

    // Act & Assert
    Assert.False(vm.SaveCommand.CanExecute(null));
}

[Fact]
public void SaveCommand_WhenNameNotEmpty_CanExecute()
{
    // Arrange
    var vm = new MainViewModel();
    vm.Name = "Test";

    // Act & Assert
    Assert.True(vm.SaveCommand.CanExecute(null));
}

[Fact]
public void SaveCommand_CanExecuteChanges_WhenNameChanges()
{
    // Arrange
    var vm = new MainViewModel();
    vm.Name = "";
    var canExecuteChanged = false;
    vm.SaveCommand.CanExecuteChanged += (_, _) => canExecuteChanged = true;

    // Act
    vm.Name = "Test"; // 触发 OnNameChanged -> NotifyCanExecuteChanged

    // Assert
    Assert.True(canExecuteChanged);
}
```

### 18.8.3 测试异步命令

```csharp
[Fact]
public async Task RefreshCommand_LoadsData()
{
    // Arrange
    var mockService = new Mock<IDataService>();
    mockService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new[] { "item1", "item2" });

    var vm = new MainViewModel(mockService.Object);

    // Act
    await vm.RefreshCommand.ExecuteAsync(null);

    // Assert
    Assert.Equal(2, vm.Items.Count);
    Assert.False(vm.RefreshCommand.IsRunning);
}

[Fact]
public async Task RefreshCommand_SetsIsRunning()
{
    // Arrange
    var tcs = new TaskCompletionSource<IEnumerable<string>>();
    var mockService = new Mock<IDataService>();
    mockService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
        .Returns(tcs.Task);

    var vm = new MainViewModel(mockService.Object);

    // Act
    var task = vm.RefreshCommand.ExecuteAsync(null);

    // Assert - 执行中
    Assert.True(vm.RefreshCommand.IsRunning);

    // Complete
    tcs.SetResult(Array.Empty<string>());
    await task;

    // Assert - 完成后
    Assert.False(vm.RefreshCommand.IsRunning);
}

[Fact]
public async Task RefreshCommand_CanBeCancelled()
{
    // Arrange
    var cts = new CancellationTokenSource();
    var mockService = new Mock<IDataService>();
    mockService.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
        .Returns<CancellationToken>(async ct =>
        {
            await Task.Delay(5000, ct);
            return Array.Empty<string>();
        });

    var vm = new MainViewModel(mockService.Object);

    // Act
    var task = vm.RefreshCommand.ExecuteAsync(null);
    vm.RefreshCommand.CancelCommand.Execute(null);

    // Assert
    await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    Assert.False(vm.RefreshCommand.IsRunning);
}
```

### 18.8.4 测试带参数的命令

```csharp
[Fact]
public void DeleteCommand_DeletesCorrectItem()
{
    // Arrange
    var mockService = new Mock<IDataService>();
    var vm = new MainViewModel(mockService.Object);
    var itemId = "item-123";

    // Act
    vm.DeleteCommand.Execute(itemId);

    // Assert
    mockService.Verify(s => s.Delete("item-123"), Times.Once);
}

[Fact]
public void NavigateCommand_ChangesCurrentPage()
{
    // Arrange
    var vm = new MainViewModel();

    // Act
    vm.NavigateCommand.Execute("settings");

    // Assert
    Assert.Equal("settings", vm.CurrentPage);
}
```

## 18.9 命令进阶模式

### 18.9.1 命令组合

```csharp
// 一个命令触发多个操作
[RelayCommand]
private async Task SaveAndCloseAsync(CancellationToken token)
{
    await SaveAsync(token);
    CloseDialog();
}

// 条件组合
[RelayCommand(CanExecute = nameof(CanSaveAndClose))]
private async Task SaveAndCloseAsync(CancellationToken token)
{
    await SaveAsync(token);
    CloseDialog();
}

private bool CanSaveAndClose => CanSave && !IsSaving;
```

### 18.9.2 命令队列

```csharp
// 确保命令按顺序执行
private readonly SemaphoreSlim _commandLock = new(1, 1);

[RelayCommand]
private async Task SequentialSaveAsync(CancellationToken token)
{
    await _commandLock.WaitAsync(token);
    try
    {
        await SaveAsync(token);
    }
    finally
    {
        _commandLock.Release();
    }
}
```

### 18.9.3 防抖命令

```csharp
// 搜索命令：用户停止输入 300ms 后才执行
private CancellationTokenSource? _searchCts;

[RelayCommand]
private async Task SearchAsync(string query, CancellationToken token)
{
    _searchCts?.Cancel();
    _searchCts = CancellationTokenSource.CreateLinkedTokenSource(token);

    try
    {
        await Task.Delay(300, _searchCts.Token); // 防抖
        var results = await _searchService.SearchAsync(query, _searchCts.Token);
        SearchResults = results;
    }
    catch (OperationCanceledException) { }
}
```

### 18.9.4 命令重试

```csharp
[RelayCommand]
private async Task ReliableFetchAsync(CancellationToken token)
{
    const int maxRetries = 3;

    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            var data = await _api.FetchAsync(token);
            Data = data;
            return;
        }
        catch (Exception ex) when (attempt < maxRetries - 1)
        {
            _logger.LogWarning(ex, "Fetch attempt {Attempt} failed", attempt + 1);
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), token);
        }
    }
}
```

## 18.10 最佳实践

### 命令命名规范

```csharp
// 同步命令：动词
[RelayCommand] private void Save() { }
[RelayCommand] private void Delete(string id) { }
[RelayCommand] private void Navigate(string page) { }

// 异步命令：动词 + Async
[RelayCommand] private async Task SaveAsync(CancellationToken t) { }
[RelayCommand] private async Task RefreshAsync(CancellationToken t) { }

// CanExecute：Can + 动词
private bool CanSave => !string.IsNullOrEmpty(_name);
private bool CanDelete => _selectedItem is not null;
```

### 命令与事件的分工

| 场景 | 使用命令 | 使用事件 |
|------|---------|---------|
| 按钮点击 | 是 | 否 |
| 菜单项选择 | 是 | 否 |
| 键盘快捷键 | 是（KeyBinding） | 否 |
| 拖拽开始/移动/释放 | 否 | 是（PointerPressed 等） |
| 鼠标悬停效果 | 否 | 是（PointerEntered/Exited） |
| 双击编辑 | 可选 | 是（DoubleTapped） |
| 右键菜单触发 | 否 | 是（ContextRequested） |
| 滚轮缩放 | 否 | 是（PointerWheelChanged） |

### CanExecute 的最佳实践

1. **保持 CanExecute 简单**：复杂的验证逻辑放在单独的方法中
2. **及时通知**：条件变化后立即调用 `NotifyCanExecuteChanged()`
3. **使用 partial void OnXxxChanged**：利用 CommunityToolkit.Mvvm 的属性变更通知
4. **避免在 CanExecute 中做耗时操作**：CanExecute 可能被频繁调用

```csharp
// 好的做法
private bool CanSave => !_isSaving && _isFormValid;

partial void OnIsFormValidChanged(bool value)
{
    SaveCommand.NotifyCanExecuteChanged();
}

// 不好的做法
private bool CanSave => !_isSaving && ValidateAllFields(); // ValidateAllFields 可能很慢
```

## 18.11 Deep Dive：命令的内部实现

### 18.11.1 RelayCommand 实现

```csharp
// 简化的 CommunityToolkit.Mvvm RelayCommand 实现
public class RelayCommand : IRelayCommand
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

### 18.11.2 AsyncRelayCommand 实现

```csharp
// 简化的 AsyncRelayCommand 实现
public class AsyncRelayCommand : IAsyncRelayCommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public Task? ExecutionTask { get; private set; }
    public IRelayCommand CancelCommand { get; }

    public AsyncRelayCommand(Func<CancellationToken, Task> execute,
        Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        CancelCommand = new RelayCommand(() => _cts?.Cancel(),
            () => _isRunning);
    }

    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    public async Task ExecuteAsync(CancellationToken token)
    {
        if (_isRunning) return;

        _isRunning = true;
        _cts = new CancellationTokenSource();
        RaiseCanExecuteChanged();

        try
        {
            ExecutionTask = _execute(_cts.Token);
            await ExecutionTask;
        }
        finally
        {
            _isRunning = false;
            _cts = null;
            ExecutionTask = null;
            RaiseCanExecuteChanged();
        }
    }
}
```

### 18.11.3 Button 如何使用命令

```csharp
// Avalonia Button 内部的命令处理（简化）
public class Button : ContentControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<Button, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<Button, object?>(nameof(CommandParameter));

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Command?.CanExecuteChanged += OnCanExecuteChanged;
        UpdateIsEnabled();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Command?.CanExecuteChanged -= OnCanExecuteChanged;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnClick()
    {
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e)
    {
        UpdateIsEnabled();
    }

    private void UpdateIsEnabled()
    {
        SetCurrentValue(IsEnabledProperty,
            Command?.CanExecute(CommandParameter) ?? true);
    }
}
```

## 18.12 Cross References

- [第 6 章 MVVM 模式实战](06-mvvm-pattern.md) -- MVVM 中的命令模式
- [第 5 章 数据绑定](05-data-binding.md) -- 命令绑定语法
- [第 16 章 输入处理与事件系统](16-input-events.md) -- 事件与命令的关系
- [第 17 章 对话框与弹出层](17-dialogs-popups.md) -- 对话框中的命令
- [第 20 章 跨平台适配](20-cross-platform.md) -- 跨平台快捷键适配

## 18.13 Common Pitfalls

1. **忘记调用 NotifyCanExecuteChanged**：CanExecute 条件变化后不通知，按钮状态不会更新。最常见的错误是在 `partial void OnXxxChanged` 中忘记调用 `SaveCommand.NotifyCanExecuteChanged()`。

2. **异步命令不处理异常**：未捕获的异常会导致命令卡在 `IsRunning = true` 状态，后续无法执行。始终使用 try/finally 确保 `IsRunning` 被重置。

3. **命令参数类型不匹配**：编译绑定会检查类型，运行时绑定不会。如果声明 `[RelayCommand] void Delete(string id)` 但传入了 `int`，运行时会失败。

4. **在 CanExecute 中做耗时操作**：CanExecute 可能在 UI 线程上被频繁调用。保持它轻量，使用属性缓存结果。

5. **忘记取消异步操作**：如果用户关闭页面时异步命令还在执行，可能导致内存泄漏或异常。使用 `CancellationToken` 并在页面关闭时取消。

6. **命令与事件同时处理同一交互**：如果一个按钮同时绑定了 `Command` 和 `Click` 事件，可能会执行两次逻辑。选择其中一种方式。

7. **在 DataTemplate 中绑定父 ViewModel 的命令**：在数据模板中，`DataContext` 是数据项本身。需要使用 `ElementName` 或 `AncestorType` 来绑定父 ViewModel 的命令。

8. **异步命令并发执行**：默认情况下 `AsyncRelayCommand` 不允许并发执行。如果需要并发（如同时下载多个文件），使用 `[RelayCommand(AllowConcurrentExecutions = true)]`。

9. **CommandParameter 绑定到 null**：如果 `CommandParameter` 绑定的数据项可能为 null，确保命令方法能处理 null 参数。

10. **KeyBinding 的 Gesture 与 InputGesture 的区别**：`KeyBinding.Gesture` 实际注册快捷键，`MenuItem.InputGesture` 只是视觉提示文本。

## 18.14 Try It Yourself

1. **基础练习**：创建一个 ViewModel，包含 `IncrementCommand` 和 `DecrementCommand`，当计数器为 0 时禁用递减命令。

2. **异步命令练习**：创建一个 `LoadDataCommand`，支持取消，在执行期间显示 ProgressBar，完成后显示数据列表。

3. **带参数命令练习**：创建一个列表，每个项目有 `EditCommand` 和 `DeleteCommand`，通过 `CommandParameter` 传递项目 ID。

4. **CanExecute 练习**：创建一个表单，`SaveCommand` 的 CanExecute 依赖于多个字段的验证状态，任何字段变化时自动更新按钮状态。

5. **键盘快捷键练习**：为一个编辑器页面添加 `KeyBinding`：Ctrl+S 保存、Ctrl+Z 撤销、Ctrl+Shift+Z 重做。

6. **命令测试练习**：为第 3 步的命令编写单元测试，测试 CanExecute 逻辑和执行结果。

7. **防抖命令练习**：创建一个搜索框，实现 300ms 防抖的搜索命令，用户快速输入时不会频繁触发搜索。

8. **综合练习**：在 CodexSwitch 的 `MainWindowViewModel` 中找到所有 `[RelayCommand]`，研究它们如何与 XAML 绑定、如何管理状态、如何处理异步操作。然后为其中一个命令编写单元测试。
