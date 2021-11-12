namespace FileFlow.Client.Helpers
{
    using System.Text.RegularExpressions;
    using FileFlow.Plugin;

    public class FlowHelper
    {
        public static string GetFlowPartIcon(FlowElementType type)
        {
            return "fas fa-chevron-right";
        }

        public static string FormatLabel(string name)
        {
            return Regex.Replace(name.Replace("_", " "), "(?<=[A-Za-z])(?=[A-Z][a-z])|(?<=[a-z0-9])(?=[0-9]?[A-Z])", " ");
        }
    }
}