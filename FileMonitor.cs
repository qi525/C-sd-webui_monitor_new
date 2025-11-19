using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    /// <summary>
    /// 文件夹监控模块 - 检测文件数量变化，触发警报
    /// 逻辑: 如果 30 秒内文件数没有增加，则触发警报
    /// 
    /// 注意: _monitorPath 应该是完整的监控路径（来自 config.json）
    /// 例如: C:\outputs\txt2img-images\2025-11-20
    /// </summary>
    public class FileMonitor
    {
        private string _monitorPath;  // 完整的监控路径
        private bool _isAlarm = false;
        private int _lastFileCount = -1;
        private DateTime _lastFileChangeTime = DateTime.Now;
        private const int NoChangeAlarmSeconds = 30;
        private const int CheckIntervalMs = 3000;
        private bool _isRunning = false;

        public FileMonitor(string monitorPath)
        {
            _monitorPath = monitorPath;
            System.Diagnostics.Debug.WriteLine($"[FileMonitor] 初始化: {_monitorPath}");
        }

        /// <summary>
        /// 设置监控路径（支持运行时改变）
        /// </summary>
        public void SetMonitorPath(string monitorPath)
        {
            System.Diagnostics.Debug.WriteLine($"[FileMonitor] 路径改变: {_monitorPath} -> {monitorPath}");
            _monitorPath = monitorPath;
            _lastFileCount = -1;
            _lastFileChangeTime = DateTime.Now;
        }

        public void Start()
        {
            _isRunning = true;
            _ = Task.Run(() =>
            {
                while (_isRunning)
                {
                    try
                    {
                        CheckFileCount();
                        Thread.Sleep(CheckIntervalMs);
                    }
                    catch (Exception ex)
                    { 
                        System.Diagnostics.Debug.WriteLine($"[FileMonitor] 错误: {ex.Message}");
                        Thread.Sleep(CheckIntervalMs);
                    }
                }
            });
        }

        private void CheckFileCount()
        {
            // 直接使用 _monitorPath，它已经是完整的监控路径
            if (!Directory.Exists(_monitorPath))
            {
                // 路径不存在，重置状态
                _lastFileCount = 0;
                return;
            }

            int currentFileCount = Directory.GetFiles(_monitorPath).Length;
            System.Diagnostics.Debug.WriteLine($"[FileMonitor] 文件数: {currentFileCount}, 路径: {_monitorPath}");

            // 初始化：首次检查时仅记录文件数
            if (_lastFileCount == -1)
            {
                _lastFileCount = currentFileCount;
                _lastFileChangeTime = DateTime.Now;
                return;
            }

            // 检查是否有新文件生成
            if (currentFileCount > _lastFileCount)
            {
                // 文件数增加 ✅
                _lastFileCount = currentFileCount;
                _lastFileChangeTime = DateTime.Now;
                _isAlarm = false;
            }
            else
            {
                // 文件数不变或减少 - 检查是否超过 30 秒
                int secondsSinceLastChange = (int)(DateTime.Now - _lastFileChangeTime).TotalSeconds;
                
                if (secondsSinceLastChange >= NoChangeAlarmSeconds)
                {
                    // 超过 30 秒没有新文件 🚨
                    _isAlarm = true;
                }
                else
                {
                    _isAlarm = false;
                }
            }
        }

        public bool IsAlarm => _isAlarm;
        public int FileCount => _lastFileCount;

        public void Stop()
        {
            _isRunning = false;
        }
    }
}