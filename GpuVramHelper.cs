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
        
        // 缓存最新计算结果（供UI快速读取）
        private static (string gpuName, double usedVramGB, double totalVramGB, bool success) _lastGpuVramInfo = ("Unknown GPU", 0, 0, false);
        private static double _lastDedicatedGB = 0;
        private static double _lastSharedGB = 0;
        private static double _lastDedicatedUsed = 0;
        private static double _lastSharedUsed = 0;

        public static (string gpuName, double usedVramGB, double totalVramGB, bool success) GetGpuVramInfo()
        {
            // 返回最新计算结果，不阻塞
            return _lastGpuVramInfo;
        }
        
        /// <summary>后台异步计算GPU数据（不阻塞UI）</summary>
        public static void UpdateGpuAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    var (gpuName, dedicatedGB, sharedGB) = GetGpuMemoryFromDxgi();
                    double totalVram = Math.Max(dedicatedGB, sharedGB);
                    double usedVram = QueryUsedGpuVramGB();
                    _lastGpuVramInfo = (gpuName, usedVram, totalVram, totalVram > 0);
                    _lastDedicatedGB = dedicatedGB;
                    _lastSharedGB = sharedGB;
                    _lastDedicatedUsed = QueryCounterValue("\\GPU Adapter Memory(*)\\Dedicated Usage");
                    _lastSharedUsed = QueryCounterValue("\\GPU Adapter Memory(*)\\Shared Usage");
                }
                catch { }
            });
        }

        public static double GetGpuDedicatedMemoryGB()
        {
            return _lastDedicatedGB;
        }

        public static double GetGpuSharedMemoryGB()
        {
            return _lastSharedGB;
        }

        public static double GetGpuAdapterDedicatedUsedGB()
        {
            return _lastDedicatedUsed;
        }

        public static double GetGpuAdapterSharedUsedGB()
        {
            return _lastSharedUsed;
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
