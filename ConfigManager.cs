using System;
using System.IO;
using System.Text.Json;

#nullable enable

namespace WebUIMonitor
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private static MonitoringConfig? _config;
        private static MonitoringConfig GetConfig() => _config ??= JsonSerializer.Deserialize<MonitoringConfig>(File.ReadAllText(ConfigPath))!;
        public static string GetMonitoringPath() => GetConfig().MonitoringPath!;
        public static string GetAudioPath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GetConfig().AudioPath!);
    }

    public class MonitoringConfig
    {
        public string? MonitoringPath { get; set; }
        public string? AudioPath { get; set; }
        public bool AutoDetect { get; set; }
    }
}
