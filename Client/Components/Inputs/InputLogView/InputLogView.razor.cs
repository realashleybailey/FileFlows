namespace FileFlows.Client.Components.Inputs
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Web;
    using FileFlows.Shared.Helpers;
    using Microsoft.AspNetCore.Components;
    public partial class InputLogView : Input<string>, IDisposable
    {
        [Parameter] public string RefreshUrl { get; set; }

        [Parameter] public int RefreshSeconds { get; set; }

        private string Colorized { get; set; }

        private string PreviousValue { get; set; }

        private Timer RefreshTimer;
        private bool Refreshing = false;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            this.Colorized = Colorize(this.Value);

            if (string.IsNullOrEmpty(RefreshUrl) == false)
            {
                this.RefreshTimer = new Timer();
                this.RefreshTimer.AutoReset = true;
                this.RefreshTimer.Interval = RefreshSeconds > 0 ? RefreshSeconds * 1000 : 10_000;
                this.RefreshTimer.Elapsed += RefreshTimerElapsed;
                this.RefreshTimer.Start();
            }
        }

        public void Dispose()
        {
            if (this.RefreshTimer != null)
            {
                this.RefreshTimer.Stop();
                this.RefreshTimer.Elapsed -= RefreshTimerElapsed;
            }
        }

        private void RefreshTimerElapsed(object sender, EventArgs e)
        {
            if (Refreshing)
                return;
            Refreshing = true;
            Task.Run(async () =>
            {
                try
                {
                    var refreshResult = await HttpHelper.Get<string>(this.RefreshUrl);
                    if (refreshResult.Success == false)
                        return;
                    this.Value = refreshResult.Data;
                    this.Colorized = Colorize(this.Value); 
                    this.StateHasChanged();
                }
                catch (Exception) { }
                finally
                {
                    Refreshing = false;
                }
            });
        }

        private string Colorize(string log)
        {
            StringBuilder colorized = new StringBuilder();
            if (string.IsNullOrWhiteSpace(PreviousValue) == false && log.StartsWith(PreviousValue))
            {
                PreviousValue = log;
                // this avoid us from redoing stuff we've already done
                colorized.Append(this.Value);
                log = log.Substring(PreviousValue.Length).TrimStart();
            }
            else
            {
                PreviousValue = log;
            }


            foreach (var line in log.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    colorized.Append("<div class=\"line\">");
                    colorized.Append(ColorizeSection(line));
                    colorized.Append("</div>" + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    colorized.Append(HtmlEncode(ex.Message));
                }
            }
            string result = colorized.ToString();
            return result;
        }

        Regex regTime = new Regex(@"[\d]{2}:[\d]{2}:[\d]{2}\.[\d]+");
        Regex regKeyValue = new Regex(@"^([^\s][^:]+[^\s]):([\s].*?$)?$");
        Regex regDatedLine = new Regex(@"^([\d]{4}-[\d]{2}-[\d]{2} [\d]{2}:[\d]{2}:[\d]{2}\.[\d]{4}) - (INFO|WARN|DBUG|ERRR) -> (.*?$)");
        Regex regHttpMethods = new Regex(@"\[(GET|POST|PUT|DELETE)\]");
        Regex regUrl = new Regex(@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?");
        Regex regWindowsNetwork = new Regex(@"(\\\\[a-zA-Z0-9-]+\\[a-zA-Z0-9`~!@#$%^&(){}'._-]+([ ]+[a-zA-Z0-9`~!@#$%^&(){}'._-]+)*)(\\[^ \\/:*?""<>|]+([ ]+[^ \\/:*?""<>|]+)*)*\\?");
        Regex regWindowsFilename = new Regex(@"([a-zA-Z]:)(\\[^ \\/:*?""<>|]+([ ]+[^ \\/:*?""<>|]+)*)+\\?");

        private string ColorizeSection(string section)
        {
            if(string.IsNullOrWhiteSpace(section))
                return section ?? string.Empty;
            if (section.StartsWith("==="))
            {
                return "<span class=\"heading\">" + HtmlEncode(section) + "</span>";
            }
            else if (regDatedLine.TryMatch(section, out Match dlMatch))
            {
                string date = dlMatch.Groups[1].Value;
                string type = dlMatch.Groups[2].Value;
                string content = dlMatch.Groups[3].Value;
                return "<span class=\"date\">" + HtmlEncode(date) + "</span> - " +
                       "<span class=\"logtype logtype-" + type + "\">" + HtmlEncode(type) + "</span> <span class=\"arrow\">-></span> " + 
                       ColorizeSection(content);
            }

            if (regKeyValue.TryMatch(section, out Match kvMatch))
            {
                string key = kvMatch.Groups[1].Value;
                string value = kvMatch.Groups[2].Value;
                section = "<span class=\"key\">" + ColorizeSection(key) + ":</span>" +
                          "<span class=\"value\">" + ColorizeSection(value) + "</span>";
            }
            else
            {
                section = HtmlEncode(section);
            }

            section = regHttpMethods.Replace(section, "<span class=\"http-method\">[$1]</span>");
            section = regUrl.Replace(section, "<span class=\"url\">$0</span>");
            section = regWindowsFilename.Replace(section, "<span class=\"file\">$0</span>");
            section = regTime.Replace(section, "<span class=\"time\">$0</span>");
            
            return section;
        }

        private string HtmlEncode(string input)
        {
            input = HttpUtility.HtmlEncode(input);
            return input;

        }
    }
}