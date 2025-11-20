using System;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    public class SystemMonitor
    {
        private PerformanceCounter _cpuCounter, _committedBytesCounter, _commitLimitCounter;
        private const long ONE_GB = 1024L * 1024L * 1024L;
        private const long ONE_MBPS = 1024L * 1024L;
        private double _cachedDownloadMbps = 0;
        private double _cachedUploadMbps = 0;
        private readonly object _lockObject = new object();

        public SystemMonitor()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _committedBytesCounter = new PerformanceCounter("Memory", "Committed Bytes", true);
            _commitLimitCounter = new PerformanceCounter("Memory", "Commit Limit", true);
            _cpuCounter.NextValue();
            _committedBytesCounter.NextValue();
            _commitLimitCounter.NextValue();
        }

        public string GetCurrentDateTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public float GetCpuUsage() => _cpuCounter.NextValue();

        public (double totalGB, double usedGB, double percentageUsed) GetPhysicalMemory()
        {
            var availableMB = new PerformanceCounter("Memory", "Available MBytes", true).NextValue();
            var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
            long totalBytes = 0;
            foreach (ManagementObject mo in searcher.Get())
            {
                if (long.TryParse(mo["Capacity"]?.ToString(), out long capacity))
                    totalBytes += capacity;
            }
            if (totalBytes > 0)
            {
                double totalGB = totalBytes / (double)ONE_GB;
                double usedGB = totalGB - availableMB / 1024.0;
                return (totalGB, usedGB, Math.Round(usedGB / totalGB * 100, 1));
            }
            return (32.0, 16.0, 50.0);
        }

        public (double totalGB, double usedGB, double percentageUsed, string text) GetVirtualMemory()
        {
            long committed = (long)(_committedBytesCounter?.NextValue() ?? 0);
            long limit = (long)(_commitLimitCounter?.NextValue() ?? 0);
            double totalGB = limit / (double)ONE_GB;
            double usedGB = committed / (double)ONE_GB;
            double percent = limit > 0 ? Math.Round((double)committed / limit * 100, 1) : 0;
            return (totalGB, usedGB, percent, $"{usedGB:F1} GB / {totalGB:F1} GB ({percent:F1}%)");
        }

        public (double downloadMbps, double uploadMbps) GetNetworkSpeed()
        {
            lock (_lockObject) { return (_cachedDownloadMbps, _cachedUploadMbps); }
        }

        public void UpdateNetworkSpeedCacheAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    // 获取所有网络接口的速度并求和
                    double downloadBytes = GetAllNetworkInterfacesSpeed("Bytes Received/sec");
                    double uploadBytes = GetAllNetworkInterfacesSpeed("Bytes Sent/sec");
                    
                    // 字节/秒 转换为 Mbps
                    double downloadMbps = Math.Max(0, downloadBytes * 8 / ONE_MBPS);
                    double uploadMbps = Math.Max(0, uploadBytes * 8 / ONE_MBPS);
                    
                    lock (_lockObject)
                    {
                        _cachedDownloadMbps = Math.Round(downloadMbps, 2);
                        _cachedUploadMbps = Math.Round(uploadMbps, 2);
                    }
                }
                catch { }
            });
        }

        private double GetAllNetworkInterfacesSpeed(string counterName)
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                string[] instances = category.GetInstanceNames();
                double totalSpeed = 0;
                
                foreach (var instance in instances)
                {
                    try
                    {
                        using (var counter = new PerformanceCounter("Network Interface", counterName, instance))
                        {
                            // 第一次调用初始化，第二次获取实际值
                            counter.NextValue();
                            System.Threading.Thread.Sleep(100);
                            totalSpeed += counter.NextValue();
                        }
                    }
                    catch { }
                }
                
                return totalSpeed;
            }
            catch { return 0; }
        }
    }
}
