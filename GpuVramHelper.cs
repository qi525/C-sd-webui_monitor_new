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
        private static string _cachedGpuName = "Unknown GPU";  // 仅初始化一次
        private static double _cachedTotalVramGB = 0;
        private static double _cachedUsedVramGB = 0;
        private static double _cachedDedicatedUsedGB = 0;
        private static double _cachedSharedUsedGB = 0;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// 【完全缓存模式】返回缓存的 GPU 信息（无实时计算）
        /// GPU 名称在初始化时获取一次后就不变，显存使用量由 UpdateGpuMemoryCacheAsync 更新
        /// </summary>
        public static (string gpuName, double usedVramGB, double totalVramGB, bool success) GetGpuVramInfo()
        {
            lock (_lockObject)
            {
                return (_cachedGpuName, _cachedUsedVramGB, _cachedTotalVramGB, _cachedTotalVramGB > 0);
            }
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
        /// 包括：总显存、已用显存、GPU 名称、专用/共享显存使用量
        /// </summary>
        public static void UpdateGpuMemoryCacheAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    // 【GPU 名称】仅第一次初始化（线程安全）
                    lock (_lockObject)
                    {
                        if (_cachedGpuName == "Unknown GPU")
                        {
                            _cachedGpuName = QueryGpuName();
                        }
                    }
                    
                    // 【总显存】从 DXGI 查询
                    var (_, dedicatedGB, sharedGB) = GetGpuMemoryFromDxgi();
                    double totalVram = Math.Max(dedicatedGB, sharedGB);
                    
                    // 【已用显存】从 Performance Counter 查询
                    double usedVram = QueryUsedGpuVramGB();
                    
                    // 【专用/共享显存】从 PowerShell 计数器查询
                    double dedicated = QueryCounterValue("\\GPU Adapter Memory(*)\\Dedicated Usage");
                    double shared = QueryCounterValue("\\GPU Adapter Memory(*)\\Shared Usage");
                    
                    // 【更新缓存】
                    lock (_lockObject)
                    {
                        _cachedTotalVramGB = totalVram;
                        _cachedUsedVramGB = usedVram;
                        _cachedDedicatedUsedGB = dedicated;
                        _cachedSharedUsedGB = shared;
                    }
                }
                catch { }
            });
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
