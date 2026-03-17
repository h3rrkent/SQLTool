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
