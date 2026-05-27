# 4. 布局系统

> **写给零基础的你**：布局系统就像房间里的家具摆放规则。你不会把沙发放在门口挡路，也不会把电视放在浴室里。布局系统就是告诉每个控件"你该待在哪里、占多大地方"的规则。

## 4.1 概述

Avalonia 的布局系统负责计算每个控件的大小和位置。理解布局系统是构建响应式 UI 的基础。本章详细讲解所有布局面板的所有属性和用法、ScrollViewer、ViewBox、布局性能优化等核心知识点，并通过 CodexSwitch 的真实代码展示实际用法。

学完本章后，你将能够：
- 掌握 Grid 的所有属性（RowDefinitions、ColumnDefinitions、Grid.Row、Grid.Column、Grid.RowSpan、Grid.ColumnSpan、Grid.IsSharedSizeScope、GridSplitter）
- 掌握 StackPanel、DockPanel、Canvas、WrapPanel 的所有属性
- 理解 ScrollViewer 的所有属性和用法
- 理解布局性能优化策略
- 掌握 Measure/Arrange 的内部机制

## 4.2 核心概念

### 4.2.1 布局流程概述

Avalonia 的布局分为两个递归阶段：

1. **Measure（测量）**：从根节点向下遍历，每个控件告诉父控件"我需要多大空间"
2. **Arrange（排列）**：从根节点向下遍历，父控件告诉每个子控件"你在什么位置，你有多大"

```
根 Window (可用: 1180x760)
    ↓ Measure
    └── Grid (可用: 1180x760, 期望: 1180x760)
        ├── Column 0: 220px
        │   └── Sidebar (可用: 220x760, 期望: 220x760)
        └── Column 1: *
            └── Content (可用: 960x760, 期望: 960x760)
                ├── Row 0: 64px → TopBar
                └── Row 1: * → PageContent
```

每个控件重写两个方法来参与布局：

```csharp
// MeasureOverride: 计算控件期望的大小
protected override Size MeasureOverride(Size availableSize)
{
    _child.Measure(availableSize);
    return _child.DesiredSize;
}

// ArrangeOverride: 在分配的空间内排列子元素
protected override Size ArrangeOverride(Size finalSize)
{
    _child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
    return finalSize;
}
```

### 4.2.2 尺寸模式

> **小白提示**：Grid 就像一个表格，你可以把界面分成行和列，每个控件放在某个格子里。尺寸模式就是决定每行/每列"有多宽/多高"的规则。

| 模式 | 语法 | 说明 | 类比 |
|------|------|------|------|
| 固定尺寸 | `220` | 像素值，不随窗口变化 | 固定宽度的门，不会变 |
| 比例尺寸 | `*` 或 `2*` | 按比例分配剩余空间 | 把蛋糕按比例切分 |
| 自动尺寸 | `Auto` | 根据内容自动计算 | 衣服根据身材自动调整大小 |

```xml
<!-- 三种尺寸模式的组合 -->
<Grid RowDefinitions="Auto,*,2*,100">
    <!-- Auto: 根据内容高度自动计算（第 1 行） -->
    <!-- *: 填充 1/4 剩余空间（第 2 行） -->
    <!-- 2*: 填充 2/4 剩余空间（第 3 行） -->
    <!-- 100: 固定 100 像素（第 4 行） -->
</Grid>
```

> **小白提示**：`*` 和 `2*` 的意思是"按比例分配剩余空间"。如果总剩余空间是 300px，`*` 得到 100px（1/3），`2*` 得到 200px（2/3）。就像分蛋糕——你拿 1 份，我拿 2 份。

### 4.2.3 对齐与边距

```xml
<!-- 水平对齐 -->
<TextBlock Text="Left"   HorizontalAlignment="Left"/>    <!-- 靠左 -->
<TextBlock Text="Center" HorizontalAlignment="Center"/>  <!-- 居中 -->
<TextBlock Text="Right"  HorizontalAlignment="Right"/>   <!-- 靠右 -->
<TextBlock Text="Stretch" HorizontalAlignment="Stretch"/> <!-- 拉伸填满 -->

<!-- 垂直对齐 -->
<Border VerticalAlignment="Top"/>      <!-- 靠上 -->
<Border VerticalAlignment="Center"/>   <!-- 居中 -->
<Border VerticalAlignment="Bottom"/>   <!-- 靠下 -->
<Border VerticalAlignment="Stretch"/>  <!-- 拉伸填满 -->

<!-- Margin: 控件外部的空白（控件与邻居之间的距离） -->
<Grid Margin="24,12,24,28">  <!-- 左,上,右,下 -->

<!-- Padding: 控件内部的空白（边框与内容之间的距离） -->
<Border Padding="10,7">
```

> **小白提示：Margin 和 Padding 的区别？**  想象一个相框：
> - **Margin** = 相框与墙壁之间的距离（相框外部的间距）
> - **Padding** = 相框边框与照片之间的距离（相框内部的间距）

**Margin vs Padding：**

| 属性 | 说明 | 影响范围 | 类比 |
|------|------|---------|------|
| `Margin` | 控件外部的空白 | 控件与相邻控件之间的距离 | 相框与墙壁的距离 |
| `Padding` | 控件内部的空白 | 控件边框与内容之间的距离 | 相框边框与照片的距离 |

## 4.3 进阶用法

### 4.3.1 Grid 布局详解

> **小白提示**：Grid 就像一个"表格"。你可以把界面分成行和列，每个控件放在某个格子里。CodexSwitch 的主窗口就是用 Grid 把界面分成了"侧边栏"和"内容区"两列。

Grid 是 Avalonia 中最强大、最常用的布局面板。CodexSwitch 的主窗口完全基于 Grid 构建。

#### RowDefinitions 和 ColumnDefinitions

```xml
<!-- CodexSwitch MainWindow 的主 Grid -->
<!-- 把窗口分成两列：左边 220px 侧边栏，右边填充剩余空间 -->
<Grid ColumnDefinitions="220,*">
    <!-- Column 0: 固定 220px 侧边栏（放在第 1 列） -->
    <ui:CodexSidebar/>

    <!-- Column 1: 填充剩余空间（放在第 2 列） -->
    <Grid Grid.Column="1" RowDefinitions="64,*">
        <!-- Row 0: 固定 64px 顶部栏（放在第 1 行） -->
        <shell:TopBar/>

        <!-- Row 1: 填充剩余空间（放在第 2 行） -->
        <Grid Grid.Row="1">
            <pages:HomePage IsVisible="{Binding IsHomePageVisible}"/>
            <pages:ProvidersPage IsVisible="{Binding IsProvidersPageVisible}"/>
        </Grid>
    </Grid>
</Grid>
```

> **小白提示**：`Grid.Column="1"` 的意思是"这个控件放在第 2 列"（从 0 开始数）。`Grid.Row="1"` 的意思是"这个控件放在第 2 行"。

**RowDefinitions 和 ColumnDefinitions 的语法：**

```xml
<!-- 简写语法 -->
<Grid RowDefinitions="Auto,*,2*,100"
      ColumnDefinitions="220,*"/>

<!-- 完整语法 -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="2*"/>
        <RowDefinition Height="100"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="220"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
</Grid>
```

#### Grid.Row 和 Grid.Column 附加属性

```xml
<!-- Grid.Row 和 Grid.Column 定位子元素 -->
<Grid RowDefinitions="Auto,*" ColumnDefinitions="220,*">
    <Sidebar/>                          <!-- Row=0, Column=0 (默认) -->
    <TopBar Grid.Column="1"/>           <!-- Row=0, Column=1 -->
    <Content Grid.Row="1" Grid.Column="1"/>  <!-- Row=1, Column=1 -->
</Grid>
```

#### Grid.RowSpan 和 Grid.ColumnSpan

```xml
<!-- Grid.RowSpan 和 Grid.ColumnSpan 跨行列 -->
<Grid RowDefinitions="Auto,*,Auto" ColumnDefinitions="220,*">
    <Header Grid.ColumnSpan="2"/>       <!-- 跨两列 -->
    <Sidebar Grid.Row="1"/>
    <Content Grid.Row="1" Grid.Column="1"/>
    <Footer Grid.Row="2" Grid.ColumnSpan="2"/>  <!-- 跨两列 -->
</Grid>
```

CodexSwitch 中跨行的实际用法（`ProvidersPage.axaml`）：

```xml
<Grid ColumnDefinitions="Auto,*"
      RowDefinitions="Auto,Auto,Auto"
      ColumnSpacing="7">
    <Border Grid.RowSpan="3"
            Classes="usage-status-dot"
            VerticalAlignment="Top"
            Margin="0,5,0,0"/>
    <TextBlock Grid.Column="1" Text="{Binding UsageSummary}"/>
    <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding UsageMeta}"/>
    <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding UsageResetText}"/>
</Grid>
```

#### RowSpacing 和 ColumnSpacing

```xml
<!-- 行间距和列间距 -->
<Grid RowDefinitions="Auto,*"
      ColumnDefinitions="220,*"
      RowSpacing="10"
      ColumnSpacing="20">
    <!-- ... -->
</Grid>
```

CodexSwitch 中的间距用法：

```xml
<Grid Margin="24,12,24,28"
      RowDefinitions="Auto,*"
      RowSpacing="18">
    <!-- Section 标题区域 -->
    <ui:CodexSection Title="{i18n:Tr providers.title}"
                     Description="{i18n:Tr providers.description}"/>

    <!-- 提供商列表 -->
    <ItemsControl Grid.Row="1"
                  ItemsSource="{Binding SelectedProviderRows}"/>
</Grid>
```

#### Grid.IsSharedSizeScope

`Grid.IsSharedSizeScope` 允许多个 Grid 共享列或行的尺寸。

```xml
<!-- 外层容器设置 IsSharedSizeScope -->
<StackPanel Grid.IsSharedSizeScope="True">
    <!-- Grid 1 -->
    <Grid ColumnDefinitions="Auto,*">
        <TextBlock Text="Name:" Grid.Column="0"/>
        <TextBox Grid.Column="1"/>
    </Grid>

    <!-- Grid 2: 第一列与 Grid 1 的第一列宽度相同 -->
    <Grid ColumnDefinitions="Auto,*">
        <TextBlock Text="Email:" Grid.Column="0"/>
        <TextBox Grid.Column="1"/>
    </Grid>
</StackPanel>
```

#### GridSplitter

GridSplitter 允许用户拖动来调整行列大小。

```xml
<Grid ColumnDefinitions="200,5,*">
    <TreeView Grid.Column="0"/>

    <!-- GridSplitter: 可拖动的分隔条 -->
    <GridSplitter Grid.Column="1"
                  Width="5"
                  HorizontalAlignment="Stretch"
                  Background="Gray"/>

    <ContentControl Grid.Column="2"/>
</Grid>
```

**GridSplitter 的关键属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `ResizeDirection` | GridResizeDirection | 调整方向（Auto, Columns, Rows） |
| `ResizeBehavior` | GridResizeBehavior | 调整行为（PreviousAndNext, CurrentAndNext 等） |
| `HorizontalAlignment` | HorizontalAlignment | 水平对齐（影响拖动方向） |
| `VerticalAlignment` | VerticalAlignment | 垂直对齐（影响拖动方向） |

### 4.3.2 StackPanel 详解

StackPanel 按水平或垂直方向依次排列子元素。

```xml
<!-- 垂直 StackPanel（默认） -->
<StackPanel Spacing="10">
    <TextBlock Text="Item 1"/>
    <TextBlock Text="Item 2"/>
    <TextBlock Text="Item 3"/>
</StackPanel>

<!-- 水平 StackPanel -->
<StackPanel Orientation="Horizontal" Spacing="10">
    <Button Content="A"/>
    <Button Content="B"/>
    <Button Content="C"/>
</StackPanel>
```

**StackPanel 的关键属性：**

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Orientation` | Orientation | Vertical | 排列方向 |
| `Spacing` | double | 0 | 子元素之间的间距 |

CodexSwitch 在侧边栏中大量使用 StackPanel：

```xml
<!-- MainWindow.axaml 侧边栏头部 -->
<ui:CodexSidebarHeader>
    <StackPanel Spacing="10">
        <StackPanel Orientation="Horizontal" Spacing="10">
            <ui:CodexImageIcon Path="{Binding CodexIconPath}"
                               Width="24" Height="24"/>
            <text:CodexText Text="{i18n:Tr app.name}"
                            Role="Subtitle"
                            VerticalAlignment="Center"/>
        </StackPanel>
        <ui:CodexSegmentedControl HorizontalAlignment="Left">
            <!-- ... -->
        </ui:CodexSegmentedControl>
    </StackPanel>
</ui:CodexSidebarHeader>
```

### 4.3.3 DockPanel 详解

DockPanel 将子元素停靠到上、下、左、右四个边缘，最后一个子元素填充剩余空间。

```xml
<DockPanel>
    <TextBlock DockPanel.Dock="Top" Text="Header"/>
    <TextBlock DockPanel.Dock="Bottom" Text="Footer"/>
    <TextBlock DockPanel.Dock="Left" Text="Sidebar"/>
    <ContentPresenter/>  <!-- 填充剩余空间 -->
</DockPanel>
```

**DockPanel 的关键属性：**

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `LastChildFill` | bool | true | 最后一个子元素是否填充剩余空间 |

**DockPanel.Dock 附加属性：**

| 值 | 说明 |
|---|------|
| `Top` | 停靠到顶部 |
| `Bottom` | 停靠到底部 |
| `Left` | 停靠到左侧 |
| `Right` | 停靠到右侧 |

**停靠顺序很重要**——先添加的元素先占据边缘位置。

```xml
<!-- 示例：先左后右 -->
<DockPanel>
    <Border DockPanel.Dock="Left" Width="200" Background="Red"/>
    <Border DockPanel.Dock="Right" Width="200" Background="Blue"/>
    <Border Background="Green"/>  <!-- 填充剩余空间 -->
</DockPanel>

<!-- 示例：先右后左 -->
<DockPanel>
    <Border DockPanel.Dock="Right" Width="200" Background="Blue"/>
    <Border DockPanel.Dock="Left" Width="200" Background="Red"/>
    <Border Background="Green"/>  <!-- 填充剩余空间 -->
</DockPanel>
```

### 4.3.4 Canvas 详解

Canvas 使用绝对坐标定位子元素，适合自定义绘图和动画场景。

```xml
<Canvas>
    <Rectangle Canvas.Left="10" Canvas.Top="20"
               Width="100" Height="50" Fill="Red"/>
    <Ellipse Canvas.Left="150" Canvas.Top="30"
             Width="60" Height="60" Fill="Blue"/>
</Canvas>
```

**Canvas 的附加属性：**

| 属性 | 说明 |
|------|------|
| `Canvas.Left` | 距离左边界的距离 |
| `Canvas.Top` | 距离上边界的距离 |
| `Canvas.Right` | 距离右边界的距离 |
| `Canvas.Bottom` | 距离下边界的距离 |

**Canvas 的特点：**
- 不参与测量/排列流程
- 子元素需要手动设置位置
- 可以超出 Canvas 边界
- 适合自定义绘图和动画

### 4.3.5 WrapPanel 详解

WrapPanel 类似于 StackPanel，但当一行放不下时会自动换行。

```xml
<WrapPanel ItemSpacing="8" LineSpacing="8">
    <Button Content="Tag 1"/>
    <Button Content="Tag 2"/>
    <Button Content="Tag 3"/>
    <!-- 一行放不下时自动换到下一行 -->
</WrapPanel>
```

**WrapPanel 的关键属性：**

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Orientation` | Orientation | Horizontal | 排列方向 |
| `ItemSpacing` | double | 0 | 项目之间的水平间距 |
| `LineSpacing` | double | 0 | 行之间的垂直间距 |
| `ItemWidth` | double | NaN | 统一项目宽度 |
| `ItemHeight` | double | NaN | 统一项目高度 |

### 4.3.6 ScrollViewer 详解

ScrollViewer 为内容提供滚动条，当内容超出可视区域时自动出现。

```xml
<!-- ProvidersPage.axaml 使用 ScrollViewer -->
<ScrollViewer>
    <Grid Margin="24,12,24,28"
          RowDefinitions="Auto,*"
          RowSpacing="18">
        <ui:CodexSection Title="{i18n:Tr providers.title}"
                         Description="{i18n:Tr providers.description}"/>
        <ItemsControl Grid.Row="1"
                      ItemsSource="{Binding SelectedProviderRows}"/>
    </Grid>
</ScrollViewer>
```

**ScrollViewer 的关键属性：**

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `HorizontalScrollBarVisibility` | ScrollBarVisibility | Auto | 水平滚动条可见性 |
| `VerticalScrollBarVisibility` | ScrollBarVisibility | Auto | 垂直滚动条可见性 |
| `HorizontalSnapPointsType` | SnapPointsType | None | 水平吸附点类型 |
| `VerticalSnapPointsType` | SnapPointsType | None | 垂直吸附点类型 |
| `IsScrollChainingEnabled` | bool | true | 是否启用滚动链 |
| `BringIntoViewOnFocusChange` | bool | true | 焦点变化时是否滚动到可见区域 |

**ScrollBarVisibility 枚举：**

| 值 | 说明 |
|---|------|
| `Auto` | 需要时显示（默认） |
| `Visible` | 始终显示 |
| `Hidden` | 始终隐藏（但仍可滚动） |
| `Disabled` | 禁用滚动 |

```xml
<!-- 只允许垂直滚动 -->
<ScrollViewer HorizontalScrollBarVisibility="Disabled"
              VerticalScrollBarVisibility="Auto">
    <!-- 内容 -->
</ScrollViewer>

<!-- 始终显示滚动条 -->
<ScrollViewer HorizontalScrollBarVisibility="Visible"
              VerticalScrollBarVisibility="Visible">
    <!-- 内容 -->
</ScrollViewer>
```

### 4.3.7 ViewBox

ViewBox 提供缩放功能，可以将内容缩放到指定大小。

```xml
<!-- 将内容缩放到 200x200 -->
<Viewbox Width="200" Height="200">
    <Canvas Width="100" Height="100">
        <Ellipse Width="100" Height="100" Fill="Red"/>
    </Canvas>
</Viewbox>

<!-- 指定拉伸模式 -->
<Viewbox Stretch="Uniform">
    <!-- 内容保持比例 -->
</Viewbox>

<Viewbox Stretch="Fill">
    <!-- 内容填充整个区域 -->
</Viewbox>
```

**ViewBox 的关键属性：**

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Stretch` | Stretch | Uniform | 拉伸模式 |
| `StretchDirection` | StretchDirection | Both | 拉伸方向 |

**Stretch 枚举：**

| 值 | 说明 |
|---|------|
| `None` | 不拉伸 |
| `Fill` | 填充整个区域（可能变形） |
| `Uniform` | 保持比例，适应区域 |
| `UniformToFill` | 保持比例，填充区域（可能裁剪） |

## 4.4 组件详解大全

### 4.4.1 Grid 完整属性列表

**Grid 的属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `RowDefinitions` | RowDefinitions | 行定义集合 |
| `ColumnDefinitions` | ColumnDefinitions | 列定义集合 |
| `RowSpacing` | double | 行间距 |
| `ColumnSpacing` | double | 列间距 |
| `IsSharedSizeScope` | bool | 是否启用共享尺寸 |

**Grid 的附加属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Grid.Row` | int | 子元素所在的行 |
| `Grid.Column` | int | 子元素所在的列 |
| `Grid.RowSpan` | int | 子元素跨越的行数 |
| `Grid.ColumnSpan` | int | 子元素跨越的列数 |
| `Grid.IsSharedSizeScope` | bool | 是否启用共享尺寸 |

**RowDefinition 的属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Height` | GridLength | 行高度（Auto, *, 固定值） |
| `MinHeight` | double | 最小高度 |
| `MaxHeight` | double | 最大高度 |

**ColumnDefinition 的属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Width` | GridLength | 列宽度（Auto, *, 固定值） |
| `MinWidth` | double | 最小宽度 |
| `MaxWidth` | double | 最大宽度 |

### 4.4.2 StackPanel 完整属性列表

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Orientation` | Orientation | Vertical | 排列方向（Horizontal, Vertical） |
| `Spacing` | double | 0 | 子元素之间的间距 |

### 4.4.3 DockPanel 完整属性列表

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `LastChildFill` | bool | true | 最后一个子元素是否填充剩余空间 |

**DockPanel.Dock 附加属性：**

| 值 | 说明 |
|---|------|
| `Top` | 停靠到顶部 |
| `Bottom` | 停靠到底部 |
| `Left` | 停靠到左侧 |
| `Right` | 停靠到右侧 |

### 4.4.4 Canvas 完整属性列表

**Canvas 的附加属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Canvas.Left` | double | 距离左边界的距离 |
| `Canvas.Top` | double | 距离上边界的距离 |
| `Canvas.Right` | double | 距离右边界的距离 |
| `Canvas.Bottom` | double | 距离下边界的距离 |

### 4.4.5 WrapPanel 完整属性列表

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Orientation` | Orientation | Horizontal | 排列方向 |
| `ItemSpacing` | double | 0 | 项目之间的水平间距 |
| `LineSpacing` | double | 0 | 行之间的垂直间距 |
| `ItemWidth` | double | NaN | 统一项目宽度 |
| `ItemHeight` | double | NaN | 统一项目高度 |

## 4.5 CodexSwitch 实战

### 4.5.1 MainWindow.axaml 的布局结构

```
Window (1180x760)
└── Border.app-shell
    └── Grid (ColumnDefinitions="220,*")
        ├── Column 0: CodexSidebar (220px)
        │   └── Grid (RowDefinitions="Auto,*,Auto")
        │       ├── Row 0: CodexSidebarHeader
        │       │   └── StackPanel (Spacing="10")
        │       │       ├── StackPanel (Horizontal, Spacing="10")
        │       │       │   ├── CodexImageIcon (24x24)
        │       │       │   └── CodexText (Subtitle)
        │       │       ├── CodexSegmentedControl
        │       │       └── CodexBadge
        │       ├── Row 1: CodexSidebarContent
        │       │   └── CodexSidebarMenu
        │       └── Row 2: CodexSidebarFooter
        └── Column 1: Grid (RowDefinitions="64,*")
            ├── Row 0: TopBar (64px)
            └── Row 1: Grid (页面容器)
                ├── HomePage (IsVisible binding)
                ├── ProvidersPage (IsVisible binding)
                ├── CodexSessionsPage (IsVisible binding)
                ├── AddProviderPage (IsVisible binding)
                ├── UsagePage (IsVisible binding)
                ├── ModelsPage (IsVisible binding)
                ├── SettingsPage (IsVisible binding)
                └── ClaudePage (IsVisible binding)
```

### 4.5.2 页面切换模式

CodexSwitch 使用 Grid 叠加 + IsVisible 绑定实现页面切换：

```xml
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

所有页面叠加在同一个 Grid Cell 中，通过 `IsVisible` 绑定到 ViewModel 的布尔属性，同一时刻只有一个页面可见。

**为什么使用这种方式而不是导航框架：**
- 页面需要保持状态（如滚动位置、表单输入）
- 避免频繁创建和销毁页面实例
- 简化页面间的通信
- 更好的性能（页面只创建一次）

## 4.6 举一反三

### 4.6.1 布局性能优化

**避免不必要的 Measure/Arrange：**

```xml
<!-- 差：每次属性变化都触发布局 -->
<StackPanel>
    <Border Height="{Binding DynamicHeight}"/>
    <Border Height="{Binding DynamicHeight}"/>
</StackPanel>

<!-- 好：使用固定高度或比例 -->
<Grid RowDefinitions="Auto,*">
    <Border/>
    <Border Grid.Row="1"/>
</Grid>
```

**减少嵌套层级：**

```xml
<!-- 差：深层嵌套 -->
<Grid>
    <StackPanel>
        <Grid>
            <StackPanel>
                <Border>
                    <TextBlock/>
                </Border>
            </StackPanel>
        </Grid>
    </StackPanel>
</Grid>

<!-- 好：扁平化 -->
<Grid>
    <Border>
        <TextBlock/>
    </Border>
</Grid>
```

**使用合适的布局面板：**

| 场景 | 推荐面板 |
|------|---------|
| 表单布局 | Grid |
| 简单列表 | StackPanel |
| 工具栏 | StackPanel (Horizontal) |
| 自适应标签 | WrapPanel |
| 停靠布局 | DockPanel |
| 自定义绘图 | Canvas |

### 4.6.2 响应式布局

```xml
<!-- 使用 * 比例实现响应式 -->
<Grid ColumnDefinitions="*,2*">
    <Sidebar/>
    <Content Grid.Column="1"/>
</Grid>

<!-- 使用 MinWidth/MaxWidth 限制范围 -->
<Grid ColumnDefinitions="200,*,300">
    <Sidebar MinWidth="150" MaxWidth="300"/>
    <Content Grid.Column="1"/>
    <Panel Grid.Column="2" MinWidth="200" MaxWidth="400"/>
</Grid>
```

## 4.7 最佳实践与设计模式

### 4.7.1 布局选择指南

1. **Grid**：最通用，适合复杂布局
2. **StackPanel**：简单堆叠，适合工具栏、表单
3. **DockPanel**：停靠布局，适合主窗口框架
4. **WrapPanel**：自动换行，适合标签云、图片集
5. **Canvas**：绝对定位，适合自定义绘图

### 4.7.2 间距和边距规范

1. **使用 Margin**：控件之间的间距
2. **使用 Padding**：控件内部的间距
3. **使用 Spacing**：StackPanel/Grid 内部的间距
4. **保持一致性**：使用设计令牌（如 `CsRadiusMd`）保持间距一致

## Deep Dive

### Measure/Arrange 机制详解

#### 测量阶段

```
根节点调用 Measure(availableSize)
    ↓
每个子节点收到 availableSize 并计算 DesiredSize
    ↓
DesiredSize = 子节点想要的最小尺寸（考虑 MinWidth/MinHeight）
    ↓
父节点汇总所有子节点的 DesiredSize
    ↓
返回父节点的 DesiredSize
```

Grid 的测量逻辑：

```csharp
// Grid 的 MeasureOverride 伪逻辑
protected override Size MeasureOverride(Size availableSize)
{
    // 1. 对 Auto 行列：测量其中的子元素，确定实际大小
    foreach (var row in Rows.Where(r => r.Height.IsAuto))
    {
        row.ActualHeight = ChildrenInRow(row)
            .Max(c => { c.Measure(availableSize); return c.DesiredSize.Height; });
    }

    // 2. 对 * 行列：分配剩余空间
    double remaining = availableSize - autoSize - fixedSize;
    foreach (var row in Rows.Where(r => r.Height.IsStar))
    {
        row.ActualHeight = remaining * (row.Height.Value / totalStars);
    }

    // 3. 测量所有子元素
    foreach (var child in Children)
        child.Measure(new Size(columnWidth, rowHeight));

    return totalSize;
}
```

#### 排列阶段

```
根节点调用 Arrange(finalSize)
    ↓
父节点计算每个子节点的最终位置和大小
    ↓
子节点被放置在计算出的 Rect 中
    ↓
子节点可能进一步 Arrange 自己的子节点
```

#### 布局失效与重绘

当属性变化导致布局需要重新计算时，控件调用：

- `InvalidateMeasure()` — 标记需要重新测量
- `InvalidateArrange()` — 标记需要重新排列
- `InvalidateVisual()` — 标记需要重新渲染

Avalonia 会在下一帧批量处理所有失效请求，避免重复计算。使用 `AffectsMeasure<T>()` 可以让属性系统自动处理失效。

## Cross References

- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 AXAML 中 Grid 附加属性和布局属性的语法
- **[第 7 章：样式与主题](07-styling-theming.md)** — 了解样式系统中的资源引用
- **[第 8 章：DataTemplate](08-data-templates.md)** — 掌握 DataTemplate 中的布局

## Common Pitfalls

### 1. StackPanel 不限制子元素尺寸

**问题**：StackPanel 沿排列方向不限制子元素，子元素可能超出可视区域。

```xml
<!-- 问题：垂直 StackPanel 中每个子元素都可以要求无限高度 -->
<StackPanel>
    <Border Height="200"/>
    <Border Height="200"/>
    <Border Height="200"/>
    <!-- 超出窗口高度，但没有滚动条 -->
</StackPanel>

<!-- 解决：用 ScrollViewer 包裹 -->
<ScrollViewer>
    <StackPanel>
        <Border Height="200"/>
        <Border Height="200"/>
    </StackPanel>
</ScrollViewer>
```

### 2. Grid 中子元素没有指定 Grid.Row/Grid.Column

**问题**：子元素默认放在 Row=0, Column=0，容易重叠。

```xml
<!-- 问题：两个元素都在 Row=0, Column=0，互相重叠 -->
<Grid RowDefinitions="Auto,*">
    <Header/>
    <Content/>  <!-- 也在 Row=0, Column=0！ -->
</Grid>

<!-- 正确：显式指定行 -->
<Grid RowDefinitions="Auto,*">
    <Header Grid.Row="0"/>
    <Content Grid.Row="1"/>
</Grid>
```

### 3. Auto 和 * 的混淆

**问题**：`Auto` 根据内容计算大小，`*` 分配剩余空间。

```xml
<!-- 问题：期望 Sidebar 固定 220px，但用了 Auto -->
<Grid ColumnDefinitions="Auto,*">
    <Sidebar/>  <!-- 宽度 = 内容宽度，可能不是 220px -->
</Grid>

<!-- 正确：使用固定值 -->
<Grid ColumnDefinitions="220,*">
    <Sidebar/>  <!-- 宽度固定 220px -->
</Grid>
```

### 4. Margin 导致布局溢出

**问题**：Margin 不包含在元素的 DesiredSize 计算中，可能导致内容超出可视区域。

```xml
<!-- 问题：ScrollViewer 的 Margin 使内容区域变小 -->
<Grid RowDefinitions="*,Auto">
    <ScrollViewer Grid.Row="0" Margin="24,12,24,28">
        <!-- 实际可用空间 = Grid 高度 - Margin -->
    </ScrollViewer>
</Grid>

<!-- 正确：理解 Margin 的影响，或使用 Padding -->
```

### 5. 忘记 ScrollViewer

**问题**：内容超出容器时没有滚动条，用户无法看到全部内容。

```xml
<!-- 问题：ItemsControl 内容可能很多 -->
<ItemsControl ItemsSource="{Binding ProviderRows}"/>

<!-- 正确：用 ScrollViewer 包裹 -->
<ScrollViewer>
    <ItemsControl ItemsSource="{Binding ProviderRows}"/>
</ScrollViewer>
```

### 6. DockPanel 的 LastChildFill 误解

**问题**：最后一个子元素总是填充剩余空间，无论是否设置了 Dock。

```xml
<!-- 问题：最后一个 Border 会填充剩余空间 -->
<DockPanel>
    <Border DockPanel.Dock="Left" Width="200"/>
    <Border DockPanel.Dock="Right" Width="200"/>
    <Border Width="100"/>  <!-- 会被拉伸到填充剩余空间 -->
</DockPanel>

<!-- 解决：设置 LastChildFill="False" -->
<DockPanel LastChildFill="False">
    <Border DockPanel.Dock="Left" Width="200"/>
    <Border DockPanel.Dock="Right" Width="200"/>
    <Border Width="100"/>  <!-- 保持 100px 宽度 -->
</DockPanel>
```

### 7. Canvas 不限制子元素位置

**问题**：Canvas 的子元素可以超出边界。

```xml
<!-- 问题：子元素可能超出 Canvas 边界 -->
<Canvas Width="200" Height="200">
    <Rectangle Canvas.Left="150" Width="100" Height="50" Fill="Red"/>
    <!-- 超出 Canvas 右边界 -->
</Canvas>

<!-- 解决：使用 ClipToBounds 裁剪 -->
<Canvas Width="200" Height="200" ClipToBounds="True">
    <Rectangle Canvas.Left="150" Width="100" Height="50" Fill="Red"/>
    <!-- 超出部分被裁剪 -->
</Canvas>
```

### 8. WrapPanel 的 ItemWidth/ItemHeight 误解

**问题**：设置 `ItemWidth` 后，所有项目都会使用这个宽度。

```xml
<!-- 问题：所有项目都会变成 100px 宽 -->
<WrapPanel ItemWidth="100">
    <Button Content="Short"/>
    <Button Content="Very Long Text"/>
</WrapPanel>

<!-- 解决：不设置 ItemWidth，让项目自适应 -->
<WrapPanel>
    <Button Content="Short"/>
    <Button Content="Very Long Text"/>
</WrapPanel>
```

### 9. 嵌套 Grid 的性能问题

**问题**：过度嵌套 Grid 会导致性能下降。

```xml
<!-- 差：多层嵌套 Grid -->
<Grid>
    <Grid>
        <Grid>
            <Grid>
                <TextBlock/>
            </Grid>
        </Grid>
    </Grid>
</Grid>

<!-- 好：减少嵌套 -->
<Grid>
    <TextBlock/>
</Grid>
```

### 10. 忘记设置 Grid.Row/Grid.Column

**问题**：子元素默认放在 Row=0, Column=0。

```xml
<!-- 问题：三个元素都在 Row=0, Column=0 -->
<Grid RowDefinitions="Auto,Auto,Auto">
    <TextBlock Text="Header"/>
    <TextBlock Text="Content"/>
    <TextBlock Text="Footer"/>
</Grid>

<!-- 正确：显式指定行 -->
<Grid RowDefinitions="Auto,Auto,Auto">
    <TextBlock Text="Header" Grid.Row="0"/>
    <TextBlock Text="Content" Grid.Row="1"/>
    <TextBlock Text="Footer" Grid.Row="2"/>
</Grid>
```

### 11. ScrollViewer 内容尺寸问题

**问题**：ScrollViewer 的内容需要明确的高度/宽度才能滚动。

```xml
<!-- 问题：StackPanel 没有明确高度，无法滚动 -->
<ScrollViewer>
    <StackPanel>
        <Border Height="200"/>
        <Border Height="200"/>
    </StackPanel>
</ScrollViewer>

<!-- 正确：给内容一个明确的高度 -->
<ScrollViewer>
    <StackPanel Height="1000">
        <Border Height="200"/>
        <Border Height="200"/>
    </StackPanel>
</ScrollViewer>
```

### 12. 使用固定尺寸导致响应式失效

**问题**：使用固定像素值会导致在不同分辨率下显示效果不一致。

```xml
<!-- 差：固定像素值 -->
<Grid ColumnDefinitions="300,600">
    <Sidebar/>
    <Content Grid.Column="1"/>
</Grid>

<!-- 好：使用比例 -->
<Grid ColumnDefinitions="1,2">
    <Sidebar/>
    <Content Grid.Column="1"/>
</Grid>
```

## Try It Yourself

### 练习 1：拆解 MainWindow.axaml 的布局结构

打开 `MainWindow.axaml`，绘制其布局树。识别每个 Grid 的 RowDefinitions 和 ColumnDefinitions，理解固定尺寸、比例尺寸和自动尺寸的使用场景。

### 练习 2：实验尺寸模式

创建一个测试页面，用 `Auto,*,2*,100` 四种行定义组合。拖动窗口边缘改变大小，观察各行如何响应。

### 练习 3：实现嵌套布局

用 Grid 嵌套实现一个经典的应用布局：顶部 Header（跨两列）、左侧 Sidebar、右侧 Content、底部 Footer（跨两列）。

### 练习 4：StackPanel vs Grid 对比

创建两个相同布局的页面，一个用 StackPanel 嵌套，一个用 Grid，对比代码量和灵活性。在窗口缩放时观察两者的差异。

### 练习 5：ScrollViewer 行为测试

在 ProvidersPage.axaml 中临时删除 ScrollViewer，添加大量测试数据，观察内容是否溢出。重新加上 ScrollViewer，确认滚动正常。

### 练习 6：实现 GridSplitter

创建一个包含 GridSplitter 的布局，允许用户拖动调整左右面板的大小。

### 练习 7：实现 WrapPanel 自适应布局

创建一个使用 WrapPanel 的标签云，添加不同长度的标签，观察自动换行行为。

### 练习 8：布局性能测试

创建一个包含 1000 个元素的页面，分别使用 Grid、StackPanel、Canvas 测量渲染性能。
