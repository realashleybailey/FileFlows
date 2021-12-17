using FileFlows.Node;
using FileFlows.Node.Workers;
using FileFlows.Server.Workers;

namespace FileFlows.WindowsNode
{
    public partial class Form1 : Form
    {
        public Form1(bool minimize)
        {
            InitializeComponent();
            this.txtServer.Text = AppSettings.Instance.ServerUrl ?? String.Empty;
            this.txtTempPath.Text = AppSettings.Instance.TempPath ?? String.Empty;
            this.numRunners.Value = AppSettings.Instance.Runners;
            this.chkEnabled.Checked = AppSettings.Instance.Enabled;
            this.chkRunAtStartup.Checked = DetectRunAtStartUp();

            if (minimize)
            {
                this.Hide();
                this.WindowState = FormWindowState.Minimized;   
                notifyIcon.Visible = true;
            }

            ServerShared.Services.Service.ServiceBaseUrl = this.txtServer.Text;
            WorkerManager.StartWorkers(new FlowWorker()
            {
                IsEnabledCheck = () => AppSettings.Instance.Enabled
            });
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
                WorkerManager.StopWorkers();
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
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

        private void openToolStripMenuItem_Click(object sender, EventArgs e) => Open();
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e) => Open();

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void EnableForm(bool enable = true)
        {
            if (this.txtServer.InvokeRequired)
            {
                Invoke(new Action(() => EnableForm(enable)));
                return;
            }
            this.txtServer.Enabled = enable;
            this.txtTempPath.Enabled = enable;
            this.btnBrowse.Enabled = enable;    
            this.numRunners.Enabled = enable;
            this.btnSave.Enabled = enable;
            this.chkEnabled.Enabled = enable;
            this.chkRunAtStartup.Enabled = enable;
        }

        private void SetUrl(string url)
        {
            if(this.txtServer.InvokeRequired)
            {
                Invoke(new Action(() => SetUrl(url)));
                return;
            }
            txtServer.Text = url;
        }

        private void ShowMessageBox(string text)
        {
            if (this.txtServer.InvokeRequired)
            {
                Invoke(new Action(() => ShowMessageBox(text)));
                return;
            }
            MessageBox.Show(text, "Failed to Connect");
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string url = this.txtServer.Text;
            string path = this.txtTempPath.Text;
            int runners = (int)this.numRunners.Value;
            bool enabled = this.chkEnabled.Checked;
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(path))
                return;

            bool runAtStartup = this.chkRunAtStartup.Checked;
            RegisterRunAtStartUp(runAtStartup);

            EnableForm(false);
            Task.Run(async () =>
            {
                var result = ConnectionTester.SaveConnection(url, path, runners, enabled);
                if (result.Item1)
                {
                    // can connect
                    SetUrl(result.Item2);
                    ServerShared.Services.Service.ServiceBaseUrl = result.Item2;
                    AppSettings.Save(new AppSettings
                    {
                        ServerUrl = result.Item2,
                        Enabled = enabled,
                        Runners = runners,
                        TempPath = path
                    });
                }
                else
                {
                    // cant connect
                    ShowMessageBox(result.Item2);
                }
                EnableForm(true);
            });
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if(folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                this.txtTempPath.Text = folderBrowserDialog.SelectedPath;
            }
        }



        private void RegisterRunAtStartUp(bool run)
        {
            try
            {
                var regkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
                var key = regkey.GetValue("FileFlowsNode");
                if (run && key == null)
                {
                    // not there
                    regkey.SetValue("FileFlowsNode", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\" -minimized");
                }
                else if (run == false && key != null)
                {
                    regkey.DeleteValue("FileFlowsNode", false);
                }
            }
            catch (Exception) { }
        }

        private bool DetectRunAtStartUp()
        {
            try
            {
                var regkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                return regkey.GetValue("FileFlowsNode") != null;
            }
            catch (Exception) { }
            return false;
        }
    }
}