namespace FileFlows.Server.Helpers;

using System;
using System.Collections.Generic;
using NPoco;
using FileFlows.Server.Models;
using System.Data.SQLite;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using System.Text;

/// <summary>
/// Database helper for communicating with the database
/// </summary>
public class DbHelper
{
    private static string DbFilename;

    static string CreateDbScript =
        @$"CREATE TABLE {nameof(DbObject)}(
            Uid             VARCHAR(36)           NOT NULL          PRIMARY KEY,
            Name            VARCHAR(255)       NOT NULL,
            Type            VARCHAR(255)       NOT NULL,
            DateCreated     datetime           default           current_timestamp,
            DateModified    datetime           default           current_timestamp,
            Data            TEXT               NOT NULL
        );";
    // for mysql change "current_timestamp" to "now()"

    
    /// <summary>
    /// Gets an instance of the database
    /// </summary>
    /// <returns>an instance of the database</returns>
    internal static IDatabase GetDb()
    {
        try
        {
            return new Database($"Data Source={DbFilename};Version=3;", null, SQLiteFactory.Instance);
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Error loading database: " + ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Select a list of objects
    /// </summary>
    /// <typeparam name="T">the type of objects to select</typeparam>
    /// <returns>a list of objects</returns>
    public static async Task<IEnumerable<T>> Select<T>() where T : FileFlowObject, new()
    {
        using (var db = GetDb())
        {
            var dbObjects = await db.FetchAsync<DbObject>("where Type=@0 order by Name", typeof(T).FullName);
            return dbObjects.Select(x => Convert<T>(x));
        }
    }
    
    /// <summary>
    /// Selects types from the database
    /// </summary>
    /// <param name="where">a where clause for the select</param>
    /// <param name="arguments">the arguments for the select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a list of objects</returns>
    public static async Task<IEnumerable<T>> Select<T>(string where, params object[] arguments) where T : FileFlowObject, new()
    {
        using (var db = GetDb())
        {
            var dbObjects = await db.FetchAsync<DbObject>($"where Type=@0 and {where} order by Name", typeof(T).FullName, arguments);
            return dbObjects.Select(x => Convert<T>(x));
        }
    }

    /// <summary>
    /// Get names of types
    /// </summary>
    /// <param name="andWhere">and where caluse</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to select</typeparam>
    /// <returns>a list of names</returns>
    public static async Task<IEnumerable<string>> GetNames<T>(string andWhere = "", params object[] args)
    {
        using (var db = GetDb())
        {
            if (string.IsNullOrEmpty(andWhere) == false && andWhere.Trim().ToLower().StartsWith("and ") == false)
                andWhere = " and " + andWhere;
            args = new object[] { typeof(T).FullName }.Union(args ?? new object[] { }).ToArray();
            return await db.FetchAsync<string>($"select Name from {nameof(DbObject)} where Type=@0 {andWhere} order by name", args);
        }
    }

    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <typeparam name="T">The type to select</typeparam>
    /// <returns>a single instance</returns>
    public static async Task<T> Single<T>() where T : FileFlowObject, new()
    {
        using (var db = GetDb())
        {
            var dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0", typeof(T).FullName);
            if (string.IsNullOrEmpty(dbObject?.Data))
                return new T();
            return Convert<T>(dbObject);
        }
    }

    /// <summary>
    /// Selects a single instance
    /// </summary>
    /// <param name="uid">the UID of the item to select</param>
    /// <typeparam name="T">the type of item to select</typeparam>
    /// <returns>a single instance</returns>
    public static async Task<T> Single<T>(Guid uid) where T : FileFlowObject, new()
    {
        using (var db = GetDb())
        {
            var dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0 and Uid=@1", typeof(T).FullName, uid.ToString());
            if (string.IsNullOrEmpty(dbObject?.Data))
                return new T();
            return Convert<T>(dbObject);
        }
    }

    /// <summary>
    /// Selects a single instance by its name
    /// </summary>
    /// <param name="name">the name of the item to select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a single instance</returns>
    public static async Task<T> SingleByName<T>(string name) where T : FileFlowObject, new()
    {
        using (var db = GetDb())
        {
            var dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0 and lower(Name)=lower(@1)", typeof(T).FullName, name);
            if (string.IsNullOrEmpty(dbObject?.Data))
                return new T();
            return Convert<T>(dbObject);
        }
    }

    private static string SqlEscape(string input)
    {
        if (input == null)
            return string.Empty;
        return "'" + input.Replace("'", "''") + "'";
    }

    /// <summary>
    /// Updates the last modified date of an object
    /// </summary>
    /// <param name="uid">the UID of the object to update</param>
    internal static async Task UpdateLastModified(Guid uid)
    {
        using (var db = GetDb())
        {
            await db.ExecuteAsync($"update {nameof(DbObject)} set DateModified = @0 where Uid = @1", DateTime.Now, uid);
        }
    }

    /// <summary>
    /// This will batch insert many objects into thee datbase
    /// </summary>
    /// <param name="items">Items to insert</param>
    internal static async Task AddMany(FileFlowObject[] items)
    {
        if (items?.Any() != true)
            return;
        int max = 500;
        int count = items.Length;

        var serializerOptions = new System.Text.Json.JsonSerializerOptions
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
                string json = System.Text.Json.JsonSerializer.Serialize((FileFlowObject)obj, serializerOptions);

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
                using (var db = GetDb())
                {
                    await db.ExecuteAsync(sql.ToString());
                }
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
    public static async Task<T> Single<T>(string andWhere, params object[] args) where T : FileFlowObject, new()
    {
        using (var db = GetDb())
        {
            args = new object[] { typeof(T).FullName }.Union(args).ToArray();
            var dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0 and " + andWhere, args);
            if (string.IsNullOrEmpty(dbObject?.Data))
                return new T();
            return Convert<T>(dbObject);
        }
    }
    
    private static T Convert<T>(DbObject dbObject) where T : FileFlowObject, new()
    {
        var serializerOptions = new System.Text.Json.JsonSerializerOptions
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

    // public static T Update<T>(T obj) where T : ViObject, new()
    // {
    //     var updated = Update((ViObject)obj);
    //     if (updated.Uid == Guid.Empty)
    //         return new T(); // since we hate nulls
    //     return Convert<T>(updated);
    // }

    /// <summary>
    /// Updates an object
    /// </summary>
    /// <param name="obj">the object to update</param>
    /// <typeparam name="T">the object type</typeparam>
    /// <returns>the updated object</returns>
    public static async Task<T> Update<T>(T obj) where T : FileFlowObject, new()
    {
        if (obj == null)
            return new T();
        using (var db = GetDb())
        {
            return await AddOrUpdateObject(db, obj);
        }
    }

    private static async Task<T> AddOrUpdateObject<T>(IDatabase db, T obj) where T : FileFlowObject
    {
        var serializerOptions = new System.Text.Json.JsonSerializerOptions
        {
            Converters = { new DataConverter(), new BoolConverter(), new Shared.Json.ValidatorConverter() }
        };
        // need to case obj to (ViObject) here so the DataConverter is used
        string json = System.Text.Json.JsonSerializer.Serialize((FileFlowObject)obj, serializerOptions);

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
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <typeparam name="T">The type of objects being deleted</typeparam>
    public static async Task Delete<T>(params Guid[] uids) where T : FileFlowObject
    {
        if (uids?.Any() != true)
            return; // nothing to delete

        var typeName = typeof(T).FullName;
        string strUids = String.Join(",", uids.Select(x => "'" + x.ToString() + "'"));
        using (var db = GetDb())
        {
            await db.ExecuteAsync($"delete from {nameof(DbObject)} where Type=@0 and Uid in ({strUids})", typeName);
        }
    }

    /// <summary>
    /// Creates the database
    /// </summary>
    /// <param name="connectionString">the connection string to the database</param>
    /// <returns>true if successfully created</returns>
    public static async Task<bool> CreateDatabase(string connectionString = "Server=localhost;Uid=root;Pwd=root;")
    {
        try
        {
            return await CreateSqliteDatabase();
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed creating database: " + ex.Message);
            return false;
        }
    }

#if (DEBUG)
    /// <summary>
    /// Clean the database and purge old data
    /// </summary>
    /// <returns>True if successful</returns>
    public static async Task<bool> CleanDatabase()
    {
        try
        {
            var db = GetDb();
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

    private static string GetDbFilename()
    {
        DbFilename = Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");
        return DbFilename;
    }

    private static async Task<bool> CreateSqliteDatabase()
    {
        string dbFile = GetDbFilename();

        if (Program.Docker)
        {
            var fi = new FileInfo(dbFile);
            string parentFile = Path.Combine(fi.Directory.Parent.FullName, fi.Name);
            if (File.Exists(parentFile))
            {
                Logger.Instance.ILog("Moving parent folder db folder");
                FileHelper.MoveFile(parentFile, dbFile);                    
            }
        }

        if (System.IO.File.Exists(dbFile) == false)
            SQLiteConnection.CreateFile(dbFile);
        else
        {
            // create backup 
            File.Copy(dbFile, dbFile + ".backup", true);
        }

        using (var con = new SQLiteConnection($"Data Source={dbFile};Version=3;"))
        {
            con.Open();
            using (var cmd = new SQLiteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{nameof(DbObject)}'", con))
            {
                if (cmd.ExecuteScalar() != null)
                    return true;// tables exist, all good                    
            }
            using (var cmd = new SQLiteCommand(CreateDbScript, con))
            {
                cmd.ExecuteNonQuery();
            }
            con.Close();
        }

        using var db = GetDb();
        await AddInitialData(db);
        return true;
    }

    private static async Task AddInitialData(IDatabase db)
    {
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
    }
}