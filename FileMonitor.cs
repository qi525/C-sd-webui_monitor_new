using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    public class FileMonitor
    {
        private string _path;
        private bool _isAlarm = false;
        private int _lastFileCount = -1;
        private DateTime _lastFileChangeTime = DateTime.Now;
        private bool _isRunning = false;

        public void SetPath(string path) => _path = path;

        public void Start()
        {
            _isRunning = true;
            _ = Task.Run(() =>
            {
                while (_isRunning)
                {
                    CheckFileCount();
                    Thread.Sleep(3000);
                }
            });
        }

        private void CheckFileCount()
        {
            if (string.IsNullOrEmpty(_path) || !Directory.Exists(_path))
            {
                _lastFileCount = 0;
                return;
            }

            int currentCount = Directory.GetFiles(_path).Length;

            if (_lastFileCount == -1)
            {
                _lastFileCount = currentCount;
                _lastFileChangeTime = DateTime.Now;
                return;
            }

            if (currentCount > _lastFileCount)
            {
                _lastFileCount = currentCount;
                _lastFileChangeTime = DateTime.Now;
                _isAlarm = false;
            }
            else
            {
                int seconds = (int)(DateTime.Now - _lastFileChangeTime).TotalSeconds;
                _isAlarm = seconds >= 30;
            }
        }

        public bool IsAlarm => _isAlarm;
        public int FileCount => _lastFileCount;
        public void Stop() => _isRunning = false;
    }
}