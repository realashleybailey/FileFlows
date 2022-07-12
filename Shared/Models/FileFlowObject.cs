using FileFlows.Plugin;

namespace FileFlows.Shared.Models;

/// <summary>
/// A file flow object
/// This is the base object for all database objects
/// </summary>
public class FileFlowObject: IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or sets the UID of the item
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the item
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the date this item was created
    /// </summary>
    public DateTime DateCreated { get; set; }
    
    /// <summary>
    /// Gets or sets the date this item was last modified
    /// </summary>
    public DateTime DateModified { get; set; }
}

/// <summary>
/// Interface used for unique objects.
/// This mean only one object of this type can exist in the database
/// </summary>
public interface IUniqueObject<T>
{
    /// <summary>
    /// Gets or sets the UID of the item
    /// </summary>
    T Uid { get; set; }
}

/// <summary>
/// Interface to show what is using this object
/// </summary>
public interface IInUse
{
    /// <summary>
    /// Gets or sets what is using this object
    /// </summary>
    List<ObjectReference> UsedBy { get; set; }

    /// <summary>
    /// Gets if this object is in use
    /// </summary>
    bool InUse => UsedBy?.Count > 0;

}