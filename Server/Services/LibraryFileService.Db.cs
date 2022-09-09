using System.Reflection;
using System.Text.RegularExpressions;
using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;
using NPoco;

namespace FileFlows.Server.Services;

/// <summary>
/// Service for communicating with FileFlows server for library files
/// </summary>
public partial class LibraryFileService
{   
    
    //private static Dictionary<Guid, LibraryFile> CachedData;

    private static SemaphoreSlim GetNextSemaphore = new (1);

    private const string UNPROCESSED_ORDER_BY =
        " case when ProcessingOrder > 0 then ProcessingOrder " +
        " else 1000000 end, JSON_EXTRACT(obj.Data, '$.Priority') desc, LibraryFile.DateModified desc";

    private const string LIBRARY_JOIN =
        " left join DbObject obj on obj.Type = 'FileFlows.Shared.Models.Library' and LibraryFile.LibraryUid = obj.Uid ";

    
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
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to get: " + Regex.Replace(sql, @"\s\s+", " ").Trim());
        return result;
    }
    
    private async Task<List<T>> Database_Fetch<T>(string sql, params object[] args)
    {
        List<T> results;
        DateTime dt = DateTime.Now;
        try
        {
            using (var db = await GetDbWithMappings())
            {
                results = await db.Db.FetchAsync<T>(sql, args);
            }
        }
        finally
        {
            Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to fetch: " + Regex.Replace(sql, @"\s\s+", " ").Trim());
        }

        return results;
    }

    private async Task Database_Execute(string sql, params object[] args)
    {
        DateTime dt = DateTime.Now;
        int effected;
        using (var db = await GetDbWithMappings())
        {
            effected = await db.Db.ExecuteAsync(sql, args);
        }
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to execute [{effected}]: " + Regex.Replace(sql, @"\s\s+", " ").Trim());
    }
    private async Task<T> Database_ExecuteScalar<T>(string sql, params object[] args)
    {
        T result;
        DateTime dt = DateTime.Now;
        using (var db = await GetDbWithMappings())
        {
            result = await db.Db.ExecuteScalarAsync<T>(sql, args);
        }
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to execute: " + Regex.Replace(sql, @"\s\s+", " ").Trim());
        return result;
    }

    private static readonly string[] LibraryFileUpdateColums = typeof(LibraryFile).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x =>
    {
        return x.GetCustomAttribute<IgnoreAttribute>() == null;
    }).Select(x => x.Name).ToArray();

    private async Task Database_Update(LibraryFile o)
    {
        DateTime dt = DateTime.Now;
        
        using (var db = await GetDbWithMappings())
        {
            await db.Db.UpdateAsync("LibraryFile", "Uid", o, o.Uid, LibraryFileUpdateColums);
        }
        Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to update object");
    }

    private async Task Database_Insert(object o)
    {
        DateTime dt = DateTime.Now;
        try
        {
            using (var db = await GetDbWithMappings())
            {
                await db.Db.InsertAsync(o);
            }
            Logger.Instance.ILog($"Took '{(DateTime.Now - dt)}' to insert object");
        }
        catch (Exception ex)
        {
            Logger.Instance.ILog($"Failed insert object: " + ex.Message);
            throw;
        }

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