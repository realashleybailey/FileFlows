namespace FileFlow.Client.Components.Inputs
{
    using System.Collections.Generic;
    using Models;
    using Microsoft.AspNetCore.Components;
    public partial class InputSelect : Input<object>
    {
        [Parameter]
        public IEnumerable<ListOption> Options { get; set; }
    }
}