namespace ViWatcher.Client.Shared 
{
    using Microsoft.AspNetCore.Components;
    using ViWatcher.Client.Components;
    public partial class MainLayout: LayoutComponentBase
    {
        public NavMenu Menu { get; set; }
        public Blocker Blocker{ get; set; }
    }
}