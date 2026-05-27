# 3. AXAML 基础语法

AXAML (Avalonia XAML) 是 Avalonia 的标记语言，与 WPF 的 XAML 高度相似但有一些关键差异。本章将系统性地讲解 AXAML 的语法、编译机制和实战用法。

## 3.1 命名空间声明

每个 AXAML 文件的根元素必须声明命名空间。CodexSwitch 的 `MainWindow.axaml` 展示了典型的命名空间使用模式：

```xml
<Window xmlns="https://github.com/avaloniaui"              <!-- Avalonia 默认命名空间 -->
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"  <!-- XAML 通用命名空间 -->
        xmlns:vm="using:CodexSwitch.ViewModels"             <!-- C# using 语法 -->
        xmlns:shell="using:CodexSwitch.Views.Shell"         <!-- 页面命名空间 -->
        xmlns:pages="using:CodexSwitch.Views.Pages"         <!-- 页面命名空间 -->
        xmlns:dialogs="using:CodexSwitch.Views.Dialogs"     <!-- 对话框命名空间 -->
        xmlns:ui="using:CodexSwitchUI.Controls"             <!-- 引用组件库 -->
        xmlns:text="using:CodexSwitchUI.Primitives"         <!-- 基础文本控件 -->
        xmlns:i18n="using:CodexSwitch.I18n"                 <!-- 自定义 MarkupExtension -->
        xmlns:lucide="using:Lucide.Avalonia"                <!-- 第三方图标库 -->
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"  <!-- 设计器支持 -->
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d">
```

### Avalonia vs WPF 命名空间差异

| 特性 | WPF | Avalonia |
|------|-----|----------|
| 默认命名空间 | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` | `https://github.com/avaloniaui` |
| 引用类型 | `clr-namespace:MyApp.ViewModels` | `using:MyApp.ViewModels`（两种都支持） |
| 设计器命名空间 | `d:` | 相同 |

### 命名空间解析机制

XAML 命名空间解析遵循以下规则：

1. **默认命名空间**（无前缀）：映射到 Avalonia 控件库（Button, TextBlock, Grid 等）
2. **x: 命名空间**：XAML 语言特性（x:Name, x:DataType, x:Class, x:Key 等）
3. **自定义前缀**：映射到特定的 .NET 命名空间，用于引用项目中的类型或第三方库

```xml
<!-- 命名空间解析示例 -->
<Window xmlns="https://github.com/avaloniaui"           <!-- Avalonia 控件 -->
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"  <!-- XAML 语言 -->
        xmlns:vm="using:MyApp.ViewModels"               <!-- 自定义命名空间 -->
        xmlns:local="using:MyApp.Controls">             <!-- 本地控件 -->

    <!-- 使用默认命名空间的控件 -->
    <StackPanel>
        <!-- 使用 x: 命名空间的特性 -->
        <TextBlock x:Name="title" x:DataType="vm:MainViewModel"/>

        <!-- 使用自定义命名空间的控件 -->
        <vm:CustomControl/>
        <local:MyButton/>
    </StackPanel>
</Window>
```

## 3.2 x: 指令详解

### x:Class

将 AXAML 文件关联到一个 C# 代码后置类。编译器会生成一个 partial class，将 AXAML 中的元素映射为字段。

```xml
<!-- MainWindow.axaml -->
<Window x:Class="CodexSwitch.Views.MainWindow"
        xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
</Window>
```

```csharp
// MainWindow.axaml.cs — 编译器自动合并这两部分
namespace CodexSwitch.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();  // 加载 AXAML 定义的视觉树
    }
}
```

### x:Name

在代码后置中创建一个命名字段，同时将元素注册到 XAML 名称作用域中。

```xml
<!-- 用于代码后置访问 -->
<Border x:Name="PART_DialogRoot">
    <ContentPresenter/>
</Border>
```

```csharp
// 在代码后置中可以直接访问
private void OnLoaded(object? sender, RoutedEventArgs e)
{
    PART_DialogRoot.Background = Brushes.Red;
}
```

### x:DataType

声明绑定上下文的类型，启用编译绑定（Compiled Bindings）。这是 Avalonia 相比 WPF 的重要改进。

```xml
<!-- 窗口级 x:DataType — 影响整个视觉树 -->
<Window x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding CurrentPage}"/>  <!-- 编译时验证属性存在 -->
</Window>

<!-- DataTemplate 级 x:DataType — 影响模板内部 -->
<DataTemplate x:DataType="vm:ProviderListItem">
    <TextBlock Text="{Binding DisplayName}"/>  <!-- 编译时验证属性存在 -->
</DataTemplate>
```

CodexSwitch 中的实际用法（来自 `ProvidersPage.axaml`）：

```xml
<UserControl x:DataType="vm:MainWindowViewModel">
    <ItemsControl ItemsSource="{Binding SelectedProviderRows}">
        <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="vm:ProviderListItem">
                <Grid Classes="provider-list-row">
                    <TextBlock Text="{Binding DisplayName}"/>
                    <TextBlock Text="{Binding BaseUrl}"/>
                </Grid>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</UserControl>
```

### x:Key

为资源字典中的条目指定键名，用于通过 `{StaticResource}` 或 `{DynamicResource}` 引用。

```xml
<!-- 资源定义 -->
<Window.Resources>
    <SolidColorBrush x:Key="CsPrimaryBrush" Color="#3B82F6"/>
    <x:Double x:Key="DefaultFontSize">14</x:Double>
    <CornerRadius x:Key="CsRadiusMd">8</CornerRadius>
</Window.Resources>

<!-- 引用资源 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>
```

### x:Static

在 AXAML 中引用 C# 静态成员。

```xml
<!-- 引用静态属性 -->
<TextBlock Text="{x:Static local:App.Version}"/>

<!-- 引用静态字段 -->
<SolidColorBrush Color="{x:Static SystemColors.HighlightColor}"/>
```

## 3.3 Markup Extensions

Markup Extensions 是 AXAML 中花括号 `{}` 包裹的特殊语法，用于在赋值时执行动态逻辑。

### Binding

```xml
<!-- 基本绑定 -->
<TextBlock Text="{Binding CurrentPage}"/>

<!-- 带路径的绑定 -->
<TextBlock Text="{Binding Path=SelectedProvider.Name}"/>

<!-- 带模式的绑定 -->
<TextBox Text="{Binding SearchText, Mode=TwoWay}"/>

<!-- 带 StringFormat -->
<TextBlock Text="{Binding TotalCost, StringFormat='{}${0:N2}'}"/>
```

### StaticResource / DynamicResource

```xml
<!-- StaticResource — 编译时解析，不可变 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- DynamicResource — 运行时解析，可响应资源变更 -->
<Button Background="{DynamicResource CsPrimaryBrush}"/>
```

### x:Static

```xml
<TextBlock Text="{x:Static local:App.Name}"/>
```

### TemplateBinding

仅在 ControlTemplate 内部使用，将模板属性绑定到控件的 StyledProperty。

```xml
<ControlTemplate>
    <Border Background="{TemplateBinding Background}"
            CornerRadius="{TemplateBinding CornerRadius}"
            Padding="{TemplateBinding Padding}">
        <ContentPresenter/>
    </Border>
</ControlTemplate>
```

### 自定义 Markup Extension（CodexSwitch 实例）

CodexSwitch 的 `i18n:Tr` 是一个自定义 MarkupExtension，用于国际化文本：

```xml
<!-- 使用自定义 MarkupExtension 加载本地化文本 -->
<Window Title="{i18n:Tr app.name}">
    <TextBlock Text="{i18n:Tr nav.home}"/>
    <TextBlock Text="{i18n:Tr providers.title}"/>
    <TextBlock Text="{i18n:Tr providers.description}"/>
</Window>
```

```csharp
// TrExtension 的简化实现
public class TrExtension : MarkupExtension
{
    public string Key { get; }

    public TrExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // 从 I18nService 查找本地化文本
        return I18nService.Instance.Translate(Key);
    }
}
```

## 3.4 事件处理器

AXAML 支持两种方式连接事件：

### AXAML 直接绑定（Code-Behind 事件）

```xml
<!-- 在 ProvidersPage.axaml 中使用的事件绑定 -->
<Border PointerPressed="ProviderDragHandle_OnPointerPressed"
        PointerMoved="ProviderDragHandle_OnPointerMoved"
        PointerReleased="ProviderDragHandle_OnPointerReleased"
        PointerCaptureLost="ProviderDragHandle_OnPointerCaptureLost">
    <lucide:LucideIcon Kind="GripVertical" Size="18" StrokeWidth="2"/>
</Border>
```

```csharp
// ProvidersPage.axaml.cs 中的事件处理
private void ProviderDragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    // 开始拖拽逻辑
}
```

### Command 绑定（MVVM 事件）

```xml
<!-- 通过 Command 绑定到 ViewModel 中的 ICommand -->
<ui:CodexSidebarMenuButton Command="{Binding ShowProvidersCommand}">
    <TextBlock Text="{i18n:Tr nav.providers}"/>
</ui:CodexSidebarMenuButton>

<!-- 带参数的 Command -->
<ui:CodexProviderCard Command="{Binding SelectCommand}"
                       CommandParameter="{Binding}">
</ui:CodexProviderCard>
```

## 3.5 附加属性

附加属性是在 AXAML 中以 `OwnerType.PropertyName` 语法使用的属性，通常用于布局或容器控制子元素行为。

```xml
<!-- Grid 附加属性 -->
<Grid ColumnDefinitions="220,*" RowDefinitions="Auto,*">
    <Sidebar Grid.Row="0"/>
    <Content Grid.Row="1" Grid.Column="1"/>
</Grid>

<!-- ToolTip 附加属性 -->
<Button ToolTip.Tip="{i18n:Tr providers.edit}"/>

<!-- DockPanel 附加属性 -->
<DockPanel>
    <TextBlock DockPanel.Dock="Top" Text="Header"/>
    <TextBlock DockPanel.Dock="Left" Text="Sidebar"/>
    <ContentPresenter/>
</DockPanel>
```

## 3.6 设计时支持

```xml
<!-- d:DesignWidth 和 d:DesignHeight 设置设计时画布尺寸 -->
<Window d:DesignWidth="1180"
        d:DesignHeight="760"
        mc:Ignorable="d">

    <!-- Design.DataContext 设置设计时数据上下文 -->
    <Design.DataContext>
        <vm:MainWindowViewModel/>
    </Design.DataContext>
</Window>
```

`Design.DataContext` 使得 IDE 的 AXAML 设计器能够解析绑定路径，提供 IntelliSense 和设计时预览，但不会影响运行时行为。

## 3.7 AXAML 中的集合和内容属性

### 内容属性

每个控件有一个默认的内容属性（Content Property），可以直接在标签内部填充内容：

```xml
<!-- ContentControl 的 Content 属性是内容属性 -->
<Border>
    <!-- 这里直接写的内容就是 Border.Child -->
    <StackPanel>
        <TextBlock Text="Hello"/>
    </StackPanel>
</Border>

<!-- 等价于显式设置 -->
<Border>
    <Border.Child>
        <StackPanel>
            <TextBlock Text="Hello"/>
        </StackPanel>
    </Border.Child>
</Border>
```

### 集合属性

```xml
<!-- Grid 的 RowDefinitions 和 ColumnDefinitions 是集合 -->
<Grid RowDefinitions="Auto,*,Auto"
      ColumnDefinitions="220,*">
</Grid>

<!-- 等价于显式设置 -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="220"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
</Grid>
```

---

## Deep Dive

### AXAML 编译过程

AXAML 不是运行时解析的 XML，而是在编译时被转换为 IL 代码。理解这个过程对调试和优化至关重要。

```
.axaml 文件
    ↓
MSBuild 任务 (AvaloniaXamlIlTask)
    ↓
XamlX 解析器 (解析 XML → XAML AST)
    ↓
类型检查 (验证属性名、类型、绑定路径)
    ↓
代码生成 (生成 C# IL 代码)
    ↓
编译到程序集 (.dll)
```

#### 编译产物

当你编译一个包含 `x:Class` 的 AXAML 文件时，编译器会生成：

1. **InitializeComponent 方法**：加载 AXAML 定义的视觉树，创建控件实例并设置属性
2. **命名元素字段**：为每个 `x:Name` 元素生成对应字段
3. **绑定代码**：如果使用编译绑定，生成强类型的绑定代码

```csharp
// 编译器为 MainWindow.axaml 生成的伪代码
public partial class MainWindow : Window
{
    // x:Name="PART_Root" 生成的字段
    internal Border PART_Root;

    private void InitializeComponent()
    {
        // 加载 AXAML 并构建视觉树
        AvaloniaXamlLoader.Load(this);
    }
}
```

#### 编译绑定 vs 运行时绑定

当设置了 `x:DataType` 时，绑定表达式会在编译时验证：

```xml
<!-- 编译绑定：编译时验证 CurrentPage 属性存在 -->
<Window x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding CurrentPage}"/>  <!-- OK -->
    <TextBlock Text="{Binding NonExistent}"/>  <!-- 编译错误 -->
</Window>

<!-- 运行时绑定：不验证，运行时通过反射 -->
<Window>
    <TextBlock Text="{Binding CurrentPage}"/>  <!-- 可能运行时失败 -->
</Window>
```

CodexSwitch 项目启用了全局编译绑定（在 `.csproj` 中）：

```xml
<PropertyGroup>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

这意味着每个 AXAML 文件都必须设置 `x:DataType`，否则绑定将无法工作。

#### AXAML 与 BAML 的区别

| 特性 | WPF BAML | Avalonia AXAML |
|------|----------|----------------|
| 格式 | 二进制压缩 XAML | 源生成 IL 代码 |
| 加载方式 | 运行时从程序集资源加载 | 编译为方法，直接执行 |
| 类型检查 | 运行时 | 编译时（带 x:DataType） |
| 性能 | 需要解析和反射 | 与手写 C# 相当 |
| Native AOT | 不支持 | 完全支持 |

---

## Cross References

- **[第 1 章：Avalonia 框架概览](01-avalonia-overview.md)** — 了解 Avalonia 的整体架构和设计哲学
- **[第 2 章：项目结构](02-project-structure.md)** — 了解 .csproj 配置和 MSBuild 任务如何驱动 AXAML 编译
- **[第 5 章：数据绑定](05-data-binding.md)** — 深入学习 Binding 语法和 x:DataType 的编译绑定机制
- **[第 7 章：样式与主题](07-styling-theming.md)** — 了解样式系统中的资源引用和 Selector 语法
- **[第 9 章：自定义控件](09-custom-controls.md)** — 学习 ControlTemplate 中的 x:Name 和模板部件模式

---

## Common Pitfalls

### 1. 混淆 WPF 和 Avalonia 的默认命名空间

**问题**：WPF 开发者习惯性使用 WPF 的命名空间 URI。

```xml
<!-- 错误：WPF 命名空间 -->
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <StackPanel/>  <!-- 编译错误：找不到 StackPanel -->
</Window>

<!-- 正确：Avalonia 命名空间 -->
<Window xmlns="https://github.com/avaloniaui">
    <StackPanel/>  <!-- OK -->
</Window>
```

### 2. 忘记 x:DataType 导致绑定静默失败

**问题**：CodexSwitch 启用了全局编译绑定，没有 `x:DataType` 的绑定会直接编译失败或退化为运行时绑定。

```xml
<!-- 错误：缺少 x:DataType -->
<UserControl>
    <TextBlock Text="{Binding DisplayName}"/>  <!-- 编译警告或失败 -->
</UserControl>

<!-- 正确：设置 x:DataType -->
<UserControl x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding DisplayName}"/>  <!-- 编译绑定，类型安全 -->
</UserControl>
```

### 3. 在 DataTemplate 中忘记设置 x:DataType

**问题**：ItemsControl 的 ItemTemplate 需要单独设置 `x:DataType`。

```xml
<!-- 错误：DataTemplate 没有 x:DataType -->
<ItemsControl ItemsSource="{Binding Providers}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}"/>  <!-- 无法编译验证 -->
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<!-- 正确：DataTemplate 设置 x:DataType -->
<ItemsControl ItemsSource="{Binding SelectedProviderRows}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <TextBlock Text="{Binding DisplayName}"/>  <!-- 编译验证 -->
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 4. x:Name 与 x:Key 的混淆

**问题**：`x:Name` 用于命名控件（注册到名称作用域），`x:Key` 用于资源字典的键。

```xml
<!-- 错误：对资源使用 x:Name -->
<SolidColorBrush x:Name="PrimaryBrush" Color="#3B82F6"/>  <!-- 不会注册到资源 -->
<Button Background="{StaticResource PrimaryBrush}"/>  <!-- 运行时找不到 -->

<!-- 正确：资源用 x:Key -->
<SolidColorBrush x:Key="PrimaryBrush" Color="#3B82F6"/>  <!-- 注册为资源 -->
<Button Background="{StaticResource PrimaryBrush}"/>  <!-- OK -->
```

### 5. StaticResource 和 DynamicResource 选择错误

**问题**：`StaticResource` 在编译时解析，如果资源定义在引用之后，会编译失败。

```xml
<!-- 错误：资源定义在使用之后 -->
<Button Background="{StaticResource MyBrush}"/>  <!-- 编译失败：找不到 MyBrush -->
<SolidColorBrush x:Key="MyBrush" Color="Red"/>

<!-- 正确：资源定义在使用之前，或使用 DynamicResource -->
<SolidColorBrush x:Key="MyBrush" Color="Red"/>
<Button Background="{StaticResource MyBrush}"/>  <!-- OK -->

<!-- 或者使用 DynamicResource（运行时解析，不要求顺序） -->
<Button Background="{DynamicResource MyBrush}"/>  <!-- OK，但性能稍差 -->
```

---

## Try It Yourself

### 练习 1：分析 CodexSwitch 的命名空间使用

打开 `MainWindow.axaml`，逐个识别每个 `xmlns` 前缀的作用：

1. `xmlns="https://github.com/avaloniaui"` — 默认的 Avalonia 控件
2. `xmlns:vm="using:CodexSwitch.ViewModels"` — ViewModel 类型
3. `xmlns:ui="using:CodexSwitchUI.Controls"` — 自定义 UI 组件库
4. `xmlns:i18n="using:CodexSwitch.I18n"` — 国际化 MarkupExtension
5. `xmlns:lucide="using:Lucide.Avalonia"` — 图标库

尝试将其中一个前缀改为不同的名称（如 `vm` 改为 `viewmodel`），然后在所有使用处更新，运行项目确认正常。

### 练习 2：创建自定义 MarkupExtension

创建一个 `FormatExtension`，将数字格式化为货币：

```csharp
public class FormatExtension : MarkupExtension
{
    public object? Value { get; set; }
    public string? StringFormat { get; set; }

    public override object? ProvideValue(IServiceProvider serviceProvider)
    {
        if (Value != null && StringFormat != null)
            return string.Format(StringFormat, Value);
        return Value?.ToString();
    }
}
```

在 AXAML 中使用：

```xml
<TextBlock Text="{local:Format Value=99.95, StringFormat='{}${0:N2}'}"/>
```

### 练习 3：探索 x:DataType 的效果

1. 在 `ProvidersPage.axaml` 中临时删除 `x:DataType="vm:MainWindowViewModel"`
2. 编译项目，观察编译器输出的警告
3. 尝试在绑定中使用一个不存在的属性名，观察编译错误
4. 恢复 `x:DataType`，再次测试相同的错误属性名

### 练习 4：练习附加属性

在 `MainWindow.axaml` 中，识别所有附加属性的使用：

```xml
<!-- Grid.Row 和 Grid.Column 是附加属性 -->
<shell:TopBar/>
<Grid Grid.Row="1">
    <pages:HomePage IsVisible="{Binding IsHomePageVisible}"/>
</Grid>

<!-- ToolTip.Tip 是附加属性 -->
<Button ToolTip.Tip="{i18n:Tr providers.edit}"/>
```

尝试修改 `TopBar` 的 `Grid.Row` 值，观察它在布局中的位置变化。

### 练习 5：跟踪 AXAML 编译产物

1. 编译 CodexSwitch 项目：
   ```bash
   dotnet build CodexSwitch/CodexSwitch.csproj -c Debug
   ```

2. 在 `obj/Debug/net10.0/` 目录下查找编译器生成的文件：
   ```bash
   find CodexSwitch/obj -name "*.g.cs" | head -20
   ```

3. 查看生成的 `MainWindow.g.cs`，理解 `InitializeComponent` 如何加载 AXAML
