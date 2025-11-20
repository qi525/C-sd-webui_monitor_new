#nullable enable
using System;
using System.IO;
using System.Media;

namespace WebUIMonitor
{
    public class AudioPlayer
    {
        private readonly SoundPlayer? _player;
        private bool _isPlaying;

        public AudioPlayer(string? audioPath)
        {
            if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath))
            {
                Console.WriteLine($"✗ 音频文件不存在: {audioPath}");
                return;
            }

            _player = new SoundPlayer(audioPath);
            Console.WriteLine($"✓ 音频文件已加载: {audioPath}");
        }

        public void Play()
        {
            if (_player == null || _isPlaying) return;
            _isPlaying = true;
            Console.WriteLine("▶ 开始循环播放音频");
            _player.PlayLooping();
        }

        public void Stop()
        {
            if (_player == null) return;
            _player.Stop();
            _isPlaying = false;
        }
    }
}

