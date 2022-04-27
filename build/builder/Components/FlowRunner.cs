public class FlowRunner : Component
{
    public override string OutputPath => BuildOptions.TempPath + "/" + this.Name;
}