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

        // UI 控件
        private Label lblDateTime;
        private Label lblGpuName;
        private Label lblGpuVramUsage;
        private ProgressBar pgbGpuVram;
        private Label lblCpuUsage;
        private ProgressBar pgbCpu;
        private Label lblMemoryUsage;
        private ProgressBar pgbMemory;
        private Label lblVirtualMemoryUsage;
        private ProgressBar pgbVirtualMemory;
        private Label lblFileCount;
        private Label lblStatus;
        private Label lblAlarmStatus;
        private Label lblMonitorPath;

        public Form1()
        {
            InitializeUI();
            _service = new MonitoringService(ConfigManager.GetMonitoringPath());
            _service.OnDataUpdated += OnMonitoringDataUpdated;
            _service.Start();
            StartUpdateTimer();
        }

        /// <summary>
        /// 监控服务数据更新事件 - 在UI线程上安全地更新UI
        /// </summary>
        private void OnMonitoringDataUpdated(MonitoringData data)
        {
            // 确保在UI线程上执行
            if (InvokeRequired)
            {
                BeginInvoke(new Action<MonitoringData>(OnMonitoringDataUpdated), data);
                return;
            }

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
            Controls.Add(new Label { Location = new Point(15, y), Size = new Size(660, 2), BackColor = Color.Gray });
            y += 10;
            lblFileCount = AddLabel("文件数: 0", ref y, 10);
            lblStatus = AddLabel("✓ 正在出图", ref y, 5, Color.Green, FontStyle.Bold, 12);
            lblMonitorPath = AddLabel("目前监控文件夹位置: 加载中...", ref y, 5, Color.White, FontStyle.Regular, 10);
            lblAlarmStatus = AddLabel("", ref y, 0, Color.Red, FontStyle.Bold, 12);
        }

        private Label CreateLabel(string text, int x, int y, int width, 
            Color? foreColor = null, FontStyle style = FontStyle.Regular, int fontSize = 11)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 25),
                AutoSize = false,
                Font = new Font("Arial", fontSize, style),
                ForeColor = foreColor ?? Color.White
            };
            this.Controls.Add(label);
            return label;
        }

        private Label AddLabel(string text, ref int y, int spacing = 5, Color? color = null, FontStyle style = FontStyle.Regular, int size = 11)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(15, y),
                Size = new Size(660, 25),
                AutoSize = false,
                Font = new Font("Arial", size, style),
                ForeColor = color ?? Color.White
            };
            Controls.Add(lbl);
            y += 25 + spacing;
            return lbl;
        }

        private ProgressBar AddProgressBar(ref int y)
        {
            var pb = new ProgressBar
            {
                Location = new Point(15, y),
                Size = new Size(660, 20),
                Minimum = 0,
                Maximum = 100,
                ForeColor = Color.Green
            };
            Controls.Add(pb);
            y += 25;
            return pb;
        }

        private void StartUpdateTimer()
        {
            _updateTimer = new Timer { Interval = 1000 };
            // Timer不再调用GetCurrentData，而是等待后台线程推送数据
            // 这样避免了UI线程的阻塞
            _updateTimer.Start();
        }

        /// <summary>
        /// 从服务获取数据并更新UI - 这就是唯一的UI更新入口
        /// </summary>
        private void UpdateUIWithData(MonitoringData data)
        {
            // 更新日期时间
            lblDateTime.Text = $"日期时间: {data.DateTime}";

            // 更新 GPU
            lblGpuName.Text = $"显卡名称: {data.GpuName}";
            lblGpuVramUsage.Text = $"显存占用: {data.GpuVramUsedGB:F1} GB / 16.0 GB ({data.GpuVramPercent:F1}%)";
            pgbGpuVram.Value = (int)data.GpuVramPercent;
            pgbGpuVram.ForeColor = GetProgressBarColor(data.GpuVramPercent);

            // 更新 CPU
            lblCpuUsage.Text = $"CPU 占用: {data.CpuPercent:F1}%";
            pgbCpu.Value = (int)data.CpuPercent;
            pgbCpu.ForeColor = GetProgressBarColor(data.CpuPercent);

            // 更新物理内存
            lblMemoryUsage.Text = $"内存占用: {data.PhysicalMemoryUsed:F1} GB / {data.PhysicalMemoryTotal:F1} GB ({data.PhysicalMemoryPercent:F1}%)";
            pgbMemory.Value = (int)data.PhysicalMemoryPercent;
            pgbMemory.ForeColor = GetProgressBarColor(data.PhysicalMemoryPercent);

            // 更新虚拟内存
            lblVirtualMemoryUsage.Text = $"虚拟内存占用: {data.VirtualMemoryText}";
            pgbVirtualMemory.Value = (int)data.VirtualMemoryPercent;
            pgbVirtualMemory.ForeColor = GetProgressBarColor(data.VirtualMemoryPercent);

            // 更新文件数
            string fileCountText = data.FileCount >= 0 ? $"文件数: {data.FileCount}" : "文件数: 初始化中...";
            lblFileCount.Text = fileCountText;

            // 更新监控路径 - 显示今日的实际监控路径
            lblMonitorPath.Text = $"目前监控文件夹位置: {data.TodayMonitoringPath}";

            // 更新状态
            if (data.IsAlarm)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "⚠️ 已停止";
            }
            else
            {
                lblStatus.ForeColor = Color.Green;
                lblStatus.Text = "✓ 正在出图";
            }
        }

        private Color GetProgressBarColor(double percent)
        {
            if (percent >= 75)
                return Color.Red;
            else if (percent >= 50)
                return Color.Orange;
            else
                return Color.Green;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _service?.Stop();
            base.OnFormClosing(e);
        }
    }
}
