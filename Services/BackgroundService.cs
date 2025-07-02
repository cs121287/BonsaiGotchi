using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BonsaiGotchiGame.Services
{
    public class BackgroundService
    {
        private readonly Dictionary<TimeOfDay, string> _backgroundPaths;
        private readonly Dictionary<TimeOfDay, BitmapImage> _cachedBackgrounds = [];
        private bool _hasAttemptedLoading = false;

        public enum TimeOfDay
        {
            Morning,
            Afternoon,
            Evening,
            Night
        }

        public BackgroundService()
        {
            _backgroundPaths = new Dictionary<TimeOfDay, string>
            {
                // Changed file extensions from jpg to png
                { TimeOfDay.Morning, "morning.png" },
                { TimeOfDay.Afternoon, "afternoon.png" },
                { TimeOfDay.Evening, "evening.png" },
                { TimeOfDay.Night, "night.png" }
            };

            // Don't preload in constructor - wait until first request
            // This helps avoid startup errors
        }

        private void PreloadBackgrounds()
        {
            if (_hasAttemptedLoading)
                return;

            _hasAttemptedLoading = true;

            try
            {
                // Try to load all backgrounds
                // Fixed CA2263: Using generic overload instead of System.Type overload
                foreach (var timeOfDay in Enum.GetValues<TimeOfDay>())
                {
                    PreloadBackground(timeOfDay);
                }

                // If no backgrounds were loaded, create default ones
                if (_cachedBackgrounds.Count == 0)
                {
                    CreateDefaultBackgrounds();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error preloading backgrounds: {ex.Message}");
                CreateDefaultBackgrounds();
            }
        }

        private void CreateDefaultBackgrounds()
        {
            Console.WriteLine("Creating default color backgrounds");

            // Create color backgrounds as fallbacks
            _cachedBackgrounds[TimeOfDay.Morning] = CreateColorBackground(Colors.LightYellow);
            _cachedBackgrounds[TimeOfDay.Afternoon] = CreateColorBackground(Colors.LightGreen);
            _cachedBackgrounds[TimeOfDay.Evening] = CreateColorBackground(Colors.LightSalmon);
            _cachedBackgrounds[TimeOfDay.Night] = CreateColorBackground(Colors.MidnightBlue);
        }

        private static BitmapImage CreateColorBackground(Color color)
        {
            // Create a solid color background
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

            // Convert to BitmapImage
            var bitmap = new BitmapImage();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
            }

            return bitmap;
        }

        private void PreloadBackground(TimeOfDay timeOfDay)
        {
            if (_backgroundPaths.TryGetValue(timeOfDay, out string? filename) && !string.IsNullOrEmpty(filename))
            {
                // Try different paths to find the image
                if (!TryLoadBackgroundImage(timeOfDay, filename))
                {
                    Console.WriteLine($"Failed to load background for {timeOfDay}");
                }
            }
        }

        private bool TryLoadBackgroundImage(TimeOfDay timeOfDay, string filename)
        {
            try
            {
                // Log what we're looking for
                Console.WriteLine($"Trying to load background image: {filename}");

                // 1. Try as content file (relative to executable)
                string contentPath = Path.Combine("Assets", "Backgrounds", filename);
                Console.WriteLine($"Checking path: {contentPath}");
                if (File.Exists(contentPath))
                {
                    LoadImageFromFile(timeOfDay, contentPath);
                    Console.WriteLine($"Loaded background from: {contentPath}");
                    return true;
                }

                // 2. Try as absolute path in executable directory
                string absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Backgrounds", filename);
                Console.WriteLine($"Checking path: {absolutePath}");
                if (File.Exists(absolutePath))
                {
                    LoadImageFromFile(timeOfDay, absolutePath);
                    Console.WriteLine($"Loaded background from: {absolutePath}");
                    return true;
                }

                // 3. Try with user profile path from screenshot
                string userProfilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "source", "repos", "BonsaiGotchiGame", "Assets", "Backgrounds", filename);

                Console.WriteLine($"Checking path: {userProfilePath}");
                if (File.Exists(userProfilePath))
                {
                    LoadImageFromFile(timeOfDay, userProfilePath);
                    Console.WriteLine($"Loaded background from: {userProfilePath}");
                    return true;
                }

                // 4. Try loading from embedded resource 
                try
                {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    string resourceName = $"BonsaiGotchiGame.Assets.Backgrounds.{filename}";
                    Console.WriteLine($"Checking embedded resource: {resourceName}");

                    // List available resources for debugging
                    var resourceNames = assembly.GetManifestResourceNames();
                    Console.WriteLine("Available embedded resources:");
                    foreach (var resource in resourceNames)
                    {
                        Console.WriteLine($"- {resource}");
                    }

                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        _cachedBackgrounds[timeOfDay] = bitmap;
                        Console.WriteLine($"Loaded background from embedded resource: {resourceName}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading from embedded resource: {ex.Message}");
                }

                Console.WriteLine($"Could not find background image: {filename}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading background {filename}: {ex.Message}");
                return false;
            }
        }

        private void LoadImageFromFile(TimeOfDay timeOfDay, string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                _cachedBackgrounds[timeOfDay] = bitmap;

                // Log details about the loaded image
                Console.WriteLine($"Successfully loaded image. Size: {bitmap.PixelWidth}x{bitmap.PixelHeight}, Format: {bitmap.Format}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadImageFromFile: {ex.Message}");
                throw; // Rethrow so the caller can handle it
            }
        }

        public string GetBackgroundPath(int gameHour)
        {
            var timeOfDay = GetTimeOfDay(gameHour);

            // Try to get the filename
            string fileName = _backgroundPaths.TryGetValue(timeOfDay, out string? path) ? path : "morning.png";

            // Return a path that's most likely to exist
            return Path.Combine("Assets", "Backgrounds", fileName);
        }

        public BitmapImage GetBackgroundImage(int gameHour)
        {
            // Load backgrounds on first request if not already loaded
            if (!_hasAttemptedLoading)
            {
                PreloadBackgrounds();
            }

            var timeOfDay = GetTimeOfDay(gameHour);

            // Return cached image if available
            if (_cachedBackgrounds.TryGetValue(timeOfDay, out BitmapImage? image) && image != null)
            {
                return image;
            }

            // Fall back to morning or first available background
            // Fixed CA2263: Using generic overload instead of System.Type overload
            foreach (var tod in Enum.GetValues<TimeOfDay>())
            {
                if (_cachedBackgrounds.TryGetValue(tod, out BitmapImage? fallback) && fallback != null)
                {
                    return fallback;
                }
            }

            // Last resort - create and return a default background
            return CreateColorBackground(Colors.LightGreen);
        }

        public static TimeOfDay GetTimeOfDay(int gameHour)
        {
            if (gameHour >= 6 && gameHour < 12)
                return TimeOfDay.Morning;
            else if (gameHour >= 12 && gameHour < 18)
                return TimeOfDay.Afternoon;
            else if (gameHour >= 18 && gameHour < 22)
                return TimeOfDay.Evening;
            else
                return TimeOfDay.Night;
        }
    }
}