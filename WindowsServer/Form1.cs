using System.Diagnostics;

namespace FileFlows.WindowsServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            lnkOpen.Text = Program.Url;
            this.Hide();
            this.WindowState = FormWindowState.Minimized;
            notifyIcon.Visible = true;
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

        private void QuitMe()
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