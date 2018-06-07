using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace WinService.Debugger
{
    public partial class ServiceRunner : Form
    {
        private readonly IDebuggableService _theService;
        private delegate void UpdateTextBoxDelegate(string text, TextBox tb);

        public ServiceRunner()
        {
            InitializeComponent();
        }

        public ServiceRunner(IDebuggableService service)
        {
            InitializeComponent();
            _theService = service;
            EventLog log = _theService.GetEventLog();
            log.EnableRaisingEvents = true;
            log.EntryWritten += log_EntryWritten;
            Show();
        }

        void log_EntryWritten(object sender, EntryWrittenEventArgs e)
        {
            EventLog log = sender as EventLog;
            string message = e.Entry.Message;
            UpdateTextBox(message, tbAvailTSE);

        }

        private void UpdateTextBox(string message, TextBox tb)
        {
            if (tb.InvokeRequired)
            {
                tb.BeginInvoke(new UpdateTextBoxDelegate(UpdateTextBox), new object[] { message, tb });
            }
            else
                tb.Text += Environment.NewLine + Environment.NewLine + message;
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            _theService.Pause();
            toolStripStatusLabel1.Text = "Paused";
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            _theService.Continue();
            toolStripStatusLabel1.Text = "Started";
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _theService.Stop();
            toolStripStatusLabel1.Text = "Stoped";
        }

        private void ServiceRunner_FormClosing(object sender, FormClosingEventArgs e)
        {
            //_theService.Stop();
            //Thread.Sleep(1000);
        }

        private void start_Click(object sender, EventArgs e)
        {
            _theService.Start(null);
        }
    }
}
