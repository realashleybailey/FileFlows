namespace FileFlows.ServerShared.Services
{
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public interface IFlowService
    {
        Task<Flow> Get(Guid uid);
    }

    public class FlowService : Service, IFlowService
    {

        public static Func<IFlowService> Loader { get; set; }

        public static IFlowService Load()
        {
            if (Loader == null)
                return new FlowService();
            return Loader.Invoke();
        }

        public async Task<Flow> Get(Guid uid)
        {
            try
            {
                var result = await HttpHelper.Get<Flow>($"{ServiceBaseUrl}/api/flow/" + uid.ToString());
                if (result.Success == false)
                    throw new Exception("Failed to locate flow: " + result.Body);
                return result.Data;
            }
            catch (Exception ex)
            {
                Logger.Instance?.WLog("Failed to get flow: " + uid + " => " + ex.Message);
                return null;
            }
        }
    }
}
