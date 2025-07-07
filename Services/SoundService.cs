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
        private readonly object _soundLock = new object(); // Add lock for thread safety

        private float _soundEffectVolume = 1.0f;
        private float _musicVolume = 0.5f;
        private bool _soundEnabled = true;
        private bool _musicEnabled = true;
        private bool _disposed = false;
        private string? _currentMusicPath = null;

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
                    try
                    {
                        _backgroundMusicPlayer.Volume = _musicVolume;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error setting music volume: {ex.Message}");
                    }
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
                    try
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
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error toggling music state: {ex.Message}");
                    }
                }
            }
        }

        public SoundService()
        {
            try
            {
                _backgroundMusicPlayer = new MediaPlayer();

                // Initialize with default volume
                if (_backgroundMusicPlayer != null)
                {
                    _backgroundMusicPlayer.Volume = _musicVolume;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing SoundService: {ex.Message}");
                _backgroundMusicPlayer = null;
            }
        }

        public void LoadSoundEffect(string soundName, string filePath)
        {
            if (_disposed) return;
            if (string.IsNullOrEmpty(soundName) || string.IsNullOrEmpty(filePath)) return;

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Sound file not found: {filePath}");
                    return;
                }

                lock (_soundLock)
                {
                    if (_soundEffects.ContainsKey(soundName))
                    {
                        _soundEffects[soundName]?.Dispose();
                        _soundEffects.Remove(soundName);
                    }

#pragma warning disable CA1416 // Validate platform compatibility
                    var soundPlayer = new SoundPlayer(filePath);
                    _soundEffects[soundName] = soundPlayer;
                    soundPlayer.LoadAsync();
#pragma warning restore CA1416
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sound effect '{soundName}': {ex.Message}");
            }
        }

        public void PlaySoundEffect(string soundName)
        {
            if (_disposed || !_soundEnabled) return;
            if (string.IsNullOrEmpty(soundName)) return;

            lock (_soundLock)
            {
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
        }

        public void LoadBackgroundMusic(string filePath)
        {
            if (_disposed) return;
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Music file not found: {filePath}");
                    return;
                }

                if (_backgroundMusicPlayer == null)
                    _backgroundMusicPlayer = new MediaPlayer();

                // Store the current music path for potential reloading
                _currentMusicPath = filePath;

                // Stop any current playback and clean up
                try
                {
                    _backgroundMusicPlayer.Stop();
                    _backgroundMusicPlayer.Close();
                }
                catch { /* Ignore errors during cleanup */ }

                _backgroundMusicPlayer.Open(new Uri(filePath, UriKind.RelativeOrAbsolute));
                _backgroundMusicPlayer.Volume = _musicVolume;
                _backgroundMusicPlayer.MediaEnded += BackgroundMusicPlayer_MediaEnded;
                _backgroundMusicPlayer.MediaFailed += BackgroundMusicPlayer_MediaFailed;
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

        private void BackgroundMusicPlayer_MediaFailed(object? sender, ExceptionEventArgs e)
        {
            Console.WriteLine($"Background music playback failed: {e.ErrorException.Message}");

            // Try to reload the music if we have a path
            if (!string.IsNullOrEmpty(_currentMusicPath) && File.Exists(_currentMusicPath))
            {
                try
                {
                    // Wait a moment before attempting reload
                    System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => {
                        if (!_disposed && _backgroundMusicPlayer != null)
                        {
                            _backgroundMusicPlayer.Close();
                            _backgroundMusicPlayer.Open(new Uri(_currentMusicPath, UriKind.RelativeOrAbsolute));
                            if (_musicEnabled)
                            {
                                _backgroundMusicPlayer.Play();
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to reload music: {ex.Message}");
                }
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

                // Try to recover from error
                if (!string.IsNullOrEmpty(_currentMusicPath))
                {
                    try
                    {
                        LoadBackgroundMusic(_currentMusicPath);
                        _backgroundMusicPlayer?.Play();
                    }
                    catch { /* Ignore if recovery fails */ }
                }
            }
        }

        public void PauseBackgroundMusic()
        {
            if (_disposed || _backgroundMusicPlayer == null) return;

            try
            {
                _backgroundMusicPlayer.Pause();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pausing background music: {ex.Message}");
            }
        }

        public void StopBackgroundMusic()
        {
            if (_disposed || _backgroundMusicPlayer == null) return;

            try
            {
                _backgroundMusicPlayer.Stop();
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
                            _backgroundMusicPlayer.MediaFailed -= BackgroundMusicPlayer_MediaFailed;
                            _backgroundMusicPlayer.Stop();
                            _backgroundMusicPlayer.Close();
                            _backgroundMusicPlayer = null;
                        }

                        lock (_soundLock)
                        {
                            if (_soundEffects != null)
                            {
                                foreach (var soundPlayer in _soundEffects.Values)
                                {
                                    if (soundPlayer != null)
                                    {
#pragma warning disable CA1416 // Validate platform compatibility
                                        soundPlayer.Dispose();
#pragma warning restore CA1416
                                    }
                                }

                                _soundEffects.Clear();
                            }
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