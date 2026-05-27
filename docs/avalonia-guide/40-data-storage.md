# 34. 本地数据存储 -- SQLite、EF Core 与凭据管理

> **写给零基础的你**：你的应用需要保存数据——用户的设置、聊天记录、文件列表等。本章教你如何在 Avalonia 应用中使用本地数据库（SQLite）和安全存储（凭据管理器）来持久化数据。

## 34.1 概述

本章涵盖 Avalonia 应用的本地数据存储方案：

- **SQLite 数据库**：轻量级、跨平台的嵌入式数据库
- **Entity Framework Core 集成**：ORM 框架，用面向对象的方式操作数据库
- **凭据管理**：安全存储密码、Token 等敏感信息
- **配置文件管理**：JSON、TOML、YAML 等格式
- **日志系统**：Serilog 集成

## 34.2 SQLite 集成

### 34.2.1 为什么选择 SQLite

| 特性 | SQLite | JSON 文件 | 本地服务器 |
|------|--------|----------|-----------|
| 查询能力 | SQL 全功能 | 需要全部加载 | SQL 全功能 |
| 并发安全 | 读并发/写串行 | 需要自己实现 | 取决于引擎 |
| 部署复杂度 | 零配置 | 零配置 | 需要安装 |
| 跨平台 | 全平台 | 全平台 | 取决于引擎 |
| 数据量 | GB 级 | 受内存限制 | 无限制 |
| 适合场景 | 结构化数据 | 简单配置 | 大型应用 |

### 34.2.2 安装配置

```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.*" />
<!-- 或使用 EF Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.*" />
```

### 34.2.3 原生 ADO.NET 使用

```csharp
using Microsoft.Data.Sqlite;

public class SimpleDatabase
{
    private readonly string _connectionString;

    public SimpleDatabase(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Notes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Content TEXT,
                CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
            )";
        command.ExecuteNonQuery();
    }

    public List<Note> GetAllNotes()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Content, CreatedAt FROM Notes ORDER BY UpdatedAt DESC";

        var notes = new List<Note>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            notes.Add(new Note
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Content = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3))
            });
        }
        return notes;
    }

    public void InsertNote(Note note)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Notes (Title, Content) VALUES ($title, $content)";
        command.Parameters.AddWithValue("$title", note.Title);
        command.Parameters.AddWithValue("$content", note.Content ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }
}
```

### 34.2.4 数据库文件路径

```csharp
public static class DatabasePaths
{
    public static string GetAppDataPath(string appName)
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);

        if (OperatingSystem.IsMacOS())
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", appName);

        // Linux
        var xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local", "share");
        return Path.Combine(xdgData, appName.ToLowerInvariant());
    }

    public static string GetDatabasePath(string appName, string dbName = "app.db")
    {
        var dir = GetAppDataPath(appName);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, dbName);
    }
}
```

## 34.3 Entity Framework Core 集成

### 34.3.1 定义 DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Setting> Settings => Set<Setting>();

    private readonly string _dbPath;

    public AppDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={_dbPath}");

        // 开发环境启用详细日志
        #if DEBUG
        options.LogTo(Console.WriteLine, LogLevel.Information);
        #endif
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasMany(e => e.Notes).WithOne(e => e.Project);
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
        });
    }
}

// 实体模型
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Note> Notes { get; set; } = new();
}

public class Note
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
}

public class Setting
{
    public string Key { get; set; } = "";
    public string? Value { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### 34.3.2 数据库迁移

```bash
# 创建迁移
dotnet ef migrations add InitialCreate --project MyApp

# 应用迁移
dotnet ef database update --project MyApp

# 在代码中自动迁移（开发环境）
```

```csharp
// 自动迁移（仅开发环境）
public static void MigrateDatabase(AppDbContext context)
{
    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }
}
```

### 34.3.3 在 Avalonia 中使用

```csharp
// 通过依赖注入注册
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, string dbPath)
    {
        services.AddSingleton<AppDbContext>(sp => new AppDbContext(dbPath));
        services.AddSingleton<ProjectRepository>();
        services.AddSingleton<NoteRepository>();
        return services;
    }
}

// Repository 模式
public class ProjectRepository
{
    private readonly AppDbContext _db;

    public ProjectRepository(AppDbContext db) => _db = db;

    public async Task<List<Project>> GetAllAsync()
    {
        return await _db.Projects
            .Include(p => p.Notes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Project> CreateAsync(string name, string? description = null)
    {
        var project = new Project { Name = name, Description = description };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task DeleteAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project != null)
        {
            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
        }
    }
}
```

```csharp
// ViewModel 中使用
public partial class ProjectsViewModel : ViewModelBase
{
    private readonly ProjectRepository _repo;

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();

    public ProjectsViewModel(ProjectRepository repo)
    {
        _repo = repo;
        _ = LoadProjects();
    }

    private async Task LoadProjects()
    {
        var projects = await _repo.GetAllAsync();
        Projects = new ObservableCollection<Project>(projects);
    }

    [RelayCommand]
    private async Task AddProject()
    {
        var project = await _repo.CreateAsync("New Project");
        Projects.Add(project);
    }
}
```

### 34.3.4 异步操作与 UI 线程

```csharp
// 重要：数据库操作必须异步执行，不能阻塞 UI 线程
public async Task<List<Project>> SearchProjects(string keyword)
{
    // EF Core 的 ToListAsync 是异步的
    return await _db.Projects
        .Where(p => p.Name.Contains(keyword))
        .ToListAsync();
}

// 在后台线程执行大量数据操作
public async Task ImportData(IEnumerable<Project> projects)
{
    await Task.Run(async () =>
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var batch in projects.Chunk(100))
            {
                _db.Projects.AddRange(batch);
                await _db.SaveChangesAsync();
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    });

    // 回到 UI 线程刷新列表
    await Dispatcher.UIThread.InvokeAsync(() => LoadProjects());
}
```

## 34.4 凭据安全存储

### 34.4.1 跨平台凭据管理接口

```csharp
public interface ISecureStorage
{
    void Save(string key, string value);
    string? Read(string key);
    void Delete(string key);
    bool Exists(string key);
}
```

### 34.4.2 Windows 凭据管理器

```csharp
using System.Runtime.InteropServices;

public class WindowsSecureStorage : ISecureStorage
{
    // 使用 Windows Credential Manager API
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr buffer);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    private const uint CRED_TYPE_GENERIC = 1;
    private const uint CRED_PERSIST_LOCAL_MACHINE = 2;
    private const string AppPrefix = "MyApp_";

    public void Save(string key, string value)
    {
        var targetName = AppPrefix + key;
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var blob = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, blob, bytes.Length);
            var credential = new CREDENTIAL
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = targetName,
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = blob,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                UserName = Environment.UserName
            };
            CredWrite(ref credential, 0);
        }
        finally
        {
            Marshal.FreeHGlobal(blob);
        }
    }

    public string? Read(string key)
    {
        var targetName = AppPrefix + key;
        if (!CredRead(targetName, CRED_TYPE_GENERIC, 0, out var ptr))
            return null;

        try
        {
            var credential = Marshal.PtrToStructure<CREDENTIAL>(ptr);
            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        finally
        {
            CredFree(ptr);
        }
    }

    public void Delete(string key)
    {
        CredDelete(AppPrefix + key, CRED_TYPE_GENERIC, 0);
    }

    public bool Exists(string key) => Read(key) != null;
}
```

### 34.4.3 macOS Keychain

```csharp
public class MacSecureStorage : ISecureStorage
{
    // macOS Keychain 需要通过 Security.framework 的 C API
    // 推荐使用 NuGet 包 Keychain.Net 或类似封装

    // 简化实现：使用文件加密作为后备方案
    private string GetKeyPath(string key)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "Keychains", "MyApp");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, key);
    }

    public void Save(string key, string value)
    {
        // 实际应使用 Security.framework API
        // 这里展示后备方案
        var encrypted = ProtectedData.Protect(
            System.Text.Encoding.UTF8.GetBytes(value),
            null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(GetKeyPath(key), encrypted);
    }

    public string? Read(string key)
    {
        var path = GetKeyPath(key);
        if (!File.Exists(path)) return null;
        var decrypted = ProtectedData.Unprotect(
            File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(decrypted);
    }

    public void Delete(string key)
    {
        var path = GetKeyPath(key);
        if (File.Exists(path)) File.Delete(path);
    }

    public bool Exists(string key) => File.Exists(GetKeyPath(key));
}
```

### 34.4.4 Linux Secret Service

```csharp
public class LinuxSecureStorage : ISecureStorage
{
    // Linux 使用 libsecret (GNOME Keyring) 或 KWallet
    // 推荐使用 NuGet 包 DesktopLinux 或通过 D-Bus 调用

    // 后备方案：使用文件加密
    // 与 macOS 实现类似
}
```

### 34.4.5 平台工厂

```csharp
public static class SecureStorageFactory
{
    public static ISecureStorage Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsSecureStorage();
        if (OperatingSystem.IsMacOS())   return new MacSecureStorage();
        if (OperatingSystem.IsLinux())   return new LinuxSecureStorage();
        throw new PlatformNotSupportedException();
    }
}
```

## 34.5 配置文件管理

### 34.5.1 JSON 配置

```csharp
using System.Text.Json;

public class JsonConfigManager<T> where T : class, new()
{
    private readonly string _filePath;
    private T _data;

    public JsonConfigManager(string filePath)
    {
        _filePath = filePath;
        _data = Load();
    }

    public T Data => _data;

    private T Load()
    {
        if (!File.Exists(_filePath)) return new T();
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<T>(json) ?? new T();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // 原子写入：先写临时文件，再替换
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
```

### 34.5.2 TOML 配置

```csharp
// NuGet: Tommy 或 Tomlyn
using Tomlyn;

public class TomlConfigManager
{
    public T Load<T>(string filePath) where T : class, new()
    {
        if (!File.Exists(filePath)) return new T();
        var content = File.ReadAllText(filePath);
        return Toml.ToModel<T>(content);
    }

    public void Save<T>(string filePath, T data) where T : class
    {
        var toml = Toml.FromModel(data);
        File.WriteAllText(filePath, toml);
    }
}
```

## 34.6 日志系统

### 34.6.1 Serilog 集成

```xml
<PackageReference Include="Serilog" Version="4.*" />
<PackageReference Include="Serilog.Sinks.File" Version="6.*" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.*" />
```

```csharp
using Serilog;

public static class LoggingSetup
{
    public static void Configure(string logDir)
    {
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logDir, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}

// 在 App 启动时配置
public partial class App : Application
{
    public override void Initialize()
    {
        var logDir = Path.Combine(
            DatabasePaths.GetAppDataPath("MyApp"), "Logs");
        LoggingSetup.Configure(logDir);

        Log.Information("应用启动");

        AvaloniaXamlLoader.Load(this);
    }
}
```

## 34.7 Cross References

- **第 2 章**：项目结构与启动流程（依赖注入注册数据库服务）
- **第 6 章**：MVVM 模式实战（Repository 模式与 ViewModel 集成）
- **第 25 章**：ASP.NET Core 集成（EF Core 在 Web API 中的使用）

## 34.8 Common Pitfalls

1. **UI 线程阻塞**：数据库操作必须异步执行，绝不能在 UI 线程同步查询
2. **数据库文件锁定**：SQLite 不支持多进程同时写入
3. **迁移丢失**：发布新版本时忘记打包迁移文件
4. **凭据存储不安全**：不要用明文存储密码和 Token
5. **配置文件冲突**：多实例同时写入配置文件可能导致数据损坏
6. **日志文件膨胀**：必须设置 `retainedFileCountLimit`

## 34.9 Try It Yourself

1. 创建一个带 SQLite 数据库的笔记应用，实现 CRUD 操作
2. 使用 EF Core 实现数据迁移
3. 实现跨平台的安全存储，保存 OAuth Token
4. 集成 Serilog，实现日志文件自动轮转
