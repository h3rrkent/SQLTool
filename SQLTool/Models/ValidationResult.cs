// SQLTool/Models/ValidationResult.cs
namespace SQLTool.Models;

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; set; } = new();
}
