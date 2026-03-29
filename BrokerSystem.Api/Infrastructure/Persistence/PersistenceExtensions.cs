using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BrokerSystem.Api.Infrastructure.Persistence;

/// <summary>
/// Defines the contract for SQL dialect-specific operations (e.g. Paging, Concat).
/// Used to maintain clean Dapper queries across different database providers (SQL Server vs SQLite).
/// </summary>
public interface ISqlDialect
{
    string Top(int count);
    string Limit(int count);
    string Paging(string offsetParam, string pageSizeParam);
    string Concat(params string[] parts);
    string Year(string column);
    string Month(string column);
    string FormattedMonthYear(string column);
    string IsNull(string expression, string defaultValue);
    string DateDiffDay(string start, string end);
}

/// <summary>
/// T-SQL implementation for SQL Server (using TOP, OFFSET/FETCH, and '+').
/// </summary>
public class SqlServerDialect : ISqlDialect
{
    public string Top(int count) => $"TOP {count}";
    public string Limit(int count) => ""; // SQL Server uses TOP

    public string Paging(string offsetParam, string pageSizeParam)
        => $"OFFSET {offsetParam} ROWS FETCH NEXT {pageSizeParam} ROWS ONLY";

    public string Concat(params string[] parts) => string.Join(" + ", parts);
    public string Year(string column) => $"YEAR({column})";
    public string Month(string column) => $"MONTH({column})";

    public string FormattedMonthYear(string column)
        => $"CAST(YEAR({column}) AS VARCHAR(4)) + '-' + RIGHT('0' + CAST(MONTH({column}) AS VARCHAR(2)), 2)";

    public string IsNull(string expression, string defaultValue) => $"ISNULL({expression}, {defaultValue})";
    public string DateDiffDay(string start, string end) => $"DATEDIFF(day, {start}, {end})";
}

/// <summary>
/// SQLite implementation (using LIMIT, OFFSET, and '||').
/// </summary>
public class SqliteDialect : ISqlDialect
{
    public string Top(int count) => ""; // SQLite uses LIMIT
    public string Limit(int count) => $"LIMIT {count}";

    public string Paging(string offsetParam, string pageSizeParam)
        => $"LIMIT {pageSizeParam} OFFSET {offsetParam}";

    public string Concat(params string[] parts) => string.Join(" || ", parts);
    public string Year(string column) => $"strftime('%Y', {column})";
    public string Month(string column) => $"strftime('%m', {column})";
    public string FormattedMonthYear(string column) => $"strftime('%Y-%m', {column})";

    public string IsNull(string expression, string defaultValue) => $"COALESCE({expression}, {defaultValue})";
    public string DateDiffDay(string start, string end) => $"CAST(JULIANDAY({end}) - JULIANDAY({start}) AS INT)";
}

public static class DatabaseFacadeExtensions
{
    /// <summary>
    /// Extension method to automatically resolve the correct <see cref="ISqlDialect"/> 
    /// based on the current EF Core database provider.
    /// </summary>
    public static ISqlDialect Sql(this DatabaseFacade database)
    {
        return database.ProviderName switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" => new SqliteDialect(),
            _ => new SqlServerDialect()
        };
    }
}
