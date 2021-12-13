namespace FileFlows.Server.Services
{
    using FileFlows.Server.Controllers;
    using FileFlows.ServerShared.Services;
    using FileFlows.Shared.Models;
    using System;
    using System.Threading.Tasks;

    public class SettingsService : ISettingsService
    {
        public Task<Settings> Get() => new SettingsController().Get();
    }
}
