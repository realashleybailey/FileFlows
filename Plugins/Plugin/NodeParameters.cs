namespace FileFlows.Plugin
{
    using System.Collections.Generic;
    public class NodeParameters
    {
        /// <summary>
        /// The original filename of the file
        /// </summary>
        public string FileName { get; init; }
        /// <summary>
        /// Gets or sets the file relative to the library path
        /// </summary>
        public string RelativeFile { get; set; }

        /// <summary>
        /// The current working file as it is being processed in the flow, 
        /// this is what a node should save any changes too, and if the node needs 
        /// to change the file path this should be updated too
        /// </summary>
        public string WorkingFile { get; private set; }

        public ILogger? Logger { get; set; }

        public NodeResult Result { get; set; } = NodeResult.Success;

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public Func<string, string>? GetToolPath { get; set; }

        public string TempPath { get; set; }

        public Action<float>? PartPercentageUpdate { get; set; }

        public NodeParameters(string filename)
        {
            this.FileName = filename;
            this.WorkingFile = filename;
            this.RelativeFile = string.Empty;
            this.TempPath = string.Empty;
        }

        public void SetWorkingFile(string filename)
        {
            if (this.WorkingFile == filename)
                return;
            if (this.WorkingFile != this.FileName)
            {
                string fileToDelete = this.WorkingFile;
                // delete the old working file
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2_000); // wait 2 seconds for the file to be released if used
                    try
                    {
                        System.IO.File.Delete(fileToDelete);
                    }
                    catch (Exception ex)
                    {
                        Logger?.WLog("Failed to delete temporary file: " + ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                });
            }
            this.WorkingFile = filename;
        }

        public T GetParameter<T>(string name)
        {
            if (Parameters.ContainsKey(name) == false)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)string.Empty;
                return default(T)!;
            }
            return (T)Parameters[name];
        }
    }


    public enum NodeResult
    {
        Failure = 0,
        Success = 1,
    }
}