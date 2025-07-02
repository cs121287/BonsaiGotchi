using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Services
{
    public class SaveLoadService
    {
        private const string SaveFileName = "bonsai_save.json";

        public async Task SaveBonsaiAsync(Bonsai bonsai)
        {
            if (bonsai == null)
                throw new ArgumentNullException(nameof(bonsai));

            try
            {
                var saveData = new BonsaiSaveData
                {
                    Name = bonsai.Name,
                    Water = bonsai.Water,
                    Health = bonsai.Health,
                    Growth = bonsai.Growth,
                    Energy = bonsai.Energy,
                    Age = bonsai.Age,
                    LastUpdateTime = bonsai.LastUpdateTime,
                    // Save the new game time properties
                    GameHour = bonsai.GameHour,
                    GameMinute = bonsai.GameMinute,
                    GameDay = bonsai.GameDay,
                    GameMonth = bonsai.GameMonth,
                    GameYear = bonsai.GameYear
                };

                string json = JsonSerializer.Serialize(saveData);
                string savePath = GetSaveFilePath();

                // Ensure directory exists
                string? directoryName = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                // Save the file
                await File.WriteAllTextAsync(savePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save bonsai: {ex.Message}", ex);
            }
        }

        public async Task<Bonsai> LoadBonsaiAsync()
        {
            try
            {
                string savePath = GetSaveFilePath();

                if (!File.Exists(savePath))
                {
                    return new Bonsai(); // Return a new bonsai if no save file exists
                }

                string json = await File.ReadAllTextAsync(savePath);
                var saveData = JsonSerializer.Deserialize<BonsaiSaveData>(json);

                if (saveData == null)
                    return new Bonsai();

                var bonsai = new Bonsai(saveData.Name)
                {
                    Water = saveData.Water,
                    Health = saveData.Health,
                    Growth = saveData.Growth,
                    Energy = saveData.Energy,
                    Age = saveData.Age,
                    LastUpdateTime = saveData.LastUpdateTime
                };

                // Load game time properties if they exist in the save data
                // Using reflection to check if the properties exist in the older save files
                var saveDataType = saveData.GetType();
                var gameHourProperty = saveDataType.GetProperty("GameHour");
                if (gameHourProperty != null)
                {
                    bonsai.GameHour = saveData.GameHour;
                    bonsai.GameMinute = saveData.GameMinute;
                    bonsai.GameDay = saveData.GameDay;
                    bonsai.GameMonth = saveData.GameMonth;
                    bonsai.GameYear = saveData.GameYear;
                }

                return bonsai;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load bonsai: {ex.Message}", ex);
            }
        }

        private string GetSaveFilePath()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BonsaiGotchiGame");

            return Path.Combine(appDataPath, SaveFileName);
        }

        private class BonsaiSaveData
        {
            public string Name { get; set; } = string.Empty;
            public int Water { get; set; }
            public int Health { get; set; }
            public int Growth { get; set; }
            public int Energy { get; set; }
            public int Age { get; set; }
            public DateTime LastUpdateTime { get; set; }
            
            // Add game time properties
            public int GameHour { get; set; }
            public int GameMinute { get; set; }
            public int GameDay { get; set; }
            public int GameMonth { get; set; }
            public int GameYear { get; set; }
        }
    }
}