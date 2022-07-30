using FileFlows.Shared.Models;

namespace FileFlows.Server;

/// <summary>
/// FileFlows System Events
/// </summary>
public class SystemEvents
{
    internal delegate void LibraryFileEvent(LibraryFileEventArgs args);
    internal delegate void UpdateEvent(UpdateEventArgs args);

    /// <summary>
    /// Event that is fired when a server update is available
    /// </summary>
    internal static event UpdateEvent OnServerUpdateAvailable;
    
    /// <summary>
    /// Event that is fired when the server is updating
    /// </summary>
    internal static event UpdateEvent OnServerUpdating;
    
    /// <summary>
    /// Triggers the server update event
    /// </summary>
    /// <param name="version">the version of the update available</param>
    internal static void TriggerServerUpdateAvailable(string version)
    {
        OnServerUpdateAvailable?.Invoke(new () { Version = version, CurrentVersion = Globals.Version.ToString() });
    }

    
    /// <summary>
    /// Triggers the server updating event
    /// </summary>
    /// <param name="version">the version of the update</param>
    internal static void TriggerServerUpdating(string version)
    {
        OnServerUpdating?.Invoke(new () { Version = version, CurrentVersion = Globals.Version.ToString() });
    }
    
    /// <summary>
    /// Event that is fired when a library file is added to the system
    /// </summary>
    internal static event LibraryFileEvent OnLibraryFileAdd;
    
    /// <summary>
    /// Event that is fired when a library file starts processing
    /// </summary>
    internal static event LibraryFileEvent OnLibraryFileProcessingStarted;
    
    /// <summary>
    /// Event that is fired when a library file finishes processing
    /// </summary>
    internal static event LibraryFileEvent OnLibraryFileProcessed;
    
    /// <summary>
    /// Event that is fired when a library file finishes processing successfully
    /// </summary>
    internal static event LibraryFileEvent OnLibraryFileProcessedSuceess;
    
    /// <summary>
    /// Event that is fired when a library file finishes processing failed
    /// </summary>
    internal static event LibraryFileEvent OnLibraryFileProcessedFailed;

    /// <summary>
    /// Triggers the file added event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    internal static void TriggerFileAdded(LibraryFile file, Library library)
    {
        OnLibraryFileAdd?.Invoke(new () { File = file, Library = library});
    }
    
    /// <summary>
    /// Triggers the library file processing started event event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    internal static void TriggerLibraryFileProcessingStarted(LibraryFile file, Library library)
    {
        OnLibraryFileProcessingStarted?.Invoke(new () { File = file, Library = library});
    }
    
    /// <summary>
    /// Triggers the library file processed event event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    internal static void TriggerLibraryFileProcessed(LibraryFile file, Library library)
    {
        OnLibraryFileProcessed?.Invoke(new () { File = file, Library = library});
    }
    
    /// <summary>
    /// Triggers the library file processed successfully event event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    internal static void TriggerLibraryFileProcessedSuccess(LibraryFile file, Library library)
    {
        OnLibraryFileProcessedSuceess?.Invoke(new () { File = file, Library = library});
    }
    
    /// <summary>
    /// Triggers the library file processed failed event event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    internal static void TriggerLibraryFileProcessedFailed(LibraryFile file, Library library)
    {
        OnLibraryFileProcessedFailed?.Invoke(new () { File = file, Library = library});
    }

    /// <summary>
    /// Arguments for a library file event
    /// </summary>
    public class LibraryFileEventArgs
    {
        /// <summary>
        /// Gets or sets the library the file
        /// </summary>
        public LibraryFile File { get; set; }
        
        /// <summary>
        /// Gets or sets the library the file is from
        /// </summary>
        public Library Library { get; set; }
        
    }

    /// <summary>
    /// Event fired for update events
    /// </summary>
    public class UpdateEventArgs
    {
        /// <summary>
        /// Gets or sets the version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the current version
        /// </summary>
        public string CurrentVersion { get; set; }
    }
}