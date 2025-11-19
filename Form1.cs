using System;
using System.Drawing;
using System.Windows.Forms;

namespace WebUIMonitor
{
    public class Form1 : Form
    {
        private FileMonitor _monitor;
        private AudioPlayer _audio;
        private Label lblStatus;
        private Label lblFileCount;
        private Label lblAlarmStatus;
        private Timer _updateTimer;

        public Form1()
        {
            InitializeUI();
            _monitor = new FileMonitor(Config.GetWebUIOutputPath());
            _audio = new AudioPlayer(Config.GetAudioPath());
            
            _monitor.Start();
            StartUpdateTimer();
        }

        private void InitializeUI()
        {
            this.Text = "WebUI 文件监控";
            this.Size = new Size(400, 200);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;

            // 状态标签
            lblStatus = new Label
            {
                Text = "监控中...",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.Green
            };
            this.Controls.Add(lblStatus);

            // 文件数标签
            lblFileCount = new Label
            {
                Text = "文件数: 0",
                Location = new Point(20, 60),
                AutoSize = true,
                Font = new Font("Arial", 12),
                ForeColor = Color.White
            };
            this.Controls.Add(lblFileCount);

            // 警报状态标签
            lblAlarmStatus = new Label
            {
                Text = "✓ 正常",
                Location = new Point(20, 100),
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.Green
            };
            this.Controls.Add(lblAlarmStatus);
        }

        private void StartUpdateTimer()
        {
            _updateTimer = new Timer { Interval = 1000 };
            _updateTimer.Tick += (s, e) =>
            {
                lblFileCount.Text = $"文件数: {_monitor.FileCount}";

                if (_monitor.IsAlarm)
                {
                    lblStatus.ForeColor = Color.Red;
                    lblStatus.Text = "⚠️ 警报中!";
                    lblAlarmStatus.ForeColor = Color.Red;
                    lblAlarmStatus.Text = "⚠️ 警报";
                    _audio.Play();
                }
                else
                {
                    lblStatus.ForeColor = Color.Green;
                    lblStatus.Text = "✓ 正常";
                    lblAlarmStatus.ForeColor = Color.Green;
                    lblAlarmStatus.Text = "✓ 正常";
                    _audio.Stop();
                }
            };
            _updateTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _monitor?.Stop();
            _audio?.Stop();
            base.OnFormClosing(e);
        }
    }
}
