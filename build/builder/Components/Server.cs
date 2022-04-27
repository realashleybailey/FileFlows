public class Server : Component
{
    public override Type[] Dependencies => new [] { typeof(Client), typeof(FlowRunner) };
}