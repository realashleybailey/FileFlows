namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;

    public partial class InputWrapper : ComponentBase
    {
        [Parameter]
        public IInput Input { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        private string HelpHtml = string.Empty;
        protected override void OnInitialized()
        {
            string help = Regex.Replace(Input?.Help ?? string.Empty, "<.*?>", string.Empty);
            foreach (Match match in Regex.Matches(help, @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()!@:%_\+.~#?&\/\/=]*)", RegexOptions.Multiline))
            {
                help = help.Replace(match.Value, $"<a target=\"_blank\" href=\"{HttpUtility.HtmlAttributeEncode(match.Value)}\">{HttpUtility.HtmlEncode(match.Value)}</a>");
            }
            this.HelpHtml = help;
        }
    }
}