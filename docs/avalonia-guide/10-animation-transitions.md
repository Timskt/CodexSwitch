# 10. 动画与过渡效果

## 10.1 过渡 (Transitions) vs 动画 (Animations)

| 特性 | Transitions | Animations |
|------|------------|------------|
| 触发方式 | 属性值变化自动触发 | 手动启动/停止 |
| 用途 | 状态切换（悬停、选中） | 持续效果（加载、闪烁） |
| 语法 | 声明式（在 Style 中） | 代码或 XAML |
| 性能 | 优化过，自动管理生命周期 | 需手动管理 |
| 灵活性 | 低——只能响应属性变化 | 高——任意逻辑 |

## 10.2 样式中的 Transitions

### BrushTransition

```xml
<Style Selector="Border.app-shell">
    <Setter Property="BorderBrush" Value="{StaticResource CsBorderBrush}"/>
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.18"/>
        </Transitions>
    </Setter>
</Style>
```

### DoubleTransition

```xml
<Style Selector="Button:pointerover /template/ ContentPresenter">
    <Setter Property="Opacity" Value="0.8"/>
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.12"/>
        </Transitions>
    </Setter>
</Style>
```

### 组合过渡

```xml
<Style Selector="ui|CsSegmentedButton">
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.10"/>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.10"/>
            <BrushTransition Property="Foreground" Duration="0:0:0.10"/>
        </Transitions>
    </Setter>
</Style>
```

## 10.3 代码中的 DispatcherTimer 动画

CodexSwitch 的 `CsRollingNumber` 展示了帧动画的完整实现——当值增大时，旧数字淡出上移，新数字淡入下移，持续时间随差值动态调整。

### 核心结构

```csharp
public sealed class CsRollingNumber : Control
{
    private static readonly TimeSpan AnimationFrameInterval = TimeSpan.FromMilliseconds(16); // ~60fps
    private static readonly TimeSpan MinimumAnimationDuration = TimeSpan.FromMilliseconds(320);
    private static readonly TimeSpan MaximumAnimationDuration = TimeSpan.FromMilliseconds(820);
    private DispatcherTimer? _animationTimer;
    private DateTimeOffset _animationStartedAt;
    private TimeSpan _animationDuration;
    private double _displayValue;
    private double _startValue;
    private long _targetValue;
    private bool _hasValue;
    private bool _isAttached;
```

### 值变化时启动动画

```csharp
    private void OnValueChanged(long newValue)
    {
        if (!_hasValue)
        {
            SetImmediateValue(newValue);
            return;
        }

        if (newValue <= _displayValue)
        {
            SetImmediateValue(newValue);  // 值变小时直接设置，不动画
            return;
        }

        _startValue = _displayValue;
        _targetValue = newValue;
        _animationStartedAt = DateTimeOffset.UtcNow;
        _animationDuration = ResolveDuration(newValue - _startValue);
        StartAnimation();
    }
```

### 定时器生命周期

```csharp
    private void StartAnimation()
    {
        if (!_isAttached)
        {
            _displayValue = _targetValue;
            InvalidateMeasure();
            InvalidateVisual();
            return;
        }

        if (_animationTimer is not null)
            return;

        _animationTimer = new DispatcherTimer { Interval = AnimationFrameInterval };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();
        InvalidateVisual();
    }

    private void StopAnimation()
    {
        if (_animationTimer is null)
            return;

        _animationTimer.Tick -= OnAnimationTick;
        _animationTimer.Stop();
        _animationTimer = null;
    }
```

### 帧更新逻辑

```csharp
    private void OnAnimationTick(object? sender, EventArgs e)
    {
        var progress = GetAnimationProgress();
        if (progress >= 1d)
        {
            _displayValue = _targetValue;
            StopAnimation();
        }
        else
        {
            _displayValue = _startValue + (_targetValue - _startValue) * EaseOutCubic(progress);
        }

        InvalidateMeasure();
        InvalidateVisual();
    }

    private double GetAnimationProgress()
    {
        var elapsed = DateTimeOffset.UtcNow - _animationStartedAt;
        return Math.Clamp(elapsed.TotalMilliseconds / _animationDuration.TotalMilliseconds, 0d, 1d);
    }
```

### 动态持续时间

动画持续时间根据值差自动缩放——差值越大，动画越长，但不会超过上限：

```csharp
    private static TimeSpan ResolveDuration(double delta)
    {
        var factor = Math.Clamp(Math.Log10(delta + 1d) / 4d, 0d, 1d);
        var duration = MinimumAnimationDuration.TotalMilliseconds +
            (MaximumAnimationDuration.TotalMilliseconds - MinimumAnimationDuration.TotalMilliseconds) * factor;
        return TimeSpan.FromMilliseconds(duration);
    }
```

## 10.4 渲染动画：淡入淡出 + 位移

`CsRollingNumber.Render` 在动画进行时同时绘制旧文本和新文本，通过透明度和 Y 偏移实现滚动效果：

```csharp
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var text = FormatValue((long)Math.Round(_displayValue), UseCompactFormat);
        var layout = CreateTextLayout(text);
        var y = Math.Round((Bounds.Height - layout.Height) / 2d);

        if (_animationTimer is null)
        {
            layout.Draw(context, new Point(0, y));
            return;
        }

        var progress = GetAnimationProgress();
        var eased = EaseOutCubic(progress);
        var travel = Math.Min(12d, Math.Max(5d, FontSize * 0.55d));

        using var clip = context.PushClip(new Rect(Bounds.Size));

        // 旧文本：透明度降低，向上移动
        using (context.PushOpacity(1d - eased))
        {
            var oldText = FormatValue((long)Math.Round(_startValue), UseCompactFormat);
            var oldLayout = CreateTextLayout(oldText);
            oldLayout.Draw(context, new Point(0, y - travel * eased));
        }

        // 新文本：透明度增加，从下方移入
        using (context.PushOpacity(0.35d + 0.65d * eased))
        {
            layout.Draw(context, new Point(0, y + travel * (1d - eased)));
        }
    }
```

## 10.5 CsDialog 的淡入动画

`CsDialog` 使用 `Dispatcher.UIThread.Post` 配合 `DispatcherPriority.Render` 实现延迟一帧设置透明度，使 CSS transition 生效：

```csharp
public sealed class CsDialog : ContentControl
{
    public CsDialog()
    {
        Opacity = 0;  // 初始不可见
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // 当变为可见时
        if (change.Property != IsVisibleProperty || change.NewValue is not bool isVisible || !isVisible)
            return;

        Opacity = 0;
        // Post 到 Render 优先级，确保布局完成后再设置 Opacity=1
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => Opacity = 1,
            Avalonia.Threading.DispatcherPriority.Render);
    }
}
```

配合 AXAML 中的 `DoubleTransition`：

```xml
<Style Selector="local|CsDialog">
    <Setter Property="Transitions">
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
        </Transitions>
    </Setter>
</Style>
```

## 10.6 缓动函数

Avalonia 内置的缓动函数：

| 函数 | 效果 | 用途 |
|------|------|------|
| `LinearEasing` | 匀速 | 旋转 |
| `CubicEaseOut` | 快到慢 | 按钮点击、弹出 |
| `CubicEaseIn` | 慢到快 | 消失 |
| `CubicEaseInOut` | 慢到快到慢 | 页面切换 |
| `BounceEaseOut` | 弹跳 | 强调效果 |
| `ElasticEaseOut` | 弹性 | 弹性效果 |
| `BackEaseOut` | 回弹 | 超调效果 |

CodexSwitch 中手写的缓动函数：

```csharp
// CsRollingNumber 使用的三次方缓出
private static double EaseOutCubic(double value)
{
    var inverted = 1d - value;
    return 1d - inverted * inverted * inverted;
}

// CsActivityArrow 使用的正弦缓入缓出
private static double EaseInOutSine(double value)
{
    return -(Math.Cos(Math.PI * value) - 1d) / 2d;
}
```

## 10.7 Avalonia 内置动画系统

### XAML 关键帧动画

```xml
<Button>
    <Button.Styles>
        <Style Selector="Button:pointerover">
            <Style.Animations>
                <Animation Duration="0:0:0.3" Easing="CubicEaseOut">
                    <KeyFrame Cue="0%">
                        <Setter Property="ScaleTransform.ScaleX" Value="1"/>
                        <Setter Property="ScaleTransform.ScaleY" Value="1"/>
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="ScaleTransform.ScaleX" Value="1.05"/>
                        <Setter Property="ScaleTransform.ScaleY" Value="1.05"/>
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </Button.Styles>
</Button>
```

### 关键帧插值与多属性

```xml
<Animation Duration="0:0:1" IterationCount="Infinite" PlaybackDirection="Alternate">
    <KeyFrame Cue="0%">
        <Setter Property="Opacity" Value="0"/>
        <Setter Property="TranslateTransform.Y" Value="0"/>
    </KeyFrame>
    <KeyFrame Cue="50%">
        <Setter Property="Opacity" Value="1"/>
        <Setter Property="TranslateTransform.Y" Value="-20"/>
    </KeyFrame>
    <KeyFrame Cue="100%">
        <Setter Property="Opacity" Value="0"/>
        <Setter Property="TranslateTransform.Y" Value="0"/>
    </KeyFrame>
</Animation>
```

### 迭代与填充模式

- `IterationCount="3"`：播放 3 次
- `IterationCount="Infinite"`：无限循环
- `FillMode="None"`：动画结束后恢复初始值
- `FillMode="Forward"`：动画结束后保持最终值
- `PlaybackDirection="Alternate"`：正向播放后反向播放

## 10.8 TranslateTransform 动画

拖拽归位时使用 `DoubleTransition` 配合 `TranslateTransform`：

```csharp
private static TranslateTransform EnsureTranslateTransform(Control control, bool animate)
{
    if (control.RenderTransform is not TranslateTransform transform)
    {
        transform = new TranslateTransform();
        control.RenderTransform = transform;
    }

    transform.Transitions = animate
        ?
        [
            new DoubleTransition
            {
                Property = TranslateTransform.YProperty,
                Duration = TimeSpan.FromMilliseconds(150),
                Easing = new CubicEaseOut()
            }
        ]
        : null;
    return transform;
}
```

当 `Transitions` 被设置后，修改 `transform.Y` 会自动触发平滑过渡，而不是立即跳到目标值。

---

## 深入：动画系统内部原理

### DispatcherTimer vs CompositionAnimation

Avalonia 提供两种动画方式：

**DispatcherTimer（CodexSwitch 使用的方式）**

```csharp
_animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
_animationTimer.Tick += OnAnimationTick;
_animationTimer.Start();
```

优点：完全控制动画逻辑、可以访问业务数据、适合复杂自定义动画。
缺点：手动管理生命周期、可能影响 UI 线程性能。

**CompositionAnimation（Avalonia 内置）**

```xml
<Animation Duration="0:0:0.3" Easing="CubicEaseOut">
    <KeyFrame Cue="0%"><Setter Property="Opacity" Value="0"/></KeyFrame>
    <KeyFrame Cue="100%"><Setter Property="Opacity" Value="1"/></KeyFrame>
</Animation>
```

优点：声明式易于维护、自动管理生命周期。
缺点：灵活性较低、不适合复杂业务逻辑。

### 动画帧循环

```
渲染帧触发（~60fps）
    -> 更新所有活动动画
    -> 计算当前进度（0.0 - 1.0）
    -> 应用缓动函数
    -> 更新属性值 / 调用 InvalidateVisual
    -> 触发重绘
    -> 等待下一帧
```

### 属性更新通知链

当动画更新属性值时：

```
动画计算新值
    -> 调用 SetValue() 或直接修改属性
    -> 触发 PropertyChanged 事件
    -> AffectsRender / AffectsMeasure 标记脏区域
    -> 在下一帧渲染时更新
```

`CsRollingNumber` 在静态构造函数中注册了 `AffectsRender` 和 `AffectsMeasure`，确保 `ValueProperty` 变化时触发重绘：

```csharp
static CsRollingNumber()
{
    AffectsMeasure<CsRollingNumber>(
        ValueProperty, UseCompactFormatProperty, FontSizeProperty, FontWeightProperty);
    AffectsRender<CsRollingNumber>(
        ValueProperty, UseCompactFormatProperty, FontSizeProperty,
        FontWeightProperty, ForegroundProperty);

    ValueProperty.Changed.AddClassHandler<CsRollingNumber>((number, args) =>
    {
        if (args.NewValue is long value)
            number.OnValueChanged(value);
    });
}
```

---

## 跨引用

- **自定义控件**：动画通常嵌入在自定义控件中，参见 [第 9 章](09-custom-controls.md)
- **自定义渲染**：`Render(DrawingContext)` 是动画的绘制出口，参见 [第 14 章](14-custom-rendering.md)
- **拖拽交互**：拖拽归位使用 TranslateTransform 动画，参见 [第 13 章](13-drag-drop.md)
- **属性系统**：动画依赖 StyledProperty 和变更通知，参见 [第 22 章](22-property-system.md)

---

## 常见陷阱

### 陷阱 1：忘记在 OnDetachedFromVisualTree 中停止动画

```csharp
// 错误：控件被移除后定时器继续运行，导致内存泄漏和异常
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    // 忘记停止 _animationTimer
}

// 正确：总是停止定时器
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    _isAttached = false;
    StopAnimation();
}
```

### 陷阱 2：动画结束后不设置最终值

```csharp
// 错误：停止定时器后 _displayValue 未到达目标值
if (progress >= 1d)
{
    _animationTimer.Stop();
    // _displayValue 停留在中间值
}

// 正确：先设置最终值再停止
if (progress >= 1d)
{
    _displayValue = _targetValue;
    StopAnimation();
}
```

### 陷阱 3：在动画中修改 Width/Height 触发布局风暴

```csharp
// 错误：每帧触发布局
control.Width = animatedWidth;

// 正确：使用 RenderTransform，不影响布局
transform.X = animatedX;
```

### 陷阱 4：未附加到视觉树时启动定时器

`CsRollingNumber` 在 `StartAnimation` 中检查 `_isAttached`，如果控件尚未附加到视觉树则直接设置最终值。这是因为 `DispatcherTimer` 依赖 UI 线程消息循环，控件不可见时动画没有意义。

### 陷阱 5：CsDialog 的 Post 时序

`CsDialog` 先设置 `Opacity = 0`，再 `Post` 设置 `Opacity = 1`。如果在同一帧内设置，transition 不会触发——必须跨帧才能让过渡系统检测到属性变化。

---

## 动手练习

### 练习 1：为按钮添加点击缩放动画

```xml
<Style Selector="ui|CsButton:pressed">
    <Style.Animations>
        <Animation Duration="0:0:0.1">
            <KeyFrame Cue="0%">
                <Setter Property="ScaleTransform.ScaleX" Value="1"/>
                <Setter Property="ScaleTransform.ScaleY" Value="1"/>
            </KeyFrame>
            <KeyFrame Cue="50%">
                <Setter Property="ScaleTransform.ScaleX" Value="0.95"/>
                <Setter Property="ScaleTransform.ScaleY" Value="0.95"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
                <Setter Property="ScaleTransform.ScaleX" Value="1"/>
                <Setter Property="ScaleTransform.ScaleY" Value="1"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

### 练习 2：实现加载旋转指示器

```xml
<Style Selector="ui|CsLoadingSpinner">
    <Style.Animations>
        <Animation Duration="0:0:1"
                   IterationCount="Infinite"
                   Easing="LinearEasing">
            <KeyFrame Cue="0%">
                <Setter Property="RotateTransform.Angle" Value="0"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
                <Setter Property="RotateTransform.Angle" Value="360"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

### 练习 3：实现弹性缓动

尝试替换 `CsRollingNumber` 中的 `EaseOutCubic` 为弹性效果：

```csharp
private static double EaseOutElastic(double t)
{
    if (t == 0 || t == 1) return t;
    return Math.Pow(2, -10 * t) * Math.Sin((t - 0.075) * (2 * Math.PI) / 0.3) + 1;
}
```

观察数字滚动时的弹性回弹效果，理解缓动函数对动画质感的影响。
