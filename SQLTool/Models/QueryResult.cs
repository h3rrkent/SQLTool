// SQLTool/Models/QueryResult.cs
namespace SQLTool.Models;

public class QueryResult
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    public int RowCount { get; set; }
    public bool Truncated { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccess => ErrorMessage is null;
    public string? TruncationMessage => Truncated
        ? $"Results limited to {RowCount:N0} rows. Refine your query to see all data." : null;
}
