using System;
using System.Windows.Forms;
using BonsaiGotchi.BreedingSystem;
using BonsaiGotchi.EnvironmentSystem;
using BonsaiGotchi.CollectionUI;
using BonsaiGotchi.EnvironmentUI;

namespace BonsaiGotchi
{
    /// <summary>
    /// Manager for integrating Phase 3 advanced features
    /// </summary>
    public class Phase3IntegrationManager
    {
        // Core managers
        private readonly BreedingManager breedingManager;
        private readonly EnvironmentManager environmentManager;
        
        // Random generator
        private readonly Random random;
        
        // Active bonsai
        private BonsaiPet activeBonsai;
        
        // Parent form
        private Form parentForm;
        
        /// <summary>
        /// Initialize the Phase 3 integration manager
        /// </summary>
        public Phase3IntegrationManager(Form parentForm, BonsaiPet initialBonsai = null)
        {
            this.parentForm = parentForm;
            
            // Create shared random generator
            random = new Random();
            
            // Create core managers
            environmentManager = new EnvironmentManager(random);
            breedingManager = new BreedingManager(random, initialBonsai);
            
            // Set active bonsai
            activeBonsai = initialBonsai;
            
            // Start environment simulation
            environmentManager.Start();
        }
        
        /// <summary>
        /// Get the current environment manager
        /// </summary>
        public EnvironmentManager Environment => environmentManager;
        
        /// <summary>
        /// Get the current breeding manager
        /// </summary>
        public BreedingManager Breeding => breedingManager;
        
        /// <summary>
        /// Set the active bonsai
        /// </summary>
        public void SetActiveBonsai(BonsaiPet bonsai)
        {
            if (bonsai == null) return;
            
            activeBonsai = bonsai;
            breedingManager.SetActiveBonsai(bonsai);
        }
        
        /// <summary>
        /// Update the bonsai with environmental effects
        /// </summary>
        public void UpdateBonsaiWithEnvironment(BonsaiPet bonsai, TimeSpan elapsed)
        {
            if (bonsai == null) return;
            
            // Apply environmental effects to the bonsai
            
            // Season effects
            ApplySeasonEffects(bonsai, elapsed);
            
            // Weather effects
            ApplyWeatherEffects(bonsai, elapsed);
            
            // Time of day effects
            ApplyTimeOfDayEffects(bonsai);
            
            // Climate effects
            ApplyClimateEffects(bonsai, elapsed);
            
            // Environmental events
            ApplyEventEffects(bonsai, elapsed);
        }
        
        /// <summary>
        /// Apply effects of the current season
        /// </summary>
        private void ApplySeasonEffects(BonsaiPet bonsai, TimeSpan elapsed)
        {
            double daysFraction = elapsed.TotalDays;
            if (daysFraction < 0.001) return;
            
            // Base growth rate changes by season
            double growthChange = 0;
            
            switch (environmentManager.CurrentSeason)
            {
                case Season.Spring:
                    // Spring is good for growth
                    growthChange = 0.2 * daysFraction;
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
                    growthChange = 0.1 * daysFraction;
                    // Natural pruning happens (leaves falling)
                    bonsai.PruningQuality += 0.5 * daysFraction;
                    break;
                    
                case Season.Winter:
                    // Winter is dormant period
                    growthChange = -0.1 * daysFraction; // Can actually decrease slightly
                    bonsai.Hydration -= 0.5 * daysFraction; // Low water needs
                    bonsai.Hunger -= 0.5 * daysFraction; // Low feeding needs
                    break;
            }
            
            // Apply growth change
            bonsai.Growth = Math.Max(0, Math.Min(100, bonsai.Growth + growthChange));
        }
        
        /// <summary>
        /// Apply effects of the current weather
        /// </summary>
        private void ApplyWeatherEffects(BonsaiPet bonsai, TimeSpan elapsed)
        {
            double daysFraction = elapsed.TotalDays;
            if (daysFraction < 0.001) return;
            
            switch (environmentManager.CurrentWeather)
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
        }
        
        /// <summary>
        /// Apply effects of the time of day
        /// </summary>
        private void ApplyTimeOfDayEffects(BonsaiPet bonsai)
        {
            // Certain activities are better at certain times of day
            switch (environmentManager.CurrentTimeOfDay)
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
        /// Apply effects of the current climate
        /// </summary>
        private void ApplyClimateEffects(BonsaiPet bonsai, TimeSpan elapsed)
        {
            double daysFraction = elapsed.TotalDays;
            if (daysFraction < 0.001) return;
            
            // Different climates have different baseline effects
            switch (environmentManager.CurrentClimate)
            {
                case ClimateZone.Temperate:
                    // Temperate is balanced
                    break;
                    
                case ClimateZone.Tropical:
                    // Tropical increases growth but also pests
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
        }
        
        /// <summary>
        /// Apply effects of active environmental events
        /// </summary>
        private void ApplyEventEffects(BonsaiPet bonsai, TimeSpan elapsed)
        {
            double daysFraction = elapsed.TotalDays;
            if (daysFraction < 0.001) return;
            
            // Apply effects for each active event
            foreach (var environmentalEvent in environmentManager.ActiveEvents)
            {
                // Scale effects based on intensity (0-100)
                double intensityFactor = environmentalEvent.Intensity / 100.0;
                
                switch (environmentalEvent.Type)
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
                        bonsai.SoilQuality -= 1.0 * intensityFactor * daysFraction; // Some nutrient leaching
                        break;
                        
                    case EventType.Insects:
                        bonsai.PestInfestation += 5.0 * intensityFactor * daysFraction;
                        break;
                        
                    case EventType.Pollen:
                        // No direct effect on bonsai, but might affect player (allergies)
                        break;
                        
                    // Add cases for other event types as needed
                }
            }
        }
        
        /// <summary>
        /// Show the collection manager form
        /// </summary>
        public void ShowCollectionManager()
        {
            var collectionForm = new CollectionManagerForm(breedingManager, random);
            collectionForm.ShowDialog(parentForm);
        }
        
        /// <summary>
        /// Show the environment monitor form
        /// </summary>
        public void ShowEnvironmentMonitor()
        {
            var environmentForm = new EnvironmentMonitorForm(environmentManager);
            environmentForm.ShowDialog(parentForm);
        }
        
        /// <summary>
        /// Save all Phase 3 data
        /// </summary>
        public async Task SaveDataAsync(string basePath)
        {
            try
            {
                // Create directories if they don't exist
                string breedingPath = Path.Combine(basePath, "breeding");
                Directory.CreateDirectory(breedingPath);
                
                // Save breeding data
                string breedingFile = Path.Combine(breedingPath, "breeding_data.json");
                await breedingManager.SaveDataAsync(breedingFile);
                
                // Environment data doesn't need to be saved as it's regenerated each session
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save Phase 3 data: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load all Phase 3 data
        /// </summary>
        public async Task LoadDataAsync(string basePath)
        {
            try
            {
                // Load breeding data
                string breedingFile = Path.Combine(basePath, "breeding", "breeding_data.json");
                if (File.Exists(breedingFile))
                {
                    await breedingManager.LoadDataAsync(breedingFile);
                }
                
                // Environment data doesn't need to be loaded as it's regenerated each session
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load Phase 3 data: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            environmentManager.Stop();
        }
    }
}