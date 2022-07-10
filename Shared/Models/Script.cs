namespace FileFlows.Shared.Models;

/// <summary>
/// A script is a special function node that lets you reuse them
/// </summary>
public class Script:IUniqueObject<string>
{
    /// <summary>
    /// Gets or sets the name of the script
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the javascript code of the script
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Alias for Name
    /// </summary>
    public string Uid
    {
        get => Name;
        set => Name = value;
    }
}