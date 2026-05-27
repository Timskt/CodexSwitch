# 10. 动画与过渡效果

> **写给零基础的你**：动画就是让东西"动起来"。按钮被点击时缩放一下、页面切换时淡入淡出、加载时转圈圈——这些都是动画。Avalonia 提供了两种方式：**Transitions（过渡）** 像自动门，你走过去它自动开，走开了自动关；**Animations（动画）** 像电影，你按下播放键它就开始演。

## 10.1 概述

动画是现代 UI 的灵魂。学完本章，你将能够：

- 理解 Transitions（过渡）和 Animations（动画）的区别和适用场景
- 掌握所有内置动画类型（DoubleAnimation、ColorAnimation、PointAnimation）
- 使用关键帧动画实现复杂的时间线效果
- 使用 Storyboard 编排多个动画
- 使用动画触发器（Trigger、DataTrigger、EventTrigger）驱动动画
- 实现页面切换动画（ViewTransition）
- 理解合成层动画和属性动画的性能差异
- 参考 CodexSwitch 的真实动画代码，掌握工业级动画开发

CodexSwitch 中有三种典型动画模式：样式 Transitions（如按钮悬停效果）、DispatcherTimer 帧动画（如 CsRollingNumber 数字滚动）和 XAML 关键帧动画（如缩放效果）。本章将深入讲解每种模式的原理和最佳实践。

## 10.2 核心概念

### 10.2.1 过渡 (Transitions) vs 动画 (Animations)

| 特性 | Transitions | Animations |
|------|------------|------------|
| 触发方式 | 属性值变化自动触发 | 手动启动/停止 |
| 用途 | 状态切换（悬停、选中） | 持续效果（加载、闪烁） |
| 语法 | 声明式（在 Style 中） | 代码或 XAML |
| 性能 | 优化过，自动管理生命周期 | 需手动管理 |
| 灵活性 | 低——只能响应属性变化 | 高——任意逻辑 |
| 缓动函数 | 支持 | 支持 |
| 关键帧 | 不支持 | 支持 |

**选择指南**：
- 状态变化（悬停、聚焦、选中、禁用） -> Transitions
- 持续动画（加载旋转、脉冲、呼吸灯） -> Animations
- 复杂时间线（多步骤、编排） -> 关键帧 Animations
- 自定义逻辑动画（数字滚动、粒子效果） -> DispatcherTimer

### 10.2.2 所有 Transition 类型

> **小白提示**：Transition 就像"自动平滑过渡"。你不需要手动控制每一帧，只需要说"这个属性变化时，用 0.2 秒平滑过渡"。就像自动门——你走过去，它自动慢慢打开，不需要你推。

Avalonia 提供以下 Transition 类型：

```xml
<!-- 数值过渡：透明度、尺寸、位置等 -->
<DoubleTransition Property="Opacity" Duration="0:0:0.2"/>

<!-- 颜色过渡：Background、Foreground、BorderBrush 等 -->
<BrushTransition Property="Background" Duration="0:0:0.15"/>

<!-- 整数过渡 -->
<Int32Transition Property="ZIndex" Duration="0:0:0.1"/>

<!-- 厚度过渡：Margin、Padding 等 -->
<ThicknessTransition Property="Margin" Duration="0:0:0.2"/>

<!-- 圆角过渡 -->
<CornerRadiusTransition Property="CornerRadius" Duration="0:0:0.15"/>
```

### 10.2.3 在样式中使用 Transitions

**BrushTransition**

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

**DoubleTransition**

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

**组合过渡**

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

当多个属性需要同时过渡时，将它们放在同一个 `<Transitions>` 块中。

### 10.2.4 内置动画类型详解

**DoubleAnimation** — 数值动画

```xml
<Animation Duration="0:0:0.5" Easing="CubicEaseOut">
    <KeyFrame Cue="0%">
        <Setter Property="Opacity" Value="0"/>
    </KeyFrame>
    <KeyFrame Cue="100%">
        <Setter Property="Opacity" Value="1"/>
    </KeyFrame>
</Animation>
```

适用于：Opacity、Width、Height、TranslateTransform.X/Y、ScaleTransform.ScaleX/Y、RotateTransform.Angle 等数值属性。

**ColorAnimation** — 颜色动画

```xml
<Animation Duration="0:0:0.3">
    <KeyFrame Cue="0%">
        <Setter Property="Background" Value="#FF0000"/>
    </KeyFrame>
    <KeyFrame Cue="100%">
        <Setter Property="Background" Value="#00FF00"/>
    </KeyFrame>
</Animation>
```

颜色动画在 ARGB 空间逐通道插值。适用于 Background、Foreground、BorderBrush 等 Brush 属性。

**PointAnimation** — 点动画

```xml
<Animation Duration="0:0:1">
    <KeyFrame Cue="0%">
        <Setter Property="StartPoint" Value="0,0"/>
    </KeyFrame>
    <KeyFrame Cue="100%">
        <Setter Property="StartPoint" Value="100,100"/>
    </KeyFrame>
</Animation>
```

适用于 LinearGradientBrush 的 StartPoint/EndPoint 等 Point 属性。

## 10.3 进阶用法

### 10.3.1 关键帧动画详解

关键帧动画允许你在时间线上定义多个"关键帧"，Avalonia 在帧之间自动插值。

**线性关键帧（LinearKeyFrame）**

```xml
<Animation Duration="0:0:2">
    <KeyFrame Cue="0%">
        <Setter Property="Opacity" Value="0"/>
    </KeyFrame>
    <KeyFrame Cue="50%">
        <Setter Property="Opacity" Value="1"/>  <!-- 线性插值到此处 -->
    </KeyFrame>
    <KeyFrame Cue="100%">
        <Setter Property="Opacity" Value="0"/>  <!-- 线性插值到此处 -->
    </KeyFrame>
</Animation>
```

**缓动关键帧（EasingKeyFrame）**

```xml
<Animation Duration="0:0:1">
    <KeyFrame Cue="0%" Value="0">
        <Setter Property="Opacity" Value="0"/>
    </KeyFrame>
    <KeyFrame Cue="100%" Value="1" KeySpline="0.42 0 0.58 1">
        <Setter Property="Opacity" Value="1"/>
    </KeyFrame>
</Animation>
```

`KeySpline` 定义贝塞尔控制点，与 CSS 的 `cubic-bezier()` 相同。

**离散关键帧（DiscreteKeyFrame）**

```xml
<Animation Duration="0:0:1">
    <KeyFrame Cue="0%">
        <Setter Property="Content" Value="Loading"/>
    </KeyFrame>
    <KeyFrame Cue="33%">
        <Setter Property="Content" Value="Loading."/>
    </KeyFrame>
    <KeyFrame Cue="66%">
        <Setter Property="Content" Value="Loading.."/>
    </KeyFrame>
    <KeyFrame Cue="100%">
        <Setter Property="Content" Value="Loading..."/>
    </KeyFrame>
</Animation>
```

离散关键帧不插值——在 Cue 到达时直接跳到目标值。适用于文字内容、可见性等离散属性。

### 10.3.2 动画属性详解

**IterationCount** — 迭代次数

```xml
<!-- 播放 3 次 -->
<Animation Duration="0:0:1" IterationCount="3">

<!-- 无限循环 -->
<Animation Duration="0:0:1" IterationCount="Infinite">
```

**PlaybackDirection** — 播放方向

```xml
<!-- 正向 -> 反向 -> 正向 -> ... -->
<Animation Duration="0:0:1" PlaybackDirection="Alternate">

<!-- 正向 -> 跳回起点 -> 正向 -> ... -->
<Animation Duration="0:0:1" PlaybackDirection="Normal">

<!-- 反向播放 -->
<Animation Duration="0:0:1" PlaybackDirection="Reverse">

<!-- 反向 -> 正向 -> 反向 -> ... -->
<Animation Duration="0:0:1" PlaybackDirection="AlternateReverse">
```

**FillMode** — 填充模式

```xml
<!-- 动画结束后恢复初始值 -->
<Animation Duration="0:0:1" FillMode="None">

<!-- 动画结束后保持最终值 -->
<Animation Duration="0:0:1" FillMode="Forward">

<!-- 动画开始前显示初始值 -->
<Animation Duration="0:0:1" FillMode="Backward">

<!-- 开始前和结束后都保持 -->
<Animation Duration="0:0:1" FillMode="Both">
```

**Delay** — 延迟

```xml
<!-- 延迟 500ms 后开始 -->
<Animation Duration="0:0:1" Delay="0:0:0.5">
```

### 10.3.3 动画触发器

**在 Style.Animations 中触发动画**

```xml
<!-- 指针悬停时播放 -->
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

<!-- 按下时播放 -->
<Style Selector="Button:pressed">
    <Style.Animations>
        <Animation Duration="0:0:0.1">
            <KeyFrame Cue="0%">
                <Setter Property="ScaleTransform.ScaleX" Value="1"/>
            </KeyFrame>
            <KeyFrame Cue="50%">
                <Setter Property="ScaleTransform.ScaleX" Value="0.95"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
                <Setter Property="ScaleTransform.ScaleX" Value="1"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

**加载时自动播放的动画**

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

### 10.3.4 ViewTransition — 页面切换动画

Avalonia 11.1+ 支持 `ViewTransition` 用于页面切换动画：

```xml
<!-- 在 ContentControl 或 TransitioningContentControl 中使用 -->
<TransitioningContentControl Content="{Binding CurrentPage}">
    <TransitioningContentControl.PageTransition>
        <CompositeTransition>
            <!-- 离场动画 -->
            <PageSlide Duration="0:0:0.3"
                       Orientation="Horizontal"
                       SlideOut="-1"
                       SlideIn="1"/>
            <!-- 入场动画 -->
        </CompositeTransition>
    </TransitioningContentControl.PageTransition>
</TransitioningContentControl>
```

**内置 ViewTransition 类型**

| 类型 | 说明 |
|------|------|
| `PageSlide` | 水平/垂直滑动 |
| `CrossFade` | 交叉淡入淡出 |
| `CompositeTransition` | 组合多个过渡 |

**自定义 ViewTransition**

```csharp
public class ScaleFadeTransition : IPageTransition
{
    public async Task Start(Visual from, Visual to, bool forward, CancellationToken cancellationToken)
    {
        if (from is not null)
        {
            // 离场：缩小 + 淡出
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, 1.0),
                            new Setter(ScaleTransform.ScaleYProperty, 1.0),
                            new Setter(Visual.OpacityProperty, 1.0)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, 0.8),
                            new Setter(ScaleTransform.ScaleYProperty, 0.8),
                            new Setter(Visual.OpacityProperty, 0.0)
                        }
                    }
                }
            };
            await animation.RunAsync(from, cancellationToken);
        }

        if (to is not null)
        {
            to.Opacity = 0;
            to.IsVisible = true;
            // 入场：放大 + 淡入
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, 0.8),
                            new Setter(ScaleTransform.ScaleYProperty, 0.8),
                            new Setter(Visual.OpacityProperty, 0.0)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, 1.0),
                            new Setter(ScaleTransform.ScaleYProperty, 1.0),
                            new Setter(Visual.OpacityProperty, 1.0)
                        }
                    }
                }
            };
            await animation.RunAsync(to, cancellationToken);
        }
    }
}
```

## 10.4 组件详解大全

### 10.4.1 缓动函数完整列表

Avalonia 内置的缓动函数：

| 函数 | 效果 | 用途 | CSS 等价 |
|------|------|------|---------|
| `LinearEasing` | 匀速 | 旋转 | `linear` |
| `CubicEaseOut` | 快到慢 | 按钮点击、弹出 | `cubic-bezier(0, 0, 0.58, 1)` |
| `CubicEaseIn` | 慢到快 | 消失 | `cubic-bezier(0.42, 0, 1, 1)` |
| `CubicEaseInOut` | 慢到快到慢 | 页面切换 | `cubic-bezier(0.42, 0, 0.58, 1)` |
| `QuadraticEaseOut` | 快到慢（二次） | 轻量动画 | — |
| `QuarticEaseOut` | 快到慢（四次） | 强调动画 | — |
| `QuinticEaseOut` | 快到慢（五次） | 强调动画 | — |
| `SineEaseOut` | 正弦缓出 | 柔和动画 | — |
| `BounceEaseOut` | 弹跳 | 强调效果 | — |
| `ElasticEaseOut` | 弹性 | 弹性效果 | — |
| `BackEaseOut` | 回弹 | 超调效果 | — |
| `CircleEaseOut` | 圆弧 | 中等缓动 | — |
| `ExponentialEaseOut` | 指数 | 强烈缓动 | — |

**常用缓动函数的数学表达**

```csharp
// 三次方缓出 — CodexSwitch 的 CsRollingNumber 使用
private static double EaseOutCubic(double t)
{
    var inverted = 1d - t;
    return 1d - inverted * inverted * inverted;
}

// 正弦缓入缓出 — CodexSwitch 的 CsActivityArrow 使用
private static double EaseInOutSine(double t)
{
    return -(Math.Cos(Math.PI * t) - 1d) / 2d;
}

// 弹性缓出
private static double EaseOutElastic(double t)
{
    if (t == 0 || t == 1) return t;
    return Math.Pow(2, -10 * t) * Math.Sin((t - 0.075) * (2 * Math.PI) / 0.3) + 1;
}

// 弹跳缓出
private static double EaseOutBounce(double t)
{
    if (t < 1d / 2.75)
        return 7.5625 * t * t;
    if (t < 2d / 2.75)
        return 7.5625 * (t -= 1.5 / 2.75) * t + 0.75;
    if (t < 2.5d / 2.75)
        return 7.5625 * (t -= 2.25 / 2.75) * t + 0.9375;
    return 7.5625 * (t -= 2.625 / 2.75) * t + 0.984375;
}
```

### 10.4.2 Storyboard 编排

在 XAML 中使用 Storyboard 编排多个动画：

```xml
<Window.Styles>
    <Style Selector="Button.my-button">
        <Style.Animations>
            <Animation Duration="0:0:0.6" Delay="0:0:0.1">
                <!-- 同时缩放和位移 -->
                <KeyFrame Cue="0%">
                    <Setter Property="ScaleTransform.ScaleX" Value="0.5"/>
                    <Setter Property="ScaleTransform.ScaleY" Value="0.5"/>
                    <Setter Property="TranslateTransform.Y" Value="20"/>
                    <Setter Property="Opacity" Value="0"/>
                </KeyFrame>
                <KeyFrame Cue="100%">
                    <Setter Property="ScaleTransform.ScaleX" Value="1"/>
                    <Setter Property="ScaleTransform.ScaleY" Value="1"/>
                    <Setter Property="TranslateTransform.Y" Value="0"/>
                    <Setter Property="Opacity" Value="1"/>
                </KeyFrame>
            </Animation>
        </Style.Animations>
    </Style>
</Window.Styles>
```

**在代码中控制动画**

```csharp
// 从资源中获取动画并运行
var animation = this.FindResource("MyAnimation") as Animation;
if (animation is not null)
{
    await animation.RunAsync(myControl, CancellationToken.None);
}
```

### 10.4.3 PhysicsAnimator — 弹簧动画

Avalonia 的 `PhysicsAnimator` 提供基于物理的弹簧动画：

```csharp
// 使用弹簧动画移动元素
var spring = new Spring(stiffness: 300, damping: 20, mass: 1);
var animator = new SpringAnimator(spring);

animator.From = 0;
animator.To = 200;
animator.TargetProperty = TranslateTransform.YProperty;

await animator.RunAsync(myControl, CancellationToken.None);
```

**Spring 参数说明**

| 参数 | 说明 | 增大效果 |
|------|------|---------|
| `stiffness` | 弹簧刚度 | 振动更快 |
| `damping` | 阻尼 | 振动更少 |
| `mass` | 质量 | 振动更慢 |

## 10.5 CodexSwitch 实战

### 10.5.1 CsRollingNumber — 数字滚动动画

这是 CodexSwitch 中最复杂的动画控件，当 token 用量数值增大时，旧数字淡出上移，新数字淡入下移。

**动画触发**

```csharp
private void OnValueChanged(long newValue)
{
    if (!_hasValue)
    {
        SetImmediateValue(newValue);  // 首次设置，不动画
        return;
    }

    if (newValue <= _displayValue)
    {
        SetImmediateValue(newValue);  // 值变小，不动画
        return;
    }

    // 记录起始值，启动动画
    _startValue = _displayValue;
    _targetValue = newValue;
    _animationStartedAt = DateTimeOffset.UtcNow;
    _animationDuration = ResolveDuration(newValue - _startValue);
    StartAnimation();
}
```

**动态持续时间**

```csharp
private static TimeSpan ResolveDuration(double delta)
{
    var factor = Math.Clamp(Math.Log10(delta + 1d) / 4d, 0d, 1d);
    var duration = MinimumAnimationDuration.TotalMilliseconds +
        (MaximumAnimationDuration.TotalMilliseconds - MinimumAnimationDuration.TotalMilliseconds) * factor;
    return TimeSpan.FromMilliseconds(duration);
}
```

差值为 10 时约 320ms，差值为 10000 时约 820ms，使用对数缩放。

**渲染动画效果**

```csharp
public override void Render(DrawingContext context)
{
    if (_animationTimer is null)
    {
        // 静态状态：直接绘制
        layout.Draw(context, new Point(0, y));
        return;
    }

    var progress = GetAnimationProgress();
    var eased = EaseOutCubic(progress);
    var travel = Math.Min(12d, Math.Max(5d, FontSize * 0.55d));

    // 裁剪区域防止溢出
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

### 10.5.2 CsActivityArrow — 脉冲动画

双箭头脉冲效果，活跃时颜色在前景色和活跃色之间渐变：

```csharp
private void DrawAnimatedArrow(DrawingContext context, double phase, double maxOpacity)
{
    var progress = GetProgress(phase);       // 相位偏移产生跟随效果
    var eased = EaseInOutSine(progress);
    var travel = Math.Max(4d, FontSize * 0.42d);
    var offset = direction * (-travel / 2d + eased * travel);
    var pulse = Math.Sin(progress * Math.PI); // 0->1->0 脉冲

    var opacity = maxOpacity * (0.12d + 0.88d * pulse);
    var brush = CreatePulseBrush(0.22d + 0.78d * pulse, opacity);

    DrawArrow(context, offset, brush, opacity);
}
```

两个箭头使用不同的 `phase`（0 和 0.55），产生一前一后的跟随效果。

### 10.5.3 CsDialog — 极简淡入

```csharp
// 方法 1：Dispatcher.UIThread.Post 实现跨帧淡入
public CsDialog()
{
    Opacity = 0;
}

protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    if (change.Property != IsVisibleProperty ||
        change.NewValue is not bool isVisible || !isVisible) return;

    Opacity = 0;
    // Post 到 Render 优先级——下一帧设置 Opacity=1
    // DoubleTransition 检测到 0->1 的变化，触发过渡动画
    Dispatcher.UIThread.Post(() => Opacity = 1, DispatcherPriority.Render);
}
```

### 10.5.4 拖拽归位动画

CodexSwitch 的 Provider 列表拖拽使用 `DoubleTransition` 实现平滑归位：

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

被拖拽的元素不使用 Transition（`animate: false`），因为需要立即跟随指针。其他行的让路动画使用 Transition（`animate: true`），因为它们应该平滑地移动到新位置。

## 10.6 举一反三

### 10.6.1 动画与 CSS 动画的对比

| CSS | Avalonia |
|-----|----------|
| `transition: opacity 0.2s` | `<DoubleTransition Property="Opacity" Duration="0:0:0.2"/>` |
| `@keyframes` | `<Animation>` + `<KeyFrame>` |
| `animation-iteration-count: infinite` | `IterationCount="Infinite"` |
| `animation-direction: alternate` | `PlaybackDirection="Alternate"` |
| `animation-fill-mode: forwards` | `FillMode="Forward"` |
| `cubic-bezier()` | `Easing="CubicEaseOut"` 或 `KeySpline` |
| `animation-delay` | `Delay="0:0:0.5"` |

### 10.6.2 与 WPF 动画的差异

| WPF | Avalonia |
|-----|----------|
| `<Storyboard>` | `<Animation>` |
| `<DoubleAnimation>` | `<KeyFrame>` with `DoubleTransition` |
| `BeginStoryboard` | `Style.Animations` |
| `EventTrigger` | PseudoClass 触发 |
| `AnimationClock` | `DispatcherTimer` |

## 10.7 最佳实践与设计模式

### 10.7.1 动画时长建议

| 类型 | 建议时长 | 示例 |
|------|---------|------|
| 微交互（悬停、按下） | 100-200ms | 按钮悬停、颜色变化 |
| 状态切换 | 200-300ms | 对话框打开、面板展开 |
| 页面过渡 | 300-500ms | 页面切换、视图转换 |
| 强调效果 | 500-1000ms | 脉冲、呼吸灯 |
| 持续动画 | 1000ms+ | 加载旋转、进度指示 |

### 10.7.2 使用 RenderTransform 而非 Margin

```csharp
// 好：不影响布局
transform.Y = animatedOffset;

// 差：每帧触发布局计算
control.Margin = new Thickness(0, animatedOffset, 0, 0);
```

### 10.7.3 合成层动画 vs 属性动画

Avalonia 的渲染管线支持两种动画方式：

**属性动画（Property Animation）**
- 修改 UI 元素的属性（如 Opacity、RenderTransform）
- 每帧调用 InvalidateVisual
- 适合简单动画

**合成层动画（Composition Animation）**
- 直接在合成层操作，跳过布局
- 更高性能
- 适合大量元素的动画

```csharp
// 使用 CompositionTarget.Rendering 事件实现高性能动画
CompositionTarget.Rendering += (_, _) =>
{
    // 在合成层直接更新变换
    _element.RenderTransform = new TranslateTransform(_x, _y);
};
```

## Deep Dive：动画系统内部原理

### DispatcherTimer vs CompositionAnimation

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

```
动画计算新值
    -> 调用 SetValue() 或直接修改属性
    -> 触发 PropertyChanged 事件
    -> AffectsRender / AffectsMeasure 标记脏区域
    -> 在下一帧渲染时更新
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

## Cross References

- **自定义控件**：动画通常嵌入在自定义控件中，参见 [第 9 章](09-custom-controls.md)
- **自定义渲染**：`Render(DrawingContext)` 是动画的绘制出口，参见 [第 14 章](14-custom-rendering.md)
- **拖拽交互**：拖拽归位使用 TranslateTransform 动画，参见 [第 13 章](13-drag-drop.md)
- **属性系统**：动画依赖 StyledProperty 和变更通知，参见 [第 22 章](22-property-system.md)
- **样式系统**：Transitions 在 Style 中定义，参见 [第 7 章](07-styling-theming.md)

## Common Pitfalls

### 陷阱 1：忘记在 OnDetachedFromVisualTree 中停止动画

```csharp
// 错误：控件被移除后定时器继续运行
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    // 忘记停止 _animationTimer —— 内存泄漏
}

// 正确：
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    _isAttached = false;
    StopAnimation();
}
```

### 陷阱 2：动画结束后不设置最终值

```csharp
// 错误：_displayValue 停留在中间值
if (progress >= 1d)
{
    _animationTimer.Stop();
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

// 正确：使用 RenderTransform
transform.X = animatedX;
```

### 陷阱 4：未附加到视觉树时启动定时器

`CsRollingNumber` 在 `StartAnimation` 中检查 `_isAttached`，如果控件尚未附加到视觉树则直接设置最终值。`DispatcherTimer` 依赖 UI 线程消息循环，控件不可见时动画没有意义。

### 陷阱 5：CsDialog 的 Post 时序

`CsDialog` 先设置 `Opacity = 0`，再 `Post` 设置 `Opacity = 1`。如果在同一帧内设置，transition 不会触发——必须跨帧才能让过渡系统检测到属性变化。

### 陷阱 6：Transitions 在清理时被意外触发

```csharp
// 错误：先重置 Y，再清除 Transitions
transform.X = 0;
transform.Y = 0;  // 触发 150ms 过渡！
transform.Transitions = null;

// 正确：先清除 Transitions，再重置
transform.Transitions = null;
transform.X = 0;
transform.Y = 0;
```

### 陷阱 7：动画中使用 absolute 值而非相对值

```xml
<!-- 错误：绝对值在不同场景下不通用 -->
<KeyFrame Cue="100%">
    <Setter Property="TranslateTransform.Y" Value="-50"/>
</KeyFrame>

<!-- 正确：考虑使用相对值或绑定 -->
```

### 陷阱 8：缓动函数选错导致动画生硬

```xml
<!-- 不适合：线性缓动用于状态变化 -->
<DoubleTransition Property="Opacity" Duration="0:0:0.2" Easing="LinearEasing"/>

<!-- 更好：缓出用于状态变化（自然减速） -->
<DoubleTransition Property="Opacity" Duration="0:0:0.2" Easing="CubicEaseOut"/>
```

### 陷阱 9：Iterations 无限循环但未停止

```xml
<!-- 问题：加载完成后旋转动画仍在继续 -->
<Animation IterationCount="Infinite" ...>
```

解决方案：使用样式类控制动画的启用/禁用，或在代码中停止动画。

### 陷阱 10：TranslateTransform 的 Transitions 与 RenderTransform 冲突

一个控件只能有一个 `RenderTransform`。如果同时需要缩放和位移，使用 `TransformGroup`：

```xml
<Border.RenderTransform>
    <TransformGroup>
        <ScaleTransform/>
        <TranslateTransform/>
    </TransformGroup>
</Border.RenderTransform>
```

## Try It Yourself

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

替换 `CsRollingNumber` 中的 `EaseOutCubic` 为弹性效果：

```csharp
private static double EaseOutElastic(double t)
{
    if (t == 0 || t == 1) return t;
    return Math.Pow(2, -10 * t) * Math.Sin((t - 0.075) * (2 * Math.PI) / 0.3) + 1;
}
```

观察数字滚动时的弹性回弹效果。

### 练习 4：实现渐入渐出的呼吸灯效果

创建一个 `CsBreathingDot` 控件，颜色在两个颜色之间缓动渐变：

```xml
<Style Selector="ui|CsBreathingDot">
    <Style.Animations>
        <Animation Duration="0:0:2"
                   IterationCount="Infinite"
                   PlaybackDirection="Alternate"
                   Easing="SineEaseInOut">
            <KeyFrame Cue="0%">
                <Setter Property="Opacity" Value="0.3"/>
            </KeyFrame>
            <KeyFrame Cue="100%">
                <Setter Property="Opacity" Value="1.0"/>
            </KeyFrame>
        </Animation>
    </Style.Animations>
</Style>
```

### 练习 5：实现交错动画

为列表中的每一项添加入场动画，但每项延迟 100ms：

```csharp
int delay = 0;
foreach (var item in items)
{
    var control = CreateItemControl(item);
    control.Opacity = 0;
    control.RenderTransform = new TranslateTransform(0, 20);

    Dispatcher.UIThread.Post(async () =>
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = {
                    new Setter(Visual.OpacityProperty, 0d),
                    new Setter(TranslateTransform.YProperty, 20d)
                }},
                new KeyFrame { Cue = new Cue(1), Setters = {
                    new Setter(Visual.OpacityProperty, 1d),
                    new Setter(TranslateTransform.YProperty, 0d)
                }}
            }
        };
        await animation.RunAsync(control, CancellationToken.None);
    }, DispatcherPriority.Loaded);

    delay += 100;
}
```

### 练习 6：实现 CSS-like transition 组合

为一个卡片控件同时添加 4 个属性的过渡：

```xml
<Style Selector="Border.card:pointerover">
    <Setter Property="Background" Value="{StaticResource CsCardHoverBrush}"/>
    <Setter Property="BorderBrush" Value="{StaticResource CsHoverBorderBrush}"/>
    <Setter Property="BoxShadow" Value="0 4 12 0 #40000000"/>
    <Setter Property="RenderTransform">
        <Setter.Value>
            <TranslateTransform Y="-2"/>
        </Setter.Value>
    </Setter>
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.15"/>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.15"/>
            <BoxShadowTransition Property="BoxShadow" Duration="0:0:0.15"/>
            <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>
```

### 练习 7：实现自定义 ViewTransition

创建一个从点击位置扩散的圆形揭示动画：

```csharp
public class CircularRevealTransition : IPageTransition
{
    public Point Origin { get; set; }

    public async Task Start(Visual from, Visual to, bool forward, CancellationToken token)
    {
        // 使用 Clip 实现圆形揭示效果
        // from 被圆形裁剪逐渐缩小
        // to 从圆形裁剪逐渐放大
    }
}
```

### 练习 8：对比不同缓动函数的视觉差异

创建一个测试页面，对同一动画分别使用 Linear、CubicEaseOut、ElasticEaseOut、BounceEaseOut 缓动，同时播放，观察差异。
