using System;
using System.Diagnostics;

namespace WebUIMonitor
{
    /// <summary>
    /// GPU 显存获取助手 - 使用 PowerShell 性能计数器 \GPU Process Memory(*)\Local Usage
    /// 参考 Python 代码的方式
    /// </summary>
    public static class GpuVramHelper
    {
        private const long ONE_GB = 1024L * 1024L * 1024L;
        private const double TOTAL_VRAM_GB = 16.0;

        public static (string gpuName, double usedVramGB, bool success) GetGpuVramInfo()
        {
            try
            {
                string psCommand = @"powershell -ExecutionPolicy Bypass -Command " +
                    @"""((Get-Counter '\GPU Process Memory(*)\Local Usage' -ErrorAction SilentlyContinue).CounterSamples | " +
                    @"Select-Object -ExpandProperty CookedValue | Measure-Object -Sum).Sum""";

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {psCommand}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return ("Intel Arc A770", TOTAL_VRAM_GB, false);

                    process.WaitForExit(5000);
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    if (string.IsNullOrWhiteSpace(output)) return ("Intel Arc A770", TOTAL_VRAM_GB, false);

                    return double.TryParse(output, out double usedBytes) 
                        ? ("Intel Arc A770", usedBytes / ONE_GB, true) 
                        : ("Intel Arc A770", 0, false);
                }
            }
            catch { return ("Intel Arc A770", TOTAL_VRAM_GB, false); }
        }
    }
}
