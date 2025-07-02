using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using BonsaiGotchiGame.ViewModels;
using BonsaiGotchiGame.Services;
using BonsaiGotchiGame.Models;
using BonsaiGotchiGame.Views;
using System.ComponentModel;

namespace BonsaiGotchiGame
{
    public partial class MainWindow : Window, IDisposable
    {
        // Changed from readonly to regular fields since they're assigned in the Loaded event
        private MainViewModel? _viewModel;
        private SpriteAnimator? _spriteAnimator;
        private BackgroundSpriteAnimator? _backgroundSpriteAnimator;
        private bool _disposed = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Ensure window is visible and positioned on-screen
                EnsureWindowIsOnScreen();

                // Verify image directory exists
                CheckImageDirectory();

                // Create view model with exception handling
                try
                {
                    _viewModel = new MainViewModel();
                    DataContext = _viewModel;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error initializing view model: {ex.Message}\n\nStack trace: {ex.StackTrace}",
                        "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _viewModel = new MainViewModel(true); // Create with safe mode
                    DataContext = _viewModel;
                }

                // Initialize sprite animators after controls are loaded
                this.Loaded += MainWindow_Loaded;

                // Add closing event handler
                this.Closing += MainWindow_Closing;

                // Explicitly set window to be visible
                this.Visibility = Visibility.Visible;
                this.Show();
                this.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A critical error occurred during startup: {ex.Message}\n\nStack trace: {ex.StackTrace}",
                    "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initialize bonsai sprite animation
                _spriteAnimator = new SpriteAnimator(BonsaiImage);
                
                // Initialize background sprite animation
                _backgroundSpriteAnimator = new BackgroundSpriteAnimator(BackgroundImage);
                
                // Update background for current time - with null check
                if (_viewModel?.Bonsai != null)
                {
                    _backgroundSpriteAnimator?.UpdateBackground(_viewModel.Bonsai.GameHour);
                }
                
                // Subscribe to sprite state changes
                if (_viewModel != null)
                {
                    _viewModel.BonsaiStateChanged += ViewModel_BonsaiStateChanged;

                    // Initial animation based on current state
                    if (_viewModel.Bonsai != null && _spriteAnimator != null)
                    {
                        _spriteAnimator.UpdateAnimation(_viewModel.Bonsai.CurrentState);
                    }
                    else if (_spriteAnimator != null)
                    {
                        // Default animation if bonsai is null
                        _spriteAnimator.UpdateAnimation(BonsaiState.Idle);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing animation: {ex.Message}",
                    "Animation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ViewModel_BonsaiStateChanged(object? sender, BonsaiState state)
        {
            _spriteAnimator?.UpdateAnimation(state);
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                // Save game data before closing
                _viewModel?.SaveBonsai();

                // Clean up resources
                CleanupResources();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during window closing: {ex.Message}");
            }
        }

        private void CleanupResources()
        {
            // Unsubscribe from events
            if (_viewModel != null)
            {
                _viewModel.BonsaiStateChanged -= ViewModel_BonsaiStateChanged;
            }

            // Stop animation timers
            _spriteAnimator?.Stop();

            // Clean up view model resources
            if (_viewModel is IDisposable disposableViewModel)
            {
                disposableViewModel.Dispose();
            }

            // Clean up sprite animator resources
            if (_spriteAnimator is IDisposable disposableSpriteAnimator)
            {
                disposableSpriteAnimator.Dispose();
            }
            
            // Clean up background sprite animator
            _backgroundSpriteAnimator?.Dispose();
        }

        private static void CheckImageDirectory()
        {
            try
            {
                // Check relative path for images
                string imagesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
                if (!Directory.Exists(imagesDirectory))
                {
                    MessageBox.Show($"Images directory not found at: {imagesDirectory}\n\nThe application may not display sprites correctly.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    // Check for specific image files
                    string idleImagePath = Path.Combine(imagesDirectory, "idle.png");
                    if (!File.Exists(idleImagePath))
                    {
                        MessageBox.Show($"Default image not found: {idleImagePath}\n\nThe application may not display sprites correctly.",
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                
                // Also check backgrounds directory
                string backgroundsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Backgrounds");
                if (!Directory.Exists(backgroundsDirectory))
                {
                    MessageBox.Show($"Backgrounds directory not found at: {backgroundsDirectory}\n\nThe application may not display backgrounds correctly.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking image directory: {ex.Message}");
            }
        }

        private void EnsureWindowIsOnScreen()
        {
            // Set reasonable default window position and size
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Default size if not already set
            if (this.Height <= 0) this.Height = 600;
            if (this.Width <= 0) this.Width = 400;

            // Handle case where window might be off-screen
            this.Loaded += (s, e) =>
            {
                try
                {
                    var workingArea = SystemParameters.WorkArea;

                    // If window is completely off-screen, center it
                    if (this.Left < 0 || this.Left + this.Width > workingArea.Width ||
                        this.Top < 0 || this.Top + this.Height > workingArea.Height)
                    {
                        this.Left = (workingArea.Width - this.Width) / 2;
                        this.Top = (workingArea.Height - this.Height) / 2;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error positioning window: {ex.Message}");
                    // Fallback to center position
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            };

            // Make sure window state is normal (not minimized)
            this.WindowState = WindowState.Normal;
        }

        // Event handler for the Water button click
        private void Water_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.Water();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error watering bonsai: {ex.Message}",
                    "Operation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler for the Prune button click
        private void Prune_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.Prune();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pruning bonsai: {ex.Message}",
                    "Operation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler for the Rest button click
        private void Rest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.Rest();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resting bonsai: {ex.Message}",
                    "Operation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler for the Fertilize button click
        private void Fertilize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.Fertilize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fertilizing bonsai: {ex.Message}",
                    "Operation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler for the Settings button click
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and show settings window
                var settingsWindow = new SettingsWindow
                {
                    Owner = this
                };

                // Show the window and check if user saved changes
                if (settingsWindow.ShowDialog() == true)
                {
                    // Apply settings changes
                    _viewModel?.UpdateSettings();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening settings: {ex.Message}",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event handler for the Save Game button click
        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.SaveBonsai();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving game: {ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    // Dispose managed resources
                    CleanupResources();
                }

                // Free unmanaged resources

                _disposed = true;
            }
        }

        ~MainWindow()
        {
            Dispose(false);
        }
        #endregion
    }
}