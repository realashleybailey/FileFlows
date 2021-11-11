namespace FileFlow.Shared.Models
{
    using System;

    public class FlowWorkerStatus
    {
        public Guid Uid { get; set; }
        public string CurrentFile { get; set; }
        public string WorkingFile { get; set; }

        public ProcessStatus Status => string.IsNullOrEmpty(CurrentFile) ? ProcessStatus.Waiting : ProcessStatus.Processing;
        public int TotalParts { get; set; }
        public int CurrentPart { get; set; }

        public string CurrentPartName { get; set; }

        public float CurrentPartPercent { get; set; }

        public DateTime StartedAt { get; set; }

        public TimeSpan ProcessingTime => StartedAt > new DateTime(2000, 1, 1) ? DateTime.Now.Subtract(StartedAt) : new TimeSpan();
    }

    public enum ProcessStatus
    {
        Waiting,
        Processing
    }
}