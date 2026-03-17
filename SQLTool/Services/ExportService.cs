// SQLTool/Services/ExportService.cs
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using SQLTool.Models;

namespace SQLTool.Services;

public class ExportService : IExportService
{
    public string ToCsv(QueryResult result)
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        foreach (var col in result.Columns) { csv.WriteField(col); }
        csv.NextRecord();
        foreach (var row in result.Rows)
        {
            foreach (var col in result.Columns) { csv.WriteField(row.GetValueOrDefault(col)?.ToString() ?? ""); }
            csv.NextRecord();
        }
        return sb.ToString();
    }

    public byte[] ToExcel(QueryResult result, string sheetName = "Results")
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheetName);
        for (var i = 0; i < result.Columns.Count; i++)
            ws.Cell(1, i + 1).Value = result.Columns[i];

        for (var r = 0; r < result.Rows.Count; r++)
            for (var c = 0; c < result.Columns.Count; c++)
                ws.Cell(r + 2, c + 1).Value = result.Rows[r].GetValueOrDefault(result.Columns[c])?.ToString() ?? "";

        ws.Row(1).Style.Font.Bold = true;
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
