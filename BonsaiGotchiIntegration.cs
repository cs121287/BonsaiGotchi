using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using BonsaiGotchi.BreedingSystem;
using BonsaiGotchi.EnvironmentSystem;
using BonsaiGotchi.MiniGames;

namespace BonsaiGotchi
{
    /// <summary>
    /// Main integration class for BonsaiGotchi phases 1, 2, and 3
    /// </summary>
    public class BonsaiGotchiIntegration
    {
        // Core managers
        public EnvironmentManager EnvironmentManager { get; private set; }
        public BreedingManager BreedingManager { get; private set; }
        
        // Main form
        private BonsaiGotchiForm mainForm;
        
        // Random number generator
        private Random random;
        
        public BonsaiGotchiIntegration(BonsaiGotchiForm mainForm)
        {
            this.mainForm = mainForm;
            random = new Random();
            
            // Initialize core systems
            InitializeEnvironmentManager();
            InitializeBreedingManager();
        }
        
        /// <summary>
        /// Initialize the environment manager system
        /// </summary>
        private void InitializeEnvironmentManager()
        {
            EnvironmentManager = new EnvironmentManager(random);
            EnvironmentManager.Start();
            
            // Subscribe to environment events
            SubscribeToEnvironmentEvents();
        }
        
        /// <summary>
        /// Initialize the breeding manager system
        /// </summary>
        private void InitializeBreedingManager()
        {
            BreedingManager = new BreedingManager(random);
        }
        
        /// <summary>
        /// Subscribe to environment manager events
        /// </summary>
        private void SubscribeToEnvironmentEvents()
        {
            EnvironmentManager.SeasonChanged += (s, e) => 
            {
                // Update current bonsai with new season
                mainForm.UpdateCurrentBonsaiWithEnvironment();
            };
            
            EnvironmentManager.WeatherChanged += (s, e) => 
            {
                // Update current bonsai with new weather
                mainForm.UpdateCurrentBonsaiWithEnvironment();
            };
            
            EnvironmentManager.TimeOfDayChanged += (s, e) => 
            {
                // Update current bonsai with new time of day
                mainForm.UpdateCurrentBonsaiWithEnvironment();
            };
            
            EnvironmentManager.EnvironmentalEventStarted += (s, e) => 
            {
                // Notify about new environmental event
                mainForm.NotifyEnvironmentalEvent(e.Event, true);
            };
            
            EnvironmentManager.EnvironmentalEventEnded += (s, e) => 
            {
                // Notify about ended environmental event
                mainForm.NotifyEnvironmentalEvent(e.Event, false);
            };
        }
        
        /// <summary>
        /// Set the active bonsai for all systems
        /// </summary>
        public void SetActiveBonsai(BonsaiPet bonsai)
        {
            if (bonsai == null) return;
            
            // Set as active in breeding manager
            BreedingManager.SetActiveBonsai(bonsai);
        }
        
        /// <summary>
        /// Update bonsai based on environment
        /// </summary>
        public void UpdateBonsaiWithEnvironment(BonsaiPet bonsai, TimeSpan elapsed)
        {
            if (bonsai == null) return;
            
            ApplySeasonEffects(bonsai, elapsed);
            ApplyWeatherEffects(bonsai, elapsed);
            ApplyTimeOfDayEffects(bonsai);
            ApplyClimateEffects(bonsai, elapsed);
            ApplyEventEffects(bonsai, elapsed);
        }
        
        /// <summary>
        /// Apply seasonal effects to bonsai
        /// </summary>
        private void ApplySeasonEffects(BonsaiPet bonsai, TimeSpan elapsed)
        {
            double daysFraction = elapsed.TotalDays;
            if (daysFraction < 0.001) return;
            
            // Apply different effects based on season
            switch (EnvironmentManager.CurrentSeason)
            {
                case Season.Spring:
                    // Spring is good for growth
                    bonsai.Growth += 0.2 * daysFraction;
                    bonsai.Hydration -= 1.0 * daysFraction; // Moderate water needs
                    break;
                    
                case Season.Summer:
                    // Summer increases water needs
                    bonsai.Hydration -= 2.5 * daysFraction;
                    // Increased risk of stress in summer
                    bonsai.StressLevel += 1.0 * daysFraction;
                    break;
                    
                case Season.Autumn:
                    // Autumn slows growth
                    bonsai.Growth += 0.1 * daysFraction;
                    // Natural pruning happens (leaves falling)
                    bonsai.PruningQuality += 0.5 * daysFraction;
                    break;
                    
                case Season.Winter:
                    // Winter is dormant period
                    bonsai.Growth -= 0.1 * daysFraction; // Can actually decrease slightly
                    bonsai.Hydration -= 0.5 * daysFraction; // Low water needs
                    bonsai.Hunger -= 0.5 * daysFraction; // Low feeding needs
                    break;
            }
            
            // Ensure values stay in valid range
            bonsai.Growth = Math.Max(0, Math.Min(100, bonsai.Growth));
            bonsai.Hydration = Math.Max(0, Math.Min(100, bonsai.Hydration));
            bonsai.Hunger = Math.Max(0, Math.Min(100, bonsai.Hunger));
        }
        
        /// <summary>
        /// Apply weather effects to bonsai
        /// </summary>
        private void ApplyWeatherEffects(BonsaiPet bonsai, TimeSpan elapsed)
        {
            double daysFraction = elapsed.TotalDays;
            if (daysFraction < 0.001) return;
            
            // Apply different effects based on weather
            switch (EnvironmentManager.CurrentWeather)
            {
                case Weather.Sunny:
                    // Sunny weather increases happiness but decreases hydration
                    bonsai.Happiness += 1.0 * daysFraction;
                    bonsai.Hydration -= 1.5 * daysFraction;
                    break;
                    
                case Weather.Cloudy:
                    // Cloudy weather is neutral
                    break;
                    
                case Weather.Rain:
                    // Rain naturally waters the tree
                    bonsai.Hydration += 2.0 * daysFraction;
                    // But can make it a bit unhappy
                    bonsai.Happiness -= 0.5 * daysFraction;
                    break;
                    
                case Weather.Humid:
                    // Humidity slows water loss but increases pest risk
                    bonsai.Hydration -= 0.5 * daysFraction;
                    bonsai.PestInfestation += 0.3 * daysFraction;
                    break;
                    
                case Weather.Wind:
                    // Wind causes stress and increases water needs
                    bonsai.StressLevel += 1.0 * daysFraction;
                    bonsai.Hydration -= 1.0 * daysFraction;
                    break;
                    
                case Weather.Storm:
                    // Storms are stressful but provide water
                    bonsai.StressLevel += 3.0 * daysFraction;
                    bonsai.Hydration += 3.0 * daysFraction;
                    bonsai.Happiness -= 2.0 * daysFraction;
                    break;
                    
                case Weather.Snow:
                    // Snow is stressful and cold
                    bonsai.StressLevel += 2.0 * daysFraction;
                    bonsai.Growth -= 0.2 * daysFraction;
                    // But reduces pest activity
                    bonsai.PestInfestation -= 1.0 * daysFraction;
                    break;
            }
            
            // Ensure values stay in valid range
            bonsai.Happiness = Math.Max(0, Math.Min(100, bonsai.Happiness));
            bonsai.Hydration = Math.Max(0, Math.Min(100, bonsai.Hydration));
            bonsai.StressLevel = Math.Max(0, Math.Min(100, bonsai.StressLevel));
            bonsai.PestInfestation = Math.Max(0, Math.Min(100, bonsai.PestInfestation));
            bonsai.Growth = Math.Max(0, Math.Min(100, bonsai.Growth));
        }
        
        /// <summary>
        /// Apply time of day effects to bonsai
        /// </summary>
        private void ApplyTimeOfDayEffects(BonsaiPet bonsai)
        {
            // Different times of day affect care effectiveness
            switch (EnvironmentManager.CurrentTimeOfDay)
            {
                case TimeOfDay.Morning:
                    // Morning is good for watering
                    bonsai.MorningActivityBonus = true;
                    bonsai.EveningActivityBonus = false;
                    bonsai.NightActivityPenalty = false;
                    break;
                    
                case TimeOfDay.Day:
                    // Day is neutral
                    bonsai.MorningActivityBonus = false;
                    bonsai.EveningActivityBonus = false;
                    bonsai.NightActivityPenalty = false;
                    break;
                    
                case TimeOfDay.Evening:
                    // Evening is good for pruning
                    bonsai.MorningActivityBonus = false;
                    bonsai.EveningActivityBonus = true;
                    bonsai.NightActivityPenalty = false;
                    break;
                    
                case TimeOfDay.Night:
                    // Night is bad for most activities
                    bonsai.MorningActivityBonus = false;
                    bonsai.EveningActivityBonus = false;
                    bonsai.NightActivityPenalty = true;
                    break;
            }
        }
        
        /// <summary>
        /// Apply climate zone effects to bonsai
        /// </summary>
        private void ApplyClimateEffects(BonsaiPet bonsai, TimeSpan elapsed)
        {
            double daysFraction = elapsed.TotalDays;
            if (daysFraction < 0.001) return;
            
            // Apply different effects based on climate zone
            switch (EnvironmentManager.CurrentClimate)
            {
                case ClimateZone.Temperate:
                    // Temperate is balanced
                    break;
                    
                case ClimateZone.Tropical:
                    // Tropical increases growth but also pest risk
                    bonsai.Growth += 0.2 * daysFraction;
                    bonsai.PestInfestation += 0.2 * daysFraction;
                    break;
                    
                case ClimateZone.Desert:
                    // Desert increases water needs but reduces pests
                    bonsai.Hydration -= 1.0 * daysFraction;
                    bonsai.PestInfestation -= 0.2 * daysFraction;
                    break;
                    
                case ClimateZone.Alpine:
                    // Alpine reduces growth but makes tree hardier
                    bonsai.Growth -= 0.1 * daysFraction;
                    bonsai.StressLevel -= 0.3 * daysFraction;
                    break;
            }
            
            // Ensure values stay in valid range
            bonsai.Growth = Math.Max(0, Math.Min(100, bonsai.Growth));
            bonsai.Hydration = Math.Max(0, Math.Min(100, bonsai.Hydration));
            bonsai.PestInfestation = Math.Max(0, Math.Min(100, bonsai.PestInfestation));
            bonsai.StressLevel = Math.Max(0, Math.Min(100, bonsai.StressLevel));
        }
        
        /// <summary>
        /// Apply active environmental events to bonsai
        /// </summary>
        private void ApplyEventEffects(BonsaiPet bonsai, TimeSpan elapsed)
        {
            double daysFraction = elapsed.TotalDays;
            if (daysFraction < 0.001) return;
            
            // Apply effects for active environmental events
            foreach (var envEvent in EnvironmentManager.ActiveEvents)
            {
                // Scale effects based on intensity
                double intensityFactor = envEvent.Intensity / 100.0;
                
                switch (envEvent.Type)
                {
                    case EventType.Heatwave:
                        bonsai.Hydration -= 5.0 * intensityFactor * daysFraction;
                        bonsai.StressLevel += 3.0 * intensityFactor * daysFraction;
                        break;
                        
                    case EventType.Drought:
                        bonsai.Hydration -= 8.0 * intensityFactor * daysFraction;
                        bonsai.Hunger += 2.0 * intensityFactor * daysFraction;
                        break;
                        
                    case EventType.HeavyRain:
                        bonsai.Hydration += 5.0 * intensityFactor * daysFraction;
                        bonsai.SoilQuality -= 1.0 * intensityFactor * daysFraction; // Nutrient leaching
                        break;
                        
                    case EventType.Insects:
                        bonsai.PestInfestation += 5.0 * intensityFactor * daysFraction;
                        break;
                        
                    case EventType.ColdSnap:
                        bonsai.StressLevel += 4.0 * intensityFactor * daysFraction;
                        bonsai.Growth -= 2.0 * intensityFactor * daysFraction;
                        break;
                        
                    case EventType.Frost:
                        bonsai.StressLevel += 3.0 * intensityFactor * daysFraction;
                        bonsai.Health -= 1.0 * intensityFactor * daysFraction;
                        break;
                }
                
                // Ensure values stay in valid range
                bonsai.Hydration = Math.Max(0, Math.Min(100, bonsai.Hydration));
                bonsai.StressLevel = Math.Max(0, Math.Min(100, bonsai.StressLevel));
                bonsai.Hunger = Math.Max(0, Math.Min(100, bonsai.Hunger));
                bonsai.PestInfestation = Math.Max(0, Math.Min(100, bonsai.PestInfestation));
                bonsai.SoilQuality = Math.Max(0, Math.Min(100, bonsai.SoilQuality));
                bonsai.Growth = Math.Max(0, Math.Min(100, bonsai.Growth));
                bonsai.Health = Math.Max(0, Math.Min(100, bonsai.Health));
            }
        }
        
        /// <summary>
        /// Save all system data
        /// </summary>
        public async Task SaveAllDataAsync(string basePath)
        {
            try
            {
                // Create directories if they don't exist
                string breedingPath = Path.Combine(basePath, "breeding_data");
                Directory.CreateDirectory(breedingPath);
                
                // Save breeding data
                string breedingFile = Path.Combine(breedingPath, "breeding_data.json");
                await BreedingManager.SaveDataAsync(breedingFile);
                
                // Environment data doesn't need to be saved as it's regenerated each session
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save integrated data: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load all system data
        /// </summary>
        public async Task LoadAllDataAsync(string basePath)
        {
            try
            {
                // Load breeding data if it exists
                string breedingFile = Path.Combine(basePath, "breeding_data", "breeding_data.json");
                if (File.Exists(breedingFile))
                {
                    await BreedingManager.LoadDataAsync(breedingFile);
                }
                
                // Environment data doesn't need to be loaded as it's regenerated each session
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load integrated data: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            EnvironmentManager.Stop();
        }
    }
}