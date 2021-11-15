namespace FileFlow.BasicNodes.File
{
    using System.ComponentModel;
    using System.Threading.Tasks;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;

    public class DeleteSourceDirectory : Node
    {
        public override int Inputs => 1;
        public override int Outputs => 1;
        public override FlowElementType Type => FlowElementType.Process;
        public override string Icon => "far fa-trash-alt";

        public override int Execute(NodeParameters args)
        {
            string path = args.FileName.Substring(0, args.FileName.Length - args.RelativeFile.Length);
            args.Logger.ILog("Library path: " + path);
            int pathIndex = args.RelativeFile.IndexOf(System.IO.Path.DirectorySeparatorChar);
            if (pathIndex < 0)
            {
                args.Logger.ILog("File is in library root, will not delete");
                return base.Execute(args);
            }

            string topdir = args.RelativeFile.Substring(0, pathIndex);
            string pathToDelete = Path.Combine(path, topdir);
            args.Logger.ILog("Deleting directory: " + pathToDelete);
            try
            {
                System.IO.Directory.Delete(pathToDelete, true);
            }
            catch (Exception ex)
            {
                args.Logger.ELog("Failed to delete directory: " + ex.Message);
                return -1;
            }
            return base.Execute(args);
        }
    }
}