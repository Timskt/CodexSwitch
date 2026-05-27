# 14. 自定义渲染

> **写给零基础的你**：渲染就是"画画"。前面的章节用现成的控件（按钮、文本框等）来搭界面，本章教你用"画笔"自己画——画线条、画圆形、画文字，想画什么画什么。适合做图表、游戏画面、自定义图形等。

## 14.1 概述

自定义渲染是创建视觉效果最底层、最灵活的方式。学完本章，你将能够：

- 理解 Avalonia 的渲染管线（Measure -> Arrange -> Render）
- 熟练使用 `DrawingContext` 的所有绘制方法
- 掌握 `PushClip`、`PushOpacity`、`PushTransform` 的作用域管理
- 使用 `StreamGeometry` 和 `PathGeometry` 创建自定义路径
- 使用 `RenderTargetBitmap` 实现离屏渲染
- 理解 Pen 和 Brush 的所有类型
- 在自定义渲染和自定义控件之间做出正确的选择

CodexSwitch 中有三个典型的自定义渲染控件：`CsRollingNumber`（文本渲染 + 动画）、`CsActivityArrow`（几何图形渲染 + 颜色插值）和 `CsSegmentedControl`（模板部件 + 位移动画）。本章将深入讲解每个控件的渲染实现。

## 14.2 核心概念

### 14.2.1 渲染管线

> **小白提示**：渲染管线就像"画画的三个步骤"：
> 1. **测量**（MeasureOverride）= 先量一下画布要多大
> 2. **排列**（ArrangeOverride）= 把画布放到桌子上的哪个位置
> 3. **渲染**（Render）= 在画布上画画

```
MeasureOverride() -> ArrangeOverride() -> Render()
     测量大小           排列位置           绘制内容
```

- `MeasureOverride`：计算控件需要多大空间（"我需要一张 A4 纸"）
- `ArrangeOverride`：在分配的空间内确定最终位置和大小（"把纸放在桌子左上角"）
- `Render`：使用 `DrawingContext` 绘制像素（"开始画画"）

### 14.2.2 DrawingContext 完整 API

> **小白提示**：`DrawingContext` 就像你的"画笔工具箱"。里面有各种工具：`DrawRectangle` 画矩形、`DrawEllipse` 画椭圆、`DrawText` 写文字、`DrawImage` 贴图片。你用这些工具在"画布"上创作。

`DrawingContext` 是 Avalonia 的绘图接口，所有自定义绘制都通过它完成。

**绘制方法**

| 方法 | 作用 | 参数 |
|------|------|------|
| `DrawRectangle` | 绘制矩形 | brush, pen, rect, radiusX, radiusY, boxShadows |
| `DrawEllipse` | 绘制椭圆 | brush, pen, center, radiusX, radiusY |
| `DrawLine` | 绘制线段 | pen, point1, point2 |
| `DrawGeometry` | 绘制几何路径 | brush, pen, geometry |
| `DrawImage` | 绘制图像 | source, rect |
| `DrawText` | 绘制文本（FormattedText） | formattedText, origin |

**状态管理方法（Push 模式）**

| 方法 | 作用 | 返回 |
|------|------|------|
| `PushClip` | 设置裁剪区域 | `IDisposable` |
| `PushOpacity` | 设置透明度 | `IDisposable` |
| `PushTransform` | 应用变换矩阵 | `IDisposable` |
| `PushGeometryClip` | 设置几何裁剪 | `IDisposable` |
| `PushOpacityMask` | 设置透明度遮罩 | `IDisposable` |

### 14.2.3 Push 模式详解

`DrawingContext` 的 `Push*` 方法返回 `IDisposable`，形成作用域：

```csharp
using (context.PushOpacity(0.5))
{
    // 此处绘制的内容透明度为 0.5
    context.DrawRectangle(brush, null, rect);

    using (context.PushClip(clipRect))
    {
        // 此处同时受 Opacity 和 Clip 影响
        layout.Draw(context, point);
    }
    // Clip 作用域结束，但 Opacity 仍为 0.5
}
// Opacity 恢复到 PushOpacity 之前的值
```

嵌套使用：

```csharp
using (context.PushOpacity(0.8))                    // 外层透明度 0.8
{
    context.DrawRectangle(bgBrush, null, outerRect);

    using (context.PushClip(innerRect))              // 裁剪到内部区域
    {
        using (context.PushTransform(someMatrix))    // 应用变换
        {
            using (context.PushOpacity(0.5))         // 内层透明度 0.5（叠加后为 0.4）
            {
                layout.Draw(context, point);
            }
        }
    }
}
```

### 14.2.4 Pen 和 Brush 详解

**Brush 类型**

```csharp
// 纯色画刷
var solidBrush = new SolidColorBrush(Colors.White);
var solidBrush2 = new SolidColorBrush(Color.Parse("#FF5733"));

// 线性渐变画刷
var linearBrush = new LinearGradientBrush
{
    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
    GradientStops =
    {
        new GradientStop(Colors.Blue, 0),
        new GradientStop(Colors.Red, 1)
    }
};

// 径向渐变画刷
var radialBrush = new RadialGradientBrush
{
    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
    RadiusX = new RelativePoint(0.5, RelativeUnit.Relative),
    RadiusY = new RelativePoint(0.5, RelativeUnit.Relative),
    GradientStops =
    {
        new GradientStop(Colors.White, 0),
        new GradientStop(Colors.Transparent, 1)
    }
};
```

**Pen**

```csharp
// 基本 Pen
var pen = new Pen(Brushes.White, 2.0);

// 完整 Pen
var pen = new Pen(
    brush: new SolidColorBrush(Colors.White),
    thickness: 2.0,
    dashStyle: DashStyle.Dash,
    lineCap: PenLineCap.Round,       // 线端样式
    lineJoin: PenLineJoin.Round,     // 线连接样式
    miterLimit: 10);                 // 斜接限制

// 预定义 DashStyle（不设置 DashStyle 或设为 null 即为实线）
DashStyle.Dash      // 虚线
DashStyle.Dot       // 点线
DashStyle.DashDot   // 点划线
DashStyle.DashDotDot // 双点划线
```

### 14.2.5 坐标系

```
(0,0) ---------> X (右)
  |
  |
  v
  Y (下)
```

Avalonia 的坐标系原点在左上角，Y 轴向下。所有绘制坐标都是相对于控件左上角的局部坐标。

## 14.3 进阶用法

### 14.3.1 DrawRectangle 绘制矩形

```csharp
// 纯填充矩形
context.DrawRectangle(brush, null, new Rect(10, 10, 100, 50));

// 纯描边矩形
context.DrawRectangle(null, pen, new Rect(10, 10, 100, 50));

// 填充 + 描边
context.DrawRectangle(brush, pen, new Rect(10, 10, 100, 50));

// 圆角矩形
context.DrawRectangle(brush, pen, new Rect(10, 10, 100, 50), 8, 8);

// 不同圆角（通过 CornerRadius 需要使用 DrawGeometry）
```

### 14.3.2 DrawEllipse 绘制椭圆

```csharp
// 圆形
var center = new Point(50, 50);
context.DrawEllipse(brush, pen, center, 30, 30);

// 椭圆
context.DrawEllipse(brush, pen, center, 40, 20);
```

### 14.3.3 DrawLine 绘制线段

```csharp
// 基本线段
var pen = new Pen(Brushes.White, 2);
context.DrawLine(pen, new Point(0, 0), new Point(100, 100));

// 带箭头的线段（需要自己绘制箭头头部）
var tip = new Point(100, 50);
var tail = new Point(0, 50);
var headLeft = new Point(85, 35);
var headRight = new Point(85, 65);

context.DrawLine(pen, tail, tip);     // 箭杆
context.DrawLine(pen, tip, headLeft); // 左翼
context.DrawLine(pen, tip, headRight);// 右翼
```

### 14.3.4 DrawGeometry 绘制几何路径

```csharp
// 使用 StreamGeometry 绘制自定义路径
var geometry = new StreamGeometry();
using (var ctx = geometry.Open())
{
    ctx.BeginFigure(new Point(10, 50), isFilled: true);
    ctx.LineTo(new Point(50, 10));
    ctx.LineTo(new Point(90, 50));
    ctx.LineTo(new Point(70, 50));
    ctx.LineTo(new Point(70, 90));
    ctx.LineTo(new Point(30, 90));
    ctx.LineTo(new Point(30, 50));
    ctx.EndFigure(isClosed: true);
}

context.DrawGeometry(brush, pen, geometry);
```

**StreamGeometry 的绘图命令**

```csharp
using var ctx = geometry.Open();

// 开始图形
ctx.BeginFigure(startPoint, isFilled: true);

// 直线
ctx.LineTo(new Point(100, 100));

// 三次贝塞尔曲线
ctx.CubicTo(
    new Point(50, 0),    // 控制点 1
    new Point(100, 50),   // 控制点 2
    new Point(100, 100),  // 终点
    isStroked: true,
    isSmoothJoin: true);

// 二次贝塞尔曲线
ctx.QuadraticTo(
    new Point(50, 50),    // 控制点
    new Point(100, 100),  // 终点
    isStroked: true,
    isSmoothJoin: true);

// 弧线
ctx.ArcTo(
    new Point(100, 100),  // 终点
    new Size(50, 50),     // 半径
    rotationAngle: 0,     // 旋转角度
    isLargeArc: false,    // 是否大弧
    sweep: SweepDirection.Clockwise);

// 结束图形
ctx.EndFigure(isClosed: true);  // true = 闭合路径
```

### 14.3.5 DrawText 绘制文本

```csharp
// 使用 TextLayout
var text = "Hello, World!";
var typeface = new Typeface("Arial", FontStyle.Normal, FontWeight.Normal);
var layout = new TextLayout(text, typeface, 16, Brushes.White);
layout.Draw(context, new Point(10, 10));

// 使用 FormattedText（更底层）
var formattedText = new FormattedText(
    text,
    CultureInfo.CurrentCulture,
    FlowDirection.LeftToRight,
    new Typeface("Arial"),
    16,
    Brushes.White);
context.DrawText(formattedText, new Point(10, 10));
```

### 14.3.6 DrawImage 绘制图像

```csharp
// 绘制整个图像
var image = new Bitmap("path/to/image.png");
context.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));

// 绘制图像的一部分（裁剪源）
var sourceRect = new Rect(0, 0, 100, 100); // 源图像的裁剪区域
var destRect = new Rect(10, 10, 200, 200); // 目标区域
context.DrawImage(image, sourceRect, destRect);
```

### 14.3.7 PushTransform 应用变换

```csharp
// 平移
using (context.PushTransform(Matrix.CreateTranslation(50, 50)))
{
    // 此处绘制的内容偏移 (50, 50)
    context.DrawRectangle(brush, null, new Rect(0, 0, 100, 50));
}

// 缩放
using (context.PushTransform(Matrix.CreateScale(2, 2)))
{
    // 此处绘制的内容放大 2 倍
}

// 旋转（绕中心点旋转 45 度）
var center = new Point(50, 50);
using (context.PushTransform(Matrix.CreateRotation(45 * Math.PI / 180, center)))
{
    // 此处绘制的内容绕 center 旋转 45 度
}

// 组合变换（先缩放，再旋转，最后平移）
var transform = Matrix.CreateScale(2, 2)
    * Matrix.CreateRotation(45 * Math.PI / 180, center)
    * Matrix.CreateTranslation(100, 0);
using (context.PushTransform(transform))
{
    // ...
}
```

### 14.3.8 PushClip 裁剪

```csharp
// 矩形裁剪
using (context.PushClip(new Rect(0, 0, 100, 100)))
{
    // 只有在 (0,0)-(100,100) 区域内的绘制才会显示
    layout.Draw(context, new Point(-20, -20)); // 部分被裁剪
}

// 圆角裁剪（使用 StreamGeometry 构建圆角矩形路径）
var clipGeometry = new StreamGeometry();
using (var ctx = clipGeometry.Open())
{
    ctx.BeginFigure(new Point(8, 0), true);
    ctx.ArcTo(new Point(0, 8), new Size(8, 8), 0, false, SweepDirection.CounterClockwise);
    ctx.LineTo(new Point(0, 92));
    ctx.ArcTo(new Point(8, 100), new Size(8, 8), 0, false, SweepDirection.CounterClockwise);
    ctx.LineTo(new Point(92, 100));
    ctx.ArcTo(new Point(100, 92), new Size(8, 8), 0, false, SweepDirection.CounterClockwise);
    ctx.LineTo(new Point(100, 8));
    ctx.ArcTo(new Point(92, 0), new Size(8, 8), 0, false, SweepDirection.CounterClockwise);
    ctx.EndFigure(true);
}
using (context.PushGeometryClip(clipGeometry))
{
    // 此处绘制的内容被圆角矩形裁剪
}

// 圆形裁剪
var circleClip = new EllipseGeometry(new Rect(0, 0, 100, 100));
using (context.PushGeometryClip(circleClip))
{
    // 此处绘制的内容被圆形裁剪
}
```

### 14.3.9 StreamGeometry 与自定义路径

**绘制圆弧进度条**

```csharp
public override void Render(DrawingContext context)
{
    var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
    var radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - StrokeThickness / 2;

    // 背景圆环
    context.DrawEllipse(null, new Pen(Brushes.Gray, StrokeThickness), center, radius, radius);

    // 进度弧
    if (Progress > 0)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var startAngle = -90;  // 从顶部开始
            var sweepAngle = Progress * 360;
            var startRad = startAngle * Math.PI / 180;
            var endRad = (startAngle + sweepAngle) * Math.PI / 180;

            var startPoint = new Point(
                center.X + radius * Math.Cos(startRad),
                center.Y + radius * Math.Sin(startRad));
            var endPoint = new Point(
                center.X + radius * Math.Cos(endRad),
                center.Y + radius * Math.Sin(endRad));

            ctx.BeginFigure(startPoint, false);
            ctx.ArcTo(endPoint,
                new Size(radius, radius),
                0,
                sweepAngle > 180,
                SweepDirection.Clockwise);
        }

        context.DrawGeometry(null, new Pen(Foreground, StrokeThickness), geometry);
    }
}
```

### 14.3.10 RenderTargetBitmap — 离屏渲染

`RenderTargetBitmap` 允许在内存中创建图像，然后绘制到其他地方：

```csharp
// 创建离屏位图
var bitmap = new RenderTargetBitmap(new PixelSize(200, 100), new Vector(96, 96));

// 在位图上绘制
using (var ctx = bitmap.CreateDrawingContext())
{
    ctx.DrawRectangle(Brushes.White, null, new Rect(0, 0, 200, 100));
    ctx.DrawText(formattedText, new Point(10, 10));
    ctx.DrawEllipse(Brushes.Blue, null, new Point(100, 50), 30, 30);
}

// 将位图绘制到 DrawingContext
context.DrawImage(bitmap, new Rect(0, 0, 200, 100));
```

**使用场景**

| 场景 | 说明 |
|------|------|
| 缓存复杂绘制 | 一次绘制，多次使用 |
| 缩略图 | 生成控件的缩略图 |
| 导出 | 将控件导出为图片 |
| 打印 | 将内容绘制到打印上下文 |

## 14.4 组件详解大全

### 14.4.1 CsRollingNumber — 文本渲染

**属性与脏标记**

```csharp
public sealed class CsRollingNumber : Control
{
    public static readonly StyledProperty<long> ValueProperty =
        AvaloniaProperty.Register<CsRollingNumber, long>(nameof(Value));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<CsRollingNumber, double>(nameof(FontSize), 15d);

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<CsRollingNumber, IBrush?>(nameof(Foreground), Brushes.White);

    static CsRollingNumber()
    {
        AffectsMeasure<CsRollingNumber>(
            ValueProperty, UseCompactFormatProperty, FontSizeProperty, FontWeightProperty);
        AffectsRender<CsRollingNumber>(
            ValueProperty, UseCompactFormatProperty, FontSizeProperty,
            FontWeightProperty, ForegroundProperty);
    }
}
```

**测量：TextLayout 决定尺寸**

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    var displayText = FormatValue((long)Math.Round(_displayValue), UseCompactFormat);
    var targetText = FormatValue(Value, UseCompactFormat);
    var displayLayout = CreateTextLayout(displayText);
    var targetLayout = CreateTextLayout(targetText);

    // 取两种文本的最大尺寸，防止动画过程中文字被裁剪
    return new Size(
        Math.Ceiling(Math.Max(displayLayout.Width, targetLayout.Width)),
        Math.Ceiling(Math.Max(displayLayout.Height, targetLayout.Height)));
}
```

**Render：双层透明度叠加**

```csharp
public override void Render(DrawingContext context)
{
    base.Render(context);

    if (_animationTimer is null)
    {
        // 静态状态：直接绘制
        layout.Draw(context, new Point(0, y));
        return;
    }

    var progress = GetAnimationProgress();
    var eased = EaseOutCubic(progress);
    var travel = Math.Min(12d, Math.Max(5d, FontSize * 0.55d));

    // 裁剪区域防止文本超出控件边界
    using var clip = context.PushClip(new Rect(Bounds.Size));

    // 旧文本：淡出上移
    using (context.PushOpacity(1d - eased))
    {
        oldLayout.Draw(context, new Point(0, y - travel * eased));
    }

    // 新文本：淡入下移
    using (context.PushOpacity(0.35d + 0.65d * eased))
    {
        layout.Draw(context, new Point(0, y + travel * (1d - eased)));
    }
}
```

### 14.4.2 CsActivityArrow — 几何图形渲染

**箭头绘制**

```csharp
private void DrawArrow(DrawingContext context, double offsetY, IBrush brush, double opacity)
{
    var direction = Direction >= 0d ? 1d : -1d;
    var centerX = Bounds.Width / 2d;
    var centerY = Bounds.Height / 2d + offsetY;
    var shaftLength = Math.Max(7d, FontSize * 0.72d);
    var headLength = Math.Max(3d, FontSize * 0.28d);
    var headWidth = Math.Max(3d, FontSize * 0.26d);
    var thickness = Math.Max(1.7d, FontSize * 0.16d);

    // 箭头几何：尖端、尾部、头部两翼
    var tip = new Point(centerX, centerY + direction * shaftLength / 2d);
    var tail = new Point(centerX, centerY - direction * shaftLength / 2d);
    var headBaseY = tip.Y - direction * headLength;
    var left = new Point(centerX - headWidth, headBaseY);
    var right = new Point(centerX + headWidth, headBaseY);

    var pen = new Pen(brush, thickness);

    using var pushedOpacity = context.PushOpacity(Math.Clamp(opacity, 0d, 1d));
    context.DrawLine(pen, tail, tip);    // 箭杆
    context.DrawLine(pen, tip, left);    // 左翼
    context.DrawLine(pen, tip, right);   // 右翼
}
```

**颜色插值 (Lerp)**

```csharp
private IBrush CreatePulseBrush(double amount, double opacity)
{
    var start = ResolveColor(Foreground, FallbackForegroundColor);
    var end = ResolveColor(ActiveForeground, FallbackActiveForegroundColor);
    var color = Lerp(start, end, Math.Clamp(amount, 0d, 1d));
    return new SolidColorBrush(color);
}

private static Color Lerp(Color start, Color end, double amount)
{
    return Color.FromArgb(
        LerpByte(start.A, end.A, amount),
        LerpByte(start.R, end.R, amount),
        LerpByte(start.G, end.G, amount),
        LerpByte(start.B, end.B, amount));
}

private static byte LerpByte(byte start, byte end, double amount)
{
    return (byte)Math.Round(start + (end - start) * amount);
}
```

颜色 Lerp 对 ARGB 四个通道分别插值，实现平滑的颜色过渡。

## 14.5 举一反三

### 14.5.1 自定义渲染 vs 自定义控件

| 场景 | 自定义渲染 | 自定义控件/模板 |
|------|-----------|---------------|
| 绘制几何图形 | `Render` | 不适合 |
| 绘制文本 | `Render` + `TextLayout` | `TextBlock` |
| 绘制图表 | `Render` | 不适合 |
| 绘制图标 | `Render` + `DrawGeometry` | Path 控件 |
| 标准 UI 元素 | 不适合 | 控件 + 模板 |
| 动画效果 | `Render` | Transitions/Animations |

### 14.5.2 与 SkiaSharp 的对比

| 特性 | Avalonia DrawingContext | SkiaSharp |
|------|----------------------|-----------|
| 抽象级别 | 高（UI 框架集成） | 低（直接绘图） |
| 性能 | 接近原生 | 最佳 |
| API | WPF 风格 | Skia 风格 |
| 集成 | 天然集成 | 需要手动集成 |
| 跨平台 | 自动 | 需要手动处理 |

## 14.6 最佳实践与设计模式

### 14.6.1 总是调用 base.Render

```csharp
public override void Render(DrawingContext context)
{
    base.Render(context);  // 渲染背景、边框等默认内容
    // 自定义绘制...
}
```

### 14.6.2 使用 using 管理 Push 状态

```csharp
// 好：using 确保自动恢复
using (context.PushOpacity(0.5))
{
    // ...
}

// 差：手动管理容易出错
context.PushOpacity(0.5);
// ... 如果这里抛出异常，Opacity 永远不会恢复
```

### 14.6.3 缓存画刷对象

```csharp
// 差：每帧创建新 SolidColorBrush
private IBrush CreateBrush(double amount)
{
    return new SolidColorBrush(Lerp(start, end, amount));  // GC 压力
}

// 好：缓存并复用
private readonly SolidColorBrush _cachedBrush = new();

private IBrush CreateBrush(double amount)
{
    _cachedBrush.Color = Lerp(start, end, amount);
    return _cachedBrush;
}
```

### 14.6.4 合理使用 ClipToBounds

```csharp
// 如果控件内容可能超出边界（如动画中的文本偏移），必须设置
ClipToBounds = true;
```

### 14.6.5 Render 中避免耗时操作

```csharp
// 错误：Render 中加载资源
public override void Render(DrawingContext context)
{
    var image = new Bitmap("path/to/image.png");  // 每帧加载！
    context.DrawImage(image, rect);
}

// 正确：在外部加载，Render 中只使用
private Bitmap? _cachedImage;
protected override void OnAttachedToVisualTree(...)
{
    _cachedImage = new Bitmap("path/to/image.png");
}
```

## Deep Dive：渲染管线内部原理

### Skia 渲染后端

Avalonia 默认使用 Skia 作为渲染后端。`DrawingContext` 的调用最终转换为 Skia 绘图指令：

```
context.DrawRectangle(...)
    -> SKCanvas.DrawRoundRect(...)
context.DrawLine(...)
    -> SKCanvas.DrawLine(...)
context.DrawText(...)
    -> SKCanvas.DrawText(...)
```

### 渲染帧调度

```
属性变更
    -> InvalidateVisual()
    -> 标记控件为"脏"
    -> 渲染循环在下一帧调用 Render()
    -> DrawingContext 记录绘制指令
    -> Skia 批量执行 GPU 绘制
```

Avalonia 的渲染是增量的——只有标记为"脏"的区域才会重绘。

### 文本渲染的开销

`TextLayout` 的创建涉及字体度量计算，是相对昂贵的操作。对于大段文本，应考虑缓存 `TextLayout`。CodexSwitch 的 `CsRollingNumber` 在 `Render` 和 `MeasureOverride` 中都创建 `TextLayout`，这在 60fps 的动画中是可接受的，因为数字文本通常很短。

### 画刷分配策略

每帧创建 `SolidColorBrush` 会产生 GC 压力。在性能敏感的场景中，缓存画刷对象并修改其 `Color` 属性：

```csharp
private readonly SolidColorBrush _cachedBrush = new();

private IBrush CreatePulseBrushOptimized(double amount)
{
    var color = Lerp(start, end, amount);
    _cachedBrush.Color = color;
    return _cachedBrush;
}
```

## Cross References

- **自定义控件**：自定义控件的完整生命周期，参见 [第 9 章](09-custom-controls.md)
- **动画**：DispatcherTimer 帧动画的实现，参见 [第 10 章](10-animation-transitions.md)
- **属性系统**：StyledProperty 和 AffectsRender 机制，参见 [第 22 章](22-property-system.md)
- **调试技巧**：DevTools 查看渲染区域和性能，参见 [第 21 章](21-debugging.md)
- **样式系统**：Brush 和 Color 在样式中的使用，参见 [第 7 章](07-styling-theming.md)

## Common Pitfalls

### 陷阱 1：忘记调用 base.Render

```csharp
// 错误：跳过基类渲染
public override void Render(DrawingContext context)
{
    // base.Render(context);  // 遗漏
    // 后果：背景、边框等默认渲染丢失
}

// 正确：
public override void Render(DrawingContext context)
{
    base.Render(context);
    // 自定义绘制...
}
```

### 陷阱 2：MeasureOverride 返回固定大小

```csharp
// 错误：不管内容大小都返回固定尺寸
protected override Size MeasureOverride(Size availableSize)
{
    return new Size(100, 20);
}
// 后果：文本可能被裁剪或留白过多

// 正确：根据内容计算
protected override Size MeasureOverride(Size availableSize)
{
    var layout = CreateTextLayout(text);
    return new Size(Math.Ceiling(layout.Width), Math.Ceiling(layout.Height));
}
```

### 陷阱 3：Render 中做耗时操作

```csharp
// 错误：在 Render 中加载资源
public override void Render(DrawingContext context)
{
    var data = LoadFromDisk();  // 耗时操作
}
// 后果：帧率下降，UI 卡顿
```

### 陷阱 4：未使用 PushClip 导致溢出

```csharp
// 错误：动画中的文本可能超出控件边界
using (context.PushOpacity(opacity))
{
    layout.Draw(context, new Point(0, y - travel));  // Y 可能为负
}

// 正确：先 PushClip 限制绘制区域
using var clip = context.PushClip(new Rect(Bounds.Size));
```

### 陷阱 5：IDisposable 未正确释放

```csharp
// 错误：PushOpacity 的返回值未用 using 保存
context.PushOpacity(0.5);
// Opacity 0.5 永远不会恢复

// 正确：
using (context.PushOpacity(0.5))
{
    // ...
}
```

### 陷阱 6：ClipToBounds 未设置

```csharp
// CsRollingNumber 构造函数中
public CsRollingNumber()
{
    ClipToBounds = true;  // 必须显式设置
}
```

自定义控件默认 `ClipToBounds = false`。

### 陷阱 7：每帧创建新 TextLayout

```csharp
// 差：如果文本不变，不需要每帧创建
public override void Render(DrawingContext context)
{
    var layout = new TextLayout(_cachedText, ...);  // 每帧创建
}

// 好：缓存 TextLayout
private TextLayout? _cachedLayout;
private string? _cachedText;

private TextLayout GetLayout(string text)
{
    if (_cachedText != text)
    {
        _cachedText = text;
        _cachedLayout = new TextLayout(text, ...);
    }
    return _cachedLayout!;
}
```

### 陷阱 8：DrawRectangle 的 radiusX/Y 与 CornerRadius 不同

`DrawRectangle` 的 `radiusX` 和 `radiusY` 只支持均匀圆角。如果需要四个角不同的圆角，使用 `DrawGeometry` + `RoundedRect`。

### 陷阱 9：StreamGeometry 不可变

`StreamGeometry` 创建后不可修改。如果需要动态更新路径，每次需要创建新的 `StreamGeometry`。对于频繁更新的路径，考虑使用 `PathGeometry`（可变但稍慢）。

### 陷阱 10：Render 中的坐标系是局部坐标

```csharp
// 错误：使用屏幕坐标
context.DrawRectangle(brush, null, new Rect(screenX, screenY, 100, 50));

// 正确：使用局部坐标（相对于控件左上角）
context.DrawRectangle(brush, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
```

### 陷阱 11：PushTransform 的矩阵顺序

矩阵乘法不满足交换律。变换的应用顺序是从右到左：

```csharp
// 先缩放，再旋转，最后平移
var transform = Matrix.CreateTranslation(100, 0)   // 最后应用
              * Matrix.CreateRotation(angle, center) // 第二个应用
              * Matrix.CreateScale(2, 2);           // 最先应用
```

### 陷阱 12：RenderTargetBitmap 的内存管理

`RenderTargetBitmap` 占用 GPU 内存。使用完毕后应该 Dispose：

```csharp
using var bitmap = new RenderTargetBitmap(new PixelSize(200, 100));
// 使用 bitmap...
// using 块结束后自动释放
```

## Try It Yourself

### 练习 1：实现自定义进度条

创建一个 `CsProgressBar` 控件：

```csharp
public override void Render(DrawingContext context)
{
    base.Render(context);

    // 背景矩形
    var backgroundRect = new Rect(0, 0, Bounds.Width, Bounds.Height);
    context.DrawRectangle(BackgroundBrush, null, backgroundRect, 4, 4);

    // 填充矩形
    var fillWidth = Bounds.Width * Progress;
    var fillRect = new Rect(0, 0, fillWidth, Bounds.Height);
    context.DrawRectangle(ForegroundBrush, null, fillRect, 4, 4);
}
```

### 练习 2：实现脉冲圆点

创建一个活跃状态指示器：

```csharp
public override void Render(DrawingContext context)
{
    base.Render(context);
    var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
    var radius = Math.Min(Bounds.Width, Bounds.Height) / 2;

    if (IsActive)
    {
        var progress = GetProgress();
        var pulse = Math.Sin(progress * Math.PI);
        var brush = LerpBrush(InactiveColor, ActiveColor, pulse);
        context.DrawEllipse(brush, null, center, radius, radius);
    }
    else
    {
        context.DrawEllipse(InactiveBrush, null, center, radius, radius);
    }
}
```

### 练习 3：实现自定义图标控件

使用 `DrawGeometry` 绘制矢量图标，根据 `IsActive` 属性切换颜色，添加旋转动画。

### 练习 4：实现径向进度条

创建一个环形进度控件，使用 `StreamGeometry` 的 `ArcTo` 绘制弧线。

### 练习 5：实现迷你图表

创建一个 `CsMiniChart` 控件：
- 使用 `DrawLine` 绘制折线图
- 使用渐变 Brush 填充折线下方的区域
- 支持鼠标悬停时显示具体数值

### 练习 6：性能对比

创建两个版本的动画控件：
- 版本 A：每帧创建新 `SolidColorBrush`
- 版本 B：缓存 `SolidColorBrush`，修改 `Color` 属性

使用 DevTools 的帧率监控比较两者的 GC 压力差异。

### 练习 7：实现自定义按钮渲染

创建一个按钮控件，在 `Render` 中绘制：
- 圆角矩形背景
- 渐变效果
- 点击时的波纹效果（从点击位置向外扩散）

### 练习 8：实现离屏渲染缓存

创建一个复杂的自定义控件，使用 `RenderTargetBitmap` 缓存绘制结果：
- 首次渲染时绘制到 `RenderTargetBitmap`
- 后续帧直接使用缓存的位图
- 当属性变化时重新绘制
