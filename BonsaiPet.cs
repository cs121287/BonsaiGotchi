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
    /// Main class for the virtual bonsai pet system
    /// </summary>
    public partial class BonsaiPet
    {
        #region Core Properties

        // Basic stats
        public string Name { get; private set; }
        public double Health { get; set; } = 100;
        public double Happiness { get; set; } = 100;
        public double Hunger { get; set; } = 0;
        public double Growth { get; set; } = 0;
        
        // Extended stats (Phase 2)
        public double Hydration { get; set; } = 100;
        public double PruningQuality { get; set; } = 100;
        public double SoilQuality { get; set; } = 100;
        public double StressLevel { get; set; } = 0;
        public double PestInfestation { get; set; } = 0;
        public double DiseaseLevel { get; set; } = 0;
        
        // Activity tracking (Phase 2)
        public DateTime LastActivity { get; set; }
        public double ActivityScore { get; set; } = 70;
        public int TotalWaterings { get; set; } = 0;
        public int TotalFeedings { get; set; } = 0;
        public int TotalPrunings { get; set; } = 0;
        public int TotalRepottings { get; set; } = 0;
        public int TotalPestsRemoved { get; set; } = 0;
        public int PerfectCareStreak { get; set; } = 0;
        
        // Time and aging
        public int Age { get; private set; } = 0;
        public DateTime CreationDate { get; private set; }
        public DateTime InGameTime { get; private set; }
        public double TimeMultiplier { get; private set; } = 1.0;
        
        // Growth stage
        public GrowthStage CurrentStage { get; private set; } = GrowthStage.Seedling;
        
        // Visual attributes
        public BonsaiStyle Style { get; private set; } = BonsaiStyle.FormalUpright;
        public int TreeSeed { get; private set; }
        
        // Care history
        public List<CareAction> CareHistory { get; private set; } = new List<CareAction>();
        
        // Notifications
        public List<BonsaiNotification> ActiveNotifications { get; private set; } = new List<BonsaiNotification>();
        
        // Status flags
        public bool IsSick => Health < 30 || DiseaseLevel > 50;
        public bool IsDead => Health <= 0;
        public bool HasPests => PestInfestation > 30;
        public bool HasDisease => DiseaseLevel > 30;
        
        // Environmental preferences (can be unique per bonsai)
        public LightPreference LightNeeds { get; set; }
        public WaterPreference WaterNeeds { get; set; }
        public SoilPreference SoilNeeds { get; set; }
        
        // Season and weather system
        public Season CurrentSeason { get; set; }
        public Weather CurrentWeather { get; set; }
        public DateTime LastSeasonChange { get; set; }
        public DateTime LastWeatherChange { get; set; }
        
        // Stress and emotional state
        public EmotionalState CurrentMood { get; set; }
        public int MoodScore { get; set; } // -100 to 100, affects visual cues
        
        // Activity timing bonuses/penalties (Phase 3)
        public bool MorningActivityBonus { get; set; }
        public bool EveningActivityBonus { get; set; }
        public bool NightActivityPenalty { get; set; }
        
        // Additional information about the bonsai's origin (Phase 3)
        public bool IsFromBreeding { get; set; }
        public Guid? ParentId1 { get; set; }
        public Guid? ParentId2 { get; set; }

        #endregion

        #region Events

        // Notification of state changes
        public event EventHandler StatsChanged;
        public event EventHandler<NotificationEventArgs> NotificationTriggered;
        public event EventHandler<StageAdvancedEventArgs> StageAdvanced;

        #endregion

        // Random number generator
        private Random random;
        
        #region Constructors

        /// <summary>
        /// Create a new bonsai pet with a given name
        /// </summary>
        public BonsaiPet(string name, Random randomGenerator = null)
        {
            Name = name;
            CreationDate = DateTime.Now;
            InGameTime = new DateTime(1, 1, 1, 0, 0, 0); // Start at midnight on day 1
            
            // Use provided random generator or create new one
            random = randomGenerator ?? new Random();
            
            // Generate random seed for tree generation
            TreeSeed = random.Next();
            
            // Pick a random style
            Style = (BonsaiStyle)random.Next(0, Enum.GetValues(typeof(BonsaiStyle)).Length);
            
            // Initialize activity tracking
            LastActivity = DateTime.Now;
            
            // Initialize enhanced properties
            InitializeEnhancedProperties(random);
        }
        
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
            ActivityScore = 70 + random.Next(0, 30);
            
            // Initialize environmental preferences (random for each bonsai)
            LightNeeds = (LightPreference)random.Next(0, Enum.GetValues(typeof(LightPreference)).Length);
            WaterNeeds = (WaterPreference)random.Next(0, Enum.GetValues(typeof(WaterPreference)).Length);
            SoilNeeds = (SoilPreference)random.Next(0, Enum.GetValues(typeof(SoilPreference)).Length);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Update the bonsai state based on elapsed time
        /// </summary>
        public void Update(TimeSpan elapsedTime)
        {
            if (IsDead) return;

            // Convert real-world time to game time based on multiplier
            TimeSpan gameTimeElapsed = TimeSpan.FromMinutes(elapsedTime.TotalMinutes * TimeMultiplier);
            InGameTime = InGameTime.Add(gameTimeElapsed);
            
            // Calculate real-world days passed since last update
            double daysFraction = elapsedTime.TotalHours / 24;
            
            // Update age (in days)
            Age = (int)(DateTime.Now - CreationDate).TotalDays;
            
            // Decay stats naturally over time
            UpdateStats(daysFraction);
            
            // Check for life stage advancement
            CheckForStageAdvancement();
            
            // Check for critical health conditions
            CheckHealthConditions();
            
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
        /// Update the bonsai's stats based on time passing
        /// </summary>
        private void UpdateStats(double daysFraction)
        {
            if (daysFraction < 0.001) return;
            
            // Calculate stat changes per day
            double hungerIncrease = 10.0 * daysFraction;
            double happinessDecrease = 5.0 * daysFraction;
            double hydrationDecrease = 15.0 * daysFraction;
            double soilQualityDecrease = 2.0 * daysFraction;
            double growthIncrease = 2.0 * daysFraction;
            
            // Apply stat changes
            Hunger = Math.Min(100, Hunger + hungerIncrease);
            Happiness = Math.Max(0, Happiness - happinessDecrease);
            Hydration = Math.Max(0, Hydration - hydrationDecrease);
            SoilQuality = Math.Max(0, SoilQuality - soilQualityDecrease);
            
            // Growth increases more if conditions are good
            if (Health > 70 && Hunger < 50 && Hydration > 50 && SoilQuality > 60)
            {
                growthIncrease *= 1.5;
            }
            
            // Apply growth increase based on current stage (growth slows as tree matures)
            if (CurrentStage == GrowthStage.Seedling)
            {
                Growth += growthIncrease * 1.5;
            }
            else if (CurrentStage == GrowthStage.Sapling)
            {
                Growth += growthIncrease * 1.2;
            }
            else if (CurrentStage == GrowthStage.YoungTree)
            {
                Growth += growthIncrease;
            }
            else if (CurrentStage == GrowthStage.MatureTree)
            {
                Growth += growthIncrease * 0.5;
            }
            else if (CurrentStage == GrowthStage.ElderTree)
            {
                Growth += growthIncrease * 0.2;
            }
            
            // Cap growth at 100
            Growth = Math.Min(100, Growth);
            
            // Recalculate health based on other stats
            UpdateHealth();
        }
        
        /// <summary>
        /// Update health based on other factors
        /// </summary>
        private void UpdateHealth()
        {
            // Start with base health value (assuming perfect conditions)
            double targetHealth = 100;
            
            // Reduce health based on negative conditions
            if (Hunger > 70) targetHealth -= (Hunger - 70) * 0.5;
            if (Hydration < 30) targetHealth -= (30 - Hydration) * 0.5;
            if (Hydration > 90) targetHealth -= (Hydration - 90) * 0.3; // Over-watering
            if (SoilQuality < 40) targetHealth -= (40 - SoilQuality) * 0.3;
            if (StressLevel > 60) targetHealth -= (StressLevel - 60) * 0.4;
            if (PestInfestation > 50) targetHealth -= (PestInfestation - 50) * 0.5;
            if (DiseaseLevel > 40) targetHealth -= (DiseaseLevel - 40) * 0.7;
            if (PruningQuality < 30) targetHealth -= (30 - PruningQuality) * 0.2;
            
            // Health changes gradually rather than instantly
            double healthChange = (targetHealth - Health) * 0.1;
            Health = Math.Max(0, Math.Min(100, Health + healthChange));
            
            // Trigger events if health reaches critical levels
            if (Health <= 20 && !HasNotificationOfType("LowHealth"))
            {
                AddNotification(new BonsaiNotification(
                    "Critical Health",
                    $"{Name} is in critical condition! Take immediate action to save your bonsai.",
                    NotificationSeverity.Critical));
            }
            else if (Health <= 40 && !HasNotificationOfType("PoorHealth"))
            {
                AddNotification(new BonsaiNotification(
                    "Poor Health",
                    $"{Name} isn't doing well. Take better care of your bonsai.",
                    NotificationSeverity.Warning));
            }
        }
        
        /// <summary>
        /// Check if bonsai has advanced to the next growth stage
        /// </summary>
        private void CheckForStageAdvancement()
        {
            GrowthStage oldStage = CurrentStage;
            
            // Determine new stage based on growth
            if (Growth >= 98 && CurrentStage == GrowthStage.MatureTree)
            {
                CurrentStage = GrowthStage.ElderTree;
            }
            else if (Growth >= 75 && CurrentStage == GrowthStage.YoungTree)
            {
                CurrentStage = GrowthStage.MatureTree;
            }
            else if (Growth >= 50 && CurrentStage == GrowthStage.Sapling)
            {
                CurrentStage = GrowthStage.YoungTree;
            }
            else if (Growth >= 25 && CurrentStage == GrowthStage.Seedling)
            {
                CurrentStage = GrowthStage.Sapling;
            }
            
            // Notify if stage has advanced
            if (CurrentStage != oldStage)
            {
                // Trigger stage advancement event
                StageAdvanced?.Invoke(this, new StageAdvancedEventArgs(oldStage, CurrentStage));
                
                // Add notification
                AddNotification(new BonsaiNotification(
                    "Stage Advanced!",
                    $"{Name} has grown to the {CurrentStage} stage!",
                    NotificationSeverity.Achievement));
            }
        }
        
        /// <summary>
        /// Check for critical health conditions
        /// </summary>
        private void CheckHealthConditions()
        {
            if (Health <= 0 && !IsDead)
            {
                // Bonsai has died
                AddNotification(new BonsaiNotification(
                    "Bonsai Died",
                    $"{Name} has died. Take better care of your next bonsai.",
                    NotificationSeverity.Critical));
            }
            else if (IsSick && !HasNotificationOfType("Sick"))
            {
                // Bonsai is sick
                AddNotification(new BonsaiNotification(
                    "Bonsai Sick",
                    $"{Name} is sick and needs medical attention.",
                    NotificationSeverity.Alert));
            }
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

        #region Care Actions

        /// <summary>
        /// Water the bonsai
        /// </summary>
        public void Water()
        {
            if (IsDead) return;
            
            // Store initial values for comparison
            double initialHydration = Hydration;
            
            // Calculate base watering amount
            double wateringAmount = 30.0;
            
            // Apply time of day bonus/penalty if available
            wateringAmount *= GetTimeOfDayBonusFactor();
            
            // Apply the watering amount
            Hydration = Math.Min(100, Hydration + wateringAmount);
            
            // Reduce hunger slightly as water provides some nutrients
            Hunger = Math.Max(0, Hunger - 5.0);
            
            // Happiness increases slightly with watering
            Happiness = Math.Min(100, Happiness + 5.0);
            
            // Track the watering
            TotalWaterings++;
            LastActivity = DateTime.Now;
            ActivityScore = Math.Min(100, ActivityScore + 10);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Watering,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Hydration +{Hydration - initialHydration:0.0}"
            });
            
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
            
            // Check for over-watering
            if (Hydration > 90 && initialHydration > 70)
            {
                // Tree is being over-watered
                StressLevel = Math.Min(100, StressLevel + 10);
                AddNotification(new BonsaiNotification(
                    "Over-Watering Warning",
                    $"{Name} is getting too much water. Let the soil dry out before watering again.",
                    NotificationSeverity.Warning));
            }
            
            // Update stats and check for perfect care streak
            UpdatePerfectCareStreak();
            OnStatsChanged();
        }
        
        /// <summary>
        /// Enhanced water method with time of day effects
        /// </summary>
        public void EnhancedWater()
        {
            if (IsDead) return;
            
            // Store initial hydration for comparison
            double initialHydration = Hydration;
            
            // Call base watering method
            Water();
            
            // Apply time of day bonus or penalty
            double hydrationChange = Hydration - initialHydration;
            double adjustedChange = hydrationChange * GetTimeOfDayBonusFactor() - hydrationChange;
            
            Hydration = Math.Max(0, Math.Min(100, Hydration + adjustedChange));
            
            // Add notification if there was a significant bonus or penalty
            if (GetTimeOfDayBonusFactor() > 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Watering Bonus", 
                    $"Watering at this time of day is especially effective!",
                    NotificationSeverity.Information));
            }
            else if (GetTimeOfDayBonusFactor() < 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Watering Penalty", 
                    $"Watering at night is less effective. Try watering in the morning for best results.",
                    NotificationSeverity.Warning));
            }
        }

        /// <summary>
        /// Feed the bonsai
        /// </summary>
        public void Feed()
        {
            if (IsDead) return;
            
            // Store initial values
            double initialHunger = Hunger;
            
            // Calculate base feeding amount
            double feedingAmount = 25.0;
            
            // Apply time of day bonus/penalty if available
            feedingAmount *= GetTimeOfDayBonusFactor();
            
            // Apply the feeding
            Hunger = Math.Max(0, Hunger - feedingAmount);
            
            // Improve soil quality
            SoilQuality = Math.Min(100, SoilQuality + 10);
            
            // Happiness increases
            Happiness = Math.Min(100, Happiness + 8);
            
            // Growth gets a small boost
            Growth = Math.Min(100, Growth + 1);
            
            // Track the feeding
            TotalFeedings++;
            LastActivity = DateTime.Now;
            ActivityScore = Math.Min(100, ActivityScore + 15);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Feeding,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Hunger -{initialHunger - Hunger:0.0}, Soil +10"
            });
            
            // Update stats
            UpdatePerfectCareStreak();
            OnStatsChanged();
        }
        
        /// <summary>
        /// Enhanced feeding method that considers time of day
        /// </summary>
        public void EnhancedFeed()
        {
            if (IsDead) return;
            
            double bonusFactor = GetTimeOfDayBonusFactor();
            
            // Store initial values for comparison
            double initialHunger = Hunger;
            
            // Call base feeding method
            Feed();
            
            // Apply time of day bonus or penalty
            double hungerChange = initialHunger - Hunger; // Hunger decreases when feeding
            double adjustedChange = hungerChange * bonusFactor - hungerChange;
            
            Hunger = Math.Max(0, Math.Min(100, Hunger - adjustedChange));
            
            // Add notification if there was a significant bonus or penalty
            if (bonusFactor > 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Feeding Bonus", 
                    $"Feeding at this time of day is especially effective!",
                    NotificationSeverity.Information));
            }
            else if (bonusFactor < 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Feeding Penalty", 
                    $"Feeding at night is less effective. Try feeding during the day for best results.",
                    NotificationSeverity.Warning));
            }
        }
        
        /// <summary>
        /// Prune the bonsai
        /// </summary>
        public void Prune()
        {
            if (IsDead) return;
            
            // Store initial values
            double initialPruningQuality = PruningQuality;
            
            // Calculate base pruning amount
            double pruningAmount = 35.0;
            
            // Apply time of day bonus/penalty if available
            pruningAmount *= GetTimeOfDayBonusFactor();
            
            // Apply the pruning
            PruningQuality = Math.Min(100, PruningQuality + pruningAmount);
            
            // Reduces stress slightly
            StressLevel = Math.Max(0, StressLevel - 5);
            
            // Improves appearance and happiness
            Happiness = Math.Min(100, Happiness + 10);
            
            // Track the pruning
            TotalPrunings++;
            LastActivity = DateTime.Now;
            ActivityScore = Math.Min(100, ActivityScore + 20);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Pruning,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Pruning +{PruningQuality - initialPruningQuality:0.0}, Stress -5"
            });
            
            // Update stats
            UpdatePerfectCareStreak();
            OnStatsChanged();
        }
        
        /// <summary>
        /// Enhanced pruning method that considers time of day
        /// </summary>
        public void EnhancedPrune()
        {
            if (IsDead) return;
            
            double bonusFactor = GetTimeOfDayBonusFactor();
            
            // Store initial values for comparison
            double initialPruningQuality = PruningQuality;
            
            // Call base pruning method
            Prune();
            
            // Apply time of day bonus or penalty
            double pruningChange = PruningQuality - initialPruningQuality;
            double adjustedChange = pruningChange * bonusFactor - pruningChange;
            
            PruningQuality = Math.Max(0, Math.Min(100, PruningQuality + adjustedChange));
            
            // Add notification if there was a significant bonus or penalty
            if (bonusFactor > 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Pruning Bonus", 
                    $"Pruning at this time of day is especially effective!",
                    NotificationSeverity.Information));
            }
            else if (bonusFactor < 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Pruning Penalty", 
                    $"Pruning at night is less effective. Try pruning in the evening for best results.",
                    NotificationSeverity.Warning));
            }
        }
        
        /// <summary>
        /// Repot the bonsai
        /// </summary>
        public void Repot()
        {
            if (IsDead) return;
            
            // Repotting causes temporary stress but improves soil quality
            StressLevel = Math.Min(100, StressLevel + 20);
            
            // Restore soil quality
            SoilQuality = 100;
            
            // Reset pest infestation in soil
            PestInfestation = Math.Max(0, PestInfestation - 40);
            
            // Reset disease related to soil
            DiseaseLevel = Math.Max(0, DiseaseLevel - 25);
            
            // Small growth boost
            Growth = Math.Min(100, Growth + 5);
            
            // Track the repotting
            TotalRepottings++;
            LastActivity = DateTime.Now;
            ActivityScore = Math.Min(100, ActivityScore + 30);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Repotting,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = "Soil Quality restored to 100%, +20 Stress temporarily"
            });
            
            // Add notification
            AddNotification(new BonsaiNotification(
                "Repotting Complete",
                $"{Name} has been repotted with fresh soil. It will be stressed for a while but will recover.",
                NotificationSeverity.Information));
            
            // Update stats
            UpdatePerfectCareStreak();
            OnStatsChanged();
        }
        
        /// <summary>
        /// Play with the bonsai to increase happiness
        /// </summary>
        public void Play()
        {
            Play(100.0); // Default full play session
        }
        
        /// <summary>
        /// Play with the bonsai with a specific effectiveness
        /// </summary>
        /// <param name="effectiveness">How effective the play is (0-100)</param>
        public void Play(double effectiveness)
        {
            if (IsDead) return;
            
            // Calculate happiness increase based on effectiveness
            double happinessIncrease = 20.0 * (effectiveness / 100.0);
            
            // Apply happiness increase
            Happiness = Math.Min(100, Happiness + happinessIncrease);
            
            // Decreases stress
            StressLevel = Math.Max(0, StressLevel - (10.0 * (effectiveness / 100.0)));
            
            // Track the activity
            LastActivity = DateTime.Now;
            ActivityScore = Math.Min(100, ActivityScore + 15);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Playing,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Happiness +{happinessIncrease:0.0}, Stress reduced"
            });
            
            // Update stats
            UpdatePerfectCareStreak();
            OnStatsChanged();
        }
        
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

        #region Helper Methods

        /// <summary>
        /// Add a notification
        /// </summary>
        private void AddNotification(BonsaiNotification notification)
        {
            // Add to active notifications
            ActiveNotifications.Add(notification);
            
            // Limit to 20 notifications
            while (ActiveNotifications.Count > 20)
            {
                ActiveNotifications.RemoveAt(0);
            }
            
            // Trigger notification event
            NotificationTriggered?.Invoke(this, new NotificationEventArgs(notification));
        }
        
        /// <summary>
        /// Set the time multiplier
        /// </summary>
        public void SetTimeMultiplier(double multiplier)
        {
            // Clamp to reasonable range
            TimeMultiplier = Math.Max(0.1, Math.Min(100.0, multiplier));
        }
        
        /// <summary>
        /// Notify that stats have changed
        /// </summary>
        private void OnStatsChanged()
        {
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Get bonus factor for care actions based on time of day
        /// </summary>
        public double GetTimeOfDayBonusFactor()
        {
            if (MorningActivityBonus)
                return 1.2; // 20% bonus
            
            if (EveningActivityBonus)
                return 1.1; // 10% bonus
            
            if (NightActivityPenalty)
                return 0.8; // 20% penalty
            
            return 1.0; // No bonus or penalty
        }
        
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
        
        #region Serialization Methods

        /// <summary>
        /// Save the bonsai pet to a file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            // Create serialization options
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IgnoreNullValues = true
            };
            
            // Serialize to JSON
            string json = JsonSerializer.Serialize(this, options);
            
            // Write to file
            File.WriteAllText(filePath, json);
        }
        
        /// <summary>
        /// Load a bonsai pet from a file
        /// </summary>
        public static BonsaiPet LoadFromFile(string filePath)
        {
            // Read the file
            string json = File.ReadAllText(filePath);
            
            // Deserialize from JSON
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true
            };
            
            BonsaiPet bonsai = JsonSerializer.Deserialize<BonsaiPet>(json, options);
            
            // Initialize the random number generator
            bonsai.random = new Random();
            
            return bonsai;
        }

        #endregion
    }
    
    #region Support Classes
    
    /// <summary>
    /// Tracks a care action performed on a bonsai
    /// </summary>
    public class CareAction
    {
        public CareActionType ActionType { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime GameTimestamp { get; set; }
        public string EffectDescription { get; set; }
    }
    
    /// <summary>
    /// Notification about bonsai state
    /// </summary>
    public class BonsaiNotification
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public BonsaiNotification(string title, string message, NotificationSeverity severity)
        {
            Title = title;
            Message = message;
            Severity = severity;
        }
    }
    
    /// <summary>
    /// Event args for notification event
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        public BonsaiNotification Notification { get; }
        
        public NotificationEventArgs(BonsaiNotification notification)
        {
            Notification = notification;
        }
    }
    
    /// <summary>
    /// Event args for stage advancement event
    /// </summary>
    public class StageAdvancedEventArgs : EventArgs
    {
        public GrowthStage OldStage { get; }
        public GrowthStage NewStage { get; }
        
          public StageAdvancedEventArgs(GrowthStage oldStage, GrowthStage newStage)
        {
            OldStage = oldStage;
            NewStage = newStage;
        }
    }
    
    #endregion
    
    #region Enums
    
    /// <summary>
    /// Type of care action performed
    /// </summary>
    public enum CareActionType
    {
        Watering,
        Feeding,
        Pruning,
        Repotting,
        Playing,
        Medicine,
        PestRemoval,
        AdjustLight
    }
    
    /// <summary>
    /// Notification severity for UI display
    /// </summary>
    public enum NotificationSeverity
    {
        Information,
        Warning,
        Alert,
        Critical,
        Achievement
    }
    
    /// <summary>
    /// Bonsai growth stage
    /// </summary>
    public enum GrowthStage
    {
        Seedling,
        Sapling,
        YoungTree,
        MatureTree,
        ElderTree
    }
    
    /// <summary>
    /// Bonsai style
    /// </summary>
    public enum BonsaiStyle
    {
        FormalUpright,
        InformalUpright,
        Windswept,
        Cascade,
        Slanting
    }
    
    /// <summary>
    /// Season
    /// </summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
    
    /// <summary>
    /// Weather
    /// </summary>
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
    
    /// <summary>
    /// Emotional state
    /// </summary>
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
    
    /// <summary>
    /// Light preference
    /// </summary>
    public enum LightPreference
    {
        FullSun,
        PartialSun,
        Shade
    }
    
    /// <summary>
    /// Water preference
    /// </summary>
    public enum WaterPreference
    {
        Low,
        Moderate,
        High
    }
    
    /// <summary>
    /// Soil preference
    /// </summary>
    public enum SoilPreference
    {
        Sandy,
        Loamy,
        Clay
    }
    
    /// <summary>
    /// Water temperature
    /// </summary>
    public enum WaterTemperature
    {
        Cold,
        Room,
        Warm
    }
    
    /// <summary>
    /// Music type
    /// </summary>
    public enum MusicType
    {
        Classical,
        Nature,
        Upbeat,
        Meditation
    }
    
    /// <summary>
    /// Light exposure
    /// </summary>
    public enum LightExposure
    {
        Direct,
        Filtered,
        Indirect
    }
    
    #endregion
}