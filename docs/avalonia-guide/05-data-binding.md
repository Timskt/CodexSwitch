# 5. 数据绑定

> **写给零基础的你**：数据绑定就像用一根绳子把两个东西连起来——当一头动了，另一头也跟着动。比如，你的名字存在程序里（数据源），屏幕上有个文字框显示名字（UI）。绑定了之后，你改了程序里的名字，屏幕上的文字框就自动更新，不用你手动去改。

> **关于 ViewModel**：本章会多次提到 ViewModel，它在第 6 章会详细解释。现在你只需要知道：ViewModel 就是一个"中间人"，负责把数据准备好给界面显示。可以先跳过 ViewModel 相关的部分，学完第 6 章再回来看。

## 5.1 概述

数据绑定是 MVVM 模式的核心，它将 UI 元素与数据源连接起来，实现声明式的 UI 更新。本章系统性地讲解 Avalonia 的绑定语法、模式、编译绑定机制，详细讲解 Binding 的所有属性、RelativeSource 的所有模式、MultiBinding、集合绑定等核心知识点。

学完本章后，你将能够：
- 掌握 Binding 的所有属性（Path、Mode、Source、RelativeSource、Converter、ConverterParameter、FallbackValue、TargetNullValue、StringFormat、Delay）
- 掌握 RelativeSource 的所有模式（Self、TemplatedParent、FindAncestor）
- 掌握 MultiBinding 和 IMultiValueConverter 的用法
- 理解集合绑定（ObservableCollection、ICollectionView）
- 理解编译绑定 vs 运行时绑定的区别
- 掌握设计时数据（d:DataContext、d:DesignData）

## 5.2 核心概念

### 5.2.1 绑定模式

> **小白提示**：绑定模式决定了数据的"流动方向"。就像水管：
> - **OneWay** = 单向水管，水从水厂流到你家（数据从程序流向界面）
> - **TwoWay** = 双向水管，你家的水也能流回水厂（界面改了，程序也跟着更新）
> - **OneTime** = 只放一次水，之后水管就关了（只初始化一次，之后不再更新）
> - **OneWayToSource** = 反向水管，水从你家流到水厂（界面改了，程序更新，但程序改了界面不变）

绑定模式决定了数据在 UI 和 ViewModel 之间的流动方向：

| 模式 | 方向 | 说明 | 典型用途 | 类比 |
|------|------|------|---------|------|
| `OneWay` | 数据源 → UI | 默认模式，UI 自动反映数据变化 | TextBlock, Image | 电视直播，只能看不能改 |
| `TwoWay` | 数据源 ↔ UI | 双向同步 | TextBox, CheckBox, ComboBox | 微信聊天，双方都能发消息 |
| `OneTime` | 数据源 → UI（仅一次） | 绑定后不再更新 | 静态标签、版本号 | 报纸，印好了就不变了 |
| `OneWayToSource` | UI → 数据源 | UI 变化推送到数据源 | 较少使用 | 单向快递，只能寄不能收 |

```xml
<!-- 单向绑定（默认） -->
<TextBlock Text="{Binding CurrentPage}"/>

<!-- 双向绑定 -->
<ui:CodexSelect SelectedItem="{Binding DefaultModel, Mode=TwoWay}"/>

<!-- 单次绑定 -->
<TextBlock Text="{Binding CurrentVersionTag, Mode=OneTime}"/>

<!-- 单向到源 -->
<TextBlock Text="{Binding SearchText, Mode=OneWayToSource}"/>
```

### 5.2.2 绑定路径

> **小白提示**：绑定路径就像"地址"。`{Binding DisplayName}` 就是说"去 DataContext 里找 DisplayName 这个属性"。`{Binding User.Address.City}` 就是说"先找 User，再找 User 里的 Address，再找 Address 里的 City"——就像"北京市海淀区中关村大街 1 号"这样的地址。

```xml
<!-- 简单属性：直接找 DisplayName -->
<TextBlock Text="{Binding DisplayName}"/>

<!-- 嵌套属性：找 User 里面的 Address 里面的 City -->
<TextBlock Text="{Binding User.Address.City}"/>

<!-- 索引器：找 Items 里的第 1 个元素（从 0 开始数） -->
<TextBlock Text="{Binding Items[0]}"/>

<!-- 自身引用：绑定到控件自己的属性 -->
<TextBlock Text="{Binding $self.Bounds.Width}"/>

<!-- 命名元素引用：绑定到另一个控件的属性 -->
<Border Background="{Binding #MyPanel.Background}"/>

<!-- 带 Path 的显式写法 -->
<TextBlock Text="{Binding Path=SelectedProvider.Name}"/>
```

CodexSwitch 中绑定路径的实际用法：

```xml
<!-- ProvidersPage.axaml: 简单属性绑定 -->
<ui:CodexProviderCard Command="{Binding SelectCommand}"
                       CommandParameter="{Binding}"
                       IsActive="{Binding IsActive}"
                       Header="{Binding DisplayName}"
                       Description="{Binding BaseUrl}">
</ui:CodexProviderCard>
```

注意 `CommandParameter="{Binding}"` 使用了"自身绑定"——将当前 DataContext（即 `ProviderListItem`）作为命令参数传递。

### 5.2.3 Binding 的所有属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Path` | string | 绑定路径（如 `Name`, `User.Address.City`） |
| `Mode` | BindingMode | 绑定模式（OneWay, TwoWay, OneTime, OneWayToSource） |
| `Source` | object | 绑定源对象 |
| `RelativeSource` | RelativeSource | 相对绑定源 |
| `Converter` | IValueConverter | 值转换器 |
| `ConverterParameter` | object | 转换器参数 |
| `FallbackValue` | object | 绑定失败时的默认值 |
| `TargetNullValue` | object | 绑定值为 null 时的默认值 |
| `StringFormat` | string | 字符串格式化 |
| `Delay` | int | 绑定更新延迟（毫秒） |
| `ElementName` | string | 绑定到命名元素 |
| `UpdateSourceTrigger` | UpdateSourceTrigger | 源更新触发器 |

#### FallbackValue 和 TargetNullValue

```xml
<!-- 当绑定失败时显示的值 -->
<TextBlock Text="{Binding UserName, FallbackValue='Guest'}"/>

<!-- 当绑定值为 null 时显示的值 -->
<TextBlock Text="{Binding Description, TargetNullValue='No description'}"/>
```

| 场景 | FallbackValue | TargetNullValue |
|------|---------------|-----------------|
| 属性不存在 | 使用 | 不使用 |
| 路径错误 | 使用 | 不使用 |
| 值为 null | 不使用 | 使用 |

#### StringFormat

```xml
<!-- 数字格式化 -->
<TextBlock Text="{Binding Price, StringFormat='{}${0:F2}'}"/>

<!-- 日期格式化 -->
<TextBlock Text="{Binding CreatedAt, StringFormat='{}{0:yyyy-MM-dd}'}"/>

<!-- 复合格式化 -->
<TextBlock Text="{Binding Count, StringFormat='Total: {0:N0} items'}"/>

<!-- 带单位的格式化 -->
<TextBlock Text="{Binding Temperature, StringFormat='{}{0:N1} C'}"/>
```

#### Delay

```xml
<!-- 延迟 300ms 后更新源（常用于搜索框） -->
<TextBox Text="{Binding SearchText, Delay=300}"/>

<!-- 延迟 500ms 后更新源 -->
<TextBox Text="{Binding FilterText, Delay=500, Mode=TwoWay}"/>
```

**Delay 的使用场景：**
- 搜索框：用户输入时延迟搜索，避免频繁请求
- 筛选器：用户输入时延迟筛选，避免频繁刷新
- 自动保存：用户编辑时延迟保存，避免频繁写入

#### Converter

```xml
<!-- 使用值转换器 -->
<TextBlock Text="{Binding IsVisible, Converter={StaticResource BoolToStringConverter}}"/>

<!-- 带参数的转换器 -->
<TextBlock Text="{Binding Price,
    Converter={StaticResource PriceFormatConverter},
    ConverterParameter='USD'}"/>
```

### 5.2.4 编译绑定（x:DataType）

当设置了 `x:DataType` 时，绑定表达式在编译时验证，而不是运行时通过反射。

```xml
<!-- 编译绑定：编译时验证 CurrentPage 属性存在 -->
<Window x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding CurrentPage}"/>  <!-- OK -->
    <TextBlock Text="{Binding NonExistent}"/>  <!-- 编译错误 -->
</Window>
```

CodexSwitch 在 `.csproj` 中启用了全局编译绑定：

```xml
<PropertyGroup>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

**编译绑定的优势：**

| 特性 | 运行时绑定 | 编译绑定 |
|------|-----------|---------|
| 性能 | 慢（反射） | 快（直接调用） |
| 类型安全 | 否 | 是 |
| AOT 兼容 | 否 | 是 |
| 错误检测 | 运行时 | 编译时 |
| 调试难度 | 高 | 低 |

## 5.3 进阶用法

### 5.3.1 RelativeSource 绑定

```xml
<!-- 绑定到自身 -->
<TextBlock Text="{Binding $self.Name}"/>

<!-- 在 ControlTemplate 中绑定到模板父元素 -->
<Border Background="{TemplateBinding Background}"/>

<!-- 绑定到父级（AncestorType） -->
<TextBlock Text="{Binding DataContext.Title,
             RelativeSource={RelativeSource AncestorType=Window}}"/>

<!-- 绑定到祖先级 -->
<TextBlock Text="{Binding DataContext.Title,
             RelativeSource={RelativeSource AncestorType=UserControl, AncestorLevel=2}}"/>
```

**RelativeSource 的模式：**

| 模式 | 说明 | Avalonia 简写 |
|------|------|--------------|
| `Self` | 绑定到自身 | `$self` |
| `TemplatedParent` | 绑定到模板父元素 | `TemplateBinding` |
| `FindAncestor` | 查找祖先元素 | `RelativeSource` |

**$self 语法：**

```xml
<!-- 绑定到自身的属性 -->
<TextBlock Text="{Binding $self.Bounds.Width}"/>

<!-- 绑定到自身的 Tag -->
<Button Tag="Hello" Content="{Binding $self.Tag}"/>

<!-- 在 Style 中使用 -->
<Style Selector="TextBlock">
    <Setter Property="ToolTip.Tip" Value="{Binding $self.Text}"/>
</Style>
```

**#elementName 语法：**

```xml
<!-- 绑定到命名元素 -->
<Button Command="{Binding #MyListBox.SelectedItem.DeleteCommand}"/>

<!-- 绑定到命名元素的属性 -->
<Border Background="{Binding #MyPanel.Background}"/>

<!-- 在 DataTemplate 中绑定到父级元素 -->
<DataTemplate x:DataType="vm:ItemModel">
    <Button Command="{Binding #Root.DataContext.DeleteCommand}"/>
</DataTemplate>
```

### 5.3.2 MultiBinding

```xml
<!-- 多个绑定组合 -->
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="{}{0} - {1}">
            <Binding Path="FirstName"/>
            <Binding Path="LastName"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

**IMultiValueConverter 示例：**

```csharp
public class FullNameConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2)
        {
            var firstName = values[0]?.ToString() ?? "";
            var lastName = values[1]?.ToString() ?? "";
            return $"{firstName} {lastName}".Trim();
        }
        return string.Empty;
    }
}
```

```xml
<!-- 使用 IMultiValueConverter -->
<TextBlock>
    <TextBlock.Text>
        <MultiBinding Converter="{StaticResource FullNameConverter}">
            <Binding Path="FirstName"/>
            <Binding Path="LastName"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

### 5.3.3 PriorityBinding

PriorityBinding 允许按优先级显示多个绑定值，当高优先级绑定尚未完成时，显示低优先级的值。

```xml
<TextBlock>
    <TextBlock.Text>
        <PriorityBinding>
            <Binding Path="SlowValue" IsAsync="True"/>
            <Binding Path="FastValue"/>
        </PriorityBinding>
    </TextBlock.Text>
</TextBlock>
```

### 5.3.4 集合绑定

#### ObservableCollection

```csharp
// ViewModel 中的集合属性
public ObservableCollection<ProviderListItem> Providers { get; } = new();

// 添加项时 UI 自动更新
Providers.Add(new ProviderListItem { Name = "Provider 1" });

// 移除项时 UI 自动更新
Providers.RemoveAt(0);

// 替换整个集合（不会触发 UI 更新，需要通知）
Providers = new ObservableCollection<ProviderListItem>(newItems);  // 错误！

// 正确做法：清空并重新添加
Providers.Clear();
foreach (var item in newItems)
    Providers.Add(item);
```

```xml
<!-- 绑定到集合 -->
<ItemsControl ItemsSource="{Binding Providers}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <TextBlock Text="{Binding Name}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

#### ICollectionView

```csharp
// 创建集合视图（支持排序、筛选、分组）
var view = new DataGridCollectionView(providers);
view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending"));
view.Filter = item => ((ProviderListItem)item).IsActive;

// 绑定到集合视图
ItemsSource = view;
```

### 5.3.5 设计时数据

```xml
<!-- d:DataContext 设置设计时数据上下文 -->
<Window x:DataType="vm:MainWindowViewModel"
        d:DesignWidth="1180"
        d:DesignHeight="760"
        mc:Ignorable="d">

    <Design.DataContext>
        <vm:MainWindowViewModel/>
    </Design.DataContext>

    <!-- 运行时使用 DataContext，设计时使用 Design.DataContext -->
    <TextBlock Text="{Binding CurrentPage}"/>
</Window>
```

**Design.DataContext 的作用：**
- 在 IDE 设计器中提供 IntelliSense
- 在设计时预览绑定数据
- 不影响运行时行为

## 5.4 组件详解大全

### 5.4.1 Binding 对象详解

```csharp
// 代码中创建 Binding
var binding = new Binding("Name")
{
    Mode = BindingMode.TwoWay,
    Source = viewModel,
    Converter = new StringToBoolConverter(),
    FallbackValue = "Unknown",
    StringFormat = "Name: {0}"
};

// 应用绑定
textBlock.Bind(TextBlock.TextProperty, binding);
```

### 5.4.2 IValueConverter 接口

```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Avalonia 的 IsVisible 是 bool 类型，不是 WPF 的 Visibility 枚举
        if (value is bool boolValue)
            return boolValue;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true;
    }
}
```

```xml
<!-- 使用值转换器 -->
<UserControl.Styles>
    <Style Selector="Border.hidden">
        <Setter Property="IsVisible" Value="False"/>
    </Style>
</UserControl.Styles>

<Border Classes.hidden="{Binding IsHidden, Converter={StaticResource BoolToVisibilityConverter}}"/>
```

### 5.4.3 编译绑定生成的代码

当设置了 `x:DataType` 时，编译器生成的绑定代码：

```xml
<Window x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding CurrentPage}"/>
</Window>
```

生成的伪代码：

```csharp
// 编译器生成的绑定代码
var binding = new CompiledBinding(
    source: vm,
    getter: vm => vm.CurrentPage,
    setter: (vm, value) => vm.CurrentPage = value
);
textBlock.Bind(TextBlock.TextProperty, binding);
```

## 5.5 CodexSwitch 实战

### 5.5.1 ProvidersPage.axaml 的绑定分析

```xml
<!-- 集合绑定 -->
<ItemsControl ItemsSource="{Binding SelectedProviderRows}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <!-- 简单属性绑定 -->
            <ui:CodexProviderCard IsActive="{Binding IsActive}"
                                  Header="{Binding DisplayName}"
                                  Description="{Binding BaseUrl}">

                <!-- 自身绑定（传递 DataContext 作为参数） -->
                <ui:CodexProviderCard Command="{Binding SelectCommand}"
                                       CommandParameter="{Binding}"/>

                <!-- 双向绑定 -->
                <ui:CodexSelect SelectedItem="{Binding DefaultModel, Mode=TwoWay}"
                                IsEnabled="{Binding CanChangeDefaultModel}"/>

                <!-- 条件类绑定 -->
                <Border Classes="provider-usage"
                        Classes.success="{Binding IsUsageValid}"
                        Classes.error="{Binding IsUsageError}"
                        Classes.refreshing="{Binding IsUsageRefreshing}"
                        IsVisible="{Binding HasUsageInfo}"/>

                <!-- ToolTip 绑定 -->
                <Border ToolTip.Tip="{Binding UsageToolTip}"/>

                <!-- StringFormat 绑定 -->
                <TextBlock Text="{Binding UsageSummary}"
                           TextAlignment="Right"
                           TextTrimming="CharacterEllipsis"/>
            </ui:CodexProviderCard>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 5.5.2 MainWindow.axaml 的绑定分析

```xml
<!-- 简单属性绑定 -->
<Window Title="{i18n:Tr app.name}">

<!-- 菜单按钮命令绑定 -->
<ui:CodexSidebarMenuButton Command="{Binding ShowProvidersCommand}"
                            IsActive="{Binding IsProvidersPageVisible}">
    <TextBlock Text="{i18n:Tr nav.providers}"/>
</ui:CodexSidebarMenuButton>

<!-- 图标路径绑定 -->
<ui:CodexImageIcon Path="{Binding CodexIconPath}" Width="24" Height="24"/>

<!-- 状态文本绑定 -->
<ui:CodexBadge Variant="Secondary">
    <TextBlock Text="{Binding ServiceStateText}"/>
</ui:CodexBadge>

<!-- 可见性绑定 -->
<text:CodexText IsVisible="{Binding IsSidebarUpdateStatusVisible}"/>

<!-- 页面切换绑定 -->
<pages:HomePage IsVisible="{Binding IsHomePageVisible}"/>
<pages:ProvidersPage IsVisible="{Binding IsProvidersPageVisible}"/>

<!-- 集合绑定（应用切换器） -->
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
```

## 5.6 举一反三

### 5.6.1 INotifyPropertyChanged 的角色

绑定引擎依赖 `INotifyPropertyChanged` 来感知数据变化：

```csharp
// ViewModelBase 继承自 ObservableObject（实现了 INotifyPropertyChanged）
public abstract class ViewModelBase : ObservableObject { }

// [ObservableProperty] 自动生成属性和变更通知
[ObservableProperty]
private string _currentPage = "Home";
// 自动生成:
// public string CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }
```

当 `CurrentPage` 被赋新值时，`SetProperty` 调用 `PropertyChanged` 事件，绑定引擎收到通知后更新 UI。

### 5.6.2 绑定与值转换器的配合

```csharp
// 日期格式化转换器
public class DateTimeFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var format = parameter as string ?? "yyyy-MM-dd HH:mm";
            return dateTime.ToString(format);
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

```xml
<!-- 使用日期格式化转换器 -->
<TextBlock Text="{Binding CreatedAt, Converter={StaticResource DateTimeFormatConverter}, ConverterParameter='yyyy-MM-dd'}"/>
```

## 5.7 最佳实践与设计模式

### 5.7.1 绑定最佳实践

1. **始终设置 x:DataType**：启用编译绑定，提前发现错误
2. **使用 OneWay 模式**：除非需要双向同步，否则使用 OneWay
3. **使用 StringFormat**：在 AXAML 中格式化，减少 ViewModel 代码
4. **使用 FallbackValue**：为绑定失败提供默认值
5. **避免复杂的绑定路径**：路径越深，性能越差

### 5.7.2 值转换器最佳实践

1. **保持简单**：转换器应该只做简单的值转换
2. **处理 null 值**：始终检查输入值是否为 null
3. **返回合理默认值**：转换失败时返回合理的默认值
4. **使用静态资源**：将转换器定义为静态资源，避免重复创建

## Deep Dive

### 绑定引擎内部机制

Avalonia 的绑定引擎分为两种实现：

#### 运行时绑定（Reflection-based）

```
绑定表达式 → 解析路径 → 反射查找属性 → 订阅 PropertyChanged → 更新目标
```

运行时绑定通过反射访问属性，支持动态路径但性能较差，且不兼容 Native AOT。

#### 编译绑定（Source Generator-based）

```
绑定表达式 → 编译时解析路径 → 生成强类型访问代码 → 直接调用 getter/setter
```

编译绑定通过源代码生成器将绑定表达式转换为直接的属性访问代码。

#### INotifyPropertyChanged 的实现

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

- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 AXAML 中的绑定语法和 MarkupExtension
- **[第 6 章：MVVM 模式](06-mvvm-pattern.md)** — 理解 ViewModel 如何通过 [ObservableProperty] 驱动绑定
- **[第 8 章：DataTemplate](08-data-templates.md)** — 掌握 DataTemplate 中的绑定
- **[第 15 章：编译绑定](15-compiled-bindings.md)** — 深入了解 x:DataType 和编译绑定的内部机制
- **[第 19 章：值转换器](19-value-converters.md)** — 学习 IValueConverter 和 IMultiValueConverter 的高级用法

## Common Pitfalls

### 1. 忘记 x:DataType 导致绑定静默失败

**问题**：CodexSwitch 启用了全局编译绑定，没有 `x:DataType` 的绑定会编译失败或退化为运行时绑定。

```xml
<!-- 错误：缺少 x:DataType -->
<UserControl>
    <TextBlock Text="{Binding DisplayName}"/>  <!-- 编译警告或失败 -->
</UserControl>

<!-- 正确：设置 x:DataType -->
<UserControl x:DataType="vm:MainWindowViewModel">
    <TextBlock Text="{Binding DisplayName}"/>
</UserControl>
```

### 2. DataTemplate 中忘记设置 x:DataType

**问题**：ItemsControl 的 ItemTemplate 需要单独设置 `x:DataType`，不能继承外层的类型。

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

### 3. 忘记 INotifyPropertyChanged

**问题**：ViewModel 没有实现 `INotifyPropertyChanged`，UI 不会更新。

```csharp
// 错误：没有实现 INotifyPropertyChanged
public class MainViewModel
{
    public string Name { get; set; }  // 修改后 UI 不会更新
}

// 正确：继承 ObservableObject
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;  // 自动生成变更通知
}
```

### 4. 绑定路径拼写错误

**问题**：没有 `x:DataType` 时，拼写错误不会在编译时发现。

```xml
<!-- 错误：拼写错误，运行时静默失败 -->
<TextBlock Text="{Binding UserNmae}"/>

<!-- 正确：启用编译绑定后，编译器会报错 -->
<TextBlock Text="{Binding UserName}"/>
```

### 5. 替换集合引用而非修改集合

**问题**：替换 ObservableCollection 的引用不会触发 UI 更新。

```csharp
// 错误：替换引用
Items = new ObservableCollection<ItemViewModel>(newItems);

// 正确：修改现有集合
Items.Clear();
foreach (var item in newItems)
    Items.Add(item);
```

### 6. StaticResource 和 DynamicResource 选择错误

**问题**：`StaticResource` 在编译时解析，如果资源定义在引用之后，会编译失败。

```xml
<!-- 错误：资源定义在使用之后 -->
<Button Background="{StaticResource MyBrush}"/>  <!-- 编译失败 -->

<!-- 正确：资源定义在使用之前 -->
<SolidColorBrush x:Key="MyBrush" Color="Red"/>
<Button Background="{StaticResource MyBrush}"/>  <!-- OK -->

<!-- 或者使用 DynamicResource（运行时解析） -->
<Button Background="{DynamicResource MyBrush}"/>  <!-- OK -->
```

### 7. Converter 参数类型错误

**问题**：ConverterParameter 在 AXAML 中是字符串，需要在转换器中正确处理。

```csharp
// 错误：假设参数是 int
public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
{
    var count = (int)parameter;  // 可能抛出异常
}

// 正确：正确解析参数
public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
{
    var count = int.TryParse(parameter?.ToString(), out var c) ? c : 0;
}
```

### 8. 在 DataTemplate 中访问父级 DataContext

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

### 9. 绑定到错误的属性类型

**问题**：绑定的源属性和目标属性类型不匹配。

```xml
<!-- 错误：TextBlock.Text 是 string，绑定到 int -->
<TextBlock Text="{Binding Count}"/>  <!-- 可能显示 "0" 但不格式化 -->

<!-- 正确：使用 StringFormat -->
<TextBlock Text="{Binding Count, StringFormat='{}{0:N0}'}"/>
```

### 10. TwoWay 绑定的源更新时机

**问题**：TextBox 的 TwoWay 绑定默认在失去焦点时更新源。

```xml
<!-- 问题：用户输入时 ViewModel 不会立即更新 -->
<TextBox Text="{Binding Name, Mode=TwoWay}"/>

<!-- 解决：使用 UpdateSourceTrigger -->
<TextBox Text="{Binding Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
```

### 11. 忘记取消异步绑定

**问题**：异步绑定可能导致内存泄漏。

```csharp
// 错误：没有取消异步绑定
var binding = new Binding("SlowValue") { IsAsync = true };
textBlock.Bind(TextBlock.TextProperty, binding);

// 正确：在控件卸载时取消绑定
textBlock.DetachedFromVisualTree += (_, _) =>
{
    textBlock.Unbind(TextBlock.TextProperty);
};
```

### 12. MultiBinding 的 Converter 返回类型错误

**问题**：MultiBinding 的 Converter 必须返回目标属性的类型。

```csharp
// 错误：返回 string，但目标属性是 bool
public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
{
    return "true";  // 错误：应该返回 bool
}

// 正确：返回正确的类型
public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
{
    return true;  // 正确：返回 bool
}
```

## Try It Yourself

### 练习 1：分析 ProvidersPage.axaml 的绑定

打开 `ProvidersPage.axaml`，逐行识别所有绑定表达式：

1. `{Binding SelectedProviderRows}` — 集合绑定
2. `{Binding DisplayName}` — DataTemplate 内的属性绑定
3. `{Binding DefaultModel, Mode=TwoWay}` — 双向绑定
4. `{Binding IsUsageValid}` — Classes 附加属性绑定
5. `{Binding SelectCommand}` — 命令绑定

### 练习 2：创建一个带编译绑定的页面

1. 创建一个 ViewModel 和对应的 AXAML 页面
2. 故意在绑定中使用一个不存在的属性名
3. 编译项目，观察编译错误
4. 修复错误后，验证绑定正常工作

### 练习 3：实现 Classes 附加属性绑定

在 AXAML 中使用 `Classes.xxx="{Binding IsSelected}"` 实现条件样式，然后在 Style 中为不同的类组合定义不同的视觉效果。

### 练习 4：实现 MultiBinding

创建一个 `IMultiValueConverter`，将多个属性值组合为一个显示字符串，在 AXAML 中使用 `MultiBinding` 和 `StringFormat`。

### 练习 5：调试绑定错误

故意创建一个绑定到不存在属性的页面，运行项目后在调试输出中查找绑定错误信息。使用 Avalonia DevTools 检查绑定状态和 DataContext。

### 练习 6：实现值转换器

创建一个 `BoolToColorConverter`，将布尔值转换为颜色（true=绿色，false=红色），在 AXAML 中使用。

### 练习 7：实现集合绑定

创建一个 ViewModel，包含一个 `ObservableCollection`，在 AXAML 中绑定到 ItemsControl，测试添加、删除、清空操作。

### 练习 8：实现设计时数据

为你的页面创建设计时 ViewModel，提供示例数据，在 IDE 设计器中预览。
