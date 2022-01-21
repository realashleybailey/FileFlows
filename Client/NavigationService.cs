using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;

namespace FileFlows.Client
{
    public interface INavigationService
    {
        Task<bool> NavigateTo(string url);

        void RegisterNavigationCallback(Func<Task<bool>> callback);
        void UnRegisterNavigationCallback(Func<Task<bool>> callback);
    }

    public class NavigationService : INavigationService
    {
        public static NavigationManager NavigationManager { get; set; }

        private readonly List<Func<Task<bool>>> _Callbacks = new List<Func<Task<bool>>>();


        public async Task<bool> NavigateTo(string url)
        {
            foreach(var callback in _Callbacks)
            {
                bool ok = await callback.Invoke();
                if (ok == false)
                    return false;
            }
            NavigationManager.NavigateTo(url);
            return true;
        }

        public void RegisterNavigationCallback(Func<Task<bool>> callback)
        {
            if(_Callbacks.Contains(callback) == false)
                _Callbacks.Add(callback);
        }

        public void UnRegisterNavigationCallback(Func<Task<bool>> callback)
        {
            if(_Callbacks.Contains(callback))
                _Callbacks.Remove(callback);
        }
    }
}
