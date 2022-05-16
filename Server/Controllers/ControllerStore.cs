namespace FileFlows.Server.Controllers
{
    using FileFlows.Server.Helpers;
    using FileFlows.Shared.Models;
    using Microsoft.AspNetCore.Mvc;

    public abstract class ControllerStore<T>:Controller where T : FileFlowObject, new()
    {
        protected static Dictionary<Guid, T> Data;
        protected static Mutex _mutex = new Mutex(false);

        protected async Task<IEnumerable<string>> GetNames(Guid? uid = null)
        {
            var data = await GetData();
            if(uid == null)
                return data.Select(x => x.Value.Name);
            return data.Where(x => x.Key != uid.Value).Select(x => x.Value.Name);
        }


        /// <summary>
        /// Checks to see if a name is in use
        /// </summary>
        /// <param name="uid">the Uid of the item</param>
        /// <param name="name">the name of the item</param>
        /// <returns>true if name is in use</returns>
        protected async Task<bool> NameInUse(Guid uid, string name)
        {
            name = name.ToLower().Trim();
            return (await GetData()).Any(x => uid != x.Key && x.Value.Name.ToLower() == name);
        }

        protected async Task<string> GetNewUniqueName(string name)
        {
            string newName = name.Trim();
            var names = (await GetData()).Select(x => x.Value.Name.ToLower()).ToList();
            int count = 2;
            while (names.Contains(newName.ToLower()))
            {
                newName = name + " (" + count + ")";
                ++count;
                if (count > 100)
                    throw new Exception("Could not find unique name, aborting.");
            }
            return newName;
        }

        /// <summary>
        /// Clears the cached data 
        /// </summary>
        public void ClearData() => Data = null;

        /// <summary>
        /// Gets all the data indexed by the items UID
        /// </summary>
        /// <returns>all the data indexed by the items UID</returns>
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

        internal virtual async Task<List<T>> GetDataList() => (await GetData()).Values.ToList();

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
            DbManager.UpdateDateModified(item.Uid, item.DateModified);
        }
    }
}
