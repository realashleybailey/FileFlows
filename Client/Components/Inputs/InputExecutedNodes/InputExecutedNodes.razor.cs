namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.JSInterop;
    using System.Threading.Tasks;

    public partial class InputExecutedNodes: Input<IEnumerable<ExecutedNode>>
    {
    }
}