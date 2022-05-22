using System.Text;
using FileFlows.Server.Helpers;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using NPoco;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A database manager is responsible for the communication to and from the database instance
/// Each support database will have it's own manager
/// </summary>
public abstract class DbManager
{
    
    protected string ConnectionString { get; init; }

    protected enum DbCreateResult
    {
        Failed = 0,
        Created = 1,
        AlreadyExisted = 2
    }
    protected readonly string CreateDbScript =
        @$"CREATE TABLE {nameof(DbObject)}(
            Uid             VARCHAR(36)           NOT NULL          PRIMARY KEY,
            Name            VARCHAR(255)       NOT NULL,
            Type            VARCHAR(255)       NOT NULL,
            DateCreated     datetime           default           current_timestamp,
            DateModified    datetime           default           current_timestamp,
            Data            TEXT               NOT NULL
        );";

    /// <summary>
    /// Gets if the database manager should use a memory cache
    /// </summary>
    public virtual bool UseMemoryCache => false; 
    
    /// <summary>
    /// Gets the database used by this configuration
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <returns>the database manager to use</returns>
    public static DbManager GetManager(string connectionString)
    {
        connectionString ??= SqliteDbManager.GetConnetionString(SqliteDbFile);
        
        if (connectionString.Contains(".sqlite"))
            return new SqliteDbManager(connectionString);
        
        return new MySqlDbManager(connectionString);
    }

    private static string SqliteDbFile => Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");

    /// <summary>
    /// Get an instance of the IDatabase
    /// </summary>
    /// <returns>an instance of the IDatabase</returns>
    protected abstract IDatabase GetDb();
    
    
    #region Setup Code

    /// <summary>
    /// Creates the database and the initial data
    /// </summary>
    /// <returns>if the database was successfully created or not</returns>
    public async Task<bool> CreateDb()
    {
        var dbResult = CreateDatabase();
        if (dbResult == DbCreateResult.Failed)
            return false;
        
        if (dbResult == DbCreateResult.AlreadyExisted)
            return true;
        
        if (CreateDatabaseStructure() == false)
            return false;
        
        
        if (this is SqliteDbManager == false)
        {
            // not a sqlite database, check if one exists and migrate
            if (File.Exists(SqliteDbFile))
            {
                // migrate teh data
                bool migrated = DbMigrater.Migrate(SqliteDbManager.GetConnetionString(SqliteDbFile), this.ConnectionString);

                if (migrated)
                {
                    File.Move(SqliteDbFile, SqliteDbFile + ".migrated");
                }

                // migrated, we dont need to insert initial data
                return true;
            }
        }
        
        return await CreateInitialData();
    }

    /// <summary>
    /// Creates the actual Database
    /// </summary>
    /// <returns>true if successfully created</returns>
    protected abstract DbCreateResult CreateDatabase();
    /// <summary>
    /// Creates the tables etc in the database
    /// </summary>
    /// <returns>true if successfully created</returns>
    protected abstract bool CreateDatabaseStructure();

    /// <summary>
    /// Inserts the initial data into the database
    /// </summary>
    /// <returns>true if successfully inserted</returns>
    private async Task<bool> CreateInitialData()
    {
        using var db = GetDb();
        bool windows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        await AddOrUpdateObject(db, new Tool
        {
            Name = "FFMpeg",
            Path = windows ? Path.Combine(DirectoryHelper.BaseDirectory, @"Tools\ffmpeg.exe") : "/usr/local/bin/ffmpeg",
            DateCreated = DateTime.Now,
            DateModified = DateTime.Now
        });

        await AddOrUpdateObject(db, new Settings
        {
            Name = "Settings",
            AutoUpdatePlugins = true,
            DateCreated = DateTime.Now,
            DateModified = DateTime.Now
        });

        string tempPath;
        if(DirectoryHelper.IsDocker)
            tempPath = "/temp";
        else if(windows)
            tempPath = @Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FileFlows\\Temp");
        else
            tempPath = Path.Combine(DirectoryHelper.BaseDirectory, "Temp");
        
        if (Directory.Exists(tempPath) == false)
            Directory.CreateDirectory(tempPath);

        await AddOrUpdateObject(db, new ProcessingNode
        {
            Name = Globals.FileFlowsServer,
            Address = Globals.FileFlowsServer,
            Schedule = new string('1', 672),
            Enabled = true,
            FlowRunners = 1,
            TempPath = tempPath,
        });

        return true;
    }

    #endregion
    
    
    #region helper methods
    /// <summary>
    /// Escapes a string so it is safe to be used in a sql command
    /// </summary>
    /// <param name="input">the string to escape</param>
    /// <returns>the escaped string</returns>
    protected static string SqlEscape(string input) => input == null ? string.Empty : "'" + input.Replace("'", "''") + "'";
    #endregion
    
    

    /// <summary>
    /// Select a list of objects
    /// </summary>
    /// <typeparam name="T">the type of objects to select</typeparam>
    /// <returns>a list of objects</returns>
    public async Task<IEnumerable<T>> Select<T>() where T : FileFlowObject, new()
    {
        using var db = GetDb();
        var dbObjects = await db.FetchAsync<DbObject>("where Type=@0 order by Name", typeof(T).FullName);
        return dbObjects.Select(x => Convert<T>(x));
    }
    
    /// <summary>
    /// Selects types from the database
    /// </summary>
    /// <param name="where">a where clause for the select</param>
    /// <param name="arguments">the arguments for the select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a list of objects</returns>
    public async Task<IEnumerable<T>> Select<T>(string where, params object[] arguments) where T : FileFlowObject, new()
    {
        using var db = GetDb();
        var dbObjects = await db.FetchAsync<DbObject>($"where Type=@0 and {where} order by Name", typeof(T).FullName, arguments);
        return dbObjects.Select(x => Convert<T>(x));
    }

    /// <summary>
    /// Get names of types
    /// </summary>
    /// <param name="andWhere">and where caluse</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to select</typeparam>
    /// <returns>a list of names</returns>
    public async Task<IEnumerable<string>> GetNames<T>(string andWhere = "", params object[] args)
    {
        using var db = GetDb();
        
        if (string.IsNullOrEmpty(andWhere) == false && andWhere.Trim().ToLower().StartsWith("and ") == false)
            andWhere = " and " + andWhere;
        args = new object[] { typeof(T).FullName }.Union(args ?? new object[] { }).ToArray();
        return await db.FetchAsync<string>($"select Name from {nameof(DbObject)} where Type=@0 {andWhere} order by name", args);
    }


    /// <summary>
    /// Get names of types indexed by their UID
    /// </summary>
    /// <param name="andWhere">and where clause</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to select</typeparam>
    /// <returns>a list of names</returns>
    public async Task<Dictionary<Guid, string>> GetIndexedNames<T>(string andWhere = "", params object[] args)
    {
        using var db = GetDb();
        
        if (string.IsNullOrEmpty(andWhere) == false && andWhere.Trim().ToLower().StartsWith("and ") == false)
            andWhere = " and " + andWhere;
        args = new object[] { typeof(T).FullName }.Union(args ?? new object[] { }).ToArray();
        var results = await db.FetchAsync<(Guid Uid, string Name)>($"select Uid, Name from {nameof(DbObject)} where Type=@0 {andWhere} order by name", args);
        return results.ToDictionary(x => x.Uid, x => x.Name);
    }

    /// <summary>
    /// Checks to see if a name is in use
    /// </summary>
    /// <param name="uid">the Uid of the item</param>
    /// <param name="name">the name of the item</param>
    /// <returns>true if name is in use</returns>
    public bool NameInUse<T>(Guid uid, string name)
    {
        using var db = GetDb();
        string result = db.FirstOrDefault<string>(
            $"select top 1 Name from {nameof(DbObject)} where Type=@0 and uid <> @1", name, uid.ToString());
        return string.IsNullOrEmpty(result);
    }

    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <typeparam name="T">The type to select</typeparam>
    /// <returns>a single instance</returns>
    public async Task<T> Single<T>() where T : FileFlowObject, new()
    {
        using var db = GetDb();
        var dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0", typeof(T).FullName);
        if (string.IsNullOrEmpty(dbObject?.Data))
            return new T();
        return Convert<T>(dbObject);
    }

    /// <summary>
    /// Selects a single instance
    /// </summary>
    /// <param name="uid">the UID of the item to select</param>
    /// <typeparam name="T">the type of item to select</typeparam>
    /// <returns>a single instance</returns>
    public async Task<T> Single<T>(Guid uid) where T : FileFlowObject, new()
    {
        using var db = GetDb();
        
        var dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0 and Uid=@1", typeof(T).FullName, uid.ToString());
        if (string.IsNullOrEmpty(dbObject?.Data))
            return new T();
        return Convert<T>(dbObject);
    }

    /// <summary>
    /// Selects a single instance by its name
    /// </summary>
    /// <param name="name">the name of the item to select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a single instance</returns>
    public async Task<T> SingleByName<T>(string name) where T : FileFlowObject, new()
    {
        using var db = GetDb();
        
        var dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0 and lower(Name)=lower(@1)", typeof(T).FullName, name);
        if (string.IsNullOrEmpty(dbObject?.Data))
            return new T();
        return Convert<T>(dbObject);
    }
    
    /// <summary>
    /// Adds or updates an object in the database
    /// </summary>
    /// <param name="db">The IDatabase used for this operation</param>
    /// <param name="obj">The object being added or updated</param>
    /// <typeparam name="T">The type of object being added or updated</typeparam>
    /// <returns>The updated object</returns>
    private static async Task<T> AddOrUpdateObject<T>(IDatabase db, T obj) where T : FileFlowObject
    {
        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DataConverter(), new BoolConverter(), new Shared.Json.ValidatorConverter() }
        };
        // need to case obj to (ViObject) here so the DataConverter is used
        string json = JsonSerializer.Serialize((FileFlowObject)obj, serializerOptions);

        var type = obj.GetType();
        obj.Name = obj.Name?.EmptyAsNull() ?? type.Name;
        var dbObject = db.FirstOrDefault<DbObject>("where Type=@0 and Uid = @1", type.FullName, obj.Uid.ToString());
        if (dbObject == null)
        {
            obj.Uid = Guid.NewGuid();
            obj.DateCreated = DateTime.Now;
            obj.DateModified = obj.DateCreated;
            // create new 
            dbObject = new DbObject
            {
                Uid = obj.Uid.ToString(),
                Name = obj.Name,
                DateCreated = obj.DateCreated,
                DateModified = obj.DateModified,

                Type = type.FullName,
                Data = json
            };
            await db.InsertAsync(dbObject);
        }
        else
        {
            obj.DateModified = DateTime.Now;
            dbObject.Name = obj.Name;
            dbObject.DateModified = obj.DateModified;
            dbObject.Data = json;
            await db.UpdateAsync(dbObject);
        }
        return obj;
    }
    

    /// <summary>
    /// Updates the last modified date of an object
    /// </summary>
    /// <param name="uid">the UID of the object to update</param>
    internal async Task UpdateLastModified(Guid uid)
    {
        using var db = GetDb();
        await db.ExecuteAsync($"update {nameof(DbObject)} set DateModified = @0 where Uid = @1", DateTime.Now, uid);
    }

    /// <summary>
    /// This will batch insert many objects into thee database
    /// </summary>
    /// <param name="items">Items to insert</param>
    internal async Task AddMany(FileFlowObject[] items)
    {
        if (items?.Any() != true)
            return;
        int max = 500;
        int count = items.Length;

        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DataConverter(), new BoolConverter() }
        };
        for (int i = 0; i < count; i += max)
        {
            StringBuilder sql = new StringBuilder();
            for (int j = i; j < i + max && j < count; j++)
            {
                var obj = items[j];
                // need to case obj to (ViObject) here so the DataConverter is used
                string json = JsonSerializer.Serialize(obj, serializerOptions);

                var type = obj.GetType();
                obj.Name = obj.Name?.EmptyAsNull() ?? type.Name;
                obj.Uid = Guid.NewGuid();
                obj.DateCreated = DateTime.Now;
                obj.DateModified = obj.DateCreated;

                sql.AppendLine($"insert into {nameof(DbObject)} (Uid, Name, Type, Data) values (" +
                               SqlEscape(obj.Uid.ToString()) + "," +
                               SqlEscape(obj.Name) + "," +
                               SqlEscape(type?.FullName ?? String.Empty) + "," +
                               SqlEscape(json) +
                               ");");
            }
            if (sql.Length > 0)
            {
                var db = GetDb();
                await db.ExecuteAsync(sql.ToString());
            }
        }
    }

    /// <summary>
    /// Selects a single item from the database
    /// </summary>
    /// <param name="andWhere">the and where clause</param>
    /// <param name="args">any parameters to the select statement</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>an single instance</returns>
    public async Task<T> Single<T>(string andWhere, params object[] args) where T : FileFlowObject, new()
    {
        using var db = GetDb();
        args = new object[] { typeof(T).FullName }.Union(args).ToArray();
        var dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0 and " + andWhere, args);
        if (string.IsNullOrEmpty(dbObject?.Data))
            return new T();
        return Convert<T>(dbObject);
    }
    
    private T Convert<T>(DbObject dbObject) where T : FileFlowObject, new()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new BoolConverter(), new Shared.Json.ValidatorConverter() }
        };

        // need to case obj to (ViObject) here so the DataConverter is used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        T result = System.Text.Json.JsonSerializer.Deserialize<T>(dbObject.Data, serializerOptions);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        result.Uid = Guid.Parse(dbObject.Uid);
        result.Name = dbObject.Name;
        result.DateModified = dbObject.DateModified;
        result.DateCreated = dbObject.DateCreated;
        return result;
    }
    
    /// <summary>
    /// Updates an object
    /// </summary>
    /// <param name="obj">the object to update</param>
    /// <typeparam name="T">the object type</typeparam>
    /// <returns>the updated object</returns>
    public async Task<T> Update<T>(T obj) where T : FileFlowObject, new()
    {
        if (obj == null)
            return new T();
        using var db = GetDb();
        return await AddOrUpdateObject(db, obj);
    }
    
    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <typeparam name="T">The type of objects being deleted</typeparam>
    public virtual async Task Delete<T>(params Guid[] uids) where T : FileFlowObject
    {
        if (uids?.Any() != true)
            return; // nothing to delete

        var typeName = typeof(T).FullName;
        string strUids = String.Join(",", uids.Select(x => "'" + x.ToString() + "'"));
        using var db = GetDb();
        await db.ExecuteAsync($"delete from {nameof(DbObject)} where Type=@0 and Uid in ({strUids})", typeName);
    }
    
    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public virtual async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return; // nothing to delete

        string strUids = String.Join(",", uids.Select(x => "'" + x.ToString() + "'"));
        using var db = GetDb();
        await db.ExecuteAsync($"delete from {nameof(DbObject)} where Uid in ({strUids})");
    }
    
#if (DEBUG)
    /// <summary>
    /// Clean the database and purge old data
    /// </summary>
    /// <returns>True if successful</returns>
    public async Task<bool> CleanDatabase()
    {
        try
        {
            using var db = GetDb();
            await db.ExecuteAsync($"delete from {nameof(DbObject)} where Type = @0", typeof(LibraryFile).FullName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed cleaning database: " + ex.Message);
            return false;
        }
    }
#endif
}