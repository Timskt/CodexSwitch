# 29. 形状与矢量绘图

> **写给零基础的你**：前面第 14 章讲了用代码画画（DrawingContext），本章讲用 AXAML 声明式地画画。就像用 HTML 画图比用 JavaScript 画图更直观一样，用 AXAML 定义形状比用 C# 代码更简单。Shape 控件是 Avalonia 中绘制矢量图形的基础，它们可以用于创建图表、图标、装饰元素、数据可视化等。

## 29.1 概述

Avalonia 提供了一套完整的矢量绘图控件（Shape），可以在 AXAML 中声明式地创建各种几何形状。这些控件支持样式、绑定、动画，是创建图表、图标、装饰元素的基础。CodexSwitch 使用 PathIcon 来显示导航图标和状态图标。

学完本章后，你将能够：
- 掌握所有 Shape 控件（Line, Rectangle, Ellipse, Polygon, Polyline, Path）
- 理解 Path 的 Data 语法（M, L, H, V, C, Q, A, Z 等命令）
- 掌握所有 Brush 类型（SolidColorBrush, LinearGradientBrush, RadialGradientBrush, ImageBrush）
- 理解 Geometry 和 Shape 的区别
- 掌握 Pen 属性（DashArray, LineCap, LineJoin）
- 掌握 PathIcon 和 DrawingGroup 的用法

## 29.2 核心概念

### 29.2.1 Shape 控件一览

所有 Shape 控件都继承自 `Shape` 基类，共享以下公共属性：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Fill` | IBrush | 填充画刷 |
| `Stroke` | IBrush | 描边画刷 |
| `StrokeThickness` | double | 描边宽度 |
| `StrokeDashArray` | IList<double> | 虚线模式 |
| `StrokeDashOffset` | double | 虚线偏移 |
| `StrokeStartLineCap` | PenLineCap | 起始端点样式 |
| `StrokeEndLineCap` | PenLineCap | 结束端点样式 |
| `StrokeLineJoin` | PenLineJoin | 连接点样式 |
| `StrokeMiterLimit` | double | 尖角限制 |
| `Stretch` | Stretch | 拉伸模式 |
| `GeometryTransform` | Transform | 几何变换 |

**Shape 控件列表：**

| 控件 | 说明 | 关键属性 |
|------|------|---------|
| `Line` | 线段 | StartPoint, EndPoint |
| `Rectangle` | 矩形 | Width, Height, RadiusX, RadiusY |
| `Ellipse` | 椭圆 | Width, Height |
| `Polygon` | 多边形（闭合） | Points |
| `Polyline` | 折线（不闭合） | Points |
| `Path` | 任意路径 | Data |

### 29.2.2 Line（线段）

Line 绘制两点之间的直线段。

**Line 的所有属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `StartPoint` | Point | 起点坐标 |
| `EndPoint` | Point | 终点坐标 |
| `Stroke` | IBrush | 线条颜色 |
| `StrokeThickness` | double | 线条宽度 |

#### 示例 1：基本线段

```xml
<Line StartPoint="0,0" EndPoint="100,50"
      Stroke="Red" StrokeThickness="2"/>
```

#### 示例 2：虚线

```xml
<Line StartPoint="0,0" EndPoint="200,0"
      Stroke="Gray" StrokeThickness="1"
      StrokeDashArray="4,2"/>
```

`StrokeDashArray="4,2"` 表示：4px 实线，2px 空白，重复。

#### 示例 3：带端点的线

```xml
<!-- 起始端：圆形 -->
<!-- 结束端：三角形 -->
<Line StartPoint="10,10" EndPoint="190,10"
      Stroke="Blue" StrokeThickness="2"
      StrokeStartLineCap="Round"
      StrokeEndLineCap="Triangle"/>
```

**PenLineCap 端点样式：**

| 值 | 说明 |
|---|------|
| `Flat` | 平头（默认） |
| `Square` | 方头 |
| `Round` | 圆头 |
| `Triangle` | 三角头 |

#### 示例 4：虚线模式

```xml
<!-- 短虚线 -->
<Line StrokeDashArray="2,2"/>

<!-- 长虚线 -->
<Line StrokeDashArray="6,3"/>

<!-- 点划线 -->
<Line StrokeDashArray="6,2,2,2"/>

<!-- 长短交替 -->
<Line StrokeDashArray="8,3,2,3"/>
```

#### 示例 5：绘制坐标轴

```xml
<Canvas Width="300" Height="200">
    <!-- X 轴 -->
    <Line StartPoint="30,180" EndPoint="280,180"
          Stroke="Black" StrokeThickness="2"/>
    <!-- Y 轴 -->
    <Line StartPoint="30,180" EndPoint="30,20"
          Stroke="Black" StrokeThickness="2"/>
    <!-- 刻度 -->
    <Line StartPoint="80,180" EndPoint="80,175" Stroke="Black"/>
    <Line StartPoint="130,180" EndPoint="130,175" Stroke="Black"/>
    <Line StartPoint="180,180" EndPoint="180,175" Stroke="Black"/>
    <Line StartPoint="230,180" EndPoint="230,175" Stroke="Black"/>
</Canvas>
```

### 29.2.3 Rectangle（矩形）

Rectangle 绘制矩形或圆角矩形。

**Rectangle 的所有属性：**

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Width` | double | NaN | 宽度 |
| `Height` | double | NaN | 高度 |
| `RadiusX` | double | 0 | X 方向圆角半径 |
| `RadiusY` | double | 0 | Y 方向圆角半径 |

#### 示例 1：基本矩形

```xml
<Rectangle Width="100" Height="50" Fill="Blue"/>
```

#### 示例 2：圆角矩形

```xml
<Rectangle Width="100" Height="50" Fill="Green"
           RadiusX="10" RadiusY="10"/>
```

#### 示例 3：带边框的矩形

```xml
<Rectangle Width="100" Height="50"
           Fill="Transparent"
           Stroke="Black" StrokeThickness="2"/>
```

#### 示例 4：虚线边框

```xml
<Rectangle Width="100" Height="50"
           Fill="Transparent"
           Stroke="Gray" StrokeThickness="1"
           StrokeDashArray="4,2"/>
```

#### 示例 5：圆形（RadiusX/RadiusY 设为宽度的一半）

```xml
<!-- 当 RadiusX 和 RadiusY 设为宽高的一半时，变成椭圆或圆形 -->
<Rectangle Width="100" Height="100" Fill="Red"
           RadiusX="50" RadiusY="50"/>
```

#### 示例 6：使用 Rectangle 作为卡片背景

```xml
<Grid>
    <Rectangle Fill="{StaticResource CardBackgroundBrush}"
               RadiusX="8" RadiusY="8"/>
    <StackPanel Margin="16">
        <TextBlock Text="卡片标题" FontWeight="Bold"/>
        <TextBlock Text="卡片内容"/>
    </StackPanel>
</Grid>
```

### 29.2.4 Ellipse（椭圆）

Ellipse 绘制椭圆或圆形。

#### 示例 1：椭圆

```xml
<Ellipse Width="100" Height="60" Fill="Red"/>
```

#### 示例 2：圆形（宽高相等）

```xml
<Ellipse Width="50" Height="50" Fill="Blue"/>
```

#### 示例 3：空心圆

```xml
<Ellipse Width="50" Height="50"
         Fill="Transparent"
         Stroke="Black" StrokeThickness="2"/>
```

#### 示例 4：使用 Ellipse 作为头像占位符

```xml
<Border Width="48" Height="48" CornerRadius="24"
        ClipToBounds="True">
    <Ellipse Width="48" Height="48" Fill="{StaticResource AvatarPlaceholderBrush}"/>
</Border>
```

#### 示例 5：使用 Ellipse 绘制饼图的一部分

```xml
<!-- 使用 Ellipse 的 StrokeDashArray 模拟饼图 -->
<Ellipse Width="100" Height="100"
         Fill="Transparent"
         Stroke="Blue" StrokeThickness="20"
         StrokeDashArray="31.4,68.6"
         StrokeDashOffset="25"
         StrokeStartLineCap="Round"
         StrokeEndLineCap="Round"/>
```

### 29.2.5 Polygon（多边形）

Polygon 绘制闭合的多边形，由一系列顶点定义。

**Polygon 的属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Points` | IList<Point> | 顶点坐标列表 |
| `FillRule` | FillRule | 填充规则（EvenOdd/Nonzero） |

#### 示例 1：三角形

```xml
<Polygon Points="50,0 0,100 100,100" Fill="Yellow"/>
```

#### 示例 2：五角星

```xml
<Polygon Points="50,0 61,35 98,35 68,57 79,91 50,70 21,91 32,57 2,35 39,35"
         Fill="Gold" Stroke="Orange" StrokeThickness="1"/>
```

#### 示例 3：菱形

```xml
<Polygon Points="50,0 100,50 50,100 0,50" Fill="Purple"/>
```

#### 示例 4：六边形

```xml
<Polygon Points="75,0 150,43 150,130 75,173 0,130 0,43"
         Fill="LightBlue" Stroke="Blue" StrokeThickness="2"/>
```

#### 示例 5：箭头形状

```xml
<Polygon Points="0,20 60,20 60,0 100,35 60,70 60,50 0,50"
         Fill="Green"/>
```

#### 示例 6：FillRule 对比

```xml
<!-- EvenOdd：奇偶规则 -->
<Polygon Points="..." FillRule="EvenOdd" Fill="Red"/>

<!-- Nonzero：非零规则 -->
<Polygon Points="..." FillRule="Nonzero" Fill="Blue"/>
```

### 29.2.6 Polyline（折线）

Polyline 绘制不闭合的折线，常用于绘制图表。

#### 示例 1：基本折线

```xml
<Polyline Points="0,100 20,80 40,90 60,40 80,60 100,20"
          Stroke="Blue" StrokeThickness="2"
          Fill="Transparent"/>
```

#### 示例 2：带填充的折线图

```xml
<!-- 填充折线下方的区域 -->
<Polyline Points="0,100 20,80 40,90 60,40 80,60 100,20"
          Stroke="Blue" StrokeThickness="2"
          Fill="LightBlue"/>
```

#### 示例 3：折线图（带坐标轴）

```xml
<Canvas Width="300" Height="200">
    <!-- 坐标轴 -->
    <Line StartPoint="30,180" EndPoint="280,180"
          Stroke="Black" StrokeThickness="1"/>
    <Line StartPoint="30,180" EndPoint="30,20"
          Stroke="Black" StrokeThickness="1"/>

    <!-- 数据线 -->
    <Polyline Points="30,160 70,120 110,140 150,60 190,100 230,40 270,80"
              Stroke="Red" StrokeThickness="2"
              Fill="Transparent"/>

    <!-- 数据点 -->
    <Ellipse Width="6" Height="6" Fill="Red"
             Canvas.Left="27" Canvas.Top="157"/>
    <Ellipse Width="6" Height="6" Fill="Red"
             Canvas.Left="67" Canvas.Top="117"/>
    <!-- ... 更多数据点 -->
</Canvas>
```

### 29.2.7 Path（路径）

Path 是最强大的 Shape 控件，可以绘制任意复杂的几何图形。它通过 Data 属性接收几何数据。

**Path 的属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Data` | Geometry | 几何数据（使用 Path Mini Language） |
| `Fill` | IBrush | 填充画刷 |
| `Stroke` | IBrush | 描边画刷 |

#### Path Mini Language 命令详解

Path Mini Language 是一种用于定义几何路径的字符串语法。每个命令由一个字母和一组坐标组成。

| 命令 | 参数 | 说明 | 示例 |
|------|------|------|------|
| `M x,y` | x, y | 移动到 (MoveTo)，不绘制 | `M 10,10` |
| `L x,y` | x, y | 直线到 (LineTo) | `L 100,10` |
| `H x` | x | 水平线到 x 位置 | `H 100` |
| `V y` | y | 垂直线到 y 位置 | `V 100` |
| `C x1,y1 x2,y2 x,y` | 控制点1, 控制点2, 终点 | 三次贝塞尔曲线 | `C 40,10 60,10 90,100` |
| `Q x1,y1 x,y` | 控制点, 终点 | 二次贝塞尔曲线 | `Q 50,10 90,100` |
| `A rx,ry rot large sweep x,y` | 半径, 旋转, 大弧, 扫描, 终点 | 弧线 | `A 40,40 0 0,1 90,50` |
| `Z` | 无 | 闭合路径（回到起点） | `Z` |

**小写命令表示相对坐标：**

| 大写（绝对） | 小写（相对） | 说明 |
|-------------|-------------|------|
| `M x,y` | `m dx,dy` | 移动 |
| `L x,y` | `l dx,dy` | 直线 |
| `H x` | `h dx` | 水平线 |
| `V y` | `v dy` | 垂直线 |
| `C x1,y1 x2,y2 x,y` | `c dx1,dy1 dx2,dy2 dx,dy` | 三次贝塞尔 |
| `Q x1,y1 x,y` | `q dx1,dy1 dx,dy` | 二次贝塞尔 |
| `A ...` | `a ...` | 弧线 |

#### 示例 1：基本路径

```xml
<!-- 使用 PathGeometry（详细写法） -->
<Path Stroke="Black" StrokeThickness="2" Fill="LightBlue">
    <Path.Data>
        <PathGeometry>
            <PathFigure StartPoint="10,10">
                <LineSegment Point="100,10"/>
                <LineSegment Point="100,100"/>
                <LineSegment Point="10,100"/>
                <LineSegment Point="10,10"/>
            </PathFigure>
        </PathGeometry>
    </Path.Data>
</Path>

<!-- 使用 Path Mini Language（简洁写法） -->
<Path Data="M 10,10 L 100,10 L 100,100 L 10,100 Z"
      Stroke="Black" StrokeThickness="2" Fill="LightBlue"/>
```

#### 示例 2：贝塞尔曲线

```xml
<!-- 三次贝塞尔曲线（S 形） -->
<Path Data="M 10,100 C 40,10 60,10 90,100"
      Stroke="Red" StrokeThickness="2" Fill="Transparent"/>

<!-- 二次贝塞尔曲线（抛物线） -->
<Path Data="M 10,100 Q 50,10 90,100"
      Stroke="Blue" StrokeThickness="2" Fill="Transparent"/>

<!-- 多段贝塞尔曲线 -->
<Path Data="M 10,100 C 30,10 50,10 70,100 C 90,10 110,10 130,100"
      Stroke="Green" StrokeThickness="2" Fill="Transparent"/>
```

#### 示例 3：弧线

```xml
<!-- 弧线：rx,ry 旋转角度 大弧标记 扫描方向 终点 -->
<Path Data="M 10,50 A 40,40 0 0,1 90,50"
      Stroke="Green" StrokeThickness="2" Fill="Transparent"/>

<!-- 大弧 -->
<Path Data="M 10,50 A 40,40 0 1,1 90,50"
      Stroke="Orange" StrokeThickness="2" Fill="Transparent"/>

<!-- 椭圆弧 -->
<Path Data="M 10,50 A 60,30 0 0,1 90,50"
      Stroke="Purple" StrokeThickness="2" Fill="Transparent"/>
```

**Arc 命令参数详解：**
- `rx, ry`：椭圆的 X 和 Y 半径
- `rot`：椭圆的旋转角度
- `large`：0=小弧，1=大弧
- `sweep`：0=逆时针，1=顺时针
- `x, y`：终点坐标

#### 示例 4：使用相对坐标

```xml
<!-- 绝对坐标 -->
<Path Data="M 10,10 L 50,10 L 50,50 L 10,50 Z"/>

<!-- 等效的相对坐标 -->
<Path Data="M 10,10 l 40,0 l 0,40 l -40,0 z"/>
```

#### 示例 5：复杂的 Path（心形）

```xml
<Path Data="M 50,30 C 50,0 0,0 0,35 C 0,70 50,90 50,100 C 50,90 100,70 100,35 C 100,0 50,0 50,30 Z"
      Fill="Red" Stroke="DarkRed" StrokeThickness="1"/>
```

#### 示例 6：圆角矩形 Path

```xml
<!-- 使用 H, V, A 命令绘制圆角矩形 -->
<Path Data="M 20,0 H 180 A 20,20 0 0,1 200,20 V 80 A 20,20 0 0,1 180,100 H 20 A 20,20 0 0,1 0,80 V 20 A 20,20 0 0,1 20,0 Z"
      Fill="White" Stroke="Gray" StrokeThickness="1"/>
```

### 29.2.8 Brush 类型详解

Brush（画刷）定义了如何填充或描边 Shape。Avalonia 支持多种 Brush 类型。

#### SolidColorBrush（纯色画刷）

```xml
<!-- 颜色名称 -->
<Rectangle Fill="Red"/>

<!-- 十六进制颜色 -->
<Rectangle Fill="#FF0000"/>
<Rectangle Fill="#80FF0000"/>  <!-- 半透明：Alpha=0x80 -->

<!-- ARGB 格式 -->
<Rectangle Fill="#FFFF0000"/>  <!-- 不透明红色 -->
<Rectangle Fill="#00FF0000"/>  <!-- 完全透明 -->

<!-- 透明 -->
<Rectangle Fill="Transparent"/>
```

**颜色格式：**
- `#RGB`：3位简写（扩展为 #RRGGBB）
- `#RRGGBB`：6位 RGB
- `#ARGB`：4位简写（扩展为 #AARRGGBB）
- `#AARRGGBB`：8位 ARGB（A=Alpha，00=透明，FF=不透明）

#### LinearGradientBrush（线性渐变）

```xml
<!-- 基本线性渐变（左上到右下） -->
<Rectangle Width="200" Height="100">
    <Rectangle.Fill>
        <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
            <GradientStop Color="Red" Offset="0"/>
            <GradientStop Color="Blue" Offset="1"/>
        </LinearGradientBrush>
    </Rectangle.Fill>
</Rectangle>

<!-- 水平渐变 -->
<LinearGradientBrush StartPoint="0%,50%" EndPoint="100%,50%">
    <GradientStop Color="#FF6B6B" Offset="0"/>
    <GradientStop Color="#4ECDC4" Offset="1"/>
</LinearGradientBrush>

<!-- 多色渐变 -->
<LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,0%">
    <GradientStop Color="Red" Offset="0"/>
    <GradientStop Color="Yellow" Offset="0.33"/>
    <GradientStop Color="Green" Offset="0.66"/>
    <GradientStop Color="Blue" Offset="1"/>
</LinearGradientBrush>

<!-- 透明渐变 -->
<LinearGradientBrush StartPoint="0%,0%" EndPoint="0%,100%">
    <GradientStop Color="Black" Offset="0"/>
    <GradientStop Color="Transparent" Offset="1"/>
</LinearGradientBrush>
```

**GradientStop 属性：**
- `Color`：颜色值
- `Offset`：偏移量（0.0 到 1.0），表示渐变位置

#### RadialGradientBrush（径向渐变）

```xml
<!-- 基本径向渐变 -->
<Ellipse Width="100" Height="100">
    <Ellipse.Fill>
        <RadialGradientBrush Center="50%,50%" RadiusX="50%" RadiusY="50%">
            <GradientStop Color="White" Offset="0"/>
            <GradientStop Color="Blue" Offset="1"/>
        </RadialGradientBrush>
    </Ellipse.Fill>
</Ellipse>

<!-- 偏心径向渐变 -->
<RadialGradientBrush Center="30%,30%" RadiusX="50%" RadiusY="50%">
    <GradientStop Color="White" Offset="0"/>
    <GradientStop Color="DarkBlue" Offset="1"/>
</RadialGradientBrush>

<!-- 金属质感 -->
<RadialGradientBrush Center="50%,30%" RadiusX="50%" RadiusY="50%">
    <GradientStop Color="#E0E0E0" Offset="0"/>
    <GradientStop Color="#808080" Offset="0.7"/>
    <GradientStop Color="#404040" Offset="1"/>
</RadialGradientBrush>
```

**RadialGradientBrush 属性：**
- `Center`：中心点（百分比）
- `RadiusX`：X 方向半径（百分比）
- `RadiusY`：Y 方向半径（百分比）
- `GradientOrigin`：渐变原点（可以不同于 Center）

#### ImageBrush（图像画刷）

```xml
<!-- 使用图像作为填充 -->
<Rectangle Width="200" Height="100">
    <Rectangle.Fill>
        <ImageBrush Source="avares://MyApp/Assets/pattern.png"
                    Stretch="UniformToFill"/>
    </Rectangle.Fill>
</Rectangle>

<!-- 背景平铺 -->
<Rectangle>
    <Rectangle.Fill>
        <ImageBrush Source="avares://MyApp/Assets/tile.png"
                    Stretch="None"
                    TileMode="Tile"
                    DestinationRect="0,0,50,50"/>
    </Rectangle.Fill>
</Rectangle>
```

**ImageBrush 属性：**
- `Source`：图像源
- `Stretch`：拉伸模式（None/Fill/Uniform/UniformToFill）
- `TileMode`：平铺模式（None/Tile/FlipX/FlipY/FlipXY）
- `DestinationRect`：目标矩形

### 29.2.9 Pen 属性详解

Pen（画笔）属性控制描边的外观。

#### StrokeDashArray（虚线模式）

```xml
<!-- 短虚线 -->
<Rectangle StrokeDashArray="4,2"/>

<!-- 长虚线 -->
<Rectangle StrokeDashArray="8,4"/>

<!-- 点划线 -->
<Rectangle StrokeDashArray="8,2,2,2"/>

<!-- 长短交替 -->
<Rectangle StrokeDashArray="10,3,3,3"/>

<!-- 密集虚线 -->
<Rectangle StrokeDashArray="1,1"/>
```

**StrokeDashArray 的值：**
- 交替表示"绘制长度,空白长度"
- 奇数个值时自动重复：`4,2` 等效于 `4,2,4,2`

#### StrokeDashOffset（虚线偏移）

```xml
<!-- 偏移虚线起始位置 -->
<Rectangle StrokeDashArray="4,2" StrokeDashOffset="2"/>
```

#### PenLineCap（端点样式）

```xml
<!-- Flat：平头（默认，与端点对齐） -->
<Line StrokeStartLineCap="Flat" StrokeEndLineCap="Flat"/>

<!-- Square：方头（延伸半线宽） -->
<Line StrokeStartLineCap="Square" StrokeEndLineCap="Square"/>

<!-- Round：圆头 -->
<Line StrokeStartLineCap="Round" StrokeEndLineCap="Round"/>

<!-- Triangle：三角头 -->
<Line StrokeStartLineCap="Triangle" StrokeEndLineCap="Triangle"/>
```

#### PenLineJoin（连接点样式）

```xml
<!-- Miter：尖角（默认） -->
<Polyline StrokeLineJoin="Miter"/>

<!-- Bevel：斜角 -->
<Polyline StrokeLineJoin="Bevel"/>

<!-- Round：圆角 -->
<Polyline StrokeLineJoin="Round"/>
```

#### StrokeMiterLimit（尖角限制）

```xml
<!-- 当两条线的夹角很小时，Miter 连接会产生很长的尖角 -->
<!-- MiterLimit 限制尖角的最大长度 -->
<Polyline StrokeLineJoin="Miter" StrokeMiterLimit="4"/>
```

### 29.2.10 Geometry 与 Shape 的区别

| 概念 | 说明 | 用途 |
|------|------|------|
| `Shape` | 可渲染的 UI 控件 | 直接放在 AXAML 中显示 |
| `Geometry` | 几何定义（不可渲染） | 用于 Path.Data、Clip、命中测试 |

```csharp
// 在代码中创建 Geometry
var geometry = Geometry.Parse("M 10,10 L 100,10 L 100,100 Z");

// 用于裁剪
myBorder.Clip = geometry;

// 用于命中测试
bool isInside = geometry.FillContains(point);

// 组合几何体
var combined = Geometry.Combine(
    geometry1, geometry2,
    GeometryCombineMode.Union);
```

**Geometry 类型：**
- `PathGeometry`：路径几何体
- `RectangleGeometry`：矩形几何体
- `EllipseGeometry`：椭圆几何体
- `StreamGeometry`：流式几何体（只写，性能好）
- `CombinedGeometry`：组合几何体

## 29.3 进阶用法

### 29.3.1 PathIcon

PathIcon 使用 SVG 路径数据显示图标，比 Image 更轻量，支持 Foreground 着色。

**PathIcon 的属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Data` | Geometry | SVG 路径数据 |
| `Width` | double | 宽度 |
| `Height` | double | 高度 |
| `Foreground` | IBrush | 前景色（图标颜色） |

#### 示例 1：基本 PathIcon

```xml
<PathIcon Data="M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
          Width="24" Height="24"
          Foreground="{StaticResource CsPrimaryBrush}"/>
```

#### 示例 2：作为按钮图标

```xml
<Button>
    <StackPanel Orientation="Horizontal" Spacing="8">
        <PathIcon Data="{StaticResource SettingsIconPath}"
                  Width="16" Height="16"/>
        <TextBlock Text="设置"/>
    </StackPanel>
</Button>
```

#### 示例 3：CodexSwitch 中的 PathIcon 使用

```xml
<!-- CodexSwitch 使用 Lucide 图标库 -->
<ui:CodexSidebarMenuButton Command="{Binding ShowHomeCommand}"
                          IsActive="{Binding IsHomeNavSelected}">
    <ui:CodexSidebarMenuButton.Icon>
        <lucide:LucideIcon Kind="LayoutDashboard" Size="17" StrokeWidth="2"/>
    </ui:CodexSidebarMenuButton.Icon>
    <TextBlock Text="{i18n:Tr nav.home}"/>
</ui:CodexSidebarMenuButton>
```

#### 示例 4：定义图标资源

```xml
<!-- 在 App.axaml 或资源字典中定义图标 -->
<Application.Resources>
    <Geometry x:Key="HomeIcon">M3,12L12,3L21,12V21H15V15H9V21H3V12Z</Geometry>
    <Geometry x:Key="SettingsIcon">M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5Z</Geometry>
</Application.Resources>

<!-- 使用 -->
<PathIcon Data="{StaticResource HomeIcon}" Width="16" Height="16"/>
```

#### 示例 5：带状态的 PathIcon

```xml
<!-- 根据状态改变图标颜色 -->
<PathIcon Data="{StaticResource CheckIcon}"
          Width="16" Height="16">
    <PathIcon.Styles>
        <Style Selector="PathIcon">
            <Setter Property="Foreground" Value="Gray"/>
        </Style>
        <Style Selector="PathIcon.success">
            <Setter Property="Foreground" Value="Green"/>
        </Style>
        <Style Selector="PathIcon.error">
            <Setter Property="Foreground" Value="Red"/>
        </Style>
    </PathIcon.Styles>
</PathIcon>
```

### 29.3.2 使用 DrawingGroup

DrawingGroup 允许将多个绘图组合在一起，支持变换和裁剪。

```xml
<Image>
    <Image.Source>
        <DrawingImage>
            <DrawingGroup>
                <GeometryDrawing Brush="Blue">
                    <GeometryDrawing.Geometry>
                        <EllipseGeometry Center="50,50" RadiusX="40" RadiusY="40"/>
                    </GeometryDrawing.Geometry>
                </GeometryDrawing>
                <GeometryDrawing Brush="White">
                    <GeometryDrawing.Geometry>
                        <EllipseGeometry Center="50,50" RadiusX="20" RadiusY="20"/>
                    </GeometryDrawing.Geometry>
                </GeometryDrawing>
            </DrawingGroup>
        </DrawingImage>
    </Image.Source>
</Image>
```

### 29.3.3 Shape 动画

```xml
<!-- 矩形颜色动画 -->
<Rectangle Width="100" Height="100" Fill="Red">
    <Rectangle.Styles>
        <Style Selector="Rectangle:pointerover">
            <Style.Animations>
                <Animation Duration="0:0:0.3">
                    <KeyFrame Cue="0%">
                        <Setter Property="Fill" Value="Red"/>
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Fill" Value="Blue"/>
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </Rectangle.Styles>
</Rectangle>

<!-- Path 绘制动画 -->
<Path Stroke="Blue" StrokeThickness="2" Fill="Transparent"
      StrokeDashArray="1,1" StrokeDashOffset="100">
    <Path.Styles>
        <Style Selector="Path">
            <Style.Animations>
                <Animation Duration="0:0:2" IterationCount="Infinite">
                    <KeyFrame Cue="0%">
                        <Setter Property="StrokeDashOffset" Value="100"/>
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="StrokeDashOffset" Value="0"/>
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </Path.Styles>
</Path>
```

## 29.4 CodexSwitch 实战

### 29.4.1 CodexSwitch 的 Brush 资源系统

CodexSwitch 在 CodexTheme.axaml 中定义了完整的 Brush 资源系统：

```xml
<Styles.Resources>
    <!-- 圆角 -->
    <CornerRadius x:Key="CsRadiusSm">6</CornerRadius>
    <CornerRadius x:Key="CsRadiusMd">8</CornerRadius>
    <CornerRadius x:Key="CsRadiusLg">10</CornerRadius>
    <CornerRadius x:Key="CsRadiusXl">14</CornerRadius>

    <!-- 主要颜色 -->
    <SolidColorBrush x:Key="CsBackgroundBrush" Color="#171717"/>
    <SolidColorBrush x:Key="CsForegroundBrush" Color="#FAFAFA"/>
    <SolidColorBrush x:Key="CsCardBrush" Color="#262626"/>
    <SolidColorBrush x:Key="CsPrimaryBrush" Color="#E5E5E5"/>
    <SolidColorBrush x:Key="CsSecondaryBrush" Color="#404040"/>
    <SolidColorBrush x:Key="CsDestructiveBrush" Color="#FF6467"/>
    <SolidColorBrush x:Key="CsSuccessBrush" Color="#36D399"/>
    <SolidColorBrush x:Key="CsWarningBrush" Color="#FACC15"/>

    <!-- 侧边栏颜色 -->
    <SolidColorBrush x:Key="CsSidebarBrush" Color="#262626"/>
    <SolidColorBrush x:Key="CsSidebarPrimaryBrush" Color="#5277FF"/>

    <!-- Provider 卡片颜色 -->
    <SolidColorBrush x:Key="CsProviderCardBrush" Color="#1F242D"/>
    <SolidColorBrush x:Key="CsProviderCardActiveBrush" Color="#142235"/>
</Styles.Resources>
```

### 29.4.2 CodexSwitch 中的自定义控件渲染

CodexSwitch 的 `CsRollingNumber` 控件展示了如何使用 DrawingContext 进行自定义渲染：

```csharp
public override void Render(DrawingContext context)
{
    base.Render(context);

    var text = FormatValue((long)Math.Round(_displayValue), UseCompactFormat);
    var layout = CreateTextLayout(text);
    var y = Math.Round((Bounds.Height - layout.Height) / 2d);

    // 动画渲染
    if (_animationTimer is not null)
    {
        var progress = GetAnimationProgress();
        var eased = EaseOutCubic(progress);
        var travel = Math.Min(12d, Math.Max(5d, FontSize * 0.55d));

        // 旧数字淡出
        using (context.PushOpacity(1d - eased))
        {
            oldLayout.Draw(context, new Point(0, y - travel * eased));
        }

        // 新数字淡入
        using (context.PushOpacity(0.35d + 0.65d * eased))
        {
            layout.Draw(context, new Point(0, y + travel * (1d - eased)));
        }
    }
}
```

## 29.5 举一反三

### 29.5.1 使用 Path 绘制图表

```xml
<!-- 柱状图 -->
<Canvas Width="300" Height="200">
    <Rectangle Canvas.Left="20" Canvas.Top="60" Width="30" Height="140"
               Fill="#4ECDC4" RadiusX="4" RadiusY="4"/>
    <Rectangle Canvas.Left="70" Canvas.Top="100" Width="30" Height="100"
               Fill="#45B7D1" RadiusX="4" RadiusY="4"/>
    <Rectangle Canvas.Left="120" Canvas.Top="40" Width="30" Height="160"
               Fill="#96CEB4" RadiusX="4" RadiusY="4"/>
    <Rectangle Canvas.Left="170" Canvas.Top="80" Width="30" Height="120"
               Fill="#FFEAA7" RadiusX="4" RadiusY="4"/>
</Canvas>
```

### 29.5.2 使用 Geometry 进行裁剪

```xml
<!-- 使用 EllipseGeometry 裁剪图片为圆形 -->
<Image Source="avares://MyApp/Assets/avatar.png"
       Width="100" Height="100">
    <Image.Clip>
        <EllipseGeometry Center="50,50" RadiusX="50" RadiusY="50"/>
    </Image.Clip>
</Image>
```

### 29.5.3 创建自定义图标系统

```xml
<!-- 定义图标字典 -->
<ResourceDictionary>
    <Geometry x:Key="Icon.Home">M3,12L12,3L21,12V21H15V15H9V21H3V12Z</Geometry>
    <Geometry x:Key="Icon.Settings">M19.14,12.94c0.04-0.3,0.06-0.61,0.06-0.94...</Geometry>
    <Geometry x:Key="Icon.Search">M15.5,14h-0.79l-0.28-0.27...</Geometry>
</ResourceDictionary>

<!-- 使用 PathIcon 引用 -->
<PathIcon Data="{StaticResource Icon.Home}" Width="16" Height="16"/>
```

## 29.6 最佳实践与设计模式

1. **简单形状用 Shape 控件**：矩形、圆形、线段直接用 AXAML 声明
2. **复杂路径用 Path Mini Language**：比 PathGeometry 更简洁
3. **图标用 PathIcon**：比 Image 更轻量，支持 Foreground 着色
4. **性能敏感场景用 Geometry**：Geometry 比 Shape 更轻量
5. **缓存复杂的 Path.Data**：定义为 StaticResource 避免重复解析
6. **使用 SolidColorBrush 资源**：统一管理颜色，方便主题切换
7. **渐变画刷注意性能**：复杂的渐变可能影响渲染性能
8. **虚线模式使用整数**：避免浮点数导致的渲染不一致

## Deep Dive

### Path Mini Language 的解析过程

1. 将字符串按命令分割（每个大写字母开始一个新命令）
2. 解析每个命令的参数
3. 构建 PathFigure 列表
4. 根据命令类型创建对应的 PathSegment
5. 组合成 PathGeometry

### Brush 的渲染原理

1. SolidColorBrush：直接使用颜色填充
2. LinearGradientBrush：计算每个像素在渐变线上的投影位置，插值计算颜色
3. RadialGradientBrush：计算每个像素到中心的距离，插值计算颜色
4. ImageBrush：将图像映射到目标区域

### Shape 的布局过程

1. MeasureOverride：根据 Stretch 和可用空间计算期望大小
2. ArrangeOverride：确定实际大小
3. Render：根据 Geometry 和 Brush 绘制

## Cross References

- **[第 14 章：自定义渲染](14-custom-rendering.md)** -- DrawingContext 编程式绘图
- **[第 7 章：样式与主题](07-styling-theming.md)** -- Brush 资源定义
- **[第 10 章：动画](10-animation-transitions.md)** -- Shape 动画
- **[第 4 章：布局系统](04-layout-system.md)** -- Canvas 布局
- **[第 15 章：自定义控件](15-custom-controls.md)** -- 自定义渲染

## Common Pitfalls

### 1. 忘记设置 Stroke 或 Fill

**问题**：Shape 默认没有 Stroke 和 Fill，看不到任何东西。

```xml
<!-- 看不到 -->
<Rectangle Width="100" Height="50"/>

<!-- 正确：至少设置一个 -->
<Rectangle Width="100" Height="50" Fill="Blue"/>
<Rectangle Width="100" Height="50" Stroke="Black" StrokeThickness="2"/>
```

### 2. Path.Data 语法错误

**问题**：Path Mini Language 的语法错误不会在编译时发现。

```xml
<!-- 错误：缺少逗号分隔符 -->
<Path Data="M 10 10 L 100 10"/>

<!-- 正确：使用逗号分隔 -->
<Path Data="M 10,10 L 100,10"/>

<!-- 也可以用空格分隔（每个数值用空格，坐标对之间用空格） -->
<Path Data="M 10 10 L 100 10"/>
```

### 3. Shape 不支持 ClipToBounds

**问题**：Shape 的渲染区域可能超出其 Bounds。

```xml
<!-- 解决：在父容器上设置 ClipToBounds -->
<Border ClipToBounds="True">
    <Path Data="..." Fill="Red"/>
</Border>
```

### 4. 渐变画刷的方向错误

**问题**：渐变方向与预期不符。

```xml
<!-- StartPoint 和 EndPoint 使用百分比 -->
<!-- 0%,0% = 左上角，100%,100% = 右下角 -->
<LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%"/>
```

### 5. PathIcon 的 Data 属性为空

**问题**：PathIcon 没有设置 Data 属性，不显示任何内容。

```xml
<!-- 错误：没有 Data -->
<PathIcon Width="16" Height="16"/>

<!-- 正确：设置 Data -->
<PathIcon Data="M12,2A10,10 0 0,0 2,12..." Width="16" Height="16"/>
```

### 6. StrokeDashArray 的值格式错误

**问题**：StrokeDashArray 使用了错误的分隔符。

```xml
<!-- 错误：使用分号 -->
<Rectangle StrokeDashArray="4;2"/>

<!-- 正确：使用逗号 -->
<Rectangle StrokeDashArray="4,2"/>
```

### 7. Geometry.Parse 失败

**问题**：Path Mini Language 字符串格式错误导致解析失败。

```csharp
// 解决：使用 try-catch
try
{
    var geometry = Geometry.Parse(pathData);
}
catch (Exception ex)
{
    // 处理解析错误
}
```

### 8. ImageBrush 的图像路径错误

**问题**：avares:// 路径大小写敏感。

```xml
<!-- 错误 -->
<ImageBrush Source="avares://MyApp/assets/logo.PNG"/>

<!-- 正确 -->
<ImageBrush Source="avares://MyApp/Assets/Logo.png"/>
```

### 9. Shape 的 Stretch 属性理解错误

**问题**：Stretch 属性影响 Shape 如何填充其容器。

```xml
<!-- Stretch.None：按原始大小显示 -->
<Rectangle Stretch="None" Width="50" Height="50"/>

<!-- Stretch.Fill：拉伸填充容器 -->
<Rectangle Stretch="Fill"/>

<!-- Stretch.Uniform：保持比例适应容器 -->
<Rectangle Stretch="Uniform"/>

<!-- Stretch.UniformToFill：保持比例填充容器（可能裁剪） -->
<Rectangle Stretch="UniformToFill"/>
```

### 10. 径向渐变的 Center 属性

**问题**：Center 使用绝对像素而不是百分比。

```xml
<!-- 错误：使用像素 -->
<RadialGradientBrush Center="50,50"/>

<!-- 正确：使用百分比 -->
<RadialGradientBrush Center="50%,50%"/>
```

## Try It Yourself

### 练习 1：绘制简单图标

使用 Path Mini Language 绘制一个简单的图标（如房子、星星、箭头）。

**提示**：从简单的形状开始，组合 M、L、Z 命令。

### 练习 2：创建渐变按钮

创建一个带有线性渐变背景的按钮，悬停时渐变颜色变化。

**提示**：使用 LinearGradientBrush + 样式动画。

### 练习 3：实现柱状图

使用 Rectangle 控件绘制一个简单的柱状图。

**提示**：使用 Canvas 布局，绑定数据到 Rectangle.Height。

### 练习 4：创建自定义进度环

使用 Path 的弧线命令创建一个环形进度条。

**提示**：使用 A 命令绘制弧线，通过 StrokeDashArray 控制进度。

### 练习 5：实现路径动画

创建一个动画，让一个点沿着 Path 定义的路径移动。

**提示**：使用 DispatcherTimer 更新点的位置。

### 练习 6：创建图标按钮

使用 PathIcon 创建一组带图标的按钮。

**提示**：定义 Geometry 资源，使用 PathIcon 引用。

### 练习 7：实现数据可视化

使用 Shape 控件创建一个简单的饼图。

**提示**：使用 Ellipse + StrokeDashArray 模拟饼图扇区。

### 练习 8：CodexSwitch 风格的主题色板

模仿 CodexSwitch 的主题色板，定义一套完整的 Brush 资源。

**提示**：使用 SolidColorBrush 定义主色、辅色、状态色等。
