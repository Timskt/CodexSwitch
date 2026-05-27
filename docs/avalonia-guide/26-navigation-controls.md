# 26. 导航控件与页面路由

> **写给零基础的你**：导航就像"翻书"。你从目录点击一个章节名，就翻到那一页。导航控件就是帮你在不同页面之间"翻来翻去"的工具。在桌面应用中，导航不一定像浏览器那样有地址栏和前进后退按钮，更多时候是通过侧边栏菜单、选项卡或按钮来切换内容区域。

## 26.1 概述

Avalonia 提供了多种导航控件和页面路由机制。从最简单的 TabControl 到完整的 Frame 导航系统，再到社区提供的 NavigationView，开发者可以根据应用复杂度选择合适的方案。CodexSwitch 使用 Grid + IsVisible 的方式切换页面（参见第 4 章），这是简单应用的最佳实践。但对于更复杂的应用场景，Avalonia 还提供了更标准的导航框架。

学完本章后，你将能够：
- 掌握 TabControl 的所有属性和自定义用法
- 掌握 Expander、GroupBox 等分组容器
- 理解 ContentControl 导航模式与 IsVisible 模式的对比
- 掌握 Frame 导航（BackStack、ForwardStack、Navigate）
- 了解 NavigationView（FluentAvaloniaUI）的集成方式
- 理解不同页面切换策略及其优劣

## 26.2 核心概念

### 26.2.1 页面切换策略对比

在 Avalonia 中实现页面切换有多种策略，每种都有其适用场景：

| 策略 | 实现方式 | 优点 | 缺点 | 适用场景 |
|------|---------|------|------|---------|
| IsVisible 切换 | Grid 叠加 + IsVisible 绑定 | 状态保持、简单、性能好 | 内存占用（所有页面常驻） | 简单应用（CodexSwitch） |
| ContentControl | 绑定 Content 到 ViewModel | 支持 DataTemplate 选择、按需创建 | 需要手动管理缓存 | MVVM 应用 |
| Frame 导航 | Frame.Navigate() | 支持前进/后退、URI 路由 | 需要页面类继承 | 复杂导航应用 |
| NavigationView | 侧边栏导航 + Frame | 完整导航体验、WinUI 风格 | 较重、需要社区包 | 设置页、管理系统 |

**选择建议：**
- 页面少于 10 个，且需要保持所有页面状态 -> IsVisible
- 页面较多，需要按需加载 -> ContentControl + DataTemplate
- 需要前进/后退、URL 路由 -> Frame
- 需要侧边栏导航体验 -> NavigationView

### 26.2.2 TabControl 详解

TabControl 是最简单的"多页面"控件，通过选项卡切换内容。它适合设置页面、属性面板等需要在同一区域展示多个相关视图的场景。

**什么是 TabControl？**
TabControl 由两部分组成：选项卡头部（Tab Strip）和内容区域（Content Area）。用户点击不同的选项卡头部，内容区域就会切换到对应的面板。

#### TabControl 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SelectedIndex` | int | -1 | 当前选中的选项卡索引，-1 表示未选中 |
| `SelectedItem` | object | null | 当前选中的选项卡项 |
| `ItemsSource` | IEnumerable | null | 数据源，用于动态生成选项卡 |
| `ItemTemplate` | DataTemplate | null | 选项卡头部的模板 |
| `ContentTemplate` | DataTemplate | null | 选项卡内容区域的模板 |
| `TabStripPlacement` | Dock | Top | 选项卡条的位置（Top/Bottom/Left/Right） |
| `IsTabStop` | bool | true | 是否参与 Tab 键导航 |

#### TabControl 的事件

| 事件 | 说明 |
|------|------|
| `SelectionChanged` | 选中项改变时触发 |

#### 示例 1：基本 TabControl

```xml
<TabControl>
    <TabItem Header="常规">
        <StackPanel Spacing="10" Margin="16">
            <TextBlock Text="常规设置" FontWeight="Bold"/>
            <TextBox Watermark="输入名称"/>
            <CheckBox Content="启用日志"/>
        </StackPanel>
    </TabItem>
    <TabItem Header="高级">
        <StackPanel Spacing="10" Margin="16">
            <TextBlock Text="高级设置" FontWeight="Bold"/>
            <NumericUpDown Value="100" Minimum="0" Maximum="1000"/>
            <CheckBox Content="调试模式"/>
        </StackPanel>
    </TabItem>
    <TabItem Header="关于">
        <StackPanel Spacing="10" Margin="16">
            <TextBlock Text="版本 1.0.0"/>
            <TextBlock Text="版权所有 2024"/>
        </StackPanel>
    </TabItem>
</TabControl>
```

#### 示例 2：ItemsSource 绑定（MVVM 模式）

```xml
<TabControl ItemsSource="{Binding Tabs}"
            SelectedItem="{Binding SelectedTab, Mode=TwoWay}">
    <TabControl.ItemTemplate>
        <DataTemplate x:DataType="vm:TabItemViewModel">
            <TextBlock Text="{Binding Header}"/>
        </DataTemplate>
    </TabControl.ItemTemplate>
    <TabControl.ContentTemplate>
        <DataTemplate x:DataType="vm:TabItemViewModel">
            <ContentControl Content="{Binding Content}"/>
        </DataTemplate>
    </TabControl.ContentTemplate>
</TabControl>
```

```csharp
// TabItemViewModel 定义
public class TabItemViewModel : ObservableObject
{
    public string Header { get; set; } = "";
    public object Content { get; set; } = null!;
}

// MainWindowViewModel 中
public ObservableCollection<TabItemViewModel> Tabs { get; } = new()
{
    new TabItemViewModel { Header = "常规", Content = new GeneralSettingsViewModel() },
    new TabItemViewModel { Header = "高级", Content = new AdvancedSettingsViewModel() },
    new TabItemViewModel { Header = "关于", Content = new AboutViewModel() },
};

[ObservableProperty]
private TabItemViewModel? _selectedTab;
```

#### 示例 3：TabStripPlacement 选项卡位置

```xml
<!-- 选项卡在左侧（垂直选项卡） -->
<TabControl TabStripPlacement="Left">
    <TabItem Header="Tab 1">
        <TextBlock Text="Content 1" Margin="16"/>
    </TabItem>
    <TabItem Header="Tab 2">
        <TextBlock Text="Content 2" Margin="16"/>
    </TabItem>
</TabControl>

<!-- 选项卡在底部 -->
<TabControl TabStripPlacement="Bottom">
    <TabItem Header="Tab 1">
        <TextBlock Text="Content 1" Margin="16"/>
    </TabItem>
</TabControl>
```

#### 示例 4：自定义 TabItem 样式

```xml
<Window.Styles>
    <!-- 自定义选项卡头部样式 -->
    <Style Selector="TabItem">
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Padding" Value="16,10"/>
    </Style>
    <!-- 选中状态样式 -->
    <Style Selector="TabItem:selected">
        <Setter Property="FontWeight" Value="Bold"/>
    </Style>
    <!-- 自定义选项卡头部模板 -->
    <Style Selector="TabItem /template/ ContentPresenter#PART_HeaderPresenter">
        <Setter Property="Background" Value="Transparent"/>
    </Style>
</Window.Styles>
```

#### 示例 5：在选项卡头部放置图标

```xml
<TabControl>
    <TabItem>
        <TabItem.Header>
            <StackPanel Orientation="Horizontal" Spacing="6">
                <PathIcon Data="{StaticResource SettingsIcon}" Width="16" Height="16"/>
                <TextBlock Text="设置"/>
            </StackPanel>
        </TabItem.Header>
        <TextBlock Text="设置内容" Margin="16"/>
    </TabItem>
    <TabItem>
        <TabItem.Header>
            <StackPanel Orientation="Horizontal" Spacing="6">
                <PathIcon Data="{StaticResource InfoIcon}" Width="16" Height="16"/>
                <TextBlock Text="关于"/>
            </StackPanel>
        </TabItem.Header>
        <TextBlock Text="关于内容" Margin="16"/>
    </TabItem>
</TabControl>
```

### 26.2.3 Expander 详解

Expander 是一个可折叠的内容容器，用户点击标题可以展开或收起内容。它类似于网页中的"手风琴"或"折叠面板"。

**什么是 Expander？**
Expander 有一个 Header（标题栏）和一个 Content（内容区域）。默认情况下内容是收起的，用户点击标题栏后内容展开，再次点击则收起。

#### Expander 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `IsExpanded` | bool | false | 是否展开 |
| `Header` | object | null | 标题内容（可以是任意对象） |
| `HeaderTemplate` | DataTemplate | null | 标题模板 |
| `Content` | object | null | 展开内容 |
| `ExpandDirection` | ExpandDirection | Down | 展开方向（Down/Up/Left/Right） |

#### Expander 的事件

| 事件 | 说明 |
|------|------|
| `Expanded` | 展开时触发 |
| `Collapsed` | 收起时触发 |

#### 示例 1：基本 Expander

```xml
<Expander Header="高级选项">
    <StackPanel Spacing="10" Margin="16">
        <CheckBox Content="启用调试模式"/>
        <CheckBox Content="详细日志"/>
        <CheckBox Content="性能监控"/>
    </StackPanel>
</Expander>
```

#### 示例 2：自定义 Header

```xml
<Expander>
    <Expander.Header>
        <StackPanel Orientation="Horizontal" Spacing="8">
            <PathIcon Data="{StaticResource SettingsIcon}" Width="16" Height="16"/>
            <TextBlock Text="服务器设置" FontWeight="SemiBold"/>
            <TextBlock Text="(3 项)" Foreground="Gray"/>
        </StackPanel>
    </Expander.Header>
    <StackPanel Spacing="10" Margin="16">
        <TextBox Watermark="主机地址"/>
        <NumericUpDown Watermark="端口" Value="8080"/>
        <TextBox Watermark="API 密钥"/>
    </StackPanel>
</Expander>
```

#### 示例 3：默认展开 + 双向绑定

```xml
<Expander IsExpanded="{Binding IsAdvancedExpanded, Mode=TwoWay}"
          Header="高级选项">
    <StackPanel Spacing="10" Margin="16">
        <NumericUpDown Value="{Binding MaxRetries}" Minimum="1" Maximum="10"/>
        <NumericUpDown Value="{Binding Timeout}" Minimum="1000" Maximum="60000"/>
    </StackPanel>
</Expander>
```

#### 示例 4：手风琴模式实现

Avalonia 没有内置的手风琴控件，但可以通过代码实现一次只展开一个 Expander 的效果：

```xml
<StackPanel>
    <Expander Header="常规设置"
              IsExpanded="{Binding IsSection1Expanded, Mode=TwoWay}"
              Expanded="OnSectionExpanded">
        <TextBlock Text="常规设置内容" Margin="16"/>
    </Expander>
    <Expander Header="网络设置"
              IsExpanded="{Binding IsSection2Expanded, Mode=TwoWay}"
              Expanded="OnSectionExpanded">
        <TextBlock Text="网络设置内容" Margin="16"/>
    </Expander>
    <Expander Header="安全设置"
              IsExpanded="{Binding IsSection3Expanded, Mode=TwoWay}"
              Expanded="OnSectionExpanded">
        <TextBlock Text="安全设置内容" Margin="16"/>
    </Expander>
</StackPanel>
```

```csharp
// ViewModel 中实现手风琴逻辑
private void OnSectionExpanded(string expandedSection)
{
    // 收起其他所有 section
    IsSection1Expanded = expandedSection == "Section1";
    IsSection2Expanded = expandedSection == "Section2";
    IsSection3Expanded = expandedSection == "Section3";
}
```

#### 示例 5：ExpandDirection 展开方向

```xml
<!-- 向下展开（默认） -->
<Expander ExpandDirection="Down" Header="向下展开">
    <TextBlock Text="内容在下方" Margin="16"/>
</Expander>

<!-- 向上展开 -->
<Expander ExpandDirection="Up" Header="向上展开">
    <TextBlock Text="内容在上方" Margin="16"/>
</Expander>

<!-- 向右展开 -->
<Expander ExpandDirection="Right" Header="向右展开">
    <TextBlock Text="内容在右侧" Margin="16"/>
</Expander>
```

#### 示例 6：Expander 中放置复杂内容

```xml
<Expander Header="Provider 列表">
    <Expander.Content>
        <DataGrid ItemsSource="{Binding Providers}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True"
                  MaxHeight="300">
            <DataGrid.Columns>
                <DataGridTextColumn Header="名称" Binding="{Binding Name}"/>
                <DataGridTextColumn Header="URL" Binding="{Binding BaseUrl}"/>
            </DataGrid.Columns>
        </DataGrid>
    </Expander.Content>
</Expander>
```

### 26.2.4 GroupBox 详解

GroupBox 是一个带标题的分组容器，用于将相关控件组织在一起。它在视觉上用一个边框和标题把内容包围起来。

**什么时候用 GroupBox？**
- 当你需要将一组相关的表单控件分组时
- 当你需要在视觉上区分不同区域的控件时
- 当你需要一个带标题的边框容器时

```xml
<!-- 基本用法 -->
<GroupBox Header="用户信息" Margin="8">
    <StackPanel Spacing="10" Margin="8">
        <TextBox Watermark="姓名"/>
        <TextBox Watermark="邮箱"/>
        <TextBox Watermark="电话"/>
    </StackPanel>
</GroupBox>

<!-- 自定义 Header -->
<GroupBox Margin="8">
    <GroupBox.Header>
        <StackPanel Orientation="Horizontal" Spacing="8">
            <Ellipse Width="8" Height="8" Fill="Green"/>
            <TextBlock Text="连接状态"/>
        </StackPanel>
    </GroupBox.Header>
    <StackPanel Spacing="10" Margin="8">
        <TextBlock Text="已连接到服务器"/>
        <TextBlock Text="延迟: 23ms"/>
    </StackPanel>
</GroupBox>

<!-- 嵌套 GroupBox -->
<GroupBox Header="服务器配置" Margin="8">
    <StackPanel Spacing="10" Margin="8">
        <GroupBox Header="基本设置">
            <StackPanel Spacing="8" Margin="8">
                <TextBox Watermark="主机"/>
                <NumericUpDown Watermark="端口" Value="8080"/>
            </StackPanel>
        </GroupBox>
        <GroupBox Header="认证">
            <StackPanel Spacing="8" Margin="8">
                <TextBox Watermark="用户名"/>
                <TextBox Watermark="密码" PasswordChar="*"/>
            </StackPanel>
        </GroupBox>
    </StackPanel>
</GroupBox>
```

### 26.2.5 ContentControl 导航模式

ContentControl 是 Avalonia 中最灵活的"页面容器"。通过绑定 Content 属性到不同的 ViewModel，配合 DataTemplate 选择机制，可以实现优雅的页面切换。

**ContentControl 的所有属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Content` | object | 当前显示的内容 |
| `ContentTemplate` | DataTemplate | 内容模板 |
| `DataTemplates` | DataTemplates | 用于根据类型选择模板的集合 |

#### 示例 1：ContentControl + DataTemplate 选择

```xml
<ContentControl Content="{Binding CurrentPage}">
    <ContentControl.DataTemplates>
        <DataTemplate x:DataType="vm:HomePageViewModel">
            <pages:HomePage/>
        </DataTemplate>
        <DataTemplate x:DataType="vm:SettingsPageViewModel">
            <pages:SettingsPage/>
        </DataTemplate>
        <DataTemplate x:DataType="vm:ProvidersPageViewModel">
            <pages:ProvidersPage/>
        </DataTemplate>
    </ContentControl.DataTemplates>
</ContentControl>
```

```csharp
// ViewModel 中
[ObservableProperty]
private ObservableObject _currentPage = new HomePageViewModel();

[RelayCommand]
private void ShowHome() => CurrentPage = new HomePageViewModel();

[RelayCommand]
private void ShowSettings() => CurrentPage = new SettingsPageViewModel();

[RelayCommand]
private void ShowProviders() => CurrentPage = new ProvidersPageViewModel();
```

#### 示例 2：带缓存的 ContentControl 导航

```csharp
// 页面缓存管理器
public class PageCache
{
    private readonly Dictionary<Type, ObservableObject> _cache = new();

    public ObservableObject GetOrCreate<T>() where T : ObservableObject, new()
    {
        if (!_cache.TryGetValue(typeof(T), out var page))
        {
            page = new T();
            _cache[typeof(T)] = page;
        }
        return page;
    }
}

// 使用
[RelayCommand]
private void ShowHome() => CurrentPage = _cache.GetOrCreate<HomePageViewModel>();

[RelayCommand]
private void ShowSettings() => CurrentPage = _cache.GetOrCreate<SettingsPageViewModel>();
```

#### 示例 3：ContentControl 与 IsVisible 模式对比

| 特性 | IsVisible | ContentControl + DataTemplate |
|------|-----------|-------------------------------|
| 状态保持 | 所有页面始终存在，状态自然保持 | 依赖页面缓存策略 |
| 内存占用 | 所有页面常驻内存 | 按需创建/销毁 |
| 页面切换 | 简单的布尔切换 | 类型匹配 + 模板选择 |
| 支持导航历史 | 不支持 | 可扩展 |
| 复杂度 | 低 | 中等 |
| 性能 | 切换瞬间完成 | 首次创建有开销 |
| 适用场景 | 页面少、状态重要 | 页面多、需要按需加载 |

### 26.2.6 Frame 导航

Frame 提供了类似浏览器的导航体验，支持前进/后退和 URI 路由。这是 Avalonia 中最完整的导航方案。

**什么是 Frame？**
Frame 是一个内容容器，维护一个导航栈。你可以 Navigate 到新页面，也可以 GoBack 返回上一页，就像浏览器的前进后退一样。

#### Frame 的所有属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Content` | object | 当前显示的内容 |
| `BackStack` | IReadOnlyList<PageStackEntry> | 后退栈 |
| `ForwardStack` | IReadOnlyList<PageStackEntry> | 前进栈 |
| `CanGoBack` | bool | 是否可以后退 |
| `CanGoForward` | bool | 是否可以前进 |
| `IsNavigationStackEnabled` | bool | 是否启用导航栈 |
| `CacheSize` | int | 页面缓存大小 |

#### Frame 的方法

| 方法 | 说明 |
|------|------|
| `Navigate(object)` | 导航到指定页面或数据 |
| `Navigate(Type)` | 导航到指定类型的页面 |
| `GoBack()` | 后退 |
| `GoForward()` | 前进 |
| `GoBack(NavigationTransitionInfo)` | 带过渡效果后退 |
| `ClearBackStack()` | 清空后退栈 |

#### Frame 的事件

| 事件 | 说明 |
|------|------|
| `Navigating` | 导航开始前触发（可取消） |
| `Navigated` | 导航完成后触发 |
| `NavigationFailed` | 导航失败时触发 |

#### 示例 1：基本 Frame 导航

```xml
<Grid ColumnDefinitions="200,*">
    <!-- 侧边导航 -->
    <StackPanel Grid.Column="0" Spacing="4" Margin="8">
        <Button Content="首页" Click="OnNavigateHome"/>
        <Button Content="设置" Click="OnNavigateSettings"/>
        <Button Content="关于" Click="OnNavigateAbout"/>
        <Separator/>
        <Button Content="后退" Click="OnGoBack"
                IsEnabled="{Binding #MainFrame.CanGoBack}"/>
        <Button Content="前进" Click="OnGoForward"
                IsEnabled="{Binding #MainFrame.CanGoForward}"/>
    </StackPanel>

    <!-- 内容区域 -->
    <Frame x:Name="MainFrame" Grid.Column="1"/>
</Grid>
```

```csharp
private void OnNavigateHome(object? sender, RoutedEventArgs e)
{
    MainFrame.Navigate(new HomePage());
}

private void OnNavigateSettings(object? sender, RoutedEventArgs e)
{
    MainFrame.Navigate(new SettingsPage());
}

private void OnGoBack(object? sender, RoutedEventArgs e)
{
    if (MainFrame.CanGoBack) MainFrame.GoBack();
}

private void OnGoForward(object? sender, RoutedEventArgs e)
{
    if (MainFrame.CanGoForward) MainFrame.GoForward();
}
```

#### 示例 2：使用 Navigate(Type) 和数据传递

```csharp
// 导航到页面并传递参数
MainFrame.Navigate(typeof(DetailPage), selectedItem);

// 在 DetailPage 中接收参数
public class DetailPage : UserControl
{
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is null && Tag is ItemModel item)
        {
            DataContext = new DetailViewModel(item);
        }
    }
}
```

#### 示例 3：监听导航事件

```csharp
MainFrame.Navigating += (s, e) =>
{
    // 可以取消导航
    if (HasUnsavedChanges)
    {
        e.Cancel = true;
        ShowSavePrompt();
    }
};

MainFrame.Navigated += (s, e) =>
{
    // 导航完成后的处理
    UpdateBreadcrumb(e.SourcePageType?.Name ?? "");
};
```

### 26.2.7 NavigationView（FluentAvaloniaUI）

Avalonia 目前没有内置的 NavigationView 控件（不像 WinUI 3 那样），但可以通过 FluentAvaloniaUI 社区包获得完整的 NavigationView 实现。

**什么是 NavigationView？**
NavigationView 是 WinUI 3 风格的导航控件，提供侧边栏菜单、内容区域、返回按钮、搜索框等完整的导航体验。它是现代 Windows 应用（如设置、文件管理器）的标准导航模式。

**安装：**
```xml
<PackageReference Include="FluentAvaloniaUI" Version="2.*" />
```

#### 示例 1：基本 NavigationView

```xml
<nav:NavigationView x:Name="NavView"
                    IsBackButtonVisible="Auto"
                    IsBackEnabled="{Binding CanGoBack}"
                    SelectedItem="{Binding SelectedNavItem, Mode=TwoWay}"
                    ItemInvoked="OnNavItemInvoked">
    <nav:NavigationView.MenuItems>
        <nav:NavigationViewItem Content="首页" Tag="home">
            <nav:NavigationViewItem.Icon>
                <PathIcon Data="{StaticResource HomeIcon}"/>
            </nav:NavigationViewItem.Icon>
        </nav:NavigationViewItem>
        <nav:NavigationViewItem Content="设置" Tag="settings">
            <nav:NavigationViewItem.Icon>
                <PathIcon Data="{StaticResource SettingsIcon}"/>
            </nav:NavigationViewItem.Icon>
        </nav:NavigationViewItem>
    </nav:NavigationView.MenuItems>

    <Frame x:Name="ContentFrame"/>
</nav:NavigationView>
```

```csharp
private void OnNavItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
{
    if (e.InvokedItemContainer is NavigationViewItem item)
    {
        var tag = item.Tag?.ToString();
        var pageType = tag switch
        {
            "home" => typeof(HomePage),
            "settings" => typeof(SettingsPage),
            _ => null
        };
        if (pageType != null)
            ContentFrame.Navigate(pageType);
    }
}
```

#### 示例 2：NavigationView 带 Footer 项

```xml
<nav:NavigationView>
    <nav:NavigationView.MenuItems>
        <nav:NavigationViewItem Content="首页"/>
        <nav:NavigationViewItem Content="文档"/>
    </nav:NavigationView.MenuItems>
    <nav:NavigationView.FooterMenuItems>
        <nav:NavigationViewItem Content="设置"/>
        <nav:NavigationViewItem Content="关于"/>
    </nav:NavigationView.FooterMenuItems>
    <Frame/>
</nav:NavigationView>
```

## 26.3 进阶用法

### 26.3.1 TabControl 自定义内容切换动画

```xml
<Window.Styles>
    <!-- 为 TabControl 内容添加切换动画 -->
    <Style Selector="TabControl /template/ ContentPresenter#PART_SelectedContentHost">
        <Setter Property="ContentTransitions">
            <Transitions>
                <CrossFade Duration="0:0:0.2"/>
            </Transitions>
        </Setter>
    </Style>
</Window.Styles>
```

### 26.3.2 嵌套导航

在实际应用中，经常需要多层嵌套的导航结构：

```xml
<!-- 主框架：顶层导航 -->
<TabControl TabStripPlacement="Left">
    <TabItem Header="管理">
        <!-- 子框架：内部导航 -->
        <ContentControl Content="{Binding ManagementPage}">
            <ContentControl.DataTemplates>
                <DataTemplate x:DataType="vm:ProviderListViewModel">
                    <pages:ProviderListPage/>
                </DataTemplate>
                <DataTemplate x:DataType="vm:ProviderDetailViewModel">
                    <pages:ProviderDetailPage/>
                </DataTemplate>
            </ContentControl.DataTemplates>
        </ContentControl>
    </TabItem>
    <TabItem Header="设置">
        <pages:SettingsPage/>
    </TabItem>
</TabControl>
```

### 26.3.3 路由守卫（Navigation Guard）

在导航前检查条件，防止用户意外离开：

```csharp
public class NavigationGuard
{
    private readonly Func<bool> _hasUnsavedChanges;

    public NavigationGuard(Func<bool> hasUnsavedChanges)
    {
        _hasUnsavedChanges = hasUnsavedChanges;
    }

    public async Task<bool> CanNavigateAsync()
    {
        if (!_hasUnsavedChanges()) return true;

        // 显示确认对话框
        var dialog = new ConfirmDialog
        {
            Title = "未保存的更改",
            Message = "您有未保存的更改，确定要离开吗？"
        };
        var result = await dialog.ShowAsync();
        return result == DialogResult.Yes;
    }
}
```

## 26.4 CodexSwitch 实战

### 26.4.1 CodexSwitch 的页面切换实现

CodexSwitch 使用 Grid 叠加 + IsVisible 绑定的方式，这是该应用场景下的最佳选择。以下是真实的实现代码：

```xml
<!-- MainWindow.axaml 中的真实代码 -->
<Grid Grid.Row="1">
    <pages:HomePage IsVisible="{Binding IsHomePageVisible}"/>
    <pages:ProvidersPage IsVisible="{Binding IsProvidersPageVisible}"/>
    <pages:CodexSessionsPage IsVisible="{Binding IsCodexSessionsPageVisible}"/>
    <pages:AddProviderPage IsVisible="{Binding IsAddProviderPageVisible}"/>
    <pages:UsagePage IsVisible="{Binding IsUsagePageVisible}"/>
    <pages:ModelsPage IsVisible="{Binding IsModelsPageVisible}"/>
    <pages:SettingsPage IsVisible="{Binding IsSettingsPageVisible}"/>
    <pages:ClaudePage IsVisible="{Binding IsClaudePageVisible}"/>
</Grid>
```

**为什么选择这种方式：**
- 所有页面共享同一个 MainWindowViewModel，通信简单
- 页面状态（滚动位置、表单输入）在切换后保持
- 实现简单，代码量少
- 性能好（页面只创建一次）
- 没有导航历史的需求

### 26.4.2 侧边栏导航实现

CodexSwitch 的侧边栏使用自定义组件实现：

```xml
<!-- 侧边栏菜单项 -->
<ui:CodexSidebarMenu>
    <ui:CodexSidebarMenuItem>
        <ui:CodexSidebarMenuButton Command="{Binding ShowHomeCommand}"
                                  IsActive="{Binding IsHomeNavSelected}">
            <ui:CodexSidebarMenuButton.Icon>
                <lucide:LucideIcon Kind="LayoutDashboard" Size="17" StrokeWidth="2"/>
            </ui:CodexSidebarMenuButton.Icon>
            <TextBlock Text="{i18n:Tr nav.home}"/>
        </ui:CodexSidebarMenuButton>
    </ui:CodexSidebarMenuItem>
    <ui:CodexSidebarMenuItem>
        <ui:CodexSidebarMenuButton Command="{Binding ShowProvidersCommand}"
                                  IsActive="{Binding IsProvidersPageVisible}">
            <ui:CodexSidebarMenuButton.Icon>
                <lucide:LucideIcon Kind="ServerCog" Size="17" StrokeWidth="2"/>
            </ui:CodexSidebarMenuButton.Icon>
            <TextBlock Text="{i18n:Tr nav.providers}"/>
        </ui:CodexSidebarMenuButton>
    </ui:CodexSidebarMenuItem>
</ui:CodexSidebarMenu>
```

这种模式的关键点：
1. 每个菜单按钮绑定一个 Command
2. IsActive 属性绑定到对应的页面可见性
3. 点击命令会切换 ViewModel 中的布尔属性，从而切换页面

## 26.5 举一反三

### 26.5.1 向导（Wizard）模式

使用 TabControl 或 ContentControl 实现多步向导：

```xml
<Grid RowDefinitions="*,Auto">
    <!-- 内容区域 -->
    <ContentControl Content="{Binding CurrentStep}">
        <ContentControl.DataTemplates>
            <DataTemplate x:DataType="vm:Step1ViewModel">
                <views:Step1View/>
            </DataTemplate>
            <DataTemplate x:DataType="vm:Step2ViewModel">
                <views:Step2View/>
            </DataTemplate>
            <DataTemplate x:DataType="vm:Step3ViewModel">
                <views:Step3View/>
            </DataTemplate>
        </ContentControl.DataTemplates>
    </ContentControl>

    <!-- 导航按钮 -->
    <StackPanel Grid.Row="1" Orientation="Horizontal"
                HorizontalAlignment="Right" Spacing="8" Margin="16">
        <Button Content="上一步"
                Command="{Binding PreviousStepCommand}"
                IsEnabled="{Binding CanGoPrevious}"/>
        <Button Content="{Binding IsLastStep, Converter={StaticResource BoolToStringConverter},
                 ConverterParameter='完成|下一步'}"
                Command="{Binding NextStepCommand}"
                IsEnabled="{Binding CanGoNext}"/>
    </StackPanel>
</Grid>
```

### 26.5.2 面包屑导航

```xml
<ItemsControl ItemsSource="{Binding BreadcrumbItems}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal" Spacing="4"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="4">
                <Button Content="{Binding Name}"
                        Command="{Binding NavigateCommand}"
                        Classes="link"/>
                <TextBlock Text="/" IsVisible="{Binding IsNotLast}"/>
            </StackPanel>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

## 26.6 最佳实践与设计模式

1. **简单应用用 IsVisible**：页面少、状态需要保持的场景，这是最简单的方案
2. **中型应用用 ContentControl**：需要 DataTemplate 选择的场景
3. **复杂应用用 Frame**：需要导航历史、URI 路由的场景
4. **设置页面用 TabControl**：选项卡式的设置界面
5. **可折叠内容用 Expander**：高级选项、分组内容
6. **为 TabControl 设置 SelectedItem 双向绑定**：避免选中状态丢失
7. **Expander 中避免放置大量内容**：使用虚拟化或延迟加载

## Deep Dive

### TabControl 的内部结构

TabControl 的 ControlTemplate 包含以下关键部分：
- `PART_Header`: 选项卡头部区域（TabStrip）
- `PART_SelectedContentHost`: 内容显示区域

你可以通过自定义 ControlTemplate 完全改变 TabControl 的外观：

```xml
<Style Selector="TabControl">
    <Setter Property="Template">
        <ControlTemplate>
            <Grid ColumnDefinitions="Auto,*">
                <!-- 左侧选项卡列表 -->
                <ItemsPresenter Name="PART_HeaderPresenter"
                                Grid.Column="0"
                                ItemsPanel="{TemplateBinding ItemsPanel}"/>
                <!-- 右侧内容区域 -->
                <ContentPresenter Name="PART_SelectedContentHost"
                                  Grid.Column="1"
                                  Content="{TemplateBinding SelectedContent}"
                                  ContentTemplate="{TemplateBinding SelectedContentTemplate}"/>
            </Grid>
        </ControlTemplate>
    </Setter>
</Style>
```

### Expander 的动画原理

Expander 的展开/收起动画通过 ClipToBounds 和 Height 动画实现。当 IsExpanded 变为 true 时，内容区域的高度从 0 动画到实际高度。

## Cross References

- **[第 4 章：布局系统](04-layout-system.md)** -- Grid 叠加的页面切换模式
- **[第 6 章：MVVM 模式](06-mvvm-pattern.md)** -- ViewModel 驱动的页面导航
- **[第 8 章：DataTemplate](08-data-templates.md)** -- DataTemplate 选择机制
- **[第 12 章：多窗口](12-multi-window-tray.md)** -- 独立窗口 vs 页面导航
- **[第 10 章：动画](10-animation-transitions.md)** -- 页面切换动画

## Common Pitfalls

### 1. TabControl 的 SelectedItem 绑定失败

**问题**：SelectedItem 绑定到 ViewModel 属性时不生效。

```xml
<!-- 错误：缺少双向绑定 -->
<TabControl SelectedItem="{Binding SelectedTab}"/>

<!-- 正确：使用 TwoWay 绑定 -->
<TabControl SelectedItem="{Binding SelectedTab, Mode=TwoWay}"/>
```

### 2. Frame 导航后忘记处理后退

**问题**：使用 Frame 导航但没有处理 BackRequested 事件，用户按后退键无反应。

```csharp
// 解决：在 Window 中处理后退
this.GetObservable(Window.BackRequestedEvent).Subscribe(e =>
{
    if (MainFrame.CanGoBack)
    {
        MainFrame.GoBack();
        e.Handled = true;
    }
});
```

### 3. Expander 中放置大量内容导致性能问题

**问题**：Expander 展开时一次性渲染所有内容，如果内容很复杂会导致卡顿。

```xml
<!-- 解决：使用虚拟化或延迟加载 -->
<Expander IsExpanded="{Binding IsExpanded}">
    <ScrollViewer MaxHeight="400">
        <ItemsControl ItemsSource="{Binding LargeCollection}"
                      VirtualizingStackPanel.IsVirtualizing="True">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <VirtualizingStackPanel/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
        </ItemsControl>
    </ScrollViewer>
</Expander>
```

### 4. ContentControl 切换时丢失状态

**问题**：每次切换页面都创建新的 ViewModel，导致输入状态丢失。

```csharp
// 解决：使用页面缓存
private readonly Dictionary<Type, ObservableObject> _pageCache = new();

private ObservableObject GetOrCreatePage<T>() where T : ObservableObject, new()
{
    if (!_pageCache.TryGetValue(typeof(T), out var page))
    {
        page = new T();
        _pageCache[typeof(T)] = page;
    }
    return page;
}
```

### 5. TabStripPlacement="Left" 时内容区域太窄

**问题**：垂直选项卡占据了大量水平空间。

```xml
<!-- 解决：限制选项卡宽度 -->
<TabControl TabStripPlacement="Left">
    <TabControl.Styles>
        <Style Selector="TabItem">
            <Setter Property="MinWidth" Value="120"/>
            <Setter Property="MaxWidth" Value="200"/>
        </Style>
    </TabControl.Styles>
    <!-- ... -->
</TabControl>
```

### 6. Expander 的 Header 无法点击

**问题**：自定义 Header 中的控件拦截了点击事件。

```xml
<!-- 解决：确保 Header 中的控件不会拦截点击 -->
<Expander>
    <Expander.Header>
        <Border Background="Transparent" Padding="8">
            <TextBlock Text="标题"/>
        </Border>
    </Expander.Header>
</Expander>
```

### 7. NavigationView 的后退按钮不显示

**问题**：IsBackButtonVisible 设置错误。

```xml
<!-- 错误 -->
<nav:NavigationView IsBackButtonVisible="Collapsed"/>

<!-- 正确：使用 Auto 让它自动根据导航栈显示 -->
<nav:NavigationView IsBackButtonVisible="Auto"/>
```

### 8. Frame 导航时页面闪烁

**问题**：导航到新页面时出现短暂的空白。

```xml
<!-- 解决：使用过渡动画 -->
<Frame x:Name="MainFrame">
    <Frame.ContentTransitions>
        <TransitionCollection>
            <NavigationThemeTransition/>
        </TransitionCollection>
    </Frame.ContentTransitions>
</Frame>
```

### 9. IsVisible 切换时页面闪烁

**问题**：快速切换页面时出现短暂的闪烁。

```xml
<!-- 解决：确保 Grid 中所有页面都设置了合适的背景色 -->
<Grid>
    <pages:HomePage IsVisible="{Binding IsHomePageVisible}"
                    Background="{StaticResource PageBackground}"/>
    <pages:SettingsPage IsVisible="{Binding IsSettingsPageVisible}"
                       Background="{StaticResource PageBackground}"/>
</Grid>
```

### 10. 多层嵌套导航导致内存泄漏

**问题**：Frame 导航创建的页面没有被正确释放。

```csharp
// 解决：设置合适的 CacheSize
MainFrame.CacheSize = 5; // 只缓存最近 5 个页面
```

## Try It Yourself

### 练习 1：创建带图标的 TabControl

创建一个 TabControl，每个选项卡头部都有图标和文字。

**提示**：在 TabItem.Header 中使用 StackPanel 包含 PathIcon 和 TextBlock。

### 练习 2：实现手风琴面板

创建一个手风琴面板，一次只能展开一个 Expander。

**提示**：在每个 Expander 的 Expanded 事件中收起其他 Expander。

### 练习 3：实现向导导航

创建一个 3 步向导，使用 ContentControl 切换每一步的内容，并在底部显示"上一步/下一步"按钮。

**提示**：使用 CurrentStep 属性和 DataTemplate 选择。

### 练习 4：自定义 TabControl 样式

创建一个现代风格的 TabControl，选项卡在左侧，选中时有指示器动画。

**提示**：自定义 ControlTemplate，使用 Border 和 Transitions。

### 练习 5：实现 Frame 导航

创建一个带有侧边栏和内容区域的应用，使用 Frame 实现页面导航，支持前进/后退。

**提示**：Frame.Navigate() 和 GoBack()/GoForward()。

### 练习 6：创建面包屑导航

实现一个面包屑导航组件，显示当前页面的路径，并允许点击返回到任意层级。

**提示**：使用 ItemsControl 和 StackPanel(orientation=horizontal)。

### 练习 7：实现路由守卫

创建一个导航守卫，在用户有未保存更改时阻止导航。

**提示**：在 Frame.Navigating 事件中检查条件并设置 e.Cancel = true。

### 练习 8：CodexSwitch 风格的侧边栏

模仿 CodexSwitch 的侧边栏实现，创建一个带图标、高亮状态和版本信息的侧边栏导航。

**提示**：使用 Grid + IsVisible 模式，侧边栏使用 StackPanel 组织菜单项。
