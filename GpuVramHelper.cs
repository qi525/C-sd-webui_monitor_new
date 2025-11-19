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
        private const long ONE_MB = 1024L * 1024L;
        
        // Intel Arc A770 总显存 (GB)
        private const double TOTAL_VRAM_GB = 16.0;

        /// <summary>
        /// 获取 GPU 显存信息 - 使用 PowerShell 性能计数器
        /// 查询路径: \GPU Process Memory(*)\Local Usage
        /// </summary>
        /// <returns>返回 (GPU 名称, 已用显存 GB, 是否成功)</returns>
        public static (string gpuName, double usedVramGB, bool success) GetGpuVramInfo()
        {
            try
            {
                // 构建 PowerShell 命令：获取 GPU Process Memory Local Usage 的总和
                // 这获取的是实际使用的显存（字节）
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
                    if (process == null)
                    {
                        Debug.WriteLine("[GPU] 无法启动 PowerShell 进程");
                        return ("Intel Arc A770", TOTAL_VRAM_GB, false);
                    }

                    string output = process.StandardOutput.ReadToEnd().Trim();
                    string error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit(5000);

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Debug.WriteLine($"[GPU] PowerShell 错误: {error}");
                    }

                    if (string.IsNullOrWhiteSpace(output))
                    {
                        Debug.WriteLine("[GPU] PowerShell 返回空输出，使用默认值 16GB");
                        return ("Intel Arc A770", TOTAL_VRAM_GB, false);
                    }

                    // 尝试解析为双精度浮点数（字节数）
                    if (double.TryParse(output, out double usedBytes))
                    {
                        double usedGB = usedBytes / ONE_GB;
                        double usedPercent = (usedBytes / (TOTAL_VRAM_GB * ONE_GB)) * 100;

                        Debug.WriteLine($"[GPU] 显存使用: {usedGB:F2} GB / {TOTAL_VRAM_GB:F1} GB ({usedPercent:F1}%)");

                        // 返回实际使用的显存
                        return ($"Intel Arc A770", usedGB, true);
                    }
                    else
                    {
                        Debug.WriteLine($"[GPU] 无法解析 PowerShell 输出为数字: {output}");
                        return ("Intel Arc A770", 0, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GPU] 异常: {ex.Message}");
                return ("Intel Arc A770", TOTAL_VRAM_GB, false);
            }
        }

        /// <summary>
        /// 仅获取总显存（GB）
        /// </summary>
        public static double GetTotalVramGB()
        {
            return TOTAL_VRAM_GB;
        }

        /// <summary>
        /// 仅获取 GPU 名称
        /// </summary>
        public static string GetGpuName()
        {
            var (gpuName, _, success) = GetGpuVramInfo();
            return gpuName;
        }
    }
}
