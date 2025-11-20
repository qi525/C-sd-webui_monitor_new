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
        public event Action<MonitoringData> OnDataUpdated;

        public MonitoringService(string initialPath)
        {
            _fileMonitor = new FileMonitor();
            _systemMonitor = new SystemMonitor();
            _audioPlayer = new AudioPlayer(ConfigManager.GetAudioPath());
        }

        public void Start()
        {
            _fileMonitor.Start();
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    GpuVramHelper.UpdateGpuMemoryCacheAsync(); // 后台更新GPU缓存（非阻塞）
                    _systemMonitor.UpdateNetworkSpeedCacheAsync(); // 后台更新网络速度缓存（非阻塞）
                    OnDataUpdated?.Invoke(GetData());
                    await Task.Delay(500);
                }
            });
        }

        public void Stop()
        {
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
            string path = GetMonitorPath();
            _fileMonitor.SetPath(path);
            
            var (gpuName, usedVramGB, totalVramGB, gpuSuccess) = GpuVramHelper.GetGpuVramInfo();
            var (physTotal, physUsed, physPercent) = _systemMonitor.GetPhysicalMemory();
            var (vmTotal, vmUsed, vmPercent, vmText) = _systemMonitor.GetVirtualMemory();
            var (downloadMbps, uploadMbps) = _systemMonitor.GetNetworkSpeed();
            
            bool isAlarm = _fileMonitor.IsAlarm;
            if (isAlarm) _audioPlayer.Play(); else _audioPlayer.Stop();
            
            return new MonitoringData 
            { 
                DateTime = _systemMonitor.GetCurrentDateTime(),
                GpuName = GpuVramHelper.GetGpuName(),
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
                DownloadMbps = downloadMbps,
                UploadMbps = uploadMbps,
                FileCount = _fileMonitor.FileCount,
                IsAlarm = isAlarm,
                TodayMonitoringPath = path 
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
        public double DownloadMbps { get; set; }
        public double UploadMbps { get; set; }
        public int FileCount { get; set; }
        public bool IsAlarm { get; set; }
        public string TodayMonitoringPath { get; set; }
    }
}
