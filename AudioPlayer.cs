#nullable enable
using System;
using System.IO;
using System.Media;

namespace WebUIMonitor
{
    public class AudioPlayer
    {
        private SoundPlayer? _player;
        private bool _isPlaying = false;

        public AudioPlayer(string audioPath)
        {
            if (File.Exists(audioPath))
            {
                try
                {
                    _player = new SoundPlayer(audioPath);
                    Console.WriteLine($"✓ 音频文件已加载: {audioPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ 音频加载失败: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"✗ 音频文件不存在: {audioPath}");
            }
        }

        public void Play()
        {
            if (_player == null || _isPlaying) return;

            try
            {
                _isPlaying = true;
                Console.WriteLine("▶ 开始循环播放音频");
                _player.PlayLooping();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 播放失败: {ex.Message}");
                _isPlaying = false;
            }
        }        public void Stop()
        {
            if (_player == null) return;
            
            try
            {
                _player.Stop();
                _isPlaying = false;
            }
            catch { }
        }
    }
}

