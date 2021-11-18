namespace FileFlows.Client.Shared
{
    using Microsoft.AspNetCore.Components;
    using FileFlows.Client.Components;
    public partial class MainLayout : LayoutComponentBase
    {
        public NavMenu Menu { get; set; }
        public Blocker Blocker { get; set; }

        public Editor Editor { get; set; }
    }
}