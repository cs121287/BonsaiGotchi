using System;
using System.IO;
using System.Text.Json;

namespace BonsaiGotchiGame.Models
{
    public class GameSettings
    {
        private static readonly string _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BonsaiGotchiGame",
            "settings.json");

        private static GameSettings? _instance;

        public static GameSettings Instance
        {
            get
            {
                _instance ??= LoadSettings();
                return _instance;
            }
        }

        public bool EnableAutoSave { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 5; // Minutes
        public int TimeProgressionSpeed { get; set; } = 1; // 1x, 2x, 5x, 10x
        public bool PlaySounds { get; set; } = true;
        public float SoundVolume { get; set; } = 0.8f;
        public bool PlayMusic { get; set; } = true;
        public float MusicVolume { get; set; } = 0.5f;
        public bool ShowTips { get; set; } = true;
        public string Theme { get; set; } = "Forest Green";

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath) ?? string.Empty);
                string json = JsonSerializer.Serialize(this);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception)
            {
                // Silently fail, use defaults
            }
        }

        private static GameSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<GameSettings>(json);
                    return settings ?? new GameSettings();
                }
            }
            catch (Exception)
            {
                // Silently fail, use defaults
            }

            return new GameSettings();
        }
    }
}