namespace FileFlows.Server.Controllers
{
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;
    using Microsoft.AspNetCore.Mvc;

    public abstract class ControllerStore<T>:Controller where T : ViObject, new()
    {
        protected static Dictionary<Guid, T> Data;
        private static Mutex _mutex = new Mutex(false);

        protected async Task<IEnumerable<string>> GetNames(Guid? uid = null)
        {
            var data = await GetData();
            if(uid == null)
                return data.Select(x => x.Value.Name);
            return data.Where(x => x.Key != uid.Value).Select(x => x.Value.Name);
        }

        protected async Task<bool> NameInUse(Guid uid, string name)
        {
            name = name.ToLower().Trim();
            return (await GetData()).Any(x => uid != x.Key && x.Value.Name.ToLower() == name);
        }

        protected void ClearData() => Data = null;

        internal async Task<Dictionary<Guid, T>> GetData()
        {
            if (Data == null)
            {
                _mutex.WaitOne();
                try
                {
                    if (Data == null) // make sure its still null after getting mutex
                    {
                        Data = (await DbHelper.Select<T>())
                                             .ToDictionary(x => x.Uid, x => x);
                    }
                }
                finally
                {
                    _mutex?.ReleaseMutex(); 
                }
            }
            return Data;
        }

        internal async Task<List<T>> GetDataList() => (await GetData()).Values.ToList();

        protected async Task<T> GetByUid(Guid uid)
        {
            var data = await GetData();
            if(data.ContainsKey(uid))
                return data[uid];
            return default;
        }

        protected async Task DeleteAll(ReferenceModel model)
        {
            if (model?.Uids?.Any() != true)
                return;
            await DeleteAll(model.Uids);
        }

        internal async Task DeleteAll(params Guid[] uids)
        {
            var data = await GetData();
            foreach(var uid in uids)
            {
                _mutex.WaitOne();
                try
                {
                    if (data.ContainsKey(uid))  
                    data.Remove(uid);
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
            await DbManager.DeleteAll(uids);
        }

        internal async Task<T> Update(T model, bool checkDuplicateName = false)
        {
            if (checkDuplicateName)
            {
                if(await NameInUse(model.Uid, model.Name))
                    throw new Exception("ErrorMessages.NameInUse");
            }
            var updated = await DbManager.Update(model);
            _mutex.WaitOne();
            try
            {
                if (Data.ContainsKey(updated.Uid))
                    Data[updated.Uid] = updated;
                else
                    Data.Add(updated.Uid, updated);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
            return updated;
        }
        
        internal async Task UpdateDateModified(Guid uid)
        {
            var item = await GetByUid(uid);
            if (item == null)
                return;
            item.DateModified = DateTime.Now;
            await DbManager.UpdateDateModified(item.Uid, item.DateModified);
        }
    }
}
