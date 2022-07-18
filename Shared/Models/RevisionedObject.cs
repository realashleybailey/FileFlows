namespace FileFlows.Shared.Models;

/// <summary>
/// A revisioned object is a saved state of a object
/// </summary>
public class RevisionedObject : IUniqueObject<Guid>
{
    /// <summary>
    /// Gets or sets the UID of the item
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the type that has been revision
    /// </summary>
    public string RevisionType { get; set; }

    /// <summary>
    /// Gets or sets the UID of the revisioned object
    /// </summary>
    public Guid RevisionUid { get; set; }

    /// <summary>
    /// Gets or sets the revisioned name
    /// </summary>
    public string RevisionName { get; set; }

    /// <summary>
    /// Gets or sets the date this revision was made
    /// </summary>
    public DateTime RevisionDate { get; set; }

    /// <summary>
    /// Gets or sets the date this object was first created
    /// We only store this so we can restore it if the object was deleted
    /// </summary>
    public DateTime RevisionCreated { get; set; }

    /// <summary>
    /// Gets or sets the revisioned data
    /// </summary>
    public string RevisionData { get; set; }

}