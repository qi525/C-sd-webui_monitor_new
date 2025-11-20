using System;
using System.IO;

namespace WebUIMonitor
{
    /// <summary>
    /// 监控文件夹路径管理器
    /// 逻辑：
    /// 1. 读取 config.json 中的基础路径
    /// 2. 检查是否存在 yyyy-MM-dd 子文件夹，存在就监控它，否则监控基础路径
    /// 3. 获取文件数量
    /// </summary>
    public class MonitoringPathManager
    {
        public string ConfiguredPath { get; private set; }
        public MonitoringPathManager(string configuredPath) => ConfiguredPath = configuredPath ?? throw new ArgumentNullException(nameof(configuredPath));
        public void SetBasePath(string basePath) => ConfiguredPath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        public string GetActualMonitorPath()
        {
            var candidate = Path.Combine(ConfiguredPath, DateTime.Now.ToString("yyyy-MM-dd"));
            return Directory.Exists(candidate) ? candidate : ConfiguredPath;
        }
        public int GetFileCount()
        {
            var p = GetActualMonitorPath();
            return Directory.Exists(p) ? Directory.GetFiles(p).Length : 0;
        }
    }
}
