// SQLTool/Services/QueryEngine.cs
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using SQLTool.Models;

namespace SQLTool.Services;

public class QueryEngine : IQueryEngine
{
    private const int RowLimit = 10_000;
    private readonly IConfigService _configService;

    public QueryEngine(IConfigService configService) => _configService = configService;

    public ValidationResult ValidateParameters(QueryDefinition query, List<ParameterValue> values)
    {
        var result = new ValidationResult();
        foreach (var param in query.Parameters)
        {
            var submitted = values.FirstOrDefault(v => v.Name == param.Name);
            var raw = submitted?.Value;

            if (param.Required && string.IsNullOrWhiteSpace(raw))
            {
                result.Errors.Add($"{param.Label} is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(raw)) continue;

            switch (param.Type)
            {
                case "date":
                    if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", out _))
                        result.Errors.Add($"{param.Label} must be a valid date (yyyy-MM-dd).");
                    break;
                case "number":
                    if (!decimal.TryParse(raw, out var num))
                        result.Errors.Add($"{param.Label} must be a number.");
                    else
                    {
                        if (param.Min.HasValue && num < param.Min) result.Errors.Add($"{param.Label} must be at least {param.Min}.");
                        if (param.Max.HasValue && num > param.Max) result.Errors.Add($"{param.Label} must be at most {param.Max}.");
                    }
                    break;
                case "text":
                    if (raw.Length > 500) result.Errors.Add($"{param.Label} must be 500 characters or fewer.");
                    break;
                case "dropdown":
                    if (!param.Options.Any(o => o.Value == raw))
                        result.Errors.Add($"{param.Label} has an invalid value.");
                    break;
            }
        }
        return result;
    }

    public async Task<QueryResult> ExecuteAsync(QueryDefinition query, DatabaseEntry database, List<ParameterValue> values)
    {
        var connectionString = _configService.GetConnectionString(database.ConnectionStringKey);
        if (connectionString is null)
            return new QueryResult { ErrorMessage = $"Connection string '{database.ConnectionStringKey}' not configured." };

        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new SqlConnection(connectionString);
            var parameters = new DynamicParameters();
            foreach (var p in query.Parameters)
            {
                var val = values.FirstOrDefault(v => v.Name == p.Name)?.Value;
                parameters.Add(p.Name, CoerceValue(p, val));
            }

            var cmd = new CommandDefinition(query.Sql, parameters, commandTimeout: database.QueryTimeoutSeconds);
            var raw = (await conn.QueryAsync(cmd)).ToList();
            sw.Stop();

            var rows = raw.Take(RowLimit + 1)
                .Select(r => ((IDictionary<string, object?>)r).ToDictionary(k => k.Key, k => k.Value))
                .ToList();

            var truncated = rows.Count > RowLimit;
            if (truncated) rows = rows.Take(RowLimit).ToList();

            var columns = rows.FirstOrDefault()?.Keys.ToList() ?? new();
            return new QueryResult
            {
                Rows = rows,
                Columns = columns,
                TotalRowCount = rows.Count,
                Truncated = truncated,
                ElapsedMilliseconds = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new QueryResult { ErrorMessage = ex.Message };
        }
    }

    private static object? CoerceValue(QueryParameter param, string? raw)
    {
        if (raw is null) return null;
        return param.Type switch
        {
            "date" => DateOnly.TryParseExact(raw, "yyyy-MM-dd", out var d) ? d.ToDateTime(TimeOnly.MinValue) : null,
            "number" => decimal.TryParse(raw, out var n) ? n : null,
            "boolean" => raw.Equals("true", StringComparison.OrdinalIgnoreCase),
            _ => raw
        };
    }
}
