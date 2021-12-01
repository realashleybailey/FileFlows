namespace FileFlows.Server.Workers
{
    public abstract class Worker
    {
        public enum ScheduleType
        {
            Second,
            Minute,
            Hourly,
            Daily
        }

        private int Seconds { get; set; }
        private ScheduleType Schedule { get; set; }

        public Worker(ScheduleType schedule, int interval)
        {
            if (interval < 1)
                interval = 1;

            if (schedule == ScheduleType.Minute)
                interval *= 60;
            if (schedule == ScheduleType.Hourly)
                interval *= 60 * 60;
            if (schedule == ScheduleType.Daily)
                interval *= 60 * 60 * 24;

            this.Schedule = schedule;
            this.Seconds = interval;
        }

        static readonly List<Worker> Workers = new List<Worker>();

        public static void StartWorkers()
        {
            if (Workers.Any())
                return; // workers already running
            Workers.Add(new LibraryWorker());
            Workers.Add(new FlowWorker());
            Workers.Add(new TelemetryReporter());
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
                timer.SynchronizingObject = null;
                timer.Interval = Seconds * 1_000;
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

            _ = Task.Run(() =>
            {
                Executing = true;
                try
                {
                    Execute();
                }
                catch (Exception ex)
                {
                    Logger.Instance.ELog($"Error in worker '{this.GetType().Name}': {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                }
                finally
                {
                    Executing = false;
                }
            });
        }

        protected virtual void Execute()
        {
        }
    }
}