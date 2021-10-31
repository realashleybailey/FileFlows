namespace FileFlow.Plugin
{

    using FileFlow.Plugin;
    public class Node
    {
        public virtual FlowElementType Type { get; }

        public virtual int Inputs { get; }
        public virtual int Outputs { get; }

        public string Name => base.GetType().FullName.Substring("FileFlow.Shared.Nodes.".Length);

        public string Group
        {
            get
            {
                var type = base.GetType();
                string group = type.FullName.Substring(0, type.FullName.LastIndexOf("."));
                return group.Substring(group.LastIndexOf(".") + 1);
            }
        }

        /// <summary>
        /// Executes the node
        /// </summary>
        /// <param name="args">the arguments passed into the node</param>
        /// <returns>the number of the output node to call next, this is 1 based</returns>
        public virtual int Execute(NodeParameters args)
        {
            if (Outputs > 0)
                return 1;
            return -1;
        }
    }
}