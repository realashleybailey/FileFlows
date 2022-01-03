using Microsoft.AspNetCore.Components;
using System;
using System.Timers;

namespace FileFlows.Client.Components
{
    public partial class Toast:ComponentBase
    {
        private Timer timer = new Timer();

        public static Toast Instance { get; private set; }

        private readonly List<ToastMessage> Messages = new List<ToastMessage>();


        protected override void OnInitialized()
        {
            Instance = this;
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Interval = 200;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach(var message in Messages.ToArray())
            {
                if (message.Dismissing)
                {
                    if (message.DismissingTime < DateTime.Now.AddMilliseconds(-750))
                    {
                        lock (Messages)
                        {
                            Messages.Remove(message);
                            this.StateHasChanged();
                            if (Messages.Count == 0)
                                timer.Enabled = false;
                        }
                    }
                }
                else if(message.TimeShown < DateTime.Now.AddMilliseconds(-message.Duration))
                {
                    message.Dismissing = true;
                    message.DismissingTime = DateTime.Now;
                    this.StateHasChanged();
                }
            }
        }

        public static void ShowError(string message, int duration = 5_000)
        {
            Instance.ShowMessage(message, "flow-toast-error", duration: duration);
        }

        public static void ShowInfo(string message, int duration = 5_000)
        {
            Instance.ShowMessage(message, "flow-toast-info", duration: duration);
        }
        public static void ShowSuccess(string message, int duration = 5_000)
        {
            Instance.ShowMessage(message, "flow-toast-success", duration: duration);
        }
        public static void ShowWarning(string message, int duration = 5_000)
        {
            Instance.ShowMessage(message, "flow-toast-warning", duration: duration);
        }

        private void ShowMessage(string message, string @class, int duration = 5_000)
        {
            ToastMessage tm = new ToastMessage
            {
                Message = Translater.TranslateIfNeeded(message),
                Class = @class,
                TimeShown = DateTime.Now,
                Duration = duration
            };

            lock (Messages)
            {
                Messages.Add(tm);
                if(timer.Enabled == false)
                {
                    timer.Enabled = true;
                }
                this.StateHasChanged();
            }
        }

        private void Dismiss(ToastMessage toast)
        {
            lock (Messages)
            {
                toast.DismissingTime = DateTime.Now;
                toast.Dismissing = true;
            }
            this.StateHasChanged();
        }

        class ToastMessage
        {
            public string Message { get; set; }
            public string Class { get; set; }
            public DateTime TimeShown { get; set; }
            public int Duration { get; set; }   

            public bool Dismissing { get; set; }
            public DateTime DismissingTime { get; set; }
        }
    }

    
}
