public class Server : Component
{
    public override Type[] Dependencies => new [] { typeof(Client), typeof(FlowRunner) };
    // protected override void Build()
    // {        
    //     base.Build();
    //     Utils.CopyLauncher("FileFlows", OutputPath);
    // }

}