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
        private string _lastMonitorDate = "";  // 追踪上一次的监控日期（yyyy-MM-dd），用于自动检测午夜日期变化
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
            
            // 【智能回退逻辑】
            // 1. 优先返回当前日期的子文件夹（适配Stable Diffusion默认结构）
            // 2. 如果不存在，回退到配置的基础路径（适配没有日期子文件夹的用户）
            // 3. 确保日期变化时UI能正确显示，同时不丢弃没有日期子文件夹的用户
            return Directory.Exists(todayFolder) ? todayFolder : basePath;
        }

        private MonitoringData GetData()
        {
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string path = GetMonitorPath();
            
            // 【核心修复】基于日期字符串（yyyy-MM-dd）检测日期变化，而不是路径
            // 保证100%捕获午夜日期变化，即使路径相同也会重置
            if (currentDate != _lastMonitorDate)
            {
                _fileMonitor.Reset();  // 重置文件监控状态
                _lastMonitorDate = currentDate;  // 更新为当前日期
            }
            
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
