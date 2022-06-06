using System.Data;
using System.Reflection;
using System.Text;
using FileFlows.Plugin;
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
            Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
            Name            VARCHAR(1024)      NOT NULL,
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
    /// Gets if this database uses TOP to limit queries, otherwise LIMIT will be used
    /// </summary>
    public virtual bool UseTop => false;
    
    /// <summary>
    /// Method used by the manager to extract a json variable, mysql/mariadb use JSON_EXTRACT
    /// </summary>
    protected virtual string JsonExtractMethod => "JSON_EXTRACT";
    
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

        if(connectionString.Contains(";Uid="))
            return new MySqlDbManager(connectionString);
        
        return new SqlServerDbManager(connectionString);
    }

    /// <summary>
    /// Gets the file of the default database
    /// </summary>
    public static string SqliteDbFile => Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");

    /// <summary>
    /// Gets the default database connection string using the Sqlite database file
    /// </summary>
    /// <returns>the default database connection string using the Sqlite database file</returns>
    public static string GetDefaultConnectionString() => SqliteDbManager.GetConnetionString(SqliteDbFile);
    
    /// <summary>
    /// Get an instance of the IDatabase
    /// </summary>
    /// <returns>an instance of the IDatabase</returns>
    protected abstract IDatabase GetDb();


    #region Setup Code

    /// <summary>
    /// Creates the database and the initial data
    /// </summary>
    /// <param name="recreate">if the database should be recreated if already exists</param>
    /// <param name="insertInitialData">if the initial data should be inserted</param>
    /// <returns>if the database was successfully created or not</returns>
    public async Task<bool> CreateDb(bool recreate = false, bool insertInitialData = true)
    {
        var dbResult = CreateDatabase(recreate);
        if (dbResult == DbCreateResult.Failed)
            return false;

        if (dbResult == DbCreateResult.AlreadyExisted)
        {
            CreateStoredProcedures();
            return true;
        }
        
        if (CreateDatabaseStructure() == false)
            return false;

        CreateStoredProcedures();


        if (recreate == false && this is SqliteDbManager == false)
        {
            // not a sqlite database, check if one exists and migrate
            if (File.Exists(SqliteDbFile))
            {
                // migrate the data
                bool migrated = DbMigrater.Migrate(SqliteDbManager.GetConnetionString(SqliteDbFile), this.ConnectionString);

                if (migrated)
                {
                    File.Move(SqliteDbFile, SqliteDbFile + ".migrated");
                }
                
                // migrated, we dont need to insert initial data
                return true;
            }
        }

        if (insertInitialData == false)
            return true;
        
        return await CreateInitialData();
    }

    /// <summary>
    /// Creates the actual Database
    /// </summary>
    /// <param name="recreate">if the database should be recreated if already exists</param>
    /// <returns>true if successfully created</returns>
    protected abstract DbCreateResult CreateDatabase(bool recreate);
    /// <summary>
    /// Creates the tables etc in the database
    /// </summary>
    /// <returns>true if successfully created</returns>
    protected abstract bool CreateDatabaseStructure();

    /// <summary>
    /// Creates (or recreates) any stored procedures and functions used by this database
    /// </summary>
    protected virtual void CreateStoredProcedures() { }

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
            Uid = Globals.InternalNodeUid,
            Name = Globals.InternalNodeName,
            Address = Globals.InternalNodeName,
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
    public virtual async Task<IEnumerable<T>> Select<T>() where T : FileFlowObject, new()
    {
        using var db = GetDb();
        DateTime start = DateTime.Now;
        var dbObjects = await db.FetchAsync<DbObject>("where Type=@0", typeof(T).FullName);
        return ConvertFromDbObject<T>(dbObjects);
    }

    /// <summary>
    /// Converts DbObjects into strong types
    /// </summary>
    /// <param name="dbObjects">a collection of DbObjects</param>
    /// <typeparam name="T">The type to convert to</typeparam>
    /// <returns>A converted list of objects</returns>
    internal IEnumerable<T> ConvertFromDbObject<T>(IEnumerable<DbObject> dbObjects) where T : FileFlowObject, new()
    {
        var list = dbObjects.ToList();
        T[] results = new T [list.Count];
        Parallel.ForEach(list, (x, state, index) =>
        {
            var converted = Convert<T>(x);
            if(converted != null)
                results[index] = converted;
        });
        return results.Where(x => x != null);
    }

    /// <summary>
    /// Converts DbObject into strong type
    /// </summary>
    /// <param name="dbObject">the DbObject to convert</param>
    /// <typeparam name="T">The type to convert to</typeparam>
    /// <returns>A converted object</returns>
    internal T ConvertFromDbObject<T>(DbObject dbObject)where T : FileFlowObject, new()
    {
        if(dbObject == null)
            return default;
        return Convert<T>(dbObject);
    }

    /// <summary>
    /// Selects types from the database
    /// </summary>
    /// <param name="where">a where clause for the select</param>
    /// <param name="arguments">the arguments for the select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a list of objects</returns>
    public virtual async Task<IEnumerable<T>> Select<T>(string where, params object[] arguments) where T : FileFlowObject, new()
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
    public virtual async Task<IEnumerable<string>> GetNames<T>(string andWhere = "", params object[] args)
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
    public virtual async Task<Dictionary<Guid, string>> GetIndexedNames<T>(string andWhere = "", params object[] args)
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
    public virtual bool NameInUse<T>(Guid uid, string name)
    {
        using var db = GetDb();
        string sql = $"Name from {nameof(DbObject)} where Type=@0 and uid <> @1 and Name = @2";
        if (UseTop)
            sql = "select top 1 " + sql;
        else
            sql = "select " + sql + " limit 1";
        
        string result = db.FirstOrDefault<string>(sql, typeof(T).FullName, uid, name);
        return string.IsNullOrEmpty(result) == false;
    }

    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <typeparam name="T">The type to select</typeparam>
    /// <returns>a single instance</returns>
    public virtual async Task<T> Single<T>() where T : FileFlowObject, new()
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
    public virtual async Task<T> Single<T>(Guid uid) where T : FileFlowObject, new()
    {
        using var db = GetDb();
        
        var dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0 and Uid=@1", typeof(T).FullName, uid);
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
    public virtual async Task<T> SingleByName<T>(string name) where T : FileFlowObject, new()
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
    private async Task<T> AddOrUpdateObject<T>(IDatabase db, T obj) where T : FileFlowObject, new()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DataConverter(), new BoolConverter(), new Shared.Json.ValidatorConverter() }
        };
        // need to case obj to (ViObject) here so the DataConverter is used
        string json = JsonSerializer.Serialize((FileFlowObject)obj, serializerOptions);

        var type = obj.GetType();
        obj.Name = obj.Name?.EmptyAsNull() ?? type.Name;
        var dbObject = db.FirstOrDefault<DbObject>("where Type=@0 and Uid = @1", type.FullName, obj.Uid);
        if (dbObject == null)
        {
            if(obj.Uid == Guid.Empty)
                obj.Uid = Guid.NewGuid();
            obj.DateCreated = DateTime.Now;
            obj.DateModified = obj.DateCreated;
            // create new 
            dbObject = new DbObject
            {
                Uid = obj.Uid,
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

        if (UseMemoryCache == false)
            return await Single<T>(dbObject.Uid);//return await Single<T>(Guid.Parse(dbObject.Uid));
        
        return obj;
    }
    

    /// <summary>
    /// Updates the last modified date of an object
    /// </summary>
    /// <param name="uid">the UID of the object to update</param>
    internal virtual  async Task UpdateLastModified(Guid uid)
    {
        using var db = GetDb();
        await db.ExecuteAsync($"update {nameof(DbObject)} set DateModified = @0 where Uid = @1", DateTime.Now, uid);
    }

    /// <summary>
    /// This will batch insert many objects into thee database
    /// </summary>
    /// <param name="items">Items to insert</param>
    internal virtual async Task AddMany(FileFlowObject[] items)
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
    public virtual async Task<T> Single<T>(string andWhere, params object[] args) where T : FileFlowObject, new()
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
        T result = JsonSerializer.Deserialize<T>(dbObject.Data, serializerOptions);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        //result.Uid = Guid.Parse(dbObject.Uid);
        result.Uid = dbObject.Uid;
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
    public virtual async Task<T> Update<T>(T obj) where T : FileFlowObject, new()
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
    /// <param name="andWhere">and where clause</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to delete</typeparam>
    public virtual async Task Delete<T>(string andWhere = "", params object[] args)
    {
        using var db = GetDb();
        
        if (string.IsNullOrEmpty(andWhere) == false && andWhere.Trim().ToLower().StartsWith("and ") == false)
            andWhere = " and " + andWhere;
        args = new object[] { typeof(T).FullName }.Union(args ?? new object[] { }).ToArray();
        string sql = $"delete from {nameof(DbObject)} where Type=@0 {andWhere}";
        await db.ExecuteAsync(sql, args);
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


    /// <summary>
    /// Finds an existing library file in the database
    /// </summary>
    /// <param name="fullPath">the full path of the library file</param>
    /// <returns>the result of the known library file</returns>
    public virtual  async Task<LibraryFile> FindKnownLibraryFile(string fullPath)
    {
        using var db = GetDb();

        // first see if this file exists by its name
        var dbObject = await db.FirstOrDefaultAsync<DbObject>(
            "where Type=@0 and name = @1", typeof(LibraryFile).FullName, fullPath);
        if (string.IsNullOrEmpty(dbObject?.Data) == false)
            return Convert<LibraryFile>(dbObject);

        return new LibraryFile();
    }

    /// <summary>
    /// Finds an existing library file in the database by a fingerprint
    /// </summary>
    /// <param name="fingerprint">the fingerprint of the file</param>
    /// <returns>the result of the known file</returns>
    public virtual async Task<LibraryFile> FindKnownLibraryByFingerprint(string fingerprint)
    {
        if (string.IsNullOrEmpty(fingerprint))
            return new LibraryFile();

        using var db = GetDb();

        var dbObject = await db.FirstOrDefaultAsync<DbObject>(
                $"where Type=@0 and {JsonExtractMethod}(Data, '$.Fingerprint') = @1", typeof(LibraryFile).FullName, fingerprint ?? string.Empty);

        if (string.IsNullOrEmpty(dbObject?.Data) == false)
            return Convert<LibraryFile>(dbObject);

        return new LibraryFile();
    }


    /// <summary>
    /// Reads in a embedded SQL script
    /// </summary>
    /// <param name="dbType">The type of database this script is for</param>
    /// <param name="script">The script name</param>
    /// <returns>the sql script</returns>
    public static string GetSqlScript(string dbType, string script)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"FileFlows.Server.Database.Scripts.{dbType}.{script}";

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex) 
        {
            Logger.Instance.ELog($"Failed getting embedded SQL script '{dbType}.{script}': {ex.Message}");
            return string.Empty; 
        }
    }

    /// <summary>
    /// Gets the sql scripts for a database
    /// </summary>
    /// <param name="dbType">the type of database</param>
    /// <returns>a list of stored procedures</returns>
    protected static Dictionary<string,string> GetStoredProcedureScripts(string dbType)
    {
        Dictionary<string, string> scripts = new();
        foreach (string script in new[] { "GetLibraryFiles" })
        {
            string sql = GetSqlScript(dbType, script + ".sql");
            scripts.Add(script, sql);
        }
        return scripts;
    }

    /// <summary>
    /// Gets the library file status  
    /// </summary>
    /// <returns>the library file status counts</returns>
    public abstract Task<IEnumerable<LibraryStatus>> GetLibraryFileOverview();

    /// <summary>
    /// Gets the library file with the corresponding status
    /// </summary>
    /// <param name="status">the library file status</param>
    /// <param name="start">the row to start at</param>
    /// <param name="max">the maximum items to return</param>
    /// <param name="quarter">the current quarter</param>
    /// <param name="nodeUid">optional UID of node to limit results for</param>
    /// <returns>an enumerable of library files</returns>
    public abstract Task<IEnumerable<LibraryFile>> GetLibraryFiles(FileStatus status, int start, int max, int quarter, Guid? nodeUid);

    /// <summary>
    /// Gets an item from the database by it's name
    /// </summary>
    /// <param name="name">the name of the object</param>
    /// <typeparam name="T">the type to fetch</typeparam>
    /// <returns>the object if found</returns>
    public virtual async Task<T> GetByName<T>(string name) where T : FileFlowObject, new()
    {
        using var db = GetDb();

        // first see if this file exists by its name
        var dbObject = await db.FirstOrDefaultAsync<DbObject>(
            "where Type=@0 and name = @1", typeof(T).FullName, name);
        if (string.IsNullOrEmpty(dbObject?.Data) == false)
            return Convert<T>(dbObject);

        return new ();
    }
    
    /// <summary>
    /// Gets the failure flow for a particular library
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>the failure flow</returns>
    public abstract Task<Flow> GetFailureFlow(Guid libraryUid);
    
    
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