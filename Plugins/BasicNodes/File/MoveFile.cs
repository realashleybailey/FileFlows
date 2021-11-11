namespace FileFlow.BasicNodes.File
{
    using System.ComponentModel;
    using System.Threading.Tasks;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;

    public class MoveFile : Node
    {
        public override int Inputs => 1;
        public override int Outputs => 1;
        public override FlowElementType Type => FlowElementType.Process;

        [Folder(1)]
        public string DestinationPath { get; set; }

        [Boolean(2)]
        public bool MoveFolder { get; set; }

        public override int Execute(NodeParameters args)
        {
            string dest = DestinationPath;
            if (string.IsNullOrEmpty(dest))
            {
                args.Logger.ELog("No destination specified");
                args.Result = NodeResult.Failure;
                return -1;
            }
            args.Result = NodeResult.Failure;

            if (MoveFolder)
                dest = Path.Combine(dest, args.RelativeFile);
            else
                dest = Path.Combine(dest, new FileInfo(args.FileName).Name);

            var destDir = new FileInfo(dest).DirectoryName;
            if (Directory.Exists(destDir) == false)
                Directory.CreateDirectory(destDir);

            long fileSize = new FileInfo(args.WorkingFile).Length;

            Task task = Task.Run(() =>
            {
                System.IO.File.Move(args.WorkingFile, dest, true);
            });

            while (task.IsCompleted == false)
            {
                long currentSize = new FileInfo(dest).Length;
                args.PartPercentageUpdate(currentSize / fileSize * 100);
                System.Threading.Thread.Sleep(50);
            }
            args.PartPercentageUpdate(100);

            return 0;
        }
    }
}