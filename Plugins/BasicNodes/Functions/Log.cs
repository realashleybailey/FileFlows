namespace FileFlow.BasicNodes.Functions
{
    using System.ComponentModel;
    using FileFlow.Plugin;
    using FileFlow.Plugin.Attributes;
    using Jint.Runtime;
    using Jint.Native.Object;
    using Jint;
    using System.Text;

    public class Log : Node
    {
        public override int Inputs => 1;
        public override int Outputs => 1;
        public override FlowElementType Type => FlowElementType.Logic;

        [Enum(typeof(LogType), 1)]
        public LogType LogType { get; set; }

        [Text(2)]
        public string Message { get; set; }
        public override int Execute(NodeParameters args)
        {
            switch (LogType)
            {
                case LogType.Error: args.Logger.ELog(Message); break;
                case LogType.Warning: args.Logger.WLog(Message); break;
                case LogType.Debug: args.Logger.DLog(Message); break;
                case LogType.Info: args.Logger.ILog(Message); break;
            }

            return base.Execute(args);
        }
    }
}