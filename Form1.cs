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
            
            // 从配置文件加载路径
            var configManager = new ConfigManager();
            string initialPath = configManager.GetMonitoringPath();
            
            // 创建服务
            _service = new MonitoringService(initialPath);
            
            // 订阅数据更新事件（异步回调，在UI线程上更新）
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

            int yPos = 15;
            const int labelHeight = 25;
            const int labelWidth = 660;

            // 日期时间
            lblDateTime = CreateLabel("日期时间: 加载中...", 15, yPos, labelWidth);
            yPos += labelHeight + 5;

            // GPU 名称
            lblGpuName = CreateLabel("显卡名称: 加载中...", 15, yPos, labelWidth);
            yPos += labelHeight + 5;

            // GPU 显存使用
            lblGpuVramUsage = CreateLabel("显存占用: 加载中...", 15, yPos, labelWidth);
            yPos += labelHeight + 5;

            // GPU 显存进度条
            pgbGpuVram = new ProgressBar
            {
                Location = new Point(15, yPos),
                Size = new Size(660, 20),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                ForeColor = Color.Green
            };
            this.Controls.Add(pgbGpuVram);
            yPos += 25;

            // CPU 占用
            lblCpuUsage = CreateLabel("CPU 占用: 加载中...", 15, yPos, labelWidth);
            yPos += labelHeight + 5;
            
            pgbCpu = new ProgressBar
            {
                Location = new Point(15, yPos),
                Size = new Size(660, 20),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                ForeColor = Color.Green
            };
            this.Controls.Add(pgbCpu);
            yPos += 25;

            // 物理内存占用
            lblMemoryUsage = CreateLabel("内存占用: 加载中...", 15, yPos, labelWidth);
            yPos += labelHeight + 5;
            
            pgbMemory = new ProgressBar
            {
                Location = new Point(15, yPos),
                Size = new Size(660, 20),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                ForeColor = Color.Green
            };
            this.Controls.Add(pgbMemory);
            yPos += 25;

            // 虚拟内存占用
            lblVirtualMemoryUsage = CreateLabel("虚拟内存占用: 加载中...", 15, yPos, labelWidth);
            yPos += labelHeight + 5;
            
            pgbVirtualMemory = new ProgressBar
            {
                Location = new Point(15, yPos),
                Size = new Size(660, 20),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                ForeColor = Color.Green
            };
            this.Controls.Add(pgbVirtualMemory);
            yPos += 25;

            // 分割线
            var separator = new Label
            {
                Location = new Point(15, yPos),
                Size = new Size(660, 2),
                BackColor = Color.Gray
            };
            this.Controls.Add(separator);
            yPos += 10;

            // 文件数
            lblFileCount = CreateLabel("文件数: 0", 15, yPos, labelWidth);
            yPos += labelHeight + 10;

            // 状态标签
            lblStatus = CreateLabel("✓ 正在出图", 15, yPos, labelWidth, Color.Green, FontStyle.Bold, 12);
            yPos += labelHeight + 5;

            // 监控文件夹位置标签
            lblMonitorPath = CreateLabel("目前监控文件夹位置: 加载中...", 15, yPos, labelWidth, Color.White, FontStyle.Regular, 10);
            yPos += labelHeight + 5;

            // 警报状态
            lblAlarmStatus = CreateLabel("", 15, yPos, labelWidth, Color.Red, FontStyle.Bold, 12);
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

            // 更新监控路径（这是关键！直接从Service获取，不会被覆盖）
            lblMonitorPath.Text = data.DisplayPath;

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
