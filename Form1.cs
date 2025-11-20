using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace WebUIMonitor
{
    public class Form1 : Form
    {
        private MonitoringService _service;
        private Timer _updateTimer;

        private Label lblDateTime, lblGpuName, lblGpuVramUsage, lblCpuUsage, lblMemoryUsage, lblVirtualMemoryUsage, lblFileCount, lblStatus, lblMonitorPath;
        private ProgressBar pgbGpuVram, pgbCpu, pgbMemory, pgbVirtualMemory;

        public Form1()
        {
            InitializeUI();
            _service = new MonitoringService(ConfigManager.GetMonitoringPath());
            _service.OnDataUpdated += OnMonitoringDataUpdated;
            _service.Start();
            StartUpdateTimer();
        }

        private void OnMonitoringDataUpdated(MonitoringData data)
        {
            if (InvokeRequired) { BeginInvoke(new Action<MonitoringData>(OnMonitoringDataUpdated), data); return; }
            UpdateUIWithData(data);
        }

        private void InitializeUI()
        {
            this.Text = "WebUI 文件监控 & 系统监控";
            this.Size = new Size(720, 500);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Arial", 11);

            int y = 15;
            lblDateTime = AddLabel("日期时间: 加载中...", ref y);
            lblGpuName = AddLabel("显卡名称: 加载中...", ref y);
            lblGpuVramUsage = AddLabel("显存占用: 加载中...", ref y);
            pgbGpuVram = AddProgressBar(ref y);
            lblCpuUsage = AddLabel("CPU 占用: 加载中...", ref y);
            pgbCpu = AddProgressBar(ref y);
            lblMemoryUsage = AddLabel("内存占用: 加载中...", ref y);
            pgbMemory = AddProgressBar(ref y);
            lblVirtualMemoryUsage = AddLabel("虚拟内存占用: 加载中...", ref y);
            pgbVirtualMemory = AddProgressBar(ref y);
            Controls.Add(new Label { Location = new Point(15, y), Size = new Size(660, 2), BackColor = Color.Gray }); y += 10;
            lblFileCount = AddLabel("文件数: 0", ref y, 10);
            lblStatus = AddLabel("✓ 正在出图", ref y, 5, Color.Green, FontStyle.Bold, 12);
            lblMonitorPath = AddLabel("目前监控文件夹位置: 加载中...", ref y, 5, Color.White, FontStyle.Regular, 10);
        }

        private Label AddLabel(string text, ref int y, int spacing = 5, Color? color = null, FontStyle style = FontStyle.Regular, int size = 11)
        {
            var lbl = new Label { Text = text, Location = new Point(15, y), Size = new Size(660, 25), AutoSize = false, Font = new Font("Arial", size, style), ForeColor = color ?? Color.White };
            Controls.Add(lbl);
            y += 25 + spacing;
            return lbl;
        }

        private ProgressBar AddProgressBar(ref int y)
        {
            var pb = new ProgressBar { Location = new Point(15, y), Size = new Size(660, 20), Minimum = 0, Maximum = 100, ForeColor = Color.Green };
            Controls.Add(pb);
            y += 25;
            return pb;
        }

        private void StartUpdateTimer()
        {
            _updateTimer = new Timer { Interval = 1000 };
            _updateTimer.Start();
        }

        private void UpdateUIWithData(MonitoringData data)
        {
            // 更新日期和显卡
            lblDateTime.Text = $"日期时间: {data.DateTime}";
            lblGpuName.Text = $"显卡名称: {data.GpuName}";
            UpdateControl(lblGpuVramUsage, pgbGpuVram, $"显存占用: {data.GpuVramUsedGB:F1} GB / {data.GpuVramTotalGB:F1} GB ({data.GpuVramPercent:F1}%)", data.GpuVramPercent);
            
            // 更新 CPU 和内存
            UpdateControl(lblCpuUsage, pgbCpu, $"CPU 占用: {data.CpuPercent:F1}%", data.CpuPercent);
            UpdateControl(lblMemoryUsage, pgbMemory, $"内存占用: {data.PhysicalMemoryUsed:F1} GB / {data.PhysicalMemoryTotal:F1} GB ({data.PhysicalMemoryPercent:F1}%)", data.PhysicalMemoryPercent);
            UpdateControl(lblVirtualMemoryUsage, pgbVirtualMemory, $"虚拟内存占用: {data.VirtualMemoryText}", data.VirtualMemoryPercent);
            
            // 更新文件和路径
            lblFileCount.Text = data.FileCount >= 0 ? $"文件数: {data.FileCount}" : "文件数: 初始化中...";
            lblMonitorPath.Text = $"目前监控文件夹位置: {data.TodayMonitoringPath}";
            
            // 更新状态指示
            UpdateStatus(data.IsAlarm);
        }

        private void UpdateControl(Label label, ProgressBar bar, string text, double percent)
        {
            label.Text = text;
            bar.Value = (int)percent;
            bar.ForeColor = GetProgressBarColor(percent);
        }

        private void UpdateStatus(bool isAlarm)
        {
            if (isAlarm) { lblStatus.ForeColor = Color.Red; lblStatus.Text = "⚠️ 已停止"; }
            else { lblStatus.ForeColor = Color.Green; lblStatus.Text = "✓ 正在出图"; }
        }

        private Color GetProgressBarColor(double percent) => percent >= 75 ? Color.Red : percent >= 50 ? Color.Orange : Color.Green;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _service?.Stop();
            base.OnFormClosing(e);
        }
    }
}
