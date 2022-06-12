namespace FileFlows.ServerShared.Workers;

/// <summary>
/// A worker that will run at a set schedule
/// </summary>
public abstract class Worker
{
    /// <summary>
    /// Available schedule types
    /// </summary>
    public enum ScheduleType
    {
        Second,
        Minute,
        Hourly,
        Daily
    }

    /// <summary>
    /// Gets or sets the amount seconds between each execution of this worker
    /// </summary>
    private int Seconds { get; set; }
    /// <summary>
    /// Gets or sets the schedule of this worker
    /// </summary>
    private ScheduleType Schedule { get; set; }

    /// <summary>
    /// Creates an instance of a worker
    /// </summary>
    /// <param name="schedule">the type of schedule this worker runs at</param>
    /// <param name="interval">the interval of this worker</param>
    protected Worker(ScheduleType schedule, int interval)
    {
        Initialize(schedule, interval);
    }

    protected virtual void Initialize(ScheduleType schedule, int interval)
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

    private System.Timers.Timer timer;

    /// <summary>
    /// Start the worker
    /// </summary>
    public virtual void Start()
    {
        Logger.Instance?.ILog("Starting worker: " + this.GetType().Name);
        if (timer != null)
        {
            if (timer.Enabled)
                return; // already running
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


    /// <summary>
    /// Stop the worker
    /// </summary>
    public virtual void Stop()
    {
        Logger.Instance?.ILog("Stopping worker: " + this.GetType().Name);
        if (timer == null)
            return;
        timer.Stop();
        timer.Dispose();
        timer = null;
    }

    private bool Executing = false;

    private void TimerElapsed(object? sender, System.Timers.ElapsedEventArgs e) => Trigger();

    public void Trigger()
    {
        if (Executing)
            return; // dont let run twice
        Logger.Instance.ILog("Triggering worker: " + this.GetType().Name);

        _ = Task.Run(() =>
        {
            Executing = true;
            try
            {
                Execute();
            }
            catch (Exception ex)
            {
                Logger.Instance?.ELog($"Error in worker '{this.GetType().Name}': {ex.Message}{Environment.NewLine}{ex.StackTrace}");
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
