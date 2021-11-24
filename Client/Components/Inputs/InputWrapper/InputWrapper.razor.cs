namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using System.Threading.Tasks;

    public partial class InputWrapper : ComponentBase
    {
        [Parameter]
        public IInput Input { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }        
    }
}