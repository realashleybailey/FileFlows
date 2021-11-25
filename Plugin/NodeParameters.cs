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
        public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

        public Func<string, string>? GetToolPath { get; set; }

        public string TempPath { get; set; }

        public Action<float>? PartPercentageUpdate { get; set; }

        public NodeParameters(string filename)
        {
            this.FileName = filename;
            this.WorkingFile = filename;
            this.RelativeFile = string.Empty;
            this.TempPath = string.Empty;
            InitFile(filename);
        }

        private void InitFile(string filename)
        {
            var fi = new FileInfo(filename);
            var fiOriginal = new FileInfo(FileName);
            UpdateVariables(new Dictionary<string, object> {
                { "ext", fi.Extension ?? "" },
                { "fileName", Path.GetFileNameWithoutExtension(fi.Name ?? "") },
                { "fileSize", fi.Exists ? fi.Length : 0 },
                { "fileOrigExt", fiOriginal.Extension ?? "" },
                { "fileOrigFileName", Path.GetFileNameWithoutExtension(fiOriginal.Name ?? "") },
                { "folderName", fi.Directory?.Name ?? "" },
                { "folderFullName", fi.DirectoryName ?? "" },
                { "folderOrigName", fiOriginal.Directory?.Name ?? "" },
                { "folderOrigFullName", fiOriginal.DirectoryName ?? "" },
            });

        }

        public void SetWorkingFile(string filename, bool dontDelete = false)
        {
            if (this.WorkingFile == filename)
                return;
            if (this.WorkingFile != this.FileName)
            {
                string fileToDelete = this.WorkingFile;
                if (dontDelete == false)
                {
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
            }
            this.WorkingFile = filename;
            InitFile(filename);
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

        public void SetParameter(string name, object value)
        {
            if (Parameters.ContainsKey(name) == false)
                Parameters[name] = value;
            else
                Parameters.Add(name, value);
        }

        public bool MoveFile(string destination)
        {
            bool moved = false;
            long fileSize = new FileInfo(WorkingFile).Length;
            Task task = Task.Run(() =>
            {
                try
                {
                    var fileInfo = new FileInfo(destination);
                    if (fileInfo.Exists)
                        fileInfo.Delete();
                    else if (fileInfo.Directory.Exists == false)
                        fileInfo.Directory.Create();
                    Logger?.ILog($"Moving file: \"{WorkingFile}\" to \"{destination}\"");
                    System.IO.File.Move(WorkingFile, destination, true);

                    SetWorkingFile(destination);

                    moved = true;
                }
                catch (Exception ex)
                {
                    Logger?.ELog("Failed to move file: " + ex.Message);
                }
            });

            while (task.IsCompleted == false)
            {
                long currentSize = 0;
                var destFileInfo = new FileInfo(destination);
                if (destFileInfo.Exists)
                    currentSize = destFileInfo.Length;

                if (PartPercentageUpdate != null)
                    PartPercentageUpdate(currentSize / fileSize * 100);
                System.Threading.Thread.Sleep(50);
            }

            if (moved == false)
                return false;

            if (PartPercentageUpdate != null)
                PartPercentageUpdate(100);
            return true;
        }

        public void UpdateVariables(Dictionary<string, object> updates)
        {
            if (updates == null)
                return;
            foreach (var key in updates.Keys)
            {
                if (Variables.ContainsKey(key))
                    Variables[key] = updates[key];
                else
                    Variables.Add(key, updates[key]);
            }
        }

        /// <summary>
        /// Replaces variables in a given string
        /// </summary>
        /// <param name="input">the input string</param>
        /// <param name="variables">the variables used to replace</param>
        /// <param name="stripMissing">if missing variables shouild be removed</param>
        /// <returns>the string with the variables replaced</returns>
        public string ReplaceVariables(string input, bool stripMissing = false) => VariablesHelper.ReplaceVariables(input, Variables, stripMissing);


        /// <summary>
        /// Gets a safe filename with any reserved characters removed or replaced
        /// </summary>
        /// <param name="fullFileName">the full filename of the file to make safe</param>
        /// <returns>the safe filename</returns>
        public FileInfo GetSafeName(string fullFileName)
        {
            var dest = new FileInfo(fullFileName);

            string destName = dest.Name;
            string destDir = dest?.DirectoryName ?? "";

            // replace these here to avoid double spaces in name
            if (Path.GetInvalidFileNameChars().Contains(':'))
            {
                destName = destName.Replace(" : ", " - ");
                destName = destName.Replace(": ", " - ");
                destDir = destDir.Replace(" : ", " - ");
                destDir = destDir.Replace(": ", " - ");
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (c == ':')
                {
                    destName = destName.Replace(c.ToString(), " - ");
                    if(c != Path.DirectorySeparatorChar && c != Path.PathSeparator)
                        destDir = destDir.Replace(c.ToString(), " - ");
                }
                else
                {
                    destName = destName.Replace(c.ToString(), "");
                    if (c != Path.DirectorySeparatorChar && c != Path.PathSeparator)
                        destDir = destDir.Replace(c.ToString(), "");
                }
            }
            // put the drive letter back if it was replaced iwth a ' - '
            destDir = System.Text.RegularExpressions.Regex.Replace(destDir, @"^([a-z]) \- ", "$1:", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return new FileInfo(Path.Combine(destDir, destName));
        }
    }


    public enum NodeResult
    {
        Failure = 0,
        Success = 1,
    }
}