namespace FileFlows.Server.Helpers
{
    public class TimeHelper
    {
        internal static int GetCurrentQuarter()
        {
            DateTime date = DateTime.Now;

            int quarter = (((int)date.DayOfWeek) * 96) + (date.Hour * 4);
            if (date.Minute >= 45)
                quarter += 3;
            else if (date.Minute >= 30)
                quarter += 2;
            else if (date.Minute >= 15)
                quarter += 1;
            return quarter;
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
