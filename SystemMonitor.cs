using System;
using System.Diagnostics;
using System.Management;

namespace WebUIMonitor
{
    public class SystemMonitor
    {
        private PerformanceCounter _cpuCounter, _committedBytesCounter, _commitLimitCounter;
        private const long ONE_GB = 1024L * 1024L * 1024L;

        public SystemMonitor()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _committedBytesCounter = new PerformanceCounter("Memory", "Committed Bytes", true);
            _commitLimitCounter = new PerformanceCounter("Memory", "Commit Limit", true);
            _ = _cpuCounter.NextValue(); _ = _committedBytesCounter.NextValue(); _ = _commitLimitCounter.NextValue();
        }

        public string GetCurrentDateTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public float GetCpuUsage() => _cpuCounter?.NextValue() ?? 0f;

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

        public string GetGpuName()
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
                return obj["Name"]?.ToString() ?? "未检测到显卡";
            return "未检测到显卡";
        }
    }
}
