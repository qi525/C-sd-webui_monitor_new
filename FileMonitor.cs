using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    /// <summary>
    /// 文件监控器 - 实时轮询监听文件夹
    /// 
    /// 报警逻辑：
    /// - 文件数有变化 → 清除报警，重置计时器
    /// - 文件数30秒无变化 → 触发报警
    /// </summary>
    public class FileMonitor
    {
        private Func<string> _pathProvider;
        private int _currentFileCount = 0;
        private DateTime _lastChangeTime = DateTime.Now;
        private bool _isAlarm = false;
        private string _monitoringPath = "";
        private bool _isRunning = false;

        public void SetPathProvider(Func<string> pathProvider) => _pathProvider = pathProvider;

        public void Start() => _ = Task.Run(() =>
        {
            _isRunning = true;
            while (_isRunning)
            {
                try
                {
                    // 每次都实时获取路径（支持凌晨自动切换）
                    string path = _pathProvider?.Invoke() ?? "";
                    
                    if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    {
                        _monitoringPath = path;
                        _currentFileCount = 0;
                        Thread.Sleep(3000);
                        continue;
                    }

                    // 直接获取文件数，无缓存
                    int fileCount = Directory.GetFiles(path).Length;

                    // 路径有变化（凌晨切换）
                    if (_monitoringPath != path)
                    {
                        _monitoringPath = path;
                        _currentFileCount = fileCount;
                        _lastChangeTime = DateTime.Now;
                        _isAlarm = false;
                        System.Diagnostics.Debug.WriteLine($"[FileMonitor] 路径切换: {path}");
                    }
                    // 文件数有变化
                    else if (fileCount != _currentFileCount)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FileMonitor] 文件数变化: {_currentFileCount} → {fileCount}");
                        _currentFileCount = fileCount;
                        _lastChangeTime = DateTime.Now;
                        _isAlarm = false;
                    }
                    // 文件数未变化，检查是否超过30秒
                    else
                    {
                        int secondsNoChange = (int)(DateTime.Now - _lastChangeTime).TotalSeconds;
                        bool newAlarm = secondsNoChange >= 30;
                        
                        if (newAlarm && !_isAlarm)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FileMonitor] 触发报警 - {secondsNoChange}秒内文件数保持 {_currentFileCount} 不变");
                        }
                        _isAlarm = newAlarm;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FileMonitor] 异常: {ex.Message}");
                }

                Thread.Sleep(3000);
            }
        });

        public bool IsAlarm => _isAlarm;
        public int FileCount => _currentFileCount;
        public string CurrentPath => _monitoringPath;

        public void Stop() => _isRunning = false;
    }
}