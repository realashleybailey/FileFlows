namespace FileFlows.Client
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using FileFlows.Shared;
    using FileFlows.Shared.Helpers;

    public partial class App : ComponentBase
    {
        public static App Instance { get; private set; }

        [Inject] public HttpClient Client { get; set; }
        [Inject] public IJSRuntime jsRuntime { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        public bool LanguageLoaded { get; set; } = false;

        public int DisplayWidth { get; private set; }
        public int DisplayHeight { get; private set; }

        public bool IsMobile => DisplayWidth > 0 && DisplayWidth <= 768;

        public FileFlows.Shared.Models.Flow NewFlowTemplate { get; set; }

        public static FileFlows.Shared.Models.Settings Settings;

        public FileFlowsStatus FileFlowsSystem { get; private set; }

        public delegate void FileFlowsSystemUpdated(FileFlowsStatus system);

        public event FileFlowsSystemUpdated OnFileFlowsSystemUpdated;
        

        public async Task LoadLanguage()
        {
            string langFile = await LoadLanguageFile("i18n/en.json?version=" + Globals.Version);
#if (DEMO)
            string pluginLang = await LoadLanguageFile("i18n/en.plugins.json?ts=" + System.DateTime.Now.ToFileTime());
#else
            string pluginLang = await LoadLanguageFile("/api/plugin/language/en.json?ts=" + System.DateTime.Now.ToFileTime());
#endif
            Translater.Init(langFile, pluginLang);
        }

        public async Task LoadAppInfo()
        {
            FileFlowsSystem = (await HttpHelper.Get<FileFlowsStatus>("/api/settings/fileflows-status")).Data;
            this.StateHasChanged();
            this.OnFileFlowsSystemUpdated?.Invoke(FileFlowsSystem);
        }

        private async Task<string> LoadLanguageFile(string url)
        {
            return (await HttpHelper.Get<string>(url)).Data ?? "";
        }

        protected override async Task OnInitializedAsync()
        {
            Instance = this;
            ClientConsoleLogger.jsRuntime = jsRuntime;
            HttpHelper.Client = Client;
            var dimensions = await jsRuntime.InvokeAsync<Dimensions>("ff.deviceDimensions");
            DisplayWidth = dimensions.width;
            DisplayHeight = dimensions.height;

#if (DEMO)
            Settings = new FileFlows.Shared.Models.Settings
            {
                
            };
#else
            Settings = (await HttpHelper.Get<FileFlows.Shared.Models.Settings>("/api/settings")).Data ?? new FileFlows.Shared.Models.Settings();
#endif
            await LoadAppInfo();
            await LoadLanguage();
            LanguageLoaded = true;
            this.StateHasChanged();
        }

        record Dimensions(int width, int height);
    }
}