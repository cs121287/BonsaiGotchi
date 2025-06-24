using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;
using System.Drawing;

namespace BonsaiGotchi
{
    /// <summary>
    /// Extension to the BonsaiPet class with enhanced gameplay mechanics
    /// </summary>
    public partial class BonsaiPet
    {
        #region Enhanced Properties

        // Season and weather system
        public Season CurrentSeason { get; set; }
        public Weather CurrentWeather { get; set; }
        public DateTime LastSeasonChange { get; set; }
        public DateTime LastWeatherChange { get; set; }
        
        // Pest and disease system
        public double PestInfestation { get; set; }
        public double DiseaseLevel { get; set; }
        public bool HasPests => PestInfestation > 30;
        public bool HasDisease => DiseaseLevel > 30;
        
        // Stress and emotional state
        public double StressLevel { get; set; }
        public EmotionalState CurrentMood { get; set; }
        public int MoodScore { get; set; } // -100 to 100, affects visual cues
        
        // Enhanced care history
        public int TotalWaterings { get; set; }
        public int TotalFeedings { get; set; }
        public int TotalPrunings { get; set; }
        public int TotalRepottings { get; set; }
        public int TotalPestsRemoved { get; set; }
        public int PerfectCareStreak { get; set; } // Days with optimal care

        // Activity tracking
        public DateTime LastActivity { get; set; }
        public double ActivityScore { get; set; } // 0-100, higher is more activity
        
        // Environmental preferences (can be unique per bonsai)
        public LightPreference LightNeeds { get; set; }
        public WaterPreference WaterNeeds { get; set; }
        public SoilPreference SoilNeeds { get; set; }

        #endregion
        
        #region Enhanced Constructor Methods
        
        // Update constructor with enhanced properties
        private void InitializeEnhancedProperties(Random random)
        {
            // Initial season and weather
            CurrentSeason = DetermineSeason(DateTime.Now.Month);
            CurrentWeather = GenerateRandomWeather(random, CurrentSeason);
            LastSeasonChange = DateTime.Now;
            LastWeatherChange = DateTime.Now;
            
            // Initialize pest and disease levels (low initially)
            PestInfestation = random.Next(0, 10);
            DiseaseLevel = random.Next(0, 5);
            
            // Initialize stress and mood (neutral)
            StressLevel = 20 + random.Next(0, 10);
            CurrentMood = DetermineMood();
            MoodScore = 0;
            
            // Initialize activity tracking
            LastActivity = DateTime.Now;
            ActivityScore = 70 + random.Next(0, 30);
            
            // Initialize environmental preferences (random for each bonsai)
            LightNeeds = (LightPreference)random.Next(0, Enum.GetValues(typeof(LightPreference)).Length);
            WaterNeeds = (WaterPreference)random.Next(0, Enum.GetValues(typeof(WaterPreference)).Length);
            SoilNeeds = (SoilPreference)random.Next(0, Enum.GetValues(typeof(SoilPreference)).Length);
        }
        
        #endregion

        #region Enhanced Update Methods
        
        /// <summary>
        /// Enhanced update method that includes all new systems
        /// </summary>
        public void UpdateEnhanced(TimeSpan elapsedTime)
        {
            if (IsDead) return;

            // Update base stats first
            Update(elapsedTime);
            
            // Calculate real-world days passed since last update
            double daysFraction = elapsedTime.TotalHours / 24;
            
            // Update season and weather
            UpdateSeasonAndWeather();
            
            // Update pest and disease levels
            UpdatePestAndDisease(daysFraction);
            
            // Update stress level
            UpdateStressLevel(daysFraction);
            
            // Update mood
            UpdateMood();
            
            // Update activity score
            UpdateActivityScore(elapsedTime);
            
            // Apply seasonal effects
            ApplySeasonalEffects(daysFraction);
            
            // Apply weather effects
            ApplyWeatherEffects(daysFraction);
        }
        
        /// <summary>
        /// Updates season based on real-world time and generates appropriate weather
        /// </summary>
        private void UpdateSeasonAndWeather()
        {
            // Check if it's time for a season change (every 3 real months)
            DateTime now = DateTime.Now;
            Season newSeason = DetermineSeason(now.Month);
            
            if (newSeason != CurrentSeason)
            {
                CurrentSeason = newSeason;
                LastSeasonChange = now;
                
                // Notify about season change
                AddNotification(new BonsaiNotification(
                    "Season Changed", 
                    $"The season has changed to {CurrentSeason}. Your bonsai's needs will change accordingly.",
                    NotificationSeverity.Information));
            }
            
            // Update weather every 1-3 days (in-game time)
            TimeSpan weatherAge = now - LastWeatherChange;
            if (weatherAge.TotalDays > 2 || random.NextDouble() < 0.15) // 15% chance per update to change
            {
                Weather oldWeather = CurrentWeather;
                CurrentWeather = GenerateRandomWeather(random, CurrentSeason);
                LastWeatherChange = now;
                
                // Only notify on significant weather changes
                if (IsSignificantWeatherChange(oldWeather, CurrentWeather))
                {
                    AddNotification(new BonsaiNotification(
                        "Weather Changed", 
                        $"The weather has changed to {CurrentWeather}. This will affect your bonsai's needs.",
                        NotificationSeverity.Information));
                }
            }
        }
        
        /// <summary>
        /// Updates pest infestation and disease levels based on care conditions
        /// </summary>
        private void UpdatePestAndDisease(double daysFraction)
        {
            if (daysFraction < 0.001) return;
            
            // Base pest growth rates (per day)
            double baseInfestationRate = 0.5;
            double baseDiseaseRate = 0.3;
            
            // Factors that increase pest infestation
            if (Hydration > 90) baseInfestationRate += 1.0; // Too wet
            if (SoilQuality < 40) baseInfestationRate += 1.0; // Poor soil
            if (CurrentSeason == Season.Summer) baseInfestationRate += 0.5; // Summer increases pests
            if (CurrentWeather == Weather.Humid) baseInfestationRate += 1.0; // Humid weather
            if (MoodScore < -30) baseInfestationRate += 0.5; // Unhappy plants attract pests
            
            // Factors that increase disease
            if (Hydration > 95 || Hydration < 20) baseDiseaseRate += 1.0; // Extreme hydration
            if (SoilQuality < 30) baseDiseaseRate += 1.0; // Poor soil increases disease
            if (HasPests) baseDiseaseRate += 0.5; // Pests can cause disease
            if (CurrentWeather == Weather.Rain && CurrentSeason != Season.Spring) baseDiseaseRate += 0.5; // Rain outside spring
            if (Health < 40) baseDiseaseRate += 1.0; // Low health increases disease risk
            
            // Apply the rates
            PestInfestation = Math.Min(100, PestInfestation + (baseInfestationRate * daysFraction));
            DiseaseLevel = Math.Min(100, DiseaseLevel + (baseDiseaseRate * daysFraction));
            
            // Critical conditions check
            if (PestInfestation > 60 && !HasNotificationOfType("PestInfestation"))
            {
                AddNotification(new BonsaiNotification(
                    "Pest Infestation", 
                    $"{Name} is suffering from pest infestation! Use the pest removal tools immediately.",
                    NotificationSeverity.Alert));
            }
            
            if (DiseaseLevel > 60 && !HasNotificationOfType("DiseaseDetected"))
            {
                AddNotification(new BonsaiNotification(
                    "Disease Detected", 
                    $"{Name} is showing signs of disease! Apply treatment immediately.",
                    NotificationSeverity.Alert));
            }
            
            // Pests and disease affect health
            if (HasPests || HasDisease)
            {
                double healthReduction = ((PestInfestation * 0.1) + (DiseaseLevel * 0.1)) * daysFraction;
                Health = Math.Max(0, Health - healthReduction);
            }
        }
        
        /// <summary>
        /// Updates the bonsai's stress level based on care and environmental factors
        /// </summary>
        private void UpdateStressLevel(double daysFraction)
        {
            if (daysFraction < 0.001) return;
            
            // Calculate base stress decay (stress naturally decreases slightly over time)
            double stressChange = -2.0 * daysFraction;
            
            // Factors that increase stress
            if (Hydration < 20 || Hydration > 90) stressChange += 5.0 * daysFraction;
            if (Hunger > 80) stressChange += 4.0 * daysFraction;
            if (HasPests) stressChange += 3.0 * daysFraction;
            if (HasDisease) stressChange += 5.0 * daysFraction;
            if (CurrentWeather == Weather.Storm) stressChange += 3.0 * daysFraction;
            if (DaysSinceLastActivity() > 5) stressChange += 2.0 * daysFraction;
            
            // Apply stress change
            StressLevel = Math.Min(100, Math.Max(0, StressLevel + stressChange));
            
            // High stress notification
            if (StressLevel > 70 && !HasNotificationOfType("HighStress"))
            {
                AddNotification(new BonsaiNotification(
                    "High Stress", 
                    $"{Name} is very stressed! Make sure it's getting appropriate care.",
                    NotificationSeverity.Warning));
            }
        }
        
        /// <summary>
        /// Updates the bonsai's mood based on various factors
        /// </summary>
        private void UpdateMood()
        {
            // Calculate base mood score
            int newMoodScore = 0;
            
            // Physical factors
            newMoodScore += (int)((100 - Hunger) * 0.3);  // Less hunger = better mood
            newMoodScore += (int)((100 - StressLevel) * 0.3); // Less stress = better mood
            newMoodScore += (int)(Happiness * 0.3); // Direct happiness impact
            
            // Environmental factors
            if (IsWeatherPreferred()) newMoodScore += 10;
            if (IsSeasonPreferred()) newMoodScore += 10;
            
            // Care factors
            if (PerfectCareStreak > 3) newMoodScore += 10;
            if (DaysSinceLastActivity() < 2) newMoodScore += 5;
            
            // Negative factors
            if (HasPests) newMoodScore -= 20;
            if (HasDisease) newMoodScore -= 30;
            if (Health < 40) newMoodScore -= 15;
            
            // Clamp to valid range
            MoodScore = Math.Max(-100, Math.Min(100, newMoodScore));
            
            // Update emotional state
            CurrentMood = DetermineMood();
        }
        
        /// <summary>
        /// Updates activity score based on recent interactions
        /// </summary>
        private void UpdateActivityScore(TimeSpan elapsed)
        {
            // Activity naturally decays over time
            double activityDecay = elapsed.TotalHours * 2.0;
            ActivityScore = Math.Max(0, ActivityScore - activityDecay);
        }
        
        /// <summary>
        /// Apply seasonal effects to the bonsai's stats
        /// </summary>
        private void ApplySeasonalEffects(double daysFraction)
        {
            if (daysFraction < 0.001) return;
            
            switch (CurrentSeason)
            {
                case Season.Spring:
                    // Spring is good for growth
                    Growth += 0.2 * daysFraction;
                    Hydration -= 1.0 * daysFraction; // Moderate water needs
                    break;
                    
                case Season.Summer:
                    // Summer increases water needs
                    Hydration -= 2.5 * daysFraction;
                    // Increased risk of stress in summer
                    StressLevel += 1.0 * daysFraction;
                    break;
                    
                case Season.Autumn:
                    // Autumn slows growth
                    Growth += 0.1 * daysFraction;
                    // Natural pruning happens (leaves falling)
                    PruningQuality += 0.5 * daysFraction;
                    break;
                    
                case Season.Winter:
                    // Winter is dormant period
                    Growth -= 0.1 * daysFraction; // Can actually decrease slightly
                    Hydration -= 0.5 * daysFraction; // Low water needs
                    Hunger -= 0.5 * daysFraction; // Low feeding needs
                    break;
            }
            
            // Ensure values stay in valid range
            Growth = Math.Max(0, Math.Min(100, Growth));
            Hydration = Math.Max(0, Math.Min(100, Hydration));
            Hunger = Math.Max(0, Math.Min(100, Hunger));
        }
        
        /// <summary>
        /// Apply weather effects to the bonsai's stats
        /// </summary>
        private void ApplyWeatherEffects(double daysFraction)
        {
            if (daysFraction < 0.001) return;
            
            switch (CurrentWeather)
            {
                case Weather.Sunny:
                    // Sunny weather increases happiness but decreases hydration
                    Happiness += 1.0 * daysFraction;
                    Hydration -= 1.5 * daysFraction;
                    break;
                    
                case Weather.Cloudy:
                    // Cloudy weather is neutral
                    break;
                    
                case Weather.Rain:
                    // Rain naturally waters the tree
                    Hydration += 2.0 * daysFraction;
                    // But can make it a bit unhappy
                    Happiness -= 0.5 * daysFraction;
                    break;
                    
                case Weather.Humid:
                    // Humidity slows water loss but increases pest risk
                    Hydration -= 0.5 * daysFraction;
                    PestInfestation += 0.3 * daysFraction;
                    break;
                    
                case Weather.Wind:
                    // Wind causes stress and increases water needs
                    StressLevel += 1.0 * daysFraction;
                    Hydration -= 1.0 * daysFraction;
                    break;
                    
                case Weather.Storm:
                    // Storms are stressful but provide water
                    StressLevel += 3.0 * daysFraction;
                    Hydration += 3.0 * daysFraction;
                    Happiness -= 2.0 * daysFraction;
                    break;
                    
                case Weather.Snow:
                    // Snow is stressful and cold
                    StressLevel += 2.0 * daysFraction;
                    Growth -= 0.2 * daysFraction;
                    // But reduces pest activity
                    PestInfestation -= 1.0 * daysFraction;
                    break;
            }
            
            // Ensure values stay in valid range
            Happiness = Math.Max(0, Math.Min(100, Happiness));
            Hydration = Math.Max(0, Math.Min(100, Hydration));
            StressLevel = Math.Max(0, Math.Min(100, StressLevel));
            PestInfestation = Math.Max(0, Math.Min(100, PestInfestation));
            Growth = Math.Max(0, Math.Min(100, Growth));
        }
        
        #endregion
        
        #region Enhanced Care Actions
        
        /// <summary>
        /// Removes pests from the bonsai
        /// </summary>
        public void RemovePests(double effectiveness)
        {
            if (IsDead) return;
            
            // Base pest reduction
            double pestReduction = 30.0 * (effectiveness / 100.0);
            
            // Apply the reduction
            double initialPestLevel = PestInfestation;
            PestInfestation = Math.Max(0, PestInfestation - pestReduction);
            
            // Small impact on happiness (doesn't like the treatment but benefits)
            Happiness += 5.0;
            
            // Small stress from the treatment
            StressLevel += 10.0;
            
            // Track the care
            TotalPestsRemoved++;
            LastActivity = DateTime.Now;
            ActivityScore = Math.Min(100, ActivityScore + 15);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.PestRemoval,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Pest Infestation -{pestReduction:0.0}%"
            });
            
            // Update stats and trigger notification
            OnStatsChanged();
            
            // Different notification based on effectiveness
            if (effectiveness > 75)
            {
                AddNotification(new BonsaiNotification(
                    "Pests Removed", 
                    $"You expertly removed the pests from {Name}!",
                    NotificationSeverity.Achievement));
            }
            else
            {
                AddNotification(new BonsaiNotification(
                    "Pests Treated", 
                    $"You treated {Name} for pests. Some remain but the situation is improving.",
                    NotificationSeverity.Information));
            }
        }
        
        /// <summary>
        /// Treats disease on the bonsai
        /// </summary>
        public void TreatDisease(double effectiveness)
        {
            if (IsDead) return;
            
            // Base disease reduction
            double diseaseReduction = 40.0 * (effectiveness / 100.0);
            
            // Apply the reduction
            double initialDiseaseLevel = DiseaseLevel;
            DiseaseLevel = Math.Max(0, DiseaseLevel - diseaseReduction);
            
            // Small impact on happiness (doesn't like the treatment but benefits)
            Happiness += 5.0;
            
            // Moderate stress from the treatment
            StressLevel += 15.0;
            
            // May impact health temporarily
            Health -= 5.0;
            
            // Track the care
            LastActivity = DateTime.Now;
            ActivityScore = Math.Min(100, ActivityScore + 20);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Medicine,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Disease -{diseaseReduction:0.0}%"
            });
            
            // Update stats and trigger notification
            OnStatsChanged();
            
            // Different notification based on effectiveness
            if (effectiveness > 75)
            {
                AddNotification(new BonsaiNotification(
                    "Disease Treated", 
                    $"You successfully treated {Name}'s disease! It should recover fully soon.",
                    NotificationSeverity.Achievement));
            }
            else
            {
                AddNotification(new BonsaiNotification(
                    "Disease Treatment Started", 
                    $"You've begun treating {Name}'s disease. Continue treatment for best results.",
                    NotificationSeverity.Information));
            }
        }
        
        /// <summary>
        /// Enhanced water method with temperature adjustment
        /// </summary>
        public void WaterWithTemperature(WaterTemperature temperature)
        {
            if (IsDead) return;
            
            // Store initial values for comparison
            double initialHydration = Hydration;
            
            // Call base watering method
            Water();
            
            // Apply temperature effects
            switch (temperature)
            {
                case WaterTemperature.Cold:
                    // Cold water is shocking in warm seasons, good in hot seasons
                    if (CurrentSeason == Season.Summer)
                    {
                        StressLevel -= 10.0;
                        Happiness += 5.0;
                    }
                    else if (CurrentSeason == Season.Winter)
                    {
                        StressLevel += 15.0;
                        Happiness -= 10.0;
                        Health -= 5.0;
                    }
                    break;
                    
                case WaterTemperature.Room:
                    // Room temperature is generally good
                    StressLevel -= 5.0;
                    break;
                    
                case WaterTemperature.Warm:
                    // Warm water is good in cold seasons, bad in warm seasons
                    if (CurrentSeason == Season.Winter)
                    {
                        StressLevel -= 10.0;
                        Happiness += 5.0;
                    }
                    else if (CurrentSeason == Season.Summer)
                    {
                        StressLevel += 10.0;
                        Happiness -= 5.0;
                    }
                    break;
            }
            
            // Check if watering matches the bonsai's preference
            if (WaterNeeds == WaterPreference.Low && (Hydration - initialHydration) > 20)
            {
                // Doesn't like too much water
                AddNotification(new BonsaiNotification(
                    "Water Preference", 
                    $"{Name} prefers less water. Consider watering less next time.",
                    NotificationSeverity.Information));
            }
            else if (WaterNeeds == WaterPreference.High && (Hydration - initialHydration) < 15)
            {
                // Wants more water
                AddNotification(new BonsaiNotification(
                    "Water Preference", 
                    $"{Name} loves water and could use more next time!",
                    NotificationSeverity.Information));
            }
            
            // Update stats and check for perfect care streak
            UpdatePerfectCareStreak();
            OnStatsChanged();
        }
        
        /// <summary>
        /// Plays music for the bonsai, affecting its mood
        /// </summary>
        public void PlayMusic(MusicType musicType)
        {
            if (IsDead) return;
            
            // Base happiness increase
            double happinessIncrease = 15.0;
            double stressReduction = 10.0;
            
            // Apply effects based on music type and bonsai preferences
            switch (musicType)
            {
                case MusicType.Classical:
                    // Classical is refined and elegant
                    if (CurrentMood == EmotionalState.Stressed || CurrentMood == EmotionalState.Anxious)
                    {
                        happinessIncrease += 10.0;
                        stressReduction += 15.0;
                    }
                    break;
                    
                case MusicType.Nature:
                    // Nature sounds are always welcome
                    happinessIncrease += 5.0;
                    stressReduction += 10.0;
                    break;
                    
                case MusicType.Upbeat:
                    // Upbeat can help sad trees but stress calm ones
                    if (CurrentMood == EmotionalState.Sad || CurrentMood == EmotionalState.Depressed)
                    {
                        happinessIncrease += 15.0;
                    }
                    else if (CurrentMood == EmotionalState.Content || CurrentMood == EmotionalState.Happy)
                    {
                        stressReduction = 0; // No stress reduction
                        StressLevel += 5.0; // Actually adds a bit of stress
                    }
                    break;
                    
                case MusicType.Meditation:
                    // Meditation is calming
                    stressReduction += 20.0;
                    if (CurrentMood == EmotionalState.Stressed)
                    {
                        happinessIncrease += 15.0;
                    }
                    break;
            }
            
            // Apply the effects
            Happiness = Math.Min(100, Happiness + happinessIncrease);
            StressLevel = Math.Max(0, StressLevel - stressReduction);
            
            // Track activity
            LastActivity = DateTime.Now;
            ActivityScore = Math.Min(100, ActivityScore + 10);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Playing,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Happiness +{happinessIncrease:0.0}, Stress -{stressReduction:0.0}"
            });
            
            // Update stats and trigger notification
            UpdateMood(); // Immediate mood update
            OnStatsChanged();
            
            AddNotification(new BonsaiNotification(
                "Music Session", 
                $"{Name} enjoyed listening to {musicType} music!",
                NotificationSeverity.Information));
        }
        
        /// <summary>
        /// Adjusts the bonsai's exposure to light
        /// </summary>
        public void AdjustLight(LightExposure exposure)
        {
            if (IsDead) return;
            
            // Base effects
            double happinessChange = 0;
            double healthChange = 0;
            double stressChange = 0;
            
            // Match against preferences
            bool matchesPreference = false;
            switch (LightNeeds)
            {
                case LightPreference.FullSun:
                    matchesPreference = (exposure == LightExposure.Direct);
                    break;
                case LightPreference.PartialSun:
                    matchesPreference = (exposure == LightExposure.Filtered);
                    break;
                case LightPreference.Shade:
                    matchesPreference = (exposure == LightExposure.Indirect);
                    break;
            }
            
            // Apply effects based on match
            if (matchesPreference)
            {
                happinessChange = 15.0;
                healthChange = 10.0;
                stressChange = -15.0;
            }
            else
            {
                // Wrong light exposure has negative effects
                if ((LightNeeds == LightPreference.FullSun && exposure == LightExposure.Indirect) ||
                    (LightNeeds == LightPreference.Shade && exposure == LightExposure.Direct))
                {
                    // Severe mismatch
                    happinessChange = -10.0;
                    healthChange = -5.0;
                    stressChange = 15.0;
                }
                else
                {
                    // Moderate mismatch
                    happinessChange = -5.0;
                    healthChange = -2.0;
                    stressChange = 5.0;
                }
            }
            
            // Apply the effects
            Happiness = Math.Max(0, Math.Min(100, Happiness + happinessChange));
            Health = Math.Max(0, Math.Min(100, Health + healthChange));
            StressLevel = Math.Max(0, Math.Min(100, StressLevel + stressChange));
            
            // Track the activity
            LastActivity = DateTime.Now;
            ActivityScore = Math.Min(100, ActivityScore + 5);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.AdjustLight,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Light adjusted to {exposure}"
            });
            
            // Update stats and trigger notification
            OnStatsChanged();
            
            if (matchesPreference)
            {
                AddNotification(new BonsaiNotification(
                    "Perfect Light", 
                    $"{Name} loves the {exposure.ToString().ToLower()} light exposure!",
                    NotificationSeverity.Information));
            }
            else
            {
                AddNotification(new BonsaiNotification(
                    "Light Adjustment", 
                    $"{Name} has been moved to {exposure.ToString().ToLower()} light.",
                    NotificationSeverity.Information));
            }
        }

        #endregion
        
        #region Enhanced Helper Methods
        
        /// <summary>
        /// Determines the season based on calendar month
        /// </summary>
        private Season DetermineSeason(int month)
        {
            return month switch
            {
                12 or 1 or 2 => Season.Winter,
                3 or 4 or 5 => Season.Spring,
                6 or 7 or 8 => Season.Summer,
                _ => Season.Autumn
            };
        }
        
        /// <summary>
        /// Generates appropriate random weather based on season
        /// </summary>
        private Weather GenerateRandomWeather(Random random, Season season)
        {
            // Weather probabilities vary by season
            double roll = random.NextDouble();
            
            return season switch
            {
                Season.Winter => roll switch
                {
                    < 0.3 => Weather.Snow,
                    < 0.6 => Weather.Cloudy,
                    < 0.8 => Weather.Wind,
                    < 0.9 => Weather.Sunny, // Rare in winter
                    _ => Weather.Rain
                },
                
                Season.Spring => roll switch
                {
                    < 0.4 => Weather.Rain,
                    < 0.7 => Weather.Sunny,
                    < 0.85 => Weather.Cloudy,
                    < 0.95 => Weather.Humid,
                    _ => Weather.Wind
                },
                
                Season.Summer => roll switch
                {
                    < 0.6 => Weather.Sunny,
                    < 0.8 => Weather.Humid,
                    < 0.9 => Weather.Cloudy,
                    < 0.95 => Weather.Rain,
                    _ => Weather.Storm
                },
                
                Season.Autumn => roll switch
                {
                    < 0.3 => Weather.Wind,
                    < 0.6 => Weather.Cloudy,
                    < 0.8 => Weather.Rain,
                    < 0.95 => Weather.Sunny,
                    _ => Weather.Storm
                },
                
                _ => Weather.Cloudy // Default
            };
        }
        
        /// <summary>
        /// Determine if weather change is significant enough to notify
        /// </summary>
        private bool IsSignificantWeatherChange(Weather oldWeather, Weather newWeather)
        {
            // Only notify for drastic changes
            if (oldWeather == newWeather) return false;
            
            // Always notify for extreme weather
            if (newWeather == Weather.Storm || newWeather == Weather.Snow) return true;
            
            // Changes from pleasant to unpleasant or vice versa
            bool wasPleasant = oldWeather == Weather.Sunny || oldWeather == Weather.Cloudy;
            bool isPleasant = newWeather == Weather.Sunny || newWeather == Weather.Cloudy;
            
            return wasPleasant != isPleasant;
        }
        
        /// <summary>
        /// Determine if the current weather is preferred by this bonsai
        /// </summary>
        private bool IsWeatherPreferred()
        {
            // Different bonsai styles prefer different weather
            return Style switch
            {
                BonsaiStyle.FormalUpright => CurrentWeather == Weather.Sunny,
                BonsaiStyle.InformalUpright => CurrentWeather == Weather.Cloudy || CurrentWeather == Weather.Sunny,
                BonsaiStyle.Windswept => CurrentWeather == Weather.Wind,
                BonsaiStyle.Cascade => CurrentWeather == Weather.Rain || CurrentWeather == Weather.Humid,
                BonsaiStyle.Slanting => CurrentWeather == Weather.Wind || CurrentWeather == Weather.Sunny,
                _ => false
            };
        }
        
        /// <summary>
        /// Determine if the current season is preferred by this bonsai
        /// </summary>
        private bool IsSeasonPreferred()
        {
            // Different bonsai styles prefer different seasons
            return Style switch
            {
                BonsaiStyle.FormalUpright => CurrentSeason == Season.Summer,
                BonsaiStyle.InformalUpright => CurrentSeason == Season.Spring,
                BonsaiStyle.Windswept => CurrentSeason == Season.Autumn,
                BonsaiStyle.Cascade => CurrentSeason == Season.Spring || CurrentSeason == Season.Summer,
                BonsaiStyle.Slanting => CurrentSeason == Season.Autumn,
                _ => false
            };
        }
        
        /// <summary>
        /// Calculate the emotional state based on mood score
        /// </summary>
        private EmotionalState DetermineMood()
        {
            return MoodScore switch
            {
                < -70 => EmotionalState.Depressed,
                < -40 => EmotionalState.Sad,
                < -10 => EmotionalState.Anxious,
                < 10 => EmotionalState.Neutral,
                < 40 => EmotionalState.Content,
                < 70 => EmotionalState.Happy,
                _ => EmotionalState.Thriving
            };
        }
        
        /// <summary>
        /// Calculate days since last activity
        /// </summary>
        private int DaysSinceLastActivity()
        {
            return (int)(DateTime.Now - LastActivity).TotalDays;
        }
        
        /// <summary>
        /// Check for perfect care conditions and update streak
        /// </summary>
        private void UpdatePerfectCareStreak()
        {
            // Define perfect care conditions
            bool perfectCare = Health > 80 && 
                               Happiness > 75 && 
                               Hunger < 30 && 
                               Hydration > 60 && Hydration < 90 &&
                               StressLevel < 30 &&
                               !HasPests && 
                               !HasDisease;
            
            if (perfectCare)
            {
                PerfectCareStreak++;
                
                // Achievements for care streaks
                if (PerfectCareStreak == 3)
                {
                    AddNotification(new BonsaiNotification(
                        "Perfect Care Streak!", 
                        $"You've provided perfect care for {Name} for 3 days in a row!",
                        NotificationSeverity.Achievement));
                }
                else if (PerfectCareStreak == 7)
                {
                    AddNotification(new BonsaiNotification(
                        "Master Caretaker!", 
                        $"A full week of perfect care for {Name}! You're a bonsai master!",
                        NotificationSeverity.Achievement));
                }
            }
            else
            {
                // Reset streak if care isn't perfect
                PerfectCareStreak = 0;
            }
        }
        
        /// <summary>
        /// Check if a notification of a certain type exists
        /// </summary>
        private bool HasNotificationOfType(string title)
        {
            return ActiveNotifications.Exists(n => n.Title.Contains(title));
        }
        
        #endregion
    }
    
    #region Enhanced Enums

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
    
    public enum Weather
    {
        Sunny,
        Cloudy,
        Rain,
        Humid,
        Wind,
        Storm,
        Snow
    }
    
    public enum EmotionalState
    {
        Depressed,
        Sad,
        Anxious,
        Neutral,
        Content,
        Happy,
        Thriving
    }
    
    public enum LightPreference
    {
        FullSun,
        PartialSun,
        Shade
    }
    
    public enum WaterPreference
    {
        Low,
        Moderate,
        High
    }
    
    public enum SoilPreference
    {
        Sandy,
        Loamy,
        Clay
    }
    
    public enum WaterTemperature
    {
        Cold,
        Room,
        Warm
    }
    
    public enum MusicType
    {
        Classical,
        Nature,
        Upbeat,
        Meditation
    }
    
    public enum LightExposure
    {
        Direct,
        Filtered,
        Indirect
    }
    
    #endregion
}