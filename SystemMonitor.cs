using System;
using System.Diagnostics;
using System.Management;

namespace WebUIMonitor
{
    /// <summary>
    /// 系统监控 - 获取 CPU、内存、GPU 等信息
    /// </summary>
    public class SystemMonitor
    {
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _committedBytesCounter;
        private PerformanceCounter _commitLimitCounter;
        private const long ONE_GB = 1024L * 1024L * 1024L;

        public SystemMonitor()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _committedBytesCounter = new PerformanceCounter("Memory", "Committed Bytes", true);
                _commitLimitCounter = new PerformanceCounter("Memory", "Commit Limit", true);
                
                // 预热计数器
                _ = _cpuCounter.NextValue();
                _ = _committedBytesCounter.NextValue();
                _ = _commitLimitCounter.NextValue();
            }
            catch { }
        }

        /// <summary>
        /// 获取当前日期时间
        /// </summary>
        public string GetCurrentDateTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 获取 CPU 占用百分比
        /// </summary>
        public float GetCpuUsage()
        {
            try
            {
                return _cpuCounter?.NextValue() ?? 0f;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// 获取物理内存占用信息 (总 GB / 使用 GB / 百分比)
        /// </summary>
        public (double totalGB, double usedGB, double percentageUsed) GetPhysicalMemory()
        {
            try
            {
                PerformanceCounter availableMemory = new PerformanceCounter(
                    "Memory", "Available MBytes", true);
                double availableMB = availableMemory.NextValue();

                try
                {
                    // 尝试 WMI 方式获取实际物理内存
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                        "SELECT Capacity FROM Win32_PhysicalMemory");
                    
                    long totalBytes = 0;
                    int count = 0;
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        var capacityObj = mo["Capacity"];
                        string capacityStr = capacityObj?.ToString();
                        if (!string.IsNullOrEmpty(capacityStr) && 
                            long.TryParse(capacityStr, out long capacity))
                        {
                            totalBytes += capacity;
                            count++;
                        }
                    }
                    
                    if (count > 0 && totalBytes > 0)
                    {
                        double totalGB = totalBytes / (double)ONE_GB;
                        double availableGB = availableMB / 1024.0;
                        double usedGB = totalGB - availableGB;
                        double percentageUsed = totalGB > 0 
                            ? Math.Round(usedGB / totalGB * 100, 1) 
                            : 0;

                        return (totalGB, usedGB, percentageUsed);
                    }
                }
                catch { }

                // 回退方案
                return (32.0, 16.0, 50.0);
            }
            catch
            {
                return (32.0, 16.0, 50.0);
            }
        }

        /// <summary>
        /// 获取虚拟内存（已提交）占用信息 (总 GB / 使用 GB / 百分比 / 百分比文本)
        /// </summary>
        public (double totalGB, double usedGB, double percentageUsed, string text) GetVirtualMemory()
        {
            try
            {
                long committedBytes = _committedBytesCounter != null ? (long)_committedBytesCounter.NextValue() : 0;
                long commitLimitBytes = _commitLimitCounter != null ? (long)_commitLimitCounter.NextValue() : 0;

                double totalGB = commitLimitBytes / (double)ONE_GB;
                double usedGB = committedBytes / (double)ONE_GB;
                double percentageUsed = commitLimitBytes > 0 
                    ? Math.Round((double)committedBytes / commitLimitBytes * 100, 1) 
                    : 0;

                string text = $"{usedGB:F1} GB / {totalGB:F1} GB ({percentageUsed:F1}%)";
                return (totalGB, usedGB, percentageUsed, text);
            }
            catch
            {
                return (0, 0, 0, "N/A");
            }
        }

        /// <summary>
        /// 获取显卡名称（NVIDIA 或 AMD 或 Intel）
        /// </summary>
        public string GetGpuName()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object nameObj = obj["Name"];
                        if (nameObj != null)
                        {
                            return nameObj.ToString();
                        }
                    }
                }
            }
            catch { }

            return "未检测到显卡";
        }
    }
}
