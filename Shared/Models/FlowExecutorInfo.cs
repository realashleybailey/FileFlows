namespace FileFlows.Shared.Models;

public class FlowExecutorInfo
{
    public Guid Uid { get; set; }
    public Guid NodeUid { get; set; }
    public string NodeName { get; set; }

    public string Log { get; set; }

    public LibraryFile LibraryFile { get; set; }

    public string RelativeFile { get; set; }

    public ObjectReference Library { get; set; }

    public string WorkingFile { get; set; }

    public int TotalParts { get; set; }
    public int CurrentPart { get; set; }

    public string CurrentPartName { get; set; }

    public float CurrentPartPercent { get; set; }

    public DateTime StartedAt { get; set; }

    public TimeSpan ProcessingTime => StartedAt > new DateTime(2000, 1, 1) ? DateTime.UtcNow.Subtract(StartedAt) : new TimeSpan();
}

