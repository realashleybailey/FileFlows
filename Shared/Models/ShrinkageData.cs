namespace FileFlows.Shared.Models
{
    public class ShrinkageData
    {
        public double OriginalSize { get; set; }
        public double FinalSize { get; set; }
        public int Items { get; set; }

        public double Difference => OriginalSize - FinalSize;
    }
}
