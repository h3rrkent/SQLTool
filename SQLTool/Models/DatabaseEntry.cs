// SQLTool/Models/DatabaseEntry.cs
using System.Text.Json.Serialization;

namespace SQLTool.Models;

public class DatabaseEntry
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string ConnectionStringKey { get; set; } = "";
    public int QueryTimeoutSeconds { get; set; } = 30;
    [JsonIgnore]
    public string EnvironmentName { get; set; } = "";
}
