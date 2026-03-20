// SQLTool.Tests/Services/ConfigServiceTests.cs
using FluentAssertions;
using SQLTool.Models;
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

    private ConfigService CreateService()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        return new ConfigService(_tempDir, config);
    }

    // --- GetQueries ---

    [Fact]
    public void GetQueries_ReturnsOnlyEnabledByDefault()
    {
        WriteFile("queries.json", """
        {
          "queries": [
            { "id": "q1", "name": "Q1", "enabled": true,  "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] },
            { "id": "q2", "name": "Q2", "enabled": false, "sql": "SELECT 2", "allowedEnvironments": ["DEV"], "parameters": [] }
          ]
        }
        """);
        WriteFile("environments.json", """{ "environments": [] }""");

        var svc = CreateService();

        svc.GetQueries().Should().HaveCount(1).And.Contain(q => q.Id == "q1");
    }

    // --- GetAllQueries ---

    [Fact]
    public void GetAllQueries_ReturnsAllIncludingDisabled()
    {
        WriteFile("queries.json", """
        {
          "queries": [
            { "id": "q1", "name": "Q1", "enabled": true,  "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] },
            { "id": "q2", "name": "Q2", "enabled": false, "sql": "SELECT 2", "allowedEnvironments": ["DEV"], "parameters": [] }
          ]
        }
        """);
        WriteFile("environments.json", """{ "environments": [] }""");

        var svc = CreateService();

        svc.GetAllQueries().Should().HaveCount(2);
    }

    // --- SaveQuery ---

    [Fact]
    public void SaveQuery_PersistsToFile()
    {
        WriteFile("queries.json", """{ "queries": [] }""");
        WriteFile("environments.json", """{ "environments": [] }""");

        var svc = CreateService();
        var query = new QueryDefinition
        {
            Id = "new-query", Name = "New Query", Sql = "SELECT 1",
            AllowedEnvironments = new() { "DEV" }, Enabled = true
        };

        svc.SaveQuery(query);
        svc.Reload();

        svc.GetQueries().Should().Contain(q => q.Id == "new-query");
    }

    [Fact]
    public void SaveQuery_UpdatesExistingQuery_NoDuplicate()
    {
        WriteFile("queries.json", """
        { "queries": [{ "id": "q1", "name": "Old Name", "enabled": true, "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] }] }
        """);
        WriteFile("environments.json", """{ "environments": [] }""");

        var svc = CreateService();
        svc.SaveQuery(new QueryDefinition { Id = "q1", Name = "New Name", Sql = "SELECT 2", AllowedEnvironments = new() { "DEV" }, Enabled = true });
        svc.Reload();

        var all = svc.GetAllQueries();
        all.Should().HaveCount(1);
        all[0].Name.Should().Be("New Name");
    }

    // --- DeleteQuery ---

    [Fact]
    public void DeleteQuery_SoftDeleteSetsEnabledFalse()
    {
        WriteFile("queries.json", """
        { "queries": [{ "id": "q1", "name": "Q1", "enabled": true, "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] }] }
        """);
        WriteFile("environments.json", """{ "environments": [] }""");

        var svc = CreateService();

        svc.DeleteQuery("q1", permanent: false);
        svc.Reload();

        svc.GetQueries().Should().BeEmpty();
        svc.GetAllQueries().Should().HaveCount(1).And.Contain(q => q.Id == "q1");
    }

    [Fact]
    public void DeleteQuery_HardDeleteRemovesQueryPermanently()
    {
        WriteFile("queries.json", """
        { "queries": [{ "id": "q1", "name": "Q1", "enabled": true, "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] }] }
        """);
        WriteFile("environments.json", """{ "environments": [] }""");

        var svc = CreateService();

        svc.DeleteQuery("q1", permanent: true);
        svc.Reload();

        svc.GetAllQueries().Should().BeEmpty();
    }

    // --- EnableQuery ---

    [Fact]
    public void EnableQuery_SetsEnabledTrueAndPersists()
    {
        WriteFile("queries.json", """
        { "queries": [{ "id": "q1", "name": "Q1", "enabled": false, "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] }] }
        """);
        WriteFile("environments.json", """{ "environments": [] }""");

        var svc = CreateService();

        svc.EnableQuery("q1");
        svc.Reload();

        svc.GetQueries().Should().HaveCount(1).And.Contain(q => q.Id == "q1");
    }

    [Fact]
    public void EnableQuery_QueryAlreadyEnabled_RemainsEnabled()
    {
        WriteFile("queries.json", """
        { "queries": [{ "id": "q1", "name": "Q1", "enabled": true, "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] }] }
        """);
        WriteFile("environments.json", """{ "environments": [] }""");

        var svc = CreateService();

        svc.EnableQuery("q1");

        svc.GetQueries().Should().HaveCount(1);
    }

    // --- Reload ---

    [Fact]
    public void Reload_PicksUpFileChanges()
    {
        WriteFile("queries.json", """{ "queries": [] }""");
        WriteFile("environments.json", """{ "environments": [] }""");

        var svc = CreateService();
        svc.GetQueries().Should().BeEmpty();

        WriteFile("queries.json", """
        { "queries": [{ "id": "q1", "name": "Q1", "enabled": true, "sql": "SELECT 1", "allowedEnvironments": ["DEV"], "parameters": [] }] }
        """);
        svc.Reload();

        svc.GetQueries().Should().HaveCount(1);
    }
}
