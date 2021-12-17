namespace FileFlows.ServerShared.Services
{
    using FileFlows.ServerShared.Models;

    public class Service
    {
        private static string _ServiceBaseUrl;
        public static string ServiceBaseUrl 
        { 
            get => _ServiceBaseUrl;
            set
            {
                if(value == null)
                {
                    _ServiceBaseUrl = string.Empty;
                    return;
                }
                if(value.EndsWith("/"))
                    _ServiceBaseUrl = value.Substring(0, value.Length - 1); 
                else
                    _ServiceBaseUrl = value;
            }
        }

    }
}
