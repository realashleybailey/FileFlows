namespace FileFlow.Shared.Models
{
    using System;

    public class FlowWorkerStatus
    {
        public Guid Uid { get; set; }
        public string CurrentFile { get; set; }

        public ProcessStatus Status => string.IsNullOrEmpty(CurrentFile) ? ProcessStatus.Waiting : ProcessStatus.Processing;
        public int TotalParts { get; set; }
        public int CurrentPart { get; set; }

        public string CurrentPartName { get; set; }

        public float CurrentPartPercent { get; set; }
    }

    public enum ProcessStatus
    {
        Waiting,
        Processing
    }
}