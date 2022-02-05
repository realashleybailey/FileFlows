namespace FileFlows.ServerShared.Helpers
{
    using NodaTime;
    public class TimeHelper
    {
        public static string UserTimeZone { get; set; }

        public static int GetCurrentQuarter()
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

        public static DateTime UserNow()
        {
            if (UserTimeZone == null)
                return DateTime.UtcNow;

            var instant = Instant.FromDateTimeUtc(DateTime.UtcNow);
            return instant.InZone(DateTimeZoneProviders.Tzdb[UserTimeZone]).ToDateTimeUnspecified();
            //return TimeZoneInfo.ConvertTime(DateTime.UtcNow, UserTimeZone);
        }


        public static bool InSchedule(string schedule)
        {
            if (string.IsNullOrEmpty(schedule) || schedule.Length != 672)
                return true; // bad schedule treat as always in schedule

            int quarter = GetCurrentQuarter();
            return schedule[quarter] == '1';
        }
    }
}
