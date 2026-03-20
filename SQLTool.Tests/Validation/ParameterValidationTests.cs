// SQLTool.Tests/Validation/ParameterValidationTests.cs
using FluentAssertions;
using SQLTool.Models;
using SQLTool.Services;

namespace SQLTool.Tests.Validation;

public class ParameterValidationTests
{
    private static QueryEngine Engine() =>
        new QueryEngine(new Moq.Mock<IConfigService>().Object);

    private static ValidationResult Validate(QueryParameter param, string? value)
    {
        var engine = Engine();
        var query = new QueryDefinition { Parameters = new() { param } };
        var values = value is null ? new List<ParameterValue>() : new List<ParameterValue> { new() { Name = param.Name, Value = value } };
        return engine.ValidateParameters(query, values);
    }

    // --- Date ---

    [Theory]
    [InlineData("2024-01-01", true)]
    [InlineData("2000-12-31", true)]
    [InlineData("not-a-date",  false)]
    [InlineData("01-01-2024",  false)]
    [InlineData("2024/01/01",  false)]
    [InlineData("",            false)]
    public void ValidateDate_ReturnsExpected(string value, bool expected)
    {
        var result = Validate(new QueryParameter { Name = "D", Label = "Date", Type = "date", Required = true }, value);
        result.IsValid.Should().Be(expected);
    }

    // --- Number ---

    [Theory]
    [InlineData("5",    1, 10, true)]
    [InlineData("1",    1, 10, true)]
    [InlineData("10",   1, 10, true)]
    [InlineData("0",    1, 10, false)]
    [InlineData("11",   1, 10, false)]
    [InlineData("abc",  1, 10, false)]
    public void ValidateNumber_WithMinMax_ReturnsExpected(string value, decimal min, decimal max, bool expected)
    {
        var result = Validate(new QueryParameter { Name = "N", Label = "Num", Type = "number", Required = true, Min = min, Max = max }, value);
        result.IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("3.14",  true)]
    [InlineData("0",     true)]
    [InlineData("-1",    true)]
    [InlineData("1e5",   false)]  // scientific notation not accepted
    [InlineData("",      false)]
    public void ValidateNumber_NoMinMax_ReturnsExpected(string value, bool expected)
    {
        var result = Validate(new QueryParameter { Name = "N", Label = "Num", Type = "number", Required = true }, value);
        result.IsValid.Should().Be(expected);
    }

    // --- Text ---

    [Theory]
    [InlineData("hello",        true)]
    [InlineData("",             false)]  // required
    [InlineData("   ",          false)]  // whitespace only
    public void ValidateText_Required_ReturnsExpected(string value, bool expected)
    {
        var result = Validate(new QueryParameter { Name = "T", Label = "Text", Type = "text", Required = true }, value);
        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void ValidateText_Exactly500Chars_IsValid()
    {
        var result = Validate(new QueryParameter { Name = "T", Label = "Text", Type = "text", Required = true }, new string('a', 500));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateText_501Chars_IsInvalid()
    {
        var result = Validate(new QueryParameter { Name = "T", Label = "Text", Type = "text", Required = true }, new string('a', 501));
        result.IsValid.Should().BeFalse();
    }

    // --- Dropdown ---

    [Fact]
    public void ValidateDropdown_ValidOption_IsValid()
    {
        var param = new QueryParameter
        {
            Name = "S", Label = "Status", Type = "dropdown", Required = true,
            Options = new() { new DropdownOption { Value = "a", Label = "A" }, new DropdownOption { Value = "b", Label = "B" } }
        };
        Validate(param, "a").IsValid.Should().BeTrue();
        Validate(param, "b").IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateDropdown_InvalidOption_IsInvalid()
    {
        var param = new QueryParameter
        {
            Name = "S", Label = "Status", Type = "dropdown", Required = true,
            Options = new() { new DropdownOption { Value = "a", Label = "A" } }
        };
        Validate(param, "x").IsValid.Should().BeFalse();
    }

    // --- Optional parameters ---

    [Theory]
    [InlineData("date")]
    [InlineData("number")]
    [InlineData("text")]
    public void ValidateOptional_EmptyValue_IsValid(string type)
    {
        var result = Validate(new QueryParameter { Name = "P", Label = "Param", Type = type, Required = false }, null);
        result.IsValid.Should().BeTrue();
    }
}
