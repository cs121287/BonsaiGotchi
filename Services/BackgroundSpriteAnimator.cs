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
                LoadBackground(BackgroundService.TimeOfDay.Morning, "morning.png");
                LoadBackground(BackgroundService.TimeOfDay.Afternoon, "afternoon.png");
                LoadBackground(BackgroundService.TimeOfDay.Evening, "evening.png");
                LoadBackground(BackgroundService.TimeOfDay.Night, "night.png");

                // If no backgrounds loaded successfully, create defaults
                if (_backgroundSprites.Count == 0)
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
            _backgroundSprites[BackgroundService.TimeOfDay.Morning] = CreateColorBackground(Colors.LightYellow);
            _backgroundSprites[BackgroundService.TimeOfDay.Afternoon] = CreateColorBackground(Colors.LightGreen);
            _backgroundSprites[BackgroundService.TimeOfDay.Evening] = CreateColorBackground(Colors.LightSalmon);
            _backgroundSprites[BackgroundService.TimeOfDay.Night] = CreateColorBackground(Colors.MidnightBlue);
        }

        private BitmapSource CreateColorBackground(Color color)
        {
            int width = 1024;
            int height = 1024;
            
            var drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(color), null, new Rect(0, 0, width, height));
            }
            
            var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(drawingVisual);
            renderTarget.Freeze();
            
            return renderTarget;
        }

        private void LoadBackground(BackgroundService.TimeOfDay timeOfDay, string filename)
        {
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
                        bitmap.Freeze();
                        
                        _backgroundSprites[timeOfDay] = bitmap;
                        Console.WriteLine($"Successfully loaded background for {timeOfDay}");
                        return;
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
                    bitmap.Freeze();
                    
                    _backgroundSprites[timeOfDay] = bitmap;
                    Console.WriteLine($"Loaded background from embedded resource");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load from embedded resource: {ex.Message}");
            }

            Console.WriteLine($"Could not find background for {timeOfDay}");
        }

        public void UpdateBackground(int gameHour)
        {
            if (_disposed) return;

            try
            {
                var timeOfDay = BackgroundService.GetTimeOfDay(gameHour);
                
                // Get the appropriate background
                if (_backgroundSprites.TryGetValue(timeOfDay, out BitmapSource? background) && background != null)
                {
                    _targetImage.Source = background;
                    Console.WriteLine($"Set background for time of day: {timeOfDay}");
                }
                else
                {
                    // Try any available background
                    foreach (var tod in Enum.GetValues<BackgroundService.TimeOfDay>())
                    {
                        if (_backgroundSprites.TryGetValue(tod, out BitmapSource? fallback) && fallback != null)
                        {
                            _targetImage.Source = fallback;
                            Console.WriteLine($"Set fallback background: {tod}");
                            return;
                        }
                    }
                    
                    // Last resort - create a default
                    _targetImage.Source = CreateColorBackground(Colors.LightGreen);
                    Console.WriteLine("Created emergency background");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating background: {ex.Message}");
                
                // Last resort - create a very simple background
                try
                {
                    _targetImage.Source = CreateColorBackground(Colors.LightGreen);
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
                    _backgroundSprites.Clear();
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