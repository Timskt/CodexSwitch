# 39. 测试 -- 单元测试与 UI 自动化测试

> **写给零基础的你**：测试就是"检查你的软件有没有 Bug"。就像考试检查你有没有学好一样。自动化测试就是写一段代码来自动检查另一段代码是否正确，省得你每次改代码后都要手动点一遍。

## 39.1 概述

本章涵盖 Avalonia 应用的测试策略：

- **Avalonia.Headless**：无头模式 UI 测试
- **xUnit 集成**：标准单元测试框架
- **ViewModel 测试**：MVVM 架构下的 ViewModel 测试
- **UI 自动化测试**：模拟用户操作
- **截图测试**：视觉回归测试

## 39.2 测试项目设置

### 39.2.1 创建测试项目

```xml
<!-- CodexSwitch.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
        <PackageReference Include="xunit" Version="2.*" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
        <PackageReference Include="Moq" Version="4.*" />
        <PackageReference Include="FluentAssertions" Version="7.*" />

        <!-- Avalonia 测试支持 -->
        <PackageReference Include="Avalonia.Headless" Version="12.*" />
        <PackageReference Include="Avalonia.Headless.XUnit" Version="12.*" />
        <PackageReference Include="Avalonia.Themes.Fluent" Version="12.*" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="../MyApp/MyApp.csproj" />
    </ItemGroup>
</Project>
```

### 39.2.2 测试 App 配置

```csharp
// TestApp.axaml.cs
public class TestApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

// AssemblyInfo.cs 或 TestStartup.cs
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = true
            })
            .UseSkia();
}
```

## 39.3 ViewModel 测试

### 39.3.1 基本 ViewModel 测试

```csharp
public class MainWindowViewModelTests
{
    [Fact]
    public void Should_Initialize_With_Default_State()
    {
        // Arrange & Act
        var vm = new MainWindowViewModel();

        // Assert
        vm.CurrentPage.Should().Be("Home");
        vm.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void Should_Change_Page_When_Navigating()
    {
        // Arrange
        var vm = new MainWindowViewModel();

        // Act
        vm.NavigateTo("Settings");

        // Assert
        vm.CurrentPage.Should().Be("Settings");
    }

    [Fact]
    public async Task Should_Load_Data_Async()
    {
        // Arrange
        var mockService = new Mock<IDataService>();
        mockService.Setup(s => s.GetDataAsync())
            .ReturnsAsync(new List<string> { "A", "B", "C" });

        var vm = new MainWindowViewModel(mockService.Object);

        // Act
        await vm.LoadDataCommand.ExecuteAsync(null);

        // Assert
        vm.Items.Should().HaveCount(3);
        vm.IsLoading.Should().BeFalse();
    }
}
```

### 39.3.2 测试命令

```csharp
public class CommandTests
{
    [Fact]
    public void SaveCommand_Should_Not_Execute_When_Invalid()
    {
        var vm = new NoteViewModel();
        vm.Title = ""; // 无效状态

        var canExecute = vm.SaveCommand.CanExecute(null);

        canExecute.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCommand_Should_Confirm_Before_Deleting()
    {
        var mockDialog = new Mock<IDialogService>();
        mockDialog.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var mockRepo = new Mock<INoteRepository>();
        var vm = new NoteViewModel(mockRepo.Object, mockDialog.Object);

        await vm.DeleteCommand.ExecuteAsync(null);

        mockDialog.Verify(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        mockRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Once);
    }
}
```

### 39.3.3 测试属性变更通知

```csharp
public class PropertyChangeTests
{
    [Fact]
    public void Should_Raise_PropertyChanged()
    {
        var vm = new SettingsViewModel();
        var changed = new List<string>();

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                changed.Add(e.PropertyName);
        };

        vm.Theme = "Dark";

        changed.Should().Contain("Theme");
    }

    [Fact]
    public void Should_Raise_PropertyChanged_For_Dependent_Properties()
    {
        var vm = new UserViewModel();
        var changed = new List<string>();

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                changed.Add(e.PropertyName);
        };

        vm.FirstName = "John";

        changed.Should().Contain("FirstName");
        changed.Should().Contain("FullName"); // 依赖属性也应该通知
    }
}
```

## 39.4 Avalonia Headless UI 测试

### 39.4.1 窗口测试

```csharp
public class MainWindowTests
{
    [AvaloniaFact]
    public void Should_Create_Window()
    {
        var window = new MainWindow();

        window.Should().NotBeNull();
        window.Title.Should().Be("My App");
    }

    [AvaloniaFact]
    public void Should_Find_Controls()
    {
        var window = new MainWindow();
        window.Show();

        var sidebar = window.FindControl<Border>("Sidebar");
        sidebar.Should().NotBeNull();

        var content = window.FindControl<ContentPresenter>("MainContent");
        content.Should().NotBeNull();

        window.Close();
    }

    [AvaloniaFact]
    public async Task Should_Bind_DataContext()
    {
        var window = new MainWindow();
        var vm = new MainWindowViewModel();
        window.DataContext = vm;
        window.Show();

        vm.CurrentPage = "Settings";

        // 等待绑定更新
        await Dispatcher.UIThread.InvokeAsync(() => { });

        var title = window.FindControl<TextBlock>("PageTitle");
        title?.Text.Should().Be("Settings");

        window.Close();
    }
}
```

### 39.4.2 模拟用户交互

```csharp
public class InteractionTests
{
    [AvaloniaFact]
    public void Should_Click_Button()
    {
        var window = new MainWindow();
        window.Show();

        var button = window.FindControl<Button>("SaveButton");
        button.Should().NotBeNull();

        // 模拟点击
        button!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // 验证结果
        var vm = window.DataContext as MainWindowViewModel;
        vm?.IsSaved.Should().BeTrue();

        window.Close();
    }

    [AvaloniaFact]
    public async Task Should_Type_In_TextBox()
    {
        var window = new TestWindow();
        window.Show();

        var textBox = window.FindControl<TextBox>("InputBox");
        textBox.Should().NotBeNull();

        // 模拟键盘输入
        textBox!.Focus();

        // 使用 Avalonia 的输入模拟
        var topLevel = TopLevel.GetTopLevel(textBox)!;
        // 注意：Headless 模式下的输入模拟有限

        window.Close();
    }
}
```

### 39.4.3 测试对话框

```csharp
public class DialogTests
{
    [AvaloniaFact]
    public async Task Should_Show_Confirm_Dialog()
    {
        var mockDialog = new MockDialogService(confirmResult: true);
        var vm = new MainWindowViewModel(dialogService: mockDialog);

        var result = await vm.DeleteItem();

        result.Should().BeTrue();
        mockDialog.WasConfirmCalled.Should().BeTrue();
    }
}

// Mock 对话框服务
public class MockDialogService : IDialogService
{
    private readonly bool _confirmResult;
    public bool WasConfirmCalled { get; private set; }

    public MockDialogService(bool confirmResult)
    {
        _confirmResult = confirmResult;
    }

    public Task<bool> ConfirmAsync(string title, string message)
    {
        WasConfirmCalled = true;
        return Task.FromResult(_confirmResult);
    }
}
```

## 39.5 截图测试

### 39.5.1 基本截图测试

```csharp
public class ScreenshotTests
{
    [AvaloniaFact]
    public void Should_Match_Screenshot()
    {
        var window = new MainWindow();
        window.Width = 1200;
        window.Height = 800;
        window.Show();

        // 等待布局完成
        Dispatcher.UIThread.RunJobs();

        // 截图
        var bitmap = new RenderTargetBitmap(
            new PixelSize(1200, 800), new Vector(96, 96));
        bitmap.Render(window);

        // 与基准图片比较
        var baselinePath = "Screenshots/mainwindow_baseline.png";
        if (File.Exists(baselinePath))
        {
            var baseline = new Bitmap(baselinePath);
            // 比较像素差异
            var diff = CompareBitmaps(bitmap, baseline);
            diff.Should().BeLessThan(0.01); // 允许 1% 差异
        }
        else
        {
            // 首次运行，保存基准图片
            using var stream = File.Create(baselinePath);
            bitmap.Save(stream);
        }

        window.Close();
    }

    private double CompareBitmaps(Bitmap a, Bitmap b)
    {
        // 简化的像素比较
        // 实际实现需要更复杂的图像差异算法
        return 0;
    }
}
```

### 39.5.2 多主题截图测试

```csharp
[Theory]
[InlineData("Light")]
[InlineData("Dark")]
public void Should_Render_Both_Themes(string theme)
{
    var app = Application.Current!;
    var themeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
    app.RequestedThemeVariant = themeVariant;

    var window = new MainWindow();
    window.Show();

    var bitmap = CaptureWindow(window);
    var baselinePath = $"Screenshots/mainwindow_{theme.ToLower()}_baseline.png";
    CompareWithBaseline(bitmap, baselinePath);

    window.Close();
}
```

## 39.6 集成测试

### 39.6.1 服务层集成测试

```csharp
public class DatabaseIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly AppDbContext _context;

    public DatabaseIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _context = new AppDbContext(_dbPath);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Project()
    {
        // Arrange
        var project = new Project { Name = "Test Project" };

        // Act
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.Projects.FindAsync(project.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task Should_Cascade_Delete_Notes()
    {
        // Arrange
        var project = new Project
        {
            Name = "Test",
            Notes = new List<Note>
            {
                new() { Title = "Note 1" },
                new() { Title = "Note 2" }
            }
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        // Assert
        var notes = await _context.Notes.Where(n => n.ProjectId == project.Id).ToListAsync();
        notes.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}
```

### 39.6.2 HTTP 集成测试

```csharp
public class ApiIntegrationTests
{
    private readonly HttpClient _client;

    public ApiIntegrationTests()
    {
        // 使用 TestServer 测试 ASP.NET Core API
        var factory = new WebApplicationFactory<Program>();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Should_Get_Health_Check()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
```

## 39.7 测试工具函数

### 39.7.1 测试辅助方法

```csharp
public static class TestHelpers
{
    /// <summary>
    /// 等待条件满足
    /// </summary>
    public static async Task WaitUntil(Func<bool> condition,
        TimeSpan? timeout = null, int intervalMs = 50)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return;
            await Task.Delay(intervalMs);
        }
        throw new TimeoutException("Condition not met within timeout");
    }

    /// <summary>
    /// 捕获窗口截图
    /// </summary>
    public static RenderTargetBitmap CaptureWindow(Window window)
    {
        var size = new PixelSize((int)window.Width, (int)window.Height);
        var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
        bitmap.Render(window);
        return bitmap;
    }

    /// <summary>
    /// 创建测试用的临时目录
    /// </summary>
    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        return path;
    }
}
```

## 39.8 测试最佳实践

### 39.8.1 测试金字塔

```
        /  E2E Tests  \        (少量)
       / Integration   \       (适量)
      /  Unit Tests     \      (大量)
```

- **单元测试**：测试 ViewModel、Service、Model 的逻辑
- **集成测试**：测试数据库、API、服务间交互
- **E2E 测试**：测试完整的用户流程

### 39.8.2 命名规范

```csharp
// 推荐格式: Should_ExpectedBehavior_When_Condition
public class CalculatorTests
{
    [Fact]
    public void Should_Return_Sum_When_Adding_Two_Numbers() { }

    [Fact]
    public void Should_Throw_When_Dividing_By_Zero() { }

    [Fact]
    public async Task Should_Load_Data_When_Initialized() { }
}
```

### 39.8.3 AAA 模式

```csharp
[Fact]
public void Should_Update_Title_When_Saving()
{
    // Arrange - 准备测试数据和环境
    var vm = new NoteViewModel();
    vm.Title = "Original";
    vm.Content = "Content";

    // Act - 执行被测试的操作
    vm.Title = "Updated";
    vm.SaveCommand.Execute(null);

    // Assert - 验证结果
    vm.SavedTitle.Should().Be("Updated");
}
```

## 39.9 CI/CD 中的测试

```yaml
# GitHub Actions 测试步骤
- name: Run Tests
  run: |
    dotnet test CodexSwitch.Tests/CodexSwitch.Tests.csproj \
      -c Release \
      --no-build \
      --logger "trx;LogFileName=test-results.trx" \
      --collect:"XPlat Code Coverage"

- name: Upload Test Results
  uses: actions/upload-artifact@v4
  if: always()
  with:
    name: test-results
    path: '**/test-results.trx'
```

## 39.10 Cross References

- **第 6 章**：MVVM 模式实战（ViewModel 测试的基础）
- **第 18 章**：命令系统（命令测试）
- **第 25 章**：ASP.NET Core 集成（API 测试）

## 39.11 Common Pitfalls

1. **Headless 模式限制**：不是所有 UI 功能都能在 Headless 模式下测试
2. **异步测试超时**：需要合理设置超时时间
3. **测试隔离**：每个测试应该独立，不依赖其他测试的执行顺序
4. **Mock 过度**：不要 Mock 一切，保留关键集成测试
5. **截图测试敏感**：字体渲染、DPI 差异可能导致截图不一致
6. **内存泄漏**：测试结束后要 Dispose 资源

## 39.12 Try It Yourself

1. 为你的 ViewModel 编写单元测试，覆盖主要功能
2. 使用 Avalonia.Headless 创建一个窗口测试
3. 编写数据库集成测试，测试 CRUD 操作
4. 设置 CI 流水线，自动运行所有测试
