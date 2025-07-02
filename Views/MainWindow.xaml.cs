using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using BonsaiGotchiGame.ViewModels;
using BonsaiGotchiGame.Services;
using BonsaiGotchiGame.Models;
using BonsaiGotchiGame.Views;
using System.ComponentModel;

namespace BonsaiGotchiGame
{
    public partial class MainWindow : Window, IDisposable
    {
        // Fields
        private MainViewModel? _viewModel;
        private SpriteAnimator? _spriteAnimator;
        private BackgroundSpriteAnimator? _backgroundSpriteAnimator;
        private TextBlock? _statusTextBlock;
        private bool _disposed = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Find the status text block for animations
                _statusTextBlock = FindName("StatusTextBlock") as TextBlock;

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

                // Animate the controls to fade in
                AnimateInitialLoad();

                // Set up journal expander animations
                var journalExpander = FindName("JournalExpander") as Expander;
                if (journalExpander != null)
                {
                    journalExpander.Expanded += ToggleJournalExpander;
                }

                // Update background for current time
                if (_viewModel?.Bonsai != null)
                {
                    _backgroundSpriteAnimator?.UpdateBackground(_viewModel.Bonsai.GameHour);
                }

                // Subscribe to sprite state changes
                if (_viewModel != null)
                {
                    _viewModel.BonsaiStateChanged += ViewModel_BonsaiStateChanged;
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;

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

        private void ToggleJournalExpander(object? sender, RoutedEventArgs e)
        {
            var expander = sender as Expander;
            if (expander != null)
            {
                if (expander.IsExpanded)
                {
                    // Journal is expanding
                    var journalList = FindName("JournalEntriesList") as ItemsControl;
                    if (journalList != null)
                    {
                        AnimationService.FadeInElement(journalList, TimeSpan.FromMilliseconds(500));
                    }
                }
            }
        }

        private void AnimateInitialLoad()
        {
            try
            {
                // Find all the main elements to animate with proper null safety
                UIElement?[] elements = new UIElement?[5];
                elements[0] = FindName("HeaderBar") as UIElement;
                elements[1] = FindName("StatsPanel") as UIElement;
                elements[2] = FindName("BonsaiDisplayArea") as UIElement;
                elements[3] = FindName("ActionButtonsPanel") as UIElement;
                elements[4] = FindName("SaveButtonPanel") as UIElement;

                // Stagger the animations
                for (int i = 0; i < elements.Length; i++)
                {
                    if (elements[i] != null)
                    {
                        AnimationService.FadeInElement(
                            elements[i],
                            TimeSpan.FromMilliseconds(300 + (i * 100)));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AnimateInitialLoad: {ex.Message}");
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "StatusMessage" && _statusTextBlock != null)
            {
                AnimationService.AnimateStatusMessage(_statusTextBlock);
            }
        }

        private void ViewModel_BonsaiStateChanged(object? sender, BonsaiState state)
        {
            _spriteAnimator?.UpdateAnimation(state);

            // Animate status message
            if (_statusTextBlock != null)
            {
                AnimationService.AnimateStatusMessage(_statusTextBlock);
            }

            // Check for critical states and add visual feedback
            if (state == BonsaiState.Wilting || state == BonsaiState.Unhealthy || state == BonsaiState.Thirsty)
            {
                // Find the appropriate stat to pulse based on state
                UIElement? elementToPulse = null;

                if (state == BonsaiState.Wilting)
                {
                    elementToPulse = this.FindName("HealthStatPanel") as UIElement;
                }
                else if (state == BonsaiState.Thirsty)
                {
                    elementToPulse = this.FindName("WaterStatPanel") as UIElement;
                }
                else if (state == BonsaiState.Unhealthy)
                {
                    elementToPulse = this.FindName("GrowthStatPanel") as UIElement;
                }

                if (elementToPulse != null)
                {
                    AnimationService.PulseElement(elementToPulse, TimeSpan.FromSeconds(3));
                }
            }
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
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
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
                // Animate the button click - safely cast sender
                AnimationService.AnimateButtonClick(sender as Button);

                _viewModel?.Water();

                // Animate the water stat if we can find it
                var waterBar = this.FindName("WaterProgressBar") as ProgressBar;
                if (waterBar != null && _viewModel?.Bonsai != null)
                {
                    AnimationService.AnimateProgressChange(waterBar,
                        waterBar.Value, _viewModel.Bonsai.Water);
                }
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
                // Animate the button click - safely cast sender
                AnimationService.AnimateButtonClick(sender as Button);

                _viewModel?.Prune();

                // Animate the growth stat if we can find it
                var growthBar = this.FindName("GrowthProgressBar") as ProgressBar;
                if (growthBar != null && _viewModel?.Bonsai != null)
                {
                    AnimationService.AnimateProgressChange(growthBar,
                        growthBar.Value, _viewModel.Bonsai.Growth);
                }
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
                // Animate the button click - safely cast sender
                AnimationService.AnimateButtonClick(sender as Button);

                _viewModel?.Rest();

                // Animate the energy stat if we can find it
                var energyBar = this.FindName("EnergyProgressBar") as ProgressBar;
                if (energyBar != null && _viewModel?.Bonsai != null)
                {
                    AnimationService.AnimateProgressChange(energyBar,
                        energyBar.Value, _viewModel.Bonsai.Energy);
                }
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
                // Animate the button click - safely cast sender
                AnimationService.AnimateButtonClick(sender as Button);

                _viewModel?.Fertilize();

                // Animate the health stat if we can find it
                var healthBar = this.FindName("HealthProgressBar") as ProgressBar;
                if (healthBar != null && _viewModel?.Bonsai != null)
                {
                    AnimationService.AnimateProgressChange(healthBar,
                        healthBar.Value, _viewModel.Bonsai.Health);
                }
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
                // Animate the button click - safely cast sender
                AnimationService.AnimateButtonClick(sender as Button);

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
                // Animate the button click - safely cast sender
                AnimationService.AnimateButtonClick(sender as Button);

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