using System.Threading.Tasks;
using FileFlows.Plugin;

namespace FileFlows.Shared;

/// <summary>
/// An interface that is used to write log messages
/// </summary>
public interface ILogWriter
{
    /// <summary>
    /// Logs a message
    /// </summary>
    /// <param name="type">the type of log message</param>
    /// <param name="args">the arguments for the log message</param>
    Task Log(LogType type, params object[] args);
}