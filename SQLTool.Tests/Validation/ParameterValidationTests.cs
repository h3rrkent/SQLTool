// SQLTool.Tests/Validation/ParameterValidationTests.cs
using FluentAssertions;
using SQLTool.Models;
using SQLTool.Services;

namespace SQLTool.Tests.Validation;

public class ParameterValidationTests
{
    [Theory]
    [InlineData("2024-01-01", true)]
    [InlineData("not-a-date", false)]
    [InlineData("", false)]
    public void ValidateDate_ReturnsExpected(string value, bool expected)
    {
        var engine = new QueryEngine(new Moq.Mock<IConfigService>().Object);
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "D", Label = "Date", Type = "date", Required = true } }
        };
        var values = new List<ParameterValue> { new() { Name = "D", Value = value } };
        engine.ValidateParameters(query, values).IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("5", 1, 10, true)]
    [InlineData("0", 1, 10, false)]
    [InlineData("11", 1, 10, false)]
    [InlineData("abc", 1, 10, false)]
    public void ValidateNumber_WithMinMax_ReturnsExpected(string value, decimal min, decimal max, bool expected)
    {
        var engine = new QueryEngine(new Moq.Mock<IConfigService>().Object);
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "N", Label = "Num", Type = "number", Required = true, Min = min, Max = max } }
        };
        var values = new List<ParameterValue> { new() { Name = "N", Value = value } };
        engine.ValidateParameters(query, values).IsValid.Should().Be(expected);
    }
}
