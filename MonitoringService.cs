using System;
using System.IO;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    public class MonitoringService
    {
        private FileMonitor _fileMonitor;
        private SystemMonitor _systemMonitor;
        private AudioPlayer _audioPlayer;
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
                    var data = GetData();
                    OnDataUpdated?.Invoke(data);
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

            var (gpuName, usedVramGB, gpuSuccess) = GpuVramHelper.GetGpuVramInfo();
            var (physTotal, physUsed, physPercent) = _systemMonitor.GetPhysicalMemory();
            var (vmTotal, vmUsed, vmPercent, vmText) = _systemMonitor.GetVirtualMemory();

            bool isAlarm = _fileMonitor.IsAlarm;
            if (isAlarm) _audioPlayer.Play(); else _audioPlayer.Stop();

            return new MonitoringData
            {
                DateTime = _systemMonitor.GetCurrentDateTime(),
                GpuName = gpuName,
                GpuVramUsedGB = usedVramGB,
                GpuVramPercent = gpuSuccess ? (usedVramGB / 16.0) * 100 : 0,
                CpuPercent = _systemMonitor.GetCpuUsage(),
                PhysicalMemoryTotal = physTotal,
                PhysicalMemoryUsed = physUsed,
                PhysicalMemoryPercent = physPercent,
                VirtualMemoryTotal = vmTotal,
                VirtualMemoryUsed = vmUsed,
                VirtualMemoryPercent = vmPercent,
                VirtualMemoryText = vmText,
                FileCount = _fileMonitor.FileCount,
                IsAlarm = isAlarm,
                TodayMonitoringPath = path
            };
        }
    }

    public class MonitoringData
    {
        public string DateTime { get; set; }
        public string GpuName { get; set; }
        public double GpuVramUsedGB { get; set; }
        public double GpuVramPercent { get; set; }
        public double CpuPercent { get; set; }
        public double PhysicalMemoryTotal { get; set; }
        public double PhysicalMemoryUsed { get; set; }
        public double PhysicalMemoryPercent { get; set; }
        public double VirtualMemoryTotal { get; set; }
        public double VirtualMemoryUsed { get; set; }
        public double VirtualMemoryPercent { get; set; }
        public string VirtualMemoryText { get; set; }
        public int FileCount { get; set; }
        public bool IsAlarm { get; set; }
        public string TodayMonitoringPath { get; set; }
    }
}
