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
        private string _cachedDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        private double _cachedDownloadMbps = 0;
        private double _cachedUploadMbps = 0;
        private double _cachedCpuUsage = 0;
        private double _cachedPhysicalMemoryTotal = 0;
        private double _cachedPhysicalMemoryUsed = 0;
        private double _cachedPhysicalMemoryPercent = 0;
        private double _cachedVirtualMemoryTotal = 0;
        private double _cachedVirtualMemoryUsed = 0;
        private double _cachedVirtualMemoryPercent = 0;
        private string _cachedVirtualMemoryText = "0 GB / 0 GB (0%)";
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

        public string GetCurrentDateTime() 
        {
            lock (_lockObject) { return _cachedDateTime; }
        }
        
        /// <summary>返回缓存的 CPU 使用率（实时更新）</summary>
        public float GetCpuUsage()
        {
            lock (_lockObject) { return (float)_cachedCpuUsage; }
        }

        /// <summary>返回缓存的物理内存（实时更新）</summary>
        public (double totalGB, double usedGB, double percentageUsed) GetPhysicalMemory()
        {
            lock (_lockObject) { return (_cachedPhysicalMemoryTotal, _cachedPhysicalMemoryUsed, _cachedPhysicalMemoryPercent); }
        }

        /// <summary>返回缓存的虚拟内存（实时更新）</summary>
        public (double totalGB, double usedGB, double percentageUsed, string text) GetVirtualMemory()
        {
            lock (_lockObject) { return (_cachedVirtualMemoryTotal, _cachedVirtualMemoryUsed, _cachedVirtualMemoryPercent, _cachedVirtualMemoryText); }
        }

        public (double downloadMbps, double uploadMbps) GetNetworkSpeed()
        {
            lock (_lockObject) { return (_cachedDownloadMbps, _cachedUploadMbps); }
        }

        /// <summary>后台线程：异步更新所有系统监控缓存（CPU、内存、网络）</summary>
        public void UpdateSystemCacheAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    // 【时间】更新日期时间
                    string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    
                    // 【CPU】直接查询性能计数器
                    double cpuUsage = _cpuCounter.NextValue();
                    
                    // 【物理内存】查询可用内存和总内存
                    var availableMB = new PerformanceCounter("Memory", "Available MBytes", true).NextValue();
                    using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                    {
                        long totalBytes = 0;
                        foreach (ManagementObject mo in searcher.Get())
                        {
                            if (long.TryParse(mo["Capacity"]?.ToString(), out long capacity))
                                totalBytes += capacity;
                        }
                        
                        double physicalTotal = totalBytes > 0 ? totalBytes / (double)ONE_GB : 32.0;
                        double physicalUsed = physicalTotal - availableMB / 1024.0;
                        double physicalPercent = Math.Round(physicalUsed / physicalTotal * 100, 1);
                    
                        // 【虚拟内存】查询提交的字节和限制
                        long committed = (long)(_committedBytesCounter?.NextValue() ?? 0);
                        long limit = (long)(_commitLimitCounter?.NextValue() ?? 0);
                        double vmTotal = limit / (double)ONE_GB;
                        double vmUsed = committed / (double)ONE_GB;
                        double vmPercent = limit > 0 ? Math.Round((double)committed / limit * 100, 1) : 0;
                        string vmText = $"{vmUsed:F1} GB / {vmTotal:F1} GB ({vmPercent:F1}%)";
                        
                        // 【网络】获取所有网络接口的速度（无 Sleep，直接查询）
                        double downloadBytes = GetAllNetworkInterfacesSpeed("Bytes Received/sec");
                        double uploadBytes = GetAllNetworkInterfacesSpeed("Bytes Sent/sec");
                        double downloadMbps = Math.Max(0, downloadBytes * 8 / ONE_MBPS);
                        double uploadMbps = Math.Max(0, uploadBytes * 8 / ONE_MBPS);
                        
                        // 【更新缓存】一次性更新所有数据
                        lock (_lockObject)
                        {
                            _cachedDateTime = currentDateTime;
                            _cachedCpuUsage = cpuUsage;
                            _cachedPhysicalMemoryTotal = physicalTotal;
                            _cachedPhysicalMemoryUsed = physicalUsed;
                            _cachedPhysicalMemoryPercent = physicalPercent;
                            _cachedVirtualMemoryTotal = vmTotal;
                            _cachedVirtualMemoryUsed = vmUsed;
                            _cachedVirtualMemoryPercent = vmPercent;
                            _cachedVirtualMemoryText = vmText;
                            _cachedDownloadMbps = Math.Round(downloadMbps, 2);
                            _cachedUploadMbps = Math.Round(uploadMbps, 2);
                        }
                    }
                }
                catch { }
            });
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
