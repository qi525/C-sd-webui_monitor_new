using System;
using System.IO;
using System.Text.Json;

namespace WebUIMonitor
{
    /// <summary>
    /// 配置管理器 - 从 config.json 读取配置
    /// 配置文件结构：
    /// {
    ///   "MonitoringPath": "C:\\stable-diffusion-webui\\outputs",
    ///   "AudioPath": "alarm.wav",
    ///   "AutoDetect": true
    /// }
    /// </summary>
    public class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "config.json"
        );

        private MonitoringConfig _config;

        public ConfigManager()
        {
            LoadConfig();
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    _config = JsonSerializer.Deserialize<MonitoringConfig>(json);
                    
                    System.Diagnostics.Debug.WriteLine($"[配置] 从文件加载成功: {ConfigPath}");
                    System.Diagnostics.Debug.WriteLine($"[配置] MonitoringPath: {_config.MonitoringPath}");
                    System.Diagnostics.Debug.WriteLine($"[配置] AutoDetect: {_config.AutoDetect}");
                }
                else
                {
                    // 如果配置文件不存在，创建默认配置
                    CreateDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[配置] 读取失败: {ex.Message}");
                CreateDefaultConfig();
            }
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private void CreateDefaultConfig()
        {
            try
            {
                _config = new MonitoringConfig
                {
                    MonitoringPath = GetDefaultPath(),
                    AudioPath = "alarm.wav",
                    AutoDetect = true
                };

                // 保存配置文件
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(ConfigPath, json);
                
                System.Diagnostics.Debug.WriteLine($"[配置] 创建默认配置文件: {ConfigPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[配置] 创建失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取默认路径 - 自动扫描磁盘
        /// </summary>
        private string GetDefaultPath()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (var drive in drives)
            {
                if (drive.IsReady)
                {
                    string path = Path.Combine(drive.Name, "stable-diffusion-webui", "outputs");
                    if (Directory.Exists(path))
                    {
                        return path;
                    }
                }
            }

            return @"C:\stable-diffusion-webui\outputs";
        }

        /// <summary>
        /// 获取监控路径
        /// </summary>
        public string GetMonitoringPath()
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigManager] GetMonitoringPath 被调用");
            System.Diagnostics.Debug.WriteLine($"[ConfigManager] _config.MonitoringPath = {_config.MonitoringPath}");
            System.Diagnostics.Debug.WriteLine($"[ConfigManager] Directory.Exists 结果 = {Directory.Exists(_config.MonitoringPath)}");
            
            // 检查配置的路径是否存在
            if (!Directory.Exists(_config.MonitoringPath))
            {
                System.Diagnostics.Debug.WriteLine($"[配置] 错误: 监控路径不存在: {_config.MonitoringPath}");
                
                // 提示用户修改 config.json
                string message = $"❌ 配置的监控路径不存在!\n\n" +
                    $"当前路径: {_config.MonitoringPath}\n\n" +
                    $"请编辑同级目录下的 config.json 文件，\n" +
                    $"修改 \"MonitoringPath\" 为正确的文件夹路径。\n\n" +
                    $"示例:\n" +
                    $"\"MonitoringPath\": \"C:\\\\stable-diffusion-webui\\\\outputs\"\n\n" +
                    $"修改后重启程序。";
                
                System.Windows.Forms.MessageBox.Show(message, "❌ 配置错误 - 程序无法启动", 
                    System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Error);
                
                // 退出程序
                System.Environment.Exit(1);
            }

            System.Diagnostics.Debug.WriteLine($"[ConfigManager] 返回路径: {_config.MonitoringPath}");
            return _config.MonitoringPath;
        }

        /// <summary>
        /// 获取音频文件路径
        /// </summary>
        public string GetAudioPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _config.AudioPath);
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(ConfigPath, json);
                
                System.Diagnostics.Debug.WriteLine($"[配置] 已保存: {ConfigPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[配置] 保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置监控路径
        /// </summary>
        public void SetMonitoringPath(string path)
        {
            _config.MonitoringPath = path;
            SaveConfig();
        }
    }

    /// <summary>
    /// 监控配置数据结构
    /// </summary>
    public class MonitoringConfig
    {
        public string MonitoringPath { get; set; }
        public string AudioPath { get; set; }
        public bool AutoDetect { get; set; }
    }
}
