namespace FileFlows.Server.Helpers
{
    using System;
    using System.Collections.Generic;
    using NPoco;
    using FileFlows.Server.Models;
    using System.Data.SQLite;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Text;

    public class DbHelper
    {
        public static bool UseMySql = false;

        static string CreateDBSript =
            @$"CREATE TABLE {nameof(DbObject)}(
                Uid             VARCHAR(36)           NOT NULL          PRIMARY KEY,
                Name            VARCHAR(255)       NOT NULL,
                Type            VARCHAR(255)       NOT NULL,
                DateCreated     datetime           default           current_timestamp,
                DateModified    datetime           default           current_timestamp,
                Data            TEXT               NOT NULL
            );";
        // for mysql change "current_timestamp" to "now()"


        private static IDatabase GetDb()
        {
            if (UseMySql)
                return new Database("Server=localhost;Uid=root;Pwd=root;Database=FileFlows", null, MySqlConnector.MySqlConnectorFactory.Instance);
            return UseSqlLite();
        }

        private static Database UseSqlLite()
        {
            return new Database("Data Source=Data/FileFlows.sqlite;Version=3;", null, SQLiteFactory.Instance);
        }

        public static IEnumerable<T> Select<T>() where T : ViObject, new()
        {
            using (var db = GetDb())
            {
                var dbObjects = db.Fetch<DbObject>("where Type=@0 order by Name", typeof(T).FullName);
                return dbObjects.Select(x => Convert<T>(x));
            }
        }

        public static IEnumerable<string> GetNames<T>(string andWhere = "", params object[] args)
        {
            using (var db = GetDb())
            {
                if (string.IsNullOrEmpty(andWhere) == false && andWhere.Trim().ToLower().StartsWith("and ") == false)
                    andWhere = " and " + andWhere;
                args = new object[] { typeof(T).FullName }.Union(args ?? new object[] { }).ToArray();
                return db.Fetch<string>($"select Name from {nameof(DbObject)} where Type=@0 {andWhere} order by name", args);
            }
        }

        public static T Single<T>() where T : ViObject, new()
        {
            using (var db = GetDb())
            {
                var dbObject = db.FirstOrDefault<DbObject>("where Type=@0", typeof(T).FullName);
                if (string.IsNullOrEmpty(dbObject?.Data))
                    return new T();
                return Convert<T>(dbObject);
            }
        }

        public static T Single<T>(Guid uid) where T : ViObject, new()
        {
            using (var db = GetDb())
            {
                var dbObject = db.FirstOrDefault<DbObject>("where Type=@0 and Uid=@1", typeof(T).FullName, uid.ToString());
                if (string.IsNullOrEmpty(dbObject?.Data))
                    return new T();
                return Convert<T>(dbObject);
            }
        }

        public static T SingleByName<T>(string name) where T : ViObject, new()
        {
            using (var db = GetDb())
            {
                var dbObject = db.FirstOrDefault<DbObject>("where Type=@0 and lower(Name)=lower(@1)", typeof(T).FullName, name);
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

        internal static void UpdateLastModified(Guid uid)
        {
            using (var db = GetDb())
            {
                db.Execute($"update {nameof(DbObject)} set DateModified = @0 where Uid = @1", DateTime.Now, uid);
            }
        }

        /// <summary>
        /// This will batch insert many objects into thee datbase
        /// </summary>
        /// <param name="items">Items to insert</param>
        internal static void AddMany(ViObject[] items)
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
                    string json = System.Text.Json.JsonSerializer.Serialize((ViObject)obj, serializerOptions);

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
                if(sql.Length > 0)
                {
                    using (var db = GetDb())
                    {
                        db.Execute(sql.ToString());
                    }
                }                    
            }

        }

        public static T Single<T>(string andWhere, params object[] args) where T : ViObject, new()
        {
            using (var db = GetDb())
            {
                args = new object[] { typeof(T).FullName }.Union(args).ToArray();
                var dbObject = db.FirstOrDefault<DbObject>("where Type=@0 and " + andWhere, args);
                if (string.IsNullOrEmpty(dbObject?.Data))
                    return new T();
                return Convert<T>(dbObject);
            }
        }
        private static T Convert<T>(DbObject dbObject) where T : ViObject, new()
        {
            var serializerOptions = new System.Text.Json.JsonSerializerOptions
            {
                Converters = { new BoolConverter() }
            };
            // need to case obj to (ViObject) here so the DataConverter is used
            T result = System.Text.Json.JsonSerializer.Deserialize<T>(dbObject.Data, serializerOptions);
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

        public static T Update<T>(T obj) where T : ViObject, new()
        {
            if (obj == null)
                return new T();
            using (var db = GetDb())
            {
                return AddOrUpdateObject(db, obj);
            }
        }

        private static T AddOrUpdateObject<T>(IDatabase db, T obj) where T : ViObject
        {
            var serializerOptions = new System.Text.Json.JsonSerializerOptions
            {
                Converters = { new DataConverter(), new BoolConverter() }
            };
            // need to case obj to (ViObject) here so the DataConverter is used
            string json = System.Text.Json.JsonSerializer.Serialize((ViObject)obj, serializerOptions);

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
                db.Insert(dbObject);
            }
            else
            {
                obj.DateModified = DateTime.Now;
                dbObject.Name = obj.Name;
                dbObject.DateModified = obj.DateModified;
                dbObject.Data = json;
                db.Update(dbObject);
            }
            return obj;
        }

        public static void Delete<T>(params Guid[] uids) where T : ViObject
        {
            if (uids?.Any() != true)
                return; // nothing to delete

            string typeName = typeof(T).FullName;
            string strUids = String.Join(",", uids.Select(x => "'" + x.ToString() + "'"));
            using (var db = GetDb())
            {
                db.Delete<DbObject>($"where Type=@0 and Uid in ({strUids})", typeName);
            }
        }

        public static bool StartMySqlServer()
        {
            if (UseMySql == false)
                return true;
            Logger.Instance.ILog("Starting mysql service");
            using (var p = Process.Start(new ProcessStartInfo
            {
                //FileName = "etc/init.d/mysql",
                //Arguments = "start",
                FileName = "service",
                Arguments = "mysql start",
                WorkingDirectory = "/"
            }))
            {
                p.WaitForExit();
                bool started = p.ExitCode == 0;
                if (started == false)
                    return false;
            }
            using (var p = Process.Start(new ProcessStartInfo
            {
                FileName = "mysql",
                Arguments = "-uroot -e \"ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password BY 'root';\"",
                WorkingDirectory = "/"
            }))
            {
                p.WaitForExit();
                bool started = p.ExitCode == 0;
                if (started == false)
                    return false;
            }
            return true;
        }

        public static bool CreateDatabase(string connectionString = "Server=localhost;Uid=root;Pwd=root;")
        {
            if (UseMySql == false)
                return CreateSqliteDatabase();
            else
                return CreateMySqlDatabase(connectionString);
        }

        private static bool CreateSqliteDatabase()
        {
            if (Directory.Exists("Data") == false)
                Directory.CreateDirectory("Data");

            string dbFile = "Data/FileFlows.sqlite";

            if (System.IO.File.Exists(dbFile) == false)
                SQLiteConnection.CreateFile(dbFile);

            using (var con = new SQLiteConnection($"Data Source={dbFile};Version=3;"))
            {
                con.Open();
                using (var cmd = new SQLiteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{nameof(DbObject)}'", con))
                {
                    if (cmd.ExecuteScalar() != null)
                        return true;// tables exist, all good                    
                }
                using (var cmd = new SQLiteCommand(CreateDBSript, con))
                {
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }

            using var db = UseSqlLite();
            AddInitialData(db);
            return true;
        }

        private static bool CreateMySqlDatabase(string connectionString)
        {
            var db = new Database(connectionString, null, MySqlConnector.MySqlConnectorFactory.Instance);

            bool exists = String.IsNullOrEmpty(db.ExecuteScalar<string>("select schema_name from information_schema.schemata where schema_name = 'FileFlows'")) == false;
            if (exists)
            {
                db.Dispose();
                return true;
            }

            db.Execute("create database FileFlows");
            // dispose of original one
            db.Dispose();

            // create new one pointing ot the database
            db = new Database(connectionString + "Database=FileFlows", null, MySqlConnector.MySqlConnectorFactory.Instance);

            db.Execute(CreateDBSript);

            AddInitialData(db);

            return true;
        }

        private static void AddInitialData(Database db)
        {
            AddOrUpdateObject(db, new Tool
            {
                Name = "FFMpeg",
                Path = "/usr/local/bin/ffmpeg",
                DateCreated = DateTime.Now,
                DateModified = DateTime.Now
            });
            AddOrUpdateObject(db, new Settings
            {
                Name = "Settings",
                TempPath = "/temp",
                LoggingPath = "/app/Logs",
                DateCreated = DateTime.Now,
                DateModified = DateTime.Now
            });
        }
    }
}