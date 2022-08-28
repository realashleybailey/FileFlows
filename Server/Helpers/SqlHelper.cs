namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper for SQL
/// </summary>
public class SqlHelper
{
    /// <summary>
    /// Cleans SQL and removes empty lines and comments
    /// </summary>
    /// <param name="sql">the SQL to clean</param>
    /// <returns>the cleaned SQL</returns>
    public static string CleanSql(string sql)
    {
        var lines = sql.Replace("\r\n", "\n")
            .Split('\n')
            .Where(x => string.IsNullOrWhiteSpace(x) == false)
            .Where(x => x.Trim().StartsWith("--") == false);
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Adds a skip to a sql command
    /// </summary>
    /// <param name="sql">the sql command</param>
    /// <param name="skip">the number of items to skip</param>
    /// <param name="rows">the number of rows to fetch</param>
    /// <returns>the updated SQL</returns>
    public static string Skip(string sql, int skip, int rows)
    {
        if (skip + rows == 0)
            return sql;
        if (DbHelper.UseMemoryCache)
        {
            // sqlite
            return sql + $" limit {rows} offset {skip}";
        }

        // mysql
        return sql + $" limit {skip}, {rows}";
    }
}