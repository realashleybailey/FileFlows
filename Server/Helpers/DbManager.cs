using FileFlows.Server.Models;
using FileFlows.Shared.Models;
using System.Text;

namespace FileFlows.Server.Helpers
{
    public class DbManager
    {
        private enum Priority
        {
            Low = 0,
            Medium = 1,
            High = 2
        }

        private static bool Executing = false;
        private static Mutex mutex = new Mutex(false);
        PriorityQueue<Action, Priority> queue = new ();

        public static async Task<bool> DeleteAll(params Guid[] uids)
        {
            if (uids?.Any() != true)
                return true;

            string strUids = String.Join(",", uids.Select(x => "'" + x.ToString() + "'"));

            mutex.WaitOne();
            try
            {
                using(var db = DbHelper.GetDb())
                {
                    db.Execute($"delete from {nameof(DbObject)} where Uid in ({strUids})");
                }
                return await Task.FromResult(true);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        internal static async Task<T> Single<T>() where T : ViObject, new()
        {
            mutex.WaitOne();
            try
            {
                return await DbHelper.Single<T>();
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        internal static async Task<T> Update<T>(T model) where T : ViObject, new()
        {
            mutex.WaitOne();
            try
            {
                return await DbHelper.Update(model);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        internal static async Task UpdateDateModified(Guid uid, DateTime date)
        {
            mutex.WaitOne();
            try
            {
                var db = DbHelper.GetDb();
                db.Execute($"update {nameof(DbObject)} set {nameof(DbObject.DateModified)} = @0", date);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
