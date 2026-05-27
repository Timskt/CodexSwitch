# 11. 自定义渲染

当内置控件无法满足需求时，可以重写 `Render(DrawingContext)` 方法进行自定义绘制。

## 11.1 渲染基础

Avalonia 的渲染管线：

```
MeasureOverride() → ArrangeOverride() → Render()
     测量               排列              绘制
```

- `MeasureOverride`: 计算控件需要多大空间
- `ArrangeOverride`: 在分配的空间内确定最终尺寸
- `Render`: 在画布上绘制内容

## 11.2 CsRollingNumber - 数字滚动控件

这是一个完全自定义的数字显示控件，实现了平滑的数字过渡动画。

### 属性定义

```csharp
public sealed class CsRollingNumber : Control
{
    // 注册 StyledProperty
    public static readonly StyledProperty<long> ValueProperty =
        AvaloniaProperty.Register<CsRollingNumber, long>(nameof(Value));

    public static readonly StyledProperty<bool> UseCompactFormatProperty =
        AvaloniaProperty.Register<CsRollingNumber, bool>(nameof(UseCompactFormat));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<CsRollingNumber, double>(nameof(FontSize), 15d);

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        AvaloniaProperty.Register<CsRollingNumber, FontWeight>(nameof(FontWeight), FontWeight.SemiBold);

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<CsRollingNumber, IBrush?>(nameof(Foreground), Brushes.White);

    static CsRollingNumber()
    {
        // 告诉属性系统：这些属性变化时需要重新测量/重绘
        AffectsMeasure<CsRollingNumber>(ValueProperty, UseCompactFormatProperty, FontSizeProperty, FontWeightProperty);
        AffectsRender<CsRollingNumber>(ValueProperty, UseCompactFormatProperty, FontSizeProperty, FontWeightProperty, ForegroundProperty);

        // 属性变更处理器
        ValueProperty.Changed.AddClassHandler<CsRollingNumber>((number, args) =>
        {
            if (args.NewValue is long value)
                number.OnValueChanged(value);
        });
    }
}
```

### 测量

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    // 测量当前显示文本和目标文本，取最大值
    var displayText = FormatValue((long)Math.Round(_displayValue), UseCompactFormat);
    var targetText = FormatValue(Value, UseCompactFormat);
    var displayLayout = CreateTextLayout(displayText);
    var targetLayout = CreateTextLayout(targetText);

    return new Size(
        Math.Ceiling(Math.Max(displayLayout.Width, targetLayout.Width)),
        Math.Ceiling(Math.Max(displayLayout.Height, targetLayout.Height)));
}
```

### TextLayout 创建

```csharp
private TextLayout CreateTextLayout(string text)
{
    return new TextLayout(
        text,
        new Typeface(AppFonts.DefaultFontFamily, FontStyle.Normal, FontWeight, FontStretch.Normal),
        FontSize,
        Foreground ?? Brushes.White,
        textAlignment: TextAlignment.Left,
        textWrapping: TextWrapping.NoWrap);
}
```

`TextLayout` 是 Avalonia 的文本排版类，类似 WPF 的 `FormattedText`。

### 渲染（带动画）

```csharp
public override void Render(DrawingContext context)
{
    base.Render(context);

    var text = FormatValue((long)Math.Round(_displayValue), UseCompactFormat);
    var layout = CreateTextLayout(text);
    var y = Math.Round((Bounds.Height - layout.Height) / 2d);  // 垂直居中

    if (_animationTimer is null)
    {
        // 无动画：直接绘制
        layout.Draw(context, new Point(0, y));
        return;
    }

    // 有动画：双层绘制（旧值淡出 + 新值淡入）
    var progress = GetAnimationProgress();
    var eased = EaseOutCubic(progress);
    var travel = Math.Min(12d, Math.Max(5d, FontSize * 0.55d));

    using var clip = context.PushClip(new Rect(Bounds.Size));

    // 旧文本：透明度从 1→0，向上移动
    using (context.PushOpacity(1d - eased))
    {
        var oldText = FormatValue((long)Math.Round(_startValue), UseCompactFormat);
        var oldLayout = CreateTextLayout(oldText);
        oldLayout.Draw(context, new Point(0, y - travel * eased));
    }

    // 新文本：透明度从 0.35→1，从下方移入
    using (context.PushOpacity(0.35d + 0.65d * eased))
    {
        layout.Draw(context, new Point(0, y + travel * (1d - eased)));
    }
}
```

### DrawingContext API

| 方法 | 作用 |
|------|------|
| `context.DrawRectangle(brush, pen, rect)` | 绘制矩形 |
| `context.DrawEllipse(brush, pen, center, rx, ry)` | 绘制椭圆 |
| `context.DrawLine(pen, p1, p2)` | 绘制线段 |
| `context.DrawGeometry(brush, pen, geometry)` | 绘制几何图形 |
| `context.DrawText(layout, origin)` | 绘制文本 |
| `context.PushClip(rect)` | 裁剪区域 |
| `context.PushOpacity(opacity)` | 透明度 |
| `context.PushTransform(matrix)` | 变换矩阵 |
| `context.PushRenderOptions(options)` | 渲染选项 |

## 11.3 CsActivityArrow - 动态方向箭头

另一个自定义渲染控件，展示双脉冲动画效果：

```csharp
public sealed class CsActivityArrow : Control
{
    // 双脉冲颜色插值
    public override void Render(DrawingContext context)
    {
        var activeColor = _isActive ? _activeColor : _foreground;
        var alpha = 0.35d + 0.65d * _pulsePhase;

        // 使用颜色插值
        var lerpedColor = LerpColor(_foreground, activeColor, alpha);

        // 绘制箭头路径
        var geometry = CreateArrowGeometry(Direction);
        context.DrawGeometry(
            new SolidColorBrush(lerpedColor),
            null,
            geometry);
    }

    // 颜色线性插值
    private static Color LerpColor(Color from, Color to, double amount)
    {
        return Color.FromArgb(
            (byte)(from.A + (to.A - from.A) * amount),
            (byte)(from.R + (to.R - from.R) * amount),
            (byte)(from.G + (to.G - from.G) * amount),
            (byte)(from.B + (to.B - from.B) * amount));
    }
}
```

## 11.4 视觉树生命周期

```csharp
public sealed class CsRollingNumber : Control
{
    private bool _isAttached;

    // 加入视觉树时
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        if (!_hasValue)
            SetImmediateValue(Value);
    }

    // 离开视觉树时
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _isAttached = false;
        StopAnimation();  // 停止定时器，防止内存泄漏
    }
}
```

### 关键：释放资源

当控件离开视觉树时，必须停止 `DispatcherTimer`：
```csharp
private void StopAnimation()
{
    if (_animationTimer is null) return;

    _animationTimer.Tick -= OnAnimationTick;  // 解除事件订阅
    _animationTimer.Stop();
    _animationTimer = null;
}
```

## 11.5 性能优化

### InvalidateMeasure vs InvalidateVisual

```csharp
// 尺寸变化时调用（会触发布局重算）
InvalidateMeasure();

// 仅外观变化时调用（只触发重绘，更快）
InvalidateVisual();
```

### 避免在 Render 中分配

```csharp
// 坏：每次 Render 都创建新对象
public override void Render(DrawingContext context)
{
    var brush = new SolidColorBrush(Colors.Red);  // 每帧分配！
    context.DrawRectangle(brush, ...);
}

// 好：缓存画刷
private static readonly SolidColorBrush CachedBrush = new(Colors.Red);
public override void Render(DrawingContext context)
{
    context.DrawRectangle(CachedBrush, ...);
}
```

### ClipToBounds

```csharp
public CsRollingNumber()
{
    ClipToBounds = true;  // 裁剪超出边界的内容
}
```

## 11.6 自定义控件 vs 用户控件

| 特性 | 自定义控件 (Control) | 用户控件 (UserControl) |
|------|---------------------|----------------------|
| 渲染 | 完全自定义 | 使用子控件组合 |
| 模板 | 可定义 ControlTemplate | 固定的 AXAML 布局 |
| 性能 | 更好（更少的视觉元素） | 稍差（更多的对象） |
| 灵活性 | 高 | 中 |
| 用途 | 基础控件、高性能场景 | 表单、页面 |

CodexSwitch 的 `CsRollingNumber` 和 `CsActivityArrow` 是自定义控件（直接绘制），而 `ProviderCard` 等是用户控件（组合现有控件）。
