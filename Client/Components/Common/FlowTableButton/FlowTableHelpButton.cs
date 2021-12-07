using FileFlows.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace FileFlows.Client.Components.Common
{
    public class FlowTableHelpButton<TItem> : FlowTableButton<TItem>
    {
        [Inject] IJSRuntime jsRuntime { get; set; }

        [Parameter]
        public string HelpUrl { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            this._Icon = "fas fa-question-circle";
            this._Label = Translater.Instant("Labels.Help");
        }

        protected override async Task OnClick()
        {
            string url = this.HelpUrl;
            if (string.IsNullOrEmpty(HelpUrl))
                url = "https://github.com/revenz/FileFlows/wiki";
            else if (url.ToLower().StartsWith("http") == false)
                url = "https://github.com/revenz/FileFlows/wiki/" + url;
            await jsRuntime.InvokeVoidAsync("open", url, "_blank");
        }
    }
}
