// SQLTool/Models/DatabaseEntry.cs
namespace SQLTool.Models;

public class DatabaseEntry
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string ConnectionStringKey { get; set; } = "";
    public int QueryTimeoutSeconds { get; set; } = 30;
}
