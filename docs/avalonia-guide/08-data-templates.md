# 8. DataTemplate 深度解析

> **写给零基础的你**：DataTemplate 就像一个"模具"。你有一堆数据（比如联系人列表），每个联系人有名字、电话、头像。DataTemplate 定义了"每个联系人应该长什么样"——名字放左边，电话放右边，头像放最前面。有了模具，你不需要手动摆每一个联系人，系统会自动套用模具来显示。

## 8.1 基础 DataTemplate

DataTemplate 定义数据对象如何在 UI 中呈现（就像模具定义了产品的形状）：

```xml
<ItemsControl ItemsSource="{Binding Providers}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <Border Classes="provider-card" Padding="12">
                <StackPanel>
                    <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                    <TextBlock Text="{Binding Endpoint}" Foreground="{StaticResource CsMutedForegroundBrush}"/>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 关键：x:DataType

> **小白提示**：`x:DataType` 就是告诉模具"你处理的是什么材料"。就像面包模具要知道"这是面粉团"，蛋糕模具要知道"这是蛋糕糊"。写了 `x:DataType` 后，编译器就能检查你绑定的属性名是否正确。

在每个 DataTemplate 中指定 `x:DataType` 启用编译绑定：

```xml
<DataTemplate x:DataType="vm:ProviderListItem">
    <!-- 编译器会验证 ProviderListItem 是否有 Name 属性 -->
    <!-- 如果 ProviderListItem 没有 Name 属性，编译时就会报错 -->
    <TextBlock Text="{Binding Name}"/>
</DataTemplate>
```

## 8.2 多种 DataTemplate 模式

### ItemsControl.ItemTemplate

最常用的模式，用于集合控件：

```xml
<ItemsControl ItemsSource="{Binding ClientApps}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal" Spacing="4"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ClientAppItem">
            <ui:CodexSegmentedButton IsSelected="{Binding IsSelected}"
                                     Command="{Binding SelectCommand}">
                <ui:CodexImageIcon Path="{Binding IconPath}" Width="16" Height="16"/>
            </ui:CodexSegmentedButton>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 内联 DataTemplate（在 ContentControl 中）

```xml
<ContentControl Content="{Binding CurrentItem}">
    <ContentControl.ContentTemplate>
        <DataTemplate x:DataType="vm:MyItem">
            <TextBlock Text="{Binding Title}"/>
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>
```

### DataTemplate 选择器

根据数据类型选择不同模板：

```xml
<ContentControl Content="{Binding SelectedObject}">
    <ContentControl.DataTemplates>
        <DataTemplate x:DataType="models:ProviderModel">
            <TextBlock Text="{Binding ProviderName}"/>
        </DataTemplate>
        <DataTemplate x:DataType="models:UsageSnapshot">
            <TextBlock Text="{Binding TotalTokens, StringFormat='{}{0:N0} tokens'}"/>
        </DataTemplate>
    </ContentControl.DataTemplates>
</ContentControl>
```

## 8.3 CodexSwitch 中的 DataTemplate 实战

### 17 种数据模板

CodexSwitch 定义了 17 种不同的 DataTemplate，展示了各种场景：

| 模板 | 数据类型 | 场景 |
|------|---------|------|
| ProviderListItem | 侧边栏卡片列表 | 复杂卡片布局 |
| ModelEditorItem | 模型路由行 | 表单行 |
| ModelConversionEditorItem | 模型转换行 | 嵌套表单 |
| ClientAppItem | 应用切换器按钮 | 按钮组 |
| MiniStatusMetricCardItem | 状态栏指标 | 紧凑卡片 |
| MiniStatusQuotaCardItem | 配额显示 | 进度条 |
| UsageMetricItem | 仪表盘指标 | 统计卡片 |
| UsageLogItem | 日志表行 | 表格行 |
| ModelCatalogItem | 模型目录行 | 表格行 |
| ProviderTemplateItem | 提供商模板 | 下拉选项 |
| I18nLanguageOption | 语言选择器 | 下拉选项 |
| OAuthAccountListItem | OAuth 账户 | 列表项 |

### 复杂卡片模板示例

```xml
<!-- ProviderListItem 的 DataTemplate -->
<DataTemplate x:DataType="vm:ProviderListItem">
    <ui:CodexProviderCard
        IsActive="{Binding IsActive}"
        IsDefault="{Binding IsDefault}"
        Command="{Binding SelectProviderCommand}"
        CommandParameter="{Binding}"
        ContextMenu="{Binding ContextMenu}">
        <Grid ColumnDefinitions="Auto,*,Auto">
            <ui:CodexImageIcon Path="{Binding IconPath}" Width="20" Height="20"/>
            <StackPanel Grid.Column="1" Margin="10,0,0,0">
                <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                <TextBlock Text="{Binding Endpoint}"
                           Foreground="{StaticResource CsMutedForegroundBrush}"
                           FontSize="11"/>
            </StackPanel>
            <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="4">
                <ui:CodexBadge Content="{Binding ModelCount}"
                               IsVisible="{Binding HasModels}"/>
                <lucide:LucideIcon Kind="GripVertical" Size="14"
                                   Classes="drag-handle"/>
            </StackPanel>
        </Grid>
    </ui:CodexProviderCard>
</DataTemplate>
```

### 表格行模板示例

```xml
<!-- UsageLogItem 的 DataTemplate -->
<DataTemplate x:DataType="vm:UsageLogItem">
    <ui:CsTableRow>
        <ui:CsTableCell>
            <TextBlock Text="{Binding Timestamp}"
                       FontSize="12"
                       Foreground="{StaticResource CsMutedForegroundBrush}"/>
        </ui:CsTableCell>
        <ui:CsTableCell>
            <TextBlock Text="{Binding ProviderName}" FontWeight="SemiBold"/>
        </ui:CsTableCell>
        <ui:CsTableCell>
            <TextBlock Text="{Binding ModelName}"/>
        </ui:CsTableCell>
        <ui:CsTableCell>
            <TextBlock Text="{Binding InputTokens, StringFormat='{}{0:N0}'}"/>
        </ui:CsTableCell>
        <ui:CsTableCell>
            <TextBlock Text="{Binding OutputTokens, StringFormat='{}{0:N0}'}"/>
        </ui:CsTableCell>
        <ui:CsTableCell>
            <TextBlock Text="{Binding EstimatedCost, StringFormat='{}${0:F4}'}"/>
        </ui:CsTableCell>
    </ui:CsTableRow>
</DataTemplate>
```

## 8.4 FuncDataTemplate

在代码中创建 DataTemplate（高级用法）：

```csharp
// 通过代码创建 DataTemplate
var template = new FuncDataTemplate<MyItem>((item, _) =>
{
    return new TextBlock
    {
        Text = item.Name,
        FontWeight = FontWeight.SemiBold
    };
});
```

## 8.5 数据模板与虚拟化

当数据量大时，结合 `VirtualizingStackPanel` 实现虚拟化：

```xml
<ItemsControl ItemsSource="{Binding LargeCollection}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <!-- 只渲染可见区域的项，节省内存和 CPU -->
            <VirtualizingStackPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ModelCatalogItem">
            <ui:CsTableRow>
                <!-- 表格行内容 -->
            </ui:CsTableRow>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 虚拟化的工作原理

1. `VirtualizingStackPanel` 只为可见区域创建 UI 元素
2. 当用户滚动时，回收离开视口的 UI 元素
3. 复用回收的 UI 元素来显示新进入视口的数据

### 何时使用虚拟化

| 场景 | 是否虚拟化 |
|------|-----------|
| < 50 项 | 不需要 |
| 50-500 项 | 建议使用 |
| > 500 项 | 必须使用 |

## 8.6 DataTemplate 中的命令绑定

CodexSwitch 展示了多种命令绑定模式：

### 直接绑定到 ViewModel 方法

```xml
<DataTemplate x:DataType="vm:ClientAppItem">
    <Button Command="{Binding SelectCommand}"  <!-- 绑定到 Item 上的命令 -->
            CommandParameter="{Binding}"/>       <!-- 将自身作为参数 -->
</DataTemplate>
```

### 绑定到父 ViewModel 的命令

```xml
<DataTemplate x:DataType="vm:ProviderListItem">
    <Button Command="{Binding #Root.DataContext.DeleteProviderCommand}"
            CommandParameter="{Binding Id}"/>
</DataTemplate>
```

这里 `#Root` 引用命名的父元素，`DataContext` 获取父元素的 ViewModel。

### 使用 x:Reference 绑定

```xml
<Window x:Name="RootWindow" x:DataType="vm:MainWindowViewModel">
    <ItemsControl ItemsSource="{Binding Items}">
        <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="vm:ItemModel">
                <!-- 通过 x:Reference 绑定到窗口的 ViewModel -->
                <Button Command="{Binding #RootWindow.DataContext.DeleteCommand}"
                        CommandParameter="{Binding}"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>
```

## 8.7 深入：DataTemplate 绑定内部机制

### DataTemplate 的实例化过程

当 `ItemsControl` 需要显示数据项时，DataTemplate 的实例化遵循以下流程：

```
ItemsSource 变化
    ↓
ItemsControl 创建 ItemContainerGenerator
    ↓
对每个数据项，查找匹配的 DataTemplate
    ↓
实例化 DataTemplate 的根元素
    ↓
设置 DataContext 为数据项
    ↓
应用编译绑定（如果有 x:DataType）
    ↓
将实例添加到可视化树
```

### 绑定上下文传播

在 DataTemplate 中，`DataContext` 自动设置为当前数据项：

```xml
<DataTemplate x:DataType="vm:ProviderListItem">
    <!-- DataContext 是 ProviderListItem 实例 -->
    <TextBlock Text="{Binding Name}"/>
    <!-- 等价于 -->
    <TextBlock Text="{Binding DataContext.Name, RelativeSource={RelativeSource TemplatedParent}}"/>
</DataTemplate>
```

### 编译绑定 vs 反射绑定

指定 `x:DataType` 后，Avalonia 使用编译绑定：

```xml
<!-- 编译绑定：编译器验证属性存在性，生成直接访问代码 -->
<DataTemplate x:DataType="vm:ProviderListItem">
    <TextBlock Text="{Binding Name}"/>
    <!-- 编译器检查 ProviderListItem 是否有 Name 属性 -->
    <!-- 生成的代码直接访问 item.Name，无需反射 -->
</DataTemplate>

<!-- 反射绑定：运行时通过反射查找属性，性能较差 -->
<DataTemplate>
    <TextBlock Text="{Binding Name}"/>
    <!-- 运行时才检查 DataContext 是否有 Name 属性 -->
</DataTemplate>
```

编译绑定的优势：
- 编译时类型检查，提前发现错误
- 无反射开销，性能更好
- 支持 AOT 编译

### TemplateBinding vs Binding

在 ControlTemplate 中，`TemplateBinding` 和 `Binding` 有区别：

```xml
<ControlTemplate>
    <!-- TemplateBinding：单向、只读、性能更好 -->
    <Border Background="{TemplateBinding Background}"/>

    <!-- Binding：支持双向、需要转换器时使用 -->
    <Border Background="{Binding Background, RelativeSource={RelativeSource TemplatedParent}}"/>
</ControlTemplate>
```

`TemplateBinding` 是 `Binding` 的优化版本，只支持单向绑定且不能使用转换器。

## 8.8 深入：DataTemplate vs FuncDataTemplate

### DataTemplate（AXAML 声明式）

```xml
<DataTemplate x:DataType="vm:ProviderListItem">
    <Border Classes="provider-card">
        <TextBlock Text="{Binding Name}"/>
    </Border>
</DataTemplate>
```

特点：
- 声明式，易于维护
- 支持编译绑定
- 支持设计时预览
- 适合大多数场景

### FuncDataTemplate（代码式）

```csharp
// 创建 FuncDataTemplate
var template = new FuncDataTemplate<ProviderListItem>((item, _) =>
{
    return new Border
    {
        Classes = { "provider-card" },
        Child = new TextBlock
        {
            Text = item.Name
        }
    };
});

// 应用到 ItemsControl
itemsControl.ItemTemplate = template;
```

特点：
- 代码式，灵活
- 可以根据数据动态决定模板
- 不支持编译绑定
- 适合动态场景

### 何时使用 FuncDataTemplate

```csharp
// 场景 1：根据数据类型选择不同模板
var template = new FuncDataTemplate<object>((item, _) =>
{
    return item switch
    {
        ProviderModel => new ProviderTemplate(),
        UsageSnapshot => new UsageTemplate(),
        _ => new DefaultTemplate()
    };
});

// 场景 2：根据数据值动态调整 UI
var template = new FuncDataTemplate<UsageLogItem>((item, _) =>
{
    var border = new Border();
    if (item.EstimatedCost > 1.0m)
        border.Classes.Add("expensive");
    return border;
});
```

## 8.9 深入：虚拟化内部机制

### 已实现项与未实现项

虚拟化面板维护两个集合：

- **已实现项（Realized Items）**：当前在视口内可见的数据项，有对应的 UI 元素
- **未实现项（Unrealized Items）**：当前不在视口内的数据项，没有 UI 元素

```
视口（Viewport）
┌─────────────────────┐
│  已实现项 1          │ ← 有 UI 元素
│  已实现项 2          │ ← 有 UI 元素
│  已实现项 3          │ ← 有 UI 元素
└─────────────────────┘
│  未实现项 4          │ ← 无 UI 元素
│  未实现项 5          │ ← 无 UI 元素
│  ...                 │
```

### 项回收（Recycling）

当用户滚动时，虚拟化面板会回收离开视口的 UI 元素：

```
滚动前：
视口内：[项1, 项2, 项3, 项4, 项5]
回收池：[]

向下滚动 2 项：
视口内：[项3, 项4, 项5, 项6, 项7]
回收池：[项1的UI, 项2的UI]  ← 被回收

继续向下滚动：
视口内：[项5, 项6, 项7, 项8, 项9]
回收池：[项1的UI, 项2的UI, 项3的UI, 项4的UI]
        ↑ 可能被复用显示项8、项9
```

### VirtualizingStackPanel 的布局计算

```csharp
// 简化的布局逻辑
protected override Size MeasureOverride(Size availableSize)
{
    // 1. 计算视口范围
    var viewportStart = ScrollViewer.Offset.Y;
    var viewportEnd = viewportStart + ScrollViewer.Viewport.Height;

    // 2. 确定需要实现的项范围
    var startIndex = (int)(viewportStart / ItemHeight);
    var endIndex = Math.Min(Items.Count - 1,
        (int)(viewportEnd / ItemHeight));

    // 3. 实现可见范围内的项
    for (int i = startIndex; i <= endIndex; i++)
    {
        var container = GetOrCreateContainer(i);
        container.Measure(new Size(availableSize.Width, ItemHeight));
    }

    // 4. 回收不可见的项
    RecycleInvisibleContainers(startIndex, endIndex);

    // 5. 返回总尺寸
    return new Size(availableSize.Width, Items.Count * ItemHeight);
}
```

### ItemsSourceView（概念说明）

> **注意**：`ItemsSourceView` 是 WinUI 3 的 API，Avalonia 中没有这个类。Avalonia 直接使用 `IEnumerable`/`IList` 作为数据源，通过 `INotifyCollectionChanged` 接口实现集合变化通知。

Avalonia 的数据源抽象方式：

```csharp
// Avalonia 直接支持以下数据源类型：
// 1. IEnumerable — 基础数据源
// 2. IList — 支持索引访问
// 3. INotifyCollectionChanged — 集合变化通知（ObservableCollection<T>）
// 4. ICollectionView — 分组、排序、筛选视图

// 示例：使用 ObservableCollection 作为数据源
public class MyViewModel
{
    // 自动支持集合变化通知
    public ObservableCollection<ItemModel> Items { get; } = new();

    // 添加项时，绑定到此集合的 ItemsControl 自动更新
    public void AddItem(ItemModel item) => Items.Add(item);
}
```

它支持：
- `IList`、`IEnumerable`、`ObservableCollection` 等多种数据源
- 通过 `INotifyCollectionChanged` 实现集合变化通知
- 通过 `ICollectionView` 支持分组、排序、筛选

## 8.10 深入：虚拟化面板自定义

### 自定义 VirtualizingStackPanel

如果内置的 `VirtualizingStackPanel` 不能满足需求，可以自定义：

```csharp
public class CustomVirtualizingPanel : VirtualizingStackPanel
{
    // 自定义滚动逻辑
    protected override Size ArrangeOverride(Size finalSize)
    {
        // 自定义项排列逻辑
        var offset = 0d;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(0, offset, finalSize.Width, child.DesiredSize.Height));
            offset += child.DesiredSize.Height + ItemSpacing;
        }
        return finalSize;
    }

    // 自定义虚拟化策略
    protected override IInputElement? GetControl(NavigationDirection direction, ...)
    {
        // 自定义键盘导航逻辑
        return base.GetControl(direction, from, wrap);
    }
}
```

### 实现自定义滚动容器

```csharp
public class CustomScrollViewer : ScrollViewer
{
    protected override Size MeasureOverride(Size availableSize)
    {
        // 自定义测量逻辑
        // 例如：支持平滑滚动、惯性滚动等
        return base.MeasureOverride(availableSize);
    }

    protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        // 自定义滚动事件处理
        // 例如：懒加载、预加载等
        base.OnScrollChanged(e);
    }
}
```

## 8.11 深入：模板中的选择与交互

### 在 DataTemplate 中实现选择

```xml
<ListBox ItemsSource="{Binding Providers}">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <Border Classes="provider-card">
                <!-- 通过 PseudoClass 实现选中状态样式 -->
                <Border.Styles>
                    <Style Selector="Border.selected">
                        <Setter Property="Background"
                                Value="{StaticResource CsProviderCardActiveBrush}"/>
                    </Style>
                </Border.Styles>
                <TextBlock Text="{Binding Name}"/>
            </Border>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### 选择状态与容器同步

```csharp
// 在代码中同步选择状态
public class ProviderListBox : ListBox
{
    protected override void PrepareContainerForItemOverride(
        Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (item is ProviderListItem provider)
        {
            container.Classes.Add("provider-item");
            if (provider.IsActive)
                container.Classes.Add("active");
        }
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        container.Classes.Remove("provider-item");
        container.Classes.Remove("active");
    }
}
```

### 多选与批量操作

```xml
<ListBox SelectionMode="Multiple"
         ItemsSource="{Binding Providers}">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <StackPanel Orientation="Horizontal">
                <CheckBox IsChecked="{Binding IsSelected, Mode=TwoWay}"/>
                <TextBlock Text="{Binding Name}"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

## 8.12 深入：DataTemplate 性能优化

### 减少视觉树复杂度

```xml
<!-- 差：深层嵌套的视觉树 -->
<DataTemplate>
    <Border>
        <Grid>
            <StackPanel>
                <Border>
                    <TextBlock/>
                </Border>
            </StackPanel>
        </Grid>
    </Border>
</DataTemplate>

<!-- 好：扁平化的视觉树 -->
<DataTemplate>
    <Border>
        <TextBlock/>
    </Border>
</DataTemplate>
```

### 使用 x:DataType 启用编译绑定

```xml
<!-- 差：无 x:DataType，使用反射绑定 -->
<DataTemplate>
    <TextBlock Text="{Binding Name}"/>
</DataTemplate>

<!-- 好：有 x:DataType，使用编译绑定 -->
<DataTemplate x:DataType="vm:ProviderListItem">
    <TextBlock Text="{Binding Name}"/>
</DataTemplate>
```

### 合理使用虚拟化

```xml
<!-- 差：小集合使用虚拟化（有额外开销） -->
<ItemsControl ItemsSource="{Binding SmallCollection}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>

<!-- 好：小集合使用普通面板 -->
<ItemsControl ItemsSource="{Binding SmallCollection}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>

<!-- 好：大集合使用虚拟化 -->
<ItemsControl ItemsSource="{Binding LargeCollection}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

### 避免在 DataTemplate 中创建复杂对象

```xml
<!-- 差：在 DataTemplate 中创建复杂的转换器 -->
<DataTemplate>
    <TextBlock Text="{Binding Price,
        Converter={StaticResource PriceFormatConverter},
        ConverterParameter={StaticResource CurrencyFormat}}"/>
</DataTemplate>

<!-- 好：在 ViewModel 中预格式化 -->
<DataTemplate x:DataType="vm:ProductItem">
    <TextBlock Text="{Binding FormattedPrice}"/>
</DataTemplate>
```

## 8.13 跨引用

- **样式系统**：DataTemplate 中的样式参见 [第 7 章](07-styling-theming.md)
- **自定义控件**：ItemsControl 和 ContentControl 的模板机制参见 [第 9 章](09-custom-controls.md)
- **数据绑定**：绑定表达式的详细语法参见 [第 5 章](05-data-binding.md)
- **MVVM 模式**：ViewModel 与 DataTemplate 的配合参见 [第 6 章](06-mvvm-pattern.md)
- **性能优化**：虚拟化和渲染性能参见 [第 14 章](14-custom-rendering.md)

## 8.14 常见陷阱

### 陷阱 1：忘记设置 x:DataType

```xml
<!-- 问题：没有 x:DataType，绑定使用反射，性能差且无编译时检查 -->
<DataTemplate>
    <TextBlock Text="{Binding Name}"/>
    <!-- 如果 ProviderListItem 没有 Name 属性，运行时才会发现错误 -->
</DataTemplate>

<!-- 解决：总是设置 x:DataType -->
<DataTemplate x:DataType="vm:ProviderListItem">
    <TextBlock Text="{Binding Name}"/>
    <!-- 编译时检查 ProviderListItem 是否有 Name 属性 -->
</DataTemplate>
```

### 陷阱 2：在 DataTemplate 中访问父级 DataContext

```xml
<!-- 问题：直接绑定到父级 ViewModel 的属性 -->
<DataTemplate x:DataType="vm:ItemModel">
    <!-- 错误：这里的 DataContext 是 ItemModel，不是 MainWindowViewModel -->
    <Button Command="{Binding DeleteCommand}"/>
</DataTemplate>

<!-- 解决：使用 #ElementName 或 x:Reference -->
<DataTemplate x:DataType="vm:ItemModel">
    <Button Command="{Binding #Root.DataContext.DeleteCommand}"/>
</DataTemplate>
```

### 陷阱 3：虚拟化与 ItemsPanel

```xml
<!-- 问题：设置了 VirtualizingStackPanel 但没有在 ItemsControl.ItemsPanel 中 -->
<ItemsControl ItemsSource="{Binding LargeCollection}">
    <VirtualizingStackPanel/>  <!-- 错误：这不是 ItemsPanel -->
</ItemsControl>

<!-- 正确：在 ItemsPanelTemplate 中使用 -->
<ItemsControl ItemsSource="{Binding LargeCollection}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

### 陷阱 4：DataTemplate 中的事件处理

```xml
<!-- 问题：在 DataTemplate 中直接绑定事件 -->
<DataTemplate x:DataType="vm:ItemModel">
    <Button Click="Button_Click"/>  <!-- 错误：事件处理在哪个类？ -->
</DataTemplate>

<!-- 解决：使用 Command 绑定 -->
<DataTemplate x:DataType="vm:ItemModel">
    <Button Command="{Binding ClickCommand}"/>
</DataTemplate>
```

### 陷阱 5：嵌套 DataTemplate 的绑定上下文

```xml
<!-- 问题：嵌套模板中 DataContext 是内层数据 -->
<ItemsControl ItemsSource="{Binding Orders}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:OrderModel">
            <ItemsControl ItemsSource="{Binding Items}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate x:DataType="vm:OrderItemModel">
                        <!-- 这里的 DataContext 是 OrderItemModel -->
                        <TextBlock Text="{Binding OrderId}"/>
                        <!-- 错误：OrderItemModel 可能没有 OrderId -->
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<!-- 解决：使用相对绑定或命名元素 -->
<DataTemplate x:DataType="vm:OrderModel">
    <ItemsControl ItemsSource="{Binding Items}">
        <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="vm:OrderItemModel">
                <!-- 使用祖先绑定 -->
                <TextBlock Text="{Binding $parent[ItemsControl].DataContext.OrderId}"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</DataTemplate>
```

### 陷阱 6：DataTemplate 中使用 StaticResource 导致主题不切换

```xml
<!-- 问题：StaticResource 在编译时解析，主题切换时不更新 -->
<DataTemplate x:DataType="vm:ProviderListItem">
    <Border Background="{StaticResource CsCardBrush}">
        <TextBlock Text="{Binding Name}"/>
    </Border>
</DataTemplate>

<!-- 解决：如果需要响应主题切换，使用 DynamicResource -->
<DataTemplate x:DataType="vm:ProviderListItem">
    <Border Background="{DynamicResource CsCardBrush}">
        <TextBlock Text="{Binding Name}"/>
    </Border>
</DataTemplate>
```

注意：CodexSwitch 通过就地修改 `SolidColorBrush.Color` 实现主题切换，所以 `StaticResource` 也能正常工作。

### 陷阱 7：ContentControl 的 Content 与 ContentTemplate 混淆

```xml
<!-- 问题：ContentTemplate 设置了模板但 Content 没有设置数据 -->
<ContentControl ContentTemplate="{StaticResource MyTemplate}"/>
<!-- 没有 Content，模板不会显示任何内容 -->

<!-- 正确：必须同时设置 Content 和 ContentTemplate -->
<ContentControl Content="{Binding CurrentItem}"
                ContentTemplate="{StaticResource MyTemplate}"/>

<!-- 或者使用 DataTemplates 集合自动匹配 -->
<ContentControl Content="{Binding SelectedObject}">
    <ContentControl.DataTemplates>
        <DataTemplate x:DataType="vm:TypeA">...</DataTemplate>
        <DataTemplate x:DataType="vm:TypeB">...</DataTemplate>
    </ContentControl.DataTemplates>
</ContentControl>
```

### 陷阱 8：ItemsControl 默认没有滚动条

```xml
<!-- 问题：ItemsControl 默认不包含 ScrollViewer -->
<ItemsControl ItemsSource="{Binding LargeCollection}">
    <ItemsControl.ItemTemplate>...</ItemsControl.ItemTemplate>
</ItemsControl>
<!-- 如果项目太多，内容会被裁剪 -->

<!-- 解决：手动包装 ScrollViewer -->
<ScrollViewer>
    <ItemsControl ItemsSource="{Binding LargeCollection}">
        <ItemsControl.ItemTemplate>...</ItemsControl.ItemTemplate>
    </ItemsControl>
</ScrollViewer>

<!-- 或者使用 ListBox（自带滚动条） -->
<ListBox ItemsSource="{Binding LargeCollection}">
    <ListBox.ItemTemplate>...</ListBox.ItemTemplate>
</ListBox>
```

### 陷阱 9：DataTemplate 中的 ComboBox/Popup 定位问题

```xml
<!-- 问题：DataTemplate 中的 ComboBox 下拉列表可能被裁剪 -->
<DataTemplate x:DataType="vm:ItemModel">
    <Border ClipToBounds="True">
        <ComboBox ItemsSource="{Binding Options}"/>
    </Border>
</DataTemplate>

<!-- 解决：避免在 DataTemplate 中使用 ClipToBounds -->
<!-- 或者使用 Overlay 属性让 Popup 显示在最上层 -->
```

### 陷阱 10：忘记处理 null 值

```xml
<!-- 问题：如果数据项为 null，绑定会失败 -->
<DataTemplate x:DataType="vm:ProviderListItem">
    <TextBlock Text="{Binding Name}"/>
    <!-- 如果 ProviderListItem 为 null，会显示空 -->
</DataTemplate>

<!-- 解决：使用 FallbackValue 或 TargetNullValue -->
<DataTemplate x:DataType="vm:ProviderListItem">
    <TextBlock Text="{Binding Name, FallbackValue='Unknown'}"/>
</DataTemplate>
```

### 陷阱 11：ItemsPanelTemplate 中使用错误的面板类型

```xml
<!-- 问题：在 ItemsPanelTemplate 中使用普通 StackPanel，失去虚拟化 -->
<ItemsControl ItemsSource="{Binding LargeCollection}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel/>  <!-- 没有虚拟化，10000 项会很卡 -->
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>

<!-- 正确：大集合使用 VirtualizingStackPanel -->
<ItemsControl ItemsSource="{Binding LargeCollection}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

## 8.15 动手练习

### 练习 1：为 CodexSwitch 添加新的 DataTemplate

在 `MainWindow.axaml` 中为 `ModelCatalogItem` 创建一个新的 DataTemplate：

```xml
<DataTemplate x:DataType="vm:ModelCatalogItem">
    <ui:CsCard Padding="12">
        <StackPanel>
            <TextBlock Text="{Binding ModelName}" FontWeight="SemiBold"/>
            <TextBlock Text="{Binding ProviderName}"
                       Foreground="{StaticResource CsMutedForegroundBrush}"/>
            <StackPanel Orientation="Horizontal" Spacing="8">
                <TextBlock Text="{Binding InputPrice, StringFormat='{}${0:F4}/1K'}"/>
                <TextBlock Text="{Binding OutputPrice, StringFormat='{}${0:F4}/1K'}"/>
            </StackPanel>
        </StackPanel>
    </ui:CsCard>
</DataTemplate>
```

### 练习 2：实现 FuncDataTemplate

创建一个根据使用量显示不同颜色的 FuncDataTemplate：

```csharp
var template = new FuncDataTemplate<UsageLogItem>((item, _) =>
{
    var border = new Border
    {
        Padding = new Thickness(8),
        CornerRadius = new CornerRadius(4)
    };

    // 根据使用量设置背景色
    if (item.TotalTokens > 100000)
        border.Background = new SolidColorBrush(Colors.Red, 0.2);
    else if (item.TotalTokens > 10000)
        border.Background = new SolidColorBrush(Colors.Yellow, 0.2);
    else
        border.Background = new SolidColorBrush(Colors.Green, 0.2);

    border.Child = new TextBlock
    {
        Text = $"{item.ModelName}: {item.TotalTokens:N0} tokens"
    };

    return border;
});
```

### 练习 3：调试虚拟化

1. 创建一个包含 1000 个项目的 `ListBox`
2. 使用 DevTools 查看可视化树
3. 观察只有可见区域的项目有 UI 元素
4. 滚动列表，观察 UI 元素的创建和回收

### 练习 4：性能对比测试

1. 创建两个 `ItemsControl`，分别使用 `StackPanel` 和 `VirtualizingStackPanel`
2. 绑定到包含 10000 个项目的集合
3. 测量初始加载时间和内存占用
4. 测试滚动流畅度

### 练习 5：使用 DataTemplates 集合实现多类型显示

创建一个 `ContentControl`，根据绑定的数据类型自动选择不同的 DataTemplate：

```xml
<ContentControl Content="{Binding SelectedItem}">
    <ContentControl.DataTemplates>
        <DataTemplate x:DataType="vm:ProviderModel">
            <Border Background="{StaticResource CsCardBrush}" Padding="16">
                <StackPanel>
                    <TextBlock Text="{Binding ProviderName}" FontWeight="SemiBold"/>
                    <TextBlock Text="{Binding Endpoint}"/>
                </StackPanel>
            </Border>
        </DataTemplate>
        <DataTemplate x:DataType="vm:UsageSnapshot">
            <Border Background="{StaticResource CsCardBrush}" Padding="16">
                <StackPanel>
                    <TextBlock Text="使用统计" FontWeight="SemiBold"/>
                    <TextBlock Text="{Binding TotalTokens, StringFormat='{}{0:N0} tokens'}"/>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ContentControl.DataTemplates>
</ContentControl>
```

### 练习 6：实现带选择状态的 DataTemplate

创建一个 ListBox，DataTemplate 中根据选中状态显示不同的样式：

```xml
<ListBox ItemsSource="{Binding Providers}"
         SelectedItem="{Binding SelectedProvider}">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <Border Padding="12" CornerRadius="8">
                <StackPanel>
                    <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                    <TextBlock Text="{Binding Endpoint}"
                               Foreground="{StaticResource CsMutedForegroundBrush}"/>
                </StackPanel>
            </Border>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

然后在样式中处理选中状态：

```xml
<Style Selector="ListBoxItem:selected /template/ ContentPresenter">
    <Setter Property="Background" Value="{StaticResource CsSelectedBrush}"/>
</Style>
```

### 练习 7：创建嵌套 DataTemplate

实现一个两级列表：提供商 -> 模型列表：

```xml
<ItemsControl ItemsSource="{Binding Providers}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderModel">
            <Expander Header="{Binding ProviderName}">
                <ItemsControl ItemsSource="{Binding Models}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate x:DataType="vm:ModelInfo">
                            <TextBlock Text="{Binding ModelName}" Margin="16,4,0,4"/>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </Expander>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 练习 8：使用 FuncDataTemplate 实现条件渲染

根据数据值动态决定渲染方式：

```csharp
var template = new FuncDataTemplate<UsageLogItem>((item, _) =>
{
    var border = new Border
    {
        Padding = new Thickness(8),
        CornerRadius = new CornerRadius(4)
    };

    if (item.EstimatedCost > 1.0m)
    {
        border.Classes.Add("expensive");
        border.Background = new SolidColorBrush(Colors.Red, 0.1);
    }
    else if (item.EstimatedCost > 0.1m)
    {
        border.Classes.Add("moderate");
        border.Background = new SolidColorBrush(Colors.Yellow, 0.1);
    }

    border.Child = new TextBlock
    {
        Text = $"{item.ModelName}: ${item.EstimatedCost:F4}"
    };

    return border;
});
```

### 练习 9：实现带拖拽排序的 DataTemplate

为 DataTemplate 添加拖拽手柄，实现列表重排序：

```xml
<DataTemplate x:DataType="vm:ProviderListItem">
    <Grid ColumnDefinitions="Auto,*">
        <PathIcon Data="M10 4h4v4h-4zM10 16h4v4h-4z"
                  Width="12" Height="12"
                  Classes="drag-handle"
                  Cursor="SizeAll"
                  VerticalAlignment="Center"/>
        <StackPanel Grid.Column="1" Margin="8,0,0,0">
            <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
            <TextBlock Text="{Binding Endpoint}"
                       Foreground="{StaticResource CsMutedForegroundBrush}"/>
        </StackPanel>
    </Grid>
</DataTemplate>
```

### 练习 10：性能优化——减少 DataTemplate 中的视觉树深度

重构一个深层嵌套的 DataTemplate，将其扁平化：

```xml
<!-- 优化前：5 层嵌套 -->
<DataTemplate>
    <Border>
        <Grid>
            <StackPanel>
                <Border>
                    <TextBlock Text="{Binding Name}"/>
                </Border>
            </StackPanel>
        </Grid>
    </Border>
</DataTemplate>

<!-- 优化后：2 层嵌套 -->
<DataTemplate>
    <Border Padding="8">
        <TextBlock Text="{Binding Name}"/>
    </Border>
</DataTemplate>
```

## Cross References

- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 DataTemplate 中的 x:DataType 和绑定语法
- **[第 5 章：数据绑定](05-data-binding.md)** — 理解 DataTemplate 中的绑定模式和编译绑定
- **[第 6 章：MVVM 模式](06-mvvm-pattern.md)** — 了解 ViewModel 如何为 DataTemplate 提供数据
- **[第 7 章：样式与主题](07-styling-theming.md)** — 了解 DataTemplate 中的样式和资源引用
- **[第 9 章：自定义控件](09-custom-controls.md)** — 掌握 ItemsControl 和 ContentControl 的模板机制
- **[第 13 章：拖拽交互](13-drag-drop.md)** — 了解 DataTemplate 中的拖拽手柄实现
