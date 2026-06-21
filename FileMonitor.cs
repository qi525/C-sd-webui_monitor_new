using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace WebUIMonitor
{
    /// <summary>
    /// 单个文件夹的监控状态
    /// </summary>
    public class FolderMonitorState
    {
        public string Path { get; set; } = "";
        public int CurrentFileCount { get; set; } = 0;
        public DateTime LastChangeTime { get; set; } = DateTime.Now;
        public bool IsAlarm { get; set; } = false;

        /// <summary>获取路径是否存在</summary>
        public bool IsPathExists() => !string.IsNullOrEmpty(Path) && Directory.Exists(Path);

        /// <summary>获取距离上次变化已过去多少秒</summary>
        public int GetSecondsSinceLastChange() => (int)(DateTime.Now - LastChangeTime).TotalSeconds;

        /// <summary>获取是否超过报警阈值（60秒无变化）</summary>
        public bool IsTimeoutThresholdReached(int thresholdSeconds = 60) => GetSecondsSinceLastChange() >= thresholdSeconds;

        /// <summary>获取路径的文件夹名称（用于显示）</summary>
        public string GetFolderName() => string.IsNullOrEmpty(Path) ? "" : System.IO.Path.GetFileName(Path);
    }

    /// <summary>
    /// 文件监控器 - 实时轮询监听多个文件夹
    ///
    /// 报警逻辑：
    /// - 任一文件夹文件数有变化 → 清除报警，重置倒计时
    /// - 所有文件夹60秒无变化 → 触发报警
    /// </summary>
    public class FileMonitor
    {
        // ==================== 私有字段 ====================
        private Func<List<string>> _pathsProvider;
        private List<FolderMonitorState> _folderStates = new List<FolderMonitorState>();
        private bool _isRunning = false;
        private int _intervalSeconds = 3;
        private int _alarmThresholdSeconds = 60;

        // 记录"最后一个文件夹停止活动"的时间点，用于倒计时
        private DateTime _lastActivityTime = DateTime.Now;

        // ==================== 配置方法 ====================
        public void SetPathProvider(Func<List<string>> pathProvider) => _pathsProvider = pathProvider;

        public void SetIntervalSeconds(int seconds) => _intervalSeconds = seconds;

        public void SetAlarmThresholdSeconds(int seconds) => _alarmThresholdSeconds = seconds;

        // ==================== 生命周期方法 ====================
        public void Start() => _ = Task.Run(async () =>
        {
            _isRunning = true;
            LogInfo("文件监控服务已启动");

            while (_isRunning)
            {
                try
                {
                    await MonitorAllPathsAsync();
                }
                catch (Exception ex)
                {
                    LogError($"监控循环异常: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds));
            }

            LogInfo("文件监控服务已停止");
        });

        public void Stop() => _isRunning = false;

        // ==================== 核心监控逻辑 ====================
        /// <summary>监控所有配置的路径</summary>
        private async Task MonitorAllPathsAsync()
        {
            var paths = _pathsProvider?.Invoke() ?? new List<string>();

            foreach (var path in paths)
            {
                await MonitorSinglePathAsync(path);
            }
        }

        /// <summary>监控单个路径</summary>
        private async Task MonitorSinglePathAsync(string path)
        {
            var folderState = GetOrCreateFolderState(path);

            if (!IsValidPath(path))
            {
                HandleInvalidPath(folderState, path);
                return;
            }

            folderState.Path = path;
            int fileCount = await GetFileCountAsync(path);

            if (HasFileCountChanged(folderState, fileCount))
            {
                HandleFileCountChanged(folderState, fileCount);
            }
        }

        /// <summary>异步获取文件数量</summary>
        private async Task<int> GetFileCountAsync(string path)
        {
            return await Task.Run(() =>
            {
                try { return Directory.GetFiles(path).Length; }
                catch { return 0; }
            });
        }

        /// <summary>检查路径是否有效</summary>
        private bool IsValidPath(string path) => !string.IsNullOrEmpty(path) && Directory.Exists(path);

        /// <summary>处理无效路径</summary>
        private void HandleInvalidPath(FolderMonitorState folderState, string path)
        {
            folderState.Path = path;
            folderState.CurrentFileCount = 0;
        }

        /// <summary>检查文件数是否有变化</summary>
        private bool HasFileCountChanged(FolderMonitorState folderState, int newCount)
            => newCount != folderState.CurrentFileCount;

        /// <summary>处理文件数变化（出图活动）</summary>
        private void HandleFileCountChanged(FolderMonitorState folderState, int newCount)
        {
            LogInfo($"{folderState.Path}: 文件数变化 {folderState.CurrentFileCount} → {newCount}");
            folderState.CurrentFileCount = newCount;
            folderState.LastChangeTime = DateTime.Now;
            folderState.IsAlarm = false;

            // 有出图活动，重置"最后活动时间"（从现在开始算60秒倒计时）
            _lastActivityTime = DateTime.Now;
        }

        // ==================== 状态管理 ====================
        /// <summary>获取或创建文件夹状态</summary>
        private FolderMonitorState GetOrCreateFolderState(string path)
        {
            var existing = _folderStates.Find(f => f.Path == path);
            if (existing != null) return existing;

            var newState = new FolderMonitorState { Path = path, LastChangeTime = DateTime.Now };
            _folderStates.Add(newState);
            return newState;
        }

        // ==================== 只读属性 ====================
        /// <summary>所有文件夹60秒无变化 → 报警（任一文件夹有活动则不报警）</summary>
        public bool IsAlarm => _folderStates.Count > 0 && _folderStates.All(f => f.IsTimeoutThresholdReached(_alarmThresholdSeconds));

        /// <summary>获取总文件数</summary>
        public int TotalFileCount => _folderStates.Sum(f => f.CurrentFileCount);

        /// <summary>获取监控间隔（秒）</summary>
        public int IntervalSeconds => _intervalSeconds;

        /// <summary>获取报警阈值（秒）</summary>
        public int AlarmThresholdSeconds => _alarmThresholdSeconds;

        /// <summary>获取所有文件夹状态（副本）</summary>
        public List<FolderMonitorState> GetAllFolderStates() => _folderStates.ToList();

        /// <summary>获取监控中的路径数量</summary>
        public int GetMonitoredFolderCount() => _folderStates.Count(f => f.IsPathExists());

        /// <summary>获取距离最后一个出图活动过去了多少秒</summary>
        public int SecondsSinceLastChange => (int)(DateTime.Now - _lastActivityTime).TotalSeconds;

        /// <summary>获取倒计时剩余秒数（返回 60 - 已过去秒数，或 -1 表示正常出图中，或 0 表示已触发）</summary>
        public int GetCountdownSeconds()
        {
            if (_folderStates.Count == 0) return -1;

            int elapsed = SecondsSinceLastChange;

            // 如果还有文件夹在活动（<60秒），返回 -1 表示正常出图中
            if (!_folderStates.All(f => f.IsTimeoutThresholdReached(_alarmThresholdSeconds)))
                return -1;

            // 所有文件夹都超时了，已触发
            return 0;
        }

        /// <summary>获取倒计时显示文本（用于UI）</summary>
        public string GetCountdownDisplay()
        {
            if (_folderStates.Count == 0) return "--";

            int elapsed = SecondsSinceLastChange;

            // 如果还有文件夹在活动（<60秒），显示倒计时
            if (!_folderStates.All(f => f.IsTimeoutThresholdReached(_alarmThresholdSeconds)))
            {
                int remaining = _alarmThresholdSeconds - elapsed;
                return remaining > 0 ? $"{remaining}秒" : "0秒";
            }

            // 所有文件夹都超时了，已触发
            return "触发中";
        }

        // ==================== 日志辅助 ====================
        private void LogInfo(string message) => System.Diagnostics.Debug.WriteLine($"[FileMonitor] {message}");
        private void LogWarning(string message) => System.Diagnostics.Debug.WriteLine($"[FileMonitor] ⚠️ {message}");
        private void LogError(string message) => System.Diagnostics.Debug.WriteLine($"[FileMonitor] ❌ {message}");
    }
}
