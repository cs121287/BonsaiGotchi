using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Services
{
    public class BackgroundSpriteAnimator : IDisposable
    {
        private readonly Image _targetImage;
        private bool _disposed = false;
        private readonly Dictionary<BackgroundService.TimeOfDay, BitmapSource> _backgroundSprites = [];
        private readonly object _spritesLock = new object(); // Add lock for thread safety

        public BackgroundSpriteAnimator(Image targetImage)
        {
            _targetImage = targetImage ?? throw new ArgumentNullException(nameof(targetImage));

            // Initialize backgrounds
            LoadBackgrounds();
        }

        private void LoadBackgrounds()
        {
            try
            {
                bool anyBackgroundsLoaded = false;

                lock (_spritesLock)
                {
                    anyBackgroundsLoaded =
                        LoadBackground(BackgroundService.TimeOfDay.Morning, "morning.png") ||
                        LoadBackground(BackgroundService.TimeOfDay.Afternoon, "afternoon.png") ||
                        LoadBackground(BackgroundService.TimeOfDay.Evening, "evening.png") ||
                        LoadBackground(BackgroundService.TimeOfDay.Night, "night.png");
                }

                // If no backgrounds loaded successfully, create defaults
                if (!anyBackgroundsLoaded)
                {
                    Console.WriteLine("No backgrounds loaded, creating default color backgrounds");
                    CreateDefaultBackgrounds();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading backgrounds: {ex.Message}");
                CreateDefaultBackgrounds();
            }
        }

        private void CreateDefaultBackgrounds()
        {
            if (_disposed) return;

            lock (_spritesLock)
            {
                try
                {
                    _backgroundSprites[BackgroundService.TimeOfDay.Morning] = CreateColorBackground(Colors.LightYellow);
                    _backgroundSprites[BackgroundService.TimeOfDay.Afternoon] = CreateColorBackground(Colors.LightGreen);
                    _backgroundSprites[BackgroundService.TimeOfDay.Evening] = CreateColorBackground(Colors.LightSalmon);
                    _backgroundSprites[BackgroundService.TimeOfDay.Night] = CreateColorBackground(Colors.MidnightBlue);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating default backgrounds: {ex.Message}");
                    // Last resort - at least try to create one background
                    try
                    {
                        _backgroundSprites[BackgroundService.TimeOfDay.Morning] = CreateColorBackground(Colors.White);
                    }
                    catch
                    {
                        // Nothing more we can do
                    }
                }
            }
        }

        private BitmapSource CreateColorBackground(Color color)
        {
            int width = 1024; // Use 1024x1024 as specified
            int height = 1024;

            var drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(color), null, new Rect(0, 0, width, height));
            }

            var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(drawingVisual);
            renderTarget.Freeze(); // Important for thread safety

            return renderTarget;
        }

        private bool LoadBackground(BackgroundService.TimeOfDay timeOfDay, string filename)
        {
            if (_disposed) return false;

            // Try multiple locations to find the background image
            string[] possiblePaths = {
                Path.Combine("Assets", "Backgrounds", filename),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Backgrounds", filename),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "source", "repos", "BonsaiGotchiGame", "Assets", "Backgrounds", filename)
            };

            foreach (string path in possiblePaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"Loading background from: {path}");

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(path, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Important for thread safety

                        lock (_spritesLock)
                        {
                            _backgroundSprites[timeOfDay] = bitmap;
                        }
                        Console.WriteLine($"Successfully loaded background for {timeOfDay}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load from {path}: {ex.Message}");
                }
            }

            // Try as embedded resource
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = $"BonsaiGotchiGame.Assets.Backgrounds.{filename}";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Important for thread safety

                    lock (_spritesLock)
                    {
                        _backgroundSprites[timeOfDay] = bitmap;
                    }
                    Console.WriteLine($"Loaded background from embedded resource");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load from embedded resource: {ex.Message}");
            }

            Console.WriteLine($"Could not find background for {timeOfDay}");
            return false;
        }

        public void UpdateBackground(int gameHour)
        {
            if (_disposed) return;

            try
            {
                var timeOfDay = BackgroundService.GetTimeOfDay(gameHour);
                BitmapSource? background = null; // Fixing CS8600: Explicitly mark as nullable

                // Get the appropriate background using thread-safe access
                lock (_spritesLock)
                {
                    if (_backgroundSprites.TryGetValue(timeOfDay, out BitmapSource? timeBackground) && timeBackground != null) // Fixing CS8600
                    {
                        background = timeBackground;
                    }
                    else
                    {
                        // Try any available background
                        foreach (var tod in Enum.GetValues<BackgroundService.TimeOfDay>())
                        {
                            if (_backgroundSprites.TryGetValue(tod, out BitmapSource? fallback) && fallback != null) // Fixing CS8600
                            {
                                background = fallback;
                                break;
                            }
                        }
                    }
                }

                // If we found a background, set it
                if (background != null)
                {
                    BitmapSource finalBackground = background; // Create a local non-nullable copy to use in the lambda
                    Application.Current?.Dispatcher.InvokeAsync(() => {
                        if (!_disposed && _targetImage != null)
                        {
                            _targetImage.Source = finalBackground;
                            Console.WriteLine($"Set background for time of day: {timeOfDay}");
                        }
                    }, System.Windows.Threading.DispatcherPriority.Render);
                }
                else
                {
                    // Last resort - create a default
                    var defaultBackground = CreateColorBackground(Colors.LightGreen);
                    defaultBackground.Freeze();

                    Application.Current?.Dispatcher.InvokeAsync(() => {
                        if (!_disposed && _targetImage != null)
                        {
                            _targetImage.Source = defaultBackground;
                            Console.WriteLine("Created emergency background");
                        }
                    }, System.Windows.Threading.DispatcherPriority.Render);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating background: {ex.Message}");

                // Last resort - create a very simple background
                try
                {
                    if (!_disposed && _targetImage != null)
                    {
                        var emergencyBackground = CreateColorBackground(Colors.LightGreen);
                        emergencyBackground.Freeze();

                        Application.Current?.Dispatcher.InvokeAsync(() => {
                            if (!_disposed && _targetImage != null)
                            {
                                _targetImage.Source = emergencyBackground;
                            }
                        }, System.Windows.Threading.DispatcherPriority.Render);
                    }
                }
                catch
                {
                    // Nothing more we can do
                }
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
                    // Clear resources
                    lock (_spritesLock)
                    {
                        _backgroundSprites.Clear();
                    }

                    // Clear image source reference if possible
                    Application.Current?.Dispatcher.InvokeAsync(() => {
                        if (_targetImage != null)
                        {
                            _targetImage.Source = null;
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }

                _disposed = true;
            }
        }

        ~BackgroundSpriteAnimator()
        {
            Dispose(false);
        }
        #endregion
    }
}