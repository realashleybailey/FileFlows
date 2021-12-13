namespace FileFlows.ServerShared.Services
{
    using FileFlows.ServerShared.Models;

    public class Service
    {
        protected static string ServiceBaseUrl;
        private static bool InitDone = false;

        public Service()
        {
            if (InitDone == false)
                Init();
        }

        public static void Init()
        {
            Console.WriteLine("Service.Init");
            if(File.Exists("node.config") == false)
                throw new Exception("node.config not found");

            string json = File.ReadAllText("node.config");
            NodeConfig cfg = System.Text.Json.JsonSerializer.Deserialize<NodeConfig>(json) ?? new NodeConfig();
            if (string.IsNullOrEmpty(cfg.ServerUrl))
                throw new Exception("No ServerUrl configured in node.config");
            ServiceBaseUrl = cfg.ServerUrl;
            if (ServiceBaseUrl.EndsWith("/") == false)
                ServiceBaseUrl += "/";
            ServiceBaseUrl += "api";
            Console.WriteLine("ServiceBaseUrl: " + ServiceBaseUrl);
            InitDone = true;
        }
    }
}
