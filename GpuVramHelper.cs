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
        private static double? _totalVramGB;
        private static double _cachedDedicatedUsedGB = 0;
        private static double _cachedSharedUsedGB = 0;
        private static readonly object _lockObject = new object();

        public static (string gpuName, double usedVramGB, double totalVramGB, bool success) GetGpuVramInfo()
        {
            string name = GetGpuName();
            double totalVram = GetTotalVramGB();
            double usedVram = GetUsedVramGB();
            return (name, usedVram, totalVram, totalVram > 0);
        }

        private static double GetTotalVramGB()
        {
            if (_totalVramGB.HasValue) return _totalVramGB.Value;
            
            var (_, dedicatedGB, sharedGB) = GetGpuMemoryFromDxgi();
            double total = Math.Max(dedicatedGB, sharedGB);
            if (total > 0) { _totalVramGB = total; return total; }
            return 0;
        }

        private static double GetUsedVramGB()
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

        public static string GetGpuName()
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

        public static double GetGpuDedicatedMemoryGB()
        {
            var (_, dedicated, _) = GetGpuMemoryFromDxgi();
            return dedicated;
        }

        public static double GetGpuSharedMemoryGB()
        {
            var (_, _, shared) = GetGpuMemoryFromDxgi();
            return shared;
        }

        /// <summary>
        /// 第4个计数器：GPU 适配器显存 - 专用使用（返回缓存值）
        /// </summary>
        public static double GetGpuAdapterDedicatedUsedGB()
        {
            lock (_lockObject) { return _cachedDedicatedUsedGB; }
        }

        /// <summary>
        /// 第8个计数器：GPU 适配器显存 - 共享使用（返回缓存值）
        /// </summary>
        public static double GetGpuAdapterSharedUsedGB()
        {
            lock (_lockObject) { return _cachedSharedUsedGB; }
        }

        /// <summary>
        /// 后台线程调用：更新GPU显存缓存值（异步非阻塞）
        /// </summary>
        public static void UpdateGpuMemoryCacheAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    double dedicated = QueryCounterValue("\\GPU Adapter Memory(*)\\Dedicated Usage");
                    double shared = QueryCounterValue("\\GPU Adapter Memory(*)\\Shared Usage");
                    
                    lock (_lockObject)
                    {
                        _cachedDedicatedUsedGB = dedicated;
                        _cachedSharedUsedGB = shared;
                    }
                }
                catch { }
            });
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
