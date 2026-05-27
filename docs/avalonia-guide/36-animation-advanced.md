# 35. 动画系统进阶

> **写给零基础的你**：基础动画像"自动门"——你走过去它自动开；进阶动画像"电影特效"——有物理效果、粒子效果、路径跟随。本章将带你从"让东西动起来"升级到"让东西动得自然、动得漂亮"。

## 35.1 概述

### 基础动画 vs 进阶动画

| 特性 | 基础动画（第10章） | 进阶动画（本章） |
|------|-------------------|-----------------|
| 运动曲线 | 缓动函数（EaseIn/Out） | 物理模拟（弹簧、惯性、重力） |
| 运动路径 | 直线 | 任意路径（贝塞尔曲线、PathGeometry） |
| 视觉效果 | 透明度、位移、缩放 | 粒子、模糊、3D旋转、毛玻璃 |
| 控制方式 | 声明式（XAML） | 命令式（C#）+ 声明式 |
| 性能要求 | 低 | 中到高，需要优化 |

本章覆盖：Composition 动画、物理动画（弹簧/惯性/重力）、路径动画、粒子效果、Lottie 集成、模糊/阴影动画、颜色动画进阶、变换动画进阶、状态动画、页面过渡、列表动画、性能优化与调试。

## 35.2 Composition 动画（底层动画）

### 35.2.1 CompositionTarget.Rendering 事件

`CompositionTarget.Rendering` 是 Avalonia 的底层渲染回调，每一帧触发，是实现自定义动画的基石。

```csharp
public class ManualAnimationControl : Control
{
    private double _x, _y;
    private readonly Stopwatch _stopwatch = new();

    public void StartAnimation()
    {
        _stopwatch.Start();
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        double elapsed = _stopwatch.Elapsed.TotalSeconds;
        // 基于时间的动画，确保不同帧率下速度一致
        _x = 150 + 100 * Math.Sin(elapsed * 2);
        _y = 150 + 100 * Math.Cos(elapsed * 3);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawEllipse(Brushes.CornflowerBlue, null, new Point(_x, _y), 20, 20);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering; // 防止内存泄漏
        base.OnDetachedFromVisualTree(e);
    }
}
```

> **关键原则**：始终基于时间（`Stopwatch`）计算位置，而非基于帧计数。不同设备帧率不同，基于帧数会导致运动速度不一致。

### 35.2.2 帧率控制与手动动画循环

```csharp
// 使用 DispatcherTimer 控制帧率
public class TimerAnimationControl : Control
{
    private readonly DispatcherTimer _timer;
    private double _angle;

    public TimerAnimationControl()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
        _timer.Tick += (_, _) => { _angle += 3; InvalidateVisual(); };
    }
    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        using (context.PushTransform(
            Matrix.CreateTranslation(Bounds.Width / 2, Bounds.Height / 2) *
            Matrix.CreateRotation(_angle * Math.PI / 180)))
        {
            context.DrawRectangle(Brushes.OrangeRed, null, new Rect(-30, -30, 60, 60));
        }
    }
}
```

### 35.2.3 性能对比

| 指标 | Transition/Animation | CompositionTarget | DispatcherTimer |
|------|---------------------|-------------------|-----------------|
| CPU 使用率 | 低（框架优化） | 中 | 中 |
| 灵活性 | 低（预定义属性） | 高（任意绘制） | 高 |
| 线程 | 渲染线程 | 渲染线程 | UI 线程 |
| 适用场景 | 属性动画 | 粒子/物理 | 逻辑驱动 |

## 35.3 物理动画

### 35.3.1 弹簧动画（Spring Animation）

弹簧动画通过物理参数模拟弹簧运动，比缓动函数更自然。

| 参数 | 含义 | 典型值 |
|------|------|--------|
| Stiffness（刚度） | 弹簧"硬度"，越大越快到达 | 100-500 |
| Damping（阻尼） | 阻力，越大振荡越少 | 10-30 |
| Mass（质量） | 惯性大小 | 1-5 |

```csharp
public class SpringAnimation
{
    public double Stiffness { get; set; } = 200;
    public double Damping { get; set; } = 15;
    public double Mass { get; set; } = 1.0;
    public double InitialVelocity { get; set; } = 0;
    public double Tolerance { get; set; } = 0.01;

    public double Evaluate(double from, double to, double elapsedSeconds)
    {
        double distance = to - from;
        if (Math.Abs(distance) < Tolerance) return to;

        double gamma = Damping / (2.0 * Mass);   // 阻尼系数
        double omega0 = Math.Sqrt(Stiffness / Mass); // 自然频率

        if (gamma < omega0) // 欠阻尼（有振荡）—— iOS 风格
        {
            double omegaD = Math.Sqrt(omega0 * omega0 - gamma * gamma);
            double exp = Math.Exp(-gamma * elapsedSeconds);
            double A = distance;
            double B = (gamma * distance + InitialVelocity) / omegaD;
            return to - exp * (A * Math.Cos(omegaD * elapsedSeconds)
                             + B * Math.Sin(omegaD * elapsedSeconds));
        }
        else // 临界/过阻尼（无振荡）
        {
            double exp = Math.Exp(-gamma * elapsedSeconds);
            return to - exp * (distance +
                (gamma * distance + InitialVelocity) * elapsedSeconds);
        }
    }

    public bool IsComplete(double current, double target, double velocity) =>
        Math.Abs(current - target) < Tolerance && Math.Abs(velocity) < Tolerance;
}
```

#### iOS 风格弹簧预设

```csharp
public static class IOSSpringPresets
{
    public static SpringAnimation Gentle => new() { Stiffness = 120, Damping = 14, Mass = 1 };
    public static SpringAnimation Default => new() { Stiffness = 250, Damping = 22, Mass = 1 };
    public static SpringAnimation Snappy => new() { Stiffness = 400, Damping = 30, Mass = 1 };
    public static SpringAnimation Bouncy => new() { Stiffness = 180, Damping = 10, Mass = 1 };
}
```

### 35.3.2 惯性动画（Inertia）

模拟拖拽释放后的"滑行"效果。

```csharp
public class InertiaAnimation
{
    public double InitialVelocity { get; set; }
    public double Deceleration { get; set; } = 0.95;
    public double MinVelocity { get; set; } = 0.5;

    // 指数衰减模型：v(t) = v₀ * deceleration^(t*60)
    public double Evaluate(double initialPos, double elapsedSeconds)
    {
        double velocity = InitialVelocity * Math.Pow(Deceleration, elapsedSeconds * 60);
        if (Math.Abs(velocity) < MinVelocity) return initialPos;

        double lnD = Math.Log(Deceleration);
        double displacement = InitialVelocity *
            (Math.Pow(Deceleration, elapsedSeconds * 60) - 1) / (60 * lnD);
        return initialPos + displacement;
    }
}
```

#### 滚动惯性使用示例

```csharp
// 在 PointerReleased 中启动惯性
protected override void OnPointerReleased(PointerReleasedEventArgs e)
{
    if (Math.Abs(_velocityY) > 50) // 速度足够大才启动
    {
        _inertia.InitialVelocity = _velocityY;
        _releaseTime = DateTime.UtcNow;
        CompositionTarget.Rendering += OnInertiaRendering;
    }
}
```

### 35.3.3 重力动画

```csharp
public class GravityAnimation
{
    public double Gravity { get; set; } = 980;       // px/s²
    public double Restitution { get; set; } = 0.7;   // 反弹系数
    public double FloorY { get; set; } = 400;
    public double CurrentY { get; private set; }
    public double VelocityY { get; private set; }

    public void Drop(double startY) { CurrentY = startY; VelocityY = 0; }

    public void Update(double deltaTime)
    {
        VelocityY += Gravity * deltaTime;
        CurrentY += VelocityY * deltaTime;

        if (CurrentY >= FloorY)
        {
            CurrentY = FloorY;
            VelocityY = -VelocityY * Restitution;
            if (Math.Abs(VelocityY) < 5) VelocityY = 0;
        }
    }
}
```

## 35.4 路径动画

### 35.4.1 沿 PathGeometry 移动

```csharp
public class PathFollowingControl : Control
{
    public static readonly StyledProperty<PathGeometry?> PathProperty =
        AvaloniaProperty.Register<PathFollowingControl, PathGeometry?>(nameof(Path));
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<PathFollowingControl, double>(nameof(Progress));

    public PathGeometry? Path { get => GetValue(PathProperty); set => SetValue(PathProperty, value); }
    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    static PathFollowingControl()
    {
        ProgressProperty.Changed.AddClassHandler<PathFollowingControl>(
            (x, _) => x.InvalidateVisual());
    }

    public override void Render(DrawingContext context)
    {
        if (Path == null) return;
        context.DrawGeometry(null, new Pen(Brushes.LightGray, 1, dashStyle: DashStyle.Dash), Path);

        var point = GetPointAtProgress(Path, Progress);
        context.DrawEllipse(Brushes.CornflowerBlue, null, point, 12, 12);
    }

    private Point GetPointAtProgress(PathGeometry geometry, double progress)
    {
        // 离散化路径，按进度插值
        var flattened = geometry.GetFlattenedPathGeometry();
        var points = new List<Point>();
        foreach (var figure in flattened.Figures)
        {
            var start = figure.StartPoint;
            foreach (var segment in figure.Segments)
            {
                if (segment is LineSegment line) { points.Add(start); start = line.Point; }
            }
            points.Add(start);
        }
        if (points.Count < 2) return new Point();

        double totalLength = 0;
        var lengths = new List<double>();
        for (int i = 1; i < points.Count; i++)
        {
            double len = (points[i] - points[i - 1]).Length;
            lengths.Add(len); totalLength += len;
        }

        double target = progress * totalLength, acc = 0;
        for (int i = 0; i < lengths.Count; i++)
        {
            if (acc + lengths[i] >= target)
            {
                double t = (target - acc) / lengths[i];
                return new Point(
                    points[i].X + (points[i + 1].X - points[i].X) * t,
                    points[i].Y + (points[i + 1].Y - points[i].Y) * t);
            }
            acc += lengths[i];
        }
        return points[^1];
    }
}
```

#### XAML 中使用

```xml
<local:PathFollowingControl x:Name="PathFollower">
    <local:PathFollowingControl.Path>
        <PathGeometry Figures="M 50,250 C 150,50 350,50 450,250"/>
    </local:PathFollowingControl.Path>
    <local:PathFollowingControl.Styles>
        <Style Selector="local|PathFollowingControl">
            <Style.Animations>
                <Animation Duration="0:0:3" IterationCount="Infinite" PlaybackDirection="Alternate">
                    <KeyFrame Cue="0%"><Setter Property="Progress" Value="0"/></KeyFrame>
                    <KeyFrame Cue="100%"><Setter Property="Progress" Value="1"/></KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </local:PathFollowingControl.Styles>
</local:PathFollowingControl>
```

### 35.4.2 沿路径旋转

```csharp
// 在 Render 中计算运动方向
var point = GetPointAtProgress(Path, Progress);
var nextPoint = GetPointAtProgress(Path, Math.Min(1, Progress + 0.01));
double angle = Math.Atan2(nextPoint.Y - point.Y, nextPoint.X - point.X);

using (context.PushTransform(
    Matrix.CreateTranslation(point.X, point.Y) *
    Matrix.CreateRotation(angle)))
{
    // 绘制朝向运动方向的箭头
    context.DrawGeometry(Brushes.OrangeRed, null, arrowGeometry);
}
```

### 35.4.3 实战：加载动画沿圆弧运动

```csharp
public class OrbitLoadingControl : Control
{
    private readonly Stopwatch _stopwatch = new();

    public override void Render(DrawingContext context)
    {
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        double elapsed = _stopwatch.Elapsed.TotalSeconds;

        for (int i = 0; i < 3; i++)
        {
            double a = elapsed * 2 + i * (2 * Math.PI / 3);
            double x = center.X + 40 * Math.Cos(a);
            double y = center.Y + 40 * Math.Sin(a);
            double size = 6 + 3 * Math.Sin(a);
            context.DrawEllipse(new SolidColorBrush(Color.FromRgb(
                (byte)(100 + 55 * i), (byte)(150 - 30 * i), 220)),
                null, new Point(x, y), size, size);
        }
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnAttachedToVisualTree(e); _stopwatch.Start(); }
}
```

## 35.5 粒子效果

### 35.5.1 粒子系统基础

粒子系统由三部分组成：**发射器**（生成粒子）、**粒子**（运动个体）、**更新器**（每帧更新状态）。

```csharp
public class Particle
{
    public Point Position { get; set; }
    public Vector Velocity { get; set; }
    public double Size { get; set; }
    public double LifeTime { get; set; }
    public double Age { get; set; }
    public Color Color { get; set; }
    public double Opacity { get; set; } = 1.0;
    public bool IsAlive => Age < LifeTime;
    public double LifeRatio => LifeTime > 0 ? Age / LifeTime : 1;
}

public class ParticleEmitter
{
    public Point Position { get; set; }
    public double EmissionRate { get; set; } = 50;     // 每秒粒子数
    public double MinSpeed { get; set; } = 50;
    public double MaxSpeed { get; set; } = 150;
    public double MinLifeTime { get; set; } = 1.0;
    public double MaxLifeTime { get; set; } = 3.0;
    public double SpreadAngle { get; set; } = 360;
    public double BaseAngle { get; set; } = -90;        // 向上
    public Color[] Colors { get; set; } = { Colors.White, Colors.LightBlue };

    private readonly Random _random = new();
    private double _accumulated;

    public List<Particle> Emit(double deltaTime)
    {
        var list = new List<Particle>();
        _accumulated += deltaTime;
        double interval = 1.0 / EmissionRate;
        while (_accumulated >= interval)
        {
            _accumulated -= interval;
            double angle = (BaseAngle + (_random.NextDouble() - 0.5) * SpreadAngle) * Math.PI / 180;
            double speed = MinSpeed + _random.NextDouble() * (MaxSpeed - MinSpeed);
            list.Add(new Particle
            {
                Position = Position,
                Velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed),
                Size = 2 + _random.NextDouble() * 4,
                LifeTime = MinLifeTime + _random.NextDouble() * (MaxLifeTime - MinLifeTime),
                Color = Colors[_random.Next(Colors.Length)]
            });
        }
        return list;
    }
}
```

### 35.5.2 粒子系统控件

```csharp
public class ParticleSystemControl : Control
{
    private readonly ParticleEmitter _emitter;
    private readonly List<Particle> _particles = new();
    private DateTime _lastFrame;
    public int MaxParticles { get; set; } = 500;

    public ParticleSystemControl()
    {
        _emitter = new ParticleEmitter
        {
            EmissionRate = 60, SpreadAngle = 60, BaseAngle = -90,
            Colors = new[] { Color.FromRgb(255, 200, 50), Color.FromRgb(255, 150, 30) }
        };
    }

    public void Start()
    {
        _emitter.Position = new Point(Bounds.Width / 2, Bounds.Height * 0.8);
        _lastFrame = DateTime.UtcNow;
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        double dt = (now - _lastFrame).TotalSeconds;
        _lastFrame = now;

        if (_particles.Count < MaxParticles) _particles.AddRange(_emitter.Emit(dt));

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Age += dt;
            if (!p.IsAlive) { _particles.RemoveAt(i); continue; }
            p.Position = new Point(p.Position.X + p.Velocity.X * dt,
                                   p.Position.Y + p.Velocity.Y * dt);
            p.Velocity = new Vector(p.Velocity.X * 0.995, p.Velocity.Y + 200 * dt); // 阻力+重力
            p.Opacity = 1.0 - p.LifeRatio * p.LifeRatio;
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        foreach (var p in _particles)
        {
            double size = p.Size * (1 - p.LifeRatio * 0.5);
            var color = Color.FromArgb((byte)(255 * p.Opacity), p.Color.R, p.Color.G, p.Color.B);
            context.DrawEllipse(new SolidColorBrush(color), null, p.Position, size, size);
        }
    }
}
```

### 35.5.3 雪花、烟花、星空效果

**雪花**：向下发射 + 水平正弦摆动 `p.Position.X + Math.Sin(elapsed * 2 + p.Rotation) * 0.5`。

**烟花**：先发射上升"火箭"（`Velocity.Y = -400`），速度减到接近零时调用 `Explode()` 生成 80+ 粒子向四周扩散。

**星空**：200 个静止星星 + 正弦闪烁 `alpha = brightness * (0.5 + 0.5 * sin(t * speed + phase))`。

### 35.5.4 粒子池和性能优化

```csharp
public class ParticlePool
{
    private readonly Stack<Particle> _pool = new();
    public Particle Rent() => _pool.Count > 0 ? _pool.Pop() : new Particle();
    public void Return(Particle p) { if (_pool.Count < 1000) _pool.Push(p); }
}
```

| 策略 | 做法 | 效果 |
|------|------|------|
| 对象池 | 复用 Particle 对象 | 减少 GC 90%+ |
| 粒子上限 | MaxParticles | 防止帧率崩溃 |
| 批量绘制 | 合并同色粒子 | 减少 GPU 状态切换 |
| 降级策略 | 后台降低粒子数 | 节省电池 |

## 35.6 Lottie 动画集成

### 35.6.1 Lottie 简介

Lottie 是 Airbnb 开源的矢量动画格式：设计师在 After Effects 中制作，通过 Bodymovin 导出 JSON，开发者直接播放。优势：矢量不失真、文件小（10-100KB）、可编程控制、跨平台。

### 35.6.2 Avalonia 中使用 Lottie

```xml
<ItemGroup>
    <!-- 基于 SkiaSharp 的方案 -->
    <PackageReference Include="SkiaSharp" Version="2.88.*" />
</ItemGroup>
```

```csharp
using SkiaSharp.Skottie;

public class LottieAnimationControl : Control
{
    public static readonlyStyledProperty<string?> SourceProperty =
        AvaloniaProperty.Register<LottieAnimationControl, string?>(nameof(Source));
    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<LottieAnimationControl, double>(nameof(Speed), 1.0);
    public static readonly StyledProperty<bool> LoopProperty =
        AvaloniaProperty.Register<LottieAnimationControl, bool>(nameof(Loop), true);

    private Animation? _animation;
    private readonly Stopwatch _stopwatch = new();
    private bool _isPlaying;

    public string? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public double Speed { get => GetValue(SpeedProperty); set => SetValue(SpeedProperty, value); }
    public bool Loop { get => GetValue(LoopProperty); set => SetValue(LoopProperty, value); }

    static LottieAnimationControl()
    {
        SourceProperty.Changed.AddClassHandler<LottieAnimationControl>(
            (x, e) => x.LoadAnimation(e.NewValue as string));
    }

    private void LoadAnimation(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        if (Animation.TryCreate(stream, out var anim)) { _animation = anim; Play(); }
    }

    public void Play() { if (_animation == null || _isPlaying) return; _isPlaying = true; _stopwatch.Start(); CompositionTarget.Rendering += OnRendering; }
    public void Pause() { _isPlaying = false; _stopwatch.Stop(); CompositionTarget.Rendering -= OnRendering; }
    public void Stop() { Pause(); _stopwatch.Reset(); InvalidateVisual(); }
    public void SeekTo(double progress) { _animation?.Seek(progress); InvalidateVisual(); }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (_animation == null) return;
        double elapsed = _stopwatch.Elapsed.TotalSeconds * Speed;
        double frame = (elapsed * _animation.Fps) % _animation.TotalFrames;
        if (!Loop && elapsed * _animation.Fps >= _animation.TotalFrames) { Stop(); return; }
        _animation.SeekFrame(frame);
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { Stop(); _animation?.Dispose(); base.OnDetachedFromVisualTree(e); }
}
```

### 35.6.3 交互式 Lottie（点击触发）

```xml
<local:LottieAnimationControl x:Name="LikeButton"
    Source="Assets/like-animation.json" AutoPlay="False" Loop="False"
    Width="48" Height="48" PointerPressed="OnLikeClicked"/>
```

```csharp
private void OnLikeClicked(object? sender, PointerPressedEventArgs e)
{ LikeButton.Stop(); LikeButton.Play(); }
```

### 35.6.4 Lottie 文件优化

| 手段 | 说明 | 节省 |
|------|------|------|
| 合并图层 | 减少 AE 图层数 | 20-40% |
| 简化路径 | 减少贝塞尔控制点 | 10-30% |
| 压缩 JSON | gzip/brotli | 70-80% |

## 35.7 模糊和阴影动画

### 35.7.1 BlurEffect 动画

```csharp
public static void AnimateBlur(Border target, double fromRadius, double toRadius, TimeSpan duration)
{
    var blur = target.Effect as BlurEffect ?? new BlurEffect();
    target.Effect = blur;
    var sw = Stopwatch.StartNew();

    CompositionTarget.Rendering += OnFrame;
    void OnFrame(object? sender, EventArgs e)
    {
        double t = Math.Min(1.0, sw.Elapsed.TotalSeconds / duration.TotalSeconds);
        blur.Radius = fromRadius + (toRadius - fromRadius) * (1 - Math.Pow(1 - t, 3));
        if (t >= 1.0) CompositionTarget.Rendering -= OnFrame;
    }
}
```

### 35.7.2 DropShadowEffect 动画

```csharp
public static void AnimateShadow(Border target, double targetBlur, double targetOffsetY, TimeSpan duration)
{
    var shadow = target.Effect as DropShadowEffect ?? new DropShadowEffect
    { Color = Colors.Black, Opacity = 0.3, BlurRadius = 5, OffsetY = 2 };
    target.Effect = shadow;
    double startBlur = shadow.BlurRadius, startOffY = shadow.OffsetY;
    var sw = Stopwatch.StartNew();

    CompositionTarget.Rendering += OnFrame;
    void OnFrame(object? sender, EventArgs e)
    {
        double t = Math.Min(1.0, sw.Elapsed.TotalSeconds / duration.TotalSeconds);
        double e2 = 1 - (1 - t) * (1 - t); // EaseOutQuad
        shadow.BlurRadius = startBlur + (targetBlur - startBlur) * e2;
        shadow.OffsetY = startOffY + (targetOffsetY - startOffY) * e2;
        if (t >= 1.0) CompositionTarget.Rendering -= OnFrame;
    }
}
```

### 35.7.3 实战：对话框背景模糊

```csharp
// 对话框弹出时，背景逐渐模糊
public void ShowDialogWithBlur(Control dialogContent, TimeSpan duration)
{
    var overlay = new Border
    {
        Background = new SolidColorBrush(Colors.Black, 0.3),
        Child = dialogContent, Opacity = 0
    };
    Children.Add(overlay);

    var sw = Stopwatch.StartNew();
    CompositionTarget.Rendering += OnFrame;
    void OnFrame(object? sender, EventArgs e)
    {
        double t = Math.Min(1.0, sw.Elapsed.TotalSeconds / duration.TotalSeconds);
        overlay.Opacity = 1 - Math.Pow(1 - t, 3);
        if (t >= 1.0) CompositionTarget.Rendering -= OnFrame;
    }
}
```

## 35.8 颜色动画进阶

### 35.8.1 渐变动画

```csharp
public static void AnimateGradient(LinearGradientBrush brush, Color from, Color to, TimeSpan duration)
{
    var sw = Stopwatch.StartNew();
    CompositionTarget.Rendering += OnFrame;
    void OnFrame(object? sender, EventArgs e)
    {
        double t = Math.Min(1.0, sw.Elapsed.TotalSeconds / duration.TotalSeconds);
        double eased = t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        brush.GradientStops[1].Color = Color.FromArgb(
            (byte)(from.A + (to.A - from.A) * eased),
            (byte)(from.R + (to.R - from.R) * eased),
            (byte)(from.G + (to.G - from.G) * eased),
            (byte)(from.B + (to.B - from.B) * eased));
        if (t >= 1.0) CompositionTarget.Rendering -= OnFrame;
    }
}
```

### 35.8.2 主题切换动画

收集旧/新主题的颜色资源键，在 `CompositionTarget.Rendering` 中逐帧插值所有颜色，实现平滑主题过渡。

### 35.8.3 HSL 颜色空间动画

RGB 插值可能产生"脏色"中间态。HSL 插值保持颜色鲜艳：

```csharp
public static Color LerpHsl(Color from, Color to, double t)
{
    var hslF = RgbToHsl(from); var hslT = RgbToHsl(to);
    // 色相走最短路径
    if (Math.Abs(hslT.H - hslF.H) > 180)
    { if (hslT.H > hslF.H) hslF.H += 360; else hslT.H += 360; }
    return HslToRgb(new HslColor
    {
        H = hslF.H + (hslT.H - hslF.H) * t,
        S = hslF.S + (hslT.S - hslF.S) * t,
        L = hslF.L + (hslT.L - hslF.L) * t,
        A = hslF.A + (hslT.A - hslF.A) * t
    });
}
```

## 35.9 变换动画进阶

### 35.9.1 TransformGroup 组合变换

```xml
<Image Source="/Assets/logo.png" Width="100" Height="100">
    <Image.RenderTransform>
        <TransformGroup>
            <ScaleTransform ScaleX="1" ScaleY="1"/>
            <RotateTransform Angle="0"/>
            <TranslateTransform X="0" Y="0"/>
        </TransformGroup>
    </Image.RenderTransform>
</Image>
```

> **变换顺序**：先缩放再旋转，和先旋转再缩放，效果完全不同。通常顺序：Scale -> Rotate -> Translate。

### 35.9.2 矩阵变换动画

使用 `MatrixTransform` 逐元素插值两个矩阵之间的变换：`new Matrix(Lerp(from.M11, to.M11, t), ...)`。适用于自定义变换组合。

### 35.9.3 3D 旋转效果

Avalonia 使用 `PerspectiveTransform` 实现 3D 翻转。XAML 中直接声明：

```xml
<Border Width="200" Height="200" Background="CornflowerBlue">
    <Border.RenderTransform>
        <PerspectiveTransform/>
    </Border.RenderTransform>
</Border>
```

代码中通过修改 `M11 = cos(angle)`, `M13 = sin(angle) * 0.001` 实现 Y 轴翻转，在 90 度时切换前后内容。

### 35.9.4 扭曲效果

`SkewTransform` 动画可模拟物体快速移动时的形变：`skew.AngleX = Math.Clamp(-velocityX * 0.05, -maxSkew, maxSkew)`，然后平滑恢复到 0。

## 35.10 状态动画

### 35.10.1 VisualStateManager

```xml
<Button Content="状态动画按钮" Width="200" Height="50">
    <Button.Template>
        <ControlTemplate>
            <Border x:Name="PART_Border" Background="#3498DB" CornerRadius="8">
                <ContentPresenter Content="{TemplateBinding Content}"
                    HorizontalAlignment="Center" VerticalAlignment="Center"/>
            </Border>
        </ControlTemplate>
    </Button.Template>

    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup x:Name="CommonStates">
            <VisualState x:Name="Normal"/>
            <VisualState x:Name="PointerOver">
                <VisualState.Setters>
                    <Setter Target="PART_Border" Property="Background" Value="#2980B9"/>
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="Pressed">
                <VisualState.Setters>
                    <Setter Target="PART_Border" Property="Background" Value="#1F6DAD"/>
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="Disabled">
                <VisualState.Setters>
                    <Setter Target="PART_Border" Property="Background" Value="#BDC3C7"/>
                    <Setter Target="PART_Border" Property="Opacity" Value="0.6"/>
                </VisualState.Setters>
            </VisualState>
            <VisualStateGroup.Transitions>
                <VisualTransition Duration="0:0:0.2" From="Normal" To="PointerOver"/>
                <VisualTransition Duration="0:0:0.1" From="PointerOver" To="Pressed"/>
            </VisualStateGroup.Transitions>
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
</Button>
```

### 35.10.2 代码中切换状态

```csharp
public partial class StatusCard : UserControl
{
    public void SetConnectionState(string stateName) =>
        VisualStateManager.GoToState(this, stateName, true);
}
```

## 35.11 动画触发器

### 35.11.1 DataTrigger 动画

```xml
<Border Width="200" Height="100" Classes.active="{Binding IsConnected}">
    <Border.Styles>
        <Style Selector="Border">
            <Setter Property="Background" Value="#E74C3C"/>
            <Setter Property="Transitions">
                <Transitions><BrushTransition Property="Background" Duration="0:0:0.4"/></Transitions>
            </Setter>
        </Style>
        <Style Selector="Border.active">
            <Setter Property="Background" Value="#2ECC71"/>
        </Style>
    </Border.Styles>
</Border>
```

### 35.11.2 多条件触发

```csharp
public void UpdateAnimation(bool isHovered, bool isPressed, bool isEnabled)
{
    if (!isEnabled) ApplyDisabledAnimation();
    else if (isPressed) ApplyPressedAnimation();
    else if (isHovered) ApplyHoverAnimation();
    else ApplyNormalAnimation();
}
```

## 35.12 页面过渡动画

### 35.12.1 内置过渡

```xml
<!-- 滑动过渡 -->
<ContentControl Content="{Binding CurrentPage}">
    <ContentControl.ContentTransition>
        <PageSlide Duration="0:0:0.3" Orientation="Horizontal"/>
    </ContentControl.ContentTransition>
</ContentControl>

<!-- 淡入淡出 -->
<ContentControl Content="{Binding CurrentPage}">
    <ContentControl.ContentTransition>
        <CrossFade Duration="0:0:0.3"/>
    </ContentControl.ContentTransition>
</ContentControl>

<!-- 组合过渡 -->
<ContentControl Content="{Binding CurrentPage}">
    <ContentControl.ContentTransition>
        <CompositePageTransition>
            <PageSlide Duration="0:0:0.4" Orientation="Horizontal"/>
            <CrossFade Duration="0:0:0.2"/>
        </CompositePageTransition>
    </ContentControl.ContentTransition>
</ContentControl>
```

### 35.12.2 自定义缩放过渡

实现 `IPageTransition` 接口：旧页面从 `ScaleTransform(1)` 缩小到 `0.8` + 淡出，新页面从 `0.8` 放大到 `1` + 淡入。使用 `TaskCompletionSource` 等待动画完成。核心缓动：`EaseOutCubic(t) = 1 - (1-t)^3`。

## 35.13 列表动画

### 35.13.1 添加/删除项目动画

重写 `PrepareContainerForItemOverride`：设置初始 `Opacity=0` + `TranslateTransform(0,30)`，添加 `DoubleTransition` 和 `TransformOperationsTransition`，然后在下一帧设置目标值触发过渡动画。删除项目时反向播放。

## 35.14 动画性能优化

### 35.14.1 GPU 加速属性

| 属性 | GPU 加速 | 原因 |
|------|----------|------|
| `Opacity` | 是 | 合成层属性 |
| `RenderTransform` | 是 | 合成层变换 |
| `Width` / `Height` | 否 | 触发重新布局 |
| `Margin` / `Padding` | 否 | 触发重新布局 |

```xml
<!-- 好：用 Opacity + RenderTransform -->
<Border RenderTransform="translateX(0)" Opacity="1">
    <Border.Styles>
        <Style Selector="Border:pointerover">
            <Setter Property="Opacity" Value="0.8"/>
            <Setter Property="RenderTransform" Value="translateX(10px)"/>
        </Style>
    </Border.Styles>
</Border>
<!-- 不好：用 Width/Height/Margin 做动画，触发布局重算 -->
```

### 35.14.2 对象复用与帧率监控

复用 `TranslateTransform` 实例而非每帧 `new`，减少 GC 压力。帧率监控：订阅 `CompositionTarget.Rendering`，每秒统计帧数，低于 30fps 时警告。

### 35.14.3 电池优化

窗口不可见时停止动画（返回 0fps），减弱模式下降至 15fps，正常运行 60fps。

## 35.15 动画调试

### 35.15.1 时间线可视化

创建调试控件：在 `Render()` 中为每个活跃动画绘制灰色背景条 + 蓝色进度条，每帧更新。动画完成后移除条目。

### 35.15.2 慢速播放调试

通过 `Animation.SpeedRatio` 或自定义时钟将所有动画速度降至 0.1x，方便观察细节。也可通过反射批量修改 `Duration` 实现全局慢速。

## 35.16 实战：复杂动画案例

### 35.16.1 启动画面动画（Logo 展开 + 渐显）

```csharp
public class SplashScreenAnimation
{
    private readonly Control _logo, _tagline;
    private readonly Stopwatch _sw = new();

    public SplashScreenAnimation(Control logo, Control tagline)
    {
        _logo = logo; _tagline = tagline;
        _logo.Opacity = 0;
        _logo.RenderTransform = new ScaleTransform(0.3, 0.3);
        _logo.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        _tagline.Opacity = 0;
        _tagline.RenderTransform = new TranslateTransform(0, 20);
    }

    public void Play() { _sw.Start(); CompositionTarget.Rendering += OnFrame; }

    private void OnFrame(object? s, EventArgs e)
    {
        double elapsed = _sw.Elapsed.TotalSeconds;
        // 0-0.8s: Logo 弹性展开
        if (elapsed < 0.8)
        {
            double t = EaseOutBack(elapsed / 0.8);
            _logo.Opacity = t;
            _logo.RenderTransform = new ScaleTransform(0.3 + 0.7 * t, 0.3 + 0.7 * t);
        }
        // 0.5-1.2s: Tagline 淡入上移
        if (elapsed > 0.5 && elapsed < 1.2)
        {
            double t = 1 - Math.Pow(1 - (elapsed - 0.5) / 0.7, 3);
            _tagline.Opacity = t;
            _tagline.RenderTransform = new TranslateTransform(0, 20 * (1 - t));
        }
        if (elapsed > 1.5) CompositionTarget.Rendering -= OnFrame;
    }

    // 弹性缓出：略微超过目标再回来
    static double EaseOutBack(double t)
    { const double c1 = 1.70158, c3 = c1 + 1; return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2); }
}
```

### 35.16.2 通知弹出动画（弹簧效果）

```csharp
public static void ShowNotification(Control notification, Panel container)
{
    container.Children.Add(notification);
    notification.Opacity = 0;
    notification.RenderTransform = new TranslateTransform(400, 0);

    var spring = new SpringAnimation { Stiffness = 200, Damping = 18, Mass = 1.0 };
    var sw = Stopwatch.StartNew();

    CompositionTarget.Rendering += OnFrame;
    void OnFrame(object? s, EventArgs e)
    {
        double elapsed = sw.Elapsed.TotalSeconds;
        double x = spring.Evaluate(400, 0, elapsed);
        notification.RenderTransform = new TranslateTransform(x, 0);
        notification.Opacity = Math.Min(1.0, elapsed / 0.2);
        if (spring.IsComplete(x, 0, 0)) CompositionTarget.Rendering -= OnFrame;
    }
}
```

### 35.16.3 侧边栏展开/收起动画

```csharp
public class AnimatedSidebar : Border
{
    private double _currentWidth, _targetWidth;
    private readonly double _expanded = 250, _collapsed = 60;
    private bool _isExpanded = true;
    private readonly Stopwatch _sw = new();

    public void Toggle()
    {
        _currentWidth = _isExpanded ? _expanded : _collapsed;
        _targetWidth = _isExpanded ? _collapsed : _expanded;
        _isExpanded = !_isExpanded;
        _sw.Restart();
        CompositionTarget.Rendering += OnFrame;
    }

    private void OnFrame(object? s, EventArgs e)
    {
        double t = Math.Min(1.0, _sw.Elapsed.TotalSeconds / 0.3);
        double eased = 1 - Math.Pow(1 - t, 3);
        Width = _currentWidth + (_targetWidth - _currentWidth) * eased;
        if (Child != null) Child.Opacity = _isExpanded ? eased : 1 - eased;
        if (t >= 1.0) CompositionTarget.Rendering -= OnFrame;
    }
}
```

### 35.16.4 卡片翻转动画

```csharp
public class FlipCard : Border
{
    private Control? _front, _back;
    private bool _showingFront = true, _isFlipping;

    public FlipCard(Control front, Control back)
    {
        _front = front; _back = back; Child = _front;
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        PointerPressed += (_, _) => Flip();
    }

    public void Flip()
    {
        if (_isFlipping) return; _isFlipping = true;
        var sw = Stopwatch.StartNew();
        CompositionTarget.Rendering += OnFrame;
        void OnFrame(object? s, EventArgs e)
        {
            double t = Math.Min(1.0, sw.Elapsed.TotalSeconds / 0.5);
            double scaleX = t < 0.5 ? 1 - t * 2 : (t - 0.5) * 2;
            RenderTransform = new ScaleTransform(scaleX, 1);
            if (t >= 0.5 && _showingFront) { Child = _back; _showingFront = false; }
            else if (t >= 0.5 && !_showingFront) { Child = _front; _showingFront = true; }
            if (t >= 1.0) { RenderTransform = null; _isFlipping = false; CompositionTarget.Rendering -= OnFrame; }
        }
    }
}
```

### 35.16.5 加载骨架屏动画

```csharp
public class ShimmerSkeleton : Control
{
    private readonly Stopwatch _sw = new();
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    { base.OnAttachedToVisualTree(e); _sw.Start(); }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width, h = Bounds.Height;
        context.DrawRectangle(new SolidColorBrush(Color.FromRgb(220, 220, 220)), null, new Rect(0, 0, w, h), 8, 8);

        // 光波扫过效果
        double shimmerW = w * 0.4;
        double shimmerX = (_sw.Elapsed.TotalSeconds * 200) % (w + shimmerW) - shimmerW;
        var shimmer = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(shimmerX, 0, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(shimmerX + shimmerW, 0, RelativeUnit.Absolute),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 0),
                new GradientStop(Color.FromArgb(100, 255, 255, 255), 0.5),
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
            }
        };
        using (context.PushClip(new RoundedRect(new Rect(0, 0, w, h), 8, 8)))
            context.DrawRectangle(shimmer, null, new Rect(0, 0, w, h));

        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }
}
```

### 35.16.6 进度环动画

```csharp
public class ProgressRing : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(Progress));
    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(StrokeWidth), 6.0);

    private double _displayProgress;
    private readonly Stopwatch _animSw = new();
    private double _animStart, _animTarget;

    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    static ProgressRing()
    {
        ProgressProperty.Changed.AddClassHandler<ProgressRing>((x, e) =>
        { x._animStart = x._displayProgress; x._animTarget = (double)e.NewValue!; x._animSw.Restart(); });
    }

    public override void Render(DrawingContext context)
    {
        if (_animSw.IsRunning)
        {
            double t = Math.Min(1.0, _animSw.Elapsed.TotalSeconds / 0.4);
            _displayProgress = _animStart + (_animTarget - _animStart) * (1 - Math.Pow(1 - t, 3));
            if (t >= 1.0) _animSw.Stop();
        }

        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        double radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - StrokeWidth;
        double endAngle = -Math.PI / 2 + 2 * Math.PI * _displayProgress;

        // 背景圆环
        context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)), StrokeWidth), center, radius, radius);

        // 进度弧
        if (_displayProgress > 0)
        {
            var geo = new StreamGeometry();
            using (var sg = geo.Open())
            {
                double startA = -Math.PI / 2;
                sg.BeginFigure(new Point(center.X + radius * Math.Cos(startA), center.Y + radius * Math.Sin(startA)), false);
                int segs = Math.Max(2, (int)(_displayProgress * radius * Math.PI / 3));
                for (int i = 1; i <= segs; i++)
                {
                    double a = startA + (endAngle - startA) * i / segs;
                    sg.LineTo(new Point(center.X + radius * Math.Cos(a), center.Y + radius * Math.Sin(a)));
                }
                sg.EndFigure(false);
            }
            context.DrawGeometry(null, new Pen(Brushes.CornflowerBlue, StrokeWidth, lineCap: PenLineCap.Round), geo);
        }

        if (_animSw.IsRunning) Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }
}
```

```xml
<local:ProgressRing Progress="{Binding DownloadProgress}" Width="100" Height="100" StrokeWidth="8"/>
```

## Deep Dive

**动画系统架构**：`Rendering Loop -> Animation Scheduler -> Transitions / Keyframes / CSS Animations`，加上 `Custom Rendering`（Render 方法）和 `Dispatcher Callbacks`。

**动画生命周期**：创建 -> 样式绑定 -> 启动 -> 每帧调度 -> 应用属性 -> 渲染 -> 清理。

**线程模型**：`CompositionTarget.Rendering` 在渲染线程执行，属性更新需回 UI 线程。粒子系统等大量绘制场景直接在 `Render()` 中绘制更高效，避免属性更新开销。

**关键帧插值**：线性 `lerp(a,b,t)`、贝塞尔 `cubic-bezier(x1,y1,x2,y2)`、离散 `t<1?a:b`。贝塞尔公式：`B(t) = (1-t)^3*P0 + 3(1-t)^2*t*P1 + 3(1-t)*t^2*P2 + t^3*P3`。

## Cross References

- **第 9 章**：`Render()` 自定义绘制 | **第 10 章**：基础动画/Transition/关键帧
- **第 14 章**：DrawingContext/SKCanvas | **第 17 章**：对话框动画 | **第 29 章**：PathGeometry

## Common Pitfalls

1. **忘记取消订阅 Rendering**：控件销毁后回调仍在运行导致内存泄漏。在 `OnDetachedFromVisualTree` 中取消。

2. **用帧计数代替时间计算**：`_x += 2` 在 60fps 设备每秒 120px，30fps 设备 60px。应使用 `_x += speed * deltaTime`。

3. **对 Width/Height 做动画**：触发布局重算，性能差。用 `ScaleTransform` 替代。

4. **每帧创建新对象**：`control.RenderTransform = new TranslateTransform(x, y)` 每帧分配。应复用 `TranslateTransform` 实例。

5. **动画重叠不处理**：快速点击导致多个动画叠加。应先取消旧动画再启动新的。

6. **弹簧参数不合理**：阻尼太小导致永远振荡。参考 `IOSSpringPresets` 的合理参数。

7. **在非 UI 线程更新属性**：`Task.Run(() => control.Opacity = 0.5)` 会抛异常。用 `Dispatcher.UIThread.Post()`。

8. **粒子数量无上限**：粒子越来越多最终崩溃。设置 `MaxParticles` 限制。

9. **RenderTransformOrigin 未设置**：缩放/旋转默认以左上角为原点。应设置 `"0.5,0.5"` 为中心。

10. **Transition 和 Animation 冲突**：同时作用于同一属性产生不可预期行为。选择一种方式。

11. **忽略减弱动画偏好**：应尊重用户的减少动画系统设置。

12. **动画完成后不设最终值**：确保 `t >= 1.0` 时设置精确的最终属性值。

## Try It Yourself

1. **弹跳球**：使用 `GravityAnimation`，落地时加 `ScaleTransform` 模拟弹性形变。
2. **粒子文字**：将文字像素位置记录为目标，点击后粒子散开，再点击聚拢。
3. **弹性侧边栏**：用 `SpringAnimation` 控制宽度，带弹跳效果。
4. **路径跟随加载器**：3 个点沿圆形路径运动，参考 35.4.3。
5. **卡片翻转 + 阴影**：组合 `FlipCard` 和 `ShadowAnimator`。
6. **列表添加动画**：新项目从右侧滑入 + 淡入，旧项目下移让位。
7. **进度环**：参考 35.16.6，进度变化平滑过渡。
8. **雪花背景**：参考 35.5.3，加 `Math.Sin` 水平摆动。
9. **主题颜色过渡**：切换深色/浅色时所有颜色平滑过渡。
10. **FPS 监控器**：角落显示实时 FPS，低于 30 时变红警告。
