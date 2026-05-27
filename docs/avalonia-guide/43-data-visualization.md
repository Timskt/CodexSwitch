# 37. 数据可视化 -- 图表与图形

> **写给零基础的你**：数据可视化就是把数字变成图表——折线图、柱状图、饼图等。就像 Excel 里那些漂亮的图表，但你可以在自己的应用里自定义每一个细节。

## 37.1 概述

本章涵盖 Avalonia 应用的数据可视化方案：

- **LiveCharts2**：功能丰富的交互式图表库
- **ScottPlot**：轻量级科学绘图
- **OxyPlot**：经典图表库
- **实时数据图表**：动态更新的图表
- **自定义图表**：使用 Avalonia 的绘图 API 手绘图表

## 37.2 LiveCharts2（推荐）

### 37.2.1 安装配置

```xml
<PackageReference Include="LiveChartsCore.SkiaSharpView.Avalonia" Version="2.*" />
```

### 37.2.2 基本图表

```xml
<Window xmlns:lvc="clr-namespace:LiveChartsCore.SkiaSharpView.Avalonia;assembly=LiveChartsCore.SkiaSharpView.Avalonia">
    <lvc:CartesianChart Series="{Binding Series}" />
</Window>
```

```csharp
public partial class ChartViewModel : ViewModelBase
{
    public ISeries[] Series { get; set; } =
    [
        new LineSeries<double>
        {
            Values = [2, 1, 3, 5, 3, 4, 6],
            Name = "销售额"
        },
        new LineSeries<double>
        {
            Values = [1, 3, 2, 4, 2, 3, 5],
            Name = "利润"
        }
    ];

    public Axis[] XAxes { get; set; } =
    [
        new Axis
        {
            Labels = ["周一", "周二", "周三", "周四", "周五", "周六", "周日"]
        }
    ];
}
```

### 37.2.3 柱状图

```csharp
public ISeries[] BarSeries { get; set; } =
[
    new ColumnSeries<double>
    {
        Values = [3, 7, 2, 9, 4],
        Name = "销量"
    },
    new ColumnSeries<double>
    {
        Values = [5, 3, 6, 2, 8],
        Name = "目标"
    }
];
```

### 37.2.4 饼图

```csharp
public ISeries[] PieSeries { get; set; } =
[
    new PieSeries<double> { Values = [40], Name = "产品A" },
    new PieSeries<double> { Values = [25], Name = "产品B" },
    new PieSeries<double> { Values = [20], Name = "产品C" },
    new PieSeries<double> { Values = [15], Name = "产品D" }
];
```

### 37.2.5 散点图

```csharp
public ISeries[] ScatterSeries { get; set; } =
[
    new ScatterSeries<double, double>
    {
        Values = new ObservableCollection<double[]>
        {
            new[] { 1.0, 2.0 }, new[] { 3.0, 4.0 },
            new[] { 5.0, 3.0 }, new[] { 7.0, 6.0 }
        },
        GeometrySize = 10
    }
];
```

### 37.2.6 自定义样式

```csharp
public ISeries[] StyledSeries { get; set; } =
[
    new LineSeries<double>
    {
        Values = [1, 3, 2, 5, 4],
        Stroke = new SolidColorPaint(SKColors.CornflowerBlue, 3),
        Fill = new SolidColorPaint(SKColors.CornflowerBlue.WithAlpha(50)),
        GeometryStroke = new SolidColorPaint(SKColors.CornflowerBlue, 2),
        GeometryFill = new SolidColorPaint(SKColors.White),
        GeometrySize = 8,
        LineSmoothness = 0.5 // 0 = 折线, 1 = 曲线
    }
];
```

### 37.2.7 图表交互

```csharp
public ChartViewModel()
{
    // 添加工具提示
    var tooltip = new DefaultTooltip();

    // 添加缩放
    var zoomMode = ZoomAndPanMode.Both;

    // 监听数据点点击
    Series[0].ChartPointPointerDown += (sender, point) =>
    {
        Console.WriteLine($"点击了数据点: {point.Model}");
    };
}
```

## 37.3 ScottPlot

### 37.3.1 安装配置

```xml
<PackageReference Include="ScottPlot.Avalonia" Version="5.*" />
```

### 37.3.2 基本使用

```xml
<Window xmlns:sp="clr-namespace:ScottPlot.Avalonia;assembly=ScottPlot.Avalonia">
    <sp:AvaPlot x:Name="Plot" />
</Window>
```

```csharp
public partial class ScottPlotWindow : Window
{
    public ScottPlotWindow()
    {
        InitializeComponent();

        // 添加数据
        double[] xs = [1, 2, 3, 4, 5];
        double[] ys = [1, 4, 9, 16, 25];

        Plot.Plot.Add.Scatter(xs, ys);
        Plot.Plot.Title("简单散点图");
        Plot.Plot.XLabel("X 轴");
        Plot.Plot.YLabel("Y 轴");

        Plot.Refresh();
    }
}
```

### 37.3.3 实时更新

```csharp
public partial class RealtimePlot : Window
{
    private readonly ScottPlot.Plottables.Scatter _scatter;
    private readonly DispatcherTimer _timer;
    private readonly List<double> _data = new();

    public RealtimePlot()
    {
        InitializeComponent();

        _scatter = Plot.Plot.Add.Scatter(new double[] { }, new double[] { });

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += UpdateData;
        _timer.Start();
    }

    private void UpdateData(object? sender, EventArgs e)
    {
        _data.Add(Random.Shared.NextDouble() * 100);
        if (_data.Count > 200) _data.RemoveAt(0);

        _scatter.Update(
            Enumerable.Range(0, _data.Count).Select(i => (double)i).ToArray(),
            _data.ToArray());

        Plot.Refresh();
    }
}
```

## 37.4 OxyPlot

### 37.4.1 安装配置

```xml
<PackageReference Include="OxyPlot.Avalonia" Version="2.*" />
```

### 37.4.2 基本使用

```xml
<Window xmlns:oxy="clr-namespace:OxyPlot.Avalonia;assembly=OxyPlot.Avalonia">
    <oxy:PlotView Model="{Binding PlotModel}" />
</Window>
```

```csharp
public class OxyPlotViewModel
{
    public PlotModel Model { get; } = new();

    public OxyPlotViewModel()
    {
        Model.Title = "销售趋势";

        var lineSeries = new LineSeries
        {
            Title = "月销售额",
            MarkerType = MarkerType.Circle,
            MarkerSize = 4
        };

        lineSeries.Points.Add(new DataPoint(1, 10));
        lineSeries.Points.Add(new DataPoint(2, 15));
        lineSeries.Points.Add(new DataPoint(3, 12));
        lineSeries.Points.Add(new DataPoint(4, 20));

        Model.Series.Add(lineSeries);
    }
}
```

## 37.5 实时数据仪表盘

### 37.5.1 使用 LiveCharts2 实现实时仪表盘

```csharp
public partial class DashboardViewModel : ViewModelBase
{
    private readonly ObservableCollection<double> _cpuValues = new();
    private readonly ObservableCollection<double> _memoryValues = new();
    private readonly DispatcherTimer _timer;

    public ISeries[] Series { get; set; }

    public DashboardViewModel()
    {
        // 初始化数据（20 个点）
        for (int i = 0; i < 20; i++)
        {
            _cpuValues.Add(0);
            _memoryValues.Add(0);
        }

        Series =
        [
            new LineSeries<double>
            {
                Values = _cpuValues,
                Name = "CPU %",
                Stroke = new SolidColorPaint(SKColors.CornflowerBlue, 2),
                GeometrySize = 0
            },
            new LineSeries<double>
            {
                Values = _memoryValues,
                Name = "内存 %",
                Stroke = new SolidColorPaint(SKColors.OrangeRed, 2),
                GeometrySize = 0
            }
        ];

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += UpdateMetrics;
        _timer.Start();
    }

    private void UpdateMetrics(object? sender, EventArgs e)
    {
        var cpu = GetCpuUsage();
        var memory = GetMemoryUsage();

        _cpuValues.Add(cpu);
        _memoryValues.Add(memory);

        if (_cpuValues.Count > 60)
        {
            _cpuValues.RemoveAt(0);
            _memoryValues.RemoveAt(0);
        }
    }
}
```

### 37.5.2 仪表盘布局

```xml
<Grid RowDefinitions="Auto,*" ColumnDefinitions="*,*">
    <!-- 概览卡片 -->
    <StackPanel Grid.Row="0" Grid.ColumnSpan="2"
                Orientation="Horizontal" Spacing="16" Margin="16">
        <Border Background="#2D2D2D" CornerRadius="8" Padding="16">
            <StackPanel>
                <TextBlock Text="CPU 使用率" Opacity="0.7" FontSize="12" />
                <TextBlock Text="{Binding CpuUsage, StringFormat={}{0:F1}%}"
                           FontSize="28" FontWeight="Bold" />
            </StackPanel>
        </Border>
        <Border Background="#2D2D2D" CornerRadius="8" Padding="16">
            <StackPanel>
                <TextBlock Text="内存使用" Opacity="0.7" FontSize="12" />
                <TextBlock Text="{Binding MemoryUsage, StringFormat={}{0:F1}%}"
                           FontSize="28" FontWeight="Bold" />
            </StackPanel>
        </Border>
    </StackPanel>

    <!-- 实时图表 -->
    <lvc:CartesianChart Grid.Row="1" Grid.Column="0"
                        Series="{Binding Series}"
                        Margin="8" />

    <!-- 饼图 -->
    <lvc:PieChart Grid.Row="1" Grid.Column="1"
                  Series="{Binding PieSeries}"
                  Margin="8" />
</Grid>
```

## 37.6 自定义图表控件

### 37.6.1 使用 SkiaSharp 手绘图表

```csharp
public class CustomChart : Control
{
    public static readonly StyledProperty<IEnumerable<double>> DataProperty =
        AvaloniaProperty.Register<CustomChart, IEnumerable<double>>(nameof(Data));

    public IEnumerable<double> Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var data = Data?.ToArray();
        if (data == null || data.Length < 2) return;

        var bounds = new Rect(Bounds.Size);
        var max = data.Max();
        var min = data.Min();
        var range = max - min;
        if (range == 0) range = 1;

        // 绘制背景网格
        var gridPen = new Pen(Brushes.Gray, 0.5);
        for (int i = 0; i <= 4; i++)
        {
            var y = bounds.Height * i / 4;
            context.DrawLine(gridPen, new Point(0, y), new Point(bounds.Width, y));
        }

        // 绘制折线
        var linePen = new Pen(Brushes.CornflowerBlue, 2);
        var points = data.Select((v, i) => new Point(
            i * bounds.Width / (data.Length - 1),
            bounds.Height - (v - min) / range * bounds.Height
        )).ToArray();

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], false);
            for (int i = 1; i < points.Length; i++)
            {
                ctx.LineTo(points[i]);
            }
            ctx.EndFigure(false);
        }
        context.DrawGeometry(null, linePen, geometry);

        // 绘制面积填充
        var fillBrush = new SolidColorBrush(Colors.CornflowerBlue, 0.2);
        var areaGeometry = new StreamGeometry();
        using (var ctx = areaGeometry.Open())
        {
            ctx.BeginFigure(new Point(0, bounds.Height), true);
            foreach (var p in points)
            {
                ctx.LineTo(p);
            }
            ctx.LineTo(new Point(bounds.Width, bounds.Height));
            ctx.EndFigure(true);
        }
        context.DrawGeometry(fillBrush, null, areaGeometry);
    }
}
```

### 37.6.2 环形进度图

```csharp
public class RingProgress : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<RingProgress, double>(nameof(Progress), 0.5);

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, Math.Clamp(value, 0, 1));
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = size / 2 - 10;
        var strokeWidth = 12;

        // 背景环
        var bgPen = new Pen(new SolidColorBrush(Colors.Gray, 0.2), strokeWidth);
        context.DrawEllipse(null, bgPen, center, radius, radius);

        // 进度环
        var progressPen = new Pen(Brushes.CornflowerBlue, strokeWidth,
            new PenLineCap(RoundCap: PenLineCap.Round));

        var sweepAngle = Progress * 360;
        var startAngle = -90; // 从顶部开始

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var startRad = startAngle * Math.PI / 180;
            var endRad = (startAngle + sweepAngle) * Math.PI / 180;

            var startPoint = new Point(
                center.X + radius * Math.Cos(startRad),
                center.Y + radius * Math.Sin(startRad));

            ctx.BeginFigure(startPoint, false);
            ctx.ArcTo(
                new Point(
                    center.X + radius * Math.Cos(endRad),
                    center.Y + radius * Math.Sin(endRad)),
                new Size(radius, radius),
                0, sweepAngle > 180, SweepDirection.Clockwise);
            ctx.EndFigure(false);
        }
        context.DrawGeometry(null, progressPen, geometry);

        // 中心文字
        var text = $"{Progress * 100:F0}%";
        var textLayout = new TextLayout(text, new Typeface("Arial"), 20, Brushes.White,
            TextAlignment.Center);
        textLayout.Draw(context, new Point(center.X, center.Y - 10));
    }
}
```

## 37.7 图表库对比

| 特性 | LiveCharts2 | ScottPlot | OxyPlot |
|------|------------|-----------|---------|
| Avalonia 支持 | 官方支持 | 官方支持 | 社区支持 |
| 图表类型 | 丰富 | 中等 | 丰富 |
| 实时更新 | 优秀 | 良好 | 良好 |
| MVVM 绑定 | 原生支持 | 基本 | 基本 |
| 学习曲线 | 中等 | 低 | 中等 |
| 社区活跃度 | 高 | 高 | 中等 |
| 许可证 | MIT | MIT | MIT |

## 37.8 Cross References

- **第 14 章**：自定义渲染（手绘图表的基础）
- **第 10 章**：动画与过渡（图表动画效果）
- **第 29 章**：形状与矢量绘图（图表中的图形元素）

## 37.9 Common Pitfalls

1. **数据量过大**：数万个数据点会导致渲染卡顿，需要数据降采样
2. **UI 线程更新**：图表数据更新必须在 UI 线程
3. **内存泄漏**：不再使用的图表需要释放资源
4. **SkiaSharp 版本冲突**：LiveCharts2 和 ScottPlot 可能依赖不同版本的 SkiaSharp
5. **高 DPI 显示**：图表需要考虑 DPI 缩放
6. **颜色对比度**：确保图表颜色在暗色/亮色主题下都清晰可见

## 37.10 Try It Yourself

1. 使用 LiveCharts2 创建一个带折线图和柱状图的仪表盘
2. 实现实时数据更新的监控图表（每秒刷新）
3. 创建自定义环形进度控件
4. 实现图表的缩放和拖拽交互
