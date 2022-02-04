namespace FileFlows.Shared.Formatters
{
    public class FileSizeFormatter : Formatter
    {
        protected override string Format(object value)
        {
            double dValue = 0;
            if (value is long longValue)
            {
                dValue = longValue;
            }
            else if (value is int intValue)
            {
                dValue = intValue;
            }
            else if (value is decimal decimalValue)
            {
                dValue = Convert.ToDouble(decimalValue);
            }
            else if (value is float floatValue)
            {
                dValue = floatValue;
            }
            else if (value is short shortValue)
            {
                dValue = shortValue;
            }
            else if (value is byte byteValue)
            {
                dValue = byteValue;
            }

            return Format(dValue);
        }

        static string[] sizes = { "B", "KB", "MB", "GB", "TB" };

        public static string Format(double size)
        {
            int order = 0;
            double num = size;
            while (num >= 1000 && order < sizes.Length - 1)
            {
                order++;
                num /= 1000;  // 1024 would be a kibibyte.  I'm trying to embrace the proper metric meaning....
            }
            return String.Format("{0:0.##} {1}", num, sizes[order]);
        }
    }
}
