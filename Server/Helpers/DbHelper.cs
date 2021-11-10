namespace FileFlow.Server.Helpers
{
    using System;
    using System.Collections.Generic;
    using NPoco;
    using FileFlow.Server.Models;
    using System.Data.SQLite;
    using FileFlow.Shared;
    using FileFlow.Shared.Models;

    public class DbHelper
    {

        private static IDatabase GetDb()
        {

            if (File.Exists("Data/FileFlow.sqlite") == false)
            {
                if (Directory.Exists("Data") == false)
                    Directory.CreateDirectory("Data");

                SQLiteConnection.CreateFile("Data/FileFlow.sqlite");

                string sql = @$"CREATE TABLE {nameof(DbObject)}(
                               Uid             CHAR(36)           NOT NULL          PRIMARY KEY,
                               Name            VARCHAR(255)       NOT NULL,
                               Type            VARCHAR(255)       NOT NULL,
                               DateCreated     datetime           default           current_timestamp,
                               DateModified    datetime           default           current_timestamp,
                               Data            TEXT               NOT NULL
                            );";
                using (var con = new SQLiteConnection("Data Source=Data/FileFlow.sqlite;Version=3;"))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    con.Close();
                }
            }

            var db = new Database("Data Source=Data/FileFlow.sqlite;Version=3;", null, SQLiteFactory.Instance);
            return db;
        }

        public static IEnumerable<T> Select<T>() where T : ViObject, new()
        {
            using (var db = GetDb())
            {
                var dbObjects = db.Fetch<DbObject>("where Type=@0 order by name", typeof(T).FullName);
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
            var serializerOptions = new System.Text.Json.JsonSerializerOptions
            {
                Converters = { new DataConverter(), new BoolConverter() }
            };
            // need to case obj to (ViObject) here so the DataConverter is used
            string json = System.Text.Json.JsonSerializer.Serialize((ViObject)obj, serializerOptions);
            using (var db = GetDb())
            {
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
        }
        public static void Delete<T>(params Guid[] uids) where T : ViObject
        {
            string typeName = typeof(T).FullName;
            using (var db = GetDb())
            {
                foreach (var uid in uids)
                {
                    db.Delete<DbObject>("where Type=@0 and Uid = @1", typeName, uid.ToString());
                }
            }
        }
    }
}