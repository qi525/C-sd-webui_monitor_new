#nullable enable
using System;
using System.IO;
using System.Media;

namespace WebUIMonitor
{
    public class AudioPlayer
    {
        private SoundPlayer? _player;
        private bool _isPlaying;

        public AudioPlayer(string? audioPath) => _player = (!string.IsNullOrWhiteSpace(audioPath) && File.Exists(audioPath)) ? new SoundPlayer(audioPath) : null;

        public void Play() { if (_player != null && !_isPlaying) { _isPlaying = true; _player.PlayLooping(); } }

        public void Stop() { if (_player != null) { _player.Stop(); _isPlaying = false; } }
    }
}