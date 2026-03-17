# SQLTool Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Blazor Server web app that lets internal non-technical users run pre-defined SQL queries against multiple environments and databases, secured by Microsoft Entra ID.

**Architecture:** ASP.NET Core 8 Blazor Server with a 4-step wizard UI (Environment → Database → Query+Parameters → Results). Services are injected via DI: ConfigService loads JSON config, QueryEngine executes parameterized SQL via Dapper, ExportService generates CSV/Excel. Authentication uses Microsoft.Identity.Web with two app roles (SQLTool.User, SQLTool.Admin).

**Tech Stack:** .NET 8, Blazor Server, Dapper, Microsoft.Identity.Web, ClosedXML, CsvHelper, xUnit, Moq

---

## File Map

```
SQLTool/
├── SQLTool.sln
├── SQLTool/
│   ├── SQLTool.csproj
│   ├── Program.cs                         startup, DI, auth
│   ├── appsettings.json                   app settings (no connection strings)
│   ├── appsettings.Development.json       dev overrides
│   ├── config/
│   │   ├── queries.json                   query definitions
│   │   └── environments.json              environment/database definitions
│   ├── Models/
│   │   ├── QueryDefinition.cs             single query + parameters
│   │   ├── QueryParameter.cs              parameter definition (name, type, options)
│   │   ├── EnvironmentConfig.cs           environment + database list
│   │   ├── DatabaseEntry.cs               single database entry (id, label, key)
│   │   ├── QueryResult.cs                 execution result (rows, timing, truncated)
│   │   └── ParameterValue.cs              runtime parameter value submitted by user
│   ├── Services/
│   │   ├── IConfigService.cs              interface: load/save/reload config
│   │   ├── ConfigService.cs               implementation
│   │   ├── IQueryEngine.cs                interface: validate + execute
│   │   ├── QueryEngine.cs                 implementation via Dapper
│   │   ├── IExportService.cs              interface: ToCSV / ToExcel
│   │   └── ExportService.cs               implementation via CsvHelper + ClosedXML
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   ├── _Imports.razor
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor           top nav, user display, admin link
│   │   │   └── MainLayout.razor.css
│   │   ├── Pages/
│   │   │   ├── Home.razor                 4-step wizard (all steps on one page)
│   │   │   ├── AccessDenied.razor         no-role error page
│   │   │   └── Admin/
│   │   │       ├── ManageQueries.razor    query list + inline edit + param editor
│   │   │       └── Environments.razor     read-only env/db view
│   │   └── Shared/
│   │       ├── StepBar.razor              step progress indicator
│   │       ├── DatabaseSelector.razor     searchable database list (step 2)
│   │       ├── QuerySelector.razor        searchable query list (step 3)
│   │       ├── ParameterForm.razor        dynamic parameter inputs (step 3)
│   │       ├── ResultsTable.razor         table + export buttons (step 4)
│   │       ├── ProdWarningBanner.razor    PROD warning (step 3+4)
│   │       └── RedirectToLogin.razor      redirects unauthenticated users to Microsoft login
└── SQLTool.Tests/
    ├── SQLTool.Tests.csproj
    ├── Services/
    │   ├── ConfigServiceTests.cs
    │   ├── QueryEngineTests.cs
    │   └── ExportServiceTests.cs
    └── Validation/
        └── ParameterValidationTests.cs
```

---

## Task 1: Project Scaffolding

**Files:**
- Create: `SQLTool.sln`
- Create: `SQLTool/SQLTool.csproj`
- Create: `SQLTool.Tests/SQLTool.Tests.csproj`

- [ ] **Step 1: Create the solution and Blazor Server project**

```bash
cd /Users/kennethreijnders/Code/SQLTool
dotnet new sln -n SQLTool
dotnet new blazorserver -n SQLTool -o SQLTool --no-https false --auth None
dotnet sln add SQLTool/SQLTool.csproj
```

Expected: Solution created, `SQLTool/` folder with Blazor Server project.

- [ ] **Step 2: Create the test project**

```bash
dotnet new xunit -n SQLTool.Tests -o SQLTool.Tests
dotnet sln add SQLTool.Tests/SQLTool.Tests.csproj
dotnet add SQLTool.Tests/SQLTool.Tests.csproj reference SQLTool/SQLTool.csproj
```

- [ ] **Step 3: Add NuGet packages to main project**

```bash
cd SQLTool
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient
dotnet add package ClosedXML
dotnet add package CsvHelper
```

- [ ] **Step 4: Add NuGet packages to test project**

```bash
cd ../SQLTool.Tests
dotnet add package Moq
dotnet add package FluentAssertions
```

- [ ] **Step 5: Verify the project builds**

```bash
cd /Users/kennethreijnders/Code/SQLTool
dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Add .gitignore and commit**

```bash
dotnet new gitignore
echo ".superpowers/" >> .gitignore
git add .
git commit -m "chore: scaffold Blazor Server solution with test project"
```

---

## Task 2: Models

**Files:**
- Create: `SQLTool/Models/QueryParameter.cs`
- Create: `SQLTool/Models/QueryDefinition.cs`
- Create: `SQLTool/Models/DatabaseEntry.cs`
- Create: `SQLTool/Models/EnvironmentConfig.cs`
- Create: `SQLTool/Models/ParameterValue.cs`
- Create: `SQLTool/Models/QueryResult.cs`

- [ ] **Step 1: Create `QueryParameter.cs`**

```csharp
// SQLTool/Models/QueryParameter.cs
namespace SQLTool.Models;

public class QueryParameter
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "text"; // date | number | text | boolean | dropdown
    public bool Required { get; set; } = true;
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public List<DropdownOption> Options { get; set; } = new();
}

public class DropdownOption
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}
```

- [ ] **Step 2: Create `QueryDefinition.cs`**

```csharp
// SQLTool/Models/QueryDefinition.cs
namespace SQLTool.Models;

public class QueryDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> AllowedEnvironments { get; set; } = new();
    public string Sql { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public List<QueryParameter> Parameters { get; set; } = new();
}
```

- [ ] **Step 3: Create `DatabaseEntry.cs` and `EnvironmentConfig.cs`**

```csharp
// SQLTool/Models/DatabaseEntry.cs
namespace SQLTool.Models;

public class DatabaseEntry
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string ConnectionStringKey { get; set; } = "";
    public int QueryTimeoutSeconds { get; set; } = 30;
}
```

```csharp
// SQLTool/Models/EnvironmentConfig.cs
namespace SQLTool.Models;

public class EnvironmentConfig
{
    public string Name { get; set; } = "";
    public List<DatabaseEntry> Databases { get; set; } = new();
}
```

- [ ] **Step 4: Create `ParameterValue.cs` and `QueryResult.cs`**

```csharp
// SQLTool/Models/ParameterValue.cs
namespace SQLTool.Models;

public class ParameterValue
{
    public string Name { get; set; } = "";
    public string? Value { get; set; }
}
```

```csharp
// SQLTool/Models/QueryResult.cs
namespace SQLTool.Models;

public class QueryResult
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    public int TotalRowCount { get; set; }
    public bool Truncated { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccess => ErrorMessage is null;
}
```

- [ ] **Step 5: Verify build**

```bash
cd /Users/kennethreijnders/Code/SQLTool
dotnet build
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add SQLTool/Models/
git commit -m "feat: add domain models (QueryDefinition, EnvironmentConfig, QueryResult)"
```

---

## Task 3: Config Files & Config Service

**Files:**
- Create: `SQLTool/config/queries.json`
- Create: `SQLTool/config/environments.json`
- Create: `SQLTool/Services/IConfigService.cs`
- Create: `SQLTool/Services/ConfigService.cs`
- Test: `SQLTool.Tests/Services/ConfigServiceTests.cs`

- [ ] **Step 1: Create sample `queries.json`**

```json
// SQLTool/config/queries.json
{
  "queries": [
    {
      "id": "orders-by-date",
      "name": "Get orders by date range",
      "description": "Returns all orders placed between two dates",
      "allowedEnvironments": ["PROD", "TEST"],
      "sql": "SELECT TOP 10000 OrderID, CustomerName, OrderDate, TotalAmount FROM Orders WHERE OrderDate BETWEEN @StartDate AND @EndDate ORDER BY OrderDate DESC",
      "enabled": true,
      "parameters": [
        {
          "name": "StartDate",
          "label": "Start date",
          "type": "date",
          "required": true
        },
        {
          "name": "EndDate",
          "label": "End date",
          "type": "date",
          "required": true
        }
      ]
    }
  ]
}
```

- [ ] **Step 2: Create sample `environments.json`**

```json
// SQLTool/config/environments.json
{
  "environments": [
    {
      "name": "PROD",
      "databases": [
        { "id": "prod-db1", "label": "CustomerDB_NL", "connectionStringKey": "PROD_CustomerDB_NL", "queryTimeoutSeconds": 30 },
        { "id": "prod-db2", "label": "CustomerDB_BE", "connectionStringKey": "PROD_CustomerDB_BE", "queryTimeoutSeconds": 30 }
      ]
    },
    {
      "name": "TEST",
      "databases": [
        { "id": "test-db", "label": "TestDB", "connectionStringKey": "TEST_TestDB", "queryTimeoutSeconds": 60 }
      ]
    },
    {
      "name": "DEV",
      "databases": [
        { "id": "dev-db", "label": "DevDB", "connectionStringKey": "DEV_DevDB", "queryTimeoutSeconds": 120 }
      ]
    }
  ]
}
```

- [ ] **Step 3: Mark config files as content in csproj**

In `SQLTool/SQLTool.csproj`, add inside `<Project>`:

```xml
<ItemGroup>
  <Content Include="config\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

- [ ] **Step 4: Create `IConfigService.cs`**

```csharp
// SQLTool/Services/IConfigService.cs
using SQLTool.Models;

namespace SQLTool.Services;

public interface IConfigService
{
    IReadOnlyList<QueryDefinition> GetQueries();          // enabled only
    IReadOnlyList<QueryDefinition> GetAllQueries();       // all, including disabled (admin use)
    IReadOnlyList<EnvironmentConfig> GetEnvironments();
    void Reload();
    void SaveQuery(QueryDefinition query);
    void DeleteQuery(string queryId, bool permanent = false);
    string? GetConnectionString(string connectionStringKey);
}
```

- [ ] **Step 5: Write failing tests for ConfigService**

```csharp
// SQLTool.Tests/Services/ConfigServiceTests.cs
using FluentAssertions;
using SQLTool.Services;

namespace SQLTool.Tests.Services;

public class ConfigServiceTests
{
    private readonly string _tempDir;

    public ConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    private void WriteFile(string name, string content) =>
        File.WriteAllText(Path.Combine(_tempDir, name), content);

    [Fact]
    public void GetQueries_ReturnsOnlyEnabledByDefault()
    {
        WriteFile("queries.json", """
        {
          "queries": [
            { "id": "q1", "name": "Q1", "enabled": true, "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] },
            { "id": "q2", "name": "Q2", "enabled": false, "sql": "SELECT 2", "allowedEnvironments": ["DEV"], "parameters": [] }
          ]
        }
        """);
        WriteFile("environments.json", """{ "environments": [] }""");

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var svc = new ConfigService(_tempDir, config);

        svc.GetQueries().Should().HaveCount(1).And.Contain(q => q.Id == "q1");
    }

    [Fact]
    public void SaveQuery_PersistsToFile()
    {
        WriteFile("queries.json", """{ "queries": [] }""");
        WriteFile("environments.json", """{ "environments": [] }""");

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var svc = new ConfigService(_tempDir, config);
        var query = new SQLTool.Models.QueryDefinition
        {
            Id = "new-query", Name = "New Query", Sql = "SELECT 1",
            AllowedEnvironments = new() { "DEV" }, Enabled = true
        };

        svc.SaveQuery(query);
        svc.Reload();

        svc.GetQueries().Should().Contain(q => q.Id == "new-query");
    }

    [Fact]
    public void DeleteQuery_SoftDeleteSetsEnabledFalse()
    {
        WriteFile("queries.json", """
        { "queries": [{ "id": "q1", "name": "Q1", "enabled": true, "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] }] }
        """);
        WriteFile("environments.json", """{ "environments": [] }""");

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var svc = new ConfigService(_tempDir, config);

        svc.DeleteQuery("q1", permanent: false);
        svc.Reload();

        svc.GetQueries().Should().BeEmpty(); // enabled:false excluded
    }
}
```

- [ ] **Step 6: Run tests to verify they fail**

```bash
cd /Users/kennethreijnders/Code/SQLTool
dotnet test SQLTool.Tests --filter "ConfigServiceTests" -v minimal
```

Expected: FAIL — `ConfigService` not yet implemented.

- [ ] **Step 7: Implement `ConfigService.cs`**

```csharp
// SQLTool/Services/ConfigService.cs
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SQLTool.Models;

namespace SQLTool.Services;

public class ConfigService : IConfigService
{
    private readonly string _configDir;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    private List<QueryDefinition> _queries = new();
    private List<EnvironmentConfig> _environments = new();
    private readonly object _lock = new();

    public ConfigService(string configDir, IConfiguration configuration)
    {
        _configDir = configDir;
        _configuration = configuration;
        Reload();
    }

    public void Reload()
    {
        lock (_lock)
        {
            var queriesPath = Path.Combine(_configDir, "queries.json");
            var envsPath = Path.Combine(_configDir, "environments.json");

            if (File.Exists(queriesPath))
            {
                var json = File.ReadAllText(queriesPath);
                var root = JsonSerializer.Deserialize<QueryConfigRoot>(json, _jsonOptions);
                _queries = root?.Queries ?? new();
            }

            if (File.Exists(envsPath))
            {
                var json = File.ReadAllText(envsPath);
                var root = JsonSerializer.Deserialize<EnvironmentConfigRoot>(json, _jsonOptions);
                _environments = root?.Environments ?? new();
            }
        }
    }

    public IReadOnlyList<QueryDefinition> GetQueries()
    {
        lock (_lock) { return _queries.Where(q => q.Enabled).ToList(); }
    }

    public IReadOnlyList<QueryDefinition> GetAllQueries()
    {
        lock (_lock) { return _queries.ToList(); }
    }

    public IReadOnlyList<EnvironmentConfig> GetEnvironments()
    {
        lock (_lock) { return _environments.ToList(); }
    }

    public void SaveQuery(QueryDefinition query)
    {
        lock (_lock)
        {
            var existing = _queries.FindIndex(q => q.Id == query.Id);
            if (existing >= 0) _queries[existing] = query;
            else _queries.Add(query);
            PersistQueries();
        }
    }

    public void DeleteQuery(string queryId, bool permanent = false)
    {
        lock (_lock)
        {
            if (permanent)
                _queries.RemoveAll(q => q.Id == queryId);
            else
            {
                var q = _queries.FirstOrDefault(q => q.Id == queryId);
                if (q is not null) q.Enabled = false;
            }
            PersistQueries();
        }
    }

    public string? GetConnectionString(string connectionStringKey) =>
        _configuration.GetConnectionString(connectionStringKey);

    private void PersistQueries()
    {
        var path = Path.Combine(_configDir, "queries.json");
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(new QueryConfigRoot { Queries = _queries }, _jsonOptions));
        File.Move(tmp, path, overwrite: true);
    }

    private class QueryConfigRoot { public List<QueryDefinition> Queries { get; set; } = new(); }
    private class EnvironmentConfigRoot { public List<EnvironmentConfig> Environments { get; set; } = new(); }
}
```

- [ ] **Step 8: Run tests to verify they pass**

```bash
dotnet test SQLTool.Tests --filter "ConfigServiceTests" -v minimal
```

Expected: 3 tests pass.

- [ ] **Step 9: Commit**

```bash
git add SQLTool/config/ SQLTool/Services/IConfigService.cs SQLTool/Services/ConfigService.cs SQLTool.Tests/Services/ConfigServiceTests.cs SQLTool/SQLTool.csproj
git commit -m "feat: add ConfigService with JSON load/save/reload and tests"
```

---

## Task 4: Query Engine

**Files:**
- Create: `SQLTool/Services/IQueryEngine.cs`
- Create: `SQLTool/Services/QueryEngine.cs`
- Test: `SQLTool.Tests/Services/QueryEngineTests.cs`
- Test: `SQLTool.Tests/Validation/ParameterValidationTests.cs`

- [ ] **Step 1: Create `IQueryEngine.cs`**

```csharp
// SQLTool/Services/IQueryEngine.cs
using SQLTool.Models;

namespace SQLTool.Services;

public interface IQueryEngine
{
    ValidationResult ValidateParameters(QueryDefinition query, List<ParameterValue> values);
    Task<QueryResult> ExecuteAsync(QueryDefinition query, DatabaseEntry database, List<ParameterValue> values);
}

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; set; } = new();
}
```

- [ ] **Step 2: Write failing tests**

```csharp
// SQLTool.Tests/Services/QueryEngineTests.cs
using FluentAssertions;
using SQLTool.Models;
using SQLTool.Services;

namespace SQLTool.Tests.Services;

public class QueryEngineTests
{
    private QueryEngine CreateEngine(IConfigService? configService = null)
    {
        configService ??= new Moq.Mock<IConfigService>().Object;
        return new QueryEngine(configService);
    }

    [Fact]
    public void ValidateParameters_RequiredMissing_ReturnsError()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "StartDate", Label = "Start date", Type = "date", Required = true } }
        };
        var result = engine.ValidateParameters(query, new List<ParameterValue>());
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Start date"));
    }

    [Fact]
    public void ValidateParameters_DropdownInvalidValue_ReturnsError()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter
            {
                Name = "Status", Label = "Status", Type = "dropdown", Required = true,
                Options = new() { new DropdownOption { Value = "active", Label = "Active" } }
            }}
        };
        var values = new List<ParameterValue> { new() { Name = "Status", Value = "invalid" } };
        var result = engine.ValidateParameters(query, values);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateParameters_TextTooLong_ReturnsError()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "Search", Label = "Search", Type = "text", Required = true } }
        };
        var values = new List<ParameterValue> { new() { Name = "Search", Value = new string('x', 501) } };
        var result = engine.ValidateParameters(query, values);
        result.IsValid.Should().BeFalse();
    }
}
```

```csharp
// SQLTool.Tests/Validation/ParameterValidationTests.cs
using FluentAssertions;
using SQLTool.Models;
using SQLTool.Services;

namespace SQLTool.Tests.Validation;

public class ParameterValidationTests
{
    [Theory]
    [InlineData("2024-01-01", true)]
    [InlineData("not-a-date", false)]
    [InlineData("", false)]
    public void ValidateDate_ReturnsExpected(string value, bool expected)
    {
        var engine = new QueryEngine(new Moq.Mock<IConfigService>().Object);
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "D", Label = "Date", Type = "date", Required = true } }
        };
        var values = new List<ParameterValue> { new() { Name = "D", Value = value } };
        engine.ValidateParameters(query, values).IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("5", 1, 10, true)]
    [InlineData("0", 1, 10, false)]
    [InlineData("11", 1, 10, false)]
    [InlineData("abc", 1, 10, false)]
    public void ValidateNumber_WithMinMax_ReturnsExpected(string value, decimal min, decimal max, bool expected)
    {
        var engine = new QueryEngine(new Moq.Mock<IConfigService>().Object);
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "N", Label = "Num", Type = "number", Required = true, Min = min, Max = max } }
        };
        var values = new List<ParameterValue> { new() { Name = "N", Value = value } };
        engine.ValidateParameters(query, values).IsValid.Should().Be(expected);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test SQLTool.Tests --filter "QueryEngineTests|ParameterValidationTests" -v minimal
```

Expected: FAIL — `QueryEngine` not yet implemented.

- [ ] **Step 4: Implement `QueryEngine.cs`**

```csharp
// SQLTool/Services/QueryEngine.cs
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using SQLTool.Models;

namespace SQLTool.Services;

public class QueryEngine : IQueryEngine
{
    private const int RowLimit = 10_000;
    private readonly IConfigService _configService;

    public QueryEngine(IConfigService configService) => _configService = configService;

    public ValidationResult ValidateParameters(QueryDefinition query, List<ParameterValue> values)
    {
        var result = new ValidationResult();
        foreach (var param in query.Parameters)
        {
            var submitted = values.FirstOrDefault(v => v.Name == param.Name);
            var raw = submitted?.Value;

            if (param.Required && string.IsNullOrWhiteSpace(raw))
            {
                result.Errors.Add($"{param.Label} is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(raw)) continue;

            switch (param.Type)
            {
                case "date":
                    if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", out _))
                        result.Errors.Add($"{param.Label} must be a valid date (yyyy-MM-dd).");
                    break;
                case "number":
                    if (!decimal.TryParse(raw, out var num))
                        result.Errors.Add($"{param.Label} must be a number.");
                    else
                    {
                        if (param.Min.HasValue && num < param.Min) result.Errors.Add($"{param.Label} must be at least {param.Min}.");
                        if (param.Max.HasValue && num > param.Max) result.Errors.Add($"{param.Label} must be at most {param.Max}.");
                    }
                    break;
                case "text":
                    if (raw.Length > 500) result.Errors.Add($"{param.Label} must be 500 characters or fewer.");
                    break;
                case "dropdown":
                    if (!param.Options.Any(o => o.Value == raw))
                        result.Errors.Add($"{param.Label} has an invalid value.");
                    break;
            }
        }
        return result;
    }

    public async Task<QueryResult> ExecuteAsync(QueryDefinition query, DatabaseEntry database, List<ParameterValue> values)
    {
        var connectionString = _configService.GetConnectionString(database.ConnectionStringKey);
        if (connectionString is null)
            return new QueryResult { ErrorMessage = $"Connection string '{database.ConnectionStringKey}' not configured." };

        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new SqlConnection(connectionString);
            var parameters = new DynamicParameters();
            foreach (var p in query.Parameters)
            {
                var val = values.FirstOrDefault(v => v.Name == p.Name)?.Value;
                parameters.Add(p.Name, CoerceValue(p, val));
            }

            var cmd = new CommandDefinition(query.Sql, parameters, commandTimeout: database.QueryTimeoutSeconds);
            var raw = (await conn.QueryAsync(cmd)).ToList();
            sw.Stop();

            var rows = raw.Take(RowLimit + 1)
                .Select(r => ((IDictionary<string, object?>)r).ToDictionary(k => k.Key, k => k.Value))
                .ToList();

            var truncated = rows.Count > RowLimit;
            if (truncated) rows = rows.Take(RowLimit).ToList();

            var columns = rows.FirstOrDefault()?.Keys.ToList() ?? new();
            return new QueryResult
            {
                Rows = rows,
                Columns = columns,
                TotalRowCount = rows.Count,
                Truncated = truncated,
                ElapsedMilliseconds = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new QueryResult { ErrorMessage = ex.Message };
        }
    }

    private static object? CoerceValue(QueryParameter param, string? raw)
    {
        if (raw is null) return null;
        return param.Type switch
        {
            "date" => DateOnly.TryParseExact(raw, "yyyy-MM-dd", out var d) ? d.ToDateTime(TimeOnly.MinValue) : null,
            "number" => decimal.TryParse(raw, out var n) ? n : null,
            "boolean" => raw.Equals("true", StringComparison.OrdinalIgnoreCase),
            _ => raw
        };
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test SQLTool.Tests --filter "QueryEngineTests|ParameterValidationTests" -v minimal
```

Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add SQLTool/Services/ SQLTool.Tests/Services/QueryEngineTests.cs SQLTool.Tests/Validation/
git commit -m "feat: add QueryEngine with parameter validation and Dapper execution"
```

---

## Task 5: Export Service

**Files:**
- Create: `SQLTool/Services/IExportService.cs`
- Create: `SQLTool/Services/ExportService.cs`
- Test: `SQLTool.Tests/Services/ExportServiceTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
// SQLTool.Tests/Services/ExportServiceTests.cs
using FluentAssertions;
using SQLTool.Models;
using SQLTool.Services;

namespace SQLTool.Tests.Services;

public class ExportServiceTests
{
    private QueryResult SampleResult() => new()
    {
        Columns = new() { "Id", "Name" },
        Rows = new()
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" }
        }
    };

    [Fact]
    public void ToCsv_ContainsHeaderAndRows()
    {
        var svc = new ExportService();
        var csv = svc.ToCsv(SampleResult());
        csv.Should().Contain("Id").And.Contain("Name").And.Contain("Alice").And.Contain("Bob");
    }

    [Fact]
    public void ToExcel_ReturnsByteArray()
    {
        var svc = new ExportService();
        var bytes = svc.ToExcel(SampleResult(), "TestSheet");
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test SQLTool.Tests --filter "ExportServiceTests" -v minimal
```

Expected: FAIL.

- [ ] **Step 3: Implement ExportService**

```csharp
// SQLTool/Services/IExportService.cs
using SQLTool.Models;
namespace SQLTool.Services;
public interface IExportService
{
    string ToCsv(QueryResult result);
    byte[] ToExcel(QueryResult result, string sheetName = "Results");
}
```

```csharp
// SQLTool/Services/ExportService.cs
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using SQLTool.Models;

namespace SQLTool.Services;

public class ExportService : IExportService
{
    public string ToCsv(QueryResult result)
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        foreach (var col in result.Columns) { csv.WriteField(col); }
        csv.NextRecord();
        foreach (var row in result.Rows)
        {
            foreach (var col in result.Columns) { csv.WriteField(row.GetValueOrDefault(col)?.ToString() ?? ""); }
            csv.NextRecord();
        }
        return sb.ToString();
    }

    public byte[] ToExcel(QueryResult result, string sheetName = "Results")
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);
        for (var i = 0; i < result.Columns.Count; i++)
            ws.Cell(1, i + 1).Value = result.Columns[i];

        for (var r = 0; r < result.Rows.Count; r++)
            for (var c = 0; c < result.Columns.Count; c++)
                ws.Cell(r + 2, c + 1).Value = result.Rows[r].GetValueOrDefault(result.Columns[c])?.ToString() ?? "";

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test SQLTool.Tests --filter "ExportServiceTests" -v minimal
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add SQLTool/Services/IExportService.cs SQLTool/Services/ExportService.cs SQLTool.Tests/Services/ExportServiceTests.cs
git commit -m "feat: add ExportService with CSV and Excel export"
```

---

## Task 6: Authentication & Startup

**Files:**
- Modify: `SQLTool/Program.cs`
- Modify: `SQLTool/appsettings.json`
- Create: `SQLTool/appsettings.Development.json`

- [ ] **Step 1: Update `appsettings.json`**

```json
// SQLTool/appsettings.json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourdomain.onmicrosoft.com",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "CallbackPath": "/signin-oidc",
    "ClientSecret": ""
  },
  "ConfigDir": "config",
  "ConnectionStrings": {},
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Note: For local development, fill in real values using `dotnet user-secrets set` instead of editing appsettings.json directly.

- [ ] **Step 2: Replace `Program.cs`**

```csharp
// SQLTool/Program.cs
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SQLTool.Services;

var builder = WebApplication.CreateBuilder(args);

// Auth
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserPolicy", p => p.RequireRole("SQLTool.User", "SQLTool.Admin"));
    options.AddPolicy("AdminPolicy", p => p.RequireRole("SQLTool.Admin"));
    options.FallbackPolicy = options.GetPolicy("UserPolicy");
});

// Services
var configDir = Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["ConfigDir"] ?? "config");
builder.Services.AddSingleton<IConfigService>(_ => new ConfigService(configDir, builder.Configuration));
builder.Services.AddScoped<IQueryEngine, QueryEngine>();
builder.Services.AddScoped<IExportService, ExportService>();

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

- [ ] **Step 3: Build to verify no errors**

```bash
dotnet build SQLTool/SQLTool.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add SQLTool/Program.cs SQLTool/appsettings.json
git commit -m "feat: configure Microsoft Entra ID auth, DI registration, and authorization policies"
```

---

## Task 7: Layout, Routing & Access Denied

**Files:**
- Modify: `SQLTool/Components/Layout/MainLayout.razor`
- Modify: `SQLTool/Components/Routes.razor`
- Create: `SQLTool/Components/Pages/AccessDenied.razor`

- [ ] **Step 1: Create `RedirectToLogin.razor` shared component**

```razor
@* SQLTool/Components/Shared/RedirectToLogin.razor *@
@inject NavigationManager Navigation

@code {
    protected override void OnInitialized()
    {
        Navigation.NavigateTo($"MicrosoftIdentity/Account/SignIn?redirectUri={Uri.EscapeDataString(Navigation.Uri)}", forceLoad: true);
    }
}
```

- [ ] **Step 2: Update `MainLayout.razor`**

```razor
@* SQLTool/Components/Layout/MainLayout.razor *@
@inherits LayoutComponentBase
@inject NavigationManager Nav
@using Microsoft.AspNetCore.Components.Authorization

<AuthorizeView>
    <Authorized>
        <nav class="sqltool-nav">
            <span class="brand">SQLTool</span>
            <div class="nav-links">
                <a href="/">Run Query</a>
                <AuthorizeView Roles="SQLTool.Admin">
                    <a href="/admin/queries">Manage Queries</a>
                    <a href="/admin/environments">Environments</a>
                </AuthorizeView>
            </div>
            <span class="user-info">
                👤 @context.User.Identity?.Name
                <a href="/MicrosoftIdentity/Account/SignOut">Sign out</a>
            </span>
        </nav>
        <main>
            @Body
        </main>
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>
```

- [ ] **Step 3: Create `AccessDenied.razor`**

```razor
@* SQLTool/Components/Pages/AccessDenied.razor *@
@page "/access-denied"
@layout MainLayout

<div class="access-denied">
    <h2>Access Denied</h2>
    <p>Your account does not have access to SQLTool.</p>
    <p>Please contact your administrator to be assigned the <strong>SQLTool.User</strong> or <strong>SQLTool.Admin</strong> role in Microsoft Entra ID.</p>
</div>
```

- [ ] **Step 4: Verify build**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 5: Commit**

```bash
git add SQLTool/Components/
git commit -m "feat: add main layout with nav, auth display, and access denied page"
```

---

## Task 8: Home Page — Steps 1 & 2 (Environment & Database Selection)

**Files:**
- Create: `SQLTool/Components/Shared/StepBar.razor`
- Create: `SQLTool/Components/Shared/DatabaseSelector.razor`
- Modify: `SQLTool/Components/Pages/Home.razor`

- [ ] **Step 1: Create `StepBar.razor`**

```razor
@* SQLTool/Components/Shared/StepBar.razor *@
<div class="step-bar">
    @for (var i = 0; i < Steps.Count; i++)
    {
        var idx = i;
        var label = Steps[i];
        var isCurrent = idx == CurrentStep;
        var isDone = idx < CurrentStep;
        <span class="step @(isCurrent ? "current" : "") @(isDone ? "done" : "")"
              @onclick="() => { if (isDone && OnStepClick.HasDelegate) OnStepClick.InvokeAsync(idx); }">
            @(idx + 1). @label
        </span>
        @if (i < Steps.Count - 1) { <span class="arrow">→</span> }
    }
</div>

@code {
    [Parameter] public List<string> Steps { get; set; } = new();
    [Parameter] public int CurrentStep { get; set; }
    [Parameter] public EventCallback<int> OnStepClick { get; set; }
}
```

- [ ] **Step 2: Create `DatabaseSelector.razor`**

```razor
@* SQLTool/Components/Shared/DatabaseSelector.razor *@
<div class="db-selector">
    <input type="text" placeholder="🔍 Search database..." @bind="searchTerm" @bind:event="oninput" class="search-input" />
    <div class="db-list">
        @foreach (var db in Filtered)
        {
            <div class="db-item @(db.Id == SelectedId ? "selected" : "")" @onclick="() => OnSelect.InvokeAsync(db)">
                @db.Label
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public List<SQLTool.Models.DatabaseEntry> Databases { get; set; } = new();
    [Parameter] public string? SelectedId { get; set; }
    [Parameter] public EventCallback<SQLTool.Models.DatabaseEntry> OnSelect { get; set; }

    private string searchTerm = "";
    private IEnumerable<SQLTool.Models.DatabaseEntry> Filtered =>
        Databases.Where(d => string.IsNullOrEmpty(searchTerm) || d.Label.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **Step 3: Build initial Home.razor with steps 1 & 2**

```razor
@* SQLTool/Components/Pages/Home.razor *@
@page "/"
@attribute [Authorize(Policy = "UserPolicy")]
@using SQLTool.Models
@using SQLTool.Services
@inject IConfigService ConfigService

<StepBar Steps="@_steps" CurrentStep="@_currentStep" OnStepClick="GoToStep" />

@if (_currentStep == 0)
{
    <div class="step-content">
        <h3>Select Environment</h3>
        <div class="env-buttons">
            @foreach (var env in _environments)
            {
                <button class="env-btn @(_selectedEnv?.Name == env.Name ? "active" : "")"
                        @onclick="() => SelectEnvironment(env)">
                    @env.Name
                </button>
            }
        </div>
    </div>
}
else if (_currentStep == 1)
{
    <div class="step-content">
        <h3>Select Database — @_selectedEnv!.Name</h3>
        <DatabaseSelector Databases="@_selectedEnv.Databases" SelectedId="@_selectedDb?.Id"
                         OnSelect="SelectDatabase" />
    </div>
}

@code {
    private List<string> _steps = new() { "Environment", "Database", "Query", "Results" };
    private int _currentStep = 0;
    private List<EnvironmentConfig> _environments = new();
    private EnvironmentConfig? _selectedEnv;
    private DatabaseEntry? _selectedDb;

    protected override void OnInitialized()
    {
        _environments = ConfigService.GetEnvironments().ToList();
    }

    private void SelectEnvironment(EnvironmentConfig env)
    {
        _selectedEnv = env;
        _selectedDb = null;
        _currentStep = 1;
    }

    private void SelectDatabase(DatabaseEntry db)
    {
        _selectedDb = db;
        _currentStep = 2;
    }

    private void GoToStep(int step)
    {
        if (step < _currentStep) _currentStep = step;
    }
}
```

- [ ] **Step 4: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 5: Commit**

```bash
git add SQLTool/Components/
git commit -m "feat: add steps 1 and 2 (environment and database selection)"
```

---

## Task 9: Home Page — Step 3 (Query Selection & Parameters)

**Files:**
- Create: `SQLTool/Components/Shared/QuerySelector.razor`
- Create: `SQLTool/Components/Shared/ParameterForm.razor`
- Create: `SQLTool/Components/Shared/ProdWarningBanner.razor`
- Modify: `SQLTool/Components/Pages/Home.razor`

- [ ] **Step 1: Create `ProdWarningBanner.razor`**

```razor
@* SQLTool/Components/Shared/ProdWarningBanner.razor *@
@if (Show)
{
    <div class="prod-warning">
        ⚠️ This query runs on <strong>PROD</strong>. Results are read-only.
    </div>
}
@code { [Parameter] public bool Show { get; set; } }
```

- [ ] **Step 2: Create `QuerySelector.razor`**

```razor
@* SQLTool/Components/Shared/QuerySelector.razor *@
<div class="query-selector">
    <input type="text" placeholder="🔍 Search queries..." @bind="searchTerm" @bind:event="oninput" class="search-input" />
    <div class="query-list">
        @foreach (var q in Filtered)
        {
            <div class="query-item @(q.Id == SelectedId ? "selected" : "")" @onclick="() => OnSelect.InvokeAsync(q)">
                <strong>@q.Name</strong>
                @if (!string.IsNullOrEmpty(q.Description))
                {
                    <small>@q.Description</small>
                }
            </div>
        }
    </div>
</div>
@code {
    [Parameter] public List<SQLTool.Models.QueryDefinition> Queries { get; set; } = new();
    [Parameter] public string? SelectedId { get; set; }
    [Parameter] public EventCallback<SQLTool.Models.QueryDefinition> OnSelect { get; set; }
    private string searchTerm = "";
    private IEnumerable<SQLTool.Models.QueryDefinition> Filtered =>
        Queries.Where(q => string.IsNullOrEmpty(searchTerm) || q.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **Step 3: Create `ParameterForm.razor`**

```razor
@* SQLTool/Components/Shared/ParameterForm.razor *@
@using SQLTool.Models

@foreach (var param in Parameters)
{
    <div class="param-field">
        <label>@param.Label @(param.Required ? "*" : "")</label>
        @switch (param.Type)
        {
            case "date":
                <input type="date" value="@GetValue(param.Name)"
                       @onchange="e => SetValue(param.Name, e.Value?.ToString())" />
                break;
            case "number":
                <input type="number" value="@GetValue(param.Name)"
                       min="@param.Min" max="@param.Max"
                       @onchange="e => SetValue(param.Name, e.Value?.ToString())" />
                break;
            case "boolean":
                <select @onchange="e => SetValue(param.Name, e.Value?.ToString())">
                    <option value="false">No</option>
                    <option value="true">Yes</option>
                </select>
                break;
            case "dropdown":
                <select @onchange="e => SetValue(param.Name, e.Value?.ToString())">
                    <option value="">-- Select --</option>
                    @foreach (var opt in param.Options)
                    {
                        <option value="@opt.Value">@opt.Label</option>
                    }
                </select>
                break;
            default:
                <input type="text" maxlength="500" value="@GetValue(param.Name)"
                       @onchange="e => SetValue(param.Name, e.Value?.ToString())" />
                break;
        }
    </div>
}

@code {
    [Parameter] public List<QueryParameter> Parameters { get; set; } = new();
    [Parameter] public List<ParameterValue> Values { get; set; } = new();
    [Parameter] public EventCallback<List<ParameterValue>> ValuesChanged { get; set; }

    private string? GetValue(string name) => Values.FirstOrDefault(v => v.Name == name)?.Value;

    private void SetValue(string name, string? value)
    {
        var existing = Values.FirstOrDefault(v => v.Name == name);
        if (existing is not null) existing.Value = value;
        else Values.Add(new ParameterValue { Name = name, Value = value });
        ValuesChanged.InvokeAsync(Values);
    }
}
```

- [ ] **Step 4: Add step 3 to `Home.razor`**

Extend the `@if` chain in `Home.razor` to add the step 3 section:

```razor
else if (_currentStep == 2)
{
    <div class="step-content step3">
        <ProdWarningBanner Show="@(_selectedEnv!.Name == "PROD")" />
        <div class="step3-layout">
            <div class="query-panel">
                <h3>Select Query</h3>
                <QuerySelector Queries="@_queries" SelectedId="@_selectedQuery?.Id" OnSelect="SelectQuery" />
            </div>
            @if (_selectedQuery is not null)
            {
                <div class="params-panel">
                    <h3>Parameters</h3>
                    <ParameterForm Parameters="_selectedQuery.Parameters" @bind-Values="_paramValues" />
                    @if (_validationErrors.Any())
                    {
                        <ul class="validation-errors">
                            @foreach (var e in _validationErrors) { <li>@e</li> }
                        </ul>
                    }
                    <button class="run-btn" @onclick="RunQuery" disabled="@_isRunning">
                        @(_isRunning ? "Running..." : "▶ Run Query")
                    </button>
                </div>
            }
        </div>
    </div>
}
```

Also add to the `@code` block:

```csharp
private List<QueryDefinition> _queries = new();
private QueryDefinition? _selectedQuery;
private List<ParameterValue> _paramValues = new();
private List<string> _validationErrors = new();
private bool _isRunning = false;
```

And inject:

```csharp
@inject IQueryEngine QueryEngine
```

Update `SelectDatabase` to also populate the filtered query list for the selected environment:

```csharp
private void SelectDatabase(DatabaseEntry db)
{
    _selectedDb = db;
    // Load queries allowed for the selected environment
    _queries = ConfigService.GetQueries()
        .Where(q => q.AllowedEnvironments.Contains(_selectedEnv!.Name, StringComparer.OrdinalIgnoreCase))
        .ToList();
    _selectedQuery = null;
    _paramValues = new();
    _validationErrors = new();
    _currentStep = 2;
}
```

And add the query selection method:

```csharp
private void SelectQuery(QueryDefinition q)
{
    _selectedQuery = q;
    _paramValues = new();
    _validationErrors = new();
}
```

- [ ] **Step 5: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 6: Commit**

```bash
git add SQLTool/Components/
git commit -m "feat: add step 3 query selection and parameter form"
```

---

## Task 10: Home Page — Step 4 (Results & Export)

**Files:**
- Create: `SQLTool/Components/Shared/ResultsTable.razor`
- Modify: `SQLTool/Components/Pages/Home.razor`

- [ ] **Step 1: Create `ResultsTable.razor`**

```razor
@* SQLTool/Components/Shared/ResultsTable.razor *@
@using SQLTool.Models
@inject SQLTool.Services.IExportService ExportService
@inject IJSRuntime JS

@if (Result is null) { return; }

<div class="results-header">
    <span class="row-count">
        @Result.TotalRowCount rows
        @if (Result.Truncated) { <span class="truncated-warning">⚠️ Truncated to 10,000 rows</span> }
        — @Result.ElapsedMilliseconds ms
    </span>
    <div class="export-buttons">
        <button @onclick="ExportCsv">📥 Export CSV</button>
        <button @onclick="ExportExcel">📊 Export Excel</button>
    </div>
</div>

<div class="results-table-wrap">
    <table class="results-table">
        <thead>
            <tr>@foreach (var col in Result.Columns) { <th>@col</th> }</tr>
        </thead>
        <tbody>
            @foreach (var row in Result.Rows)
            {
                <tr>@foreach (var col in Result.Columns) { <td>@row.GetValueOrDefault(col)?.ToString()</td> }</tr>
            }
        </tbody>
    </table>
</div>

@code {
    [Parameter] public QueryResult? Result { get; set; }
    [Parameter] public string QueryName { get; set; } = "results";

    private async Task ExportCsv()
    {
        var csv = ExportService.ToCsv(Result!);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        await JS.InvokeVoidAsync("downloadFile", $"{QueryName}.csv", "text/csv", bytes);
    }

    private async Task ExportExcel()
    {
        var bytes = ExportService.ToExcel(Result!, QueryName);
        await JS.InvokeVoidAsync("downloadFile", $"{QueryName}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes);
    }
}
```

- [ ] **Step 2: Add JS download helper to `wwwroot/index.html` (or `_Host.cshtml`)**

Add before `</body>`:

```html
<script>
  window.downloadFile = function(filename, mimeType, bytes) {
    const blob = new Blob([new Uint8Array(bytes)], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename; a.click();
    URL.revokeObjectURL(url);
  };
</script>
```

- [ ] **Step 3: Add step 4 and RunQuery to `Home.razor`**

Extend `@if` chain:

```razor
else if (_currentStep == 3)
{
    <div class="step-content">
        <ProdWarningBanner Show="@(_selectedEnv!.Name == "PROD")" />
        <ResultsTable Result="_queryResult" QueryName="@(_selectedQuery?.Name ?? "results")" />
    </div>
}
```

Add to `@code`:

```csharp
private QueryResult? _queryResult;
```

Add `RunQuery` method:

```csharp
private async Task RunQuery()
{
    _validationErrors = new();
    var validation = QueryEngine.ValidateParameters(_selectedQuery!, _paramValues);
    if (!validation.IsValid)
    {
        _validationErrors = validation.Errors;
        return;
    }
    // Server-side allowedEnvironments check
    if (!_selectedQuery!.AllowedEnvironments.Contains(_selectedEnv!.Name, StringComparer.OrdinalIgnoreCase))
    {
        _validationErrors = new() { "This query is not allowed in the selected environment." };
        return;
    }
    _isRunning = true;
    _queryResult = await QueryEngine.ExecuteAsync(_selectedQuery!, _selectedDb!, _paramValues);
    _isRunning = false;
    _currentStep = 3;
}
```

- [ ] **Step 4: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 5: Commit**

```bash
git add SQLTool/Components/
git commit -m "feat: add step 4 results table with CSV and Excel export"
```

---

## Task 11: Admin — Manage Queries

**Files:**
- Create: `SQLTool/Components/Pages/Admin/ManageQueries.razor`

- [ ] **Step 1: Create `ManageQueries.razor`**

```razor
@* SQLTool/Components/Pages/Admin/ManageQueries.razor *@
@page "/admin/queries"
@attribute [Authorize(Policy = "AdminPolicy")]
@using SQLTool.Models
@using SQLTool.Services
@inject IConfigService ConfigService
@inject NavigationManager Nav

<h2>Manage Queries</h2>
<div class="admin-header">
    <span class="config-hint">Source: queries.json · @_queries.Count queries</span>
    <button @onclick="ReloadConfig">🔄 Reload Config</button>
    <button @onclick="AddNew">+ Add Query</button>
</div>

@foreach (var q in _allQueries)
{
    <div class="query-row @(!q.Enabled ? "disabled" : "")">
        <div class="query-info">
            <strong>@q.Name</strong>
            <small>@q.Parameters.Count param(s) · @string.Join(", ", q.AllowedEnvironments)</small>
        </div>
        <span class="status-badge @(q.Enabled ? "active" : "inactive")">@(q.Enabled ? "Active" : "Disabled")</span>
        <button @onclick="() => StartEdit(q)">✏ Edit</button>
        <button class="danger" @onclick="() => SoftDelete(q.Id)">Disable</button>
        <button class="danger" @onclick="() => HardDelete(q.Id)">Remove permanently</button>
    </div>

    @if (_editingId == q.Id)
    {
        <div class="edit-form">
            <div class="form-row">
                <label>Display Name</label>
                <input @bind="_draft!.Name" />
            </div>
            <div class="form-row">
                <label>Description</label>
                <input @bind="_draft!.Description" />
            </div>
            <div class="form-row">
                <label>Allowed Environments</label>
                @foreach (var env in _allEnvNames)
                {
                    <label><input type="checkbox" checked="@_draft!.AllowedEnvironments.Contains(env)"
                                  @onchange="e => ToggleEnv(env, (bool)(e.Value ?? false))" /> @env</label>
                }
            </div>
            <div class="form-row">
                <label>SQL</label>
                <textarea @bind="_draft!.Sql" rows="5"></textarea>
                <small>Use @ParameterName for parameters</small>
            </div>
            <div class="params-editor">
                <h4>Parameters <button @onclick="AddParam">+ Add</button></h4>
                @for (var i = 0; i < _draft!.Parameters.Count; i++)
                {
                    var idx = i;
                    var p = _draft.Parameters[idx];
                    <div class="param-row">
                        <input placeholder="SQL Name (e.g. StartDate)" @bind="p.Name" />
                        <input placeholder="Label (shown to user)" @bind="p.Label" />
                        <select @bind="p.Type">
                            <option value="text">Text</option>
                            <option value="date">Date</option>
                            <option value="number">Number</option>
                            <option value="boolean">Yes/No</option>
                            <option value="dropdown">Dropdown</option>
                        </select>
                        <label><input type="checkbox" @bind="p.Required" /> Required</label>
                        @if (p.Type == "dropdown")
                        {
                            <div class="dropdown-opts">
                                <small>Options (value:label, one per line)</small>
                                <textarea rows="3" @bind="_dropdownOptionsText[idx]"
                                          @bind:event="oninput"
                                          @onchange="() => SyncDropdownOptions(idx)"></textarea>
                            </div>
                        }
                        <button class="danger" @onclick="() => RemoveParam(idx)">🗑</button>
                    </div>
                }
            </div>
            <div class="form-actions">
                <button @onclick="CancelEdit">Cancel</button>
                <button class="primary" @onclick="SaveEdit">💾 Save to config</button>
            </div>
        </div>
    }
}

@code {
    private List<QueryDefinition> _queries = new();
    private List<QueryDefinition> _allQueries = new();
    private List<string> _allEnvNames = new();
    private string? _editingId;
    private QueryDefinition? _draft;
    private Dictionary<int, string> _dropdownOptionsText = new();

    protected override void OnInitialized() => LoadData();

    private void LoadData()
    {
        _allQueries = ConfigService.GetAllQueries().ToList();
        _queries = _allQueries.Where(q => q.Enabled).ToList();
        _allEnvNames = ConfigService.GetEnvironments().Select(e => e.Name).ToList();
    }

    private void ReloadConfig() { ConfigService.Reload(); LoadData(); }

    private void AddNew() => StartEdit(new QueryDefinition { Id = Guid.NewGuid().ToString("N")[..8] });

    private void StartEdit(QueryDefinition q)
    {
        _draft = new QueryDefinition
        {
            Id = q.Id, Name = q.Name, Description = q.Description,
            AllowedEnvironments = new(q.AllowedEnvironments),
            Sql = q.Sql, Enabled = q.Enabled,
            Parameters = q.Parameters.Select(p => new QueryParameter
            {
                Name = p.Name, Label = p.Label, Type = p.Type,
                Required = p.Required, Min = p.Min, Max = p.Max,
                Options = new(p.Options)
            }).ToList()
        };
        _dropdownOptionsText = _draft.Parameters
            .Select((p, i) => (i, string.Join("\n", p.Options.Select(o => $"{o.Value}:{o.Label}"))))
            .ToDictionary(x => x.i, x => x.Item2);
        _editingId = q.Id;
    }

    private void CancelEdit() { _editingId = null; _draft = null; }

    private void SaveEdit()
    {
        ConfigService.SaveQuery(_draft!);
        CancelEdit();
        LoadData();
    }

    private void SoftDelete(string id) { ConfigService.DeleteQuery(id, permanent: false); LoadData(); }
    private void HardDelete(string id) { ConfigService.DeleteQuery(id, permanent: true); LoadData(); }

    private void AddParam() { _draft!.Parameters.Add(new()); _dropdownOptionsText[_draft.Parameters.Count - 1] = ""; }
    private void RemoveParam(int idx) { _draft!.Parameters.RemoveAt(idx); }

    private void ToggleEnv(string env, bool add)
    {
        if (add && !_draft!.AllowedEnvironments.Contains(env)) _draft.AllowedEnvironments.Add(env);
        else if (!add) _draft!.AllowedEnvironments.Remove(env);
    }

    private void SyncDropdownOptions(int idx)
    {
        var lines = _dropdownOptionsText.GetValueOrDefault(idx, "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        _draft!.Parameters[idx].Options = lines
            .Select(l => l.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => new DropdownOption { Value = parts[0].Trim(), Label = parts[1].Trim() })
            .ToList();
    }
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 3: Commit**

```bash
git add SQLTool/Components/Pages/Admin/ SQLTool/Services/
git commit -m "feat: add admin manage queries page with inline parameter editor"
```

---

## Task 12: Admin — Environments View

**Files:**
- Create: `SQLTool/Components/Pages/Admin/Environments.razor`

- [ ] **Step 1: Create `Environments.razor`**

```razor
@* SQLTool/Components/Pages/Admin/Environments.razor *@
@page "/admin/environments"
@attribute [Authorize(Policy = "AdminPolicy")]
@using SQLTool.Services
@inject IConfigService ConfigService

<h2>Environments</h2>
<p class="hint">Connection strings are resolved from environment variables or Azure Key Vault — not stored here.</p>

@foreach (var env in ConfigService.GetEnvironments())
{
    <div class="env-section">
        <h3>@env.Name <span class="db-count">@env.Databases.Count database(s)</span></h3>
        <table class="env-table">
            <thead><tr><th>Label</th><th>Connection String Key</th><th>Timeout</th></tr></thead>
            <tbody>
                @foreach (var db in env.Databases)
                {
                    <tr>
                        <td>@db.Label</td>
                        <td><code>@db.ConnectionStringKey</code></td>
                        <td>@db.QueryTimeoutSeconds s</td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build SQLTool/SQLTool.csproj
```

- [ ] **Step 3: Run all tests**

```bash
dotnet test SQLTool.Tests -v minimal
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add SQLTool/Components/Pages/Admin/Environments.razor
git commit -m "feat: add admin environments view"
```

---

## Task 13: Push to GitHub

- [ ] **Step 1: Add `.gitignore` entries for secrets**

Verify `SQLTool/.gitignore` contains:
```
appsettings.Development.json
**/secrets.json
.superpowers/
```

- [ ] **Step 2: Push `sqltool` branch to GitHub**

```bash
cd /Users/kennethreijnders/Code/SQLTool
git remote set-url origin https://h3rrkent@github.com/h3rrkent/SQLTool.git
git push -u origin sqltool
```

- [ ] **Step 3: Verify on GitHub**

Open `https://github.com/h3rrkent/SQLTool` and confirm the `sqltool` branch is visible with all commits.

---

## Summary

| Task | Deliverable |
|------|------------|
| 1 | .NET 8 solution scaffolded with test project |
| 2 | All domain models |
| 3 | ConfigService — load/save/reload JSON config |
| 4 | QueryEngine — validate + execute parameterized SQL |
| 5 | ExportService — CSV + Excel |
| 6 | Microsoft Entra ID auth + DI wiring |
| 7 | Layout, nav, access denied |
| 8 | Step 1 & 2 — environment + database selection |
| 9 | Step 3 — query selection + parameter form |
| 10 | Step 4 — results table + export |
| 11 | Admin: manage queries + parameter editor |
| 12 | Admin: environments view |
| 13 | Push to GitHub |
