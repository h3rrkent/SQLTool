// SQLTool/Services/IQueryEngine.cs
using SQLTool.Models;

namespace SQLTool.Services;

public interface IQueryEngine
{
    ValidationResult ValidateParameters(QueryDefinition query, List<ParameterValue> values);
    Task<QueryResult> ExecuteAsync(QueryDefinition query, DatabaseEntry database, List<ParameterValue> values);
}

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; set; } = new();
}
