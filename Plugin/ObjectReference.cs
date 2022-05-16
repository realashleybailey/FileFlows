namespace FileFlows.Plugin;

/// <summary>
/// An object reference is an reference key to a different object within FileFlows
/// </summary>
public class ObjectReference
{
    /// <summary>
    /// The name of the object being referenced
    /// Note: this may be out of date and will be updated next time the object is saved
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the UID of the object being referenced
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the type of object being referenced
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets the name of the object
    /// </summary>
    /// <returns>the name of the object</returns>
    public override string ToString() => Name ?? string.Empty;
}
