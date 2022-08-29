using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for library files
/// </summary>
public partial class LibraryFileService
{   
    
    //private static Dictionary<Guid, LibraryFile> CachedData;

    private static SemaphoreSlim GetNextSemaphore = new (1);

    private const string UNPROCESSED_ORDER_BY =
        "order by case when ProcessingOrder > 0 then ProcessingOrder else 1000000 end, DateModified desc";

    private async Task<FlowDbConnection> GetDbWithMappings()
    {
        var db = await DbHelper.GetDbManager().GetDb();
        db.Db.Mappers.Add(new CustomDbMapper());
        return db;
    }

    private async Task<T> Database_Get<T>(string sql, params object[] args)
    {
        T result;
        DateTime dt = DateTime.Now;
        using (var db = await GetDbWithMappings())
        {
            result = await db.Db.SingleOrDefaultAsync<T>(sql, args);
        }
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to get: " + sql);
        return result;
    }
    
    private async Task<List<T>> Database_Fetch<T>(string sql, params object[] args)
    {
        List<T> results;
        DateTime dt = DateTime.Now;
        using (var db = await GetDbWithMappings())
        {
            results = await db.Db.FetchAsync<T>(sql, args);
        }
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to fetch: " + sql);

        return results;
    }

    private async Task Database_Execute(string sql, params object[] args)
    {
        DateTime dt = DateTime.Now;
        using (var db = await GetDbWithMappings())
        {
            await db.Db.ExecuteAsync(sql, args);
        }
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to execute: " + sql);
    }

    private async Task Database_Update(object o)
    {
        DateTime dt = DateTime.Now;
        using (var db = await GetDbWithMappings())
        {
            await db.Db.UpdateAsync(o);
        }
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to update object");
    }

    private async Task Database_Insert(object o)
    {
        DateTime dt = DateTime.Now;
        using (var db = await GetDbWithMappings())
        {
            await db.Db.InsertAsync(o);
        }
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to insert object");
    }

    private async Task Database_AddMany(IEnumerable<object> o)
    {
        DateTime dt = DateTime.Now;
        using (var db = await GetDbWithMappings())
        {
            await db.Db.InsertBulkAsync(o.Where(x => x != null));
        }
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to insert objects");
    }

    
}