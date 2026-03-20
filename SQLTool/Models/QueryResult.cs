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
