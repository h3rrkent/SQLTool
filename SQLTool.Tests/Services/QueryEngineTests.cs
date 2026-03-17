// SQLTool.Tests/Services/QueryEngineTests.cs
using FluentAssertions;
using SQLTool.Models;
using SQLTool.Services;

namespace SQLTool.Tests.Services;

public class QueryEngineTests
{
    private QueryEngine CreateEngine(IConfigService? configService = null)
    {
        configService ??= new Moq.Mock<IConfigService>().Object;
        return new QueryEngine(configService);
    }

    [Fact]
    public void ValidateParameters_RequiredMissing_ReturnsError()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "StartDate", Label = "Start date", Type = "date", Required = true } }
        };
        var result = engine.ValidateParameters(query, new List<ParameterValue>());
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Start date"));
    }

    [Fact]
    public void ValidateParameters_DropdownInvalidValue_ReturnsError()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter
            {
                Name = "Status", Label = "Status", Type = "dropdown", Required = true,
                Options = new() { new DropdownOption { Value = "active", Label = "Active" } }
            }}
        };
        var values = new List<ParameterValue> { new() { Name = "Status", Value = "invalid" } };
        var result = engine.ValidateParameters(query, values);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateParameters_TextTooLong_ReturnsError()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "Search", Label = "Search", Type = "text", Required = true } }
        };
        var values = new List<ParameterValue> { new() { Name = "Search", Value = new string('x', 501) } };
        var result = engine.ValidateParameters(query, values);
        result.IsValid.Should().BeFalse();
    }
}
