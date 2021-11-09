namespace FileFlow.Client.Shared
{
    using System.Collections.Generic;
    using System.Linq;
    using FileFlow.Shared;
    using Microsoft.AspNetCore.Components;

    public partial class NavMenu
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        private List<NavMenuItem> MenuItems = new List<NavMenuItem>();
        private bool collapseNavMenu = true;

        public NavMenuItem Active { get; private set; }

        private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;
        protected override void OnInitialized()
        {
            MenuItems.Add(new NavMenuItem("Pages.Dashboard.Title", "fas fa-home", ""));
            MenuItems.Add(new NavMenuItem("Pages.LibraryFiles.Title", "fas fa-copy", "library-files"));
            MenuItems.Add(new NavMenuItem("Pages.Flows.Title", "fas fa-project-diagram", "flows"));
            MenuItems.Add(new NavMenuItem("Pages.Libraries.Title", "fas fa-folder", "libraries"));
            MenuItems.Add(new NavMenuItem("Pages.Plugins.Title", "fas fa-puzzle-piece", "plugins"));
            MenuItems.Add(new NavMenuItem("Pages.Tools.Title", "fas fa-tools", "tools"));
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