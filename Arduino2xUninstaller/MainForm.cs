using System.Diagnostics;

namespace Arduino2xUninstaller
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();

            ArduinoUninstaller.ProgressUpdate += ArduinoUninstaller_ProgressUpdate;

            if(ArduinoUninstaller.IsInstalled())
            {
                label_Progress.Text = "Installation found, waiting for input.";
            }
            else
            {
                button_Uninstall.Enabled = false;
                label_Progress.Text = "No Installations were found.";
            }
        }

        private void ArduinoUninstaller_ProgressUpdate(int progress, string message)
        {
            progressBar.Value = progress;
            label_Progress.Text = message;
        }

        private void button_Uninstall_Click(object sender, EventArgs e)
        {
            Label_Retry:
            if(ArduinoUninstaller.IsArduinoIdeOpen())
            {
                DialogResult result = MessageBox.Show("All running instances of the Arduino IDE must be closed. Please close all running instances, or press \"Continue\" to have them killed.", "Running Instances Detected!", MessageBoxButtons.CancelTryContinue);

                switch(result)
                {
                    case DialogResult.Cancel:
                        return;
                    case DialogResult.Continue:
                        if (!ArduinoUninstaller.CloseArduinoIde())
                            goto Label_Retry;
                        break;
                    case DialogResult.Retry:
                        goto Label_Retry;

                }
            }

            ErrorCodes errorCodes = ArduinoUninstaller.Uninstall();
        }

        private void repoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/AddioElectronics/Arduino2xUninstaller") { UseShellExecute = true});
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string message = @"The official Arduino 2x Uninstaller can produce false positives for open Arduino IDE instances, and will effectively freeze the uninstall. 
This program manually removes the registry keys and deletes the files. 
It's recommended to use the official uninstaller when possible.
Use at your own risk.";



            DialogResult messageBox = MessageBox.Show(message, "About", MessageBoxButtons.OK);
        }
    }
}