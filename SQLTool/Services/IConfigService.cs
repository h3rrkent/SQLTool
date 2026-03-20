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
    void EnableQuery(string queryId);
    string? GetConnectionString(string connectionStringKey);
}
