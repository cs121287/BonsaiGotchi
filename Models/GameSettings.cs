using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BonsaiGotchiGame.Models
{
    public class GameSettings
    {
        private static GameSettings? _instance;
        private static readonly object _lock = new object();

        public static GameSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = Load();
                        }
                    }
                }
                return _instance;
            }
        }

        // Game settings
        public bool AutoSave { get; set; } = true;
        public int AutoSaveIntervalMinutes { get; set; } = 5;
        public int TimeProgressionSpeed { get; set; } = 1;
        public bool ShowTips { get; set; } = true;

        // Audio settings
        public bool PlaySounds { get; set; } = true;
        public bool PlayMusic { get; set; } = true;
        public float SoundVolume { get; set; } = 0.8f;
        public float MusicVolume { get; set; } = 0.5f;

        // Interface settings
        public int ThemeIndex { get; set; } = 0;

        private GameSettings()
        {
            // Private constructor to enforce singleton pattern
        }

        public void Save()
        {
            try
            {
                string settingsPath = GetSettingsFilePath();
                string? directoryPath = Path.GetDirectoryName(settingsPath);

                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private static GameSettings Load()
        {
            try
            {
                string settingsPath = GetSettingsFilePath();

                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    GameSettings? settings = JsonSerializer.Deserialize<GameSettings>(json);

                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }

            // Return default settings if loading fails
            return new GameSettings();
        }

        private static string GetSettingsFilePath()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BonsaiGotchiGame");

            return Path.Combine(appDataPath, "settings.json");
        }
    }
}