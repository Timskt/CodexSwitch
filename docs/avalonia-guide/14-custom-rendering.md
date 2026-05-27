# 14. 自定义渲染

## 14.1 渲染管线

```
MeasureOverride() → ArrangeOverride() → Render()
     测量               排列              绘制
```

## 14.2 CsRollingNumber

```csharp
public sealed class CsRollingNumber : Control
{
    // 属性定义
    public static readonly StyledProperty<long> ValueProperty =
        AvaloniaProperty.Register<CsRollingNumber, long>(nameof(Value));

    static CsRollingNumber()
    {
        AffectsMeasure<CsRollingNumber>(ValueProperty, ...);
        AffectsRender<CsRollingNumber>(ValueProperty, ...);
    }

    // 测量
    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = CreateTextLayout(FormatValue(Value, UseCompactFormat));
        return new Size(Math.Ceiling(layout.Width), Math.Ceiling(layout.Height));
    }

    // 渲染
    public override void Render(DrawingContext context)
    {
        var layout = CreateTextLayout(text);
        var y = Math.Round((Bounds.Height - layout.Height) / 2d);

        // 旧文本淡出上移
        using (context.PushOpacity(1d - eased))
            oldLayout.Draw(context, new Point(0, y - travel * eased));

        // 新文本淡入下移
        using (context.PushOpacity(0.35d + 0.65d * eased))
            layout.Draw(context, new Point(0, y + travel * (1d - eased)));
    }
}
```

## 14.3 DrawingContext API

| 方法 | 作用 |
|------|------|
| `DrawRectangle` | 绘制矩形 |
| `DrawEllipse` | 绘制椭圆 |
| `DrawLine` | 绘制线段 |
| `DrawGeometry` | 绘制几何图形 |
| `DrawText` | 绘制文本 |
| `PushClip` | 裁剪区域 |
| `PushOpacity` | 透明度 |
| `PushTransform` | 变换矩阵 |

## 14.4 性能优化

```csharp
// 好：缓存画刷
private static readonly SolidColorBrush CachedBrush = new(Colors.Red);

// 坏：每次 Render 都创建
var brush = new SolidColorBrush(Colors.Red);  // ❌
```

## 14.5 生命周期

```csharp
protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    _isAttached = true;
}

protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    _isAttached = false;
    StopAnimation();  // 必须停止定时器
}
```
