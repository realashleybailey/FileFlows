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
    /// Gets or sets the interval for the schedule
    /// </summary>
    protected int Interval { get; set; }
    /// <summary>
    /// Gets or sets the schedule of this worker
    /// </summary>
    protected ScheduleType Schedule { get; set; }

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
        this.Schedule = schedule;
        this.Interval = interval;
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
            timer.Interval = ScheduleNext() * 1_000;
            timer.AutoReset = false;
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

    private void TimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            Trigger();
        }
        catch (Exception)
        {
        }
        finally
        {               
            timer.Interval = ScheduleNext() * 1_000;
            timer.AutoReset = false;
            timer.Start();
        }
    }

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

    private int ScheduleNext()
    {
        switch (this.Schedule)
        {
            case ScheduleType.Daily: return ScheduleDaily();
            case ScheduleType.Hourly: return ScheduleHourly();
            case ScheduleType.Minute: return ScheduleMinute();
        }

        // seconds
        return Interval;
    }

    /// <summary>
    /// Gets how many how many seconds until specified hour
    /// </summary>
    /// <returns>how many seconds until specified hour</returns>
    private int ScheduleDaily()
    {
        DateTime now = DateTime.Now;
        DateTime next = DateTime.Today.AddHours(this.Interval);
        if (next < now)
            next = next.AddDays(1);
        return SecondsUntilNext(next);
    }

    /// <summary>
    /// Gets how many how many seconds until specified hour
    /// </summary>
    /// <returns>how many seconds until specified hour</returns>
    private int ScheduleHourly()
    {
        DateTime now = DateTime.Now.AddMinutes(3); // some padding
        DateTime next = DateTime.Today;
        while (next < now)
            next = next.AddHours(this.Interval);
        return SecondsUntilNext(next);
    }
    /// <summary>
    /// Gets how many how many seconds until specified minute
    /// </summary>
    /// <returns>how many seconds until specified minute</returns>
    private int ScheduleMinute()
    {
        DateTime now = DateTime.Now.AddSeconds(30); // some padding
        DateTime next = DateTime.Today;
        while (next < now)
            next = next.AddMinutes(this.Interval);
        return SecondsUntilNext(next);
    }
    private int SecondsUntilNext(DateTime next) => (int)Math.Ceiling((next - DateTime.Now).TotalSeconds);
}
