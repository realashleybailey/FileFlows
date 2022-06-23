namespace FileFlows.ServerShared.Services;

using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

/// <summary>
/// Statistic Service interface
/// </summary>
public interface IStatisticService
{
    /// <summary>
    /// Records a statistic value
    /// </summary>
    /// <returns>a task to await</returns>
    Task Record(string name, object value);
}

/// <summary>
/// Statistics service
/// </summary>
public class StatisticService : Service, IStatisticService
{

    /// <summary>
    /// Gets or sets a function used to load new instances of the service
    /// </summary>
    public static Func<IStatisticService> Loader { get; set; }

    /// <summary>
    /// Loads an instance of the plugin service
    /// </summary>
    /// <returns>an instance of the plugin service</returns>
    public static IStatisticService Load()
    {
        if (Loader == null)
            return new StatisticService();
        return Loader.Invoke();
    }

    /// <summary>
    /// Records a statistic value
    /// </summary>
    /// <returns>a task to await</returns>
    public async Task Record(string name, object value)
    {
        try
        {
            await HttpHelper.Post($"{ServiceBaseUrl}/api/statistic/record", new
            {
                Name = name,
                Value = value
            });
        }
        catch (Exception)
        {
        }
    }
}
