using System;
using System.IO;

namespace WebUIMonitor
{
    /// <summary>
    /// 监控文件夹路径管理器 - 处理所有与路径相关的逻辑
    /// 职责：
    /// 1. 管理监控的基础路径（来自 config.json）
    /// 2. 计算今日的实际监控路径（基础路径 + txt2img-images + yyyy-MM-dd）
    /// 3. 获取该路径下的文件数
    /// </summary>
    public class MonitoringPathManager
    {
        private string _configuredPath;  // 来自 config.json 的路径
        private string _todayPath;       // 今日的实际监控路径
        private int _lastFileCount = -1; // 缓存的文件数

        public MonitoringPathManager(string configuredPath)
        {
            _configuredPath = configuredPath ?? throw new ArgumentNullException(nameof(configuredPath));
            CalculateTodayPath();
        }

        /// <summary>
        /// 更新配置的基础路径
        /// </summary>
        public void SetBasePath(string basePath)
        {
            _configuredPath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            CalculateTodayPath();
        }

        /// <summary>
        /// 获取 config.json 中配置的路径
        /// </summary>
        public string GetConfiguredPath() => _configuredPath;

        /// <summary>
        /// 计算今日的监控路径
        /// 格式: {configuredPath}\txt2img-images\yyyy-MM-dd
        /// </summary>
        private void CalculateTodayPath()
        {
            string todayFolder = DateTime.Now.ToString("yyyy-MM-dd");
            _todayPath = Path.Combine(_configuredPath, "txt2img-images", todayFolder);
            _lastFileCount = -1;
        }

        /// <summary>
        /// 获取今日的监控路径（完整路径）
        /// </summary>
        public string GetTodayPath()
        {
            // 检查日期是否变化，如果变化则重新计算
            string todayFolder = DateTime.Now.ToString("yyyy-MM-dd");
            if (!_todayPath.Contains(todayFolder))
            {
                CalculateTodayPath();
            }
            return _todayPath;
        }

        /// <summary>
        /// 获取今日路径下的文件数
        /// </summary>
        public int GetFileCount()
        {
            string todayPath = GetTodayPath();

            if (!Directory.Exists(todayPath))
            {
                return 0;
            }

            try
            {
                int count = Directory.GetFiles(todayPath).Length;
                _lastFileCount = count;
                return count;
            }
            catch
            {
                return _lastFileCount >= 0 ? _lastFileCount : 0;
            }
        }
    }
}
