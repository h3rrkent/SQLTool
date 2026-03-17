// SQLTool/Models/EnvironmentConfig.cs
namespace SQLTool.Models;

public class EnvironmentConfig
{
    public string Name { get; set; } = "";
    public List<DatabaseEntry> Databases { get; set; } = new();
}
