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
        
        // 缓存，避免重复查询
        private double _cachedPhysicalMemoryTotal = 0;
        private double _cachedPhysicalMemoryUsed = 0;
        private double _cachedPhysicalMemoryPercent = 0;
        private double _cachedDownloadMBps = 0;
        private double _cachedUploadMBps = 0;

        public SystemMonitor()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _committedBytesCounter = new PerformanceCounter("Memory", "Committed Bytes", true);
            _commitLimitCounter = new PerformanceCounter("Memory", "Commit Limit", true);
            _cpuCounter.NextValue();
            _committedBytesCounter.NextValue();
            _commitLimitCounter.NextValue();
            
            // 初始化缓存
            UpdateCacheAsync();
        }
        
        /// <summary>后台异步更新缓存（避免UI阻塞）</summary>
        public void UpdateCacheAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    // 物理内存
                    var availableMB = new PerformanceCounter("Memory", "Available MBytes", true).NextValue();
                    using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                    {
                        long totalBytes = 0;
                        foreach (ManagementObject mo in searcher.Get())
                        {
                            if (long.TryParse(mo["Capacity"]?.ToString(), out long capacity))
                                totalBytes += capacity;
                        }
                        _cachedPhysicalMemoryTotal = totalBytes > 0 ? totalBytes / (double)ONE_GB : 32.0;
                        _cachedPhysicalMemoryUsed = _cachedPhysicalMemoryTotal - availableMB / 1024.0;
                        _cachedPhysicalMemoryPercent = Math.Round(_cachedPhysicalMemoryUsed / _cachedPhysicalMemoryTotal * 100, 1);
                    }
                    
                    // 网络速度
                    double downloadBytes = GetAllNetworkInterfacesSpeed("Bytes Received/sec");
                    double uploadBytes = GetAllNetworkInterfacesSpeed("Bytes Sent/sec");
                    _cachedDownloadMBps = Math.Max(0, downloadBytes / (1024.0 * 1024.0));
                    _cachedUploadMBps = Math.Max(0, uploadBytes / (1024.0 * 1024.0));
                }
                catch { }
            });
        }

        public string GetCurrentDateTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public float GetCpuUsage() => (float)_cpuCounter.NextValue();

        public (double totalGB, double usedGB, double percentageUsed) GetPhysicalMemory()
        {
            // 直接返回缓存值，不阻塞
            return (_cachedPhysicalMemoryTotal, _cachedPhysicalMemoryUsed, _cachedPhysicalMemoryPercent);
        }

        public (double totalGB, double usedGB, double percentageUsed, string text) GetVirtualMemory()
        {
            try
            {
                long committed = (long)(_committedBytesCounter?.NextValue() ?? 0);
                long limit = (long)(_commitLimitCounter?.NextValue() ?? 0);
                double vmTotal = limit / (double)ONE_GB;
                double vmUsed = committed / (double)ONE_GB;
                double vmPercent = limit > 0 ? Math.Round((double)committed / limit * 100, 1) : 0;
                string vmText = $"{vmUsed:F1} GB / {vmTotal:F1} GB ({vmPercent:F1}%)";
                return (vmTotal, vmUsed, vmPercent, vmText);
            }
            catch { return (0, 0, 0, "0 GB / 0 GB (0%)"); }
        }

        public (double downloadMBps, double uploadMBps) GetNetworkSpeed()
        {
            // 直接返回缓存值，不阻塞
            return (_cachedDownloadMBps, _cachedUploadMBps);
        }

        /// <summary>获取网络速度（无阻塞）</summary>
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
                            // 仅读取一次当前值，无需 Sleep 等待
                            counter.NextValue();
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
