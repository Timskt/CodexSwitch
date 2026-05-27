# 3. AXAML 基础语法

> **写给零基础的你**：AXAML 是你和 Avalonia "对话"的方式之一。如果说 C# 是用文字告诉计算机做什么，那 AXAML 就是用"填表格"的方式来描述界面长什么样。你不需要记住所有语法，用多了自然就熟了。

## 3.1 概述

AXAML (Avalonia XAML) 是 Avalonia 的标记语言，与 WPF 的 XAML 高度相似但有一些关键差异。本章将系统性地讲解 AXAML 的语法、编译机制和实战用法，包括所有 x: 指令、内置标记扩展、事件处理、ContentControl 与 ContentPresenter 的关系等核心知识点。

学完本章后，你将能够：
- 熟练使用所有 x: 指令（x:Class, x:Name, x:Key, x:DataType, x:Static, x:Null, x:Type）
- 掌握所有内置标记扩展（Binding, StaticResource, DynamicResource, TemplateBinding, RelativeSource）
- 理解事件处理的两种方式（+= 语法、Command 绑定）
- 理解 ContentControl 和 ContentPresenter 的关系
- 理解 ItemsControl 和 ItemsPresenter 的关系
- 理解 AXAML 编译流程和错误处理

## 3.2 核心概念

### 3.2.1 命名空间声明

每个 AXAML 文件的根元素必须声明命名空间。CodexSwitch 的 `MainWindow.axaml` 展示了典型的命名空间使用模式：

> **小白提示**：命名空间（namespace）就像通讯录里的分组。比如"家人"组里有爸爸、妈妈，"同事"组里有张三、李四。`xmlns:vm="using:CodexSwitch.ViewModels"` 这句话的意思是："我给 CodexSwitch.ViewModels 这个分组起了个简短的代号叫 vm，以后用 vm:XXX 就能引用里面的东西了。"

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

**命名空间解析规则：**

1. **默认命名空间**（无前缀）：映射到 Avalonia 控件库（Button, TextBlock, Grid 等）
2. **x: 命名空间**：XAML 语言特性（x:Name, x:DataType, x:Class, x:Key 等）
3. **自定义前缀**：映射到特定的 .NET 命名空间

**Avalonia vs WPF 命名空间差异：**

| 特性 | WPF | Avalonia |
|------|-----|----------|
| 默认命名空间 | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` | `https://github.com/avaloniaui` |
| 引用类型 | `clr-namespace:MyApp.ViewModels` | `using:MyApp.ViewModels` |
| 设计器命名空间 | `d:` | 相同 |

### 3.2.2 x: 指令详解

#### x:Class

> **小白提示**：`x:Class` 就像"门牌号"。它告诉编译器："这个 AXAML 文件和那个 C# 文件是同一间房间的两个部分"。AXAML 负责"装修"（界面），C# 负责"逻辑"（代码），`x:Class` 把它们连起来。

将 AXAML 文件关联到一个 C# 代码后置类。编译器会生成一个 partial class，将 AXAML 中的元素映射为字段。

```xml
<!-- MainWindow.axaml — 界面部分（装修） -->
<Window x:Class="CodexSwitch.Views.MainWindow"  <!-- 门牌号：关联到 MainWindow.cs -->
        xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
</Window>
```

```csharp
// MainWindow.axaml.cs — 逻辑部分（代码）
// 编译器自动合并 AXAML 和 C# 两部分
namespace CodexSwitch.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();  // 加载 AXAML 定义的视觉树（把装修好的房间打开）
    }
}
```

**x:Class 的规则：**
- 只能在根元素上使用
- 必须与代码后置类的完全限定名匹配
- 类必须声明为 `partial`
- 只能出现一次

#### x:Name

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

**x:Name 的规则：**
- 在同一 XAML 名称作用域中必须唯一
- 生成一个同名的字段（internal 可见性）
- 同时注册到 XAML 名称作用域（用于 `FindControl` 和 `#` 引用）

**x:Name vs x:Key 的区别：**

| 特性 | x:Name | x:Key |
|------|--------|-------|
| 用途 | 命名控件 | 资源字典的键 |
| 生成字段 | 是 | 否 |
| 注册到名称作用域 | 是 | 否 |
| 用于资源引用 | 否 | 是（StaticResource/DynamicResource） |
| 用于绑定引用 | 是（#elementName） | 否 |

#### x:DataType

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

**x:DataType 的继承规则：**
- 子控件继承父控件的 x:DataType
- DataTemplate 中设置 x:DataType 会覆盖父控件的类型
- 启用全局编译绑定后，每个 AXAML 文件都必须设置 x:DataType

#### x:Key

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

**x:Key 的使用场景：**
- 颜色、画刷资源
- 字体、字号资源
- 尺寸、间距资源
- 样式资源
- 数据模板资源

#### x:Static

在 AXAML 中引用 C# 静态成员。

```xml
<!-- 引用静态属性 -->
<TextBlock Text="{x:Static local:App.Version}"/>

<!-- 引用静态字段 -->
<SolidColorBrush Color="{x:Static SystemColors.HighlightColor}"/>

<!-- 引用常量 -->
<TextBlock Text="{x:Static local:App.AppName}"/>
```

**x:Static 的限制：**
- 只能引用静态成员
- 不能引用索引器
- 不能引用方法（只能属性和字段）

#### x:Null

表示空值，用于将属性设置为 null。

```xml
<!-- 将 Content 设置为 null -->
<ContentControl Content="{x:Null}"/>

<!-- 将 Background 设置为 null（透明） -->
<Border Background="{x:Null}"/>
```

#### x:Type

获取类型的 Type 对象，类似于 C# 中的 `typeof`。

```xml
<!-- 用于 DataTemplate 的 DataType -->
<DataTemplate x:DataType="{x:Type vm:MainWindowViewModel}">
    <TextBlock Text="{Binding Name}"/>
</DataTemplate>

<!-- 用于 Style 的 Selector -->
<Style Selector="Button">
    <!-- ... -->
</Style>
```

### 3.2.3 内置标记扩展

#### Binding

```xml
<!-- 基本绑定 -->
<TextBlock Text="{Binding CurrentPage}"/>

<!-- 带路径的绑定 -->
<TextBlock Text="{Binding Path=SelectedProvider.Name}"/>

<!-- 带模式的绑定 -->
<TextBox Text="{Binding SearchText, Mode=TwoWay}"/>

<!-- 带 StringFormat -->
<TextBlock Text="{Binding TotalCost, StringFormat='{}${0:N2}'}"/>

<!-- 带 FallbackValue -->
<TextBlock Text="{Binding UserName, FallbackValue='Guest'}"/>

<!-- 带 TargetNullValue -->
<TextBlock Text="{Binding Description, TargetNullValue='No description'}"/>

<!-- 带 Converter -->
<TextBlock Text="{Binding IsVisible, Converter={StaticResource BoolToStringConverter}}"/>

<!-- 带 Delay -->
<TextBox Text="{Binding SearchText, Delay=300}"/>
```

#### StaticResource / DynamicResource

```xml
<!-- StaticResource — 编译时解析，不可变 -->
<Button Background="{StaticResource CsPrimaryBrush}"/>

<!-- DynamicResource — 运行时解析，可响应资源变更 -->
<Button Background="{DynamicResource CsPrimaryBrush}"/>
```

**StaticResource vs DynamicResource：**

| 特性 | StaticResource | DynamicResource |
|------|---------------|-----------------|
| 解析时机 | 编译时 | 运行时 |
| 性能 | 快 | 慢 |
| 资源更新 | 不响应 | 自动响应 |
| 定义顺序 | 必须在引用之前 | 无要求 |
| 适用场景 | 静态资源 | 主题切换、运行时变化 |

#### TemplateBinding

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

**TemplateBinding vs Binding：**

| 特性 | TemplateBinding | Binding with TemplatedParent |
|------|----------------|------------------------------|
| 方向 | 单向（只读） | 可配置 |
| 转换器 | 不支持 | 支持 |
| 性能 | 更好 | 稍差 |
| 使用场景 | 简单属性传递 | 需要转换或双向绑定 |

#### RelativeSource

```xml
<!-- 绑定到自身 -->
<TextBlock Text="{Binding $self.Name}"/>

<!-- 在 ControlTemplate 中绑定到模板父元素 -->
<Border Background="{TemplateBinding Background}"/>

<!-- 绑定到父级 -->
<TextBlock Text="{Binding DataContext.Title,
             RelativeSource={RelativeSource AncestorType=Window}}"/>
```

**RelativeSource 的模式：**

| 模式 | 说明 | 示例 |
|------|------|------|
| `Self` | 绑定到自身 | `{Binding $self.Bounds.Width}` |
| `TemplatedParent` | 绑定到模板父元素 | `{Binding Background, RelativeSource={RelativeSource TemplatedParent}}` |
| `FindAncestor` | 查找祖先元素 | `{Binding DataContext.Title, RelativeSource={RelativeSource AncestorType=Window}}` |

### 3.2.4 事件处理器

AXAML 支持两种方式连接事件：

#### AXAML 直接绑定（Code-Behind 事件）

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

#### Command 绑定（MVVM 事件）

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

### 3.2.5 附加属性

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

## 3.3 进阶用法

### 3.3.1 ContentControl 和 ContentPresenter 的关系

**ContentControl** 是一个内容容器，它有一个 `Content` 属性和一个 `ContentTemplate` 属性。

```xml
<!-- ContentControl 的基本用法 -->
<ContentControl Content="{Binding CurrentItem}">
    <ContentControl.ContentTemplate>
        <DataTemplate x:DataType="vm:MyItem">
            <TextBlock Text="{Binding Title}"/>
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>
```

**ContentPresenter** 是 ContentControl 内部用于显示内容的控件。它在 ControlTemplate 中使用，负责将 Content 和 ContentTemplate 组合渲染。

```xml
<!-- ContentControl 的 ControlTemplate（简化） -->
<ControlTemplate TargetType="ContentControl">
    <Border Background="{TemplateBinding Background}"
            BorderBrush="{TemplateBinding BorderBrush}"
            BorderThickness="{TemplateBinding BorderThickness}">
        <ContentPresenter Content="{TemplateBinding Content}"
                          ContentTemplate="{TemplateBinding ContentTemplate}"/>
    </Border>
</ControlTemplate>
```

**两者的关系：**

| 概念 | ContentControl | ContentPresenter |
|------|---------------|------------------|
| 角色 | 内容容器 | 内容渲染器 |
| 属性 | Content, ContentTemplate | Content, ContentTemplate |
| 使用场景 | 直接使用 | 在 ControlTemplate 中使用 |
| 继承 | 继承自 Control | 继承自 Control |

### 3.3.2 ItemsControl 和 ItemsPresenter 的关系

**ItemsControl** 是一个集合容器，它有一个 `ItemsSource` 属性和一个 `ItemTemplate` 属性。

```xml
<!-- ItemsControl 的基本用法 -->
<ItemsControl ItemsSource="{Binding Providers}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <TextBlock Text="{Binding Name}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**ItemsPresenter** 是 ItemsControl 内部用于渲染集合的控件。它在 ControlTemplate 中使用，负责将 ItemsSource 和 ItemTemplate 组合渲染。

```xml
<!-- ItemsControl 的 ControlTemplate（简化） -->
<ControlTemplate TargetType="ItemsControl">
    <Border Background="{TemplateBinding Background}">
        <ItemsPresenter Items="{TemplateBinding Items}"
                        ItemsPanel="{TemplateBinding ItemsPanel}"
                        ItemTemplate="{TemplateBinding ItemTemplate}"/>
    </Border>
</ControlTemplate>
```

### 3.3.3 设计时支持

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

### 3.3.4 自定义 MarkupExtension

CodexSwitch 的 `i18n:Tr` 是一个自定义 MarkupExtension，用于国际化文本：

```xml
<!-- 使用自定义 MarkupExtension 加载本地化文本 -->
<Window Title="{i18n:Tr app.name}">
    <TextBlock Text="{i18n:Tr nav.home}"/>
    <TextBlock Text="{i18n:Tr providers.title}"/>
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

**创建自定义 MarkupExtension 的步骤：**

1. 继承 `MarkupExtension`
2. 实现构造函数（接收参数）
3. 重写 `ProvideValue` 方法
4. 在 AXAML 中通过 `{prefix:ExtensionName}` 语法使用

### 3.3.5 集合和内容属性

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

集合属性的简写：

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

## 3.4 组件详解大全

### 3.4.1 ContentControl 完整用法

ContentControl 是所有内容容器的基类。

```xml
<!-- 基本用法 -->
<ContentControl Content="Hello World"/>

<!-- 绑定到 ViewModel 属性 -->
<ContentControl Content="{Binding CurrentItem}"/>

<!-- 使用 ContentTemplate -->
<ContentControl Content="{Binding CurrentItem}">
    <ContentControl.ContentTemplate>
        <DataTemplate x:DataType="vm:MyItem">
            <StackPanel>
                <TextBlock Text="{Binding Title}" FontWeight="Bold"/>
                <TextBlock Text="{Binding Description}"/>
            </StackPanel>
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>

<!-- 使用 DataTemplates 进行类型选择 -->
<ContentControl Content="{Binding SelectedObject}">
    <ContentControl.DataTemplates>
        <DataTemplate x:DataType="vm:ProviderModel">
            <TextBlock Text="{Binding ProviderName}"/>
        </DataTemplate>
        <DataTemplate x:DataType="vm:UsageSnapshot">
            <TextBlock Text="{Binding TotalTokens}"/>
        </DataTemplate>
    </ContentControl.DataTemplates>
</ContentControl>
```

**ContentControl 的关键属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Content` | object | 内容对象 |
| `ContentTemplate` | DataTemplate | 内容模板 |
| `ContentTransition` | IPageTransition | 内容切换动画 |

### 3.4.2 ContentPresenter 完整用法

ContentPresenter 在 ControlTemplate 中使用，负责渲染 ContentControl 的内容。

```xml
<!-- 在 ControlTemplate 中使用 -->
<ControlTemplate TargetType="ContentControl">
    <Border Background="{TemplateBinding Background}"
            BorderBrush="{TemplateBinding BorderBrush}"
            BorderThickness="{TemplateBinding BorderThickness}"
            CornerRadius="{TemplateBinding CornerRadius}"
            Padding="{TemplateBinding Padding}">
        <ContentPresenter Content="{TemplateBinding Content}"
                          ContentTemplate="{TemplateBinding ContentTemplate}"
                          HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
                          VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"/>
    </Border>
</ControlTemplate>
```

**ContentPresenter 的关键属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Content` | object | 内容对象 |
| `ContentTemplate` | DataTemplate | 内容模板 |
| `HorizontalContentAlignment` | HorizontalAlignment | 水平对齐 |
| `VerticalContentAlignment` | VerticalAlignment | 垂直对齐 |

### 3.4.3 ItemsControl 完整用法

ItemsControl 是所有集合控件的基类。

```xml
<!-- 基本用法 -->
<ItemsControl ItemsSource="{Binding Providers}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <TextBlock Text="{Binding Name}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<!-- 自定义 ItemsPanel -->
<ItemsControl ItemsSource="{Binding Providers}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal" Spacing="4"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <Button Content="{Binding Name}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<!-- 使用 DisplayMemberPath -->
<ItemsControl ItemsSource="{Binding Providers}"
              DisplayMemberPath="Name"/>
```

**ItemsControl 的关键属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `ItemsSource` | IEnumerable | 数据源 |
| `Items` | ItemCollection | 项目集合 |
| `ItemTemplate` | DataTemplate | 项目模板 |
| `ItemsPanel` | ItemsPanelTemplate | 面板模板 |
| `DisplayMemberPath` | string | 显示成员路径 |

## 3.5 CodexSwitch 实战

### 3.5.1 MainWindow.axaml 命名空间分析

```xml
<Window xmlns="https://github.com/avaloniaui"              <!-- Avalonia 控件 -->
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"  <!-- XAML 语言 -->
        xmlns:vm="using:CodexSwitch.ViewModels"             <!-- ViewModel 类型 -->
        xmlns:shell="using:CodexSwitch.Views.Shell"         <!-- Shell 页面 -->
        xmlns:pages="using:CodexSwitch.Views.Pages"         <!-- 内容页面 -->
        xmlns:dialogs="using:CodexSwitch.Views.Dialogs"     <!-- 对话框 -->
        xmlns:ui="using:CodexSwitchUI.Controls"             <!-- 自定义控件库 -->
        xmlns:text="using:CodexSwitchUI.Primitives"         <!-- 文本控件 -->
        xmlns:i18n="using:CodexSwitch.I18n"                 <!-- 国际化 -->
        xmlns:lucide="using:Lucide.Avalonia"                <!-- 图标库 -->
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"  <!-- 设计器 -->
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        x:Class="CodexSwitch.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel">
```

### 3.5.2 DataTemplate 中的 x:DataType

CodexSwitch 的每个 DataTemplate 都设置了 x:DataType：

```xml
<!-- MainWindow.axaml: 应用切换器按钮 -->
<ItemsControl ItemsSource="{Binding ClientApps}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ClientAppItem">
            <ui:CodexSegmentedButton IsSelected="{Binding IsSelected}"
                                     Command="{Binding SelectCommand}"
                                     CommandParameter="{Binding}"
                                     ToolTip.Tip="{Binding Name}">
                <ui:CodexImageIcon Path="{Binding IconPath}" Width="16" Height="16"/>
            </ui:CodexSegmentedButton>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<!-- ProvidersPage.axaml: 提供商列表 -->
<ItemsControl ItemsSource="{Binding SelectedProviderRows}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <ui:CodexProviderCard IsActive="{Binding IsActive}"
                                  Header="{Binding DisplayName}"
                                  Description="{Binding BaseUrl}">
                <!-- ... -->
            </ui:CodexProviderCard>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 3.5.3 附加属性的实际使用

```xml
<!-- Grid.Row 和 Grid.Column 定位子元素 -->
<Grid ColumnDefinitions="220,*" RowDefinitions="Auto,*">
    <ui:CodexSidebar/>                    <!-- Row=0, Column=0 (默认) -->
    <Grid Grid.Column="1" RowDefinitions="64,*">
        <shell:TopBar/>                   <!-- Row=0, Column=0 -->
        <Grid Grid.Row="1">               <!-- Row=1, Column=0 -->
            <!-- ... -->
        </Grid>
    </Grid>
</Grid>

<!-- ToolTip.Tip 附加属性 -->
<ui:CodexSegmentedButton ToolTip.Tip="{Binding Name}"/>

<!-- Grid.RowSpan 跨行 -->
<Border Grid.RowSpan="3"
        Classes="usage-status-dot"
        VerticalAlignment="Top"
        Margin="0,5,0,0"/>
```

## 3.6 举一反三

### 3.6.1 AXAML 与 BAML 的区别

| 特性 | WPF BAML | Avalonia AXAML |
|------|----------|----------------|
| 格式 | 二进制压缩 XAML | 源生成 IL 代码 |
| 加载方式 | 运行时从程序集资源加载 | 编译为方法，直接执行 |
| 类型检查 | 运行时 | 编译时（带 x:DataType） |
| 性能 | 需要解析和反射 | 与手写 C# 相当 |
| Native AOT | 不支持 | 完全支持 |

### 3.6.2 编译绑定 vs 运行时绑定

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

**编译绑定的优势：**

| 特性 | 运行时绑定 | 编译绑定 |
|------|-----------|---------|
| 性能 | 慢（反射） | 快（直接调用） |
| 类型安全 | 否 | 是 |
| AOT 兼容 | 否 | 是 |
| 错误检测 | 运行时 | 编译时 |
| 调试难度 | 高 | 低 |

## 3.7 最佳实践与设计模式

### 3.7.1 命名规范

1. **x:Name**：使用 `PART_` 前缀命名模板部件（如 `PART_Root`, `PART_Content`）
2. **x:Key**：使用有意义的名称（如 `CsPrimaryBrush`, `CsRadiusMd`）
3. **xmlns 前缀**：使用简短但有意义的前缀（如 `vm`, `ui`, `i18n`）

### 3.7.2 编译绑定最佳实践

1. **始终设置 x:DataType**：在每个 AXAML 文件和 DataTemplate 中指定
2. **启用全局编译绑定**：在 `.csproj` 中设置 `AvaloniaUseCompiledBindingsByDefault=true`
3. **使用 $self 和 #elementName**：简化绑定表达式

## Deep Dive

### AXAML 编译过程

AXAML 不是运行时解析的 XML，而是在编译时被转换为 IL 代码。

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

**编译产物：**

当你编译一个包含 `x:Class` 的 AXAML 文件时，编译器会生成：

1. **InitializeComponent 方法**：加载 AXAML 定义的视觉树，创建控件实例并设置属性
2. **命名元素字段**：为每个 `x:Name` 元素生成对应字段
3. **绑定代码**：如果使用编译绑定，生成强类型的绑定代码

## Cross References

- **[第 1 章：Avalonia 框架概览](01-avalonia-overview.md)** — 了解 Avalonia 的整体架构和设计哲学
- **[第 2 章：项目结构](02-project-structure.md)** — 了解 .csproj 配置和 MSBuild 任务如何驱动 AXAML 编译
- **[第 5 章：数据绑定](05-data-binding.md)** — 深入学习 Binding 语法和 x:DataType 的编译绑定机制
- **[第 7 章：样式与主题](07-styling-theming.md)** — 了解样式系统中的资源引用和 Selector 语法
- **[第 8 章：DataTemplate](08-data-templates.md)** — 掌握 DataTemplate 中的 x:DataType 和绑定

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

<!-- 正确：资源定义在使用之前 -->
<SolidColorBrush x:Key="MyBrush" Color="Red"/>
<Button Background="{StaticResource MyBrush}"/>  <!-- OK -->

<!-- 或者使用 DynamicResource（运行时解析，不要求顺序） -->
<Button Background="{DynamicResource MyBrush}"/>  <!-- OK -->
```

### 6. TemplateBinding 使用错误

**问题**：TemplateBinding 只能在 ControlTemplate 内部使用。

```xml
<!-- 错误：在普通控件中使用 TemplateBinding -->
<Button Background="{TemplateBinding Background}"/>

<!-- 正确：在 ControlTemplate 中使用 -->
<ControlTemplate TargetType="Button">
    <Border Background="{TemplateBinding Background}">
        <ContentPresenter/>
    </Border>
</ControlTemplate>
```

### 7. 事件处理器签名错误

**问题**：事件处理器的签名必须匹配。

```csharp
// 错误：参数类型不匹配
private void OnClick(object sender, EventArgs e) { }

// 正确：使用正确的事件参数类型
private void OnClick(object? sender, RoutedEventArgs e) { }
```

### 8. 附加属性语法错误

**问题**：附加属性使用 `OwnerType.PropertyName` 语法。

```xml
<!-- 错误：缺少 OwnerType -->
<Button Row="0"/>

<!-- 正确：使用完整的附加属性语法 -->
<Button Grid.Row="0"/>
```

### 9. ContentControl 和 ContentPresenter 混淆

**问题**：ContentControl 用于直接使用，ContentPresenter 用于 ControlTemplate 内部。

```xml
<!-- 错误：在 ControlTemplate 中使用 ContentControl -->
<ControlTemplate TargetType="ContentControl">
    <ContentControl Content="{TemplateBinding Content}"/>
</ControlTemplate>

<!-- 正确：使用 ContentPresenter -->
<ControlTemplate TargetType="ContentControl">
    <ContentPresenter Content="{TemplateBinding Content}"/>
</ControlTemplate>
```

### 10. 忘记 mc:Ignorable="d"

**问题**：设计器命名空间需要 `mc:Ignorable="d"` 来忽略设计器专用属性。

```xml
<!-- 错误：没有 mc:Ignorable="d" -->
<Window xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        d:DesignWidth="1180">
    <!-- 编译警告：d:DesignWidth 未识别 -->
</Window>

<!-- 正确：添加 mc:Ignorable="d" -->
<Window xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        d:DesignWidth="1180">
</Window>
```

### 11. 自定义 MarkupExtension 参数类型错误

**问题**：MarkupExtension 的构造函数参数必须是简单类型。

```csharp
// 错误：构造函数参数不能是复杂类型
public class MyExtension : MarkupExtension
{
    public MyExtension(object complexParam) { }  // 不支持
}

// 正确：使用属性设置复杂参数
public class MyExtension : MarkupExtension
{
    public string? Key { get; set; }
    public override object? ProvideValue(IServiceProvider sp) => Key;
}
```

### 12. 命名空间前缀冲突

**问题**：同一文件中不能有两个相同的前缀。

```xml
<!-- 错误：重复的前缀 -->
<Window xmlns:ui="using:CodexSwitchUI.Controls"
        xmlns:ui="using:MyApp.Controls">
</Window>

<!-- 正确：使用不同的前缀 -->
<Window xmlns:ui="using:CodexSwitchUI.Controls"
        xmlns:local="using:MyApp.Controls">
</Window>
```

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

3. 查看生成的代码，理解 `InitializeComponent` 如何加载 AXAML

### 练习 6：实现 ContentControl 自定义模板

1. 创建一个自定义 ContentControl
2. 定义 ControlTemplate，使用 ContentPresenter
3. 在 AXAML 中使用自定义控件
4. 绑定 Content 和 ContentTemplate

### 练习 7：实现 ItemsControl 自定义面板

1. 创建一个 ItemsControl
2. 使用 WrapPanel 作为 ItemsPanel
3. 定义 ItemTemplate
4. 绑定到 ViewModel 的集合属性

### 练习 8：调试 AXAML 编译错误

1. 故意创建一个包含错误的 AXAML 文件：
   - 使用不存在的属性名
   - 使用错误的类型
   - 使用不存在的资源
2. 编译项目，观察编译错误信息
3. 修复错误，重新编译
