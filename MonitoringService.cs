using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    /// <summary>
    /// 监控服务 - 所有监控逻辑都在这里
    /// Form 只负责 UI 展示，所有业务逻辑都由这个类处理
    /// </summary>
    public class MonitoringService
    {
        private FileMonitor _fileMonitor;
        private SystemMonitor _systemMonitor;
        private AudioPlayer _audioPlayer;
        private MonitoringPathManager _pathManager;

        // 事件：当监控状态变化时触发
        public event Action<MonitoringData> OnDataUpdated;

        // 当前监控的基础路径
        private string _currentBasePath;

        // 缓存的监控数据 - 避免重复查询
        private MonitoringData _cachedData;
        private readonly object _cacheLock = new object();

        public MonitoringService(string initialPath)
        {
            _currentBasePath = initialPath;
            _pathManager = new MonitoringPathManager(initialPath);
            // FileMonitor 使用 PathManager 计算的实际监控路径
            string actualPath = _pathManager.GetActualMonitorPath();
            _fileMonitor = new FileMonitor(actualPath);
            _systemMonitor = new SystemMonitor();
            _audioPlayer = new AudioPlayer(new ConfigManager().GetAudioPath());
            
            // 初始化缓存数据
            _cachedData = new MonitoringData();
        }

        /// <summary>
        /// 启动监控服务
        /// </summary>
        public void Start()
        {
            _fileMonitor.Start();
            
            // 启动后台线程异步更新监控数据
            _ = Task.Run(() => BackgroundDataUpdateLoop());
        }

        /// <summary>
        /// 后台线程循环 - 定期获取数据（避免阻塞UI）
        /// </summary>
        private async Task BackgroundDataUpdateLoop()
        {
            while (_fileMonitor != null)
            {
                try
                {
                    // 异步获取所有数据（不阻塞UI线程）
                    var newData = await GetCurrentDataAsync();
                    
                    lock (_cacheLock)
                    {
                        _cachedData = newData;
                    }
                    
                    // 触发事件通知UI有数据更新
                    OnDataUpdated?.Invoke(newData);
                    
                    // 每500ms更新一次（比UI刷新频率更快，保证数据新鲜）
                    await Task.Delay(500);
                }
                catch
                {
                    await Task.Delay(500);
                }
            }
        }

        /// <summary>
        /// 停止监控服务
        /// </summary>
        public void Stop()
        {
            _fileMonitor.Stop();
            _audioPlayer?.Stop();
        }

        /// <summary>
        /// 更改监控路径
        /// </summary>
        public void SetMonitoringPath(string newBasePath)
        {
            _fileMonitor?.Stop();
            
            _currentBasePath = newBasePath;
            _pathManager.SetBasePath(newBasePath);
            _fileMonitor = new FileMonitor(newBasePath);
            _fileMonitor.Start();
        }

        /// <summary>
        /// 获取当前的监控数据（同步）- 返回缓存的数据，不阻塞
        /// </summary>
        public MonitoringData GetCurrentData()
        {
            lock (_cacheLock)
            {
                return _cachedData ?? new MonitoringData();
            }
        }

        /// <summary>
        /// 异步获取监控数据 - 在后台线程上执行，不阻塞UI
        /// </summary>
        private async Task<MonitoringData> GetCurrentDataAsync()
        {
            // 在线程池线程上执行，避免阻塞UI线程
            return await Task.Run(() =>
            {
                // 每次都重新读取 config.json，这样改了配置立即生效
                var configManager = new ConfigManager();
                string currentPath = configManager.GetMonitoringPath();
                
                // 如果路径改变了，更新 PathManager
                if (currentPath != _currentBasePath)
                {
                    System.Diagnostics.Debug.WriteLine($"[MonitoringService] 路径已改变: {_currentBasePath} -> {currentPath}");
                    _currentBasePath = currentPath;
                    _pathManager.SetBasePath(currentPath);
                    _fileMonitor.SetMonitorPath(currentPath);
                }
                
                var (gpuName, usedVramGB, gpuSuccess) = GpuVramHelper.GetGpuVramInfo();
                var (physTotal, physUsed, physPercent) = _systemMonitor.GetPhysicalMemory();
                var (vmTotal, vmUsed, vmPercent, vmText) = _systemMonitor.GetVirtualMemory();

                double cpuPercent = _systemMonitor.GetCpuUsage();
                double vramPercent = gpuSuccess ? (usedVramGB / 16.0) * 100 : 0;

                bool isAlarm = _fileMonitor.IsAlarm;
                
                // 播放或停止警报音（在后台线程执行，不阻塞UI）
                if (isAlarm)
                {
                    _audioPlayer.Play();
                }
                else
                {
                    _audioPlayer.Stop();
                }

                return new MonitoringData
                {
                    DateTime = _systemMonitor.GetCurrentDateTime(),
                    GpuName = gpuName,
                    GpuVramUsedGB = usedVramGB,
                    GpuVramPercent = vramPercent,
                    CpuPercent = cpuPercent,
                    PhysicalMemoryTotal = physTotal,
                    PhysicalMemoryUsed = physUsed,
                    PhysicalMemoryPercent = physPercent,
                    VirtualMemoryTotal = vmTotal,
                    VirtualMemoryUsed = vmUsed,
                    VirtualMemoryPercent = vmPercent,
                    VirtualMemoryText = vmText,
                    FileCount = _fileMonitor.FileCount,
                    IsAlarm = isAlarm,
                    ConfiguredPath = _currentBasePath,
                    TodayMonitoringPath = _pathManager.GetActualMonitorPath()
                };
            });
        }

        /// <summary>
        /// 获取当前基础路径
        /// </summary>
        public string GetCurrentBasePath() => _currentBasePath;
    }

    /// <summary>
    /// 监控数据 - 包含所有当前监控的信息
    /// </summary>
    public class MonitoringData
    {
        public string DateTime { get; set; }
        
        // GPU
        public string GpuName { get; set; }
        public double GpuVramUsedGB { get; set; }
        public double GpuVramPercent { get; set; }
        
        // CPU
        public double CpuPercent { get; set; }
        
        // 物理内存
        public double PhysicalMemoryTotal { get; set; }
        public double PhysicalMemoryUsed { get; set; }
        public double PhysicalMemoryPercent { get; set; }
        
        // 虚拟内存
        public double VirtualMemoryTotal { get; set; }
        public double VirtualMemoryUsed { get; set; }
        public double VirtualMemoryPercent { get; set; }
        public string VirtualMemoryText { get; set; }
        
        // 文件监控
        public int FileCount { get; set; }
        public bool IsAlarm { get; set; }
        
        // 路径信息
        /// <summary>
        /// config.json 中配置的路径（基础路径）
        /// </summary>
        public string ConfiguredPath { get; set; }
        
        /// <summary>
        /// 今日的完整监控路径（ConfiguredPath + \txt2img-images\yyyy-MM-dd）
        /// </summary>
        public string TodayMonitoringPath { get; set; }
    }
}
