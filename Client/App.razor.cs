namespace FileFlows.Client
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using FileFlows.Client.Helpers;
    using FileFlows.Shared;

    public partial class App : ComponentBase
    {
        public static App Instance { get; private set; }

        [Inject]
        public HttpClient Client { get; set; }
        [Inject]
        public IJSRuntime jsRuntime { get; set; }
        public bool LanguageLoaded { get; set; } = false;

        public int DisplayWidth { get; private set; }
        public int DisplayHeight { get; private set; }

        public bool IsMobile => DisplayWidth > 0 && DisplayWidth <= 768;

        private async Task LoadLanguage()
        {
            string langFile = await LoadLanguageFile("i18n/en.json");
            string pluginLang = await LoadLanguageFile("/api/plugin/language/en.json");
            Translater.Init(langFile, pluginLang);
        }

        private async Task<string> LoadLanguageFile(string url)
        {
            return (await HttpHelper.Get<string>(url)).Data ?? "";
        }

        protected override async Task OnInitializedAsync()
        {
            Instance = this;
            Logger.jsRuntime = jsRuntime;
            Translater.Logger = Logger.Instance;
            FileFlows.Shared.Logger.Instance = Logger.Instance;
            HttpHelper.Client = Client;
            var dimensions = await jsRuntime.InvokeAsync<Dimensions>("ff.deviceDimensions");
            DisplayWidth = dimensions.width;
            DisplayHeight = dimensions.height;
            await Task.Run(async () =>
            {
                await LoadLanguage();
                LanguageLoaded = true;
                this.StateHasChanged();
            });
        }

        record Dimensions(int width, int height);
    }
}