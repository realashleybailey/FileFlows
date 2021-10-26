namespace ViWatcher.Server.Helpers
{
    using System;
    using System.Collections.Generic;
    using NPoco;
    using Newtonsoft.Json;
    using ViWatcher.Server.Models;
    using System.Data.SQLite;

    public class DbHelper
    {

        private static IDatabase GetDb()
        {

            if (!File.Exists("Data/viwatcher.sqlite"))
            {
                SQLiteConnection.CreateFile("Data/viwatcher.sqlite");

                string sql = @$"CREATE TABLE {nameof(DbObject)}(
                               Uid             CHAR(36)           NOT NULL          PRIMARY KEY,
                               Name            VARCHAR(255)       NOT NULL,
                               Type            VARCHAR(255)       NOT NULL,
                               DateCreated     datetime           default           current_timestamp,
                               DateModified    datetime           default           current_timestamp,
                               Data            TEXT               NOT NULL
                            );";
                using (var con = new SQLiteConnection("Data Source=Data/viwatcher.sqlite;Version=3;"))
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(sql, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    con.Close();
                }
            }

            var db = new Database("Data Source=Data/viwatcher.sqlite;Version=3;", null, SQLiteFactory.Instance);
            return db;
        }

        public static IEnumerable<T> Select<T>()
        {
            using (var db = GetDb())
            {
                var dbObjects = db.Fetch<DbObject>("where Type=@0", typeof(T).FullName);
                return dbObjects.Select(x => Convert<T>(x));
            }
        }

        public static T Single<T>() where T : new() 
        {            
            using (var db = GetDb())
            {
                var dbObject = db.FirstOrDefault<DbObject>("where Type=@0", typeof(T).FullName);                
                if(string.IsNullOrEmpty(dbObject?.Data))
                    return new T();
                return Convert<T>(dbObject);
            }
        }


        private static T Convert<T>(DbObject dbObject)
        {            
            T result = JsonConvert.DeserializeObject<T>(dbObject.Data);
            if(result is ViWatcher.Shared.Models.ViObject viObj)
            {
                viObj.Uid = Guid.Parse(dbObject.Uid);
                viObj.Name = dbObject.Name;
                viObj.DateModified = dbObject.DateModified;
                viObj.DateCreated = dbObject.DateCreated;
            }
            return result;
        }

        public static T Update<T>(T obj) where T: new()
        {
            DbObject dbObject = Update((object)obj);
            if(string.IsNullOrEmpty(dbObject.Uid))
                return new T(); // since we hate nulls
            return Convert<T>(dbObject);
        }

        public static DbObject Update(object obj)
        {
            if(obj == null)
                return new DbObject();
            string json = JsonConvert.SerializeObject(obj);
            using (var db = GetDb())
            {
                var type = obj.GetType();
                var dbObject = db.FirstOrDefault<DbObject>("where Type=@0", type.FullName);     
                if(dbObject == null){
                    // create new 
                    dbObject = new DbObject
                    {
                        Uid = Guid.NewGuid().ToString(),
                        DateCreated = DateTime.Now,
                        DateModified = DateTime.Now,
                        Type = type.FullName,
                        Name = type.Name,
                        Data = json
                    };
                    db.Insert(dbObject);
                }
                else
                {
                    dbObject.DateModified = DateTime.Now;
                    dbObject.Data = json;
                    db.Update(dbObject);
                }
                return dbObject;
            }
        }

    }
}