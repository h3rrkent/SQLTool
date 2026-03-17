// SQLTool/Services/IExportService.cs
using SQLTool.Models;
namespace SQLTool.Services;
public interface IExportService
{
    string ToCsv(QueryResult result);
    byte[] ToExcel(QueryResult result, string sheetName = "Results");
}
