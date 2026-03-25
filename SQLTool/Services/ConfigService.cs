// SQLTool/Services/ConfigService.cs
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SQLTool.Models;

namespace SQLTool.Services;

public class ConfigService : IConfigService
{
    private readonly string _configDir;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    private List<QueryDefinition> _queries = new();
    private List<EnvironmentConfig> _environments = new();
    private readonly object _lock = new();

    public ConfigService(string configDir, IConfiguration configuration, ILogger<ConfigService> logger)
    {
        _configDir = configDir;
        _configuration = configuration;
        _logger = logger;
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
                try
                {
                    var json = File.ReadAllText(queriesPath);
                    var root = JsonSerializer.Deserialize<QueryConfigRoot>(json, _jsonOptions);
                    _queries = root?.Queries ?? new();
                    _logger.LogInformation("Loaded {Count} queries from {Path}", _queries.Count, queriesPath);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse {Path} — keeping previous query definitions", queriesPath);
                }
            }

            if (File.Exists(envsPath))
            {
                try
                {
                    var json = File.ReadAllText(envsPath);
                    var root = JsonSerializer.Deserialize<EnvironmentConfigRoot>(json, _jsonOptions);
                    _environments = root?.Environments ?? new();
                    foreach (var env in _environments)
                        foreach (var db in env.Databases)
                            db.EnvironmentName = env.Name;
                    _logger.LogInformation("Loaded {Count} environments from {Path}", _environments.Count, envsPath);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse {Path} — keeping previous environment definitions", envsPath);
                }
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

    public void EnableQuery(string queryId)
    {
        lock (_lock)
        {
            var q = _queries.FirstOrDefault(q => q.Id == queryId);
            if (q is not null) q.Enabled = true;
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
        Directory.CreateDirectory(_configDir);
        var path = Path.Combine(_configDir, "queries.json");
        var tmp = path + ".tmp";
        try
        {
            File.WriteAllText(tmp, JsonSerializer.Serialize(new QueryConfigRoot { Queries = _queries }, _jsonOptions));
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(tmp)) File.Delete(tmp);
            throw;
        }
    }

    private class QueryConfigRoot { public List<QueryDefinition> Queries { get; set; } = new(); }
    private class EnvironmentConfigRoot { public List<EnvironmentConfig> Environments { get; set; } = new(); }
}
