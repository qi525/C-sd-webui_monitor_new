using System;
using System.Diagnostics;
using System.Management;

namespace WebUIMonitor
{
    public class SystemMonitor
    {
        private PerformanceCounter _cpuCounter, _committedBytesCounter, _commitLimitCounter;
        private const long ONE_GB = 1024L * 1024L * 1024L;
        private const long ONE_MBPS = 1024L * 1024L;

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
                    return (physicalTotal, physicalUsed, physicalPercent);
                }
            }
            catch { return (0, 0, 0); }
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

        public (double downloadMbps, double uploadMbps) GetNetworkSpeed()
        {
            try
            {
                double downloadBytes = GetAllNetworkInterfacesSpeed("Bytes Received/sec");
                double uploadBytes = GetAllNetworkInterfacesSpeed("Bytes Sent/sec");
                double downloadMbps = Math.Max(0, downloadBytes * 8 / ONE_MBPS);
                double uploadMbps = Math.Max(0, uploadBytes * 8 / ONE_MBPS);
                return (Math.Round(downloadMbps, 2), Math.Round(uploadMbps, 2));
            }
            catch { return (0, 0); }
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
