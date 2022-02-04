using FileFlows.Shared;
using System;

namespace FileFlows.Client.Helpers
{
    public class FileSizeFormatter
    {
        static string lblIncrease, lblDecrease;

        static FileSizeFormatter()
        {
            lblIncrease = Translater.Instant("Labels.Increase");
            lblDecrease= Translater.Instant("Labels.Decrease");
        }
        public static string FormatSize(long size) => FileFlows.Shared.Formatters.FileSizeFormatter.Format((double)size);

        public static string FormatShrinkage(long original, long final)
        {
            long diff = Math.Abs(original - final);
            return FormatSize(diff) + (original < final ? " " + lblIncrease : " " + lblDecrease) +
                            "\n" + FormatSize(final) + " / " + FormatSize(original);
        }
    }
}
