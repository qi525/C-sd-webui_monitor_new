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
            if (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath))
                _player = new SoundPlayer(audioPath);
        }

        public void Play()
        {
            if (_player != null && !_isPlaying)
            {
                _isPlaying = true;
                _player.PlayLooping();
            }
        }

        public void Stop()
        {
            if (_player == null) return;
            _player.Stop();
            _isPlaying = false;
        }
    }
}

