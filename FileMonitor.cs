using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    /// <summary>
    /// 文件夹监控模块 - 检测文件数量变化，触发警报
    /// 逻辑: 文件数持续增加 → 正常; 文件数不变或减少 → 触发警报
    /// </summary>
    public class FileMonitor
    {
        private string _monitorPath;
        private bool _isAlarm = false;
        private int _lastFileCount = -1;
        private int _consecutiveIncreaseCount = 0;
        private const int IncreaseThreshold = 2; // 连续增加多少次才算"持续增加"
        private const int CheckIntervalMs = 10000; // 检查间隔（毫秒）
        private bool _isRunning = false;

        public FileMonitor(string monitorPath)
        {
            _monitorPath = monitorPath;
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
                    catch 
                    { 
                        Thread.Sleep(CheckIntervalMs); 
                    }
                }
            });
        }

        private void CheckFileCount()
        {
            // 动态获取今日文件夹路径
            // 路径结构: outputs/txt2img-images/yyyy-MM-dd/
            string basePath = _monitorPath;
            string txt2imgPath = Path.Combine(basePath, "txt2img-images");
            string todayFolder = DateTime.Now.ToString("yyyy-MM-dd");
            string path = Path.Combine(txt2imgPath, todayFolder);

            int currentFileCount = Directory.Exists(path) ? Directory.GetFiles(path).Length : 0;

            // 初始化：首次检查时仅记录文件数
            if (_lastFileCount == -1)
            {
                _lastFileCount = currentFileCount;
                return;
            }

            // 对比逻辑（参考 Python 脚本）
            if (currentFileCount > _lastFileCount)
            {
                // 文件数增加 ✅
                _consecutiveIncreaseCount++;
                
                if (_consecutiveIncreaseCount >= IncreaseThreshold)
                {
                    // 达到阈值，确认为持续增加，取消警报
                    _isAlarm = false;
                }
            }
            else if (currentFileCount == _lastFileCount)
            {
                // 文件数不变 🛑
                if (_consecutiveIncreaseCount > 0)
                {
                    _consecutiveIncreaseCount = 0;
                }
                _isAlarm = true; // 触发警报
            }
            else if (currentFileCount < _lastFileCount)
            {
                // 文件数减少 ⚠️
                _consecutiveIncreaseCount = 0;
                _isAlarm = true; // 触发警报
            }

            _lastFileCount = currentFileCount;
        }

        public bool IsAlarm => _isAlarm;
        public int FileCount => _lastFileCount;

        public void Stop()
        {
            _isRunning = false;
        }
    }
}