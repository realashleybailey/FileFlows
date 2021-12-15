namespace FileFlows.ServerShared.Services
{
    using FileFlows.Shared.Helpers;
    using FileFlows.Shared.Models;

    public interface IFlowRunnerService
    {
        Task<FlowExecutorInfo> Start(FlowExecutorInfo info);
        Task Complete(FlowExecutorInfo info);
        Task Update(FlowExecutorInfo info);
    }

    public class FlowRunnerService : Service, IFlowRunnerService
    {

        public static Func<IFlowRunnerService> Loader { get; set; }

        public static IFlowRunnerService Load()
        {
            if (Loader == null)
                return new FlowRunnerService();
            return Loader.Invoke();
        }

        public async Task Complete(FlowExecutorInfo info)
        {
            try
            {
                var result = await HttpHelper.Post($"{ServiceBaseUrl}/worker/work/finish", info);
                if (result.Success == false)
                    throw new Exception("Failed to finish work: " + result.Body);
            }
            catch (Exception ex)
            {
                Logger.Instance?.WLog("Failed to finish work: " + ex.Message);
            }
        }

        public async Task<FlowExecutorInfo> Start(FlowExecutorInfo info)
        {
            try
            {
                var result = await HttpHelper.Post<FlowExecutorInfo>($"{ServiceBaseUrl}/worker/work/start", info);
                if (result.Success == false)
                    throw new Exception("Failed to start work: " + result.Body);
                return result.Data;
            }
            catch (Exception ex)
            {
                Logger.Instance?.WLog("Failed to start work: " + ex.Message);
                return null;
            }
        }

        public async Task Update(FlowExecutorInfo info)
        {
            try
            {
                var result = await HttpHelper.Post($"{ServiceBaseUrl}/worker/work/update", info);
                if (result.Success == false)
                    throw new Exception("Failed to update work: " + result.Body);
            }
            catch (Exception ex)
            {
                Logger.Instance?.WLog("Failed to update work: " + ex.Message);
            }
        }
    }
}
