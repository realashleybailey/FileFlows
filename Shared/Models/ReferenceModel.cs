namespace FileFlows.Shared.Models;

/// <summary>
/// A list of UIDs used for deleting/enabled etc
/// </summary>
public class ReferenceModel<T>
{
    /// <summary>
    /// Gets or sets the UIDs 
    /// </summary>
    public T[] Uids { get; set; }
}