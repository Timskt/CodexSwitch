# 4. 布局系统

## 4.1 布局基础

Avalonia 的布局系统基于两个步骤：**测量 (Measure)** 和 **排列 (Arrange)**。

### 测量阶段

每个控件通过 `MeasureOverride(Size availableSize)` 告诉父控件它需要多大空间：

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    // 测量子元素
    _child.Measure(availableSize);

    // 返回所需大小
    return new Size(
        Math.Min(_child.DesiredSize.Width, availableSize.Width),
        Math.Min(_child.DesiredSize.Height, availableSize.Height));
}
```

### 排列阶段

父控件通过 `ArrangeOverride(Size finalSize)` 确定每个子元素的最终位置和大小：

```csharp
protected override Size ArrangeOverride(Size finalSize)
{
    // 在分配的空间内排列子元素
    _child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
    return finalSize;
}
```

### 布局流程详解

Avalonia 的布局流程是一个递归过程：

1. **测量阶段（Measure Pass）**：
   - 从根节点开始
   - 父节点调用子节点的 `Measure()` 方法
   - 子节点计算自己的 `DesiredSize`
   - 父节点根据子节点的 `DesiredSize` 计算自己的 `DesiredSize`

2. **排列阶段（Arrange Pass）**：
   - 从根节点开始
   - 父节点调用子节点的 `Arrange()` 方法
   - 子节点根据分配的空间确定自己的位置和大小
   - 子节点调用自己的 `ArrangeOverride()` 来排列自己的子节点

3. **渲染阶段（Render Pass）**：
   - 遍历视觉树
   - 每个控件调用自己的 `Render()` 方法
   - 绘制到屏幕上

### MeasureOverride 的返回值

`MeasureOverride` 返回的 Size 代表控件"想要"的大小：

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    // 情况 1：返回尽可能小的大小
    return new Size(0, 0);  // 控件不会占用任何空间
    
    // 情况 2：返回固定的大小
    return new Size(100, 50);  // 控件想要 100x50 的空间
    
    // 情况 3：返回根据内容计算的大小
    _child.Measure(availableSize);
    return _child.DesiredSize;  // 控件想要子元素的大小
    
    // 情况 4：返回不超过可用空间的大小
    _child.Measure(availableSize);
    return new Size(
        Math.Min(_child.DesiredSize.Width, availableSize.Width),
        Math.Min(_child.DesiredSize.Height, availableSize.Height));
}
```

### ArrangeOverride 的参数

`ArrangeOverride` 的参数 `finalSize` 是父控件分配的空间：

```csharp
protected override Size ArrangeOverride(Size finalSize)
{
    // finalSize 可能比 DesiredSize 大或小
    // 控件应该在 finalSize 范围内排列自己
    
    // 情况 1：使用全部分配的空间
    _child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
    return finalSize;
    
    // 情况 2：使用部分分配的空间
    _child.Arrange(new Rect(0, 0, _child.DesiredSize.Width, _child.DesiredSize.Height));
    return finalSize;
    
    // 情况 3：居中排列
    var x = (finalSize.Width - _child.DesiredSize.Width) / 2;
    var y = (finalSize.Height - _child.DesiredSize.Height) / 2;
    _child.Arrange(new Rect(x, y, _child.DesiredSize.Width, _child.DesiredSize.Height));
    return finalSize;
}
```

## 4.2 Grid — 最强大的布局面板

Grid 是 Avalonia 中最常用的布局面板，支持行和列定义。

### 基本用法

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>     <!-- 自适应高度 -->
        <RowDefinition Height="*"/>        <!-- 剩余空间 -->
        <RowDefinition Height="100"/>      <!-- 固定高度 -->
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="220"/>    <!-- 固定宽度 -->
        <ColumnDefinition Width="*"/>      <!-- 剩余空间 -->
    </Grid.ColumnDefinitions>

    <!-- 放置在第 0 行、第 0 列 -->
    <TextBlock Grid.Row="0" Grid.Column="0" Text="Sidebar"/>

    <!-- 放置在第 0 行、第 1 列 -->
    <ContentControl Grid.Row="0" Grid.Column="1"/>

    <!-- 跨越多行 -->
    <Border Grid.Row="0" Grid.Column="1" Grid.RowSpan="2"/>
</Grid>
```

### CodexSwitch 中的 Grid 布局

```xml
<!-- MainWindow.axaml 主布局 -->
<Grid ColumnDefinitions="220,*">
    <!-- 侧边栏：固定 220px 宽 -->
    <ui:CodexSidebar>
        <Grid RowDefinitions="Auto,*,Auto">
            <!-- Header：自适应高度 -->
            <ui:CodexSidebarHeader>...</ui:CodexSidebarHeader>

            <!-- Content：占满剩余空间 -->
            <ui:CodexSidebarContent Grid.Row="1">...</ui:CodexSidebarContent>

            <!-- Footer：自适应高度 -->
            <ui:CodexSidebarFooter Grid.Row="2">...</ui:CodexSidebarFooter>
        </Grid>
    </ui:CodexSidebar>

    <!-- 主内容区：占满剩余空间 -->
    <Grid Grid.Column="1" RowDefinitions="64,*">
        <!-- 顶栏：固定 64px 高 -->
        <shell:TopBar/>

        <!-- 页面内容：占满剩余空间 -->
        <Grid Grid.Row="1">
            <pages:HomePage IsVisible="{Binding IsHomePageVisible}"/>
            <pages:ProvidersPage IsVisible="{Binding IsProvidersPageVisible}"/>
        </Grid>
    </Grid>
</Grid>
```

### Height/Width 的三种模式

| 模式 | 示例 | 说明 |
|------|------|------|
| 固定值 | `Height="100"` | 精确像素值 |
| Auto | `Height="Auto"` | 根据内容自适应 |
| 比例 | `Height="*"` | 按比例分配剩余空间 |
| 加权比例 | `Height="2*"` | 2 倍比例 |

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>   <!-- 第 1 行：内容决定高度 -->
    <RowDefinition Height="*"/>      <!-- 第 2 行：占 1/3 剩余空间 -->
    <RowDefinition Height="2*"/>     <!-- 第 3 行：占 2/3 剩余空间 -->
</Grid.RowDefinitions>
```

### Grid 的测量算法

Grid 的测量过程比较复杂：

1. **固定行/列**：直接使用指定的大小
2. **Auto 行/列**：测量所有子元素，取最大值
3. **比例行/列**：分配剩余空间

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="100"/>  <!-- 固定：100px -->
        <RowDefinition Height="Auto"/>  <!-- Auto：测量子元素 -->
        <RowDefinition Height="*"/>     <!-- 比例：剩余空间 -->
    </Grid.RowDefinitions>
</Grid>
```

### SharedSizeGroup

`SharedSizeGroup` 允许多个 Grid 共享行/列的大小：

```xml
<Grid Grid.IsSharedSizeScope="True">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" SharedSizeGroup="Label"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <TextBlock Text="Name:"/>
    <TextBox Grid.Column="1"/>
</Grid>

<Grid Grid.IsSharedSizeScope="True">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" SharedSizeGroup="Label"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <TextBlock Text="Email:"/>
    <TextBox Grid.Column="1"/>
</Grid>
```

## 4.3 StackPanel — 线性排列

```xml
<!-- 垂直排列（默认） -->
<StackPanel Spacing="8">
    <TextBlock Text="Item 1"/>
    <TextBlock Text="Item 2"/>
    <TextBlock Text="Item 3"/>
</StackPanel>

<!-- 水平排列 -->
<StackPanel Orientation="Horizontal" Spacing="10">
    <Button Content="Save"/>
    <Button Content="Cancel"/>
</StackPanel>
```

### Spacing 属性

`Spacing` 控制子元素之间的间距，等价于每个子元素的 `Margin`：

```xml
<!-- 这两种写法等价 -->
<StackPanel Spacing="8">
    <TextBlock Text="A"/>
    <TextBlock Text="B"/>
</StackPanel>

<StackPanel>
    <TextBlock Text="A" Margin="0,0,0,8"/>
    <TextBlock Text="B"/>
</StackPanel>
```

### StackPanel 的测量行为

StackPanel 在测量时：
1. 给子元素提供无限的空间（在排列方向上）
2. 累加所有子元素的 DesiredSize
3. 加上 Spacing

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    var totalHeight = 0.0;
    var maxWidth = 0.0;
    
    foreach (var child in Children)
    {
        child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        totalHeight += child.DesiredSize.Height;
        maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
    }
    
    // 加上 Spacing
    if (Children.Count > 1)
        totalHeight += Spacing * (Children.Count - 1);
    
    return new Size(maxWidth, totalHeight);
}
```

## 4.4 DockPanel — 停靠布局

```xml
<DockPanel>
    <!-- 顶部停靠 -->
    <Border DockPanel.Dock="Top" Height="64" Background="Blue">
        <TextBlock Text="Top Bar"/>
    </Border>

    <!-- 左侧停靠 -->
    <Border DockPanel.Dock="Left" Width="220" Background="Green">
        <TextBlock Text="Sidebar"/>
    </Border>

    <!-- 底部停靠 -->
    <Border DockPanel.Dock="Bottom" Height="40" Background="Red">
        <TextBlock Text="Footer"/>
    </Border>

    <!-- 剩余空间（最后一个元素不需要指定 Dock） -->
    <Border Background="White">
        <TextBlock Text="Content"/>
    </Border>
</DockPanel>
```

### 停靠顺序很重要

元素按添加顺序停靠。先添加的元素先占据边缘位置。

### DockPanel 的测量行为

DockPanel 在测量时：
1. 按照停靠顺序处理每个子元素
2. 已停靠的元素从可用空间中扣除
3. 最后一个元素获得剩余空间

```xml
<!-- 示例：元素顺序影响布局 -->
<DockPanel>
    <!-- 先停靠 Top -->
    <Border DockPanel.Dock="Top" Height="50"/>
    
    <!-- 再停靠 Left -->
    <Border DockPanel.Dock="Left" Width="100"/>
    
    <!-- 最后是内容 -->
    <Border/>
</DockPanel>
```

## 4.5 Canvas — 绝对定位

```xml
<Canvas>
    <!-- 绝对坐标定位 -->
    <Rectangle Canvas.Left="50" Canvas.Top="30"
               Width="100" Height="50"
               Fill="Red"/>

    <!-- 右下角定位 -->
    <TextBlock Canvas.Right="10" Canvas.Bottom="10"
               Text="Bottom Right"/>
</Canvas>
```

Canvas 不参与测量/排列流程，子元素需要手动设置位置。

### Canvas 的特殊行为

Canvas 有几个特殊行为：
1. 不限制子元素的大小
2. 不限制子元素的位置
3. 子元素可以超出 Canvas 的边界
4. 不参与虚拟化

```xml
<!-- 子元素可以超出边界 -->
<Canvas Width="100" Height="100">
    <!-- 这个矩形超出了 Canvas 的边界 -->
    <Rectangle Canvas.Left="50" Canvas.Top="50"
               Width="200" Height="200"
               Fill="Red"/>
</Canvas>
```

## 4.6 WrapPanel — 自动换行

```xml
<WrapPanel ItemWidth="120" ItemHeight="80">
    <Border Background="Red"/>
    <Border Background="Green"/>
    <Border Background="Blue"/>
    <!-- 当空间不够时自动换行 -->
</WrapPanel>
```

### WrapPanel 的测量行为

WrapPanel 在测量时：
1. 尝试在当前行排列子元素
2. 如果当前行空间不足，换到下一行
3. 计算所有行的总高度

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    var lineWidth = 0.0;
    var lineHeight = 0.0;
    var totalHeight = 0.0;
    var maxWidth = 0.0;
    
    foreach (var child in Children)
    {
        child.Measure(new Size(ItemWidth ?? availableSize.Width, ItemHeight ?? availableSize.Height));
        
        if (lineWidth + child.DesiredSize.Width > availableSize.Width)
        {
            // 换行
            totalHeight += lineHeight;
            lineWidth = 0;
            lineHeight = 0;
        }
        
        lineWidth += child.DesiredSize.Width;
        lineHeight = Math.Max(lineHeight, child.DesiredSize.Height);
        maxWidth = Math.Max(maxWidth, lineWidth);
    }
    
    totalHeight += lineHeight;
    return new Size(maxWidth, totalHeight);
}
```

## 4.7 UniformGrid — 均匀网格

```xml
<UniformGrid Columns="3">
    <Border Background="Red"/>
    <Border Background="Green"/>
    <Border Background="Blue"/>
    <Border Background="Yellow"/>
    <Border Background="Purple"/>
    <Border Background="Orange"/>
</UniformGrid>
```

UniformGrid 会自动将子元素排列成均匀的网格。

## 4.8 ScrollViewer — 滚动容器

```xml
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Auto">
    <StackPanel>
        <!-- 很多内容 -->
    </StackPanel>
</ScrollViewer>
```

### ScrollViewer 的测量行为

ScrollViewer 在测量时：
1. 给子元素提供无限的空间（在可滚动的方向上）
2. 子元素可以任意大小
3. ScrollViewer 本身使用可用空间

```xml
<!-- 垂直滚动 -->
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <StackPanel>
        <!-- 子元素可以很高 -->
    </StackPanel>
</ScrollViewer>

<!-- 水平滚动 -->
<ScrollViewer HorizontalScrollBarVisibility="Auto">
    <StackPanel Orientation="Horizontal">
        <!-- 子元素可以很宽 -->
    </StackPanel>
</ScrollViewer>
```

## 4.9 对齐与边距

### HorizontalAlignment 和 VerticalAlignment

```xml
<!-- 水平对齐 -->
<TextBlock HorizontalAlignment="Left"/>    <!-- 左对齐 -->
<TextBlock HorizontalAlignment="Center"/>  <!-- 居中 -->
<TextBlock HorizontalAlignment="Right"/>   <!-- 右对齐 -->
<TextBlock HorizontalAlignment="Stretch"/> <!-- 拉伸填满 -->

<!-- 垂直对齐 -->
<TextBlock VerticalAlignment="Top"/>
<TextBlock VerticalAlignment="Center"/>
<TextBlock VerticalAlignment="Bottom"/>
<TextBlock VerticalAlignment="Stretch"/>
```

### Margin 和 Padding

```xml
<!-- Margin：外边距（控件外部的空间） -->
<Button Margin="10,20,10,20"/>  <!-- 左,上,右,下 -->
<Button Margin="10"/>           <!-- 四边相同 -->

<!-- Padding：内边距（控件内部的空间） -->
<Border Padding="16,12">
    <TextBlock Text="Content"/>
</Border>
```

## 4.10 常用布局属性速查

| 属性 | 类型 | 说明 |
|------|------|------|
| `Width` / `Height` | double | 固定尺寸 |
| `MinWidth` / `MinHeight` | double | 最小尺寸 |
| `MaxWidth` / `MaxHeight` | double | 最大尺寸 |
| `Margin` | Thickness | 外边距 |
| `Padding` | Thickness | 内边距 |
| `HorizontalAlignment` | enum | 水平对齐 |
| `VerticalAlignment` | enum | 垂直对齐 |
| `IsVisible` | bool | 是否可见（占位） |
| `Opacity` | double | 透明度 (0-1) |
| `ZIndex` | int | 层叠顺序 |
| `ClipToBounds` | bool | 是否裁剪超出边界的内容 |

---

## Deep Dive

### MeasureOverride/ArrangeOverride 详解

**MeasureOverride 的约束**：

`availableSize` 参数代表父控件提供的可用空间：

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    // availableSize.Width 可能是：
    // - 具体数值：父控件提供的宽度
    // - double.PositiveInfinity：父控件不限制宽度（如 StackPanel）
    
    // availableSize.Height 可能是：
    // - 具体数值：父控件提供的高度
    // - double.PositiveInfinity：父控件不限制高度
    
    // 控件应该返回不超过 availableSize 的大小
    // 除非 availableSize 是 Infinite
}
```

**ArrangeOverride 的灵活性**：

```csharp
protected override Size ArrangeOverride(Size finalSize)
{
    // finalSize 是父控件分配的空间
    // 控件可以使用全部或部分空间
    
    // 例 1：使用全部空间
    _child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
    
    // 例 2：使用子元素的 DesiredSize
    _child.Arrange(new Rect(0, 0, _child.DesiredSize.Width, _child.DesiredSize.Height));
    
    // 例 3：居中排列
    var x = (finalSize.Width - _child.DesiredSize.Width) / 2;
    var y = (finalSize.Height - _child.DesiredSize.Height) / 2;
    _child.Arrange(new Rect(x, y, _child.DesiredSize.Width, _child.DesiredSize.Height));
    
    return finalSize;
}
```

### 布局舍入（Layout Rounding）

布局舍入可以避免子像素渲染导致的模糊：

```xml
<!-- 启用布局舍入 -->
<Grid UseLayoutRounding="True">
    <!-- 子元素的位置和大小会被舍入到整数像素 -->
</Grid>
```

### 像素对齐（Snap to Pixel）

像素对齐确保控件的边缘与物理像素对齐：

```xml
<!-- 启用像素对齐 -->
<TextBlock SnapsToDevicePixels="True"
           TextOptions.TextRenderingMode="Alias"/>
```

### 自定义面板

创建自定义面板需要重写 `MeasureOverride` 和 `ArrangeOverride`：

```csharp
public class CircularPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var radius = Math.Min(availableSize.Width, availableSize.Height) / 2;
        
        foreach (var child in Children)
        {
            child.Measure(new Size(radius, radius));
        }
        
        return new Size(radius * 2, radius * 2);
    }
    
    protected override Size ArrangeOverride(Size finalSize)
    {
        var radius = Math.Min(finalSize.Width, finalSize.Height) / 2;
        var angleStep = 360.0 / Children.Count;
        
        for (int i = 0; i < Children.Count; i++)
        {
            var angle = i * angleStep * Math.PI / 180;
            var x = radius + radius * Math.Cos(angle) - Children[i].DesiredSize.Width / 2;
            var y = radius + radius * Math.Sin(angle) - Children[i].DesiredSize.Height / 2;
            
            Children[i].Arrange(new Rect(x, y, 
                Children[i].DesiredSize.Width, 
                Children[i].DesiredSize.Height));
        }
        
        return finalSize;
    }
}
```

### 布局失效（Layout Invalidation）

当控件的属性变化时，需要触发布局失效：

```csharp
public class MyControl : Control
{
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<MyControl, double>(nameof(Spacing));
    
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == SpacingProperty)
        {
            // 触发布局失效
            InvalidateMeasure();
        }
    }
}
```

### 性能优化

**1. 避免过度测量**：
- 不要在 `MeasureOverride` 中创建新对象
- 缓存测量结果
- 避免在测量时触发事件

**2. 选择合适的面板**：
- 简单布局使用 StackPanel
- 复杂布局使用 Grid
- 大量数据使用 VirtualizingStackPanel
- 绝对定位使用 Canvas

**3. 避免嵌套过深**：
- 减少布局容器的嵌套层级
- 使用 Grid 代替嵌套的 StackPanel

**4. 使用虚拟化**：
- 对大量数据使用 VirtualizingStackPanel
- 设置合理的虚拟化范围

---

## Cross References

- **[第 1 章：Avalonia 概览](01-avalonia-overview.md)** — 了解 Avalonia 的渲染管线
- **[第 3 章：AXAML 基础](03-axaml-fundamentals.md)** — 学习 AXAML 中的布局语法
- **[第 5 章：数据绑定](05-data-binding.md)** — 理解布局与绑定的交互
- **[第 22 章：属性系统](22-property-system.md)** — 深入了解 AvaloniaProperty
- **[第 23 章：视觉树与逻辑树](23-visual-logical-tree.md)** — 理解布局在视觉树中的作用

---

## Common Pitfalls

### 1. StackPanel 中的无限高度

**问题**：StackPanel 给子元素提供无限空间，导致子元素无法正确测量。

```xml
<!-- 错误：StackPanel 中的 TextBox 无法知道自己的高度 -->
<StackPanel>
    <TextBox AcceptsReturn="True"/>
</StackPanel>

<!-- 正确：使用 Grid 或设置固定高度 -->
<Grid>
    <TextBox AcceptsReturn="True"/>
</Grid>
```

### 2. 忘记设置 Grid.Row/Grid.Column

**问题**：在 Grid 中放置元素时忘记设置行/列索引。

```xml
<!-- 错误：没有设置 Grid.Row 和 Grid.Column -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    
    <TextBlock Text="Header"/>
    <TextBlock Text="Content"/>  <!-- 默认在第 0 行，第 0 列 -->
</Grid>

<!-- 正确：显式设置 Grid.Row -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    
    <TextBlock Text="Header"/>
    <TextBlock Grid.Row="1" Text="Content"/>
</Grid>
```

### 3. Grid 中的比例分配错误

**问题**：多个 * 行/列分配不均。

```xml
<!-- 错误：三个 * 行，每个占 1/3 -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
        <RowDefinition Height="*"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
</Grid>

<!-- 正确：使用加权比例 -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="*"/>      <!-- 1/4 -->
        <RowDefinition Height="2*"/>     <!-- 2/4 -->
        <RowDefinition Height="*"/>      <!-- 1/4 -->
    </Grid.RowDefinitions>
</Grid>
```

### 4. Canvas 中的子元素超出边界

**问题**：Canvas 不限制子元素位置，子元素可能超出边界。

```xml
<!-- 错误：子元素超出 Canvas 边界 -->
<Canvas Width="100" Height="100">
    <Rectangle Canvas.Left="50" Canvas.Top="50"
               Width="200" Height="200"/>
</Canvas>

<!-- 正确：使用 ClipToBounds -->
<Canvas Width="100" Height="100" ClipToBounds="True">
    <Rectangle Canvas.Left="50" Canvas.Top="50"
               Width="200" Height="200"/>
</Canvas>
```

### 5. 忘记 ScrollViewer

**问题**：内容超出容器时没有滚动条。

```xml
<!-- 错误：内容被裁剪 -->
<Border Height="200">
    <StackPanel>
        <!-- 很多内容 -->
    </StackPanel>
</Border>

<!-- 正确：使用 ScrollViewer -->
<Border Height="200">
    <ScrollViewer>
        <StackPanel>
            <!-- 很多内容 -->
        </StackPanel>
    </ScrollViewer>
</Border>
```

### 6. 嵌套过深的布局

**问题**：布局容器嵌套过深，影响性能。

```xml
<!-- 错误：嵌套过深 -->
<StackPanel>
    <StackPanel>
        <StackPanel>
            <TextBlock Text="Content"/>
        </StackPanel>
    </StackPanel>
</StackPanel>

<!-- 正确：使用 Grid -->
<Grid>
    <TextBlock Text="Content"/>
</Grid>
```

### 7. 忘记 UseLayoutRounding

**问题**：子像素渲染导致模糊。

```xml
<!-- 错误：没有启用布局舍入 -->
<Grid>
    <TextBlock Text="Hello"/>
</Grid>

<!-- 正确：启用布局舍入 -->
<Grid UseLayoutRounding="True">
    <TextBlock Text="Hello"/>
</Grid>
```

---

## Try It Yourself

### 练习 1：创建基本布局

1. 创建一个简单的登录表单布局：
   ```xml
   <Grid RowDefinitions="Auto,*,Auto">
       <!-- Header -->
       <StackPanel Grid.Row="0" Margin="16">
           <TextBlock Text="Login" FontSize="24"/>
       </StackPanel>
       
       <!-- Content -->
       <StackPanel Grid.Row="1" Margin="16" Spacing="12">
           <TextBox Watermark="Username"/>
           <TextBox Watermark="Password" PasswordChar="*"/>
           <Button Content="Login"/>
       </StackPanel>
       
       <!-- Footer -->
       <TextBlock Grid.Row="2" Text="Forgot password?" 
                  HorizontalAlignment="Center" Margin="16"/>
   </Grid>
   ```

2. 运行项目，观察布局效果

3. 尝试调整 Grid 的行定义

### 练习 2：探索 CodexSwitch 的布局

1. 打开 `Views/MainWindow.axaml`

2. 分析布局结构：
   - 主 Grid 的列定义
   - 侧边栏的行定义
   - 内容区的行定义

3. 尝试修改布局：
   - 改变侧边栏宽度
   - 改变顶栏高度
   - 添加新的行或列

4. 运行项目，观察布局变化

### 练习 3：创建自定义面板

1. 创建一个圆形面板：
   ```csharp
   public class CircularPanel : Panel
   {
       protected override Size MeasureOverride(Size availableSize)
       {
           var radius = Math.Min(availableSize.Width, availableSize.Height) / 2;
           
           foreach (var child in Children)
           {
               child.Measure(new Size(radius, radius));
           }
           
           return new Size(radius * 2, radius * 2);
       }
       
       protected override Size ArrangeOverride(Size finalSize)
       {
           var radius = Math.Min(finalSize.Width, finalSize.Height) / 2;
           var angleStep = 360.0 / Children.Count;
           
           for (int i = 0; i < Children.Count; i++)
           {
               var angle = i * angleStep * Math.PI / 180;
               var x = radius + radius * Math.Cos(angle) - Children[i].DesiredSize.Width / 2;
               var y = radius + radius * Math.Sin(angle) - Children[i].DesiredSize.Height / 2;
               
               Children[i].Arrange(new Rect(x, y, 
                   Children[i].DesiredSize.Width, 
                   Children[i].DesiredSize.Height));
           }
           
           return finalSize;
       }
   }
   ```

2. 在 AXAML 中使用：
   ```xml
   <local:CircularPanel Width="300" Height="300">
       <Button Content="1"/>
       <Button Content="2"/>
       <Button Content="3"/>
       <Button Content="4"/>
   </local:CircularPanel>
   ```

3. 运行项目，观察圆形布局

### 练习 4：实现复杂布局

1. 创建一个仪表板布局：
   ```xml
   <Grid RowDefinitions="Auto,*,Auto" ColumnDefinitions="*,*,*">
       <!-- Header -->
       <Border Grid.Row="0" Grid.ColumnSpan="3" 
               Background="#1E293B" Padding="16">
           <TextBlock Text="Dashboard" Foreground="White"/>
       </Border>
       
       <!-- Sidebar -->
       <Border Grid.Row="1" Grid.Column="0" 
               Background="#334155" Padding="16">
           <StackPanel Spacing="8">
               <Button Content="Home"/>
               <Button Content="Settings"/>
               <Button Content="Profile"/>
           </StackPanel>
       </Border>
       
       <!-- Main Content -->
       <Border Grid.Row="1" Grid.Column="1" Grid.ColumnSpan="2"
               Background="#F1F5F9" Padding="16">
           <TextBlock Text="Main Content"/>
       </Border>
       
       <!-- Footer -->
       <Border Grid.Row="2" Grid.ColumnSpan="3"
               Background="#1E293B" Padding="16">
           <TextBlock Text="Footer" Foreground="White"/>
       </Border>
   </Grid>
   ```

2. 运行项目，观察布局效果

3. 尝试调整列宽比例

### 练习 5：测试布局性能

1. 创建一个包含大量子元素的布局：
   ```xml
   <ScrollViewer>
       <StackPanel>
           <!-- 重复 1000 次 -->
           <Border Height="50" Margin="4" Background="#E2E8F0">
               <TextBlock Text="Item 1"/>
           </Border>
           <!-- ... -->
       </StackPanel>
   </ScrollViewer>
   ```

2. 使用 VirtualizingStackPanel 替代 StackPanel：
   ```xml
   <ScrollViewer>
       <VirtualizingStackPanel>
           <!-- 重复 1000 次 -->
       </VirtualizingStackPanel>
   </ScrollViewer>
   ```

3. 比较两种方式的内存占用和滚动性能

### 练习 6：实现响应式布局

1. 创建一个响应式布局：
   ```xml
   <Grid>
       <Grid.Styles>
           <!-- 窄屏：单列 -->
           <Style Selector="Grid">
               <Setter Property="ColumnDefinitions" Value="*"/>
           </Style>
           
           <!-- 宽屏：两列 -->
           <Style Selector="Grid[Classes.wide]">
               <Setter Property="ColumnDefinitions" Value="*,*"/>
           </Style>
       </Grid.Styles>
       
       <Border Background="#3B82F6" Grid.Column="0">
           <TextBlock Text="Column 1"/>
       </Border>
       <Border Background="#10B981" Grid.Column="1">
           <TextBlock Text="Column 2"/>
       </Border>
   </Grid>
   ```

2. 根据窗口大小动态添加/移除 `wide` class

3. 运行项目，观察响应式效果

### 练习 7：调试布局问题

1. 创建一个有布局问题的页面：
   ```xml
   <Grid>
       <TextBlock Text="This text is cut off because the container is too small"
                  TextTrimming="CharacterEllipsis"/>
   </Grid>
   ```

2. 使用 Avalonia DevTools 检查：
   - 控件的 DesiredSize
   - 控件的 Bounds
   - 布局约束

3. 修复布局问题

4. 尝试其他布局问题：
   - 子元素超出边界
   - 对齐不正确
   - 间距不一致
