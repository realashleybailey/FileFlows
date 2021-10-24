using ViWatcher.Shared;

namespace ViWatcher.Client.Shared {
    public partial class NavMenu{
        private string lblHome, lblVideoFiles, lblSettings;

        protected override void OnInitialized()
        {
            this.lblHome = Translater.Instant("Pages.Home.Title");
            this.lblVideoFiles = Translater.Instant("Pages.VideoFiles.Title");
            this.lblSettings = Translater.Instant("Pages.Settings.Title");
        }
    }
}