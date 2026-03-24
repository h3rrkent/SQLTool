// SQLTool/Services/QueryEngine.cs
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SQLTool.Models;

namespace SQLTool.Services;

public class QueryEngine : IQueryEngine
{
    private const int RowLimit = 10_000;
    private readonly IConfigService _configService;
    private readonly ILogger<QueryEngine> _logger;

    private static readonly Regex DmlDdlPattern = new(
        @"\b(INSERT|UPDATE|DELETE|MERGE|TRUNCATE|DROP|CREATE|ALTER|EXEC|EXECUTE|xp_|sp_)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public QueryEngine(IConfigService configService, ILogger<QueryEngine> logger)
    {
        _configService = configService;
        _logger = logger;
    }

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
                    if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var num))
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
        if (!query.AllowedEnvironments.Contains(database.EnvironmentName, StringComparer.OrdinalIgnoreCase))
            return new QueryResult { ErrorMessage = "Query is not permitted in this environment." };

        if (DmlDdlPattern.IsMatch(query.Sql))
            return new QueryResult { ErrorMessage = "Query contains disallowed SQL keywords (DML/DDL/stored procedures)." };

        var connectionString = _configService.GetConnectionString(database.ConnectionStringKey);
        if (connectionString is null)
            return new QueryResult { ErrorMessage = $"Connection string '{database.ConnectionStringKey}' not configured." };

        _logger.LogDebug("Executing query {QueryId} on {DatabaseKey}", query.Id, database.ConnectionStringKey);
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

            var limitedSql = $"SELECT TOP ({RowLimit + 1}) * FROM ({query.Sql}) AS __q";
            var cmd = new CommandDefinition(limitedSql, parameters, commandTimeout: database.QueryTimeoutSeconds);
            var raw = (await conn.QueryAsync(cmd)).ToList();
            sw.Stop();

            var rows = raw
                .Select(r => ((IDictionary<string, object?>)r).ToDictionary(k => k.Key, k => k.Value))
                .ToList();

            var truncated = rows.Count > RowLimit;
            if (truncated) rows = rows.Take(RowLimit).ToList();

            var columns = rows.FirstOrDefault()?.Keys.ToList() ?? new();
            _logger.LogInformation("Query {QueryId} completed in {ElapsedMs}ms, {RowCount} rows, truncated={Truncated}",
                query.Id, sw.ElapsedMilliseconds, rows.Count, truncated);

            return new QueryResult
            {
                Rows = rows,
                Columns = columns,
                RowCount = rows.Count,
                Truncated = truncated,
                ElapsedMilliseconds = sw.ElapsedMilliseconds
            };
        }
        catch (SqlException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "SQL error on query {QueryId} db {DatabaseKey}. SqlErrorNumber={N}",
                query.Id, database.ConnectionStringKey, ex.Number);
            return new QueryResult { ErrorMessage = "A database error occurred. Contact your administrator if this persists." };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Unexpected error on query {QueryId} db {DatabaseKey}", query.Id, database.ConnectionStringKey);
            return new QueryResult { ErrorMessage = "An unexpected error occurred." };
        }
    }

    private static object? CoerceValue(QueryParameter param, string? raw)
    {
        if (raw is null) return null;
        return param.Type switch
        {
            "date" => DateOnly.TryParseExact(raw, "yyyy-MM-dd", out var d) ? d.ToDateTime(TimeOnly.MinValue) : null,
            "number" => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null,
            "boolean" => raw.Equals("true", StringComparison.OrdinalIgnoreCase),
            _ => raw
        };
    }
}
