# 38. 代码编辑器 -- AvaloniaEdit 集成

> **写给零基础的你**：如果你想在应用里嵌入一个像 VS Code 那样的代码编辑器——支持语法高亮、行号、代码折叠——就需要 AvaloniaEdit。它是 Avalonia 生态中最强大的代码编辑控件。

## 38.1 概述

本章涵盖 AvaloniaEdit 的完整使用：

- **基础编辑器**：文本编辑、语法高亮
- **代码折叠**：折叠/展开代码块
- **自动补全**：IntelliSense 式的代码提示
- **搜索替换**：正则表达式搜索
- **自定义语法高亮**：为自定义语言添加高亮
- **Markdown 编辑器**：带实时预览

## 38.2 AvaloniaEdit 基础

### 38.2.1 安装配置

```xml
<PackageReference Include="AvaloniaEdit" Version="1.*" />
<PackageReference Include="AvaloniaEdit.TextMate" Version="1.*" />
```

### 38.2.2 基本编辑器

```xml
<Window xmlns:ae="clr-namespace:AvaloniaEdit;assembly=AvaloniaEdit"
        xmlns:aeHighlighting="clr-namespace:AvaloniaEdit.Highlighting;assembly=AvaloniaEdit">
    <ae:TextEditor x:Name="Editor"
                   FontFamily="Cascadia Code,Consolas,Monaco,monospace"
                   FontSize="14"
                   ShowLineNumbers="True"
                   WordWrap="True"
                   Background="#1E1E1E"
                   Foreground="#D4D4D4"
                   LineNumberForeground="#858585" />
</Window>
```

```csharp
public partial class CodeEditorWindow : Window
{
    public CodeEditorWindow()
    {
        InitializeComponent();

        // 设置语法高亮
        Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

        // 加载文本
        Editor.Text = @"using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}";

        // 监听文本变化
        Editor.TextChanged += (s, e) =>
        {
            Console.WriteLine($"文本长度: {Editor.Text.Length}");
        };

        // 设置选项
        Editor.Options.ShowSpaces = false;
        Editor.Options.ShowTabs = false;
        Editor.Options.ShowEndOfLine = false;
        Editor.Options.HighlightCurrentLine = true;
        Editor.Options.EnableRectangularSelection = true;
    }
}
```

### 38.2.3 内置语法高亮

AvaloniaEdit 内置了多种语言的语法高亮：

```csharp
// 获取所有可用的语法高亮定义
var definitions = HighlightingManager.Instance.HighlightingDefinitions;
foreach (var def in definitions)
{
    Console.WriteLine(def.Name); // C#, JavaScript, XML, HTML, CSS, JSON, etc.
}

// 设置语法高亮
Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JSON");
```

## 38.3 TextMate 语法高亮（VS Code 兼容）

### 38.3.1 使用 TextMate 主题和语法

TextMate 支持 VS Code 的 `.tmTheme` 和 `.tmLanguage` 文件，可以使用任何 VS Code 主题：

```csharp
using AvaloniaEdit.TextMate;

public partial class AdvancedEditor : Window
{
    private TextMate.Installation? _textMateInstallation;

    public AdvancedEditor()
    {
        InitializeComponent();

        // 安装 TextMate
        _textMateInstallation = Editor.InstallTextMate(
            RegistryOptions.DefaultThemes.DarkPlus);

        // 加载语法
        _textMateInstallation.SetGrammar(
            RegistryOptions.GetScopeByLanguageId(
                RegistryOptions.DefaultThemes.CSharpLanguageId));
    }
}
```

### 38.3.2 切换主题

```csharp
// 切换到亮色主题
_textMateInstallation.SetTheme(
    RegistryOptions.DefaultThemes.LightPlus);

// 切换到暗色主题
_textMateInstallation.SetTheme(
    RegistryOptions.DefaultThemes.DarkPlus);
```

### 38.3.3 自定义 TextMate 语法

```csharp
// 注册自定义语言
var registryOptions = new RegistryOptions(
    ThemeName.DarkPlus);

// 从文件加载自定义语法
var grammarPath = "Assets/Syntaxes/mylang.tmLanguage.json";
var grammarContent = File.ReadAllText(grammarPath);
registryOptions.AddGrammar(grammarContent);
```

## 38.4 代码折叠

### 38.4.1 启用代码折叠

```csharp
using AvaloniaEdit.Folding;

public class FoldingEditor
{
    private readonly TextEditor _editor;
    private FoldingManager _foldingManager;
    private XmlFoldingStrategy _foldingStrategy;

    public FoldingEditor(TextEditor editor)
    {
        _editor = editor;

        // 对于 XML/HTML
        _foldingManager = FoldingManager.Install(_editor.TextArea);
        _foldingStrategy = new XmlFoldingStrategy();
        _foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);
    }

    // 文本变化时更新折叠
    public void UpdateFolding()
    {
        _foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);
    }
}
```

### 38.4.2 C# 代码折叠

```csharp
// C# 需要自定义折叠策略
public class CSharpFoldingStrategy : AbstractFoldingStrategy
{
    public override void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var foldings = new List<NewFolding>();
        var text = document.Text;

        // 查找大括号匹配
        var stack = new Stack<int>();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                stack.Push(i);
            }
            else if (text[i] == '}' && stack.Count > 0)
            {
                var start = stack.Pop();
                // 确保折叠区域有意义（至少 2 行）
                var regionText = text[start..i];
                if (regionText.Contains('\n'))
                {
                    foldings.Add(new NewFolding(start, i + 1));
                }
            }
        }

        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        manager.UpdateFoldings(foldings, -1);
    }
}

// 使用
_foldingManager = FoldingManager.Install(_editor.TextArea);
var strategy = new CSharpFoldingStrategy();
// 定时更新或在文本变化时更新
```

### 38.4.3 #region 折叠

```csharp
public class RegionFoldingStrategy : AbstractFoldingStrategy
{
    public override void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var foldings = new List<NewFolding>();
        var lines = document.Lines;
        var regionStack = new Stack<(int offset, string name)>();

        foreach (var line in lines)
        {
            var text = document.GetText(line).Trim();

            if (text.StartsWith("#region"))
            {
                var name = text.Length > 8 ? text[8..].Trim() : "Region";
                regionStack.Push((line.Offset, name));
            }
            else if (text.StartsWith("#endregion") && regionStack.Count > 0)
            {
                var (startOffset, name) = regionStack.Pop();
                var folding = new NewFolding(startOffset, line.EndOffset)
                {
                    Name = name
                };
                foldings.Add(folding);
            }
        }

        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        manager.UpdateFoldings(foldings, -1);
    }
}
```

## 38.5 自动补全

### 38.5.1 基本自动补全

```csharp
using AvaloniaEdit.CodeCompletion;

public class AutoCompleteService
{
    private readonly TextEditor _editor;
    private CompletionWindow? _completionWindow;

    public AutoCompleteService(TextEditor editor)
    {
        _editor = editor;
        _editor.TextArea.TextEntered += OnTextEntered;
        _editor.TextArea.TextEntering += OnTextEntering;
    }

    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        // 当输入特定字符时触发补全
        if (e.Text == "." || char.IsLetter(e.Text[0]))
        {
            ShowCompletion();
        }
    }

    private void ShowCompletion()
    {
        _completionWindow = new CompletionWindow(_editor.TextArea);
        var data = _completionWindow.CompletionList.CompletionData;

        // 添加补全项
        data.Add(new MyCompletionData("Console"));
        data.Add(new MyCompletionData("Convert"));
        data.Add(new MyCompletionData("DateTime"));
        data.Add(new MyCompletionData("Dictionary"));
        data.Add(new MyCompletionData("Environment"));

        _completionWindow.Show();
        _completionWindow.Closed += (s, e) => _completionWindow = null;
    }

    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (e.Text.Length > 0 && _completionWindow != null)
        {
            // 如果输入的是非字母数字字符，完成补全
            if (!char.IsLetterOrDigit(e.Text[0]))
            {
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }
    }
}
```

### 38.5.2 自定义补全数据

```csharp
public class MyCompletionData : ICompletionData
{
    public MyCompletionData(string text)
    {
        Text = text;
    }

    public ImageSource? Image => null;
    public string Text { get; }
    public object Content => Text;
    public object Description => $"Type: {Text}";
    public double Priority => 1.0;

    public void Complete(TextArea textArea, ISegment completionSegment,
        EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
```

### 38.5.3 带图标的补全

```csharp
public class CodeCompletionData : ICompletionData
{
    public CodeCompletionData(string text, string description, CompletionKind kind)
    {
        Text = text;
        Description = description;
        Kind = kind;
    }

    public ImageSource? Image => Kind switch
    {
        CompletionKind.Method => LoadImage("method.png"),
        CompletionKind.Property => LoadImage("property.png"),
        CompletionKind.Class => LoadImage("class.png"),
        CompletionKind.Keyword => LoadImage("keyword.png"),
        _ => null
    };

    public string Text { get; }
    public object Content => Text;
    public object Description { get; }
    public double Priority => 1.0;
    public CompletionKind Kind { get; }

    public void Complete(TextArea textArea, ISegment completionSegment,
        EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }

    private ImageSource? LoadImage(string name)
    {
        // 从资源加载图标
        return null;
    }
}

public enum CompletionKind
{
    Method, Property, Class, Keyword, Variable, Enum, Interface
}
```

## 38.6 搜索与替换

### 38.6.1 内置搜索面板

```csharp
// AvaloniaEdit 内置了搜索面板
// Ctrl+F 打开搜索，Ctrl+H 打开替换

// 编程方式触发搜索
using AvaloniaEdit.Search;

public class SearchService
{
    private readonly SearchPanel _searchPanel;

    public SearchService(TextEditor editor)
    {
        _searchPanel = SearchPanel.Install(editor.TextArea);
    }

    public void OpenSearch()
    {
        _searchPanel.Open();
        _searchPanel.SearchPattern = ""; // 清空搜索框
    }

    public void OpenReplace()
    {
        _searchPanel.Open();
        // 替换模式需要额外配置
    }

    public void FindAll(string pattern)
    {
        var regex = new System.Text.RegularExpressions.Regex(pattern);
        var matches = regex.Matches(Editor.Text);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            Console.WriteLine($"Found at {match.Index}: {match.Value}");
        }
    }
}
```

## 38.7 自定义语法高亮定义

### 38.7.1 使用 XML 定义

```xml
<!-- Assets/Syntaxes/MyLang.xshd -->
<SyntaxDefinition name="MyLang" extensions=".ml">
    <Color name="Comment" foreground="#6A9955" />
    <Color name="String" foreground="#CE9178" />
    <Color name="Keyword" foreground="#569CD6" />
    <Color name="Number" foreground="#B5CEA8" />

    <RuleSet>
        <Span color="Comment" begin="//" />
        <Span color="Comment" begin="/*" end="*/" />
        <Span color="String" begin="&quot;" end="&quot;" />

        <Keywords color="Keyword">
            <Word>if</Word>
            <Word>else</Word>
            <Word>while</Word>
            <Word>for</Word>
            <Word>return</Word>
            <Word>function</Word>
            <Word>let</Word>
            <Word>const</Word>
        </Keywords>

        <Rule color="Number">\b[0-9]+\.?[0-9]*\b</Rule>
    </RuleSet>
</SyntaxDefinition>
```

```csharp
// 加载自定义语法
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

public static class CustomSyntaxLoader
{
    public static void LoadCustomSyntax()
    {
        using var stream = AssetLoader.Open(new Uri("avares://MyApp/Assets/Syntaxes/MyLang.xshd"));
        using var reader = new System.Xml.XmlReader(stream);
        var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        HighlightingManager.Instance.RegisterHighlighting("MyLang", new[] { ".ml" }, definition);
    }
}
```

### 38.7.2 使用代码定义

```csharp
public static class CustomHighlighting
{
    public static IHighlightingDefinition Create()
    {
        var builder = new HighlightingDefinitionBuilder("MyLang");

        // 注释
        builder.AddRuleSet(new HighlightingRuleSet
        {
            Spans =
            {
                new HighlightingSpan
                {
                    StartExpression = new Regex("//"),
                    EndExpression = new Regex("$"),
                    SpanColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Green) }
                }
            },
            Rules =
            {
                new HighlightingRule
                {
                    Regex = new Regex(@"\b(if|else|while|for|return)\b"),
                    Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Blue) }
                }
            }
        });

        return builder.Create();
    }
}
```

## 38.8 Markdown 编辑器与预览

### 38.8.1 双栏 Markdown 编辑器

```xml
<Grid ColumnDefinitions="*,5,*">
    <!-- 编辑器 -->
    <ae:TextEditor Grid.Column="0" x:Name="Editor"
                   FontFamily="Cascadia Code,monospace"
                   FontSize="14"
                   SyntaxHighlighting="Markdown"
                   TextChanged="OnTextChanged" />

    <GridSplitter Grid.Column="1" />

    <!-- 预览 -->
    <Border Grid.Column="2" Background="White">
        <ScrollViewer>
            <TextBlock x:Name="Preview"
                       TextWrapping="Wrap"
                       Margin="16"
                       Foreground="Black" />
        </ScrollViewer>
    </Border>
</Grid>
```

```csharp
public partial class MarkdownEditor : Window
{
    private readonly DispatcherTimer _debounceTimer;

    public MarkdownEditor()
    {
        InitializeComponent();

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounceTimer.Tick += (s, e) =>
        {
            _debounceTimer.Stop();
            UpdatePreview();
        };
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void UpdatePreview()
    {
        var markdown = Editor.Text;
        // 使用 Markdig 库渲染
        // NuGet: Markdig
        var html = Markdig.Markdown.ToHtml(markdown);
        Preview.Text = html; // 简化版，实际可用 WebView 渲染
    }
}
```

## 38.9 编辑器配置与选项

### 38.9.1 常用选项

```csharp
// 编辑器选项
Editor.Options.EnableEmailHyperlinks = true;
Editor.Options.EnableHyperlinks = true;
Editor.Options.ConvertTabsToSpaces = true;
Editor.Options.IndentationSize = 4;
Editor.Options.ShowSpaces = false;
Editor.Options.ShowTabs = false;
Editor.Options.ShowEndOfLine = false;
Editor.Options.HighlightCurrentLine = true;
Editor.Options.EnableRectangularSelection = true;
Editor.Options.ShowColumnRuler = true;
Editor.Options.ColumnRulerPosition = 120;
Editor.Options.AllowScrollBelowDocument = false;
```

### 38.9.2 键绑定

```csharp
// 添加自定义快捷键
Editor.TextArea.KeyBindings.Add(new KeyBinding
{
    Gesture = new KeyGesture(Key.F5),
    Command = new RelayCommand(() => RunCode())
});

// 移除默认快捷键
Editor.TextArea.DefaultInputHandler.CaretNavigation.KeyBindings.Clear();
```

## 38.10 Deep Dive: AvaloniaEdit 架构

AvaloniaEdit 的核心架构：

```
TextEditor (控件层)
├── TextArea (文本区域)
│   ├── TextView (文本渲染)
│   ├── Caret (光标)
│   ├── Selection (选区)
│   └── InputHandler (输入处理)
├── TextDocument (文档模型)
│   ├── Rope<char> (高效文本存储)
│   ├── UndoStack (撤销栈)
│   └── LineManager (行管理)
└── Services
    ├── SyntaxHighlighting
    ├── Folding
    ├── CodeCompletion
    └── Search
```

**性能优化要点：**
- 使用 `Rope<char>` 数据结构实现高效的文本插入/删除
- 虚拟化渲染：只渲染可见行
- 增量语法高亮：只重新着色变化的区域

## 38.11 Cross References

- **第 9 章**：自定义控件开发（理解 AvaloniaEdit 的控件架构）
- **第 16 章**：输入处理与事件系统（键盘事件处理）
- **第 29 章**：形状与矢量绘图（自定义渲染）

## 38.12 Common Pitfalls

1. **大文件性能**：超过 10MB 的文件可能导致卡顿，需要分段加载
2. **TextMate 内存占用**：TextMate 语法高亮比内置高亮消耗更多内存
3. **撤销栈溢出**：频繁的小改动可能填满撤销栈
4. **语法高亮冲突**：同时使用内置和 TextMate 高亮会导致冲突
5. **自动补全窗口位置**：在窗口边缘时可能被裁切
6. **IME 输入**：中日韩输入法可能有兼容性问题

## 38.13 Try It Yourself

1. 创建一个带语法高亮的代码编辑器，支持 C# 和 JSON
2. 实现自定义代码折叠策略
3. 创建一个带自动补全的编辑器，补全项从代码分析获取
4. 实现 Markdown 双栏编辑器（左边编辑，右边预览）
