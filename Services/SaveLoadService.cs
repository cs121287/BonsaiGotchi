using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Services
{
    public class SaveLoadService
    {
        private const string SaveFileName = "bonsai_save.json";
        private const string BackupFileName = "bonsai_save_backup.json";
        private static readonly object _fileLock = new object();
        private static readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);

        public async Task SaveBonsaiAsync(Bonsai bonsai)
        {
            if (bonsai == null)
                throw new ArgumentNullException(nameof(bonsai));

            await _asyncLock.WaitAsync();
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
                    GameYear = bonsai.GameYear,

                    // Save the XP system properties
                    XP = bonsai.XP,
                    Level = bonsai.Level,
                    Mood = bonsai.Mood,
                    Hunger = bonsai.Hunger,
                    Cleanliness = bonsai.Cleanliness,
                    GrowthStage = bonsai.GrowthStage,
                    MoodState = bonsai.MoodState,
                    HealthCondition = bonsai.HealthCondition,
                    ConsecutiveDaysGoodCare = bonsai.ConsecutiveDaysGoodCare,
                    CurrentState = bonsai.CurrentState,

                    // Save timestamp for validation
                    SaveVersion = 1,
                    SaveTimestamp = DateTime.UtcNow
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(saveData, options);
                string savePath = GetSaveFilePath();
                string backupPath = GetBackupFilePath();

                // Ensure directory exists
                string? directoryName = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                // Create backup of existing save file before overwriting
                if (File.Exists(savePath))
                {
                    try
                    {
                        File.Copy(savePath, backupPath, true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Failed to create backup: {ex.Message}");
                    }
                }

                // Save the file with atomic write operation
                string tempPath = savePath + ".tmp";
                await File.WriteAllTextAsync(tempPath, json);

                // Verify the file was written correctly
                if (File.Exists(tempPath))
                {
                    // Atomic move operation
                    File.Move(tempPath, savePath, true);
                }
                else
                {
                    throw new IOException("Failed to write temporary save file");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save bonsai: {ex.Message}", ex);
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        public async Task<Bonsai> LoadBonsaiAsync()
        {
            await _asyncLock.WaitAsync();
            try
            {
                string savePath = GetSaveFilePath();
                string backupPath = GetBackupFilePath();

                if (!File.Exists(savePath))
                {
                    return new Bonsai(); // Return a new bonsai if no save file exists
                }

                string json;
                BonsaiSaveData? saveData = null;

                // Try to load primary save file
                try
                {
                    json = await File.ReadAllTextAsync(savePath);

                    // Validate JSON before deserializing
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidDataException("Save file is empty");
                    }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    saveData = JsonSerializer.Deserialize<BonsaiSaveData>(json, options);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load primary save file: {ex.Message}");

                    // Try to load backup file
                    if (File.Exists(backupPath))
                    {
                        try
                        {
                            json = await File.ReadAllTextAsync(backupPath);
                            if (!string.IsNullOrWhiteSpace(json))
                            {
                                var options = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                };
                                saveData = JsonSerializer.Deserialize<BonsaiSaveData>(json, options);
                                System.Diagnostics.Debug.WriteLine("Successfully loaded from backup file");
                            }
                        }
                        catch (Exception backupEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load backup file: {backupEx.Message}");
                        }
                    }

                    // If both primary and backup failed, throw the original exception
                    if (saveData == null)
                    {
                        throw new Exception($"Failed to load save file and backup: {ex.Message}", ex);
                    }
                }

                if (saveData == null)
                {
                    return new Bonsai();
                }

                // Validate save data
                if (!IsValidSaveData(saveData))
                {
                    System.Diagnostics.Debug.WriteLine("Save data validation failed, creating new bonsai");
                    return new Bonsai();
                }

                var bonsai = new Bonsai(saveData.Name)
                {
                    Water = Math.Clamp(saveData.Water, 0, 100),
                    Health = Math.Clamp(saveData.Health, 0, 100),
                    Growth = Math.Clamp(saveData.Growth, 0, 100),
                    Energy = Math.Clamp(saveData.Energy, 0, 100),
                    Age = Math.Max(0, saveData.Age),
                    LastUpdateTime = saveData.LastUpdateTime
                };

                // Load game time properties with validation
                bonsai.GameHour = Math.Clamp(saveData.GameHour, 0, 23);
                bonsai.GameMinute = Math.Clamp(saveData.GameMinute, 0, 59);
                bonsai.GameDay = Math.Clamp(saveData.GameDay, 1, 30);
                bonsai.GameMonth = Math.Clamp(saveData.GameMonth, 1, 12);
                bonsai.GameYear = Math.Max(1, saveData.GameYear);

                // Load XP system properties with validation
                bonsai.XP = Math.Max(0, saveData.XP);
                //bonsai.Level = Math.Max(1, saveData.Level);
                bonsai.Mood = Math.Clamp(saveData.Mood, 0, 100);
                bonsai.Hunger = Math.Clamp(saveData.Hunger, 0, 100);
                bonsai.Cleanliness = Math.Clamp(saveData.Cleanliness, 0, 100);
                bonsai.ConsecutiveDaysGoodCare = Math.Max(0, saveData.ConsecutiveDaysGoodCare);

                // Load enum properties with validation
                if (Enum.IsDefined(typeof(GrowthStage), saveData.GrowthStage))
                {
                    // GrowthStage is read-only and updated based on level
                }

                if (Enum.IsDefined(typeof(MoodState), saveData.MoodState))
                {
                    // MoodState is read-only and updated based on mood
                }

                if (Enum.IsDefined(typeof(HealthCondition), saveData.HealthCondition))
                {
                    bonsai.HealthCondition = saveData.HealthCondition;
                }

                if (Enum.IsDefined(typeof(BonsaiState), saveData.CurrentState))
                {
                    bonsai.CurrentState = saveData.CurrentState;
                }

                return bonsai;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load bonsai: {ex.Message}", ex);
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        public async Task<bool> SaveExistsAsync()
        {
            try
            {
                string savePath = GetSaveFilePath();
                return await Task.Run(() => File.Exists(savePath));
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteSaveAsync()
        {
            await _asyncLock.WaitAsync();
            try
            {
                string savePath = GetSaveFilePath();
                string backupPath = GetBackupFilePath();

                bool deleted = false;

                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    deleted = true;
                }

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    deleted = true;
                }

                return deleted;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete save files: {ex.Message}", ex);
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        private bool IsValidSaveData(BonsaiSaveData saveData)
        {
            try
            {
                // Basic validation checks
                if (string.IsNullOrWhiteSpace(saveData.Name))
                    return false;

                if (saveData.Water < 0 || saveData.Water > 100)
                    return false;

                if (saveData.Health < 0 || saveData.Health > 100)
                    return false;

                if (saveData.Energy < 0 || saveData.Energy > 100)
                    return false;

                if (saveData.Age < 0)
                    return false;

                if (saveData.GameHour < 0 || saveData.GameHour > 23)
                    return false;

                if (saveData.GameMinute < 0 || saveData.GameMinute > 59)
                    return false;

                if (saveData.GameDay < 1 || saveData.GameDay > 30)
                    return false;

                if (saveData.GameMonth < 1 || saveData.GameMonth > 12)
                    return false;

                if (saveData.GameYear < 1)
                    return false;

                // XP system validation
                if (saveData.XP < 0)
                    return false;

                if (saveData.Level < 1)
                    return false;

                if (saveData.Mood < 0 || saveData.Mood > 100)
                    return false;

                if (saveData.Hunger < 0 || saveData.Hunger > 100)
                    return false;

                if (saveData.Cleanliness < 0 || saveData.Cleanliness > 100)
                    return false;

                if (saveData.ConsecutiveDaysGoodCare < 0)
                    return false;

                // Check if LastUpdateTime is reasonable (not too far in the future)
                if (saveData.LastUpdateTime > DateTime.Now.AddDays(1))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetSaveFilePath()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BonsaiGotchiGame");

            return Path.Combine(appDataPath, SaveFileName);
        }

        private string GetBackupFilePath()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BonsaiGotchiGame");

            return Path.Combine(appDataPath, BackupFileName);
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

            // Game time properties
            public int GameHour { get; set; }
            public int GameMinute { get; set; }
            public int GameDay { get; set; }
            public int GameMonth { get; set; }
            public int GameYear { get; set; }

            // XP system properties
            public int XP { get; set; }
            public int Level { get; set; }
            public int Mood { get; set; }
            public int Hunger { get; set; }
            public int Cleanliness { get; set; }
            public GrowthStage GrowthStage { get; set; }
            public MoodState MoodState { get; set; }
            public HealthCondition HealthCondition { get; set; }
            public int ConsecutiveDaysGoodCare { get; set; }
            public BonsaiState CurrentState { get; set; }

            // Save file metadata
            public int SaveVersion { get; set; }
            public DateTime SaveTimestamp { get; set; }
        }
    }
}