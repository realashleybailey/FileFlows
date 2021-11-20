namespace FileFlows.Plugin
{

    using FileFlows.Plugin;
    public class Node
    {
        public virtual FlowElementType Type { get; }

        public virtual int Inputs { get; }
        public virtual int Outputs { get; }

        public string Name => base.GetType().Name;

        /// <summary>
        /// Gets the fontawesome icon to use in the flow 
        /// </summary>
        public virtual string Icon => string.Empty;
        public string Group
        {
            get
            {
                var type = base.GetType();
                if (type == null || type.FullName == null) return string.Empty;
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
            return 0;
        }

        /// <summary>
        /// Cancels the node
        /// </summary>
        /// <returns>cancels the node</returns>
        public virtual Task Cancel() => Task.CompletedTask;
    }
}