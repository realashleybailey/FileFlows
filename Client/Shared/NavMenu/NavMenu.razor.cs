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
        protected override void OnInitialized()
        {
            lblVersion = Translater.Instant("Labels.VersionNumber", new { version = Globals.Version });
            lblHelp = Translater.Instant("Labels.Help");

            MenuItems.Add(new NavMenuGroup
            {
                Name = "Overview",
                Icon = "fas fa-info-circle",
                Items = new List<NavMenuItem>
                {
                    new NavMenuItem("Pages.Dashboard.Title", "fas fa-chart-pie", ""),
                    new NavMenuItem("Pages.LibraryFiles.Title", "fas fa-copy", "library-files")
                }
            });

            MenuItems.Add(new NavMenuGroup
            {
                Name = "Configuration",
                Icon = "fas fa-code-branch",
                Items = new List<NavMenuItem>
                {
                    new NavMenuItem("Pages.Flows.Title", "fas fa-sitemap", "flows"),
                    new NavMenuItem("Pages.Libraries.Title", "fas fa-folder", "libraries"),
#if (!DEMO)
                    new NavMenuItem("Pages.Nodes.Title", "fas fa-desktop", "nodes")
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
                    new NavMenuItem("Pages.Plugins.Title", "fas fa-puzzle-piece", "plugins"),
                    new NavMenuItem("Pages.Tools.Title", "fas fa-tools", "tools"),
                    new NavMenuItem("Pages.Settings.Title", "fas fa-cogs", "settings")
                }
            });


            MenuItems.Add(new NavMenuGroup
            {
                Name = "Help",
                Icon = "fas fa-question-circle",
                Items = new List<NavMenuItem>
                {
                    new NavMenuItem("Pages.Log.Title", "fas fa-file-alt", "log")
                }
            });

#endif

            string currentRoute = NavigationManager.Uri.Substring(NavigationManager.BaseUri.Length);
            Active = MenuItems.SelectMany(x => x.Items).Where(x => x.Url == currentRoute).FirstOrDefault() ?? MenuItems[0].Items.First();
        }

        private void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }

        async Task Click(NavMenuItem item)
        {
            bool ok = await NavigationService.NavigateTo(item.Url);
            if (ok)
                SetActive(item);
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