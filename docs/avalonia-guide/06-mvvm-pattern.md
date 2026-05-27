# 6. MVVM 模式实战

> **写给零基础的你**：MVVM 听起来很高深，其实就是一个"分工合作"的思路。想象一家餐厅：
> - **View（视图）** = 餐厅的装修和菜单，顾客看到的样子（界面上的按钮、文字、图片）
> - **ViewModel（视图模型）** = 服务员，接收顾客的点单，告诉厨房做什么，再把做好的菜端给顾客（处理逻辑，连接界面和数据）
> - **Model（模型）** = 厨房和食材，真正干活的地方（数据和业务逻辑）
>
> 这样分工的好处是：换个装修风格（换界面），厨房不用改；换个厨师（换数据源），装修不用动。

## 6.1 概述

MVVM（Model-View-ViewModel）是 Avalonia 推荐的应用架构模式。它将 UI（View）、展示逻辑（ViewModel）和数据模型（Model）分离，使代码更易测试、维护和扩展。本章通过 CodexSwitch 的 `MainWindowViewModel` 展示 MVVM 的完整实践，详细讲解 CommunityToolkit.Mvvm 的所有功能。

学完本章后，你将能够：
- 掌握 [ObservableProperty] 的所有选项（属性名转换、通知控制、验证）
- 掌握 [RelayCommand] 的所有选项（CanExecute、异步、取消）
- 理解 INotifyPropertyChanged 和 INotifyDataErrorInfo
- 掌握 Messenger/事件聚合器模式
- 掌握导航服务模式和对话框服务模式
- 理解 ViewModel 的单元测试

## 6.2 核心概念

### 6.2.1 MVVM 架构概述

```
┌─────────────┐     ┌──────────────────────┐     ┌───────────┐
│    View      │     │     ViewModel        │     │   Model   │
│  (AXAML)     │────▶│  (ObservableObject)  │────▶│  (Data)   │
│              │     │                      │     │           │
│  MainWindow  │     │  MainWindowViewModel │     │ AppConfig │
│  Providers   │     │  业务逻辑            │     │ Provider  │
│  Settings    │     │  状态管理            │     │ Model     │
└─────────────┘     └──────────────────────┘     └───────────┘
     │                       │
     │   DataContext          │  INotifyPropertyChanged
     │   Binding              │  ICommand
     └───────────────────────┘
```

| 层 | 职责 | 示例 |
|---|------|------|
| View | UI 声明、样式、布局 | MainWindow.axaml, ProvidersPage.axaml |
| ViewModel | 业务逻辑、状态管理、命令 | MainWindowViewModel.cs |
| Model | 数据结构 | AppConfig.cs, Provider.cs |
| Service | 业务服务、外部交互 | ProxyHostService.cs, ConfigurationStore.cs |

### 6.2.2 ViewModelBase

CodexSwitch 的所有 ViewModel 都继承自 `ViewModelBase`：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace CodexSwitch.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
}
```

`ObservableObject` 来自 CommunityToolkit.Mvvm，实现了 `INotifyPropertyChanged` 和 `INotifyPropertyChanging`。这是绑定系统正常工作的基础——当属性值变化时，UI 通过 `PropertyChanged` 事件得到通知并自动更新。

### 6.2.3 [ObservableProperty] 源代码生成器

`[ObservableProperty]` 是 CommunityToolkit.Mvvm 的核心特性，通过源代码生成器自动为字段生成属性和变更通知代码。

#### 基本用法

MainWindowViewModel 使用了大量的 `[ObservableProperty]`：

> **小白提示：什么是 [ObservableProperty]？**  `[ObservableProperty]` 就像一个"自动通知按钮"。你只需要声明一个普通的字段（比如 `_currentPage`），加上这个特性，编译器就会自动帮你生成一个属性（`CurrentPage`），并且在值变化时自动通知界面更新。你不需要手动写那些繁琐的通知代码。

```csharp
public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    // 声明字段，源生成器自动生成 CurrentPage 属性
    // 你只需要写这一行，编译器帮你生成属性和通知代码
    [ObservableProperty]
    private string _currentPage = "Home";

    [ObservableProperty]
    private string _activeProviderId = "";

    [ObservableProperty]
    private string _selectedProviderName = "";

    [ObservableProperty]
    private bool _isUsageRefreshing;

    [ObservableProperty]
    private decimal _estimatedCost;

    [ObservableProperty]
    private string _endpoint = "";

    [ObservableProperty]
    private string _proxyStatus = "Starting";
}
```

源生成器为每个 `[ObservableProperty]` 字段自动生成：

```csharp
// 编译器自动生成的代码（简化）
public string CurrentPage
{
    get => _currentPage;
    set
    {
        if (EqualityComparer<string>.Default.Equals(_currentPage, value)) return;
        OnCurrentPageChanging(value);
        OnPropertyChanging(nameof(CurrentPage));
        _currentPage = value;
        OnCurrentPageChanged(value);
        OnPropertyChanged(nameof(CurrentPage));
    }
}

// 部分方法，允许开发者插入自定义逻辑
partial void OnCurrentPageChanging(string value);
partial void OnCurrentPageChanged(string value);
```

#### 命名约定

| 字段名 | 生成的属性名 |
|--------|-------------|
| `_currentPage` | `CurrentPage` |
| `_isActive` | `IsActive` |
| `_selectedProviderId` | `SelectedProviderId` |
| `_isUsageRefreshing` | `IsUsageRefreshing` |

字段必须以 `_` 开头、使用 camelCase，类必须声明为 `partial`。

#### [ObservableProperty] 的高级选项

```csharp
// 1. 自定义属性名
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]  // 当 FirstName 变化时通知 FullName
private string _firstName;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _lastName;

public string FullName => $"{FirstName} {LastName}";

// 2. 通知 CanExecute 变化
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]  // 当 Name 变化时通知 SaveCommand
private string _name = "";

// 3. 通知属性变化
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(IsVisible))]
private bool _isSelected;

public bool IsVisible => IsSelected && IsEnabled;

// 4. 验证
[ObservableProperty]
[NotifyDataErrorInfo]
[Required(ErrorMessage = "Name is required")]
[MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
private string _name = "";
```

## 6.3 进阶用法

### 6.3.1 属性变更回调与计算属性

#### partial 方法回调

CodexSwitch 使用 `partial void OnXxxChanged` 回调来响应属性变化：

```csharp
partial void OnCurrentPageChanged(string value)
{
    // 当页面变化时，通知所有相关的计算属性
    OnPropertyChanged(nameof(IsHomePageVisible));
    OnPropertyChanged(nameof(IsProvidersPageVisible));
    OnPropertyChanged(nameof(IsCodexSessionsPageVisible));
    OnPropertyChanged(nameof(IsUsagePageVisible));
    OnPropertyChanged(nameof(IsModelsPageVisible));
    OnPropertyChanged(nameof(IsSettingsPageVisible));
    OnPropertyChanged(nameof(IsHomeNavSelected));
}
```

#### 计算属性

ViewModel 中包含只读的计算属性，根据其他属性的值派生：

```csharp
public bool IsHomePageVisible => CurrentPage == "Home";
public bool IsProvidersPageVisible => CurrentPage == "Providers";
public bool IsHomeNavSelected => IsHomePageVisible;
public bool IsUsageDataVisible => IsHomePageVisible || IsUsagePageVisible;
```

这些计算属性通过在 `OnXxxChanged` 回调中调用 `OnPropertyChanged(nameof(...))` 来触发 UI 更新。

### 6.3.2 命令（Commands）

CodexSwitch 在构造函数中手动创建 `RelayCommand` 和 `AsyncRelayCommand`：

```csharp
public MainWindowViewModel(/* dependencies */)
{
    // 无参数命令
    ShowHomeCommand = new RelayCommand(() => CurrentPage = "Home");
    ShowProvidersCommand = new RelayCommand(() => CurrentPage = "Providers");
    ShowUsageCommand = new RelayCommand(() => CurrentPage = "Usage");
    ShowModelsCommand = new RelayCommand(() => CurrentPage = "Models");

    // 调用方法的命令
    ShowCodexSessionsCommand = new RelayCommand(ShowCodexSessions);
    OpenSettingsCommand = new RelayCommand(OpenSettings);

    // 带参数的命令
    SelectClientAppCommand = new RelayCommand<ClientAppItem>(SelectClientApp);
    SelectSettingsTabCommand = new RelayCommand<string>(tab => SettingsTab = tab ?? "General");

    // 异步命令
    ToggleProxyCommand = new AsyncRelayCommand(ToggleProxyAsync);
}
```

> **小白提示：什么是 async/await？**  async/await 是 C# 处理"需要等待的操作"的方式。比如你去餐厅点餐，点完后不用站在厨房门口等，可以先回座位玩手机（做其他事），菜做好了服务员会叫你（await 返回结果）。`async` 标记这个方法"可以等待"，`await` 标记"在这里等一下，但不阻塞其他操作"。常见的需要等待的操作：网络请求、读写文件、数据库查询。

> **小白提示：什么是 CancellationToken？**  CancellationToken 就是"取消信号"。比如你点了一个外卖，等了 30 分钟还没到，你可以取消订单。CancellationToken 就是告诉程序"如果用户说不等了，就停止这个操作"。
```

命令属性声明：

```csharp
public IRelayCommand ShowHomeCommand { get; }
public IRelayCommand ShowProvidersCommand { get; }
public IRelayCommand<ClientAppItem> SelectClientAppCommand { get; }
public IAsyncRelayCommand ToggleProxyCommand { get; }
```

### 6.3.3 [RelayCommand] 属性

CommunityToolkit.Mvvm 也支持用 `[RelayCommand]` 属性自动生成命令：

```csharp
// 基本用法
[RelayCommand]
private void Increment() { Count++; }

// 带 CanExecute
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { /* ... */ }
private bool CanSave => !string.IsNullOrEmpty(Name);

// 异步命令
[RelayCommand]
private async Task LoadDataAsync(CancellationToken token)
{
    await _service.GetDataAsync(token);
}

// 带参数的异步命令
[RelayCommand]
private async Task DeleteItemAsync(ItemModel item, CancellationToken token)
{
    await _service.DeleteAsync(item.Id, token);
}

// 取消命令
[RelayCommand(IncludeCancelCommand = true)]
private async Task LongRunningTask(CancellationToken token)
{
    await Task.Delay(10000, token);
}
// 自动生成 LongRunningTaskCommand 和 CancelLongRunningTaskCommand
```

**[RelayCommand] 的所有选项：**

| 选项 | 说明 |
|------|------|
| `CanExecute` | CanExecute 方法名 |
| `IncludeCancelCommand` | 是否生成取消命令 |
| `AllowConcurrentExecutions` | 是否允许并发执行 |
| `FlowExceptionsToTaskScheduler` | 是否将异常传递到 TaskScheduler |
| `CommandName` | 自定义命令名称 |

CodexSwitch 选择手动创建命令，因为构造函数集中初始化便于管理大量命令，且 Lambda 表达式对简单逻辑更简洁。

### 6.3.4 Messenger/事件聚合器模式

CommunityToolkit.Mvvm 提供了 `IMessenger` 接口，用于实现松耦合的消息传递。

```csharp
// 定义消息
public record UserLoggedInMessage(string Username);

// 发送方
public class LoginViewModel : ViewModelBase
{
    private readonly IMessenger _messenger;

    public LoginViewModel(IMessenger messenger)
    {
        _messenger = messenger;
    }

    [RelayCommand]
    private void Login()
    {
        // ... 登录逻辑
        _messenger.Send(new UserLoggedInMessage("John"));
    }
}

// 接收方
public class MainViewModel : ViewModelBase
{
    public MainViewModel(IMessenger messenger)
    {
        messenger.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
    }

    private void OnUserLoggedIn(UserLoggedInMessage message)
    {
        WelcomeMessage = $"Welcome, {message.Username}!";
    }
}
```

**Messenger 的优势：**
- 松耦合：发送方和接收方不需要直接引用
- 跨 ViewModel 通信：不同 ViewModel 之间可以发送消息
- 生命周期管理：支持自动注销

### 6.3.5 导航服务模式

CodexSwitch 使用简单的状态驱动导航：

```csharp
[ObservableProperty]
private string _currentPage = "Home";

// 计算属性
public bool IsHomePageVisible => CurrentPage == "Home";
public bool IsProvidersPageVisible => CurrentPage == "Providers";

// 命令
ShowHomeCommand = new RelayCommand(() => CurrentPage = "Home");
ShowProvidersCommand = new RelayCommand(() => CurrentPage = "Providers");
```

```xml
<Grid Grid.Row="1">
    <pages:HomePage IsVisible="{Binding IsHomePageVisible}"/>
    <pages:ProvidersPage IsVisible="{Binding IsProvidersPageVisible}"/>
    <pages:UsagePage IsVisible="{Binding IsUsagePageVisible}"/>
</Grid>
```

所有页面叠加在同一个 Grid Cell 中，通过 `IsVisible` 绑定同一时刻只有一个页面可见。选择这种方式而非 `ContentControl + DataTemplate` 是因为页面需要保持状态（如滚动位置、表单输入）。

**更复杂的导航服务模式：**

```csharp
public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo<TViewModel>(object parameter) where TViewModel : ViewModelBase;
    bool CanGoBack { get; }
    void GoBack();
}

public class NavigationService : INavigationService
{
    private readonly Stack<ViewModelBase> _backStack = new();
    private ViewModelBase? _currentViewModel;

    public event Action<ViewModelBase>? Navigated;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        if (_currentViewModel is not null)
            _backStack.Push(_currentViewModel);

        _currentViewModel = App.GetRequiredService<TViewModel>();
        Navigated?.Invoke(_currentViewModel);
    }

    public bool CanGoBack => _backStack.Count > 0;

    public void GoBack()
    {
        if (_backStack.Count > 0)
        {
            _currentViewModel = _backStack.Pop();
            Navigated?.Invoke(_currentViewModel);
        }
    }
}
```

### 6.3.6 对话框服务模式

```csharp
public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<string?> ShowInputAsync(string title, string message, string defaultValue);
    Task ShowErrorAsync(string title, string message);
}

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var dialog = new ConfirmationDialog
        {
            Title = title,
            Message = message
        };

        var result = await dialog.ShowDialog<bool?>(App.MainWindow);
        return result == true;
    }
}
```

## 6.4 组件详解大全

### 6.4.1 ObservableObject 完整用法

`ObservableObject` 是 CommunityToolkit.Mvvm 的基类，实现了以下接口：

- `INotifyPropertyChanged`：属性变化通知
- `INotifyPropertyChanging`：属性即将变化通知

```csharp
public class MyViewModel : ObservableObject
{
    private string _name = "";

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    // 带回调的属性设置
    private int _count;

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value, nameof(Count));
    }
}
```

### 6.4.2 RelayCommand 完整用法

```csharp
// 无参数命令
var command = new RelayCommand(() => { /* 执行逻辑 */ });

// 带参数命令
var command = new RelayCommand<string>(param => { /* 执行逻辑 */ });

// 带 CanExecute
var command = new RelayCommand(Execute, CanExecute);
private bool CanExecute() => !string.IsNullOrEmpty(Name);

// 异步命令
var command = new AsyncRelayCommand(ExecuteAsync);
private async Task ExecuteAsync(CancellationToken token) { /* 异步逻辑 */ }

// 带取消的异步命令
var command = new AsyncRelayCommand(ExecuteAsync);
// command.Cancel() 可以取消执行
```

### 6.4.3 INotifyDataErrorInfo 接口

用于数据验证：

```csharp
public class ValidatableViewModel : ObservableObject, INotifyDataErrorInfo
{
    private string _name = "";

    public string Name
    {
        get => _name;
        set
        {
            SetProperty(ref _name, value);
            ValidateProperty(value, nameof(Name));
        }
    }

    public bool HasErrors => _errors.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        return _errors.TryGetValue(propertyName ?? "", out var errors)
            ? errors
            : Enumerable.Empty<string>();
    }

    private readonly Dictionary<string, List<string>> _errors = new();

    private void ValidateProperty(object? value, string propertyName)
    {
        var errors = new List<string>();

        if (propertyName == nameof(Name))
        {
            if (string.IsNullOrEmpty(value?.ToString()))
                errors.Add("Name is required");
            if (value?.ToString()?.Length < 3)
                errors.Add("Name must be at least 3 characters");
        }

        _errors[propertyName] = errors;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
```

## 6.5 CodexSwitch 实战

### 6.5.1 MainWindowViewModel 的结构

```csharp
public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    // 服务依赖
    private readonly AppPaths _paths;
    private readonly ConfigurationStore _store;
    private readonly PriceCalculator _priceCalculator;
    private readonly UsageMeter _usageMeter;
    private readonly I18nService _i18n;

    // 命令
    public IRelayCommand ShowHomeCommand { get; }
    public IRelayCommand ShowProvidersCommand { get; }
    public IRelayCommand<ClientAppItem> SelectClientAppCommand { get; }
    public IAsyncRelayCommand ToggleProxyCommand { get; }

    // 属性
    [ObservableProperty]
    private string _currentPage = "Home";

    [ObservableProperty]
    private string _activeProviderId = "";

    // 计算属性
    public bool IsHomePageVisible => CurrentPage == "Home";
    public bool IsProvidersPageVisible => CurrentPage == "Providers";

    // 属性变更回调
    partial void OnCurrentPageChanged(string value)
    {
        OnPropertyChanged(nameof(IsHomePageVisible));
        OnPropertyChanged(nameof(IsProvidersPageVisible));
    }

    // 异步释放
    public async ValueTask DisposeAsync()
    {
        await _proxyHostService.StopAsync();
        _usageLogWriter.Dispose();
    }
}
```

### 6.5.2 View-ViewModel 连接

运行时的 DataContext 在 `App.axaml.cs` 中设置：

```csharp
public override void OnFrameworkInitializationCompleted()
{
    var vm = new MainWindowViewModel(/* dependencies */);
    var mainWindow = new MainWindow { DataContext = vm };
    mainWindow.Show();
}
```

### 6.5.3 x:DataType 的继承

`x:DataType` 指定了整个视觉树的绑定上下文类型，子控件会继承，除非自己设置了 `x:DataType`：

```xml
<Window x:DataType="vm:MainWindowViewModel">  <!-- 顶层类型 -->
    <TextBlock Text="{Binding CurrentPage}"/>  <!-- 使用 MainWindowViewModel -->

    <ItemsControl ItemsSource="{Binding SelectedProviderRows}">
        <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="vm:ProviderListItem">  <!-- 覆盖为列表项类型 -->
                <TextBlock Text="{Binding DisplayName}"/>  <!-- 使用 ProviderListItem -->
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>
```

## 6.6 举一反三

### 6.6.1 源代码生成器如何工作

CommunityToolkit.Mvvm 使用 C# 源代码生成器（Source Generator）在编译时自动扩展 `[ObservableProperty]` 和 `[RelayCommand]`。

```
开发者代码:
    [ObservableProperty]
    private string _currentPage = "Home";
        ↓
源代码生成器分析特性:
    1. 提取字段名 "_currentPage"
    2. 计算属性名 "CurrentPage"（去除 "_"，首字母大写）
    3. 提取字段类型 "string"
    4. 提取默认值 "Home"
        ↓
生成代码:
    - CurrentPage 属性（getter/setter + SetProperty）
    - OnCurrentPageChanging partial 方法
    - OnCurrentPageChanged partial 方法
```

### 6.6.2 partial 方法的威力

源生成器为每个属性生成 `partial` 方法，允许开发者在不修改生成代码的情况下插入逻辑：

```csharp
[ObservableProperty]
private string _currentPage = "Home";

// 开发者实现的 partial 方法
partial void OnCurrentPageChanged(string value)
{
    OnPropertyChanged(nameof(IsHomePageVisible));
    OnPropertyChanged(nameof(IsProvidersPageVisible));
}
```

这种模式实现了"开放-封闭原则"：生成的代码封闭不修改，开发者通过 partial 方法开放扩展点。

## 6.7 最佳实践与设计模式

### 6.7.1 ViewModel 设计原则

1. **单一职责**：每个 ViewModel 只负责一个页面或功能
2. **依赖注入**：通过构造函数注入服务
3. **命令模式**：使用 ICommand 处理用户交互
4. **计算属性**：使用计算属性派生状态
5. **异步操作**：使用 AsyncRelayCommand 处理异步操作

### 6.7.2 代码组织最佳实践

1. **使用 [ObservableProperty]**：减少样板代码
2. **使用 partial 方法**：在属性变更时执行副作用
3. **使用计算属性**：派生状态，避免冗余
4. **使用命令**：将 UI 交互逻辑封装为命令
5. **实现 IAsyncDisposable**：正确释放异步资源

## Deep Dive

### INotifyPropertyChanged 的实现

```csharp
public abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        OnPropertyChanging(propertyName);
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

## Cross References

- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 x:DataType 和 DataContext 的 AXAML 语法
- **[第 5 章：数据绑定](05-data-binding.md)** — 深入理解绑定如何连接 View 和 ViewModel
- **[第 7 章：样式与主题](07-styling-theming.md)** — 了解样式系统如何响应 ViewModel 状态变化

## Common Pitfalls

### 1. 忘记 partial 关键字

**问题**：`[ObservableProperty]` 需要类声明为 `partial`。

```csharp
// 错误
public class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;  // 编译错误
}

// 正确
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;
}
```

### 2. 字段命名不符合约定

**问题**：字段必须以 `_` 开头，否则源生成器无法正确推断属性名。

```csharp
// 错误：没有 _ 前缀
[ObservableProperty]
private string name;  // 生成的属性名会是 "Name"，但不规范

// 正确
[ObservableProperty]
private string _name;
```

### 3. 计算属性没有触发通知

**问题**：计算属性依赖的属性变化时，没有手动通知 UI。

```csharp
// 问题：IsVisible 不会自动更新
[ObservableProperty]
private bool _isSelected;
public bool IsVisible => IsSelected && IsEnabled;

// 解决：在 OnSelectedChanged 中手动通知
partial void OnIsSelectedChanged(bool value)
{
    OnPropertyChanged(nameof(IsVisible));
}
```

### 4. 在 UI 线程执行耗时操作

**问题**：同步命令会阻塞 UI 线程。

```csharp
// 错误：阻塞 UI
[RelayCommand]
private void LoadData()
{
    var data = _service.LoadData();  // 阻塞
}

// 正确：使用异步命令
[RelayCommand]
private async Task LoadDataAsync(CancellationToken token)
{
    var data = await _service.LoadDataAsync(token);
}
```

### 5. 在 ViewModel 中直接引用 View

**问题**：ViewModel 直接引用 View，违反 MVVM 原则，导致无法独立测试。

```csharp
// 错误
public MainWindowViewModel(MainWindow mainWindow) { ... }

// 正确：通过 DataContext 绑定，ViewModel 不知道 View 的存在
```

### 6. 忘记取消异步命令

**问题**：异步命令在 ViewModel 销毁时可能仍在执行。

```csharp
// 问题：没有取消机制
[RelayCommand]
private async Task LoadDataAsync() { /* ... */ }

// 解决：使用 CancellationToken
[RelayCommand]
private async Task LoadDataAsync(CancellationToken token) { /* ... */ }

// 或者在 Dispose 中取消
public void Dispose()
{
    _loadDataCts?.Cancel();
}
```

### 7. 在构造函数中执行异步操作

**问题**：构造函数不能是异步的。

```csharp
// 错误：构造函数中调用异步方法
public MainWindowViewModel()
{
    await LoadDataAsync();  // 编译错误
}

// 正确：使用异步初始化方法
public MainWindowViewModel()
{
    InitializeCommand = new AsyncRelayCommand(InitializeAsync);
}

private async Task InitializeAsync()
{
    await LoadDataAsync();
}
```

### 8. 忘记通知命令的 CanExecute 变化

**问题**：当影响 CanExecute 的属性变化时，命令状态不会自动更新。

```csharp
// 问题：Name 变化时 SaveCommand 的 CanExecute 不会重新评估
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { }
private bool CanSave => !string.IsNullOrEmpty(Name);

[ObservableProperty]
private string _name = "";

// 解决：添加 NotifyCanExecuteChangedFor
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private string _name = "";
```

### 9. Messenger 注册后忘记注销

**问题**：Messenger 注册后如果不注销，可能导致内存泄漏。

```csharp
// 问题：没有注销
messenger.Register<MyMessage>(this, OnMessage);

// 解决：在 Dispose 中注销
public void Dispose()
{
    messenger.UnregisterAll(this);
}

// 或者使用自动注销（CommunityToolkit.Mvvm 8.0+）
// Messenger 在对象被 GC 时自动注销
```

### 10. 在 DataTemplate 中访问父级 ViewModel

**问题**：DataTemplate 内的 DataContext 是当前数据项，不是父级 ViewModel。

```xml
<!-- 错误：直接绑定到父级 ViewModel 的属性 -->
<DataTemplate x:DataType="vm:ItemModel">
    <Button Command="{Binding DeleteCommand}"/>  <!-- ItemModel 没有 DeleteCommand -->
</DataTemplate>

<!-- 正确：使用 #ElementName 或 x:Reference -->
<DataTemplate x:DataType="vm:ItemModel">
    <Button Command="{Binding #Root.DataContext.DeleteCommand}"/>
</DataTemplate>
```

### 11. 使用 [RelayCommand] 但忘记声明 partial

**问题**：`[RelayCommand]` 需要方法是 `private` 的，类是 `partial` 的。

```csharp
// 错误：方法是 public 的
[RelayCommand]
public void Save() { }  // 应该是 private

// 错误：类不是 partial
public class MyViewModel : ViewModelBase
{
    [RelayCommand]
    private void Save() { }  // 编译错误
}
```

### 12. 在 ViewModel 中使用 Dispatcher

**问题**：ViewModel 通常不应该直接访问 UI 线程。

```csharp
// 错误：ViewModel 直接访问 Dispatcher
Dispatcher.UIThread.Post(() => { /* ... */ });

// 正确：使用 SynchronizationContext 或让 View 处理
// 或者在服务中封装 UI 线程访问
```

## Try It Yourself

### 练习 1：创建一个简单的 MVVM 页面

1. 创建 ViewModel，使用 `[ObservableProperty]` 和 `[RelayCommand]`
2. 创建对应的 AXAML View，设置 `x:DataType` 和 `Design.DataContext`
3. 绑定属性和命令，运行项目验证

### 练习 2：分析 MainWindowViewModel 的命令模式

打开 `MainWindowViewModel.cs`，找到构造函数中的命令初始化代码。列出所有 `RelayCommand`、`RelayCommand<T>` 和 `AsyncRelayCommand` 的使用，理解不同命令类型的选择标准。

### 练习 3：实现计算属性

在 ViewModel 中创建两个 `[ObservableProperty]` 和一个计算属性，在 `OnXxxChanged` 中触发计算属性的通知。在 AXAML 中绑定计算属性，验证 UI 更新。

### 练习 4：观察源代码生成器的输出

1. 编译 CodexSwitch 项目
2. 在 IDE 中查看 `[ObservableProperty]` 生成的代码（右键"转到定义"或查看 `obj` 目录下的 `.g.cs` 文件）
3. 理解生成代码与手写 `SetProperty` 的等价性

### 练习 5：实现 IAsyncDisposable

创建一个 ViewModel，注入一个需要异步释放的服务（如 HttpClient），实现 `IAsyncDisposable`，在应用关闭时正确清理资源。

### 练习 6：实现 Messenger 消息传递

1. 定义一个消息类
2. 在一个 ViewModel 中发送消息
3. 在另一个 ViewModel 中接收消息
4. 验证消息传递正常工作

### 练习 7：实现导航服务

1. 创建一个 INavigationService 接口
2. 实现 NavigationService
3. 在 ViewModel 中注入导航服务
4. 实现页面导航和返回功能

### 练习 8：实现数据验证

1. 创建一个带验证的 ViewModel
2. 使用 INotifyDataErrorInfo 接口
3. 在 AXAML 中显示验证错误
4. 测试验证逻辑

## Cross References

- **[第 1 章：Avalonia 概览](01-avalonia-overview.md)** — 了解 Avalonia 的整体架构和 MVVM 设计理念
- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 AXAML 中的 Command 绑定和 x:DataType
- **[第 5 章：数据绑定](05-data-binding.md)** — 理解 ViewModel 如何通过绑定驱动 UI
- **[第 8 章：DataTemplate](08-data-templates.md)** — 掌握 DataTemplate 中的 ViewModel 绑定
- **[第 18 章：命令系统](18-commands.md)** — 深入学习 RelayCommand、AsyncRelayCommand 的实现原理
