namespace FileFlows.Shared.Portlets;



/// <summary>
/// Portlet definition, these are the different types of portlets in the system
/// </summary>
public abstract class PortletDefinition
{
    /// <summary>
    /// Gets a portlet definition from its UID
    /// </summary>
    /// <param name="uid">The UID of the portlet definition</param>
    /// <returns>the portlet definition</returns>
    public static PortletDefinition GetDefinition(Guid uid)
    {
        if (uid == Processing.PD_UID)
            return new Processing();
        if (uid == FilesRecentlyFinished.PD_UID)
            return new FilesRecentlyFinished();
        if (uid == FilesUpcoming.PD_UID)
            return new FilesUpcoming();
        if (uid == AudioCodecs.PD_UID)
            return new AudioCodecs();
        if (uid == Codecs.PD_UID)
            return new Codecs();
        if (uid == CpuUsage.PD_UID)
            return new CpuUsage();
        if (uid == LibraryProcessingTimes.PD_UID)
            return new LibraryProcessingTimes();
        if (uid == LogStorage.PD_UID)
            return new LogStorage();
        if (uid == MemoryUsage.PD_UID)
            return new MemoryUsage();
        if (uid == ProcessingTimes.PD_UID)
            return new ProcessingTimes();
        if (uid == TempStorage.PD_UID)
            return new TempStorage();
        if (uid == VideoCodecs.PD_UID)
            return new VideoCodecs();
        if (uid == VideoContainers.PD_UID)
            return new VideoContainers();
        if (uid == VideoResolution.PD_UID)
            return new VideoResolution();
        throw new Exception("Unknown portlet: " + uid);
    }
    
    /// <summary>
    /// Gets the UID 
    /// </summary>
    public abstract Guid Uid { get; }
    /// <summary>
    /// Gets the URL
    /// </summary>
    public abstract string Url { get; }
    /// <summary>
    /// Gets the Icon
    /// </summary>
    public abstract string Icon { get; }
    /// <summary>
    /// Gets the Name
    /// </summary>
    public abstract string Name { get; }
    /// <summary>
    /// Gets the type of portlet
    /// </summary>
    public abstract PortletType Type { get; }
    /// <summary>
    /// Gets any flags 
    /// </summary>
    public abstract int Flags { get; }
}


/// <summary>
/// Available portlet types
/// </summary>
public enum PortletType
{
    /// <summary>
    /// Processing files
    /// </summary>
    Processing = 1,
    /// <summary>
    /// Table of library files
    /// </summary>
    LibraryFileTable = 2,
    /// <summary>
    /// Box plot 
    /// </summary>
    BoxPlot = 101,
    /// <summary>
    /// Heat map
    /// </summary>
    HeatMap = 102,
    /// <summary>
    /// Pie chart
    /// </summary>
    PieChart = 103,
    /// <summary>
    /// Tree map
    /// </summary>
    TreeMap = 104,
    /// <summary>
    /// Time series percentage
    /// </summary>
    TimeSeries = 105
}