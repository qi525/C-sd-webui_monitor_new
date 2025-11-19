using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    /// <summary>
    /// 文件夹监控模块 - 检测文件数量变化，触发警报
    /// 逻辑: 如果 30 秒内文件数没有增加，则触发警报
    /// </summary>
    public class FileMonitor
    {
        private string _monitorPath;
        private bool _isAlarm = false;
        private int _lastFileCount = -1;
        private DateTime _lastFileChangeTime = DateTime.Now;
        private const int NoChangeAlarmSeconds = 30; // 30 秒没有新增文件就报警
        private const int CheckIntervalMs = 3000; // 每 3 秒检查一次
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
                _lastFileChangeTime = DateTime.Now;
                return;
            }

            // 检查是否有新文件生成
            if (currentFileCount > _lastFileCount)
            {
                // 文件数增加 ✅ - 重置计时，取消警报
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
                    // 超过 30 秒没有新文件 🚨 - 触发警报
                    _isAlarm = true;
                }
                else
                {
                    // 还在 30 秒内 - 等待中
                    _isAlarm = false;
                }
            }
        }        public bool IsAlarm => _isAlarm;
        public int FileCount => _lastFileCount;

        public void Stop()
        {
            _isRunning = false;
        }
    }
}