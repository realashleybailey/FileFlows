using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;

namespace FileFlows.Client.Services
{
    public interface IClipboardService
    {
        Task CopyToClipboard(string text);
    }

    public class ClipboardService : IClipboardService
    {
        private IJSRuntime JsRuntime { get; set; }

        public ClipboardService(IJSRuntime jsRuntime)
        {
            this.JsRuntime = jsRuntime;
        }

        public async Task CopyToClipboard(string text)
        {
            await JsRuntime.InvokeVoidAsync("ff.copyToClipboard", text);
            Toast.ShowInfo(Translater.Instant("Labels.CopiedToClipboard", new { text }));
        }
    }
}
