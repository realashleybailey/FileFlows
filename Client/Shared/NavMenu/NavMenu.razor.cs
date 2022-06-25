namespace FileFlows.Client.Shared
{
    using System.Collections.Generic;
    using System.Linq;
    using FileFlows.Shared;
    using Microsoft.AspNetCore.Components;

    public partial class NavMenu
    {
        [Inject] private INavigationService NavigationService { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        private List<NavMenuGroup> MenuItems = new List<NavMenuGroup>();
        private bool collapseNavMenu = true;

        public NavMenuItem Active { get; private set; }

        private string lblVersion, lblHelp;

        private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;
        private NavMenuItem nmiFlows, nmiLibraries;

        protected override void OnInitialized()
        {
            lblVersion = Translater.Instant("Labels.VersionNumber", new { version = Globals.Version });
            lblHelp = Translater.Instant("Labels.Help");

            App.Instance.OnFileFlowsSystemUpdated += FileFlowsSystemUpdated;
            
            this.LoadMenu();
        }
        
        void LoadMenu()
        {
            this.MenuItems.Clear();

            MenuItems.Add(new NavMenuGroup
            {
                Name = "Overview",
                Icon = "fas fa-info-circle",
                Items = new List<NavMenuItem>
                {
                    new ("Pages.Dashboard.Title", "fas fa-chart-pie", ""),
                    new ("Pages.LibraryFiles.Title", "fas fa-copy", "library-files")
                }
            });

            nmiFlows = new("Pages.Flows.Title", "fas fa-sitemap", "flows");
            nmiLibraries = new("Pages.Libraries.Title", "fas fa-folder", "libraries");

            MenuItems.Add(new NavMenuGroup
            {
                Name = "Configuration",
                Icon = "fas fa-code-branch",
                Items = new List<NavMenuItem>
                {
                    nmiFlows,
                    nmiLibraries,
#if (!DEMO)
                    new ("Pages.Nodes.Title", "fas fa-desktop", "nodes")
#endif
                }
            });

#if (!DEMO)
            MenuItems.Add(new NavMenuGroup
            {
                Name = "System",
                Icon = "fas fa-hdd",
                Items = new List<NavMenuItem>
                {
                    new ("Pages.Scripts.Title", "fas fa-scroll", "scripts"),
                    new ("Pages.Plugins.Title", "fas fa-puzzle-piece", "plugins"),
                    new ("Pages.Tools.Title", "fas fa-tools", "tools"),
                    new ("Pages.Settings.Title", "fas fa-cogs", "settings"),
                }
            });

            MenuItems.Add(new NavMenuGroup
            {
                Name = "Information",
                Icon = "fas fa-question-circle",
                Items = new List<NavMenuItem>
                {
                    new NavMenuItem("Pages.Log.Title", "fas fa-file-alt", "log"),
                    App.Instance.FileFlowsSystem.Licensed ? new NavMenuItem("Pages.System.Title", "fas fa-microchip", "system") : null
                }
            });

#endif

            string currentRoute = NavigationManager.Uri.Substring(NavigationManager.BaseUri.Length);
            Active = MenuItems.SelectMany(x => x.Items).Where(x => x.Url == currentRoute).FirstOrDefault() ?? MenuItems[0].Items.First();
        }

        private void FileFlowsSystemUpdated(FileFlowsStatus system)
        {
            this.LoadMenu();
            this.StateHasChanged();
        }

        private string? GetStepLabel(NavMenuItem nmi)
        {
            if (nmi == Active)
                return null;
            if ((App.Instance.FileFlowsSystem.ConfigurationStatus & ConfigurationStatus.Flows) !=
                ConfigurationStatus.Flows)
            {
                return nmi == nmiFlows ? "Step 1" : null;
            }

            if ((App.Instance.FileFlowsSystem.ConfigurationStatus & ConfigurationStatus.Libraries) !=
                ConfigurationStatus.Libraries)
            {
                return nmi == nmiLibraries ? "Step 2" : null; 
            }

            return null;
        }
        

        private void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }

        async Task Click(NavMenuItem item)
        {
            bool ok = await NavigationService.NavigateTo(item.Url);
            if (ok)
            {
                SetActive(item);
                collapseNavMenu = true;
                this.StateHasChanged();
            }
        }

        private void SetActive(NavMenuItem item)
        {
            Active = item;
            this.StateHasChanged();
        }
    }

    public class NavMenuGroup
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public List<NavMenuItem> Items { get; set; } = new List<NavMenuItem>();
    }

    public class NavMenuItem
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }

        public NavMenuItem(string title = "", string icon = "", string url = "")
        {
            this.Title = Translater.TranslateIfNeeded(title);
            this.Icon = icon;
            this.Url = url;
        }
    }
}