public class Client : Component
{
    public override string OutputPath => BuildOptions.Output + "/" + nameof(Server);

    protected override void Build()
    {
        base.Build();

        Utils.DeleteFiles(OutputPath, "*.dll", recurisve: true);
        Utils.DeleteFiles(OutputPath, "*.gz", recurisve: true);
        Utils.DeleteFiles(OutputPath, "*.scss", recurisve: true);
        Utils.RegexReplace(OutputPath + "/wwwroot/index.html", Regex.Escape("&& location.hostname !== ''localhost''"), string.Empty);
    }
}