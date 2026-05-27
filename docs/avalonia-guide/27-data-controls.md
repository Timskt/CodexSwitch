# 27. 数据展示控件

> **写给零基础的你**：数据展示控件就是"把一堆数据变成好看的样子"。就像 Excel 表格把数字排成行列，TreeView 把文件夹展开成树形。本章教你用 Avalonia 的数据控件来展示各种数据。这些控件是构建任何数据密集型应用的核心。

## 27.1 概述

数据展示控件是构建数据密集型应用的核心。Avalonia 提供了丰富的数据控件，从简单的列表到复杂的树形表格，都支持数据绑定、虚拟化和自定义模板。CodexSwitch 大量使用这些控件来展示 Provider 列表、模型信息和使用数据。

学完本章后，你将能够：
- 掌握 ListBox 和 ComboBox 的所有属性和高级用法
- 掌握 TreeView 和 HierarchicalDataTemplate 的用法
- 掌握 DataGrid 的列类型、排序、过滤和虚拟化
- 理解虚拟化原理和性能优化
- 理解集合视图（ICollectionView）的排序和筛选

## 27.2 核心概念

### 27.2.1 数据控件层次

```
ItemsControl                    # 最基础的集合控件（不可选择）
├── ListBox                     # 可选择的列表
├── ComboBox                    # 下拉选择框
├── Menu / MenuItem             # 菜单
├── TreeView / TreeViewItem     # 树形视图
├── TabControl / TabItem        # 选项卡
└── DataGrid                    # 数据表格（需要额外 NuGet 包）
```

所有这些控件都继承自 ItemsControl，共享以下核心概念：
- **ItemsSource**：数据源绑定
- **ItemTemplate**：项目模板
- **ItemsPanel**：布局面板（默认 StackPanel）
- **虚拟化**：只渲染可见项

### 27.2.2 ListBox 详解

ListBox 是最常用的可选择列表控件。它继承自 ItemsControl，增加了选择功能。

**什么时候用 ListBox？**
- 需要让用户从列表中选择一个或多个项目时
- 需要显示一个可滚动的项目列表时
- 不需要下拉框的交互方式时

#### ListBox 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SelectedItem` | object | null | 当前选中项 |
| `SelectedItems` | IList | null | 多选时的选中项集合 |
| `SelectedIndex` | int | -1 | 当前选中索引 |
| `SelectionMode` | SelectionMode | Single | 选择模式（Single/Multiple/Extended） |
| `ItemsSource` | IEnumerable | null | 数据源 |
| `ItemTemplate` | DataTemplate | null | 项目模板 |
| `DisplayMemberPath` | string | null | 显示成员路径（简单场景替代 ItemTemplate） |
| `VirtualizingStackPanel.IsVirtualizing` | bool | true | 是否启用虚拟化 |
| `VirtualizingStackPanel.VirtualizationMode` | VirtualizationMode | Recycling | 虚拟化模式 |
| `ScrollViewer.HorizontalScrollBarVisibility` | ScrollBarVisibility | Disabled | 水平滚动条 |

#### ListBox 的事件

| 事件 | 说明 |
|------|------|
| `SelectionChanged` | 选中项改变时触发 |

#### SelectionMode 选择模式

| 模式 | 说明 | 操作方式 |
|------|------|---------|
| `Single` | 单选 | 点击选择，只能选一个 |
| `Multiple` | 多选 | 点击切换选择，可选多个 |
| `Extended` | 延伸选择 | Ctrl+Click 多选，Shift+Click 范围选择 |

#### 示例 1：基本 ListBox

```xml
<ListBox ItemsSource="{Binding Items}"
         SelectedItem="{Binding SelectedItem, Mode=TwoWay}">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:ItemViewModel">
            <StackPanel>
                <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                <TextBlock Text="{Binding Description}" Foreground="Gray"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

#### 示例 2：多选模式

```xml
<!-- Multiple 模式：点击切换选择 -->
<ListBox ItemsSource="{Binding Items}"
         SelectionMode="Multiple"
         SelectedItems="{Binding SelectedItems}"/>

<!-- Extended 模式：Ctrl+Click 多选，Shift+Click 范围选择 -->
<ListBox ItemsSource="{Binding Items}"
         SelectionMode="Extended"
         SelectedItems="{Binding SelectedItems}"/>
```

```csharp
// ViewModel 中处理多选
public ObservableCollection<ItemViewModel> SelectedItems { get; } = new();

// 获取所有选中的项
public IEnumerable<ItemViewModel> GetSelectedItems()
{
    return SelectedItems.Cast<ItemViewModel>();
}
```

#### 示例 3：使用 DisplayMemberPath（简单场景）

```xml
<!-- 不需要自定义模板时，用 DisplayMemberPath 简化 -->
<ListBox ItemsSource="{Binding Providers}"
         DisplayMemberPath="Name"
         SelectedItem="{Binding SelectedProvider, Mode=TwoWay}"/>
```

这等价于：
```xml
<ListBox ItemsSource="{Binding Providers}"
         SelectedItem="{Binding SelectedProvider, Mode=TwoWay}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}"/>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

#### 示例 4：自定义选中项样式

```xml
<Window.Styles>
    <!-- 自定义 ListBoxItem 选中样式 -->
    <Style Selector="ListBoxItem:selected /template/ ContentPresenter">
        <Setter Property="Background" Value="{StaticResource CsPrimaryBrush}"/>
    </Style>
    <!-- 悬停样式 -->
    <Style Selector="ListBoxItem:pointerover /template/ ContentPresenter">
        <Setter Property="Background" Value="{StaticResource CsSecondaryBrush}"/>
    </Style>
</Window.Styles>
```

#### 示例 5：ListBox 中使用 WrapPanel 布局

```xml
<!-- 将 ListBox 的 ItemsPanel 替换为 WrapPanel，实现标签式布局 -->
<ListBox ItemsSource="{Binding Tags}"
         SelectionMode="Multiple"
         SelectedItems="{Binding SelectedTags}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel/>
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <Border Background="{StaticResource CsSecondaryBrush}"
                    CornerRadius="4" Padding="8,4" Margin="2">
                <TextBlock Text="{Binding}"/>
            </Border>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

#### 示例 6：ListBox 水平滚动

```xml
<ListBox ItemsSource="{Binding LongItems}"
         ScrollViewer.HorizontalScrollBarVisibility="Auto"
         ScrollViewer.VerticalScrollBarVisibility="Disabled">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal"/>
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>
```

### 27.2.3 ComboBox 详解

ComboBox 是下拉选择框控件，默认只显示当前选中项，点击后展开下拉列表。

**什么时候用 ComboBox？**
- 选项较多，不适合全部显示在界面上时
- 需要节省界面空间时
- 选项数量固定或动态加载时

#### ComboBox 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SelectedItem` | object | null | 选中项 |
| `SelectedIndex` | int | -1 | 选中索引 |
| `SelectedValue` | object | null | 选中值（通过 SelectedValueBinding 获取） |
| `SelectedValueBinding` | IBinding | null | 从选中项中提取值的绑定 |
| `IsDropDownOpen` | bool | false | 下拉是否打开 |
| `PlaceholderText` | string | null | 占位文本（未选中时显示） |
| `IsTextSearchEnabled` | bool | false | 是否启用文本搜索 |
| `MaxDropDownHeight` | double | 200 | 下拉最大高度 |
| `IsEditable` | bool | false | 是否可编辑（允许输入自定义文本） |
| `Text` | string | null | 可编辑模式下的文本 |
| `ItemsSource` | IEnumerable | null | 数据源 |
| `ItemTemplate` | DataTemplate | null | 下拉项模板 |
| `SelectionBoxItemTemplate` | DataTemplate | null | 选中项显示模板 |
| `DisplayMemberPath` | string | null | 显示成员路径 |

#### ComboBox 的事件

| 事件 | 说明 |
|------|------|
| `SelectionChanged` | 选中项改变时触发 |
| `DropDownOpened` | 下拉打开时触发 |
| `DropDownClosed` | 下拉关闭时触发 |

#### 示例 1：基本 ComboBox

```xml
<ComboBox ItemsSource="{Binding Options}"
          SelectedItem="{Binding SelectedOption, Mode=TwoWay}"
          PlaceholderText="请选择一个选项"/>
```

#### 示例 2：使用 DisplayMemberPath

```xml
<ComboBox ItemsSource="{Binding Providers}"
          DisplayMemberPath="Name"
          SelectedItem="{Binding SelectedProvider, Mode=TwoWay}"
          PlaceholderText="选择 Provider"/>
```

#### 示例 3：使用 ItemTemplate 自定义显示

```xml
<ComboBox ItemsSource="{Binding Providers}"
          SelectedItem="{Binding SelectedProvider, Mode=TwoWay}">
    <ComboBox.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderModel">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <PathIcon Data="{Binding Icon}" Width="16" Height="16"/>
                <TextBlock Text="{Binding Name}"/>
                <TextBlock Text="{Binding Protocol}" Foreground="Gray"/>
            </StackPanel>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

#### 示例 4：可编辑的 ComboBox

```xml
<!-- IsEditable 允许用户输入自定义文本 -->
<ComboBox ItemsSource="{Binding Presets}"
          IsEditable="True"
          Text="{Binding CustomValue, Mode=TwoWay}"
          PlaceholderText="输入或选择一个预设"/>
```

#### 示例 5：使用 SelectedValueBinding

```xml
<!-- SelectedValue 绑定到对象的某个属性，而不是整个对象 -->
<ComboBox ItemsSource="{Binding Countries}"
          DisplayMemberPath="Name"
          SelectedValue="{Binding CountryCode, Mode=TwoWay}"
          SelectedValueBinding="{Binding Code}"/>
```

```csharp
public class Country
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = ""; // "CN", "US", "JP" 等
}

// ViewModel 中
public string CountryCode { get; set; } = "CN"; // 绑定的是 Code，不是整个 Country 对象
```

#### 示例 6：CodexSwitch 中的 ComboBox 使用

CodexSwitch 使用自定义的 CodexSelect 控件（类似于 ComboBox）：

```xml
<ui:CodexSelect Classes="provider-model-select"
                ItemsSource="{Binding DefaultModelOptions}"
                SelectedItem="{Binding DefaultModel, Mode=TwoWay}"
                IsEnabled="{Binding CanChangeDefaultModel}"
                ToolTip.Tip="{i18n:Tr providers.defaultModel}">
    <ui:CodexSelect.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding}" TextTrimming="CharacterEllipsis"/>
        </DataTemplate>
    </ui:CodexSelect.ItemTemplate>
    <ui:CodexSelect.SelectionBoxItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding}" TextTrimming="CharacterEllipsis"/>
        </DataTemplate>
    </ui:CodexSelect.SelectionBoxItemTemplate>
</ui:CodexSelect>
```

### 27.2.4 TreeView 详解

TreeView 展示层次化的树形数据，如文件系统、组织架构等。

**什么是 TreeView？**
TreeView 由 TreeViewItem 组成，每个 TreeViewItem 可以包含子 TreeViewItem，形成树形结构。用户可以展开/折叠节点来查看子节点。

#### TreeView 的所有属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `ItemsSource` | IEnumerable | 数据源 |
| `SelectedItem` | object | 当前选中项 |
| `AutoDragDrop` | bool | 是否启用拖放 |
| `VirtualizingStackPanel.IsVirtualizing` | bool | 是否启用虚拟化 |

#### TreeViewItem 的所有属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsExpanded` | bool | 是否展开 |
| `IsSelected` | bool | 是否选中 |
| `ItemsSource` | IEnumerable | 子节点数据源 |
| `Header` | object | 头部内容 |

#### TreeDataTemplate

TreeDataTemplate 是 TreeView 专用的 DataTemplate，它通过 `ItemsSource` 指定子节点的数据源。

#### 示例 1：基本 TreeView

```xml
<TreeView ItemsSource="{Binding RootNodes}"
          SelectedItem="{Binding SelectedNode, Mode=TwoWay}">
    <TreeView.ItemTemplate>
        <TreeDataTemplate x:DataType="vm:TreeNodeViewModel"
                          ItemsSource="{Binding Children}">
            <TextBlock Text="{Binding Name}"/>
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

```csharp
// 节点 ViewModel
public class TreeNodeViewModel : ObservableObject
{
    public string Name { get; set; } = "";
    public ObservableCollection<TreeNodeViewModel> Children { get; } = new();
    public bool IsExpanded { get; set; } = true;
    public bool IsSelected { get; set; }
}
```

#### 示例 2：带图标的 TreeView

```xml
<TreeView ItemsSource="{Binding RootNodes}"
          SelectedItem="{Binding SelectedNode, Mode=TwoWay}">
    <TreeView.ItemTemplate>
        <TreeDataTemplate x:DataType="vm:TreeNodeViewModel"
                          ItemsSource="{Binding Children}">
            <StackPanel Orientation="Horizontal" Spacing="6">
                <PathIcon Data="{Binding Icon}" Width="16" Height="16"/>
                <TextBlock Text="{Binding Name}"/>
            </StackPanel>
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

#### 示例 3：控制节点展开状态

```xml
<TreeView ItemsSource="{Binding RootNodes}">
    <TreeView.ItemTemplate>
        <TreeDataTemplate x:DataType="vm:TreeNodeViewModel"
                          ItemsSource="{Binding Children}">
            <TreeViewItemTemplate.Styles>
                <Style Selector="TreeViewItem">
                    <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}"/>
                </Style>
            </TreeViewItemTemplate.Styles>
            <TextBlock Text="{Binding Name}"/>
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

#### 示例 4：异步加载子节点

```csharp
public class LazyTreeNodeViewModel : ObservableObject
{
    private bool _isLoaded;
    private ObservableCollection<LazyTreeNodeViewModel>? _children;

    public string Name { get; set; } = "";

    public ObservableCollection<LazyTreeNodeViewModel> Children
    {
        get
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                LoadChildrenAsync();
            }
            return _children ??= new();
        }
    }

    private async void LoadChildrenAsync()
    {
        var children = await FetchChildrenFromServerAsync();
        foreach (var child in children)
            _children!.Add(child);
    }
}
```

### 27.2.5 DataGrid 详解

DataGrid 是功能最强大的数据表格控件，支持列排序、筛选、分组、编辑等。

**安装：**
```xml
<!-- 在 .csproj 中添加 -->
<PackageReference Include="Avalonia.Controls.DataGrid" />

<!-- 在 App.axaml 中添加主题 -->
<Application.Styles>
    <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"/>
</Application.Styles>
```

#### DataGrid 的所有属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ItemsSource` | IEnumerable | null | 数据源 |
| `AutoGenerateColumns` | bool | false | 是否自动生成列 |
| `IsReadOnly` | bool | false | 是否只读 |
| `CanUserResizeColumns` | bool | true | 用户是否可以调整列宽 |
| `CanUserSortColumns` | bool | true | 用户是否可以排序 |
| `CanUserReorderColumns` | bool | true | 用户是否可以拖拽调整列顺序 |
| `SelectionMode` | DataGridSelectionMode | Extended | 选择模式 |
| `GridLinesVisibility` | DataGridGridLinesVisibility | None | 网格线可见性 |
| `HorizontalGridLinesBrush` | IBrush | null | 水平网格线颜色 |
| `VerticalGridLinesBrush` | IBrush | null | 垂直网格线颜色 |
| `SelectedItem` | object | null | 选中项 |
| `SelectedItems` | IList | null | 多选时的选中项集合 |
| `AreRowDetailsFrozen` | bool | false | 行详情是否固定 |
| `RowDetailsVisibilityMode` | DataGridRowDetailsVisibilityMode | VisibleWhenSelected | 行详情显示模式 |
| `FrozenColumnCount` | int | 0 | 冻结列数 |
| `MaxColumnWidth` | double | NaN | 最大列宽 |
| `MinColumnWidth` | double | 20 | 最小列宽 |
| `RowHeight` | double | NaN | 行高 |
| `HeadersVisibility` | DataGridHeadersVisibility | Column | 表头可见性 |
| `ColumnWidth` | DataGridLength | SizeToHeader | 默认列宽 |

#### DataGrid 列类型

| 列类型 | 说明 | 适用数据类型 | 关键属性 |
|--------|------|-------------|---------|
| `DataGridTextColumn` | 文本列 | string, int, double | Binding, FontSize |
| `DataGridCheckBoxColumn` | 复选框列 | bool | Binding, IsThreeState |
| `DataGridComboBoxColumn` | 下拉选择列 | 枚举、选项列表 | ItemsSource, SelectedValueBinding |
| `DataGridTemplateColumn` | 自定义模板列 | 任意类型 | CellTemplate, CellEditingTemplate |
| `DataGridHyperlinkColumn` | 超链接列 | Uri, string | Binding |

#### DataGridGridLinesVisibility 枚举

| 值 | 说明 |
|---|------|
| `None` | 不显示网格线 |
| `Horizontal` | 只显示水平线 |
| `Vertical` | 只显示垂直线 |
| `All` | 显示所有网格线 |

#### 示例 1：基本 DataGrid

```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="False"
          IsReadOnly="True"
          CanUserResizeColumns="True"
          CanUserSortColumns="True"
          GridLinesVisibility="Horizontal"
          SelectionMode="Extended"
          SelectedItem="{Binding SelectedItem, Mode=TwoWay}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="200"/>
        <DataGridTextColumn Header="URL" Binding="{Binding BaseUrl}" Width="*"/>
        <DataGridCheckBoxColumn Header="启用" Binding="{Binding IsActive}"/>
        <DataGridComboBoxColumn Header="类型"
                                ItemsSource="{Binding ProviderTypes}"
                                SelectedValueBinding="{Binding Type}"/>
    </DataGrid.Columns>
</DataGrid>
```

#### 示例 2：使用 DataGridTemplateColumn 自定义列

```xml
<DataGrid ItemsSource="{Binding Items}" AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTemplateColumn Header="操作" Width="120">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate x:DataType="vm:ProviderModel">
                    <StackPanel Orientation="Horizontal" Spacing="4">
                        <Button Content="编辑" Command="{Binding EditCommand}"
                                Classes="small"/>
                        <Button Content="删除" Command="{Binding DeleteCommand}"
                                Classes="small destructive"/>
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

#### 示例 3：可编辑的 DataGrid

```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="False"
          IsReadOnly="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="名称" Binding="{Binding Name}"/>
        <DataGridTextColumn Header="端口" Binding="{Binding Port}"/>
        <DataGridCheckBoxColumn Header="启用" Binding="{Binding IsEnabled}"/>
    </DataGrid.Columns>
</DataGrid>
```

#### 示例 4：带行详情的 DataGrid

```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="False"
          RowDetailsVisibilityMode="VisibleWhenSelected">
    <DataGrid.Columns>
        <DataGridTextColumn Header="名称" Binding="{Binding Name}"/>
        <DataGridTextColumn Header="URL" Binding="{Binding BaseUrl}"/>
    </DataGrid.Columns>
    <DataGrid.RowDetailsTemplate>
        <DataTemplate x:DataType="vm:ProviderModel">
            <StackPanel Margin="16" Spacing="8">
                <TextBlock Text="{Binding Description}" TextWrapping="Wrap"/>
                <TextBlock Text="{Binding ApiKey, StringFormat='API Key: {0}'}"/>
            </StackPanel>
        </DataTemplate>
    </DataGrid.RowDetailsTemplate>
</DataGrid>
```

#### 示例 5：冻结列

```xml
<DataGrid FrozenColumnCount="2" AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="名称" Binding="{Binding Name}"/>  <!-- 冻结 -->
        <DataGridTextColumn Header="类型" Binding="{Binding Type}"/>  <!-- 冻结 -->
        <DataGridTextColumn Header="URL" Binding="{Binding Url}"/>
        <DataGridTextColumn Header="描述" Binding="{Binding Description}"/>
    </DataGrid.Columns>
</DataGrid>
```

### 27.2.6 集合视图排序和筛选

使用 `DataGridCollectionView` 或手动实现排序和筛选逻辑：

#### 示例 1：使用 DataGridCollectionView

```csharp
// 在 ViewModel 中创建带排序和筛选的集合视图
public DataGridCollectionView ProvidersView { get; }

public MainWindowViewModel()
{
    var providers = new ObservableCollection<ProviderModel>(_providerService.GetAll());
    ProvidersView = new DataGridCollectionView(providers);

    // 添加排序
    ProvidersView.SortDescriptions.Add(
        new SortDescription("Name", ListSortDirection.Ascending));

    // 添加筛选
    ProvidersView.Filter = item =>
    {
        var provider = (ProviderModel)item;
        return provider.IsActive || ShowInactive;
    };
}
```

#### 示例 2：动态排序

```csharp
[RelayCommand]
private void SortBy(string propertyName)
{
    ProvidersView.SortDescriptions.Clear();
    var direction = _sortAscending
        ? ListSortDirection.Ascending
        : ListSortDirection.Descending;
    ProvidersView.SortDescriptions.Add(new SortDescription(propertyName, direction));
    _sortAscending = !_sortAscending;
}
```

#### 示例 3：动态筛选

```csharp
[ObservableProperty]
private string _filterText = "";

partial void OnFilterTextChanged(string value)
{
    ProvidersView.Filter = item =>
    {
        var provider = (ProviderModel)item;
        return string.IsNullOrEmpty(value)
            || provider.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
            || provider.BaseUrl.Contains(value, StringComparison.OrdinalIgnoreCase);
    };
}
```

## 27.3 进阶用法

### 27.3.1 虚拟化原理

虚拟化是处理大量数据的关键技术。它只渲染屏幕上可见的项目，而不是全部项目。

**为什么需要虚拟化？**
假设你有 10000 条数据，每条数据的 UI 元素高度为 40px。如果不虚拟化，Avalonia 需要创建 10000 个 UI 元素，总高度 400000px。但实际上屏幕只能显示约 20 个元素。虚拟化只创建和渲染这 20 个可见元素，节省了 99.8% 的内存和渲染开销。

#### 虚拟化模式

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| `None` | 不虚拟化 | 少量数据（<100 条） |
| `Simple` | 简单虚拟化，滚动时创建新元素 | 大量数据 |
| `Recycling` | 回收虚拟化，重用已创建的元素 | 非常大的数据集（推荐） |

**Simple vs Recycling 的区别：**
- **Simple**：当新项目进入可视区域时创建新的 UI 元素，当项目离开可视区域时销毁 UI 元素。这会导致频繁的创建和销毁。
- **Recycling**：当新项目进入可视区域时，重用离开可视区域的 UI 元素，只更新其 DataContext。这避免了频繁的创建和销毁，性能更好。

#### 示例 1：为 ListBox 启用虚拟化

```xml
<!-- ListBox 默认启用虚拟化，但可以显式设置 -->
<ListBox ItemsSource="{Binding LargeCollection}"
         VirtualizingStackPanel.IsVirtualizing="True"
         VirtualizingStackPanel.VirtualizationMode="Recycling">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}"/>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

#### 示例 2：为 ItemsControl 启用虚拟化

```xml
<!-- ItemsControl 默认不启用虚拟化，需要手动开启 -->
<ItemsControl ItemsSource="{Binding LargeCollection}"
              VirtualizingStackPanel.IsVirtualizing="True">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

#### 示例 3：水平虚拟化

```xml
<!-- 水平虚拟化的 ListBox -->
<ListBox ItemsSource="{Binding Items}"
         ScrollViewer.HorizontalScrollBarVisibility="Auto"
         ScrollViewer.VerticalScrollBarVisibility="Disabled">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel Orientation="Horizontal"/>
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>
```

#### 示例 4：TreeDataGrid 虚拟化

```xml
<!-- TreeDataGrid 支持内置虚拟化 -->
<TreeDataGrid Source="{Binding TreeSource}"
              HorizontalAlignment="Stretch"/>
```

### 27.3.2 DataGrid 列宽模式

```xml
<!-- SizeToHeader：列宽适应表头文字 -->
<DataGridTextColumn Header="名称" Width="SizeToHeader"/>

<!-- SizeToCells：列宽适应内容 -->
<DataGridTextColumn Header="名称" Width="SizeToCells"/>

<!-- Auto：适应表头和内容中较长的 -->
<DataGridTextColumn Header="名称" Width="Auto"/>

<!-- 固定宽度 -->
<DataGridTextColumn Header="名称" Width="200"/>

<!-- 星号宽度：按比例分配剩余空间 -->
<DataGridTextColumn Header="名称" Width="2*"/>
<DataGridTextColumn Header="URL" Width="3*"/>
<DataGridTextColumn Header="操作" Width="120"/>
```

### 27.3.3 ComboBox 文本搜索

```xml
<!-- 启用文本搜索：输入时自动匹配选项 -->
<ComboBox ItemsSource="{Binding Countries}"
          IsTextSearchEnabled="True"
          DisplayMemberPath="Name"
          SelectedItem="{Binding SelectedCountry, Mode=TwoWay}"/>
```

当 IsTextSearchEnabled="True" 时，用户在下拉列表打开时输入字符，ComboBox 会自动跳转到匹配的选项。

### 27.3.4 ItemsControl 自定义布局

ItemsControl 的 ItemsPanel 可以替换为任何 Panel，实现不同的布局效果：

```xml
<!-- 网格布局 -->
<ItemsControl ItemsSource="{Binding Items}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <UniformGrid Columns="3"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>

<!-- 环形布局（需要自定义 Panel） -->
<ItemsControl ItemsSource="{Binding Items}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <views:RingPanel Radius="150"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

## 27.4 CodexSwitch 实战

### 27.4.1 CodexSwitch 的 Provider 列表

CodexSwitch 使用 ItemsControl（而非 DataGrid）来展示 Provider 列表，因为每个 Provider 需要复杂的卡片式布局：

```xml
<ItemsControl Grid.Row="1"
              ItemsSource="{Binding SelectedProviderRows}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="vm:ProviderListItem">
            <Grid Classes="provider-list-row"
                  Margin="0,0,0,10">
                <ui:CodexProviderCard Command="{Binding SelectCommand}"
                                      IsActive="{Binding IsActive}"
                                      Header="{Binding DisplayName}"
                                      Description="{Binding BaseUrl}">
                    <ui:CodexProviderCard.Meta>
                        <StackPanel Orientation="Horizontal" Spacing="6">
                            <ui:CodexBadge Variant="Secondary">
                                <TextBlock Text="{Binding Protocol}" FontSize="11"/>
                            </ui:CodexBadge>
                            <ui:CodexSelect ItemsSource="{Binding DefaultModelOptions}"
                                            SelectedItem="{Binding DefaultModel, Mode=TwoWay}"/>
                        </StackPanel>
                    </ui:CodexProviderCard.Meta>
                </ui:CodexProviderCard>
            </Grid>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**为什么用 ItemsControl 而不是 DataGrid？**
- 每个 Provider 的 UI 是卡片式的，不是表格行
- 需要显示多种状态（Active、Disabled、OAuth）
- 需要内嵌控件（选择器、按钮）
- 数据量不大（通常 <100 个 Provider）

### 27.4.2 CodexSwitch 的样式定义

CodexSwitch 在 CodexTheme.axaml 中定义了丰富的 Brush 资源，用于数据控件的样式：

```xml
<!-- 用于 Provider 卡片的 Brush -->
<SolidColorBrush x:Key="CsProviderCardBrush" Color="#1F242D"/>
<SolidColorBrush x:Key="CsProviderCardHoverBrush" Color="#252B36"/>
<SolidColorBrush x:Key="CsProviderCardActiveBrush" Color="#142235"/>

<!-- 用于 Usage 区域的 Brush -->
<SolidColorBrush x:Key="CsProviderUsageBrush" Color="#CC020617"/>
<SolidColorBrush x:Key="CsProviderUsageBorderBrush" Color="#334155"/>
```

## 27.5 举一反三

### 27.5.1 双列表选择器

```xml
<Grid ColumnDefinitions="*,Auto,*">
    <!-- 左侧：可选项目 -->
    <ListBox Grid.Column="0"
             ItemsSource="{Binding AvailableItems}"
             SelectedItems="{Binding SelectedAvailableItems}"/>

    <!-- 中间：操作按钮 -->
    <StackPanel Grid.Column="1" Spacing="8" Margin="8">
        <Button Content=">>" Command="{Binding AddAllCommand}"/>
        <Button Content=">" Command="{Binding AddSelectedCommand}"/>
        <Button Content="<" Command="{Binding RemoveSelectedCommand}"/>
        <Button Content="<<" Command="{Binding RemoveAllCommand}"/>
    </StackPanel>

    <!-- 右侧：已选项目 -->
    <ListBox Grid.Column="2"
             ItemsSource="{Binding ChosenItems}"
             SelectedItems="{Binding SelectedChosenItems}"/>
</Grid>
```

### 27.5.2 带过滤的 ComboBox

```xml
<StackPanel>
    <TextBox Watermark="搜索..."
             Text="{Binding FilterText, Mode=TwoWay}"/>
    <ComboBox ItemsSource="{Binding FilteredItems}"
              SelectedItem="{Binding SelectedItem, Mode=TwoWay}"
              IsDropDownOpen="{Binding IsDropDownOpen, Mode=TwoWay}"/>
</StackPanel>
```

## 27.6 最佳实践与设计模式

1. **大数据集必须使用虚拟化**：超过 100 个项目时必须启用虚拟化
2. **优先使用 Recycling 模式**：对数据集使用回收模式，性能更好
3. **简化 ItemTemplate**：模板越简单，渲染性能越好
4. **使用 DataGridCollectionView**：需要排序/筛选时使用集合视图
5. **合理选择控件**：简单列表用 ListBox，表格用 DataGrid，树形用 TreeView，卡片用 ItemsControl
6. **避免在 ItemTemplate 中使用复杂的布局**：尽量保持扁平
7. **为 ComboBox 设置 MaxDropDownHeight**：防止下拉列表过长

## Deep Dive

### ItemsControl 的内部结构

ItemsControl 的 ControlTemplate 包含：
- `PART_ScrollViewer`：可选的滚动容器
- `ItemsPresenter`：项目呈现器

```
ItemsControl
└── ScrollViewer
    └── ItemsPresenter
        └── VirtualizingStackPanel (ItemsPanel)
            └── ContentPresenter (每个项目)
                └── DataTemplate (ItemTemplate)
```

### VirtualizingStackPanel 的工作原理

1. 测量所有项目的总高度（使用估算值）
2. 根据滚动位置计算可见区域
3. 只为可见区域内的项目创建 UI 元素
4. 当滚动时，回收离开可视区域的元素，重新绑定到新项目

### DataGrid 的排序实现

DataGrid 的排序通过 `ICollectionView` 实现。当用户点击列头时：
1. DataGrid 创建 SortDescription
2. SortDescription 被添加到集合视图的 SortDescriptions
3. 集合视图根据 SortDescription 排序
4. UI 自动更新

## Cross References

- **[第 5 章：数据绑定](05-data-binding.md)** -- ItemsSource 绑定
- **[第 8 章：DataTemplate](08-data-templates.md)** -- ItemTemplate 和 TreeDataTemplate
- **[第 4 章：布局系统](04-layout-system.md)** -- ScrollViewer 和虚拟化
- **[第 9 章：控件模板](09-control-templates.md)** -- 自定义控件外观
- **[第 19 章：值转换器](19-value-converters.md)** -- 数据格式化

## Common Pitfalls

### 1. 忘记启用虚拟化

**问题**：绑定大数据集到 ItemsControl 但没有启用虚拟化，导致严重卡顿。

```xml
<!-- 错误：没有虚拟化 -->
<ItemsControl ItemsSource="{Binding ThousandItems}"/>

<!-- 正确：启用虚拟化 -->
<ItemsControl ItemsSource="{Binding ThousandItems}"
              VirtualizingStackPanel.IsVirtualizing="True">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

### 2. DataGrid 忘记添加 NuGet 包

**问题**：直接使用 DataGrid 但没有安装 Avalonia.Controls.DataGrid。

```xml
<!-- 解决：在 .csproj 中添加 -->
<PackageReference Include="Avalonia.Controls.DataGrid" />

<!-- 并在 App.axaml 中添加主题 -->
<StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"/>
```

### 3. TreeView 的 ItemsSource 绑定错误

**问题**：使用 DataTemplate 而不是 TreeDataTemplate，导致子节点不显示。

```xml
<!-- 错误：使用 DataTemplate -->
<TreeView.ItemTemplate>
    <DataTemplate x:DataType="vm:TreeNode">
        <TextBlock Text="{Binding Name}"/>
    </DataTemplate>
</TreeView.ItemTemplate>

<!-- 正确：使用 TreeDataTemplate 并指定 ItemsSource -->
<TreeView.ItemTemplate>
    <TreeDataTemplate x:DataType="vm:TreeNode"
                      ItemsSource="{Binding Children}">
        <TextBlock Text="{Binding Name}"/>
    </TreeDataTemplate>
</TreeView.ItemTemplate>
```

### 4. ComboBox SelectedItem 绑定失败

**问题**：SelectedItem 绑定的对象与 ItemsSource 中的对象不是同一个实例。

```xml
<!-- 解决：使用 SelectedValue 和 SelectedValueBinding -->
<ComboBox ItemsSource="{Binding Providers}"
          DisplayMemberPath="Name"
          SelectedValue="{Binding ProviderId, Mode=TwoWay}"
          SelectedValueBinding="{Binding Id}"/>
```

### 5. ListBox 多选时 SelectedItems 为 null

**问题**：SelectedItems 是只读的，不能在 XAML 中绑定到新的集合。

```csharp
// 解决：在 ViewModel 中初始化集合
public ObservableCollection<object> SelectedItems { get; } = new();

// 或者在 SelectionChanged 事件中处理
private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
{
    foreach (var item in e.AddedItems)
        SelectedItems.Add(item);
    foreach (var item in e.RemovedItems)
        SelectedItems.Remove(item);
}
```

### 6. DataGrid 排序不生效

**问题**：绑定的集合没有实现 INotifyPropertyChanged 或集合元素没有排序属性。

```csharp
// 解决：使用 DataGridCollectionView
var view = new DataGridCollectionView(myList);
view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
MyDataGrid.ItemsSource = view;
```

### 7. 虚拟化后 ItemContainer 的样式丢失

**问题**：使用 Recycling 模式时，旧的样式状态被新项目复用。

```xml
<!-- 解决：在 ItemTemplate 中绑定所有样式状态 -->
<DataTemplate>
    <Border Background="{Binding IsActive, Converter={StaticResource BoolToBrushConverter}}">
        <!-- ... -->
    </Border>
</DataTemplate>
```

### 8. TreeView 展开大量节点时卡顿

**问题**：一次性展开所有节点，创建了大量 UI 元素。

```csharp
// 解决：使用懒加载，只在展开时加载子节点
public class LazyTreeNode
{
    public ObservableCollection<LazyTreeNode>? Children { get; set; }

    public void EnsureLoaded()
    {
        if (Children is null)
        {
            Children = new ObservableCollection<LazyTreeNode>();
            // 加载子节点...
        }
    }
}
```

### 9. ComboBox 下拉列表超出屏幕

**问题**：选项太多导致下拉列表超出屏幕边界。

```xml
<!-- 解决：设置 MaxDropDownHeight -->
<ComboBox MaxDropDownHeight="300"
          ItemsSource="{Binding LargeOptions}"/>
```

### 10. DataGrid 编辑时绑定不更新

**问题**：编辑单元格后数据没有更新到 ViewModel。

```csharp
// 解决：确保绑定使用 TwoWay 模式，且属性实现了 INotifyPropertyChanged
<DataGridTextColumn Header="名称"
                    Binding="{Binding Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
```

## Try It Yourself

### 练习 1：创建搜索过滤列表

创建一个带搜索框的 ListBox，输入文字时实时过滤列表。

**提示**：使用 TextBox.Text 绑定到 FilterText，然后更新集合视图的 Filter。

### 练习 2：实现级联 ComboBox

创建两个 ComboBox，第一个选择省份，第二个自动显示该省份的城市。

**提示**：第一个 ComboBox 的 SelectionChanged 中更新第二个的 ItemsSource。

### 练习 3：创建可排序的 DataGrid

创建一个 DataGrid，点击列头可以排序，支持升序/降序切换。

**提示**：使用 DataGridCollectionView 和 SortDescription。

### 练习 4：实现文件浏览器

使用 TreeView 实现一个简单的文件浏览器，显示文件夹和文件的层次结构。

**提示**：使用 TreeDataTemplate，文件夹节点有 Children，文件节点没有。

### 练习 5：创建标签选择器

使用 ListBox + WrapPanel 创建一个标签选择器，支持多选。

**提示**：设置 ItemsPanel 为 WrapPanel，SelectionMode 为 Multiple。

### 练习 6：实现数据表格 CRUD

创建一个完整的 DataGrid，支持添加、编辑、删除行。

**提示**：使用 DataGridTemplateColumn 放置操作按钮，使用 IsReadOnly="False" 支持编辑。

### 练习 7：创建树形多选控件

在 TreeView 中实现多选功能，每个节点前有 CheckBox。

**提示**：在 TreeDataTemplate 中添加 CheckBox，绑定到节点的 IsChecked 属性。

### 练习 8：CodexSwitch 风格的卡片列表

模仿 CodexSwitch 的 Provider 列表，使用 ItemsControl 创建一个卡片式列表。

**提示**：使用 ItemsControl + DataTemplate，每个卡片使用 Border 包裹。
