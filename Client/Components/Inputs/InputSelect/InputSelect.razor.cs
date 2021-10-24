namespace ViWatcher.Client.Components.Inputs 
{
    using System.Collections.Generic;
    using Models;
    using Microsoft.AspNetCore.Components;
    public partial class InputSelect<T> : Input<T>
    {
        [Parameter]
        public IEnumerable<ListOption> Options{ get; set; }
    }
}