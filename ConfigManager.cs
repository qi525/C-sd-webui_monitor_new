using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

#nullable enable

namespace WebUIMonitor
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private static MonitoringConfig? _config;
        private static MonitoringConfig GetConfig() => _config ??= JsonSerializer.Deserialize<MonitoringConfig>(File.ReadAllText(ConfigPath))!;
        
        public static string GetMonitoringPath() => GetConfig().MonitoringPath!;
        public static List<string> GetMonitoringPaths() => GetConfig().MonitoringPaths ?? new List<string>();
        public static string GetAudioPath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GetConfig().AudioPath!);
        public static int GetMonitoringIntervalSeconds() => GetConfig().MonitoringIntervalSeconds > 0 ? GetConfig().MonitoringIntervalSeconds : 3;
    }

    public class MonitoringConfig
    {
        public string? MonitoringPath { get; set; }
        public List<string>? MonitoringPaths { get; set; }
        public string? AudioPath { get; set; }
        public bool AutoDetect { get; set; }
        public int MonitoringIntervalSeconds { get; set; } = 3;
    }
}
