using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    public class FileMonitor
    {
        private string _path;
        private bool _isAlarm;
        private int _lastFileCount = -1;
        private DateTime _lastChangeTime = DateTime.Now;
        private bool _isRunning;

        public void SetPath(string path) => _path = path;

        public void Start() => _ = Task.Run(() => { _isRunning = true; while (_isRunning) { CheckFileCount(); Thread.Sleep(3000); } });

        private void CheckFileCount()
        {
            if (string.IsNullOrEmpty(_path) || !Directory.Exists(_path)) { _lastFileCount = 0; return; }
            int count = Directory.GetFiles(_path).Length;
            if (_lastFileCount == -1) { _lastFileCount = count; _lastChangeTime = DateTime.Now; return; }
            if (count > _lastFileCount) { _lastFileCount = count; _lastChangeTime = DateTime.Now; _isAlarm = false; return; }
            _isAlarm = (int)(DateTime.Now - _lastChangeTime).TotalSeconds >= 30;
        }

        public bool IsAlarm => _isAlarm;
        public int FileCount => _lastFileCount;
        public void Stop() => _isRunning = false;
    }
}