# 35. SVG 与矢量图形

> **写给零基础的你**：SVG 就像"用数学公式描述的图片"——不管放大多少倍都不会模糊。位图（PNG/JPG）记录的是每个像素的颜色，放大后就变成马赛克；矢量图记录的是"从 A 点画一条曲线到 B 点，填充红色"这样的指令，无论缩放到多大，渲染引擎都会重新计算，始终保持清晰。

## 35.1 概述

### 35.1.1 位图 vs 矢量图

| 特性 | 位图（Raster） | 矢量图（Vector） |
|------|--------------|----------------|
| 存储方式 | 像素网格（每个像素的颜色值） | 数学指令（路径、曲线、变换） |
| 缩放 | 放大模糊、缩小浪费 | 无限缩放，始终清晰 |
| 文件大小 | 与分辨率成正比 | 与复杂度成正比 |
| 适用场景 | 照片、复杂纹理 | 图标、插图、图表、UI 元素 |
| 典型格式 | PNG、JPG、BMP、WebP | SVG、PDF、AI、EMF |
| 可编辑性 | 差（需像素级编辑） | 好（可修改路径、颜色、变换） |

### 35.1.2 SVG 的优势

- **缩放不失真**：从 16x16 的图标到 4K 屏幕的插图，同一份 SVG 文件完美适配
- **文件体积小**：一个简单的图标 SVG 通常只有几百字节，远小于等效的 PNG
- **可编程**：颜色、大小、形状都可以通过代码动态修改
- **可动画**：SVG 原生支持 SMIL 动画，在 Avalonia 中也可以通过属性动画实现
- **可访问**：SVG 内部是结构化的 XML，包含语义信息，有利于辅助技术

### 35.1.3 Avalonia 中 SVG 的应用场景

| 场景 | 说明 | 推荐方案 |
|------|------|---------|
| 图标系统 | 导航图标、状态图标、操作按钮图标 | PathIcon + Geometry 资源 |
| 品牌标识 | Logo、品牌图形 | Svg 控件加载 SVG 文件 |
| 数据可视化 | 折线图、柱状图、饼图 | Path + Geometry 编程绘制 |
| 装饰元素 | 背景图案、分隔线、边框 | Shape 控件 + Brush |
| 动画图形 | 加载动画、过渡效果 | Path 动画 + 渐变动画 |
| 自定义控件外观 | 滑块手柄、进度指示器 | StreamGeometry 自定义绘制 |

### 35.1.4 Avalonia 中处理 SVG 的三种方式

```
方式一：PathIcon + Path Mini Language（推荐用于图标）
  -> 轻量、支持 Foreground 着色、性能好

方式二：Avalonia.Svg.Skia / Svg.Skia.Avalonia（推荐用于复杂 SVG）
  -> 加载完整 SVG 文件、支持大部分 SVG 规范

方式三：Geometry 编程绘制（推荐用于动态图形）
  -> C# 代码构建路径、最高灵活性
```

## 35.2 SVG 基础

### 35.2.1 SVG 文件结构

一个标准的 SVG 文件本质上是 XML：

```xml
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg"
     viewBox="0 0 24 24"
     width="24" height="24">
  <!-- 这里是图形内容 -->
  <circle cx="12" cy="12" r="10" fill="#3B82F6" stroke="#1E40AF" stroke-width="1"/>
  <path d="M9 12l2 2 4-4" stroke="white" stroke-width="2" fill="none"
        stroke-linecap="round" stroke-linejoin="round"/>
</svg>
```

**关键属性说明：**

| 属性 | 说明 | 示例 |
|------|------|------|
| `xmlns` | SVG 命名空间 | `http://www.w3.org/2000/svg` |
| `viewBox` | 坐标系定义（minX minY width height） | `0 0 24 24` |
| `width` / `height` | 逻辑尺寸 | `24`、`24` |
| `fill` | 填充颜色 | `#3B82F6`、`red`、`none` |
| `stroke` | 描边颜色 | `white` |
| `stroke-width` | 描边宽度 | `2` |
| `stroke-linecap` | 线端样式 | `round`、`butt`、`square` |
| `stroke-linejoin` | 连接样式 | `round`、`miter`、`bevel` |

### 35.2.2 常用 SVG 元素

#### rect — 矩形

```xml
<rect x="10" y="10" width="80" height="60" rx="8" ry="8"
      fill="#E5E7EB" stroke="#9CA3AF" stroke-width="1"/>
```

- `x`, `y`：左上角坐标
- `width`, `height`：宽高
- `rx`, `ry`：圆角半径

#### circle — 圆形

```xml
<circle cx="50" cy="50" r="40" fill="#EF4444"/>
```

- `cx`, `cy`：圆心坐标
- `r`：半径

#### ellipse — 椭圆

```xml
<ellipse cx="100" cy="50" rx="80" ry="40" fill="#8B5CF6"/>
```

#### line — 线段

```xml
<line x1="10" y1="10" x2="190" y2="90" stroke="#374151" stroke-width="2"/>
```

#### polyline — 折线（不闭合）

```xml
<polyline points="10,90 40,40 70,80 100,20 130,60 160,30 190,70"
          fill="none" stroke="#10B981" stroke-width="2"/>
```

#### polygon — 多边形（自动闭合）

```xml
<polygon points="100,10 190,80 160,180 40,180 10,80"
         fill="#F59E0B" stroke="#D97706" stroke-width="2"/>
```

#### path — 万能路径

```xml
<path d="M10,80 C40,10 65,10 95,80 S150,150 180,80"
      fill="none" stroke="#EC4899" stroke-width="3"/>
```

`path` 是 SVG 中最强大的元素，所有其他形状都可以用 `path` 的 `d` 属性（Path Mini Language）来描述。

#### text — 文本

```svg
<text x="10" y="30" font-family="Arial" font-size="20" fill="#1F2937">
  Hello SVG
</text>
```

### 35.2.3 viewBox 与坐标系

`viewBox` 定义了 SVG 的内部坐标系，它决定了"画布"的大小和原点位置：

```xml
<!-- viewBox="minX minY width height" -->
<svg viewBox="0 0 100 100" width="200" height="200">
  <!-- 内部坐标 0~100，渲染到 200x200 的区域 -->
  <rect x="0" y="0" width="100" height="100" fill="#DBEAFE"/>
  <circle cx="50" cy="50" r="40" fill="#3B82F6"/>
</svg>
```

**viewBox 的作用：**

```xml
<!-- 默认坐标系：左上角为原点，向右为 X 正方向，向下为 Y 正方向 -->
<svg viewBox="0 0 200 200">...</svg>

<!-- 移动原点到 (10, 10) -->
<svg viewBox="10 10 200 200">...</svg>

<!-- "zoom in"：viewBox 越小，内容显示越大 -->
<svg viewBox="50 50 100 100">...</svg>

<!-- "zoom out"：viewBox 越大，内容显示越小 -->
<svg viewBox="0 0 400 400">...</svg>
```

### 35.2.4 SVG 变换

SVG 支持 `transform` 属性进行几何变换：

```xml
<!-- 平移 -->
<rect transform="translate(50, 30)" width="40" height="40" fill="#3B82F6"/>

<!-- 旋转（角度, 旋转中心X, 旋转中心Y） -->
<rect transform="rotate(45, 50, 50)" x="30" y="30" width="40" height="40"
      fill="#EF4444"/>

<!-- 缩放 -->
<circle transform="scale(1.5)" cx="30" cy="30" r="20" fill="#10B981"/>

<!-- 倾斜 -->
<rect transform="skewX(20)" x="50" y="20" width="40" height="40"
      fill="#F59E0B"/>

<!-- 组合变换（从右到左依次应用） -->
<rect transform="translate(100, 50) rotate(30) scale(0.8)"
      x="-20" y="-20" width="40" height="40" fill="#8B5CF6"/>
```

## 35.3 在 Avalonia 中使用 SVG

### 35.3.1 方式一：PathIcon + Geometry（推荐用于图标）

这是 Avalonia 中最轻量、性能最好的 SVG 使用方式，特别适合图标系统。

**原理**：将 SVG 的 `<path d="...">` 中的 `d` 属性值提取出来，作为 Geometry 使用。

```xml
<!-- 定义图标资源 -->
<Application.Resources>
    <Geometry x:Key="Icon.Home">M3,12 L12,3 L21,12 V21 H15 V15 H9 V21 H3 Z</Geometry>
    <Geometry x:Key="Icon.Settings">M12,15.5 A3.5,3.5 0 0,1 8.5,12
        A3.5,3.5 0 0,1 12,8.5 A3.5,3.5 0 0,1 15.5,12
        A3.5,3.5 0 0,1 12,15.5 Z</Geometry>
    <Geometry x:Key="Icon.Search">M15.5,14 H14.71 L14.43,13.73
        C15.41,12.59 16,11.11 16,9.5 C16,5.91 13.09,3 9.5,3
        S3,5.91 3,9.5 S5.91,16 9.5,16 C11.11,16 12.59,15.41
        13.73,14.43 L14,14.71 V15.5 L19,20.49 L20.49,19
        L15.5,14 Z M9.5,14 C7.01,14 5,11.99 5,9.5 S7.01,5
        9.5,5 S14,7.01 14,9.5 S11.99,14 9.5,14 Z</Geometry>
</Application.Resources>
```

**使用 PathIcon：**

```xml
<PathIcon Data="{StaticResource Icon.Home}"
          Width="24" Height="24"
          Foreground="{DynamicResource TextPrimaryBrush}"/>
```

**PathIcon 的优势**：`Foreground` 属性可以直接控制图标颜色，非常适合主题切换。

### 35.3.2 方式二：Avalonia.Svg.Skia（推荐用于复杂 SVG）

对于完整的 SVG 文件（包含渐变、滤镜、多路径等），使用 `Svg.Skia.Avalonia` 包。

**安装：**

```shell
dotnet add package Svg.Skia.Avalonia
```

**XAML 使用：**

```xml
<Window xmlns:svg="clr-namespace:Svg.Controls.Skia.Avalonia;assembly=Svg.Skia.Avalonia">

    <!-- 从文件路径加载 -->
    <svg:Svg Path="/Assets/illustration.svg"
             Width="400" Height="300"
             Stretch="Uniform"/>

    <!-- 从 avares:// 资源加载 -->
    <svg:Svg Path="avares://MyApp/Assets/brand-logo.svg"
             Width="200" Height="60"
             Stretch="Uniform"/>

</Window>
```

**Svg 控件的关键属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Path` | string | SVG 文件路径，支持 `avares://` 和本地路径 |
| `Stretch` | Stretch | 拉伸模式（None/Fill/Uniform/UniformToFill） |
| `StretchDirection` | StretchDirection | 拉伸方向（Both/DownOnly/UpOnly） |

**从 Stream 加载：**

```csharp
using Avalonia.Platform.Storage;
using Svg.Skia;

public class SvgLoader
{
    public static SKSvg? LoadSvgFromStream(Stream stream)
    {
        var svg = new SKSvg();
        svg.Load(stream);
        return svg;
    }

    public static async Task<SKSvg?> LoadSvgFromFile(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        return LoadSvgFromStream(stream);
    }
}
```

### 35.3.3 方式三：从字符串加载 SVG

有时 SVG 内容来自网络 API 或数据库，需要从字符串加载：

```csharp
using Avalonia.Media;
using Svg.Skia;

public class SvgService
{
    /// <summary>
    /// 从 SVG 字符串创建 DrawingImage
    /// </summary>
    public static DrawingImage? CreateFromSvgString(string svgContent)
    {
        var svg = new SKSvg();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent));
        svg.Load(stream);

        // 将 SKSvg 转换为 Avalonia 可用的图像
        // 实际实现需要通过 SkiaSharp 渲染到 WriteableBitmap
        return null; // 简化示例
    }
}
```

**通过 HttpClient 获取远程 SVG：**

```csharp
public class RemoteSvgService
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> FetchSvgAsync(string url)
    {
        var response = await _httpClient.GetStringAsync(url);
        return response;
    }

    public async Task<Stream> FetchSvgStreamAsync(string url)
    {
        var response = await _httpClient.GetStreamAsync(url);
        var memoryStream = new MemoryStream();
        await response.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }
}
```

### 35.3.4 动态修改 SVG 颜色

**使用 PathIcon 的 Foreground：**

```xml
<!-- 通过样式类切换颜色 -->
<PathIcon Data="{StaticResource Icon.Heart}" Width="20" Height="20"
          Classes.red="{Binding IsLiked}">
    <PathIcon.Styles>
        <Style Selector="PathIcon">
            <Setter Property="Foreground" Value="{DynamicResource TextSecondaryBrush}"/>
        </Style>
        <Style Selector="PathIcon.red">
            <Setter Property="Foreground" Value="#EF4444"/>
        </Style>
    </PathIcon.Styles>
</PathIcon>
```

**通过 C# 代码替换 Geometry 路径：**

```csharp
public class DynamicIconViewModel : ViewModelBase
{
    private Geometry? _iconData;
    public Geometry? IconData
    {
        get => _iconData;
        set => this.RaiseAndSetIfChanged(ref _iconData, value);
    }

    public void SetIcon(string pathData)
    {
        IconData = Geometry.Parse(pathData);
    }

    public void ToggleIcon(bool isExpanded)
    {
        // 展开/收起使用不同的箭头图标
        IconData = isExpanded
            ? Geometry.Parse("M6,9L12,15L18,9")    // 向下箭头
            : Geometry.Parse("M9,6L15,12L9,18");   // 向右箭头
    }
}
```

### 35.3.5 SVG 文件作为 AvaloniaResource

确保 SVG 文件的生成操作设置正确：

```xml
<!-- .csproj 中 -->
<ItemGroup>
    <!-- SVG 文件作为嵌入资源 -->
    <AvaloniaResource Include="Assets\**\*.svg" />
</ItemGroup>
```

```xml
<!-- 目录结构 -->
<!-- MyApp/Assets/Icons/home.svg -->
<!-- MyApp/Assets/Illustrations/welcome.svg -->

<!-- 引用方式 -->
<svg:Svg Path="avares://MyApp/Assets/Icons/home.svg" Width="24" Height="24"/>
```

## 35.4 Path Mini Language 详解

Path Mini Language 是一种紧凑的路径描述语法，用于 `Path.Data` 属性或 SVG 的 `d` 属性。它是所有矢量绘图的基础。

### 35.4.1 命令一览表

| 命令 | 名称 | 参数 | 说明 |
|------|------|------|------|
| `M` / `m` | MoveTo | x, y | 移动画笔到指定点（不绘制） |
| `L` / `l` | LineTo | x, y | 画直线到指定点 |
| `H` / `h` | HorizontalLineTo | x | 画水平线到指定 X |
| `V` / `v` | VerticalLineTo | y | 画垂直线到指定 Y |
| `C` / `c` | CubicBezier | x1,y1 x2,y2 x,y | 三次贝塞尔曲线 |
| `S` / `s` | SmoothCubicBezier | x2,y2 x,y | 平滑三次贝塞尔（自动对称第一个控制点） |
| `Q` / `q` | QuadraticBezier | x1,y1 x,y | 二次贝塞尔曲线 |
| `T` / `t` | SmoothQuadraticBezier | x,y | 平滑二次贝塞尔（自动对称控制点） |
| `A` / `a` | Arc | rx,ry rotation large-arc sweep x,y | 椭圆弧 |
| `Z` / `z` | ClosePath | （无） | 闭合路径（回到起点画直线） |

**大写 = 绝对坐标，小写 = 相对坐标（相对于当前点）。**

### 35.4.2 基础命令示例

#### M（MoveTo）和 L（LineTo）

```xml
<!-- 三角形 -->
<Path Stroke="#3B82F6" StrokeThickness="2" Fill="#DBEAFE"
      Data="M 100,10 L 190,180 L 10,180 Z"/>

<!-- 等价写法（省略空格和逗号） -->
<Path Data="M100,10 190,180 10,180Z" Stroke="#3B82F6" StrokeThickness="2"/>
```

#### H（水平线）和 V（垂直线）

```xml
<!-- 用 H 和 V 画矩形 -->
<Path Stroke="#10B981" StrokeThickness="2" Fill="#D1FAE5"
      Data="M 10,10 H 190 V 90 H 10 Z"/>

<!-- 等价于 -->
<!-- M 10,10 L 190,10 L 190,90 L 10,90 Z -->
```

#### 相对坐标（小写命令）

```xml
<!-- 绝对坐标 -->
<Path Data="M 10,10 L 100,10 L 100,100 L 10,100 Z"/>

<!-- 相对坐标（从上一个点开始计算偏移） -->
<Path Data="M 10,10 l 90,0 l 0,90 l -90,0 Z"/>
```

### 35.4.3 贝塞尔曲线详解

#### C（三次贝塞尔曲线）

三次贝塞尔曲线有两个控制点，可以创建更复杂的曲线：

```
C x1,y1  x2,y2  x,y
  |       |       |
  控制点1  控制点2  终点
```

```xml
<!-- S 形曲线 -->
<Path Stroke="#EC4899" StrokeThickness="3" Fill="none"
      Data="M 10,80 C 40,10 65,10 95,80"/>

<!-- 心形曲线 -->
<Path Fill="#EF4444"
      Data="M 100,30
            C 100,30 70,0 50,30
            C 30,60 50,100 100,150
            C 150,100 170,60 150,30
            C 130,0 100,30 100,30 Z"/>

<!-- 复杂的波浪线 -->
<Path Stroke="#8B5CF6" StrokeThickness="2" Fill="none"
      Data="M 10,50 C 30,10 50,90 70,50 C 90,10 110,90 130,50
            C 150,10 170,90 190,50"/>
```

#### S（平滑三次贝塞尔）

`S` 命令自动将前一个控制点关于当前点的对称点作为第一个控制点，适合连接多段曲线：

```xml
<!-- S 形曲线：S 自动对称 C 的第二个控制点 -->
<Path Stroke="#F59E0B" StrokeThickness="3" Fill="none"
      Data="M 10,80 C 40,10 65,10 95,80 S 150,150 180,80"/>
<!-- S 150,150 180,80 等价于 C 125,150 150,150 180,80 -->
```

#### Q（二次贝塞尔曲线）

二次贝塞尔只有一个控制点，比三次贝塞尔简单但灵活度低：

```
Q x1,y1  x,y
  |       |
  控制点   终点
```

```xml
<!-- 抛物线弧 -->
<Path Stroke="#10B981" StrokeThickness="2" Fill="none"
      Data="M 10,80 Q 50,10 90,80"/>

<!-- 箭头形状 -->
<Path Fill="#3B82F6"
      Data="M 10,50 Q 50,10 90,50 Q 50,10 50,10 Q 50,10 10,50 Z"/>
```

#### T（平滑二次贝塞尔）

```xml
<!-- T 自动对称前一个 Q 的控制点 -->
<Path Stroke="#EC4899" StrokeThickness="2" Fill="none"
      Data="M 10,80 Q 50,10 90,80 T 170,80"/>
```

### 35.4.4 弧线命令详解（A 命令）

弧线命令是 Path Mini Language 中最复杂的，它有 7 个参数：

```
A rx, ry  x-rotation  large-arc-flag  sweep-flag  x, y
  |         |            |               |          |
  椭圆半径   X轴旋转角度   大弧/小弧标志     方向标志    终点坐标
```

**参数说明：**

| 参数 | 说明 | 取值 |
|------|------|------|
| `rx` | 椭圆 X 方向半径 | 正数 |
| `ry` | 椭圆 Y 方向半径 | 正数 |
| `x-rotation` | 椭圆 X 轴旋转角度（度） | 通常为 0 |
| `large-arc-flag` | 0 = 小弧（<180度），1 = 大弧（>=180度） | 0 或 1 |
| `sweep-flag` | 0 = 逆时针，1 = 顺时针 | 0 或 1 |
| `x, y` | 弧线终点坐标 | 坐标值 |

```xml
<!-- 圆弧示例：四种组合 -->
<!-- sweep=0 (逆时针), large=0 (小弧) -->
<Path Stroke="#3B82F6" StrokeThickness="2" Fill="none"
      Data="M 80,80 A 40,40 0 0,0 160,80"/>

<!-- sweep=1 (顺时针), large=0 (小弧) -->
<Path Stroke="#10B981" StrokeThickness="2" Fill="none"
      Data="M 80,80 A 40,40 0 0,1 160,80"/>

<!-- sweep=0 (逆时针), large=1 (大弧) -->
<Path Stroke="#EF4444" StrokeThickness="2" Fill="none"
      Data="M 80,80 A 40,40 0 1,0 160,80"/>

<!-- sweep=1 (顺时针), large=1 (大弧) -->
<Path Stroke="#F59E0B" StrokeThickness="2" Fill="none"
      Data="M 80,80 A 40,40 0 1,1 160,80"/>
```

**用弧线画圆：**

```xml
<!-- 圆（需要两段半圆弧拼接） -->
<Path Fill="#DBEAFE" Stroke="#3B82F6" StrokeThickness="2"
      Data="M 100,20 A 40,40 0 1,1 100,100 A 40,40 0 1,1 100,20 Z"/>

<!-- 半圆 -->
<Path Fill="#D1FAE5" Stroke="#10B981" StrokeThickness="2"
      Data="M 60,80 A 40,40 0 0,1 140,80 L 140,80 L 60,80 Z"/>

<!-- 环形进度条（270度弧线） -->
<Path Stroke="#3B82F6" StrokeThickness="6" StrokeLineCap="Round" Fill="none"
      Data="M 100,20 A 40,40 0 1,1 37.6,132.4"/>
```

### 35.4.5 常见路径模式

#### 圆角矩形

```xml
<!-- 圆角矩形（rx=ry=12） -->
<Path Fill="#F3F4F6" Stroke="#9CA3AF" StrokeThickness="1"
      Data="M 12,0 H 188 A 12,12 0 0,1 200,12
            V 88 A 12,12 0 0,1 188,100
            H 12 A 12,12 0 0,1 0,88
            V 12 A 12,12 0 0,1 12,0 Z"/>
```

#### 五角星

```xml
<Path Fill="#F59E0B" Stroke="#D97706" StrokeThickness="1"
      Data="M 100,10 L 122,70 L 188,78 L 136,124
            L 150,190 L 100,158 L 50,190 L 64,124
            L 12,78 L 78,70 Z"/>
```

#### 箭头

```xml
<!-- 右箭头 -->
<Path Fill="#3B82F6"
      Data="M 0,30 L 60,30 L 60,10 L 100,50 L 60,90 L 60,70 L 0,70 Z"/>

<!-- 带圆角的箭头 -->
<Path Fill="#10B981"
      Data="M 10,35 Q 10,25 20,25 L 55,25 L 55,10
            Q 55,5 60,5 L 95,50 L 60,95
            Q 55,95 55,90 L 55,75 L 20,75
            Q 10,75 10,65 Z"/>
```

#### 齿轮

```xml
<!-- 简化的齿轮（8 个齿） -->
<Path Fill="#6B7280" Stroke="#374151" StrokeThickness="1"
      Data="M 100,15 L 108,15 L 112,30 L 120,27 L 127,17
            L 135,22 L 130,35 L 138,42 L 150,38 L 153,47
            L 142,55 L 147,64 L 160,65 L 158,75
            L 145,72 L 138,80 L 147,90 L 140,95
            L 130,85 L 122,90 L 125,103 L 116,103
            L 110,90 L 100,93 L 95,105 L 86,100
            L 88,88 L 80,80 L 68,86 L 65,77
            L 75,70 L 70,60 L 57,58 L 60,49
            L 72,52 L 75,42 L 65,33 L 72,27
            L 82,35 L 90,28 L 87,15 Z"/>
```

#### 对勾（Checkmark）

```xml
<Path Stroke="#10B981" StrokeThickness="3" Fill="none"
      StrokeLineCap="Round" StrokeLineJoin="Round"
      Data="M 4,12 L 9,17 L 20,6"/>
```

#### X 号

```xml
<Path Stroke="#EF4444" StrokeThickness="3" Fill="none"
      StrokeLineCap="Round"
      Data="M 6,6 L 18,18 M 18,6 L 6,18"/>
```

#### 设置齿轮（Lucide 风格）

```xml
<Path Stroke="currentColor" StrokeWidth="2" Fill="none"
      StrokeLineCap="Round" StrokeLineJoin="Round"
      Data="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25
            a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38
            a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51
            a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38
            a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25
            a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44
            a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25
            a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39
            a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5
            a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38
            a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25
            a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z
            M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z"/>
```

### 35.4.6 Path Mini Language 调试技巧

**技巧一：逐段绘制**

```xml
<!-- 先画出每个点的位置 -->
<Path Stroke="Red" StrokeThickness="4" StrokeLineCap="Round" Fill="none"
      Data="M 10,50 L 50,10 L 90,50 L 130,10 L 170,50"/>
<!-- 确认点的位置正确后，再添加曲线 -->
```

**技巧二：使用 Geometry.Parse 验证**

```csharp
try
{
    var geometry = Geometry.Parse("M 10,50 C 40,10 65,10 95,80");
    Console.WriteLine($"Bounds: {geometry.Bounds}");
}
catch (Exception ex)
{
    Console.WriteLine($"解析错误: {ex.Message}");
}
```

**技巧三：可视化控制点**

```xml
<!-- 在 Canvas 上同时绘制曲线和控制点 -->
<Canvas Width="200" Height="200">
    <!-- 实际曲线 -->
    <Path Stroke="#3B82F6" StrokeThickness="2" Fill="none"
          Data="M 20,150 C 60,20 140,20 180,150"/>
    <!-- 控制点连线 -->
    <Line StartPoint="20,150" EndPoint="60,20" Stroke="#D1D5DB" StrokeThickness="1"
          StrokeDashArray="4,2"/>
    <Line StartPoint="180,150" EndPoint="140,20" Stroke="#D1D5DB" StrokeThickness="1"
          StrokeDashArray="4,2"/>
    <!-- 控制点 -->
    <Ellipse Width="8" Height="8" Fill="#EF4444" Canvas.Left="56" Canvas.Top="16"/>
    <Ellipse Width="8" Height="8" Fill="#EF4444" Canvas.Left="136" Canvas.Top="16"/>
    <!-- 起点和终点 -->
    <Ellipse Width="8" Height="8" Fill="#10B981" Canvas.Left="16" Canvas.Top="146"/>
    <Ellipse Width="8" Height="8" Fill="#10B981" Canvas.Left="176" Canvas.Top="146"/>
</Canvas>
```

## 35.5 Geometry 对象

Geometry 是 Avalonia 中描述形状的轻量对象，它只包含几何信息，不包含视觉属性（颜色、线宽等）。Shape 控件使用 Geometry 来定义形状。

### 35.5.1 PathGeometry

PathGeometry 是最灵活的 Geometry，由 PathFigure 和 PathSegment 组成：

```xml
<Path Stroke="#3B82F6" StrokeThickness="2" Fill="#DBEAFE">
    <Path.Data>
        <PathGeometry>
            <PathFigure StartPoint="10,100" IsClosed="True">
                <LineSegment Point="50,10"/>
                <LineSegment Point="100,60"/>
                <LineSegment Point="150,10"/>
                <LineSegment Point="190,100"/>
            </PathFigure>
        </PathGeometry>
    </Path.Data>
</Path>
```

**PathSegment 类型：**

| 类型 | 说明 | 关键属性 |
|------|------|---------|
| `LineSegment` | 直线段 | Point |
| `ArcSegment` | 弧线 | Point, Size, RotationAngle, IsLargeArc, SweepDirection |
| `BezierSegment` | 三次贝塞尔 | Point1, Point2, Point3 |
| `QuadraticBezierSegment` | 二次贝塞尔 | Point1, Point2 |
| `PolyLineSegment` | 多段折线 | Points |
| `PolyBezierSegment` | 多段三次贝塞尔 | Points |
| `PolyQuadraticBezierSegment` | 多段二次贝塞尔 | Points |

**组合多个 PathFigure：**

```xml
<Path Stroke="#EF4444" StrokeThickness="2" Fill="none">
    <Path.Data>
        <PathGeometry>
            <!-- 第一个图形：三角形 -->
            <PathFigure StartPoint="20,80" IsClosed="True">
                <LineSegment Point="50,20"/>
                <LineSegment Point="80,80"/>
            </PathFigure>
            <!-- 第二个图形：圆弧 -->
            <PathFigure StartPoint="100,80">
                <ArcSegment Point="180,80" Size="40,40"
                            IsLargeArc="True" SweepDirection="Clockwise"/>
            </PathFigure>
        </PathGeometry>
    </Path.Data>
</Path>
```

**C# 代码中创建 PathGeometry：**

```csharp
var geometry = new PathGeometry();
var figure = new PathFigure
{
    StartPoint = new Point(10, 100),
    IsClosed = true
};
figure.Segments.Add(new LineSegment(new Point(50, 10)));
figure.Segments.Add(new BezierSegment(
    new Point(80, 10),    // 控制点 1
    new Point(100, 80),   // 控制点 2
    new Point(130, 100)   // 终点
));
figure.Segments.Add(new ArcSegment(
    new Point(190, 50),   // 终点
    new Size(30, 30),     // 半径
    0,                    // 旋转角度
    false,                // 是否大弧
    SweepDirection.Clockwise
));
geometry.Figures.Add(figure);
```

### 35.5.2 StreamGeometry（高性能）

StreamGeometry 是只写的、不可修改的 Geometry，性能比 PathGeometry 更高，适合静态图形：

```csharp
var geometry = new StreamGeometry();
using (var context = geometry.Open())
{
    context.BeginFigure(new Point(10, 100), isFilled: true);
    context.LineTo(new Point(50, 10));
    context.LineTo(new Point(100, 60));
    context.LineTo(new Point(150, 10));
    context.LineTo(new Point(190, 100));
    context.EndFigure(isClosed: true);
}
```

**StreamGeometry vs PathGeometry：**

| 特性 | PathGeometry | StreamGeometry |
|------|-------------|----------------|
| 可修改 | 是（可增删改 Figure/Segment） | 否（创建后不可修改） |
| 性能 | 较低 | 较高（内存占用少） |
| 适用场景 | 动态变化的图形 | 静态图标、自定义控件 |
| XAML 支持 | 完整 | 需要代码创建 |

**在自定义控件中使用 StreamGeometry 缓存图标：**

```csharp
public class CachedIconControl : Control
{
    private StreamGeometry? _iconGeometry;

    public static readonly StyledProperty<string> IconDataProperty =
        AvaloniaProperty.Register<CachedIconControl, string>(nameof(IconData));

    public string IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconDataProperty)
        {
            _iconGeometry = null; // 清除缓存，下次渲染时重建
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        if (_iconGeometry is null && !string.IsNullOrEmpty(IconData))
        {
            // 将 Path Mini Language 转换为 StreamGeometry
            var parsed = Geometry.Parse(IconData);
            // 注意：这里简化处理，实际可能需要更复杂的转换
            _iconGeometry = new StreamGeometry();
            using var ctx = _iconGeometry.Open();
            // ... 复制路径数据
        }

        if (_iconGeometry is not null)
        {
            context.DrawGeometry(Foreground, null, _iconGeometry);
        }
    }
}
```

### 35.5.3 基本 Geometry 类型

```xml
<!-- RectangleGeometry -->
<Path Fill="#DBEAFE">
    <Path.Data>
        <RectangleGeometry Rect="10,10 180,80" RadiusX="10" RadiusY="10"/>
    </Path.Data>
</Path>

<!-- EllipseGeometry -->
<Path Fill="#D1FAE5">
    <Path.Data>
        <EllipseGeometry Center="100,50" RadiusX="80" RadiusY="40"/>
    </Path.Data>
</Path>

<!-- LineGeometry -->
<Path Stroke="#EF4444" StrokeThickness="2">
    <Path.Data>
        <LineGeometry StartPoint="10,10" EndPoint="190,90"/>
    </Path.Data>
</Path>
```

**C# 中创建基本 Geometry：**

```csharp
var rect = new RectangleGeometry(new Rect(10, 10, 180, 80), 10, 10);
var ellipse = new EllipseGeometry(new Rect(20, 10, 160, 80));
var line = new LineGeometry(new Point(10, 10), new Point(190, 90));
```

### 35.5.4 GeometryGroup（组合几何体）

GeometryGroup 将多个 Geometry 组合在一起，使用 FillRule 决定填充方式：

```xml
<!-- 同心圆环（EvenOdd 填充规则） -->
<Path Fill="#3B82F6" FillRule="EvenOdd">
    <Path.Data>
        <GeometryGroup>
            <EllipseGeometry Center="100,50" RadiusX="45" RadiusY="45"/>
            <EllipseGeometry Center="100,50" RadiusX="30" RadiusY="30"/>
            <EllipseGeometry Center="100,50" RadiusX="15" RadiusY="15"/>
        </GeometryGroup>
    </Path.Data>
</Path>
```

```xml
<!-- 镂空效果：外框减去内框 -->
<Path Fill="#10B981">
    <Path.Data>
        <GeometryGroup FillRule="EvenOdd">
            <RectangleGeometry Rect="10,10 180,80"/>
            <RectangleGeometry Rect="40,25 120,50"/>
        </GeometryGroup>
    </Path.Data>
</Path>
```

### 35.5.5 CombinedGeometry（布尔运算）

CombinedGeometry 支持对两个 Geometry 进行布尔运算：

```xml
<!-- 并集 (Union) -->
<Path Fill="#3B82F6">
    <Path.Data>
        <CombinedGeometry GeometryCombineMode="Union">
            <CombinedGeometry.Geometry1>
                <EllipseGeometry Center="70,50" RadiusX="40" RadiusY="40"/>
            </CombinedGeometry.Geometry1>
            <CombinedGeometry.Geometry2>
                <EllipseGeometry Center="130,50" RadiusX="40" RadiusY="40"/>
            </CombinedGeometry.Geometry2>
        </CombinedGeometry>
    </Path.Data>
</Path>

<!-- 交集 (Intersect) -->
<Path Fill="#EF4444">
    <Path.Data>
        <CombinedGeometry GeometryCombineMode="Intersect">
            <CombinedGeometry.Geometry1>
                <EllipseGeometry Center="80,50" RadiusX="40" RadiusY="40"/>
            </CombinedGeometry.Geometry1>
            <CombinedGeometry.Geometry2>
                <EllipseGeometry Center="120,50" RadiusX="40" RadiusY="40"/>
            </CombinedGeometry.Geometry2>
        </CombinedGeometry>
    </Path.Data>
</Path>

<!-- 差集 (Xor) — 镂空效果 -->
<Path Fill="#F59E0B">
    <Path.Data>
        <CombinedGeometry GeometryCombineMode="Xor">
            <CombinedGeometry.Geometry1>
                <EllipseGeometry Center="100,50" RadiusX="45" RadiusY="45"/>
            </CombinedGeometry.Geometry1>
            <CombinedGeometry.Geometry2>
                <EllipseGeometry Center="100,50" RadiusX="25" RadiusY="25"/>
            </CombinedGeometry.Geometry2>
        </CombinedGeometry>
    </Path.Data>
</Path>

<!-- 差集 (Exclude) — 从第一个减去第二个 -->
<Path Fill="#8B5CF6">
    <Path.Data>
        <CombinedGeometry GeometryCombineMode="Exclude">
            <CombinedGeometry.Geometry1>
                <RectangleGeometry Rect="30,10 140,80"/>
            </CombinedGeometry.Geometry1>
            <CombinedGeometry.Geometry2>
                <EllipseGeometry Center="100,50" RadiusX="30" RadiusY="30"/>
            </CombinedGeometry.Geometry2>
        </CombinedGeometry>
    </Path.Data>
</Path>
```

**GeometryCombineMode 枚举值：**

| 值 | 说明 | 图示效果 |
|----|------|---------|
| `Union` | 并集 | 两个形状合并 |
| `Intersect` | 交集 | 只保留重叠部分 |
| `Xor` | 异或 | 保留非重叠部分 |
| `Exclude` | 差集 | 从第一个减去第二个 |

## 35.6 Shape 控件进阶

### 35.6.1 Path 控件

Path 是最强大的 Shape 控件，可以使用 Path Mini Language 或 Geometry 对象：

```xml
<!-- 使用 Path Mini Language 字符串 -->
<Path Data="M 10,50 C 30,10 70,10 90,50 S 150,90 170,50"
      Stroke="#3B82F6" StrokeThickness="3" Fill="none"/>

<!-- 使用 StaticResource -->
<Path Data="{StaticResource StarGeometry}"
      Fill="#F59E0B" Stroke="#D97706" StrokeThickness="1"/>

<!-- 使用嵌套 Geometry 对象 -->
<Path Fill="#D1FAE5" Stroke="#10B981" StrokeThickness="2">
    <Path.Data>
        <PathGeometry>
            <PathFigure StartPoint="10,90" IsClosed="True">
                <QuadraticBezierSegment Point1="50,10" Point2="90,90"/>
            </PathFigure>
        </PathGeometry>
    </Path.Data>
</Path>
```

### 35.6.2 Polygon vs Polyline

```xml
<!-- Polygon：自动闭合 -->
<Polygon Points="50,10 90,90 10,40 90,40 10,90"
         Fill="#DBEAFE" Stroke="#3B82F6" StrokeThickness="2"/>

<!-- Polyline：不闭合（开放路径） -->
<Polyline Points="50,10 90,90 10,40 90,40 10,90"
          Stroke="#EF4444" StrokeThickness="2" Fill="none"/>
```

### 35.6.3 FillRule（EvenOdd vs NonZero）

FillRule 决定了复杂路径的填充方式：

```xml
<!-- EvenOdd：交叉区域不填充 -->
<Path Fill="#3B82F6" FillRule="EvenOdd"
      Data="M 10,10 L 190,10 L 190,90 L 10,90 Z
            M 50,30 L 150,30 L 150,70 L 50,70 Z"/>
<!-- 结果：外框填充，内框不填充（镂空） -->

<!-- NonZero：交叉区域也填充 -->
<Path Fill="#EF4444" FillRule="NonZero"
      Data="M 10,10 L 190,10 L 190,90 L 10,90 Z
            M 50,30 L 150,30 L 150,70 L 50,70 Z"/>
<!-- 结果：整个区域都填充 -->
```

**实际应用：五角星的填充**

```xml
<!-- 使用 NonZero：五角星内部也填充 -->
<Path Fill="#F59E0B" FillRule="NonZero"
      Data="M 100,10 L 122,70 L 188,78 L 136,124 L 150,190
            L 100,158 L 50,190 L 64,124 L 12,78 L 78,70 Z"/>
```

### 35.6.4 Stretch 属性

Shape 的 Stretch 属性控制它如何填充可用空间：

```xml
<!-- Stretch.None：按 Geometry 的原始坐标显示 -->
<Path Data="M 0,0 L 10,0 L 10,10 L 0,10 Z" Stretch="None"
      Fill="#3B82F6" Width="200" Height="200"/>
<!-- 结果：左上角一个很小的 10x10 方块 -->

<!-- Stretch.Fill：拉伸填充整个空间 -->
<Path Data="M 0,0 L 10,0 L 10,10 L 0,10 Z" Stretch="Fill"
      Fill="#10B981" Width="200" Height="200"/>
<!-- 结果：200x200 的方块 -->

<!-- Stretch.Uniform：保持比例适应空间 -->
<Path Data="M 0,0 L 10,0 L 10,10 L 0,10 Z" Stretch="Uniform"
      Fill="#EF4444" Width="200" Height="100"/>
<!-- 结果：100x100 的方块，居中显示 -->

<!-- Stretch.UniformToFill：保持比例填充空间（可能裁剪） -->
<Path Data="M 0,0 L 10,0 L 10,10 L 0,10 Z" Stretch="UniformToFill"
      Fill="#F59E0B" Width="200" Height="100"/>
<!-- 结果：200x200 的方块，上下各裁剪 50 -->
```

### 35.6.5 自定义 Shape

继承 Shape 基类创建自定义形状控件：

```csharp
public class RegularPolygon : Shape
{
    public static readonly StyledProperty<int> SidesProperty =
        AvaloniaProperty.Register<RegularPolygon, int>(nameof(Sides), 5);

    public static readonly StyledProperty<double> InnerRadiusProperty =
        AvaloniaProperty.Register<RegularPolygon, double>(nameof(InnerRadius), 0.5);

    public int Sides
    {
        get => GetValue(SidesProperty);
        set => SetValue(SidesProperty, value);
    }

    public double InnerRadius
    {
        get => GetValue(InnerRadiusProperty);
        set => SetValue(InnerRadiusProperty, value);
    }

    protected override Geometry CreateDefiningGeometry()
    {
        var center = new Point(50, 50);
        var outerRadius = 45.0;
        var innerRadius = outerRadius * InnerRadius;
        var points = new List<Point>();

        for (int i = 0; i < Sides; i++)
        {
            double angle = Math.PI * 2 * i / Sides - Math.PI / 2;
            points.Add(new Point(
                center.X + outerRadius * Math.Cos(angle),
                center.Y + outerRadius * Math.Sin(angle)
            ));

            if (InnerRadius < 1.0)
            {
                double innerAngle = angle + Math.PI / Sides;
                points.Add(new Point(
                    center.X + innerRadius * Math.Cos(innerAngle),
                    center.Y + innerRadius * Math.Sin(innerAngle)
                ));
            }
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], true);
            for (int i = 1; i < points.Count; i++)
            {
                ctx.LineTo(points[i]);
            }
            ctx.EndFigure(true);
        }

        return geometry;
    }
}
```

**使用自定义 Shape：**

```xml
<!-- 五边形 -->
<local:RegularPolygon Sides="5" Width="100" Height="100"
                       Fill="#3B82F6" Stroke="#1E40AF" StrokeThickness="2"/>

<!-- 六边形 -->
<local:RegularPolygon Sides="6" Width="100" Height="100"
                       Fill="#10B981" Stroke="#059669" StrokeThickness="2"/>

<!-- 五角星（星形） -->
<local:RegularPolygon Sides="5" InnerRadius="0.38" Width="100" Height="100"
                       Fill="#F59E0B" Stroke="#D97706" StrokeThickness="2"/>
```

## 35.7 Brush 进阶

### 35.7.1 SolidColorBrush

最简单的画刷，纯色填充：

```xml
<SolidColorBrush Color="#3B82F6"/>
<SolidColorBrush Color="Red" Opacity="0.5"/>
```

### 35.7.2 LinearGradientBrush（线性渐变）

```xml
<!-- 从左到右的渐变 -->
<Rectangle Width="200" Height="100">
    <Rectangle.Fill>
        <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,0%">
            <GradientStop Color="#3B82F6" Offset="0"/>
            <GradientStop Color="#8B5CF6" Offset="1"/>
        </LinearGradientBrush>
    </Rectangle.Fill>
</Rectangle>

<!-- 对角线渐变 -->
<Rectangle Width="200" Height="100">
    <Rectangle.Fill>
        <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
            <GradientStop Color="#EC4899" Offset="0"/>
            <GradientStop Color="#F59E0B" Offset="0.5"/>
            <GradientStop Color="#10B981" Offset="1"/>
        </LinearGradientBrush>
    </Rectangle.Fill>
</Rectangle>

<!-- 多色渐变彩虹 -->
<Rectangle Width="300" Height="50">
    <Rectangle.Fill>
        <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,0%">
            <GradientStop Color="#EF4444" Offset="0"/>
            <GradientStop Color="#F97316" Offset="0.17"/>
            <GradientStop Color="#EAB308" Offset="0.33"/>
            <GradientStop Color="#22C55E" Offset="0.5"/>
            <GradientStop Color="#3B82F6" Offset="0.67"/>
            <GradientStop Color="#6366F1" Offset="0.83"/>
            <GradientStop Color="#A855F7" Offset="1"/>
        </LinearGradientBrush>
    </Rectangle.Fill>
</Rectangle>
```

### 35.7.3 RadialGradientBrush（径向渐变）

```xml
<!-- 基本径向渐变 -->
<Ellipse Width="200" Height="200">
    <Ellipse.Fill>
        <RadialGradientBrush Center="50%,50%" RadiusX="50%" RadiusY="50%">
            <GradientStop Color="#FFFFFF" Offset="0"/>
            <GradientStop Color="#3B82F6" Offset="1"/>
        </RadialGradientBrush>
    </Ellipse.Fill>
</Ellipse>

<!-- 偏心径向渐变（模拟球体光影） -->
<Ellipse Width="150" Height="150">
    <Ellipse.Fill>
        <RadialGradientBrush Center="35%,35%" RadiusX="50%" RadiusY="50%"
                             GradientOrigin="35%,35%">
            <GradientStop Color="#93C5FD" Offset="0"/>
            <GradientStop Color="#3B82F6" Offset="0.5"/>
            <GradientStop Color="#1E3A8A" Offset="1"/>
        </RadialGradientBrush>
    </Ellipse.Fill>
</Ellipse>

<!-- 椭圆径向渐变 -->
<Ellipse Width="200" Height="100">
    <Ellipse.Fill>
        <RadialGradientBrush Center="50%,50%" RadiusX="50%" RadiusY="80%">
            <GradientStop Color="#FDE68A" Offset="0"/>
            <GradientStop Color="#F59E0B" Offset="1"/>
        </RadialGradientBrush>
    </Ellipse.Fill>
</Ellipse>
```

### 35.7.4 ImageBrush

用图像作为画刷填充形状：

```xml
<!-- 用图片填充矩形 -->
<Rectangle Width="200" Height="150">
    <Rectangle.Fill>
        <ImageBrush Source="avares://MyApp/Assets/pattern.png"
                    Stretch="UniformToFill" Opacity="0.8"/>
    </Rectangle.Fill>
</Rectangle>

<!-- 用图片填充 Path（自定义形状） -->
<Path Data="M 100,10 L 190,70 L 160,160 L 40,160 L 10,70 Z">
    <Path.Fill>
        <ImageBrush Source="avares://MyApp/Assets/texture.jpg"
                    Stretch="Fill"/>
    </Path.Fill>
</Path>
```

### 35.7.5 VisualBrush（用控件作为画刷）

VisualBrush 可以将任意 Visual 作为画刷，实现"镜面反射"等效果：

```xml
<StackPanel>
    <!-- 原始内容 -->
    <Border x:Key="SourceBorder" Background="#3B82F6" Width="200" Height="100"
            CornerRadius="8">
        <TextBlock Text="Hello Avalonia" Foreground="White"
                   HorizontalAlignment="Center" VerticalAlignment="Center"
                   FontSize="20"/>
    </Border>

    <!-- 用 VisualBrush 复制内容 -->
    <Rectangle Width="200" Height="100" Opacity="0.5">
        <Rectangle.Fill>
            <VisualBrush>
                <VisualBrush.Visual>
                    <Border Background="#3B82F6" Width="200" Height="100"
                            CornerRadius="8">
                        <TextBlock Text="Hello Avalonia" Foreground="White"
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Center" FontSize="20"/>
                    </Border>
                </VisualBrush.Visual>
            </VisualBrush>
        </Rectangle.Fill>
    </Rectangle>
</StackPanel>
```

### 35.7.6 渐变动画

通过动画改变 GradientStop 的颜色或位置：

```xml
<Rectangle Width="300" Height="100">
    <Rectangle.Fill>
        <LinearGradientBrush x:Name="GradientBrush"
                             StartPoint="0%,0%" EndPoint="100%,0%">
            <GradientStop x:Name="Stop1" Color="#3B82F6" Offset="0"/>
            <GradientStop x:Name="Stop2" Color="#8B5CF6" Offset="1"/>
        </LinearGradientBrush>
    </Rectangle.Fill>
    <Rectangle.Styles>
        <Style Selector="Rectangle">
            <Style.Animations>
                <Animation Duration="0:0:3" IterationCount="Infinite"
                           PlaybackDirection="Alternate">
                    <KeyFrame Cue="0%">
                        <Setter Property="Tag" Value="0"/>
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Tag" Value="1"/>
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </Rectangle.Styles>
</Rectangle>
```

**C# 代码中动画化渐变：**

```csharp
public class GradientAnimator
{
    private readonly LinearGradientBrush _brush;
    private readonly DispatcherTimer _timer;
    private double _offset;

    public GradientAnimator(LinearGradientBrush brush)
    {
        _brush = brush;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTick;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void OnTick(object? sender, EventArgs e)
    {
        _offset += 0.01;
        if (_offset > 1) _offset = 0;

        if (_brush.GradientStops.Count >= 2)
        {
            _brush.GradientStops[0].Offset = _offset;
            _brush.GradientStops[1].Offset = Math.Min(1, _offset + 0.3);
        }
    }
}
```

## 35.8 Pen 和线条样式

### 35.8.1 Pen 的完整属性

```csharp
var pen = new Pen(
    brush: new SolidColorBrush(Colors.Blue),
    thickness: 2.0,
    dashStyle: new DashStyle(new double[] { 4, 2 }, 0),
    lineCap: PenLineCap.Round,
    lineJoin: PenLineJoin.Round,
    miterLimit: 10.0
);
```

### 35.8.2 DashStyle 自定义

```xml
<!-- 实线（默认） -->
<Line StartPoint="10,10" EndPoint="190,10" Stroke="#374151" StrokeThickness="2"/>

<!-- 虚线：4px 实线 + 2px 空白 -->
<Line StartPoint="10,30" EndPoint="190,30" Stroke="#3B82F6" StrokeThickness="2"
      StrokeDashArray="4,2"/>

<!-- 点线：1px 实线 + 2px 空白 -->
<Line StartPoint="10,50" EndPoint="190,50" Stroke="#10B981" StrokeThickness="2"
      StrokeDashArray="1,2"/>

<!-- 点划线：8px 实线 + 2px 空白 + 2px 实线 + 2px 空白 -->
<Line StartPoint="10,70" EndPoint="190,70" Stroke="#EF4444" StrokeThickness="2"
      StrokeDashArray="8,2,2,2"/>

<!-- 长划线：12px 实线 + 4px 空白 -->
<Line StartPoint="10,90" EndPoint="190,90" Stroke="#F59E0B" StrokeThickness="2"
      StrokeDashArray="12,4"/>

<!-- DashOffset：偏移虚线模式 -->
<Path Stroke="#8B5CF6" StrokeThickness="2"
      StrokeDashArray="8,4" StrokeDashOffset="6"
      Data="M 10,110 L 190,110"/>
```

**常见虚线模式：**

| 模式 | StrokeDashArray | 效果 |
|------|----------------|------|
| 虚线 | `4,2` | - - - - |
| 点线 | `1,2` | . . . . |
| 点划线 | `8,2,2,2` | - . - . |
| 长划线 | `12,4` | ---- ---- |
| 短划线 | `2,2` | -- -- -- |

### 35.8.3 LineCap 类型

```xml
<!-- Flat（默认）：齐平端点 -->
<Line StartPoint="20,30" EndPoint="180,30" Stroke="#3B82F6" StrokeThickness="8"
      StrokeStartLineCap="Flat" StrokeEndLineCap="Flat"/>

<!-- Round：圆形端点 -->
<Line StartPoint="20,60" EndPoint="180,60" Stroke="#10B981" StrokeThickness="8"
      StrokeStartLineCap="Round" StrokeEndLineCap="Round"/>

<!-- Square：方形端点（比 Flat 多出半个线宽） -->
<Line StartPoint="20,90" EndPoint="180,90" Stroke="#EF4444" StrokeThickness="8"
      StrokeStartLineCap="Square" StrokeEndLineCap="Square"/>
```

### 35.8.4 LineJoin 类型

```xml
<!-- Miter（默认）：尖角连接 -->
<Polyline Points="20,20 100,80 180,20" Stroke="#3B82F6" StrokeThickness="8"
          StrokeLineJoin="Miter" Fill="none"/>

<!-- Bevel：斜角连接 -->
<Polyline Points="20,60 100,120 180,60" Stroke="#10B981" StrokeThickness="8"
          StrokeLineJoin="Bevel" Fill="none"/>

<!-- Round：圆角连接 -->
<Polyline Points="20,100 100,160 180,100" Stroke="#EF4444" StrokeThickness="8"
          StrokeLineJoin="Round" Fill="none"/>
```

**MiterLimit：**

```xml
<!-- 当两条线的夹角很小时，Miter 会产生很长的尖角 -->
<!-- MiterLimit 限制尖角的最大长度 -->
<Path Stroke="#F59E0B" StrokeThickness="4" StrokeLineJoin="Miter" StrokeMiterLimit="5"
      Data="M 10,50 L 100,55 L 190,50"/>
```

## 35.9 SVG 动画

### 35.9.1 Avalonia 中的 Path 动画

Avalonia 原生不支持 SMIL 动画，但可以通过属性动画实现类似效果：

**描边动画（路径绘制效果）：**

```xml
<Path x:Name="AnimatedPath"
      Stroke="#3B82F6" StrokeThickness="3" Fill="none"
      StrokeDashArray="1,1" StrokeDashOffset="1000"
      Data="M 10,80 C 40,10 65,10 95,80 S 150,150 180,80">
    <Path.Styles>
        <Style Selector="Path">
            <Style.Animations>
                <Animation Duration="0:0:2" Easing="CubicEaseOut">
                    <KeyFrame Cue="0%">
                        <Setter Property="StrokeDashOffset" Value="1000"/>
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

**颜色渐变动画：**

```xml
<Path Fill="#3B82F6" Stroke="Transparent" StrokeThickness="0"
      Data="M 100,10 L 190,70 L 160,160 L 40,160 L 10,70 Z">
    <Path.Styles>
        <Style Selector="Path:pointerover">
            <Style.Animations>
                <Animation Duration="0:0:0.5">
                    <KeyFrame Cue="0%">
                        <Setter Property="Fill" Value="#3B82F6"/>
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Fill" Value="#EF4444"/>
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </Path.Styles>
</Path>
```

### 35.9.2 路径跟随动画

让一个元素沿着 Path 定义的路径移动：

```csharp
public class PathFollowAnimation : Control
{
    private readonly PathGeometry _path;
    private double _progress;
    private readonly DispatcherTimer _timer;

    public PathFollowAnimation(string pathData)
    {
        _path = Geometry.Parse(pathData) as PathGeometry
                ?? new PathGeometry();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTick;
    }

    public void Start() => _timer.Start();

    private void OnTick(object? sender, EventArgs e)
    {
        _progress += 0.005;
        if (_progress > 1) _progress = 0;

        // 在路径上按比例取点
        _path.GetPointAtFractionLength(_progress, out var point, out _);

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        // 绘制路径
        context.DrawGeometry(null, new Pen(Brushes.Gray, 1), _path);

        // 绘制移动的点
        _path.GetPointAtFractionLength(_progress, out var point, out _);
        context.DrawEllipse(Brushes.Red, null, point, 6, 6);
    }
}
```

### 35.9.3 加载动画示例

```xml
<!-- 旋转的齿轮加载动画 -->
<Path x:Name="LoadingGear"
      Data="{StaticResource Icon.Gear}"
      Fill="#6B7280" Width="32" Height="32">
    <Path.Styles>
        <Style Selector="Path">
            <Style.Animations>
                <Animation Duration="0:0:2" IterationCount="Infinite"
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
    </Path.Styles>
    <Path.RenderTransform>
        <RotateTransform CenterX="16" CenterY="16"/>
    </Path.RenderTransform>
</Path>

<!-- 脉冲圆圈动画 -->
<Ellipse Width="40" Height="40" Fill="Transparent" Stroke="#3B82F6"
         StrokeThickness="2">
    <Ellipse.Styles>
        <Style Selector="Ellipse">
            <Style.Animations>
                <Animation Duration="0:0:1.5" IterationCount="Infinite">
                    <KeyFrame Cue="0%">
                        <Setter Property="Opacity" Value="1"/>
                        <Setter Property="ScaleTransform.ScaleX" Value="0.5"/>
                        <Setter Property="ScaleTransform.ScaleY" Value="0.5"/>
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Opacity" Value="0"/>
                        <Setter Property="ScaleTransform.ScaleX" Value="2"/>
                        <Setter Property="ScaleTransform.ScaleY" Value="2"/>
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>
    </Ellipse.Styles>
    <Ellipse.RenderTransform>
        <ScaleTransform CenterX="20" CenterY="20"/>
    </Ellipse.RenderTransform>
</Ellipse>
```

## 35.10 实战：图标系统

### 35.10.1 创建可复用的图标控件

```csharp
public class SvgIcon : TemplatedControl
{
    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<SvgIcon, Geometry?>(nameof(Data));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<SvgIcon, double>(nameof(IconSize), 24);

    public Geometry? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }
}
```

```xml
<!-- ControlTemplate -->
<ControlTemplate>
    <Path Data="{TemplateBinding Data}"
          Width="{TemplateBinding IconSize}"
          Height="{TemplateBinding IconSize}"
          Fill="{TemplateBinding Foreground}"
          Stretch="Uniform"/>
</ControlTemplate>
```

### 35.10.2 图标资源字典

```xml
<!-- Icons.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 导航图标 -->
    <Geometry x:Key="Icon.Home">M3,12L12,3L21,12V21H15V15H9V21H3V12Z</Geometry>
    <Geometry x:Key="Icon.Settings">M19.14,12.94C19.18,12.64 19.2,12.33 19.2,12
        C19.2,11.68 19.18,11.36 19.13,11.06L21.16,9.48C21.34,9.34 21.39,9.07
        21.28,8.87L19.36,5.55C19.24,5.33 18.99,5.26 18.77,5.33L16.38,6.29
        C15.88,5.91 15.35,5.59 14.76,5.35L14.4,2.81C14.36,2.57 14.16,2.4
        13.92,2.4H10.08C9.84,2.4 9.65,2.57 9.61,2.81L9.25,5.35
        C8.66,5.59 8.12,5.92 7.63,6.29L5.24,5.33C5.02,5.25 4.77,5.33
        4.65,5.55L2.74,8.87C2.62,9.08 2.66,9.34 2.86,9.48L4.89,11.06
        C4.84,11.36 4.8,11.69 4.8,12C4.8,12.31 4.82,12.64 4.87,12.94
        L2.84,14.52C2.66,14.66 2.61,14.93 2.72,15.13L4.64,18.45
        C4.76,18.67 5.01,18.74 5.23,18.67L7.62,17.71C8.12,18.09 8.65,18.41
        9.24,18.65L9.6,21.19C9.65,21.43 9.84,21.6 10.08,21.6H13.92
        C14.16,21.6 14.36,21.43 14.39,21.19L14.75,18.65C15.34,18.41
        15.88,18.09 16.37,17.71L18.76,18.67C18.98,18.75 19.23,18.67
        19.35,18.45L21.27,15.13C21.39,14.91 21.34,14.66 21.15,14.52
        L19.14,12.94ZM12,15.6C10.02,15.6 8.4,13.98 8.4,12
        C8.4,10.02 10.02,8.4 12,8.4C13.98,8.4 15.6,10.02 15.6,12
        C15.6,13.98 13.98,15.6 12,15.6Z</Geometry>
    <Geometry x:Key="Icon.Search">M15.5,14H14.71L14.43,13.73C15.41,12.59
        16,11.11 16,9.5C16,5.91 13.09,3 9.5,3S3,5.91 3,9.5S5.91,16
        9.5,16C11.11,16 12.59,15.41 13.73,14.43L14,14.71V15.5L19,20.49
        L20.49,19L15.5,14ZM9.5,14C7.01,14 5,11.99 5,9.5S7.01,5 9.5,5
        S14,7.01 14,9.5S11.99,14 9.5,14Z</Geometry>
    <Geometry x:Key="Icon.User">M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12
        A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20
        H4V18C4,15.79 7.58,14 12,14Z</Geometry>
    <Geometry x:Key="Icon.Bell">M21,19V20H3V19L5,17V11C5,7.9 7.03,5.17
        10,4.29C10,4.19 10,4.1 10,4A2,2 0 0,1 12,2A2,2 0 0,1 14,4
        C14,4.1 14,4.19 14,4.29C16.97,5.17 19,7.9 19,11V17L21,19Z
        M14,21A2,2 0 0,1 12,23A2,2 0 0,1 10,21Z</Geometry>

    <!-- 操作图标 -->
    <Geometry x:Key="Icon.Plus">M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z</Geometry>
    <Geometry x:Key="Icon.Minus">M19,13H5V11H19V13Z</Geometry>
    <Geometry x:Key="Icon.Close">M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41
        L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z</Geometry>
    <Geometry x:Key="Icon.Check">M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z</Geometry>
    <Geometry x:Key="Icon.ArrowRight">M4,15V9H12L8,5H16L22,12L16,19H8L12,15H4Z</Geometry>
    <Geometry x:Key="Icon.ChevronDown">M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z</Geometry>
    <Geometry x:Key="Icon.Copy">M19,21H8V7H19M19,5H8A2,2 0 0,0 6,7V21
        A2,2 0 0,0 8,23H19A2,2 0 0,0 21,21V7A2,2 0 0,0 19,5Z
        M16,1H4A2,2 0 0,0 2,3V17H4V3H16V1Z</Geometry>
    <Geometry x:Key="Icon.Trash">M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19
        A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z</Geometry>
</ResourceDictionary>
```

### 35.10.3 动态颜色切换

```xml
<!-- 主题感知的图标 -->
<PathIcon Data="{StaticResource Icon.Settings}"
          Width="20" Height="20"
          Foreground="{DynamicResource TextPrimaryBrush}"/>

<!-- 带悬停效果的图标按钮 -->
<Button Classes="icon-button" Command="{Binding SettingsCommand}">
    <PathIcon Data="{StaticResource Icon.Settings}" Width="20" Height="20"
              Foreground="{DynamicResource TextSecondaryBrush}"/>
    <Button.Styles>
        <Style Selector="Button.icon-button PathIcon">
            <Setter Property="Foreground" Value="{DynamicResource TextSecondaryBrush}"/>
        </Style>
        <Style Selector="Button.icon-button:pointerover PathIcon">
            <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}"/>
        </Style>
    </Button.Styles>
</Button>
```

### 35.10.4 图标缓存和性能优化

```csharp
public static class IconCache
{
    private static readonly Dictionary<string, Geometry> _cache = new();

    public static Geometry GetOrParse(string key, string pathData)
    {
        if (!_cache.TryGetValue(key, out var geometry))
        {
            geometry = Geometry.Parse(pathData);
            _cache[key] = geometry;
        }
        return geometry;
    }

    public static void Preload(IEnumerable<KeyValuePair<string, string>> icons)
    {
        foreach (var (key, pathData) in icons)
        {
            GetOrParse(key, pathData);
        }
    }
}
```

### 35.10.5 Lucide 图标库集成

CodexSwitch 使用 Lucide 图标库，以下是集成方式：

```xml
<!-- 安装 NuGet 包 -->
<!-- dotnet add package Lucide.Avalonia -->

<!-- 使用 -->
<Window xmlns:lucide="clr-namespace:Lucide.Avalonia;assembly=Lucide.Avalonia">
    <StackPanel>
        <lucide:LucideIcon Kind="Home" Size="24" StrokeWidth="2"
                           Foreground="{DynamicResource TextPrimaryBrush}"/>
        <lucide:LucideIcon Kind="Settings" Size="24"
                           Foreground="{DynamicResource TextSecondaryBrush}"/>
        <lucide:LucideIcon Kind="Search" Size="20"/>
        <lucide:LucideIcon Kind="Bell" Size="20"/>
        <lucide:LucideIcon Kind="User" Size="20"/>
    </StackPanel>
</Window>
```

## 35.11 实战：数据可视化基础

### 35.11.1 折线图

```csharp
public class LineChart : Control
{
    public static readonly StyledProperty<IList<double>?> ValuesProperty =
        AvaloniaProperty.Register<LineChart, IList<double>?>(nameof(Values));

    public static readonly StyledProperty<IBrush> LineBrushProperty =
        AvaloniaProperty.Register<LineChart, IBrush>(nameof(LineBrush),
            new SolidColorBrush(Colors.DodgerBlue));

    public IList<double>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IBrush LineBrush
    {
        get => GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Values is null || Values.Count < 2) return;

        var width = Bounds.Width;
        var height = Bounds.Height;
        var padding = 10;
        var chartWidth = width - padding * 2;
        var chartHeight = height - padding * 2;

        var max = Values.Max();
        var min = Values.Min();
        var range = max - min;
        if (range == 0) range = 1;

        // 绘制网格线
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)), 1);
        for (int i = 0; i <= 4; i++)
        {
            var y = padding + chartHeight * i / 4;
            context.DrawLine(gridPen, new Point(padding, y), new Point(width - padding, y));
        }

        // 构建折线路径
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (int i = 0; i < Values.Count; i++)
            {
                var x = padding + chartWidth * i / (Values.Count - 1);
                var y = padding + chartHeight * (1 - (Values[i] - min) / range);
                var point = new Point(x, y);

                if (i == 0)
                    ctx.BeginFigure(point, false);
                else
                    ctx.LineTo(point);
            }
            ctx.EndFigure(false);
        }

        // 绘制线条
        var linePen = new Pen(LineBrush, 2, lineCap: PenLineCap.Round,
                              lineJoin: PenLineJoin.Round);
        context.DrawGeometry(null, linePen, geometry);

        // 绘制数据点
        for (int i = 0; i < Values.Count; i++)
        {
            var x = padding + chartWidth * i / (Values.Count - 1);
            var y = padding + chartHeight * (1 - (Values[i] - min) / range);
            context.DrawEllipse(LineBrush, null, new Point(x, y), 4, 4);
        }
    }
}
```

**XAML 使用：**

```xml
<local:LineChart Width="400" Height="200" LineBrush="#3B82F6">
    <local:LineChart.Values>
        <x:Array x:Type="x:Double">
            <x:Double>10</x:Double>
            <x:Double>25</x:Double>
            <x:Double>18</x:Double>
            <x:Double>35</x:Double>
            <x:Double>28</x:Double>
            <x:Double>42</x:Double>
            <x:Double>38</x:Double>
            <x:Double>50</x:Double>
        </x:Array>
    </local:LineChart.Values>
</local:LineChart>
```

### 35.11.2 柱状图

```xml
<!-- 声明式柱状图（使用 Canvas + Rectangle） -->
<Canvas Width="400" Height="250" Background="#F9FAFB">
    <!-- Y 轴标签 -->
    <TextBlock Canvas.Left="5" Canvas.Top="10" Text="50" FontSize="10"
               Foreground="#9CA3AF"/>
    <TextBlock Canvas.Left="5" Canvas.Top="60" Text="40" FontSize="10"
               Foreground="#9CA3AF"/>
    <TextBlock Canvas.Left="5" Canvas.Top="110" Text="30" FontSize="10"
               Foreground="#9CA3AF"/>
    <TextBlock Canvas.Left="5" Canvas.Top="160" Text="20" FontSize="10"
               Foreground="#9CA3AF"/>
    <TextBlock Canvas.Left="5" Canvas.Top="210" Text="10" FontSize="10"
               Foreground="#9CA3AF"/>

    <!-- 柱子 -->
    <Rectangle Canvas.Left="40" Canvas.Top="70" Width="40" Height="160"
               Fill="#3B82F6" RadiusX="4" RadiusY="4"/>
    <Rectangle Canvas.Left="100" Canvas.Top="110" Width="40" Height="120"
               Fill="#10B981" RadiusX="4" RadiusY="4"/>
    <Rectangle Canvas.Left="160" Canvas.Top="30" Width="40" Height="200"
               Fill="#F59E0B" RadiusX="4" RadiusY="4"/>
    <Rectangle Canvas.Left="220" Canvas.Top="150" Width="40" Height="80"
               Fill="#EF4444" RadiusX="4" RadiusY="4"/>
    <Rectangle Canvas.Left="280" Canvas.Top="50" Width="40" Height="180"
               Fill="#8B5CF6" RadiusX="4" RadiusY="4"/>

    <!-- X 轴标签 -->
    <TextBlock Canvas.Left="45" Canvas.Top="235" Text="一月" FontSize="10"
               Foreground="#6B7280"/>
    <TextBlock Canvas.Left="105" Canvas.Top="235" Text="二月" FontSize="10"
               Foreground="#6B7280"/>
    <TextBlock Canvas.Left="165" Canvas.Top="235" Text="三月" FontSize="10"
               Foreground="#6B7280"/>
    <TextBlock Canvas.Left="225" Canvas.Top="235" Text="四月" FontSize="10"
               Foreground="#6B7280"/>
    <TextBlock Canvas.Left="285" Canvas.Top="235" Text="五月" FontSize="10"
               Foreground="#6B7280"/>
</Canvas>
```

### 35.11.3 饼图

```csharp
public class PieChart : Control
{
    public static readonly StyledProperty<IList<PieSlice>?> SlicesProperty =
        AvaloniaProperty.Register<PieChart, IList<PieSlice>?>(nameof(Slices));

    public IList<PieSlice>? Slices
    {
        get => GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Slices is null || Slices.Count == 0) return;

        var total = Slices.Sum(s => s.Value);
        if (total <= 0) return;

        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = Math.Min(Bounds.Width, Bounds.Height) / 2 - 10;
        var startAngle = -Math.PI / 2; // 从顶部开始

        foreach (var slice in Slices)
        {
            var sweepAngle = 2 * Math.PI * slice.Value / total;

            // 计算弧线的起止点
            var startPoint = new Point(
                center.X + radius * Math.Cos(startAngle),
                center.Y + radius * Math.Sin(startAngle)
            );
            var endPoint = new Point(
                center.X + radius * Math.Cos(startAngle + sweepAngle),
                center.Y + radius * Math.Sin(startAngle + sweepAngle)
            );

            var isLargeArc = sweepAngle > Math.PI;

            // 构建扇形路径
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(center, true);
                ctx.LineTo(startPoint);
                ctx.ArcTo(endPoint, new Size(radius, radius), 0,
                          isLargeArc, SweepDirection.Clockwise);
                ctx.EndFigure(true);
            }

            context.DrawGeometry(
                new SolidColorBrush(slice.Color),
                new Pen(Brushes.White, 2),
                geometry
            );

            startAngle += sweepAngle;
        }
    }
}

public class PieSlice
{
    public string Label { get; set; } = "";
    public double Value { get; set; }
    public Color Color { get; set; }
}
```

**XAML 使用：**

```xml
<local:PieChart Width="250" Height="250">
    <local:PieChart.Slices>
        <x:Array x:Type="local:PieSlice">
            <local:PieSlice Label="Chrome" Value="65" Color="#3B82F6"/>
            <local:PieSlice Label="Firefox" Value="15" Color="#EF4444"/>
            <local:PieSlice Label="Safari" Value="12" Color="#F59E0B"/>
            <local:PieSlice Label="Other" Value="8" Color="#10B981"/>
        </x:Array>
    </local:PieChart.Slices>
</local:PieChart>
```

### 35.11.4 坐标系转换

在数据可视化中，经常需要在"数据坐标"和"屏幕坐标"之间转换：

```csharp
public static class CoordinateTransform
{
    /// <summary>
    /// 将数据坐标转换为屏幕坐标
    /// </summary>
    public static Point ToScreen(
        double dataX, double dataY,
        Rect chartArea,
        double dataMinX, double dataMaxX,
        double dataMinY, double dataMaxY)
    {
        var screenX = chartArea.X +
            (dataX - dataMinX) / (dataMaxX - dataMinX) * chartArea.Width;
        // Y 轴反转（屏幕 Y 向下，数据 Y 向上）
        var screenY = chartArea.Y + chartArea.Height -
            (dataY - dataMinY) / (dataMaxY - dataMinY) * chartArea.Height;
        return new Point(screenX, screenY);
    }

    /// <summary>
    /// 将屏幕坐标转换为数据坐标
    /// </summary>
    public static Point ToData(
        double screenX, double screenY,
        Rect chartArea,
        double dataMinX, double dataMaxX,
        double dataMinY, double dataMaxY)
    {
        var dataX = dataMinX +
            (screenX - chartArea.X) / chartArea.Width * (dataMaxX - dataMinX);
        var dataY = dataMinY +
            (chartArea.Y + chartArea.Height - screenY) / chartArea.Height *
            (dataMaxY - dataMinY);
        return new Point(dataX, dataY);
    }
}
```

### 35.11.5 交互式图表（鼠标悬停）

```csharp
public class InteractiveLineChart : Control
{
    private IList<double>? _values;
    private int _hoveredIndex = -1;
    private readonly ToolTip _tooltip;

    public InteractiveLineChart()
    {
        _tooltip = new ToolTip();
        ClipToBounds = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_values is null || _values.Count < 2) return;

        var pos = e.GetPosition(this);
        var padding = 10.0;
        var chartWidth = Bounds.Width - padding * 2;

        // 计算最近的数据点索引
        var index = (int)Math.Round(
            (pos.X - padding) / chartWidth * (_values.Count - 1));
        index = Math.Clamp(index, 0, _values.Count - 1);

        if (index != _hoveredIndex)
        {
            _hoveredIndex = index;
            _tooltip.Content = $"值: {_values[index]:F1}";
            ToolTip.SetIsOpen(this, true);
            ToolTip.SetTip(this, _tooltip.Content);
            InvalidateVisual();
        }
    }

    protected override void OnPointerLeave(PointerEventArgs e)
    {
        base.OnPointerLeave(e);
        _hoveredIndex = -1;
        ToolTip.SetIsOpen(this, false);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        // ... 绘制折线（同 LineChart）

        // 高亮悬停的数据点
        if (_hoveredIndex >= 0 && _values is not null)
        {
            var padding = 10.0;
            var chartWidth = Bounds.Width - padding * 2;
            var chartHeight = Bounds.Height - padding * 2;
            var max = _values.Max();
            var min = _values.Min();
            var range = max - min;
            if (range == 0) range = 1;

            var x = padding + chartWidth * _hoveredIndex / (_values.Count - 1);
            var y = padding + chartHeight * (1 - (_values[_hoveredIndex] - min) / range);

            // 垂直指示线
            context.DrawLine(
                new Pen(new SolidColorBrush(Color.FromArgb(50, 59, 130, 246)), 1,
                        dashStyle: DashStyle.Dash),
                new Point(x, padding),
                new Point(x, Bounds.Height - padding)
            );

            // 高亮数据点
            context.DrawEllipse(
                new SolidColorBrush(Colors.DodgerBlue),
                new Pen(Brushes.White, 3),
                new Point(x, y), 6, 6
            );
        }
    }
}
```

## 35.12 SVG 工具链

### 35.12.1 Inkscape 导出 SVG

Inkscape 是免费的矢量图形编辑器，导出 Avalonia 友好 SVG 的最佳实践：

1. 打开 Inkscape，创建或编辑矢量图形
2. **文件 > 另存为 > 纯粹 SVG（Plain SVG）**（避免 Inkscape 专有扩展）
3. 勾选"简化路径"以减少节点数量
4. 导出后使用 SVGO 优化

### 35.12.2 SVGO 优化 SVG

SVGO 是 SVG 优化工具，可以大幅减小文件体积：

```shell
# 安装
npm install -g svgo

# 优化单个文件
svgo icon.svg -o icon.optimized.svg

# 批量优化
svgo -f ./icons/ -o ./icons-optimized/

# 配置文件 svgo.config.js
module.exports = {
    plugins: [
        'removeDoctype',
        'removeXMLProcInst',
        'removeComments',
        'removeMetadata',
        'removeEditorsNSData',
        'cleanupAttrs',
        'mergeStyles',
        'minifyStyles',
        'removeUselessDefs',
        'cleanupNumericValues',
        'convertColors',
        'removeUnknownsAndDefaults',
        'removeNonInheritableGroupAttrs',
        'removeUselessStrokeAndFill',
        'cleanupEnableBackground',
        'removeHiddenElems',
        'removeEmptyText',
        'convertShapeToPath',   // 将 rect/circle 转为 path
        'convertEllipseToCircle',
        'moveGroupAttrsToElems',
        'collapseGroups',
        'convertPathData',      // 简化路径数据
        'convertTransform',
        'removeEmptyAttrs',
        'removeEmptyContainers',
        'removeUnusedNS',
        'sortDefsChildren',
        'removeTitle',
        'removeDesc',
    ]
};
```

### 35.12.3 Figma/Sketch 导出 SVG

**Figma：**
1. 选中元素
2. 右侧面板 > Design > Export > SVG
3. 勾选"Outline strokes"（将描边转为路径）
4. 勾选"Include id attribute"（可选）

**Sketch：**
1. 选中图层
2. 文件 > 导出 > SVG
3. 选择"SVGO Compressed"以自动优化

### 35.12.4 SVG 转 XAML Path

将 SVG 的 `d` 属性直接用于 Avalonia 的 `Path.Data`：

```csharp
public static class SvgToAvaloniaConverter
{
    /// <summary>
    /// 将 SVG path d 属性转换为 Avalonia Path Mini Language
    /// （大多数情况下直接兼容，无需转换）
    /// </summary>
    public static string ConvertPathData(string svgPathData)
    {
        // SVG 和 Avalonia 使用相同的 Path Mini Language 语法
        // 但需要注意：
        // 1. SVG 中的 "e" 科学计数法可能需要处理
        // 2. 某些 SVG 特有属性（如 fill-rule）需要单独处理
        return svgPathData;
    }

    /// <summary>
    /// 从 SVG 文件中提取所有 path 的 d 属性
    /// </summary>
    public static IEnumerable<(string id, string pathData)> ExtractPaths(string svgFilePath)
    {
        var doc = XDocument.Load(svgFilePath);
        var ns = XNamespace.Get("http://www.w3.org/2000/svg");

        foreach (var path in doc.Descendants(ns + "path"))
        {
            var id = path.Attribute("id")?.Value ?? $"path_{Guid.NewGuid():N}";
            var d = path.Attribute("d")?.Value;
            if (!string.IsNullOrEmpty(d))
            {
                yield return (id, d);
            }
        }
    }
}
```

**在线工具推荐：**
- **[SVG Path Editor](https://yqnn.github.io/svg-path-editor/)** — 可视化编辑 Path Mini Language
- **[SVG Viewer](https://www.svgviewer.dev/)** — 预览和优化 SVG
- **[VectoR 365](https://www.vector365.io/)** — SVG 转 XAML 工具

## Deep Dive

### SVG 渲染管线

Avalonia 中 SVG 的渲染流程：

```
SVG 文件/字符串
  |
  v
[解析器] XML 解析 -> 提取几何元素、属性、变换
  |
  v
[路径构建] 将 SVG 元素转换为 Geometry 对象
  |  rect -> RectangleGeometry
  |  circle -> EllipseGeometry
  |  path -> PathGeometry / StreamGeometry
  |  polygon/polyline -> PathGeometry
  |
  v
[变换应用] 应用 transform 属性（translate, rotate, scale, matrix）
  |
  v
[画刷解析] 将 fill/stroke 转换为 Brush 对象
  |  solid color -> SolidColorBrush
  |  linearGradient -> LinearGradientBrush
  |  radialGradient -> RadialGradientBrush
  |
  v
[渲染] 通过 DrawingContext 绘制到 SkiaSharp 后端
```

### SkiaSharp 中的 SVG 处理

`Svg.Skia.Avalonia` 使用 SkiaSharp 的 `SKSvg` 来解析和渲染 SVG：

```csharp
using SkiaSharp;
using Svg.Skia;

// 解析 SVG
var svg = new SKSvg();
svg.Load("icon.svg");

// 获取画布尺寸
var bounds = svg.Picture?.CullRect ?? SKRect.Empty;

// 渲染到 SKBitmap
using var bitmap = new SKBitmap((int)bounds.Width, (int)bounds.Height);
using var canvas = new SKCanvas(bitmap);
canvas.DrawPicture(svg.Picture);
canvas.Flush();

// 转换为 Avalonia 可用的格式
using var image = SKImage.FromBitmap(bitmap);
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = data.AsStream();
var bitmapImage = new Bitmap(stream);
```

### 性能优化

**1. 缓存 Geometry**

```csharp
// 不好：每次渲染都解析路径
public override void Render(DrawingContext context)
{
    var geometry = Geometry.Parse(_pathData); // 每次都解析
    context.DrawGeometry(Brush, null, geometry);
}

// 好：缓存解析结果
private Geometry? _cachedGeometry;
public override void Render(DrawingContext context)
{
    _cachedGeometry ??= Geometry.Parse(_pathData);
    context.DrawGeometry(Brush, null, _cachedGeometry);
}
```

**2. 使用 StreamGeometry 替代 PathGeometry**

```csharp
// PathGeometry：可修改，但性能较低
var pathGeometry = new PathGeometry();
// ... 添加 segments

// StreamGeometry：不可修改，但性能更高
var streamGeometry = new StreamGeometry();
using (var ctx = streamGeometry.Open())
{
    ctx.BeginFigure(startPoint, true);
    ctx.LineTo(point1);
    ctx.LineTo(point2);
    ctx.EndFigure(true);
}
```

**3. 简化路径节点**

- 使用 SVGO 的 `convertPathData` 插件减少路径命令
- 避免过度精确的坐标（小数点后 2 位通常足够）
- 合并相邻的直线段

**4. 减少视觉树节点**

```xml
<!-- 不好：每个图标一个 Path 控件 -->
<StackPanel>
    <Path Data="..." Fill="..."/>
    <Path Data="..." Fill="..."/>
    <Path Data="..." Fill="..."/>
</StackPanel>

<!-- 好：使用 DrawingGroup 合并绘制 -->
<Image>
    <Image.Source>
        <DrawingImage>
            <DrawingGroup>
                <GeometryDrawing Geometry="..." Brush="..."/>
                <GeometryDrawing Geometry="..." Brush="..."/>
                <GeometryDrawing Geometry="..." Brush="..."/>
            </DrawingGroup>
        </DrawingImage>
    </Image.Source>
</Image>
```

**5. 使用 RenderTargetBitmap 缓存复杂图形**

```csharp
// 对于不经常变化的复杂矢量图形，渲染为位图缓存
var renderTarget = new RenderTargetBitmap(
    new PixelSize(400, 400), new Vector(96, 96));
renderTarget.Render(complexVectorControl);
// 后续使用 renderTarget 作为 Image.Source
```

## Cross References

- **[第 9 章：自定义控件](09-custom-controls.md)** -- 自定义 Shape 和自定义渲染控件的实现
- **[第 14 章：自定义渲染](14-custom-rendering.md)** -- DrawingContext 底层绘图 API、StreamGeometry 使用
- **[第 29 章：形状与矢量绘图](29-shapes-drawing.md)** -- Shape 控件基础、Path Mini Language 入门、Brush 基础
- **[第 10 章：动画](10-animation-transitions.md)** -- Path 动画、渐变动画的实现方式
- **[第 7 章：样式与主题](07-styling-theming.md)** -- Brush 资源定义、主题切换中图标颜色的处理
- **[第 4 章：布局系统](04-layout-system.md)** -- Canvas 布局在数据可视化中的应用

## Common Pitfalls

### 1. SVG viewBox 与 Avalonia 尺寸不匹配

**问题**：SVG 的 viewBox 定义了内部坐标系，但 Avalonia 控件的 Width/Height 定义了显示尺寸。两者不匹配会导致图形变形。

```xml
<!-- 问题：viewBox 是 0 0 24 24，但 Width/Height 设为 200 -->
<svg:Svg Path="avares://MyApp/Assets/icon.svg" Width="200" Height="200"
         Stretch="Fill"/>
<!-- 如果 Stretch="Fill" 且原始 SVG 不是正方形，会变形 -->

<!-- 解决：使用 Stretch="Uniform" 保持比例 -->
<svg:Svg Path="avares://MyApp/Assets/icon.svg" Width="200" Height="200"
         Stretch="Uniform"/>
```

### 2. 忘记 Path.Data 中的 Z 命令

**问题**：没有 Z（闭合命令），路径不闭合，Fill 无法正确填充。

```xml
<!-- 问题：没有 Z，Fill 可能不生效或填充异常 -->
<Path Data="M 10,10 L 100,10 L 100,100 L 10,100"
      Fill="#3B82F6"/>

<!-- 正确：添加 Z 闭合路径 -->
<Path Data="M 10,10 L 100,10 L 100,100 L 10,100 Z"
      Fill="#3B82F6"/>
```

### 3. Geometry.Parse 的路径数据格式错误

**问题**：Path Mini Language 的格式错误不会在编译时发现，只在运行时抛出异常。

```csharp
// 问题：多余的空格或错误的分隔符
var geo = Geometry.Parse("M 10 10 L 100 100");  // 可以工作
var geo = Geometry.Parse("M10,10L100,100");      // 可以工作
var geo = Geometry.Parse("M 10;10 L 100;100");   // 错误！分号不是有效分隔符

// 解决：使用 try-catch
try
{
    var geometry = Geometry.Parse(pathData);
}
catch (Exception ex)
{
    Logger.Warn($"Invalid path data: {ex.Message}");
}
```

### 4. SVG 中的命名空间问题

**问题**：从 SVG 提取路径数据时，忽略了命名空间导致解析失败。

```csharp
// 问题：没有处理 SVG 命名空间
var paths = doc.Descendants("path"); // 找不到任何元素

// 正确：使用正确的命名空间
var ns = XNamespace.Get("http://www.w3.org/2000/svg");
var paths = doc.Descendants(ns + "path");
```

### 5. FillRule 导致的填充异常

**问题**：复杂路径使用了错误的 FillRule，导致"镂空"区域被填充。

```xml
<!-- 问题：NonZero 规则下，五角星中心也被填充 -->
<Path Fill="#F59E0B" FillRule="NonZero"
      Data="M 100,10 L 122,70 L 188,78 L 136,124 L 150,190
            L 100,158 L 50,190 L 64,124 L 12,78 L 78,70 Z"/>

<!-- 正确：使用 EvenOdd 规则，五角星中心不填充 -->
<Path Fill="#F59E0B" FillRule="EvenOdd"
      Data="M 100,10 L 122,70 L 188,78 L 136,124 L 150,190
            L 100,158 L 50,190 L 64,124 L 12,78 L 78,70 Z"/>
```

### 6. 路径数据中的科学计数法

**问题**：某些 SVG 编辑器导出的路径使用科学计数法（如 `1e-5`），Avalonia 可能无法解析。

```csharp
// 问题路径："M 1e2,50 L 200,1e2"
// 解决：预处理路径数据，将科学计数法转换为普通数字
public static string NormalizeScientificNotation(string pathData)
{
    return Regex.Replace(pathData, @"(\d+\.?\d*)e([+-]?\d+)", match =>
    {
        var value = double.Parse(match.Value);
        return value.ToString("G");
    });
}
```

### 7. 大量 Path 控件的性能问题

**问题**：在列表中使用大量 PathIcon 或 Path 控件，导致滚动卡顿。

```xml
<!-- 问题：每个列表项都有一个 Path 控件 -->
<ListBox>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <Path Data="{Binding IconPath}" Fill="..." Width="16" Height="16"/>
                <TextBlock Text="{Binding Name}"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>

<!-- 优化方案：
     1. 使用虚拟化（ListBox 默认支持）
     2. 缓存 Geometry 对象（使用 StaticResource）
     3. 简化路径数据（减少节点数量）
     4. 考虑使用预渲染的位图替代矢量图
-->
```

### 8. avares:// 路径大小写敏感

**问题**：avares:// URI 对程序集名称和路径大小写敏感。

```xml
<!-- 错误：大小写不匹配 -->
<svg:Svg Path="avares://myapp/Assets/Icon.svg"/>
<svg:Svg Path="avares://MyApp/assets/icon.svg"/>

<!-- 正确：与项目名称和文件路径完全匹配 -->
<svg:Svg Path="avares://MyApp/Assets/Icon.svg"/>
```

### 9. Stretch 属性与 Path 坐标系的交互

**问题**：Path 的 Data 使用了大范围坐标（如 0~1000），但控件尺寸很小，导致渲染异常。

```xml
<!-- 问题：坐标范围 0~1000，但控件只有 24x24 -->
<Path Data="M 0,0 L 1000,0 L 1000,1000 L 0,1000 Z"
      Width="24" Height="24" Stretch="Uniform"/>
<!-- 可能导致精度问题 -->

<!-- 更好：使用合理的坐标范围 -->
<Path Data="M 0,0 L 24,0 L 24,24 L 0,24 Z"
      Width="24" Height="24" Stretch="Uniform"/>
```

### 10. CombinedGeometry 的空结果

**问题**：当两个 Geometry 没有重叠时，Intersect 和 Exclude 可能产生空结果。

```csharp
// 问题：两个不相交的圆做交集
var combined = new CombinedGeometry(
    GeometryCombineMode.Intersect,
    new EllipseGeometry(new Rect(0, 0, 50, 50)),
    new EllipseGeometry(new Rect(100, 100, 50, 50))
);
// combined.Bounds 可能是空的

// 解决：检查结果是否为空
if (!combined.Bounds.IsEmpty)
{
    context.DrawGeometry(brush, null, combined);
}
```

## Try It Yourself

### 练习 1：从 SVG 提取路径创建图标

从一个 SVG 图标文件（如 Lucide 或 Material Icons）中提取 `path` 的 `d` 属性，将其定义为 Geometry 资源，用 PathIcon 显示。

**提示**：用文本编辑器打开 SVG 文件，找到 `<path d="...">` 标签，复制 `d` 属性值。

### 练习 2：绘制渐变进度条

创建一个带有渐变填充的圆弧进度条，进度值从 0 到 100，圆弧从绿色渐变到红色。

**提示**：使用 A 命令绘制弧线，根据进度值计算弧线的终点坐标。使用 LinearGradientBrush 实现渐变。

### 练习 3：实现自定义星形控件

继承 Shape，创建一个可配置角数、内径比例的星形控件。支持 `Sides`（角数）和 `InnerRadius`（内径比例 0~1）两个属性。

**提示**：在 `CreateDefiningGeometry` 中使用三角函数计算内外圈的顶点坐标，交替连接。

### 练习 4：制作带动画的加载指示器

创建一个由 3 个圆点组成的加载动画，圆点依次上下跳动，循环播放。

**提示**：使用 Ellipse 控件 + TranslateTransform + 动画 KeyFrame，通过不同的动画 Delay 实现错位效果。

### 练习 5：实现交互式饼图

绘制一个饼图，鼠标悬停在某个扇区上时，该扇区向外偏移一定距离（"弹出"效果），并显示数值提示。

**提示**：在 `OnPointerMoved` 中判断鼠标位置在哪个扇区内（通过角度计算），使用 ToolTip 显示数值。

### 练习 6：创建 SVG 转 Path Mini Language 的工具

编写一个 C# 方法，读取 SVG 文件，提取所有 `<path>` 元素的 `d` 属性，生成对应的 Avalonia Geometry 资源字典 XAML。

**提示**：使用 `XDocument` 解析 SVG，注意处理命名空间 `xmlns="http://www.w3.org/2000/svg"`。

### 练习 7：实现路径描边动画

创建一个图标绘制动画：图标从无到有逐渐"画出来"，通过动画 `StrokeDashOffset` 实现。

**提示**：设置 `StrokeDashArray` 为路径的总长度，然后从总长度动画到 0。需要先计算路径总长度（可以在 C# 中通过遍历 PathGeometry 的 segments 来估算）。

### 练习 8：构建迷你图表组件

创建一个可复用的 `MiniChart` 控件，接受一组 `double` 数据，支持折线图和面积图两种模式，带有鼠标悬停显示数值的功能。

**提示**：使用 `StreamGeometry` 构建折线路径，面积图在折线下方填充渐变色。通过 `ControlTemplate` 和 `TemplatePart` 实现可复用性。
