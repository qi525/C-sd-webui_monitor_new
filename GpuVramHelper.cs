using System;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using SharpDX.DXGI;

namespace WebUIMonitor
{
    public static class GpuVramHelper
    {
        private const long ONE_GB = 1024L * 1024L * 1024L;
        private const double BytesToGB = 1024.0 * 1024.0 * 1024.0;
        
        // 缓存 GPU 数据
        private static string _cachedGpuName = "Unknown GPU";
        private static double _cachedDedicatedGB = 0;
        private static double _cachedSharedGB = 0;
        private static double _cachedUsedVramGB = 0;
        private static double _cachedDedicatedUsedGB = 0;
        private static double _cachedSharedUsedGB = 0;

        public static (string gpuName, double usedVramGB, double totalVramGB, bool success) GetGpuVramInfo()
        {
            // 直接返回缓存，无阻塞
            double totalVram = Math.Max(_cachedDedicatedGB, _cachedSharedGB);
            return (_cachedGpuName, _cachedUsedVramGB, totalVram, totalVram > 0);
        }
        
        /// <summary>后台异步更新 GPU 缓存，避免阻塞 UI</summary>
        public static void UpdateGpuCacheAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    var (gpuName, dedicatedGB, sharedGB) = GetGpuMemoryFromDxgi();
                    _cachedGpuName = gpuName;
                    _cachedDedicatedGB = dedicatedGB;
                    _cachedSharedGB = sharedGB;
                    _cachedUsedVramGB = QueryUsedGpuVramGB();
                    _cachedDedicatedUsedGB = QueryCounterValue("\\GPU Adapter Memory(*)\\Dedicated Usage");
                    _cachedSharedUsedGB = QueryCounterValue("\\GPU Adapter Memory(*)\\Shared Usage");
                }
                catch { }
            });
        }

        public static double GetGpuDedicatedMemoryGB()
        {
            return _cachedDedicatedGB;
        }

        public static double GetGpuSharedMemoryGB()
        {
            return _cachedSharedGB;
        }

        public static double GetGpuAdapterDedicatedUsedGB()
        {
            return _cachedDedicatedUsedGB;
        }

        public static double GetGpuAdapterSharedUsedGB()
        {
            return _cachedSharedUsedGB;
        }

        /// <summary>查询 GPU 名称（仅第一次初始化时调用）</summary>
        private static string QueryGpuName()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select Name from Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(name) && name != "Unknown GPU") return name;
                    }
                }
            }
            catch { }
            return "Unknown GPU";
        }

        /// <summary>查询已用显存（GB）</summary>
        private static double QueryUsedGpuVramGB()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("select CurrentUsage from Win32_PerfFormattedData_GPUPerformanceCounters_GPUProcessMemory"))
                {
                    long total = 0;
                    foreach (ManagementObject obj in searcher.Get())
                        if (ulong.TryParse(obj["CurrentUsage"]?.ToString() ?? "0", out ulong val))
                            total += (long)val;
                    if (total > 0) return Math.Max(0, total / (double)ONE_GB);
                }
            }
            catch { }
            return 0;
        }

        private static double QueryCounterValue(string counterPath)
        {
            try
            {
                var command = $"Get-Counter -Counter '{counterPath}' -SampleInterval 1 -MaxSamples 1 -ErrorAction SilentlyContinue | " +
                              "Select-Object -ExpandProperty CounterSamples | Select-Object -ExpandProperty CookedValue | " +
                              "Measure-Object -Sum | Select-Object -ExpandProperty Sum";

                using (var p = new Process())
                {
                    p.StartInfo.FileName = "powershell";
                    p.StartInfo.Arguments = $"-NoProfile -Command \"{command}\"";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    if (p.WaitForExit(5000) && double.TryParse(p.StandardOutput.ReadToEnd().Trim(), out double bytes) && bytes > 0)
                        return Math.Round(bytes / BytesToGB, 2);
                }
            }
            catch { }
            return 0;
        }

        private static (string name, double dedicatedGB, double sharedGB) GetGpuMemoryFromDxgi()
        {
            try
            {
                using (var factory = new Factory1())
                {
                    if (factory.GetAdapterCount1() > 0)
                    {
                        using (var adapter = factory.GetAdapter1(0))
                        {
                            var desc = adapter.Description1;
                            double dedicatedGB = (double)desc.DedicatedVideoMemory / ONE_GB;
                            double sharedGB = (double)desc.SharedSystemMemory / ONE_GB;
                            return (desc.Description ?? "Unknown GPU", dedicatedGB, sharedGB);
                        }
                    }
                }
            }
            catch { }
            return ("Unknown GPU", 0, 0);
        }
    }
}
