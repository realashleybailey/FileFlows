namespace FileFlow.Client.Shared
{
    using System.Collections.Generic;
    using System.Linq;
    using FileFlow.Shared;
    using Microsoft.AspNetCore.Components;

    public partial class NavMenu
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        private string lblHome, lblVideoFiles, lblSettings;
        private List<NavMenuItem> MenuItems = new List<NavMenuItem>();
        private bool collapseNavMenu = true;

        public NavMenuItem Active { get; private set; }

        private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;
        protected override void OnInitialized()
        {
            this.lblHome = Translater.Instant("Pages.Home.Title");
            this.lblVideoFiles = Translater.Instant("Pages.VideoFiles.Title");
            this.lblSettings = Translater.Instant("Pages.Settings.Title");

            MenuItems.Add(new NavMenuItem("Pages.Home.Title", "fas fa-home", ""));
            MenuItems.Add(new NavMenuItem("Pages.VideoFiles.Title", "fas fa-video", "video-files"));
            MenuItems.Add(new NavMenuItem("Pages.Flows.Title", "fas fa-project-diagram", "flows"));
            MenuItems.Add(new NavMenuItem("Pages.Libraries.Title", "fas fa-folder", "libraries"));
            MenuItems.Add(new NavMenuItem("Pages.Plugins.Title", "fas fa-puzzle-piece", "plugins"));
            MenuItems.Add(new NavMenuItem("Pages.Settings.Title", "fas fa-cogs", "settings"));

            string currentRoute = NavigationManager.Uri.Substring(NavigationManager.BaseUri.Length);
            Active = MenuItems.Where(x => x.Url == currentRoute).FirstOrDefault() ?? MenuItems[0];
        }

        private void ToggleNavMenu()
        {
            collapseNavMenu = !collapseNavMenu;
        }

        private void SetActive(NavMenuItem item)
        {
            Active = item;
            this.StateHasChanged();
        }
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