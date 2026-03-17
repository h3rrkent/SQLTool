// SQLTool.Tests/Services/ExportServiceTests.cs
using FluentAssertions;
using SQLTool.Models;
using SQLTool.Services;

namespace SQLTool.Tests.Services;

public class ExportServiceTests
{
    private QueryResult SampleResult() => new()
    {
        Columns = new() { "Id", "Name" },
        Rows = new()
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" }
        }
    };

    [Fact]
    public void ToCsv_ContainsHeaderAndRows()
    {
        var svc = new ExportService();
        var csv = svc.ToCsv(SampleResult());
        csv.Should().Contain("Id").And.Contain("Name").And.Contain("Alice").And.Contain("Bob");
    }

    [Fact]
    public void ToExcel_ReturnsByteArray()
    {
        var svc = new ExportService();
        var bytes = svc.ToExcel(SampleResult(), "TestSheet");
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(100);
    }
}
