using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Services
{
    public class SaveLoadService : ISaveLoadService
    {
        private const string SaveFileName = "bonsai_save.json";
        private const string BackupFileName = "bonsai_save_backup.json";
        private const string ChecksumFileName = "bonsai_save.hash";
        private const string BackupChecksumFileName = "bonsai_save_backup.hash";
        
        // Security and performance constants
        private const long MaxSaveFileSize = 10 * 1024 * 1024; // 10MB max
        private static readonly TimeSpan MinSaveInterval = TimeSpan.FromSeconds(1); // Min 1 second between saves
        private const int MaxFileNameLength = 100;
        private static readonly Regex SafeFileNameRegex = new Regex(@"^[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled);
        
        private static readonly object _fileLock = new object();
        private static readonly SemaphoreSlim _asyncLock = new SemaphoreSlim(1, 1);
        private DateTime _lastSaveTime = DateTime.MinValue;
        private readonly string _basePath;

        public SaveLoadService()
        {
            // Use a safe, validated base path
            _basePath = GetSecureBasePath();
        }

        public bool CanSave()
        {
            return DateTime.UtcNow - _lastSaveTime >= MinSaveInterval;
        }

        public long GetMaxSaveFileSize() => MaxSaveFileSize;
        public TimeSpan GetMinSaveInterval() => MinSaveInterval;

        public async Task SaveBonsaiAsync(Bonsai bonsai)
        {
            await SaveBonsaiAsync(bonsai, null);
        }

        public async Task SaveBonsaiAsync(Bonsai bonsai, ShopManager? shopManager)
        {
            if (bonsai == null)
                throw new ArgumentNullException(nameof(bonsai));

            // Rate limiting check
            if (!CanSave())
            {
                throw new InvalidOperationException($"Save operation too frequent. Minimum interval: {MinSaveInterval.TotalSeconds} seconds");
            }

            await _asyncLock.WaitAsync();
            try
            {
                var saveData = CreateSaveData(bonsai, shopManager);

                // Validate save data before proceeding
                if (!IsValidSaveDataForSaving(saveData))
                {
                    throw new InvalidOperationException("Save data validation failed - data is inconsistent or corrupted");
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false, // Compact JSON for better performance
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json;
                try
                {
                    json = JsonSerializer.Serialize(saveData, options);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to serialize save data: {ex.Message}", ex);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidOperationException("Serialization produced empty or null JSON");
                }

                // Check file size limit
                var jsonBytes = Encoding.UTF8.GetBytes(json);
                if (jsonBytes.Length > MaxSaveFileSize)
                {
                    throw new InvalidOperationException($"Save data too large: {jsonBytes.Length} bytes (max: {MaxSaveFileSize})");
                }

                string savePath = GetSecureSaveFilePath();
                string backupPath = GetSecureBackupFilePath();
                string checksumPath = GetSecureChecksumFilePath();
                string backupChecksumPath = GetSecureBackupChecksumFilePath();

                // Ensure directory exists
                string? directoryName = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                // Calculate checksum for integrity validation
                string dataChecksum = CalculateChecksum(json);

                // Create backup of existing save file before overwriting
                bool backupCreated = false;
                
                if (File.Exists(savePath))
                {
                    try
                    {
                        // Validate existing file size before backup
                        var existingFileInfo = new FileInfo(savePath);
                        if (existingFileInfo.Length > MaxSaveFileSize)
                        {
                            throw new InvalidOperationException($"Existing save file is too large: {existingFileInfo.Length} bytes");
                        }

                        // Create backup atomically
                        File.Copy(savePath, backupPath, true);
                        if (File.Exists(checksumPath))
                        {
                            File.Copy(checksumPath, backupChecksumPath, true);
                        }
                        backupCreated = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Failed to create backup: {ex.Message}");
                        throw new IOException($"Failed to create backup before saving: {ex.Message}", ex);
                    }
                }

                // Save with atomic write operation
                await PerformAtomicSave(savePath, checksumPath, json, dataChecksum, backupCreated, backupPath, backupChecksumPath);

                _lastSaveTime = DateTime.UtcNow;
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

        private async Task PerformAtomicSave(string savePath, string checksumPath, string json, string dataChecksum, 
            bool backupCreated, string backupPath, string backupChecksumPath)
        {
            string tempPath = GetSecureTempFilePath(savePath, ".tmp");
            string tempChecksumPath = GetSecureTempFilePath(checksumPath, ".tmp");

            try
            {
                // Write to temporary files first
                await File.WriteAllTextAsync(tempPath, json);
                await File.WriteAllTextAsync(tempChecksumPath, dataChecksum);

                // Verify the files were written correctly
                if (!File.Exists(tempPath) || !File.Exists(tempChecksumPath))
                {
                    throw new IOException("Failed to write temporary save files");
                }

                // Verify integrity of temporary files
                await ValidateTemporaryFiles(tempPath, tempChecksumPath);

                // Atomic move operations
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
                if (File.Exists(checksumPath))
                {
                    File.Delete(checksumPath);
                }

                File.Move(tempPath, savePath);
                File.Move(tempChecksumPath, checksumPath);

                // Verify final files exist and are correct
                if (!File.Exists(savePath) || !File.Exists(checksumPath))
                {
                    throw new IOException("Final verification failed: save files missing after move operation");
                }
            }
            catch (Exception ex)
            {
                // Clean up temp files if operation failed
                CleanupTempFiles(tempPath, tempChecksumPath);

                // Restore from backup if we created one and save failed
                if (backupCreated)
                {
                    RestoreFromBackup(backupPath, backupChecksumPath, savePath, checksumPath);
                }

                throw new IOException($"Failed to complete save operation: {ex.Message}", ex);
            }
        }

        public async Task<Bonsai> LoadBonsaiAsync()
        {
            var result = await LoadBonsaiAsync(null);
            return result.Bonsai;
        }

        public async Task<(Bonsai Bonsai, List<string> UnlockedShopItems)> LoadBonsaiAsync(ShopManager? shopManager)
        {
            await _asyncLock.WaitAsync();
            try
            {
                string savePath = GetSecureSaveFilePath();
                string backupPath = GetSecureBackupFilePath();
                string checksumPath = GetSecureChecksumFilePath();
                string backupChecksumPath = GetSecureBackupChecksumFilePath();

                if (!File.Exists(savePath) && !File.Exists(backupPath))
                {
                    return (new Bonsai(), new List<string>());
                }

                BonsaiSaveData? saveData = null;
                List<Exception> exceptions = new List<Exception>();

                // Try to load primary save file
                if (File.Exists(savePath))
                {
                    try
                    {
                        saveData = await LoadAndValidateFile(savePath, checksumPath);
                        if (saveData != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Successfully loaded from primary save file");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load primary save file: {ex.Message}");
                        exceptions.Add(ex);
                    }
                }

                // Try to load backup file if primary failed or doesn't exist
                if (saveData == null && File.Exists(backupPath))
                {
                    try
                    {
                        saveData = await LoadAndValidateFile(backupPath, backupChecksumPath);
                        if (saveData != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Successfully loaded from backup file");
                        }
                    }
                    catch (Exception backupEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load backup file: {backupEx.Message}");
                        exceptions.Add(backupEx);
                    }
                }

                // If both failed, throw comprehensive error
                if (saveData == null)
                {
                    if (exceptions.Count > 0)
                    {
                        throw new AggregateException("Failed to load both primary and backup save files", exceptions);
                    }
                    else
                    {
                        return (new Bonsai(), new List<string>());
                    }
                }

                // Final validation of save data
                if (!IsValidSaveData(saveData))
                {
                    System.Diagnostics.Debug.WriteLine("Save data validation failed, creating new bonsai");
                    return (new Bonsai(), new List<string>());
                }

                var bonsai = CreateBonsaiFromSaveData(saveData);
                var unlockedItems = ApplyShopUnlocks(saveData, shopManager);

                return (bonsai, unlockedItems);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading bonsai: {ex}");
                throw new Exception($"Failed to load bonsai: {ex.Message}", ex);
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        private async Task<BonsaiSaveData?> LoadAndValidateFile(string filePath, string checksumPath)
        {
            // Security: Validate file size before reading
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxSaveFileSize)
            {
                throw new InvalidDataException($"Save file too large: {fileInfo.Length} bytes (max: {MaxSaveFileSize})");
            }

            string json = await File.ReadAllTextAsync(filePath);

            // Validate JSON before deserializing
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException("Save file is empty");
            }

            // Validate checksum if available
            if (File.Exists(checksumPath))
            {
                try
                {
                    var checksumFileInfo = new FileInfo(checksumPath);
                    if (checksumFileInfo.Length > 1024) // Checksums should be small
                    {
                        throw new InvalidDataException("Checksum file too large");
                    }

                    string savedChecksum = (await File.ReadAllTextAsync(checksumPath)).Trim();
                    string calculatedChecksum = CalculateChecksum(json);
                    
                    if (savedChecksum != calculatedChecksum)
                    {
                        throw new InvalidDataException("Save file checksum validation failed - file may be corrupted");
                    }
                }
                catch (Exception ex) when (!(ex is InvalidDataException))
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not validate checksum: {ex.Message}");
                }
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            BonsaiSaveData? saveData = JsonSerializer.Deserialize<BonsaiSaveData>(json, options);
            
            if (saveData == null)
            {
                throw new InvalidDataException("Failed to deserialize save data - data may be corrupted");
            }

            return saveData;
        }

        public async Task<bool> SaveExistsAsync()
        {
            try
            {
                string savePath = GetSecureSaveFilePath();
                string backupPath = GetSecureBackupFilePath();
                return await Task.Run(() => File.Exists(savePath) || File.Exists(backupPath));
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
                string savePath = GetSecureSaveFilePath();
                string backupPath = GetSecureBackupFilePath();
                string checksumPath = GetSecureChecksumFilePath();
                string backupChecksumPath = GetSecureBackupChecksumFilePath();

                bool deleted = false;
                List<Exception> exceptions = new List<Exception>();

                string[] filesToDelete = { savePath, backupPath, checksumPath, backupChecksumPath };
                
                foreach (string filePath in filesToDelete)
                {
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                            deleted = true;
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                            System.Diagnostics.Debug.WriteLine($"Failed to delete {filePath}: {ex.Message}");
                        }
                    }
                }

                if (exceptions.Count > 0 && !deleted)
                {
                    throw new AggregateException("Failed to delete save files", exceptions);
                }

                return deleted;
            }
            catch (Exception ex) when (!(ex is AggregateException))
            {
                throw new Exception($"Failed to delete save files: {ex.Message}", ex);
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        // Security: Validate and sanitize file paths
        private string GetSecureBasePath()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrEmpty(appDataPath))
                {
                    throw new InvalidOperationException("Could not determine application data path");
                }

                string gamePath = Path.Combine(appDataPath, "BonsaiGotchiGame");
                
                // Validate the path doesn't contain dangerous characters
                if (gamePath.Contains("..") || gamePath.Contains("~"))
                {
                    throw new InvalidOperationException("Invalid characters in path");
                }

                return Path.GetFullPath(gamePath); // Normalize the path
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create secure base path: {ex.Message}", ex);
            }
        }

        private string GetSecurePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName.Length > MaxFileNameLength)
            {
                throw new ArgumentException($"Invalid filename: {fileName}");
            }

            if (!SafeFileNameRegex.IsMatch(fileName))
            {
                throw new ArgumentException($"Filename contains invalid characters: {fileName}");
            }

            string fullPath = Path.Combine(_basePath, fileName);
            
            // Ensure the path is within our base directory (prevent directory traversal)
            string normalizedPath = Path.GetFullPath(fullPath);
            string normalizedBase = Path.GetFullPath(_basePath);
            
            if (!normalizedPath.StartsWith(normalizedBase))
            {
                throw new ArgumentException($"Path traversal detected: {fileName}");
            }

            return normalizedPath;
        }

        private string GetSecureSaveFilePath() => GetSecurePath(SaveFileName);
        private string GetSecureBackupFilePath() => GetSecurePath(BackupFileName);
        private string GetSecureChecksumFilePath() => GetSecurePath(ChecksumFileName);
        private string GetSecureBackupChecksumFilePath() => GetSecurePath(BackupChecksumFileName);

        private string GetSecureTempFilePath(string basePath, string extension)
        {
            string fileName = Path.GetFileNameWithoutExtension(basePath) + extension;
            return GetSecurePath(fileName);
        }

        // Helper methods for cleaner code
        private BonsaiSaveData CreateSaveData(Bonsai bonsai, ShopManager? shopManager)
        {
            return new BonsaiSaveData
            {
                Name = bonsai.Name,
                Water = bonsai.Water,
                Health = bonsai.Health,
                Growth = bonsai.Growth,
                Energy = bonsai.Energy,
                Age = bonsai.Age,
                LastUpdateTime = bonsai.LastUpdateTime,
                GameHour = bonsai.GameHour,
                GameMinute = bonsai.GameMinute,
                GameDay = bonsai.GameDay,
                GameMonth = bonsai.GameMonth,
                GameYear = bonsai.GameYear,
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
                InventoryItems = new Dictionary<string, int>(bonsai.Inventory.Items),
                BonsaiBills = bonsai.Currency.BonsaiBills,
                LastDailyRewardTime = bonsai.Currency.LastDailyRewardTime,
                UnlockedShopItems = shopManager?.ShopItems
                    .Where(item => item.IsUnlocked)
                    .Select(item => item.Id)
                    .ToList() ?? new List<string>(),
                SaveVersion = 2,
                SaveTimestamp = DateTime.UtcNow
            };
        }

        private Bonsai CreateBonsaiFromSaveData(BonsaiSaveData saveData)
        {
            var bonsai = new Bonsai(saveData.Name)
            {
                Water = Math.Clamp(saveData.Water, 0, 100),
                Health = Math.Clamp(saveData.Health, 0, 100),
                Growth = Math.Clamp(saveData.Growth, 0, 100),
                Energy = Math.Clamp(saveData.Energy, 0, 100),
                Age = Math.Max(0, saveData.Age),
                LastUpdateTime = saveData.LastUpdateTime,
                GameHour = Math.Clamp(saveData.GameHour, 0, 23),
                GameMinute = Math.Clamp(saveData.GameMinute, 0, 59),
                GameDay = Math.Clamp(saveData.GameDay, 1, 30),
                GameMonth = Math.Clamp(saveData.GameMonth, 1, 12),
                GameYear = Math.Max(1, saveData.GameYear),
                XP = Math.Max(0, saveData.XP),
                Mood = Math.Clamp(saveData.Mood, 0, 100),
                Hunger = Math.Clamp(saveData.Hunger, 0, 100),
                Cleanliness = Math.Clamp(saveData.Cleanliness, 0, 100),
                ConsecutiveDaysGoodCare = Math.Max(0, saveData.ConsecutiveDaysGoodCare)
            };

            // Load enum properties with validation
            if (Enum.IsDefined(typeof(HealthCondition), saveData.HealthCondition))
            {
                bonsai.HealthCondition = saveData.HealthCondition;
            }

            if (Enum.IsDefined(typeof(BonsaiState), saveData.CurrentState))
            {
                bonsai.CurrentState = saveData.CurrentState;
            }

            // Load inventory data
            LoadInventoryData(bonsai, saveData);

            // Load currency data
            if (saveData.SaveVersion >= 2)
            {
                bonsai.Currency.LoadFromSaveData(saveData.BonsaiBills, saveData.LastDailyRewardTime);
            }

            return bonsai;
        }

        private void LoadInventoryData(Bonsai bonsai, BonsaiSaveData saveData)
        {
            if (saveData.InventoryItems != null && saveData.InventoryItems.Count > 0)
            {
                // Clear the default inventory first
                foreach (var item in bonsai.Inventory.Items.Keys.ToList())
                {
                    if (item != Bonsai.BASIC_FERTILIZER_ID)
                    {
                        bonsai.Inventory.UseItem(item, bonsai.Inventory.GetItemCount(item));
                    }
                }

                // Load saved inventory items
                foreach (var item in saveData.InventoryItems)
                {
                    if (!string.IsNullOrEmpty(item.Key) && item.Value > 0)
                    {
                        bonsai.Inventory.AddItem(item.Key, item.Value);
                    }
                }
            }
        }

        private List<string> ApplyShopUnlocks(BonsaiSaveData saveData, ShopManager? shopManager)
        {
            var unlockedItems = saveData.UnlockedShopItems ?? new List<string>();
            if (shopManager != null && unlockedItems.Count > 0)
            {
                foreach (var itemId in unlockedItems)
                {
                    var shopItem = shopManager.ShopItems.FirstOrDefault(i => i.Id == itemId);
                    if (shopItem != null)
                    {
                        shopItem.IsUnlocked = true;
                        shopItem.ButtonText = shopItem.Category == "Food" ? $"Buy More ({shopItem.Price})" : "Owned";
                    }
                }
            }
            return unlockedItems;
        }

        private async Task ValidateTemporaryFiles(string tempPath, string tempChecksumPath)
        {
            string verificationJson = await File.ReadAllTextAsync(tempPath);
            string verificationChecksum = await File.ReadAllTextAsync(tempChecksumPath);
            
            if (string.IsNullOrWhiteSpace(verificationJson))
            {
                throw new IOException("Verification failed: temporary save file is empty");
            }

            if (string.IsNullOrWhiteSpace(verificationChecksum))
            {
                throw new IOException("Verification failed: temporary checksum file is empty");
            }

            string calculatedChecksum = CalculateChecksum(verificationJson);
            if (calculatedChecksum != verificationChecksum.Trim())
            {
                throw new IOException("Verification failed: checksum mismatch in temporary files");
            }
        }

        private void CleanupTempFiles(string tempPath, string tempChecksumPath)
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (File.Exists(tempChecksumPath))
                    File.Delete(tempChecksumPath);
            }
            catch { /* Ignore cleanup errors */ }
        }

        private void RestoreFromBackup(string backupPath, string backupChecksumPath, string savePath, string checksumPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, savePath, true);
                }
                if (File.Exists(backupChecksumPath))
                {
                    File.Copy(backupChecksumPath, checksumPath, true);
                }
            }
            catch (Exception restoreEx)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore backup after save failure: {restoreEx.Message}");
            }
        }

        private bool IsValidSaveDataForSaving(BonsaiSaveData saveData)
        {
            try
            {
                if (!IsValidSaveData(saveData))
                    return false;

                // Validate XP vs Level relationship
                int expectedMinXP = CalculateMinXPForLevel(saveData.Level);
                int expectedMaxXP = CalculateMinXPForLevel(saveData.Level + 1) - 1;
                
                if (saveData.XP < expectedMinXP || (saveData.Level > 1 && saveData.XP > expectedMaxXP))
                {
                    System.Diagnostics.Debug.WriteLine($"XP/Level mismatch: Level {saveData.Level}, XP {saveData.XP}, Expected XP range: {expectedMinXP}-{expectedMaxXP}");
                    return false;
                }

                // Validate Growth Stage vs Level relationship
                GrowthStage expectedGrowthStage = CalculateExpectedGrowthStage(saveData.Level);
                if (saveData.GrowthStage != expectedGrowthStage)
                {
                    System.Diagnostics.Debug.WriteLine($"Growth Stage/Level mismatch: Level {saveData.Level}, Stage {saveData.GrowthStage}, Expected: {expectedGrowthStage}");
                    return false;
                }

                // Validate Mood State vs Mood relationship
                MoodState expectedMoodState = CalculateExpectedMoodState(saveData.Mood);
                if (saveData.MoodState != expectedMoodState)
                {
                    System.Diagnostics.Debug.WriteLine($"Mood State/Mood mismatch: Mood {saveData.Mood}, State {saveData.MoodState}, Expected: {expectedMoodState}");
                    return false;
                }

                // Validate Health vs HealthCondition logical consistency
                if (saveData.HealthCondition != HealthCondition.Healthy && saveData.Health > 95)
                {
                    System.Diagnostics.Debug.WriteLine($"Health/Condition inconsistency: Health {saveData.Health}, Condition {saveData.HealthCondition}");
                    return false;
                }

                // Validate save timestamp is not in the future
                if (saveData.SaveTimestamp > DateTime.UtcNow.AddMinutes(5))
                {
                    System.Diagnostics.Debug.WriteLine($"Save timestamp in future: {saveData.SaveTimestamp}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating save data for saving: {ex.Message}");
                return false;
            }
        }

        private bool IsValidSaveData(BonsaiSaveData saveData)
        {
            try
            {
                // Basic validation checks
                if (string.IsNullOrWhiteSpace(saveData.Name) || saveData.Name.Length > 100)
                    return false;

                if (saveData.Water < 0 || saveData.Water > 100)
                    return false;

                if (saveData.Health < 0 || saveData.Health > 100)
                    return false;

                if (saveData.Energy < 0 || saveData.Energy > 100)
                    return false;

                if (saveData.Age < 0 || saveData.Age > 1000000)
                    return false;

                if (saveData.GameHour < 0 || saveData.GameHour > 23)
                    return false;

                if (saveData.GameMinute < 0 || saveData.GameMinute > 59)
                    return false;

                if (saveData.GameDay < 1 || saveData.GameDay > 30)
                    return false;

                if (saveData.GameMonth < 1 || saveData.GameMonth > 12)
                    return false;

                if (saveData.GameYear < 1 || saveData.GameYear > 10000)
                    return false;

                // XP system validation
                if (saveData.XP < 0 || saveData.XP > 100000000)
                    return false;

                if (saveData.Level < 1 || saveData.Level > 10000)
                    return false;

                if (saveData.Mood < 0 || saveData.Mood > 100)
                    return false;

                if (saveData.Hunger < 0 || saveData.Hunger > 100)
                    return false;

                if (saveData.Cleanliness < 0 || saveData.Cleanliness > 100)
                    return false;

                if (saveData.ConsecutiveDaysGoodCare < 0 || saveData.ConsecutiveDaysGoodCare > 100000)
                    return false;

                // Currency validation
                if (saveData.SaveVersion >= 2)
                {
                    if (saveData.BonsaiBills < 0 || saveData.BonsaiBills > 1000000)
                        return false;
                }

                // Inventory validation
                if (saveData.InventoryItems != null)
                {
                    if (saveData.InventoryItems.Count > 1000) // Reasonable limit
                        return false;

                    foreach (var item in saveData.InventoryItems)
                    {
                        if (string.IsNullOrEmpty(item.Key) || item.Key.Length > 100 || item.Value < 0 || item.Value > 1000000)
                            return false;
                    }
                }

                // Time validation
                if (saveData.LastUpdateTime > DateTime.Now.AddDays(1))
                    return false;

                if (saveData.SaveTimestamp > DateTime.UtcNow.AddDays(1))
                    return false;

                // Validate that enums are defined
                if (!Enum.IsDefined(typeof(BonsaiState), saveData.CurrentState))
                    return false;

                if (!Enum.IsDefined(typeof(GrowthStage), saveData.GrowthStage))
                    return false;

                if (!Enum.IsDefined(typeof(MoodState), saveData.MoodState))
                    return false;

                if (!Enum.IsDefined(typeof(HealthCondition), saveData.HealthCondition))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating save data: {ex.Message}");
                return false;
            }
        }

        private int CalculateMinXPForLevel(int level)
        {
            if (level <= 1) return 0;
            try
            {
                return (int)(100 * Math.Pow(level - 1, 1.5));
            }
            catch
            {
                return 0;
            }
        }

        private GrowthStage CalculateExpectedGrowthStage(int level)
        {
            return level switch
            {
                <= 5 => GrowthStage.Seedling,
                <= 15 => GrowthStage.Sapling,
                <= 30 => GrowthStage.YoungBonsai,
                <= 50 => GrowthStage.MatureBonsai,
                <= 75 => GrowthStage.ElderBonsai,
                <= 100 => GrowthStage.AncientBonsai,
                _ => GrowthStage.LegendaryBonsai
            };
        }

        private MoodState CalculateExpectedMoodState(int mood)
        {
            return mood switch
            {
                >= 90 => MoodState.Ecstatic,
                >= 75 => MoodState.Happy,
                >= 60 => MoodState.Content,
                >= 40 => MoodState.Neutral,
                >= 25 => MoodState.Unhappy,
                >= 10 => MoodState.Sad,
                _ => MoodState.Miserable
            };
        }

        private string CalculateChecksum(string data)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
                    return Convert.ToBase64String(hashedBytes);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating checksum: {ex.Message}");
                return string.Empty;
            }
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
            public int GameHour { get; set; }
            public int GameMinute { get; set; }
            public int GameDay { get; set; }
            public int GameMonth { get; set; }
            public int GameYear { get; set; }
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
            public Dictionary<string, int>? InventoryItems { get; set; }
            public int BonsaiBills { get; set; }
            public DateTime LastDailyRewardTime { get; set; }
            public List<string>? UnlockedShopItems { get; set; }
            public int SaveVersion { get; set; }
            public DateTime SaveTimestamp { get; set; }
        }
    }
}