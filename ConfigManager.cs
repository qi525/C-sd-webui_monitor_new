using System;
using System.IO;
using System.Text.Json;

namespace WebUIMonitor
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static string GetMonitoringPath()
        {
            var config = JsonSerializer.Deserialize<MonitoringConfig>(File.ReadAllText(ConfigPath));
            return config.MonitoringPath;
        }

        public static string GetAudioPath()
        {
            var config = JsonSerializer.Deserialize<MonitoringConfig>(File.ReadAllText(ConfigPath));
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.AudioPath);
        }
    }

    public class MonitoringConfig
    {
        public string MonitoringPath { get; set; }
        public string AudioPath { get; set; }
        public bool AutoDetect { get; set; }
    }
}
