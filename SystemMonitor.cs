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
        
        // 缓存最新计算结果（供UI快速读取）
        private (double totalGB, double usedGB, double percentageUsed) _lastPhysicalMemory = (0, 0, 0);
        private (double downloadMBps, double uploadMBps) _lastNetworkSpeed = (0, 0);

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

        public float GetCpuUsage() => (float)_cpuCounter.NextValue();

        public (double totalGB, double usedGB, double percentageUsed) GetPhysicalMemory()
        {
            // 返回最新计算结果，不阻塞
            return _lastPhysicalMemory;
        }
        
        /// <summary>后台异步计算物理内存（不阻塞UI）</summary>
        public void UpdatePhysicalMemoryAsync()
        {
            Task.Run(() =>
            {
                try
                {
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
                        _lastPhysicalMemory = (physicalTotal, physicalUsed, physicalPercent);
                    }
                }
                catch { }
            });
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
            // 返回最新计算结果，不阻塞
            return _lastNetworkSpeed;
        }
        
        /// <summary>后台异步计算网络速度（不阻塞UI）</summary>
        public void UpdateNetworkSpeedAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    double downloadBytes = GetAllNetworkInterfacesSpeed("Bytes Received/sec");
                    double uploadBytes = GetAllNetworkInterfacesSpeed("Bytes Sent/sec");
                    double downloadMBps = Math.Max(0, downloadBytes / (1024.0 * 1024.0));
                    double uploadMBps = Math.Max(0, uploadBytes / (1024.0 * 1024.0));
                    _lastNetworkSpeed = (Math.Round(downloadMBps, 2), Math.Round(uploadMBps, 2));
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
