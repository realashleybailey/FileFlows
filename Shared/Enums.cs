namespace FileFlows.Shared;

/// <summary>
/// A list of types of operating systems
/// </summary>
public enum OperatingSystemType
{
    /// <summary>
    /// Unknown operating system
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Windows operating system
    /// </summary>
    Windows = 1,
    /// <summary>
    /// Linux operating system
    /// </summary>
    Linux = 2,
    /// <summary>
    /// Mac/Apple operating system
    /// </summary>
    Mac = 3
}

/// <summary>
/// A type of Flow
/// </summary>
public enum FlowType
{
    /// <summary>
    /// A standard flow
    /// </summary>
    Standard = 0,
    /// <summary>
    /// A special flow that is executed when a flow fails during execution
    /// </summary>
    Failure = 1
}


/// <summary>
/// The available processing libraries options
/// </summary>
public enum ProcessingLibraries
{
    /// <summary>
    /// Only process the libraries specified
    /// </summary>
    Only = 0,
    /// <summary>
    /// Process all libraries
    /// </summary>
    All = 1,
    /// <summary>
    /// Process all libraries except those specified
    /// </summary>
    AllExcept = 2
}

/// <summary>
/// A type of script
/// </summary>
public enum ScriptType
{
    /// <summary>
    /// A script used in a flow
    /// </summary>
    Flow = 0,
    /// <summary>
    /// A script used by the system to process something
    /// </summary>
    System = 1
}