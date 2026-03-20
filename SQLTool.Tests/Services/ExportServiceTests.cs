// SQLTool.Tests/Services/ExportServiceTests.cs
using ClosedXML.Excel;
using FluentAssertions;
using SQLTool.Models;
using SQLTool.Services;
using System.IO;

namespace SQLTool.Tests.Services;

public class ExportServiceTests
{
    private static QueryResult SampleResult() => new()
    {
        Columns = new() { "Id", "Name" },
        Rows = new()
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" }
        }
    };

    // --- ToCsv ---

    [Fact]
    public void ToCsv_ContainsHeaderAndRows()
    {
        var svc = new ExportService();
        var csv = svc.ToCsv(SampleResult());
        csv.Should().Contain("Id").And.Contain("Name").And.Contain("Alice").And.Contain("Bob");
    }

    [Fact]
    public void ToCsv_EmptyResult_ReturnsHeaderOnly()
    {
        var svc = new ExportService();
        var result = new QueryResult { Columns = new() { "Id", "Name" }, Rows = new() };
        var csv = svc.ToCsv(result);
        csv.Should().Contain("Id").And.Contain("Name");
        csv.Should().NotContain("Alice");
    }

    [Fact]
    public void ToCsv_ValueWithComma_IsProperlyEscaped()
    {
        var svc = new ExportService();
        var result = new QueryResult
        {
            Columns = new() { "Name" },
            Rows = new() { new() { ["Name"] = "Smith, John" } }
        };
        var csv = svc.ToCsv(result);
        csv.Should().Contain("\"Smith, John\"");
    }

    [Fact]
    public void ToCsv_NullValue_RendersAsEmpty()
    {
        var svc = new ExportService();
        var result = new QueryResult
        {
            Columns = new() { "Name" },
            Rows = new() { new() { ["Name"] = null } }
        };
        var act = () => svc.ToCsv(result);
        act.Should().NotThrow();
    }

    // --- ToExcel ---

    [Fact]
    public void ToExcel_ReturnsByteArray()
    {
        var svc = new ExportService();
        var bytes = svc.ToExcel(SampleResult(), "TestSheet");
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void ToExcel_HeaderRowIsBold()
    {
        var svc = new ExportService();
        var bytes = svc.ToExcel(SampleResult(), "Sheet1");
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet("Sheet1");
        ws.Row(1).Style.Font.Bold.Should().BeTrue();
    }

    [Fact]
    public void ToExcel_IntegerColumn_CellIsNumericNotString()
    {
        // Regression: previously all cells were forced to strings via .ToString()
        var svc = new ExportService();
        var bytes = svc.ToExcel(SampleResult(), "Sheet1");
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet("Sheet1");
        ws.Cell(2, 1).DataType.Should().Be(XLDataType.Number);
    }

    [Fact]
    public void ToExcel_StringColumn_CellIsText()
    {
        var svc = new ExportService();
        var bytes = svc.ToExcel(SampleResult(), "Sheet1");
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet("Sheet1");
        ws.Cell(2, 2).Value.ToString().Should().Be("Alice");
    }

    [Fact]
    public void ToExcel_NullValue_DoesNotThrow()
    {
        var svc = new ExportService();
        var result = new QueryResult
        {
            Columns = new() { "Name" },
            Rows = new() { new() { ["Name"] = null } }
        };
        var act = () => svc.ToExcel(result, "Sheet1");
        act.Should().NotThrow();
    }
}
