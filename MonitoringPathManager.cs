using System;
using System.IO;

namespace WebUIMonitor
{
    /// <summary>
    /// 监控文件夹路径管理器
    /// 逻辑：
    /// 1. 读取 config.json 中的基础路径
    /// 2. 检查是否存在 txt2img-images\yyyy-MM-dd 子文件夹
    /// 3. 如果存在就监控那个文件夹，否则直接监控基础路径
    /// </summary>
    public class MonitoringPathManager
    {
        private string _configuredPath;  // 来自 config.json 的路径
        private string _actualMonitorPath;  // 实际监控的路径

        public MonitoringPathManager(string configuredPath)
        {
            _configuredPath = configuredPath ?? throw new ArgumentNullException(nameof(configuredPath));
            CalculateActualPath();
        }

        /// <summary>
        /// 更新配置的基础路径
        /// </summary>
        public void SetBasePath(string basePath)
        {
            _configuredPath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            CalculateActualPath();
        }

        /// <summary>
        /// 获取 config.json 中配置的路径
        /// </summary>
        public string GetConfiguredPath() => _configuredPath;

        /// <summary>
        /// 计算实际监控的路径
        /// 逻辑：
        /// - 如果存在 {configuredPath}\txt2img-images\yyyy-MM-dd 就用它
        /// - 否则直接用 {configuredPath}
        /// </summary>
        private void CalculateActualPath()
        {
            string todayFolder = DateTime.Now.ToString("yyyy-MM-dd");
            string potentialPath = Path.Combine(_configuredPath, "txt2img-images", todayFolder);

            if (Directory.Exists(potentialPath))
            {
                // 存在子文件夹，使用它
                _actualMonitorPath = potentialPath;
                System.Diagnostics.Debug.WriteLine($"[MonitoringPathManager] 检测到子文件夹: {_actualMonitorPath}");
            }
            else
            {
                // 不存在子文件夹，使用配置的路径
                _actualMonitorPath = _configuredPath;
                System.Diagnostics.Debug.WriteLine($"[MonitoringPathManager] 使用配置路径: {_actualMonitorPath}");
            }
        }

        /// <summary>
        /// 获取实际的监控路径
        /// </summary>
        public string GetActualMonitorPath()
        {
            // 每次获取时都重新计算，这样日期变化时能自动切换到新日期的文件夹
            CalculateActualPath();
            return _actualMonitorPath;
        }

        /// <summary>
        /// 获取今日路径下的文件数
        /// </summary>
        public int GetFileCount()
        {
            string monitorPath = GetActualMonitorPath();

            if (!Directory.Exists(monitorPath))
            {
                return 0;
            }

            try
            {
                return Directory.GetFiles(monitorPath).Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}
