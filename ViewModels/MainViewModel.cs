using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BonsaiGotchiGame.Models;
using BonsaiGotchiGame.Services;
using System.Threading.Tasks;
using System.Threading;

namespace BonsaiGotchiGame.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private Bonsai? _bonsai;
        private DispatcherTimer? _gameTimer;
        private DispatcherTimer? _autoSaveTimer;
        private string _currentSpritePath = string.Empty;
        private string _currentBackgroundPath = string.Empty;
        private BitmapImage? _currentBackgroundImage;
        private string _statusMessage = string.Empty;
        private bool _isInteractionEnabled;
        private SoundService? _soundService;
        private SaveLoadService? _saveLoadService;
        private BackgroundService? _backgroundService;
        private BackgroundSpriteAnimator? _backgroundSpriteAnimator;
        private bool _safeMode;
        private bool _disposed = false;
        private readonly object _updateLock = new object(); // Add lock for thread safety
        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1); // Add lock for save operations

        // Add event for bonsai state changes with nullable declaration
        public event EventHandler<BonsaiState>? BonsaiStateChanged;

        public Bonsai? Bonsai
        {
            get => _bonsai;
            set
            {
                if (_bonsai != null)
                {
                    // Unsubscribe from old bonsai's PropertyChanged event
                    _bonsai.PropertyChanged -= Bonsai_PropertyChanged;
                }

                _bonsai = value;

                if (_bonsai != null)
                {
                    // Subscribe to new bonsai's PropertyChanged event
                    _bonsai.PropertyChanged += Bonsai_PropertyChanged;
                }

                OnPropertyChanged();

                // Update background when bonsai changes
                UpdateBackground();
            }
        }

        public string CurrentSpritePath
        {
            get => _currentSpritePath;
            set { _currentSpritePath = value; OnPropertyChanged(); }
        }

        public string CurrentBackgroundPath
        {
            get => _currentBackgroundPath;
            set { _currentBackgroundPath = value; OnPropertyChanged(); }
        }

        public BitmapImage? CurrentBackgroundImage
        {
            get => _currentBackgroundImage;
            set { _currentBackgroundImage = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsInteractionEnabled
        {
            get => _isInteractionEnabled;
            set { _isInteractionEnabled = value; OnPropertyChanged(); }
        }

        // Constructor with optional safe mode parameter
        public MainViewModel(bool safeMode = false)
        {
            _safeMode = safeMode;

            try
            {
                // Create services
                _saveLoadService = new SaveLoadService();
                _soundService = new SoundService();
                _backgroundService = new BackgroundService();

                // Create a default bonsai in case loading fails
                _bonsai = new Bonsai();

                // Initialize background
                UpdateBackground();

                // Only attempt to load if not in safe mode
                if (!_safeMode)
                {
                    // Attempt to load existing bonsai or create new one
                    LoadBonsai();
                }
                else
                {
                    StatusMessage = "Running in safe mode with a new bonsai";
                }

                // Set up interaction enabled state
                IsInteractionEnabled = true;

                // Initialize sound effects
                if (!_safeMode)
                {
                    InitializeSounds();
                }

                // Set up game timer to update every second
                _gameTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _gameTimer.Tick += GameTimerTick;
                _gameTimer.Start();

                // Set up auto-save timer
                _autoSaveTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(GameSettings.Instance?.AutoSaveInterval ?? 5)
                };
                _autoSaveTimer.Tick += AutoSaveTimerTick;

                if ((GameSettings.Instance?.EnableAutoSave ?? true) && !_safeMode)
                {
                    _autoSaveTimer.Start();
                }

                // Set a default path for the Image control (this won't be used for animation)
                CurrentSpritePath = "/Assets/Images/idle.png";
                UpdateStatusMessage();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error initializing: " + ex.Message;

                // Create minimum viable state
                _bonsai = new Bonsai();
                IsInteractionEnabled = true;
                CurrentSpritePath = "/Assets/Images/idle.png";
                CurrentBackgroundPath = "/Assets/Backgrounds/morning.png"; // Changed to PNG
            }
        }

        public void InitializeBackgroundAnimator(System.Windows.Controls.Image backgroundImageControl)
        {
            try
            {
                if (backgroundImageControl == null)
                    throw new ArgumentNullException(nameof(backgroundImageControl));

                // Dispose of existing animator if it exists
                if (_backgroundSpriteAnimator != null)
                {
                    _backgroundSpriteAnimator.Dispose();
                }

                _backgroundSpriteAnimator = new BackgroundSpriteAnimator(backgroundImageControl);
                UpdateBackground();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing background animator: {ex.Message}");
                StatusMessage = "Error initializing background";
            }
        }

        // Update the LoadBonsai method or equivalent in MainWindow.cs
        // Update the LoadBonsai method or equivalent in MainViewModel.cs
        private void LoadBonsai()
        {
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var loadedBonsai = await _saveLoadService!.LoadBonsaiAsync();

                        // Update UI on the UI thread
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            if (_disposed) return;

                            Bonsai = loadedBonsai;
                            StatusMessage = "Bonsai loaded successfully!";

                            // Notify UI that inventory has been loaded
                            SyncInventoryWithUI();

                            UpdateBackground(); // Update background after loading bonsai
                        });
                    }
                    catch (Exception ex)
                    {
                        // Update UI on the UI thread
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            if (_disposed) return;

                            Bonsai = new Bonsai();
                            StatusMessage = "Created a new bonsai!";
                            UpdateBackground(); // Update background after creating new bonsai

                            Console.WriteLine($"Failed to load bonsai: {ex.Message}");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Bonsai = new Bonsai();
                StatusMessage = "Error loading bonsai: " + ex.Message;
                UpdateBackground(); // Update background after fallback to new bonsai
            }
        }

        // Method to sync inventory with UI
        public void SyncInventoryWithUI()
        {
            if (Bonsai == null) return;

            // Notify UI that inventory items have changed
            OnPropertyChanged(nameof(Bonsai.Inventory));

            // Log inventory items for debugging
            foreach (var item in Bonsai?.Inventory?.Items ?? new Dictionary<string, int>())
            {
                Console.WriteLine($"Inventory item: {item.Key}, Count: {item.Value}");
            }
        }

        // Helper method to load a bonsai with shop manager
        public async Task<(Bonsai Bonsai, List<string> UnlockedItems)> LoadBonsaiWithShopManagerAsync(ShopManager shopManager)
        {
            if (_saveLoadService == null)
                return (new Bonsai(), new List<string>());

            try
            {
                return await _saveLoadService.LoadBonsaiAsync(shopManager);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading bonsai with shop manager: {ex.Message}");
                return (new Bonsai(), new List<string>());
            }
        }

        private void UpdateBackground()
        {
            try
            {
                if (_bonsai != null)
                {
                    // Use background sprite animator if available
                    if (_backgroundSpriteAnimator != null)
                    {
                        _backgroundSpriteAnimator.UpdateBackground(_bonsai.GameHour);
                    }
                    // Legacy background loading as fallback
                    else if (_backgroundService != null)
                    {
                        CurrentBackgroundImage = _backgroundService.GetBackgroundImage(_bonsai.GameHour);
                        CurrentBackgroundPath = _backgroundService.GetBackgroundPath(_bonsai.GameHour);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating background: {ex.Message}");
            }
        }

        private void InitializeSounds()
        {
            if (_soundService == null)
                return;

            try
            {
                // Check if files exist before loading
                string[] soundFiles = {
                    "Assets/Sounds/water.wav",
                    "Assets/Sounds/prune.wav",
                    "Assets/Sounds/rest.wav",
                    "Assets/Sounds/fertilize.wav",
                    "Assets/Sounds/bloom.wav",
                    "Assets/Sounds/wilt.wav"
                };

                bool allFilesExist = true;
                foreach (var file in soundFiles)
                {
                    if (!System.IO.File.Exists(file))
                    {
                        allFilesExist = false;
                        Console.WriteLine($"Sound file not found: {file}");
                    }
                }

                if (!allFilesExist)
                {
                    Console.WriteLine("Some sound files are missing. Sound will be disabled.");
                    return;
                }

                _soundService.LoadSoundEffect("water", "Assets/Sounds/water.wav");
                _soundService.LoadSoundEffect("prune", "Assets/Sounds/prune.wav");
                _soundService.LoadSoundEffect("rest", "Assets/Sounds/rest.wav");
                _soundService.LoadSoundEffect("fertilize", "Assets/Sounds/fertilize.wav");
                _soundService.LoadSoundEffect("bloom", "Assets/Sounds/bloom.wav");
                _soundService.LoadSoundEffect("wilt", "Assets/Sounds/wilt.wav");

                if (GameSettings.Instance?.PlayMusic ?? true)
                {
                    if (System.IO.File.Exists("Assets/Sounds/background_music.mp3"))
                    {
                        _soundService.LoadBackgroundMusic("Assets/Sounds/background_music.mp3");
                        _soundService.PlayBackgroundMusic();
                    }
                }

                // Apply volume settings
                _soundService.SoundEffectVolume = GameSettings.Instance?.SoundVolume ?? 0.8f;
                _soundService.MusicVolume = GameSettings.Instance?.MusicVolume ?? 0.5f;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing sounds: {ex.Message}");
            }
        }

        private void Bonsai_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_disposed) return;

            try
            {
                // Check if the CurrentState property changed
                if (e.PropertyName == nameof(Bonsai.CurrentState) && _bonsai != null)
                {
                    // Notify subscribers about the state change
                    BonsaiStateChanged?.Invoke(this, _bonsai.CurrentState);
                    UpdateStatusMessage();
                }

                // Update UI when game time changes
                if ((e.PropertyName == nameof(Bonsai.GameHour) ||
                    e.PropertyName == nameof(Bonsai.GameMinute) ||
                    e.PropertyName == nameof(Bonsai.GameTimeDisplay) ||
                    e.PropertyName == nameof(Bonsai.GameDateDisplay)) && _bonsai != null)
                {
                    // Force update of time display properties
                    OnPropertyChanged(nameof(Bonsai));
                }

                // Update background when game hour changes
                if (e.PropertyName == nameof(Bonsai.GameHour) && _bonsai != null)
                {
                    UpdateBackground();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Bonsai_PropertyChanged: {ex.Message}");
            }
        }

        private void GameTimerTick(object? sender, EventArgs e)
        {
            if (_disposed) return;

            try
            {
                lock (_updateLock)
                {
                    Bonsai?.UpdateState();
                    UpdateStatusMessage();
                    CheckCriticalConditions();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in game timer: {ex.Message}");
            }
        }

        private void AutoSaveTimerTick(object? sender, EventArgs e)
        {
            if (_disposed) return;

            try
            {
                if (GameSettings.Instance?.EnableAutoSave ?? true)
                {
                    SaveBonsai();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in auto-save timer: {ex.Message}");
            }
        }

        private void UpdateStatusMessage()
        {
            if (_disposed || Bonsai == null)
            {
                StatusMessage = "Bonsai not initialized";
                return;
            }

            StatusMessage = Bonsai.CurrentState switch
            {
                BonsaiState.Idle => "Your bonsai is resting peacefully.",
                BonsaiState.Growing => "Your bonsai is growing nicely!",
                BonsaiState.Blooming => "Beautiful! Your bonsai is blooming.",
                BonsaiState.Sleeping => "Shh! Your bonsai is resting.",
                BonsaiState.Thirsty => "Your bonsai needs water!",
                BonsaiState.Unhealthy => "Oh no! Your bonsai is unhealthy. Apply fertilizer.",
                BonsaiState.Wilting => "Your bonsai is wilting. It needs care!",
                _ => "Your bonsai is doing fine."
            };
        }

        private void CheckCriticalConditions()
        {
            if (_disposed || Bonsai == null || _soundService == null) return;

            try
            {
                if (Bonsai.Health < 20)
                {
                    _soundService.PlaySoundEffect("wilt");
                }
                else if (Bonsai.Water < 10)
                {
                    _soundService.PlaySoundEffect("wilt");
                }
                else if (Bonsai.Growth > 90)
                {
                    _soundService.PlaySoundEffect("bloom");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking conditions: {ex.Message}");
            }
        }

        // Implement core interaction methods
        public void Water()
        {
            if (_disposed || Bonsai == null || !IsInteractionEnabled)
                return;

            lock (_updateLock)
            {
                Bonsai.GiveWater();
                _soundService?.PlaySoundEffect("water");
            }

            // Temporary disable interaction to prevent spam
            TemporarilyDisableInteraction();
        }

        public void Prune()
        {
            if (_disposed || Bonsai == null || !IsInteractionEnabled)
                return;

            lock (_updateLock)
            {
                Bonsai.Prune();
                _soundService?.PlaySoundEffect("prune");
            }

            // Temporary disable interaction to prevent spam
            TemporarilyDisableInteraction();
        }

        public void Rest()
        {
            if (_disposed || Bonsai == null || !IsInteractionEnabled)
                return;

            lock (_updateLock)
            {
                Bonsai.Rest();
                _soundService?.PlaySoundEffect("rest");
            }

            // Temporary disable interaction for longer
            TemporarilyDisableInteraction(5);
        }

        public void Fertilize()
        {
            if (_disposed || Bonsai == null || !IsInteractionEnabled)
                return;

            lock (_updateLock)
            {
                Bonsai.ApplyFertilizer();
                _soundService?.PlaySoundEffect("fertilize");
            }

            // Temporary disable interaction to prevent spam
            TemporarilyDisableInteraction();
        }

        public void CleanArea()
        {
            if (_disposed || Bonsai == null || !IsInteractionEnabled)
                return;

            lock (_updateLock)
            {
                Bonsai.CleanArea();
            }

            // Temporary disable interaction to prevent spam
            TemporarilyDisableInteraction();
        }

        public void Exercise()
        {
            if (_disposed || Bonsai == null || !IsInteractionEnabled)
                return;

            lock (_updateLock)
            {
                Bonsai.LightExercise();
            }

            // Temporary disable interaction to prevent spam
            TemporarilyDisableInteraction();
        }

        public void Train()
        {
            if (_disposed || Bonsai == null || !IsInteractionEnabled)
                return;

            lock (_updateLock)
            {
                Bonsai.IntenseTraining();
            }

            // Temporary disable interaction to prevent spam
            TemporarilyDisableInteraction();
        }

        public void Play()
        {
            if (_disposed || Bonsai == null || !IsInteractionEnabled)
                return;

            lock (_updateLock)
            {
                Bonsai.Play();
            }

            // Temporary disable interaction to prevent spam
            TemporarilyDisableInteraction();
        }

        public void Meditate()
        {
            if (_disposed || Bonsai == null || !IsInteractionEnabled)
                return;

            lock (_updateLock)
            {
                Bonsai.Meditate();
            }

            // Temporary disable interaction to prevent spam
            TemporarilyDisableInteraction();
        }

        private void TemporarilyDisableInteraction(int seconds = 2)
        {
            if (_disposed) return;

            IsInteractionEnabled = false;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(seconds));

                    if (_disposed) return;

                    // Fix for CS8602: Check for null before accessing Dispatcher
                    var dispatcher = Application.Current?.Dispatcher;
                    if (dispatcher != null)
                    {
                        await dispatcher.InvokeAsync(() =>
                        {
                            if (_disposed) return;
                            IsInteractionEnabled = true;
                        });
                    }
                    else
                    {
                        // Fallback if no dispatcher is available
                        IsInteractionEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error re-enabling interaction: {ex.Message}");

                    // Last resort - try to re-enable interaction
                    try
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            if (!_disposed)
                                IsInteractionEnabled = true;
                        });
                    }
                    catch
                    {
                        // Nothing more we can do
                    }
                }
            });
        }

        public void SaveBonsai()
        {
            if (_disposed || Bonsai == null || _saveLoadService == null)
                return;

            Task.Run(async () =>
            {
                try
                {
                    // Use SemaphoreSlim to prevent multiple concurrent save operations
                    await _saveLock.WaitAsync();

                    try
                    {
                        await _saveLoadService.SaveBonsaiAsync(Bonsai);

                        // Fix for CS8602: Check for null before accessing Dispatcher
                        var dispatcher = Application.Current?.Dispatcher;
                        if (dispatcher != null)
                        {
                            await dispatcher.InvokeAsync(() =>
                            {
                                if (_disposed) return;

                                StatusMessage = "Game saved!";
                            });

                            // Automatically clear the message after a short delay
                            await Task.Delay(2000);

                            // Only update if the status message hasn't been changed by something else
                            await dispatcher.InvokeAsync(() =>
                            {
                                if (_disposed) return;

                                if (StatusMessage == "Game saved!")
                                {
                                    UpdateStatusMessage();
                                }
                            });
                        }
                    }
                    finally
                    {
                        _saveLock.Release();
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        // Fix for CS8602: Check for null before accessing Dispatcher
                        var dispatcher = Application.Current?.Dispatcher;
                        if (dispatcher != null)
                        {
                            await dispatcher.InvokeAsync(() =>
                            {
                                if (!_disposed)
                                    StatusMessage = "Failed to save game!";
                            });
                        }
                    }
                    catch
                    {
                        // Last resort
                        Console.WriteLine($"Failed to update UI after save error: {ex.Message}");
                    }
                    Console.WriteLine($"Error saving bonsai: {ex.Message}");
                }
            });
        }

        public void UpdateSettings()
        {
            if (_disposed) return;

            try
            {
                // Update timers based on settings
                if (_autoSaveTimer != null)
                {
                    if (GameSettings.Instance?.EnableAutoSave ?? true)
                    {
                        _autoSaveTimer.Interval = TimeSpan.FromMinutes(GameSettings.Instance?.AutoSaveInterval ?? 5);
                        _autoSaveTimer.Start();
                    }
                    else
                    {
                        _autoSaveTimer.Stop();
                    }
                }

                // Update sound settings
                if (_soundService != null)
                {
                    _soundService.SoundEnabled = GameSettings.Instance?.PlaySounds ?? true;
                    _soundService.MusicEnabled = GameSettings.Instance?.PlayMusic ?? true;
                    _soundService.SoundEffectVolume = GameSettings.Instance?.SoundVolume ?? 0.8f;
                    _soundService.MusicVolume = GameSettings.Instance?.MusicVolume ?? 0.5f;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating settings: {ex.Message}");
                StatusMessage = "Error updating settings";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                    // Unsubscribe from events
                    if (_bonsai != null)
                    {
                        _bonsai.PropertyChanged -= Bonsai_PropertyChanged;
                    }

                    // Stop and clean up timers
                    if (_gameTimer != null)
                    {
                        _gameTimer.Stop();
                        _gameTimer.Tick -= GameTimerTick;
                        _gameTimer = null;
                    }

                    if (_autoSaveTimer != null)
                    {
                        _autoSaveTimer.Stop();
                        _autoSaveTimer.Tick -= AutoSaveTimerTick;
                        _autoSaveTimer = null;
                    }

                    // Dispose services
                    if (_soundService != null)
                    {
                        _soundService.Dispose();
                        _soundService = null;
                    }

                    if (_backgroundSpriteAnimator != null)
                    {
                        _backgroundSpriteAnimator.Dispose();
                        _backgroundSpriteAnimator = null;
                    }

                    // Clear other resources
                    _saveLoadService = null;
                    _backgroundService = null;
                    _bonsai = null;

                    // Release SemaphoreSlim
                    _saveLock.Dispose();
                }

                _disposed = true;
            }
        }

        ~MainViewModel()
        {
            Dispose(false);
        }
        #endregion
    }
}