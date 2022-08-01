using Microsoft.Extensions.Logging;

namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;

    public partial class InputWrapper : ComponentBase
    {
        [Parameter] public IInput Input { get; set; }

        [Parameter] public bool NoSpacing { get; set;}

        [Parameter] public RenderFragment ChildContent { get; set; }

        private string HelpHtml = string.Empty;
        private string CurrentHelpText;
        protected override void OnInitialized()
        {
            InitHelpText();
        }

        protected override void OnParametersSet()
        {
            if (CurrentHelpText != Input?.Help)
            {
                InitHelpText();
                this.StateHasChanged();
            }
        }

        private void InitHelpText()
        {
            CurrentHelpText = Input?.Help;
            string help = Regex.Replace(Input?.Help ?? string.Empty, "<.*?>", string.Empty);
            foreach (Match match in Regex.Matches(help, @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()!@:%_\+.~#?&\/\/=]*)", RegexOptions.Multiline))
            {
                help = help.Replace(match.Value, $"<a rel=\"noreferrer\" target=\"_blank\" href=\"{HttpUtility.HtmlAttributeEncode(match.Value)}\">{HttpUtility.HtmlEncode(match.Value)}</a>");
            }
            this.HelpHtml = help;
        }
    }
}