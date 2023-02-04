using System.Threading;

namespace FileFlows.Client;

public class BackgroundTask
{
    private Task _timerTask;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private Action _action;

    public BackgroundTask(TimeSpan interval, Action action)
    {
        _timer = new PeriodicTimer(interval);
        this._action = action;
    }

    public void Start()
    {
        _timerTask = DoWorkAsync();
    }

    private async Task DoWorkAsync()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                try
                {
                    this._action();
                }
                catch (Exception)
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task StopAsync()
    {
        if (_timerTask is null)
            return;
        _cts.Cancel();
        await _timerTask;
        _cts.Dispose();
    }
}