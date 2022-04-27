public class CommandLineArgumentAttribute : Attribute
{
    public string Name { get; set; }

    public CommandLineArgumentAttribute(string name = "")
    {
        this.Name = name;
    }
}