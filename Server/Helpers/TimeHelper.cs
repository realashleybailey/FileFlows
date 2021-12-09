namespace FileFlows.Server.Helpers
{
    public class TimeHelper
    {
        private static TimeZoneInfo _UserTimeZone = TimeZoneInfo.Local;
        public static TimeZoneInfo UserTimeZone
        {
            get => _UserTimeZone;
            set
            {
                _UserTimeZone = value ?? TimeZoneInfo.Local;
            }
        }

        internal static int GetCurrentQuarter()
        {
            DateTime date = UserNow();

            int quarter = (((int)date.DayOfWeek) * 96) + (date.Hour * 4);
            if (date.Minute >= 45)
                quarter += 3;
            else if (date.Minute >= 30)
                quarter += 2;
            else if (date.Minute >= 15)
                quarter += 1;
            return quarter;
        }

        internal static DateTime UserNow()
        {
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, UserTimeZone);
        }

        internal static bool InSchedule(string schedule)
        {
            if (string.IsNullOrEmpty(schedule) || schedule.Length != 672)
                return true; // bad schedule treat as always in schedule

            int quarter = GetCurrentQuarter();
            return schedule[quarter] == '1';
        }
    }
}
