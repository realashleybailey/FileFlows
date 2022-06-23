using FileFlows.Server.Controllers;
using FileFlows.ServerShared.Services;

namespace FileFlows.Server.Services;

/// <summary>
/// Statistic service
/// </summary>
public class StatisticService : IStatisticService
{
    /// <summary>
    /// Records a statistic value
    /// </summary>
    /// <returns>a task to await</returns>
    public Task Record(string name, object value) =>
        new StatisticsController().Record(new Statistic { Name = name, Value = value });
}