namespace FileFlows.Client.Shared
{
    using Microsoft.AspNetCore.Components;
    using FileFlows.Client.Components;
    using System.Threading.Tasks;
    using Microsoft.JSInterop;

    public partial class MainLayout : LayoutComponentBase
    {
        public NavMenu Menu { get; set; }
        public Blocker Blocker { get; set; }
        public Editor Editor { get; set; }

        public bool Telemetry { get; set; }
        [Inject] IJSRuntime jsRuntime { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var telemetry = await FileFlows.Shared.Helpers.HttpHelper.Get<bool>("/api/settings/telemetry");
            this.Telemetry = telemetry.Success == false || telemetry.Data == true;
            if (this.Telemetry)
                await jsRuntime.InvokeVoidAsync("ff.enableTelemetry", new object[] { "https://static.getclicky.com/101343621.js" });
        }
    }
}