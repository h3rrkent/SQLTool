// SQLTool.Tests/Services/QueryEngineTests.cs
using FluentAssertions;
using SQLTool.Models;
using SQLTool.Services;

namespace SQLTool.Tests.Services;

public class QueryEngineTests
{
    private QueryEngine CreateEngine() =>
        new QueryEngine(new Moq.Mock<IConfigService>().Object);

    // --- Required parameter validation ---

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
    public void ValidateParameters_OptionalParameterEmpty_IsValid()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "Filter", Label = "Filter", Type = "text", Required = false } }
        };
        var result = engine.ValidateParameters(query, new List<ParameterValue>());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateParameters_NoParameters_IsValid()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition { Parameters = new() };
        var result = engine.ValidateParameters(query, new List<ParameterValue>());
        result.IsValid.Should().BeTrue();
    }

    // --- Text validation ---

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

    [Fact]
    public void ValidateParameters_TextAtMaxLength_IsValid()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "Search", Label = "Search", Type = "text", Required = true } }
        };
        var values = new List<ParameterValue> { new() { Name = "Search", Value = new string('x', 500) } };
        var result = engine.ValidateParameters(query, values);
        result.IsValid.Should().BeTrue();
    }

    // --- Number validation ---

    [Fact]
    public void ValidateParameters_NumberWithDecimalPoint_IsValidInvariantCulture()
    {
        // Regression: decimal.TryParse without CultureInfo.InvariantCulture fails on European locales
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "Amount", Label = "Amount", Type = "number", Required = true } }
        };
        var values = new List<ParameterValue> { new() { Name = "Amount", Value = "3.14" } };
        var result = engine.ValidateParameters(query, values);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateParameters_NumberNotANumber_ReturnsError()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new() { new QueryParameter { Name = "N", Label = "Number", Type = "number", Required = true } }
        };
        var values = new List<ParameterValue> { new() { Name = "N", Value = "abc" } };
        var result = engine.ValidateParameters(query, values);
        result.IsValid.Should().BeFalse();
    }

    // --- Dropdown validation ---

    [Fact]
    public void ValidateParameters_DropdownValidValue_IsValid()
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
        var values = new List<ParameterValue> { new() { Name = "Status", Value = "active" } };
        var result = engine.ValidateParameters(query, values);
        result.IsValid.Should().BeTrue();
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

    // --- Multiple errors ---

    [Fact]
    public void ValidateParameters_MultipleRequiredMissing_ReturnsAllErrors()
    {
        var engine = CreateEngine();
        var query = new QueryDefinition
        {
            Parameters = new()
            {
                new QueryParameter { Name = "A", Label = "Field A", Type = "text",   Required = true },
                new QueryParameter { Name = "B", Label = "Field B", Type = "number", Required = true }
            }
        };
        var result = engine.ValidateParameters(query, new List<ParameterValue>());
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
