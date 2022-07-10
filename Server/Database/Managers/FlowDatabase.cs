using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using FileFlows.Plugin;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// Database wrapper so can log every command to the database
/// </summary>
public class FlowDatabase:NPoco.Database
{
    /// <summary>
    /// Gets the logger for the database
    /// </summary>
    public static FileLogger Logger { get;private set; }

    static FlowDatabase()
    {
        Logger = new FileLogger(DirectoryHelper.LoggingDirectory, "FileFlowsDB", register: false);
    }
    
    /// <summary>
    /// Constructs an instance of the flow database
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    public FlowDatabase(string connectionString) : base(connectionString, null,
        MySqlConnector.MySqlConnectorFactory.Instance)
    {
        
    }

    private Dictionary<int, DateTime> ExecutedCommands = new Dictionary<int, DateTime>();

    protected override void OnExecutingCommand(DbCommand cmd)
    {
        int hashCode = cmd.GetHashCode();
        lock (ExecutedCommands)
        {
            if (ExecutedCommands.ContainsKey(hashCode))
                ExecutedCommands[hashCode] = DateTime.Now;
            else
                ExecutedCommands.Add(hashCode, DateTime.Now);
        }

        string sql = GetCommandText(cmd);
        //Logger.Log((LogType) 999, $"Executing [{hashCode.ToString("00000000")}]: " + sql);
        
    }

    protected override void OnExecutedCommand(DbCommand cmd)
    {
        int hashCode = cmd.GetHashCode();
        DateTime started;
        lock (ExecutedCommands)
        {
            if (ExecutedCommands.ContainsKey(hashCode) == false)
                return;
            started = ExecutedCommands[hashCode];
            ExecutedCommands.Remove(hashCode);
        }

        var time = DateTime.Now.Subtract(started);
        string sql = GetCommandText(cmd);
        if (sql.Contains("LogMessage"))
            sql = "LOGMESSAGE";
        else if(sql.StartsWith("UPDATE `DbObject` SET `Name` = \""))
        {
            string name = Regex.Match(sql, @"(?<=(UPDATE `DbObject` SET `Name` = ""))[^""]+").Value;
            sql = $"Updated DbObject '{name}'";
        }
        LogType lg = LogType.Info;
        if (time.TotalSeconds > 1)
            lg = LogType.Error;
        if (time.TotalMilliseconds > 100)
            lg = LogType.Warning;
        
        Logger.Log(lg, $"Executed  [{hashCode.ToString("00000000")}] [{time}]: " + sql);
    }

    private string GetCommandText(DbCommand cmd)
    {
        if (cmd.Parameters.Count == 0)
            return cmd.CommandText;
        
        string sql = cmd.CommandText;
        int count = -1;
        foreach (DbParameter param in cmd.Parameters)
        {
            ++count;
            if (param.DbType == DbType.String || param.DbType == DbType.Guid)
                sql = sql.Replace("@" + count, "\"" + param.Value.ToString().Replace("\"", "\"\"") + "\"");
            else
                sql = sql.Replace("@" + count, param.Value.ToString());
        }

        return sql;
    }
}