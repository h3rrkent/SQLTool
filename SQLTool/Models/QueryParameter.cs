// SQLTool/Models/QueryParameter.cs
namespace SQLTool.Models;

public class QueryParameter
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "text"; // date | number | text | boolean | dropdown
    public bool Required { get; set; } = true;
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public List<DropdownOption> Options { get; set; } = new();
}

public class DropdownOption
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}
