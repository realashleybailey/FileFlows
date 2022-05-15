namespace FileFlows.Shared.Models;

/// <summary>
/// Information about how a library has shrunk after processing
/// </summary>
public class ShrinkageData
{
    /// <summary>
    /// Gets or sets the original size of the library files
    /// in the library
    /// </summary>
    public double OriginalSize { get; set; }
    
    /// <summary>
    /// Gets or sets the final size of the library files
    /// in the library
    /// </summary>
    public double FinalSize { get; set; }
    
    /// <summary>
    /// Gets or sets number of processed files in the library
    /// </summary>
    public int Items { get; set; }

    /// <summary>
    /// Gets the different of original size minus the final size,
    /// i.e. how much the library has shrunk after processing
    /// </summary>
    public double Difference => OriginalSize - FinalSize;
}