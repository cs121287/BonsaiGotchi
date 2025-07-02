using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows.Media;

namespace BonsaiGotchiGame.Services
{
    public class SoundService : IDisposable
    {
        private readonly Dictionary<string, SoundPlayer> _soundEffects = new Dictionary<string, SoundPlayer>();
        private MediaPlayer? _backgroundMusicPlayer;

        private float _soundEffectVolume = 1.0f;
        private float _musicVolume = 0.5f;
        private bool _soundEnabled = true;
        private bool _musicEnabled = true;
        private bool _disposed = false;

        public float SoundEffectVolume
        {
            get => _soundEffectVolume;
            set
            {
                _soundEffectVolume = Math.Clamp(value, 0f, 1f);
                // Note: SoundPlayer doesn't support volume control directly
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Math.Clamp(value, 0f, 1f);
                if (_backgroundMusicPlayer != null && !_disposed)
                {
                    _backgroundMusicPlayer.Volume = _musicVolume;
                }
            }
        }

        public bool SoundEnabled
        {
            get => _soundEnabled;
            set => _soundEnabled = value;
        }

        public bool MusicEnabled
        {
            get => _musicEnabled;
            set
            {
                _musicEnabled = value;
                if (_backgroundMusicPlayer != null && !_disposed)
                {
                    if (_musicEnabled)
                    {
                        _backgroundMusicPlayer.Play();
                    }
                    else
                    {
                        _backgroundMusicPlayer.Pause();
                    }
                }
            }
        }

        public SoundService()
        {
            _backgroundMusicPlayer = new MediaPlayer();

            // Initialize with default volume
            if (_backgroundMusicPlayer != null)
            {
                _backgroundMusicPlayer.Volume = _musicVolume;
            }
        }

        public void LoadSoundEffect(string soundName, string filePath)
        {
            if (_disposed) return;

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Sound file not found: {filePath}");
                    return;
                }

                if (_soundEffects.ContainsKey(soundName))
                {
                    _soundEffects[soundName]?.Dispose();
                }

#pragma warning disable CA1416 // Validate platform compatibility
                _soundEffects[soundName] = new SoundPlayer(filePath);
                _soundEffects[soundName].LoadAsync();
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sound effect '{soundName}': {ex.Message}");
            }
        }

        public void PlaySoundEffect(string soundName)
        {
            if (_disposed || !_soundEnabled || _soundEffects == null)
                return;

            if (!_soundEffects.ContainsKey(soundName))
            {
                Console.WriteLine($"Sound effect not found: {soundName}");
                return;
            }

            try
            {
#pragma warning disable CA1416 // Validate platform compatibility
                _soundEffects[soundName]?.Play();
#pragma warning restore CA1416
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound effect '{soundName}': {ex.Message}");
            }
        }

        public void LoadBackgroundMusic(string filePath)
        {
            if (_disposed) return;

            if (_backgroundMusicPlayer == null)
                _backgroundMusicPlayer = new MediaPlayer();

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Music file not found: {filePath}");
                    return;
                }

                _backgroundMusicPlayer.Open(new Uri(filePath, UriKind.RelativeOrAbsolute));
                _backgroundMusicPlayer.MediaEnded += BackgroundMusicPlayer_MediaEnded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading background music: {ex.Message}");
            }
        }

        private void BackgroundMusicPlayer_MediaEnded(object? sender, EventArgs e)
        {
            if (_disposed) return;

            try
            {
                if (_backgroundMusicPlayer != null)
                {
                    _backgroundMusicPlayer.Position = TimeSpan.Zero;
                    _backgroundMusicPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restarting background music: {ex.Message}");
            }
        }

        public void PlayBackgroundMusic()
        {
            if (_disposed || !_musicEnabled || _backgroundMusicPlayer == null)
                return;

            try
            {
                _backgroundMusicPlayer.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing background music: {ex.Message}");
            }
        }

        public void PauseBackgroundMusic()
        {
            if (_disposed) return;

            try
            {
                _backgroundMusicPlayer?.Pause();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pausing background music: {ex.Message}");
            }
        }

        public void StopBackgroundMusic()
        {
            if (_disposed) return;

            try
            {
                _backgroundMusicPlayer?.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping background music: {ex.Message}");
            }
        }

        #region IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean up resources
                    try
                    {
                        if (_backgroundMusicPlayer != null)
                        {
                            _backgroundMusicPlayer.MediaEnded -= BackgroundMusicPlayer_MediaEnded;
                            _backgroundMusicPlayer.Stop();
                            _backgroundMusicPlayer.Close();
                            _backgroundMusicPlayer = null;
                        }

                        if (_soundEffects != null)
                        {
                            foreach (var soundPlayer in _soundEffects.Values)
                            {
#pragma warning disable CA1416 // Validate platform compatibility
                                soundPlayer?.Dispose();
#pragma warning restore CA1416
                            }

                            _soundEffects.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing sound service: {ex.Message}");
                    }
                }

                _disposed = true;
            }
        }

        ~SoundService()
        {
            Dispose(false);
        }
        #endregion
    }
}