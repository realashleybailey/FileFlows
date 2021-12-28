using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.FlowRunner
{
    public interface IFlowRunnerCommunicator
    {
        Task LogMessage(string message);
    }

    public class FlowRunnerCommunicator : IFlowRunnerCommunicator
    {
        public static string SignalrUrl { get; set; }
        HubConnection connection;
        private Guid LibraryFileUid;
        public delegate void Cancel();
        public event Cancel OnCancel;


        public FlowRunnerCommunicator(Guid libraryFileUid)
        {
            this.LibraryFileUid = libraryFileUid;
            connection = new HubConnectionBuilder()
                                .WithUrl(new Uri(SignalrUrl))
                                .WithAutomaticReconnect()
                                .Build();
            connection.Closed += Connection_Closed;
            connection.On<Guid>("AbortFlow", (uid) =>
            {
                if (uid != LibraryFileUid)
                    return;
                OnCancel?.Invoke();
            });
            connection.StartAsync().Wait();
            if (connection.State == HubConnectionState.Disconnected)
                throw new Exception("Failed to connect to signlaR");
        }

        public void Close()
        {
            try
            {
                connection?.DisposeAsync();
            }
            catch (Exception) { } // not sure if this can throw, but just in case
        }

        private Task Connection_Closed(Exception? arg)
        {
            Console.WriteLine("Connection_Closed");
            return Task.CompletedTask;
        }

        private void Connection_Received(string obj)
        {
            Console.WriteLine("Connection_Received", obj);
        }

        public async Task LogMessage(string message)
        {
            try
            {
                await connection.InvokeAsync("LogMessage", LibraryFileUid, message);
            } 
            catch (Exception)
            {
                // siliently fail here, we store the log in memory if one message fails its not a biggie
                // once the flow is complete we send the entire log to the server to update
            }
        }

        public static FlowRunnerCommunicator Load(Guid libraryFileUid)
        {
            return new FlowRunnerCommunicator(libraryFileUid);

        }
    }
}
