using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace BonsaiGotchiGame.Services
{
    public class BackgroundService
    {
        private readonly string _backgroundsPath;
        private BitmapImage? _morningImage;
        private BitmapImage? _afternoonImage;
        private BitmapImage? _eveningImage;
        private BitmapImage? _nightImage;

        public enum TimeOfDay
        {
            Morning,
            Afternoon,
            Evening,
            Night
        }

        public BackgroundService()
        {
            // Get the base directory of the application
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _backgroundsPath = Path.Combine(baseDirectory, "Assets", "Backgrounds");

            // Load all background images at startup
            LoadBackgroundImages();
        }

        private void LoadBackgroundImages()
        {
            try
            {
                // Load morning image (6 AM - 12 PM)
                _morningImage = LoadImage("morning");

                // Load afternoon image (12 PM - 6 PM)
                _afternoonImage = LoadImage("afternoon");

                // Load evening image (6 PM - 10 PM)
                _eveningImage = LoadImage("evening");

                // Load night image (10 PM - 6 AM)
                _nightImage = LoadImage("night");

                Console.WriteLine("Background images loaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading background images: {ex.Message}");
            }
        }

        private BitmapImage? LoadImage(string timeOfDay)
        {
            try
            {
                // Try different file extensions
                string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp" };

                foreach (string ext in extensions)
                {
                    string imagePath = Path.Combine(_backgroundsPath, timeOfDay + ext);

                    if (File.Exists(imagePath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Make it thread-safe

                        Console.WriteLine($"Loaded {timeOfDay} background: {imagePath}");
                        return bitmap;
                    }
                }

                Console.WriteLine($"Background image not found for {timeOfDay}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading {timeOfDay} image: {ex.Message}");
                return null;
            }
        }

        public BitmapImage? GetBackgroundImage(int hour)
        {
            return hour switch
            {
                >= 6 and < 12 => _morningImage,    // Morning: 6 AM - 12 PM
                >= 12 and < 18 => _afternoonImage, // Afternoon: 12 PM - 6 PM
                >= 18 and < 22 => _eveningImage,   // Evening: 6 PM - 10 PM
                _ => _nightImage                    // Night: 10 PM - 6 AM
            };
        }

        public string GetBackgroundPath(int hour)
        {
            string timeOfDay = hour switch
            {
                >= 6 and < 12 => "morning",    // Morning: 6 AM - 12 PM
                >= 12 and < 18 => "afternoon", // Afternoon: 12 PM - 6 PM
                >= 18 and < 22 => "evening",   // Evening: 6 PM - 10 PM
                _ => "night"                    // Night: 10 PM - 6 AM
            };

            // Try different file extensions to find the actual file
            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp" };

            foreach (string ext in extensions)
            {
                string imagePath = Path.Combine(_backgroundsPath, timeOfDay + ext);
                if (File.Exists(imagePath))
                {
                    return imagePath;
                }
            }

            // Return a default path if no file is found
            return Path.Combine(_backgroundsPath, timeOfDay + ".png");
        }

        public static TimeOfDay GetTimeOfDay(int hour)
        {
            return hour switch
            {
                >= 6 and < 12 => TimeOfDay.Morning,    // Morning: 6 AM - 12 PM
                >= 12 and < 18 => TimeOfDay.Afternoon, // Afternoon: 12 PM - 6 PM
                >= 18 and < 22 => TimeOfDay.Evening,   // Evening: 6 PM - 10 PM
                _ => TimeOfDay.Night                    // Night: 10 PM - 6 AM
            };
        }

        public string GetTimeOfDayName(int hour)
        {
            return hour switch
            {
                >= 6 and < 12 => "Morning",
                >= 12 and < 18 => "Afternoon",
                >= 18 and < 22 => "Evening",
                _ => "Night"
            };
        }

        public BitmapImage? GetBackgroundImageByTimeOfDay(TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                TimeOfDay.Morning => _morningImage,
                TimeOfDay.Afternoon => _afternoonImage,
                TimeOfDay.Evening => _eveningImage,
                TimeOfDay.Night => _nightImage,
                _ => _morningImage
            };
        }

        public string GetBackgroundPathByTimeOfDay(TimeOfDay timeOfDay)
        {
            string timeOfDayString = timeOfDay switch
            {
                TimeOfDay.Morning => "morning",
                TimeOfDay.Afternoon => "afternoon",
                TimeOfDay.Evening => "evening",
                TimeOfDay.Night => "night",
                _ => "morning"
            };

            // Try different file extensions to find the actual file
            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp" };

            foreach (string ext in extensions)
            {
                string imagePath = Path.Combine(_backgroundsPath, timeOfDayString + ext);
                if (File.Exists(imagePath))
                {
                    return imagePath;
                }
            }

            // Return a default path if no file is found
            return Path.Combine(_backgroundsPath, timeOfDayString + ".png");
        }

        public string GetBackgroundsDirectory()
        {
            return _backgroundsPath;
        }

        public bool BackgroundExists(int hour)
        {
            string timeOfDay = hour switch
            {
                >= 6 and < 12 => "morning",
                >= 12 and < 18 => "afternoon",
                >= 18 and < 22 => "evening",
                _ => "night"
            };

            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp" };

            foreach (string ext in extensions)
            {
                string imagePath = Path.Combine(_backgroundsPath, timeOfDay + ext);
                if (File.Exists(imagePath))
                {
                    return true;
                }
            }

            return false;
        }

        public bool BackgroundExistsByTimeOfDay(TimeOfDay timeOfDay)
        {
            string timeOfDayString = timeOfDay switch
            {
                TimeOfDay.Morning => "morning",
                TimeOfDay.Afternoon => "afternoon",
                TimeOfDay.Evening => "evening",
                TimeOfDay.Night => "night",
                _ => "morning"
            };

            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp" };

            foreach (string ext in extensions)
            {
                string imagePath = Path.Combine(_backgroundsPath, timeOfDayString + ext);
                if (File.Exists(imagePath))
                {
                    return true;
                }
            }

            return false;
        }
    }
}