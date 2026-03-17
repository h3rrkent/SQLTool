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
