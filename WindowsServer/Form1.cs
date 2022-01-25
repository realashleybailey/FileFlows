using System.Diagnostics;
namespace FileFlows.WindowsServer
{
    public partial class Form1 : Form
    {
        internal static Form1 Instance { get;private set; }

        private System.Timers.Timer timer;
        public Form1()
        {
            InitializeComponent();
            Instance = this;

            this.FormClosed += Form1_FormClosed;

            lnkOpen.Text = Program.Url;
            this.Hide();
            this.WindowState = FormWindowState.Minimized;
            notifyIcon.Visible = true;
        }

        private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if(notifyIcon != null)
                notifyIcon.Visible = false;
        }

        protected override void SetVisibleCore(bool value)
        {
            if (IsHandleCreated == false && value)
            {
                value = false;
                CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        private void btnShutDown_Click(object sender, EventArgs e)
        {
            ShutDown();
        }

        internal void QuitMe()
        {
            if (this.InvokeRequired)
            {
                Invoke(new Action(() => QuitMe()));
                return;
            }
            this.Close();
        }

        private void ShutDown()
        {
            //this.btnShutDown.Enabled = false;
            Task.Run(async () =>
            {
                try
                {
                    WebServerHelper.Stop();
                    this.QuitMe();
                }
                catch (Exception) { }
            });
        }

        private void lnkOpen_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.LaunchBrowser();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.LaunchBrowser();
        }


        private void shutDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShutDown();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
            }
        }
        void Open()
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Program.LaunchBrowser();
        }
    }
}