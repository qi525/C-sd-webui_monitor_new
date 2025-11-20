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

        private Label lblDateTime, lblGpuName, lblGpuDedicatedUsage, lblGpuSharedUsage, lblCpuUsage, lblMemoryUsage, lblVirtualMemoryUsage, lblDownloadSpeed, lblUploadSpeed, lblFileCount, lblStatus, lblMonitorPath;
        private ColoredProgressBar pgbGpuDedicated, pgbGpuShared, pgbCpu, pgbMemory, pgbDownload, pgbUpload, pgbVirtualMemory;

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
            this.Size = new Size(720, 700);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Arial", 11);

            int y = 15;
            lblDateTime = AddLabel("日期时间: 加载中...", ref y);
            lblGpuName = AddLabel("显卡名称: 加载中...", ref y, 10);
            
            lblGpuDedicatedUsage = AddLabel("GPU 专用显存: 加载中...", ref y);
            pgbGpuDedicated = AddColoredProgressBar(ref y);
            lblGpuSharedUsage = AddLabel("GPU 共享显存: 加载中...", ref y);
            pgbGpuShared = AddColoredProgressBar(ref y);
            ///Controls.Add(new Label { Location = new Point(15, y), Size = new Size(660, 2), BackColor = Color.Gray }); y += 10;
            
            lblCpuUsage = AddLabel("CPU 占用: 加载中...", ref y);
            pgbCpu = AddColoredProgressBar(ref y);
            lblMemoryUsage = AddLabel("内存占用: 加载中...", ref y);
            pgbMemory = AddColoredProgressBar(ref y);
            lblVirtualMemoryUsage = AddLabel("虚拟内存占用: 加载中...", ref y);
            pgbVirtualMemory = AddColoredProgressBar(ref y);
            ///Controls.Add(new Label { Location = new Point(15, y), Size = new Size(660, 2), BackColor = Color.Gray }); y += 10;
            
            lblDownloadSpeed = AddLabel("下载速度: 0.00 Mbps", ref y);
            pgbDownload = AddColoredProgressBar(ref y);
            lblUploadSpeed = AddLabel("上传速度: 0.00 Mbps", ref y);
            pgbUpload = AddColoredProgressBar(ref y);
            Controls.Add(new Label { Location = new Point(15, y), Size = new Size(660, 2), BackColor = Color.Gray }); y += 10;
            
            lblStatus = AddLabel("✓ 正在出图", ref y, 5, Color.Green, FontStyle.Bold, 12);
            lblMonitorPath = AddLabel("目前监控文件夹位置: 加载中...", ref y, 5, Color.White, FontStyle.Regular, 10);
            lblFileCount = AddLabel("文件数: 0", ref y, 10);
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

        private ColoredProgressBar AddColoredProgressBar(ref int y)
        {
            var pb = new ColoredProgressBar { Location = new Point(15, y), Size = new Size(660, 20), Minimum = 0, Maximum = 100, BarColor = Color.Green };
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
            lblDateTime.Text = $"日期时间: {data.DateTime}";
            lblGpuName.Text = $"显卡名称: {data.GpuName}";
            
            // GPU 显存 - 使用 Counter #4 和 #8
            double dedicatedTotal = GpuVramHelper.GetGpuDedicatedMemoryGB();
            double dedicatedUsed = GpuVramHelper.GetGpuAdapterDedicatedUsedGB();
            double dedicatedPercent = dedicatedTotal > 0 ? (dedicatedUsed / dedicatedTotal * 100) : 0;
            UpdateControl(lblGpuDedicatedUsage, pgbGpuDedicated, $"GPU 专用显存: {dedicatedUsed:F2} GB / {dedicatedTotal:F2} GB ({dedicatedPercent:F1}%)", dedicatedPercent);
            
            double sharedTotal = GpuVramHelper.GetGpuSharedMemoryGB();
            double sharedUsed = GpuVramHelper.GetGpuAdapterSharedUsedGB();
            double sharedPercent = sharedTotal > 0 ? (sharedUsed / sharedTotal * 100) : 0;
            UpdateControl(lblGpuSharedUsage, pgbGpuShared, $"GPU 共享显存: {sharedUsed:F2} GB / {sharedTotal:F2} GB ({sharedPercent:F1}%)", sharedPercent);
            
            UpdateControl(lblCpuUsage, pgbCpu, $"CPU 占用: {data.CpuPercent:F1}%", data.CpuPercent);
            UpdateControl(lblMemoryUsage, pgbMemory, $"内存占用: {data.PhysicalMemoryUsed:F1} GB / {data.PhysicalMemoryTotal:F1} GB ({data.PhysicalMemoryPercent:F1}%)", data.PhysicalMemoryPercent);
            UpdateControl(lblVirtualMemoryUsage, pgbVirtualMemory, $"虚拟内存占用: {data.VirtualMemoryText}", data.VirtualMemoryPercent);
            
            // 网络速度（进度条范围0-100Mbps）
            double downloadPercent = Math.Min(data.DownloadMbps / 100 * 100, 100);
            double uploadPercent = Math.Min(data.UploadMbps / 100 * 100, 100);
            UpdateControl(lblDownloadSpeed, pgbDownload, $"下载速度: {data.DownloadMbps:F2} Mbps", downloadPercent);
            UpdateControl(lblUploadSpeed, pgbUpload, $"上传速度: {data.UploadMbps:F2} Mbps", uploadPercent);
            
            lblFileCount.Text = data.FileCount >= 0 ? $"文件数: {data.FileCount}" : "文件数: 初始化中...";
            lblMonitorPath.Text = $"目前监控文件夹位置: {data.TodayMonitoringPath}";
            
            UpdateStatus(data.IsAlarm);
        }

        private void UpdateControl(Label label, ColoredProgressBar bar, string text, double percent)
        {
            label.Text = text;
            bar.Value = (int)percent;
            bar.BarColor = GetProgressBarColor(percent);
        }

        private void UpdateStatus(bool isAlarm)
        {
            if (isAlarm) { lblStatus.ForeColor = Color.Red; lblStatus.Text = "⚠️ 已停止"; }
            else { lblStatus.ForeColor = Color.Green; lblStatus.Text = "✓ 正在出图"; }
        }

        private Color GetProgressBarColor(double percent) => percent >= 85 ? Color.Red : percent >= 50 ? Color.Orange : Color.Green;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _service?.Stop();
            base.OnFormClosing(e);
        }
    }
}
