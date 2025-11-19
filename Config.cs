using System;
using System.IO;

namespace WebUIMonitor
{
    public class Config
    {
        public static string GetWebUIOutputPath()
        {
            // 遍历所有硬盘，自动查找 stable-diffusion-webui\outputs
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

            // 如果都不存在，返回默认路径
            return @"C:\stable-diffusion-webui\outputs";
        }

        public static string GetAudioPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alarm.wav");
        }
    }
}
