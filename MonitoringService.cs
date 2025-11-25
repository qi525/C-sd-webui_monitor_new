using System;
using System.IO;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    /// <summary>
    /// 中央监控服务：聚合所有硬件数据，定时发布更新
    /// </summary>
    public class MonitoringService
    {
        private readonly FileMonitor _fileMonitor;
        private readonly SystemMonitor _systemMonitor;
        private readonly AudioPlayer _audioPlayer;
        private bool _isRunning = false; // 【添加】停止标志
        public event Action<MonitoringData> OnDataUpdated;

        public MonitoringService(string initialPath)
        {
            _fileMonitor = new FileMonitor();
            // 【关键改进】设置路径提供器，每次都实时计算路径
            _fileMonitor.SetPathProvider(GetMonitorPath);
            _systemMonitor = new SystemMonitor();
            _audioPlayer = new AudioPlayer(ConfigManager.GetAudioPath());
        }

        public void Start()
        {
            _isRunning = true;
            _fileMonitor.Start();
            
            // 初始化一次数据
            GpuVramHelper.UpdateGpuAsync();
            _systemMonitor.UpdatePhysicalMemoryAsync();
            _systemMonitor.UpdateNetworkSpeedAsync();
            
            // 【改进】主监控循环 - 2秒刷新一次
            _ = Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        // 触发后台异步更新（不等待，立即继续）
                        GpuVramHelper.UpdateGpuAsync();
                        _systemMonitor.UpdatePhysicalMemoryAsync();
                        _systemMonitor.UpdateNetworkSpeedAsync();
                        
                        // 立即获取最新数据发送给UI
                        var data = GetData();
                        OnDataUpdated?.Invoke(data);
                    }
                    catch { }
                    
                    await Task.Delay(2000);
                }
            });
        }

        public void Stop()
        {
            _isRunning = false; // 【关键】停止循环
            _fileMonitor.Stop();
            _audioPlayer?.Stop();
        }

        private string GetMonitorPath()
        {
            string basePath = ConfigManager.GetMonitoringPath();
            string todayFolder = Path.Combine(basePath, DateTime.Now.ToString("yyyy-MM-dd"));
            return Directory.Exists(todayFolder) ? todayFolder : basePath;
        }

        private MonitoringData GetData()
        {
            var (gpuName, usedVramGB, totalVramGB, gpuSuccess) = GpuVramHelper.GetGpuVramInfo();
            var (physTotal, physUsed, physPercent) = _systemMonitor.GetPhysicalMemory();
            var (vmTotal, vmUsed, vmPercent, vmText) = _systemMonitor.GetVirtualMemory();
            var (downloadMBps, uploadMBps) = _systemMonitor.GetNetworkSpeed();
            
            bool isAlarm = _fileMonitor.IsAlarm;
            if (isAlarm) _audioPlayer.Play(); else _audioPlayer.Stop();
            
            return new MonitoringData 
            { 
                DateTime = _systemMonitor.GetCurrentDateTime(),
                GpuName = gpuName,
                GpuVramUsedGB = usedVramGB,
                GpuVramTotalGB = totalVramGB,
                GpuVramPercent = (gpuSuccess && totalVramGB > 0) ? Math.Min((usedVramGB / totalVramGB) * 100, 100) : 0,
                CpuPercent = _systemMonitor.GetCpuUsage(),
                PhysicalMemoryTotal = physTotal,
                PhysicalMemoryUsed = physUsed,
                PhysicalMemoryPercent = physPercent,
                VirtualMemoryTotal = vmTotal,
                VirtualMemoryUsed = vmUsed,
                VirtualMemoryPercent = vmPercent,
                VirtualMemoryText = vmText,
                DownloadMBps = downloadMBps,
                UploadMBps = uploadMBps,
                FileCount = _fileMonitor.FileCount,
                IsAlarm = isAlarm,
                TodayMonitoringPath = _fileMonitor.CurrentPath 
            };
        }
    }

    /// <summary>
    /// 监控数据快照（GPU、CPU、内存、硬盘）
    /// </summary>
    public class MonitoringData
    {
        public string DateTime { get; set; }
        public string GpuName { get; set; }
        public double GpuVramUsedGB { get; set; }
        public double GpuVramTotalGB { get; set; }
        public double GpuVramPercent { get; set; }
        public double CpuPercent { get; set; }
        public double PhysicalMemoryTotal { get; set; }
        public double PhysicalMemoryUsed { get; set; }
        public double PhysicalMemoryPercent { get; set; }
        public double VirtualMemoryTotal { get; set; }
        public double VirtualMemoryUsed { get; set; }
        public double VirtualMemoryPercent { get; set; }
        public string VirtualMemoryText { get; set; }
        public double DownloadMBps { get; set; }
        public double UploadMBps { get; set; }
        public int FileCount { get; set; }
        public bool IsAlarm { get; set; }
        public string TodayMonitoringPath { get; set; }
    }
}
