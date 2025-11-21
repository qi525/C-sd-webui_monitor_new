using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUIMonitor
{
    /// <summary>
    /// 文件监控器 - 监控指定文件夹的文件变化
    /// 
    /// 报警逻辑说明：
    /// - 只有当文件数量【长期不变】（超过30秒）才报警
    /// - 增加、减少都视为正常变化，不报警
    /// 
    /// 原因：
    /// 1. 用户可能会归档/分类文件，导致数量减少
    /// 2. AI 出图时会不断增加文件，但如果停止，文件数就保持不变
    /// 3. 只有文件数保持不变超过30秒，才能说明任务已停止
    /// 
    /// 实现方式：
    /// - 每次检查都【实时计算路径】，适配有/无日期子文件夹的场景
    /// - 每 3 秒检查一次（可配置）
    /// - 所有逻辑均为实时计算，无需手动触发重置
    /// </summary>
    public class FileMonitor
    {
        private Func<string> _pathProvider; // 改用委托，每次实时获取路径
        private bool _isAlarm;
        private int _lastFileCount = -1;
        private DateTime _lastChangeTime = DateTime.Now;
        private bool _isRunning = false;
        private string _currentPath; // 保存当前监控的路径，供外部查询
        private readonly object _lockObject = new object(); // 线程安全锁

        /// <summary>设置路径提供器（每次都会实时计算路径）</summary>
        public void SetPathProvider(Func<string> pathProvider) => _pathProvider = pathProvider;

        public void Start() => _ = Task.Run(() => { _isRunning = true; while (_isRunning) { CheckFileCount(); Thread.Sleep(3000); } });

        private void CheckFileCount()
        {
            // 【核心改进】每次检查时都实时获取路径
            string currentPath = _pathProvider?.Invoke();
            if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath)) 
            { 
                lock (_lockObject) { _lastFileCount = 0; _currentPath = currentPath; }
                return; 
            }
            
            int count = Directory.GetFiles(currentPath).Length;
            
            lock (_lockObject)
            {
                _currentPath = currentPath; // 【关键】同时更新路径
                
                if (_lastFileCount == -1) 
                { 
                    _lastFileCount = count; 
                    _lastChangeTime = DateTime.Now; 
                    return; 
                }
                
                // 【改进的报警逻辑】
                // 只有当文件数量保持不变超过30秒才报警
                if (count != _lastFileCount) 
                { 
                    // 文件数量有变化（增加或减少），更新计数和时间戳
                    _lastFileCount = count; 
                    _lastChangeTime = DateTime.Now; 
                    _isAlarm = false;  // 有变化就清除报警
                    return; 
                }
                
                // 文件数量没有变化，检查是否超过30秒
                _isAlarm = (int)(DateTime.Now - _lastChangeTime).TotalSeconds >= 30;
            }
        }

        public bool IsAlarm 
        { 
            get { lock (_lockObject) { return _isAlarm; } } 
        }
        public int FileCount 
        { 
            get { lock (_lockObject) { return _lastFileCount; } } 
        }
        public string CurrentPath 
        { 
            get { lock (_lockObject) { return _currentPath; } } 
        }
        public void Stop() => _isRunning = false;
    }
}