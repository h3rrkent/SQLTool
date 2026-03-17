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
