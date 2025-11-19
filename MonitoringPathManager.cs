using System;
using System.IO;

namespace WebUIMonitor
{
    /// <summary>
    /// 监控文件夹路径管理器 - 处理所有与路径相关的逻辑
    /// 职责：
    /// 1. 管理当前监控的基础路径
    /// 2. 计算今日的实际监控路径
    /// 3. 获取该路径下的文件数
    /// </summary>
    public class MonitoringPathManager
    {
        private string _basePath;  // 基础路径：outputs 文件夹
        private string _todayPath;  // 今日路径：outputs/txt2img-images/yyyy-MM-dd
        private int _lastFileCount = -1;  // 缓存的文件数
        private DateTime _lastPathUpdate = DateTime.MinValue;  // 上次更新时间

        public MonitoringPathManager(string basePath)
        {
            SetBasePath(basePath);
        }

        /// <summary>
        /// 设置基础路径（outputs 文件夹）
        /// </summary>
        public void SetBasePath(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            _lastFileCount = -1;  // 重置文件数
            UpdateTodayPath();
        }

        /// <summary>
        /// 获取当前基础路径
        /// </summary>
        public string GetBasePath() => _basePath;

        /// <summary>
        /// 获取今日监控路径（完整路径）
        /// </summary>
        public string GetTodayPath()
        {
            // 检查日期是否变化了，如果变化则重新计算
            UpdateTodayPath();
            return _todayPath;
        }

        /// <summary>
        /// 获取今日路径下的文件数
        /// </summary>
        public int GetFileCount()
        {
            UpdateTodayPath();

            if (!Directory.Exists(_todayPath))
            {
                return 0;
            }

            try
            {
                int count = Directory.GetFiles(_todayPath).Length;
                _lastFileCount = count;
                return count;
            }
            catch
            {
                return _lastFileCount >= 0 ? _lastFileCount : 0;
            }
        }

        /// <summary>
        /// 更新今日路径（如果日期变化了）
        /// </summary>
        private void UpdateTodayPath()
        {
            string todayFolder = DateTime.Now.ToString("yyyy-MM-dd");
            string newPath = Path.Combine(_basePath, "txt2img-images", todayFolder);

            // 如果路径变了，重置文件数
            if (_todayPath != newPath)
            {
                _todayPath = newPath;
                _lastFileCount = -1;
            }

            _lastPathUpdate = DateTime.Now;
        }

        /// <summary>
        /// 获取显示用的文本（完整的今日路径）
        /// </summary>
        public string GetDisplayText()
        {
            return $"目前监控文件夹位置: {GetTodayPath()}";
        }
    }
}
