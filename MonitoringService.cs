using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace WebUIMonitor
{
    /// <summary>
    /// 中央监控服务：聚合所有硬件数据，定时发布更新
    /// </summary>
    public class MonitoringService
    {
        // ==================== 私有字段 ====================
        private readonly FileMonitor _fileMonitor;
        private readonly SystemMonitor _systemMonitor;
        private readonly AudioPlayer _audioPlayer;
        private bool _isRunning = false;
        private int _updateIntervalSeconds = 2;

        // ==================== 事件 ====================
        public event Action<MonitoringData> OnDataUpdated;

        // ==================== 构造函数 ====================
        public MonitoringService(string initialPath)
        {
            _fileMonitor = new FileMonitor();
            _systemMonitor = new SystemMonitor();
            _audioPlayer = new AudioPlayer(ConfigManager.GetAudioPath());
            
            InitializeFileMonitor();
        }

        // ==================== 初始化方法 ====================
        private void InitializeFileMonitor()
        {
            // 设置多路径提供器（每次实时获取）
            _fileMonitor.SetPathProvider(GetMonitorPaths);
            _fileMonitor.SetIntervalSeconds(ConfigManager.GetMonitoringIntervalSeconds());
        }

        /// <summary>获取监控路径列表（支持日期子文件夹自动切换）</summary>
        private List<string> GetMonitorPaths()
        {
            var paths = new List<string>();
            
            // 优先使用多路径配置
            var multiPaths = ConfigManager.GetMonitoringPaths();
            if (multiPaths != null && multiPaths.Count > 0)
            {
                foreach (var basePath in multiPaths)
                {
                    var resolvedPath = ResolveDateFolder(basePath);
                    if (!string.IsNullOrEmpty(resolvedPath))
                    {
                        paths.Add(resolvedPath);
                    }
                }
            }
            // 兼容单路径配置
            else
            {
                var singlePath = ConfigManager.GetMonitoringPath();
                var resolvedPath = ResolveDateFolder(singlePath);
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    paths.Add(resolvedPath);
                }
            }
            
            return paths;
        }

        /// <summary>解析日期子文件夹路径</summary>
        private string ResolveDateFolder(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) return "";
            string todayFolder = Path.Combine(basePath, DateTime.Now.ToString("yyyy-MM-dd"));
            return Directory.Exists(todayFolder) ? todayFolder : basePath;
        }

        // ==================== 生命周期方法 ====================
        public void Start()
        {
            _isRunning = true;
            _fileMonitor.Start();
            
            InitializeHardwareMonitoring();
            StartMonitoringLoop();
            
            LogInfo("监控服务已启动");
        }

        public void Stop()
        {
            _isRunning = false;
            _fileMonitor.Stop();
            _audioPlayer?.Stop();
            LogInfo("监控服务已停止");
        }

        // ==================== 硬件监控初始化 ====================
        private void InitializeHardwareMonitoring()
        {
            GpuVramHelper.UpdateGpuAsync();
            _systemMonitor.UpdatePhysicalMemoryAsync();
            _systemMonitor.UpdateNetworkSpeedAsync();
        }

        // ==================== 主监控循环 ====================
        private void StartMonitoringLoop()
        {
            _ = Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        await UpdateHardwareDataAsync();
                        var data = CollectMonitoringData();
                        OnDataUpdated?.Invoke(data);
                    }
                    catch (Exception ex)
                    {
                        LogError($"监控循环异常: {ex.Message}");
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(_updateIntervalSeconds));
                }
            });
        }

        /// <summary>异步更新硬件数据</summary>
        private Task UpdateHardwareDataAsync()
        {
            return Task.Run(() =>
            {
                GpuVramHelper.UpdateGpuAsync();
                _systemMonitor.UpdatePhysicalMemoryAsync();
                _systemMonitor.UpdateNetworkSpeedAsync();
            });
        }

        // ==================== 数据收集 ====================
        private MonitoringData CollectMonitoringData()
        {
            var gpuInfo = GpuVramHelper.GetGpuVramInfo();
            var memoryInfo = _systemMonitor.GetPhysicalMemory();
            var virtualInfo = _systemMonitor.GetVirtualMemory();
            var networkInfo = _systemMonitor.GetNetworkSpeed();
            
            bool isAlarm = _fileMonitor.IsAlarm;
            HandleAlarm(isAlarm);
            
            return new MonitoringData 
            { 
                // 日期时间
                DateTime = _systemMonitor.GetCurrentDateTime(),
                
                // GPU 信息
                GpuName = gpuInfo.gpuName,
                GpuVramUsedGB = gpuInfo.usedVramGB,
                GpuVramTotalGB = gpuInfo.totalVramGB,
                GpuVramPercent = CalculateGpuPercent(gpuInfo),
                
                // CPU
                CpuPercent = _systemMonitor.GetCpuUsage(),
                
                // 物理内存
                PhysicalMemoryTotal = memoryInfo.totalGB,
                PhysicalMemoryUsed = memoryInfo.usedGB,
                PhysicalMemoryPercent = memoryInfo.percentageUsed,
                
                // 虚拟内存
                VirtualMemoryTotal = virtualInfo.totalGB,
                VirtualMemoryUsed = virtualInfo.usedGB,
                VirtualMemoryPercent = virtualInfo.percentageUsed,
                VirtualMemoryText = virtualInfo.text,
                
                // 网络
                DownloadMBps = networkInfo.downloadMBps,
                UploadMBps = networkInfo.uploadMBps,
                
                // 文件监控
                TotalFileCount = _fileMonitor.TotalFileCount,
                IsAlarm = isAlarm,
                MonitoredFolderCount = _fileMonitor.GetMonitoredFolderCount(),
                MonitorIntervalSeconds = _fileMonitor.IntervalSeconds,
                SecondsSinceLastChange = _fileMonitor.SecondsSinceLastChange,
                CountdownDisplay = _fileMonitor.GetCountdownDisplay(),
                FolderStates = _fileMonitor.GetAllFolderStates()
            };
        }

        /// <summary>计算 GPU 使用百分比</summary>
        private double CalculateGpuPercent((string gpuName, double usedVramGB, double totalVramGB, bool success) gpuInfo)
        {
            return gpuInfo.success && gpuInfo.totalVramGB > 0 
                ? Math.Min((gpuInfo.usedVramGB / gpuInfo.totalVramGB) * 100, 100) 
                : 0;
        }

        /// <summary>处理报警状态</summary>
        private void HandleAlarm(bool isAlarm)
        {
            if (isAlarm) _audioPlayer.Play(); else _audioPlayer.Stop();
        }

        // ==================== 只读属性 ====================
        /// <summary>获取文件监控器实例</summary>
        public FileMonitor GetFileMonitor() => _fileMonitor;
        
        /// <summary>获取系统监控器实例</summary>
        public SystemMonitor GetSystemMonitor() => _systemMonitor;
        
        /// <summary>获取更新间隔（秒）</summary>
        public int GetUpdateIntervalSeconds() => _updateIntervalSeconds;
        
        /// <summary>服务是否正在运行</summary>
        public bool IsRunning() => _isRunning;

        // ==================== 日志辅助 ====================
        private void LogInfo(string message) => System.Diagnostics.Debug.WriteLine($"[MonitoringService] {message}");
        private void LogError(string message) => System.Diagnostics.Debug.WriteLine($"[MonitoringService] ❌ {message}");
    }

    /// <summary>
    /// 监控数据快照（GPU、CPU、内存、硬盘、文件监控）
    /// </summary>
    public class MonitoringData
    {
        // ==================== 日期时间 ====================
        public string DateTime { get; set; }
        
        // ==================== GPU ====================
        public string GpuName { get; set; }
        public double GpuVramUsedGB { get; set; }
        public double GpuVramTotalGB { get; set; }
        public double GpuVramPercent { get; set; }
        
        // ==================== CPU ====================
        public double CpuPercent { get; set; }
        
        // ==================== 物理内存 ====================
        public double PhysicalMemoryTotal { get; set; }
        public double PhysicalMemoryUsed { get; set; }
        public double PhysicalMemoryPercent { get; set; }
        
        // ==================== 虚拟内存 ====================
        public double VirtualMemoryTotal { get; set; }
        public double VirtualMemoryUsed { get; set; }
        public double VirtualMemoryPercent { get; set; }
        public string VirtualMemoryText { get; set; }
        
        // ==================== 网络 ====================
        public double DownloadMBps { get; set; }
        public double UploadMBps { get; set; }
        
        // ==================== 文件监控 ====================
        public int TotalFileCount { get; set; }
        public bool IsAlarm { get; set; }
        public int MonitoredFolderCount { get; set; }
        public int MonitorIntervalSeconds { get; set; }
        public int SecondsSinceLastChange { get; set; }
        public string CountdownDisplay { get; set; } = "--";
        public List<FolderMonitorState> FolderStates { get; set; } = new List<FolderMonitorState>();
        
        // ==================== 辅助方法 ====================
        /// <summary>获取文件监控状态摘要</summary>
        public string GetFileMonitorSummary()
        {
            return $"共 {TotalFileCount} 个文件，监控 {MonitoredFolderCount} 个文件夹，已运行 {SecondsSinceLastChange} 秒";
        }
        
        /// <summary>获取所有监控文件夹的详细信息</summary>
        public string GetFolderDetailsSummary()
        {
            if (FolderStates == null || FolderStates.Count == 0)
                return "暂无监控数据";
            
            var lines = new List<string>();
            foreach (var folder in FolderStates)
            {
                string status = folder.IsPathExists() ? "✓" : "✗";
                lines.Add($"  {status} {folder.Path}: {folder.CurrentFileCount} 个文件 ({folder.GetSecondsSinceLastChange()}秒前更新)");
            }
            return string.Join("\n", lines);
        }
    }
}
