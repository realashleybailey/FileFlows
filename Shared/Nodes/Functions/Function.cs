namespace ViWatcher.Shared.Nodes.Functions
{
    using System.ComponentModel;
    using ViWatcher.Shared.Attributes;
    
    public class Function : Node,  IConfigurableInputNode, IConfigurableOutputNode
    {
        [DefaultValue(1)]
        [NumberIntAttribute(1)]
        public int Inputs{ get; set; }
        [DefaultValue(1)]
        [NumberIntAttribute(2)]
        public int Outputs{ get; set; }
        
        [DefaultValue("// VideoFile object contains info about the video file\n\n// return true to continue processing this flow\n// return false to stop it\nreturn true;")]
        [Code(3)]
        public string Code{ get; set; }
    }
}