using FileFlows.Shared;
using System;

namespace FileFlows.Client.Helpers
{
    public class FileSizeFormatter
    {
        static string[] sizes = { "B", "KB", "MB", "GB", "TB" };

        static string lblIncrease, lblDecrease;

        static FileSizeFormatter()
        {
            lblIncrease = Translater.Instant("Labels.Increase");
            lblDecrease= Translater.Instant("Labels.Decrease");
        }
        public static string FormatSize(long size)
        {
            int order = 0;
            double num = (double)size;
            while (num >= 1024 && order < sizes.Length - 1)
            {
                order++;
                num = num / 1024;
            }
            return String.Format("{0:0.##} {1}", num, sizes[order]);
        }

        public static string FormatShrinkage(long original, long final)
        {
            long diff = Math.Abs(original - final);
            return FormatSize(diff) + (original < final ? " " + lblIncrease : " " + lblDecrease) +
                            "\n" + FormatSize(final) + " / " + FormatSize(original);
        }
    }
}
