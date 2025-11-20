using System;
using System.Diagnostics;
using System.Management;
using SharpDX.DXGI;

namespace WebUIMonitor
{
    /// <summary>
    /// GPU 显存信息获取（支持 NVIDIA/AMD/Intel）
    /// 降级链：NVML → DXGI → WMI → 性能计数器 → PowerShell
    /// </summary>
    public static class GpuVramHelper
    {
        private const long ONE_GB = 1024L * 1024L * 1024L;
        private static double? _totalVramGB;
        private static PerformanceCounter _gpuMemory;

        /// <summary>
        /// 获取主 GPU 名称、实时占用、总显存（优先级降级）
        /// </summary>
        public static (string gpuName, double usedVramGB, double totalVramGB, bool success) GetGpuVramInfo()
        {
            // 方案 1：NVML（NVIDIA/AMD 原生支持）
            var (nvmlName, nvmlUsed, nvmlTotal, nvmlSuccess) = NvmlHelper.GetGpuMemory();
            if (nvmlSuccess && nvmlTotal > 0)
                return (nvmlName, nvmlUsed, nvmlTotal, true);

            // 方案 2：DXGI（Windows 原生，获取总显存）
            try
            {
                var (gpuName, totalVram) = GetGpuInfoFromDxgi();
                _totalVramGB ??= totalVram;
                double usedVram = GetUsedVram();
                return (gpuName, Math.Max(0, usedVram), totalVram, true);
            }
            catch
            {
                return ("Unknown GPU", 0, _totalVramGB ?? 0, false);
            }
        }

        /// <summary>
        /// 获取主 GPU 名称（纯名称，无显存信息）
        /// </summary>
        public static string GetGpuName()
        {
            // 优先 NVML
            var (nvmlName, _, _, success) = NvmlHelper.GetGpuMemory();
            if (success && nvmlName != "N/A") return nvmlName;

            // 降级 DXGI
            try
            {
                var (dxgiName, _) = GetGpuInfoFromDxgi();
                if (dxgiName != "Unknown GPU") return dxgiName;
            }
            catch { }

            // 降级 WMI
            try
            {
                using (var searcher = new ManagementObjectSearcher("select Name from Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                        return obj["Name"]?.ToString() ?? "Unknown GPU";
                }
            }
            catch { }

            return "Unknown GPU";
        }

        /// <summary>
        /// 获取 GPU 品牌（NVIDIA/AMD/Intel/Unknown）
        /// </summary>
        public static string GetGpuBrand()
        {
            string fullName = GetGpuName();
            if (fullName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
            if (fullName.Contains("GeForce", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
            if (fullName.Contains("AMD", StringComparison.OrdinalIgnoreCase)) return "AMD";
            if (fullName.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return "AMD";
            if (fullName.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
            if (fullName.Contains("Arc", StringComparison.OrdinalIgnoreCase)) return "Intel";
            return "Unknown";
        }

        /// <summary>
        /// 获取 GPU 型号（去除品牌信息）
        /// 例如："Intel(R) Arc(TM) A770 Graphics" → "Arc A770"
        /// </summary>
        public static string GetGpuModel()
        {
            string fullName = GetGpuName();
            if (string.IsNullOrEmpty(fullName) || fullName == "Unknown GPU") return "Unknown";

            // 移除品牌标记和多余符号
            string model = fullName
                .Replace("Intel(R)", "").Replace("Intel", "")
                .Replace("NVIDIA", "").Replace("(TM)", "").Replace("(R)", "")
                .Replace("AMD", "").Replace("Radeon", "")
                .Trim();

            return string.IsNullOrWhiteSpace(model) ? "Unknown" : model;
        }

        /// <summary>
        /// 方案 2：DXGI 获取 GPU 名称和总显存（无硬编码）
        /// </summary>
        private static (string name, double totalGB) GetGpuInfoFromDxgi()
        {
            using (var factory = new Factory1())
            {
                if (factory.GetAdapterCount1() > 0)
                {
                    using (var adapter = factory.GetAdapter1(0))
                    {
                        var desc = adapter.Description1;
                        double gb = desc.DedicatedVideoMemory > 0 
                            ? desc.DedicatedVideoMemory / (double)ONE_GB 
                            : 0;
                        return (desc.Description ?? "Unknown GPU", gb);
                    }
                }
            }
            return ("Unknown GPU", 0);
        }

        /// <summary>
        /// 获取实时显存占用（WMI → 性能计数器 → PowerShell）
        /// </summary>
        private static double GetUsedVram()
        {
            try
            {
                long total = 0;
                // 方案 2a：WMI GPU 进程显存
                using (var searcher = new ManagementObjectSearcher(
                    "select CurrentUsage from Win32_PerfFormattedData_GPUPerformanceCounters_GPUProcessMemory"))
                {
                    var results = searcher.Get();
                    if (results.Count == 0) return GetUsedVramCounter();
                    
                    foreach (ManagementObject obj in results)
                    {
                        if (ulong.TryParse(obj["CurrentUsage"]?.ToString() ?? "0", out ulong val))
                            total += (long)val;
                    }
                }
                return total > 0 ? total / (double)ONE_GB : GetUsedVramCounter();
            }
            catch { return GetUsedVramCounter(); }
        }

        /// <summary>
        /// 方案 2b：Windows 性能计数器（GPU Memory）
        /// </summary>
        private static double GetUsedVramCounter()
        {
            try
            {
                _gpuMemory ??= new PerformanceCounter("GPU Memory", "Local", "_Total", true);
                _gpuMemory.NextValue();  // 跳过第一次返回值（总是 0）
                float val = _gpuMemory.NextValue();
                return val > 0 ? val / 1024.0 : GetUsedVramFromPowerShell();
            }
            catch { return GetUsedVramFromPowerShell(); }
        }

        /// <summary>
        /// 方案 2c：PowerShell Get-Counter（最后的救命稻草）
        /// 通过 GPU Process Memory 性能计数器求和
        /// </summary>
        private static double GetUsedVramFromPowerShell()
        {
            try
            {
                string psCommand = @"powershell -NoProfile -Command " +
                    @"""((Get-Counter '\GPU Process Memory(*)\Local Usage' -ErrorAction SilentlyContinue).CounterSamples | " +
                    @"Select-Object -ExpandProperty CookedValue | Measure-Object -Sum).Sum""";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {psCommand}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    if (process == null) return 0;
                    process.WaitForExit(5000);
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    return string.IsNullOrWhiteSpace(output) ? 0 : 
                        (double.TryParse(output, out double bytes) ? bytes / ONE_GB : 0);
                }
            }
            catch { return 0; }
        }
    }
}