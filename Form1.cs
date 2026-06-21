using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

namespace WebUIMonitor
{
    public class Form1 : Form
    {
        // ==================== UI 控件 ====================
        private MonitoringService _service;
        private Label lblDateTime, lblGpuName, lblGpuDedicatedUsage, lblGpuSharedUsage;
        private Label lblCpuUsage, lblMemoryUsage, lblVirtualMemoryUsage;
        private Label lblDownloadSpeed, lblUploadSpeed;
        private Label lblStatus, lblMonitorInterval, lblFileCount, lblCountdown;
        private ColoredProgressBar pgbGpuDedicated, pgbGpuShared, pgbCpu, pgbMemory;
        private ColoredProgressBar pgbDownload, pgbUpload, pgbVirtualMemory;
        private DataGridView dgvFolderStatus;

        // ==================== 构造函数 ====================
        public Form1()
        {
            InitializeUI();
            InitializeService();
        }

        // ==================== 初始化 ====================
        private void InitializeUI()
        {
            // 窗口基本设置
            this.Text = "WebUI 文件监控 & 系统监控";
            this.Size = new Size(720, 750);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Arial", 11);

            int y = 15;

            // ===== 第1行：日期时间 =====
            lblDateTime = AddLabel("日期时间: 加载中...", ref y);

            // ===== 第2行：GPU 名称 =====
            lblGpuName = AddLabel("显卡名称: 加载中...", ref y, 10);

            // ===== GPU 显存区块 =====
            lblGpuDedicatedUsage = AddLabel("GPU 专用显存: 加载中...", ref y);
            pgbGpuDedicated = AddColoredProgressBar(ref y);
            lblGpuSharedUsage = AddLabel("GPU 共享显存: 加载中...", ref y);
            pgbGpuShared = AddColoredProgressBar(ref y);

            // ===== CPU / 内存区块 =====
            lblCpuUsage = AddLabel("CPU 占用: 加载中...", ref y);
            pgbCpu = AddColoredProgressBar(ref y);
            lblMemoryUsage = AddLabel("内存占用: 加载中...", ref y);
            pgbMemory = AddColoredProgressBar(ref y);
            lblVirtualMemoryUsage = AddLabel("虚拟内存占用: 加载中...", ref y);
            pgbVirtualMemory = AddColoredProgressBar(ref y);

            // ===== 网络速度区块 =====
            lblDownloadSpeed = AddLabel("下载速度: 0.00 MB/s", ref y);
            pgbDownload = AddColoredProgressBar(ref y);
            lblUploadSpeed = AddLabel("上传速度: 0.00 MB/s", ref y);
            pgbUpload = AddColoredProgressBar(ref y);

            // ===== 分隔线 =====
            Controls.Add(new Label { Location = new Point(15, y), Size = new Size(660, 2), BackColor = Color.Gray });
            y += 10;

            // ===== 文件监控区块 =====
            lblStatus = AddLabel("✓ 正在出图", ref y, 5, Color.Green, FontStyle.Bold, 12);
            lblMonitorInterval = AddLabel("监控间隔: 加载中...", ref y, 5, Color.White, FontStyle.Regular, 10);
            lblFileCount = AddLabel("总文件数: 0", ref y, 5);
            lblCountdown = AddLabel("超时音乐倒计时: --", ref y, 10, Color.Yellow, FontStyle.Bold, 11);

            // ===== DataGridView 表格：监控文件夹列表 =====
            dgvFolderStatus = new DataGridView
            {
                Location = new Point(15, y),
                Size = new Size(660, 150),
                ColumnCount = 2,
                BackgroundColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                RowHeadersVisible = false,
                ColumnHeadersVisible = true,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 30,
                RowTemplate = { Height = 25 }
            };

            // 设置列标题
            dgvFolderStatus.Columns[0].HeaderText = "监控文件夹位置";
            dgvFolderStatus.Columns[0].Name = "Path";
            dgvFolderStatus.Columns[0].Width = 450;
            dgvFolderStatus.Columns[1].HeaderText = "文件数";
            dgvFolderStatus.Columns[1].Name = "FileCount";
            dgvFolderStatus.Columns[1].Width = 150;
            dgvFolderStatus.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvFolderStatus.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // 表头样式
            dgvFolderStatus.EnableHeadersVisualStyles = false;
            dgvFolderStatus.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 60, 60);
            dgvFolderStatus.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvFolderStatus.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            dgvFolderStatus.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 60, 60);

            // 行样式
            dgvFolderStatus.RowsDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
            dgvFolderStatus.RowsDefaultCellStyle.ForeColor = Color.White;
            dgvFolderStatus.RowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(80, 80, 80);
            dgvFolderStatus.RowsDefaultCellStyle.SelectionForeColor = Color.White;
            dgvFolderStatus.RowsDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // 交替行颜色
            dgvFolderStatus.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(38, 38, 38);

            // 边框样式
            dgvFolderStatus.BorderStyle = BorderStyle.FixedSingle;
            dgvFolderStatus.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvFolderStatus.GridColor = Color.Gray;

            Controls.Add(dgvFolderStatus);
        }

        private void InitializeService()
        {
            _service = new MonitoringService(ConfigManager.GetMonitoringPath());
            _service.OnDataUpdated += OnMonitoringDataUpdated;
            _service.Start();
        }

        // ==================== UI 辅助方法 ====================
        private Label AddLabel(string text, ref int y, int spacing = 5, Color? color = null, FontStyle style = FontStyle.Regular, int size = 11)
        {
            var lbl = new Label {
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

        private ColoredProgressBar AddColoredProgressBar(ref int y)
        {
            var pb = new ColoredProgressBar {
                Location = new Point(15, y),
                Size = new Size(660, 20),
                Minimum = 0,
                Maximum = 100,
                BarColor = Color.Green
            };
            Controls.Add(pb);
            y += 25;
            return pb;
        }

        // ==================== 数据更新回调 ====================
        private void OnMonitoringDataUpdated(MonitoringData data)
        {
            if (InvokeRequired) { BeginInvoke(new Action<MonitoringData>(OnMonitoringDataUpdated), data); return; }
            UpdateUIWithData(data);
        }

        // ==================== UI 更新 ====================
        private void UpdateUIWithData(MonitoringData data)
        {
            UpdateDateTimeDisplay(data);
            UpdateGpuDisplay(data);
            UpdateCpuMemoryDisplay(data);
            UpdateNetworkDisplay(data);
            UpdateFileMonitorDisplay(data);
            UpdateStatus(data.IsAlarm);
        }

        /// <summary>更新日期时间显示</summary>
        private void UpdateDateTimeDisplay(MonitoringData data)
        {
            lblDateTime.Text = $"日期时间: {data.DateTime}";
        }

        /// <summary>更新 GPU 显示</summary>
        private void UpdateGpuDisplay(MonitoringData data)
        {
            double dedicatedTotal = GpuVramHelper.GetGpuDedicatedMemoryGB();
            double dedicatedUsed = GpuVramHelper.GetGpuAdapterDedicatedUsedGB();
            double dedicatedPercent = dedicatedTotal > 0 ? (dedicatedUsed / dedicatedTotal * 100) : 0;
            UpdateControl(lblGpuDedicatedUsage, pgbGpuDedicated,
                $"GPU 专用显存: {dedicatedUsed:F2} GB / {dedicatedTotal:F2} GB ({dedicatedPercent:F1}%)", dedicatedPercent);

            lblGpuName.Text = $"显卡名称: {data.GpuName}";

            double sharedTotal = GpuVramHelper.GetGpuSharedMemoryGB();
            double sharedUsed = GpuVramHelper.GetGpuAdapterSharedUsedGB();
            double sharedPercent = sharedTotal > 0 ? (sharedUsed / sharedTotal * 100) : 0;
            UpdateControl(lblGpuSharedUsage, pgbGpuShared,
                $"GPU 共享显存: {sharedUsed:F2} GB / {sharedTotal:F2} GB ({sharedPercent:F1}%)", sharedPercent);
        }

        /// <summary>更新 CPU/内存显示</summary>
        private void UpdateCpuMemoryDisplay(MonitoringData data)
        {
            UpdateControl(lblCpuUsage, pgbCpu,
                $"CPU 占用: {data.CpuPercent:F1}%", data.CpuPercent);
            UpdateControl(lblMemoryUsage, pgbMemory,
                $"内存占用: {data.PhysicalMemoryUsed:F1} GB / {data.PhysicalMemoryTotal:F1} GB ({data.PhysicalMemoryPercent:F1}%)",
                data.PhysicalMemoryPercent);
            UpdateControl(lblVirtualMemoryUsage, pgbVirtualMemory,
                $"虚拟内存占用: {data.VirtualMemoryText}", data.VirtualMemoryPercent);
        }

        /// <summary>更新网络速度显示</summary>
        private void UpdateNetworkDisplay(MonitoringData data)
        {
            double downloadPercent = Math.Min(data.DownloadMBps / 100 * 100, 100);
            double uploadPercent = Math.Min(data.UploadMBps / 100 * 100, 100);
            UpdateControl(lblDownloadSpeed, pgbDownload,
                $"下载速度: {data.DownloadMBps:F2} MB/s", downloadPercent);
            UpdateControl(lblUploadSpeed, pgbUpload,
                $"上传速度: {data.UploadMBps:F2} MB/s", uploadPercent);
        }

        /// <summary>更新文件监控显示</summary>
        private void UpdateFileMonitorDisplay(MonitoringData data)
        {
            lblFileCount.Text = data.TotalFileCount >= 0
                ? $"总文件数: {data.TotalFileCount}"
                : "文件数: 初始化中...";

            lblMonitorInterval.Text = $"监控间隔: {data.MonitorIntervalSeconds} 秒 | 监控文件夹: {data.MonitoredFolderCount} 个";

            // 超时倒计时显示
            string countdown = data.CountdownDisplay;
            if (countdown == "--")
            {
                lblCountdown.Text = "超时音乐倒计时: --";
                lblCountdown.ForeColor = Color.Gray;
            }
            else if (countdown == "触发中")
            {
                lblCountdown.Text = "⚠️ 触发音乐中";
                lblCountdown.ForeColor = Color.Red;
            }
            else
            {
                lblCountdown.Text = $"超时音乐倒计时: {countdown}";
                lblCountdown.ForeColor = Color.Yellow;
            }

            UpdateFolderDataGridView(data);
        }

        /// <summary>更新文件夹 DataGridView 表格</summary>
        private void UpdateFolderDataGridView(MonitoringData data)
        {
            if (data.FolderStates == null) return;

            // 保存当前滚动位置
            int firstDisplayedScrollingRowIndex = dgvFolderStatus.FirstDisplayedScrollingRowIndex;

            // 清除现有数据
            dgvFolderStatus.Rows.Clear();

            // 添加文件夹数据
            foreach (var folder in data.FolderStates)
            {
                bool exists = folder.IsPathExists();
                int rowIndex = dgvFolderStatus.Rows.Add(folder.Path, folder.CurrentFileCount);

                // 设置行样式
                var row = dgvFolderStatus.Rows[rowIndex];

                // 路径列
                row.Cells["Path"].Value = folder.Path;

                // 路径不存在时标红
                if (!exists)
                {
                    row.Cells["Path"].Style.ForeColor = Color.Red;
                }
                else
                {
                    // 检查是否超时（30秒无变化）
                    if (folder.IsTimeoutThresholdReached())
                    {
                        row.Cells["Path"].Style.ForeColor = Color.Orange;
                    }
                    else
                    {
                        row.Cells["Path"].Style.ForeColor = Color.LightGreen;
                    }
                }

                // 文件数列
                row.Cells["FileCount"].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                if (!exists)
                {
                    row.Cells["FileCount"].Style.ForeColor = Color.Red;
                }
                else
                {
                    row.Cells["FileCount"].Style.ForeColor = Color.White;
                }
            }

            // 恢复滚动位置
            if (firstDisplayedScrollingRowIndex >= 0 &&
                firstDisplayedScrollingRowIndex < dgvFolderStatus.Rows.Count)
            {
                dgvFolderStatus.FirstDisplayedScrollingRowIndex = firstDisplayedScrollingRowIndex;
            }
        }

        // ==================== 通用控件更新 ====================
        private void UpdateControl(Label label, ColoredProgressBar bar, string text, double percent)
        {
            label.Text = text;
            bar.Value = (int)percent;
            bar.BarColor = GetProgressBarColor(percent);
        }

        /// <summary>根据百分比获取进度条颜色</summary>
        private Color GetProgressBarColor(double percent) =>
            percent >= 85 ? Color.Red :
            percent >= 50 ? Color.Orange :
            Color.Green;

        /// <summary>更新报警状态</summary>
        private void UpdateStatus(bool isAlarm)
        {
            if (isAlarm)
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

        // ==================== 生命周期 ====================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _service?.Stop();
            base.OnFormClosing(e);
        }
    }
}
