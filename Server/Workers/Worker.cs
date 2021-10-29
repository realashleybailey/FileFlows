namespace FileFlow.Server.Workers
{
    public abstract class Worker
    {
        public enum ScheduleType
        {
            Minute,
            Hourly,
            Daily
        }

        private int Minute { get; set; }
        private ScheduleType Schedule { get; set; }

        public Worker(ScheduleType schedule, int minute)
        {
            this.Schedule = schedule;
            this.Minute = minute;
        }

        static readonly List<Worker> Workers = new List<Worker>();

        public static void StartWorkers()
        {
            if (Workers.Any())
                return; // workers already running
            Workers.Add(new LibraryWorker());
            foreach (var worker in Workers)
                worker.Start();
        }
        public static void StopWorkers()
        {
            foreach (var worker in Workers)
                worker.Stop();
            Workers.Clear();
        }

        private System.Timers.Timer timer;

        public virtual void Start()
        {
            if (timer != null)
            {
                if (timer.Enabled)
                    return; // arleady running
                timer.Start();
            }
            else
            {
                timer = new System.Timers.Timer();
                timer.Elapsed += TimerElapsed;
                timer.Interval = Minute * 60_000;
                timer.AutoReset = true;
                timer.Start();
            }
        }

        public virtual void Stop()
        {
            if (timer == null)
                return;
            timer.Stop();
            timer.Dispose();
            timer = null;
        }

        private bool Executing = false;

        private void TimerElapsed(object? sender, System.Timers.ElapsedEventArgs e) => Trigger();

        protected void Trigger()
        {
            if (Executing)
                return; // dont let run twice

            Executing = true;
            try
            {
                string prefix = " Starting " + this.GetType().Name + " ";
                if (prefix.Length % 2 == 1)
                    prefix += "#";
                prefix = new string('#', (50 - prefix.Length) / 2) + prefix + new string('#', (50 - prefix.Length) / 2);
                Logger.Instance.ILog(prefix);
                Execute();
                Logger.Instance.ILog(new string('#', 50));
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog($"Error in worker '{this.GetType().Name}': {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            finally
            {
                Executing = false;
            }
        }

        protected virtual void Execute()
        {

        }
    }
}