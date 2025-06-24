using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;

namespace BonsaiGotchi
{
    /// <summary>
    /// Core class for the bonsai pet, implementing Tamagotchi-like mechanics
    /// </summary>
    public class BonsaiPet
    {
        #region Properties

        // Identity properties
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public Guid Id { get; set; }

        // Core stats (0-100 scale)
        public double Health { get; set; }
        public double Happiness { get; set; }
        public double Hunger { get; set; }
        public double Growth { get; set; }

        // Secondary stats
        public double Hydration { get; set; }
        public double SoilQuality { get; set; }
        public double PruningQuality { get; set; }
        public int Age { get; set; }  // Age in days
        
        // In-game time
        public DateTime InGameTime { get; set; }
        public double TimeMultiplier { get; set; }

        // Development stage
        public GrowthStage CurrentStage { get; set; }
        public int StageProgress { get; set; } // Progress within current stage (0-100)

        // Care history
        public DateTime LastWatered { get; set; }
        public DateTime LastFed { get; set; }
        public DateTime LastPruned { get; set; }
        public DateTime LastRepotted { get; set; }

        // Evolution path and features
        public BonsaiStyle Style { get; set; }
        public List<string> Traits { get; set; }
        public List<CareAction> CareHistory { get; set; }
        public bool IsSick { get; set; }
        public bool IsDead { get; set; }

        // Misc
        public int TreeSeed { get; set; }  // For consistent tree generation
        public List<string> Likes { get; set; }
        public List<string> Dislikes { get; set; }
        
        // Notification system
        [JsonIgnore]
        public List<BonsaiNotification> ActiveNotifications { get; set; }

        #endregion

        #region Events

        // Events for UI updates and notifications
        public event EventHandler StatsChanged;
        public event EventHandler<BonsaiNotification> NotificationTriggered;
        public event EventHandler StageAdvanced;

        #endregion

        #region Constructors

        public BonsaiPet()
        {
            // Default constructor for serialization
            Traits = new List<string>();
            CareHistory = new List<CareAction>();
            ActiveNotifications = new List<BonsaiNotification>();
            Likes = new List<string>();
            Dislikes = new List<string>();
        }

        public BonsaiPet(string name, Random random)
        {
            // Create new bonsai pet
            Name = name;
            Id = Guid.NewGuid();
            BirthDate = DateTime.Now;
            InGameTime = new DateTime(1, 1, 1, 8, 0, 0); // Start at 8 AM on day 1
            TimeMultiplier = 1.0; // Default time speed (1 game hour = 1 real minute)

            // Initialize stats
            Health = 80 + random.NextDouble() * 20;
            Happiness = 70 + random.NextDouble() * 30;
            Hunger = 20 + random.NextDouble() * 20;
            Growth = 0;
            Hydration = 80 + random.NextDouble() * 20;
            SoilQuality = 90 + random.NextDouble() * 10;
            PruningQuality = 100;
            Age = 0;

            // Initialize stage
            CurrentStage = GrowthStage.Seedling;
            StageProgress = 0;

            // Initialize care timestamps
            LastWatered = DateTime.Now;
            LastFed = DateTime.Now;
            LastPruned = DateTime.Now;
            LastRepotted = DateTime.Now;

            // Initialize traits and history
            Style = BonsaiStyle.FormalUpright;
            Traits = new List<string>();
            CareHistory = new List<CareAction>();
            ActiveNotifications = new List<BonsaiNotification>();
            
            // Initialize status flags
            IsSick = false;
            IsDead = false;

            // Initialize tree generation seed
            TreeSeed = random.Next();

            // Generate random likes and dislikes
            GenerateLikesAndDislikes(random);
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Updates the bonsai's state based on elapsed real time
        /// </summary>
        public void Update(TimeSpan elapsedTime)
        {
            if (IsDead) return;

            // Calculate in-game time progression
            double gameMinutesElapsed = elapsedTime.TotalMinutes * 60 * TimeMultiplier;
            InGameTime = InGameTime.AddMinutes(gameMinutesElapsed);

            // Calculate real-world days passed since last update for aging
            double daysFraction = elapsedTime.TotalHours / 24;
            
            // Update age if at least one in-game day has passed
            if (InGameTime.Day > Age + 1)
            {
                int daysPassed = InGameTime.Day - (Age + 1);
                Age += daysPassed;
                
                // Check for stage advancement based on age
                CheckForStageAdvancement();
            }

            // Apply stat degradation
            DegradeStats(daysFraction);
            
            // Update derived stats
            UpdateDerivedStats();
            
            // Check health conditions
            CheckHealthConditions();
            
            // Generate notifications if needed
            GenerateNotifications();
            
            // Raise stats changed event
            OnStatsChanged();
        }

        /// <summary>
        /// Handles watering the bonsai
        /// </summary>
        public void Water()
        {
            if (IsDead) return;

            // Calculate time since last watering
            TimeSpan timeSinceLastWatering = DateTime.Now - LastWatered;

            // Base increase in hydration
            double hydrationIncrease = 30.0;

            // Bonus for optimal watering interval (between 1-3 days)
            double daysSinceWatering = timeSinceLastWatering.TotalDays;
            if (daysSinceWatering >= 1 && daysSinceWatering <= 3)
            {
                hydrationIncrease += 10.0;
            }

            // Penalties for over or under watering
            if (daysSinceWatering < 0.5)
            {
                // Overwatering penalty
                hydrationIncrease = 5.0;
                SoilQuality -= 5.0;
                Health -= 2.0;
                
                AddNotification(new BonsaiNotification(
                    "Overwatering", 
                    "You've watered your bonsai too soon! Be careful not to overwater.", 
                    NotificationSeverity.Warning));
            }
            else if (daysSinceWatering > 5)
            {
                // Severe underwatering
                Health -= 5.0;
                
                AddNotification(new BonsaiNotification(
                    "Severe Underwatering", 
                    "Your bonsai was very thirsty! Try to water more regularly.", 
                    NotificationSeverity.Warning));
            }

            // Apply hydration increase
            Hydration = Math.Min(100, Hydration + hydrationIncrease);
            
            // Update hunger and happiness
            Hunger = Math.Max(0, Hunger - 10.0);
            Happiness = Math.Min(100, Happiness + 5.0);
            
            // Update last watered timestamp
            LastWatered = DateTime.Now;
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Watering,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Hydration +{hydrationIncrease:0.0}"
            });
            
            // Update stats and trigger notification
            OnStatsChanged();
            AddNotification(new BonsaiNotification(
                "Watering", 
                $"{Name} enjoyed the water!", 
                NotificationSeverity.Information));
        }
        
        /// <summary>
        /// Handles feeding (fertilizing) the bonsai
        /// </summary>
        public void Feed()
        {
            if (IsDead) return;

            // Calculate time since last feeding
            TimeSpan timeSinceLastFeeding = DateTime.Now - LastFed;

            // Base decrease in hunger
            double hungerDecrease = 40.0;
            double happinessIncrease = 10.0;
            double growthIncrease = 5.0;
            
            // Bonus for optimal feeding interval (between 3-7 days)
            double daysSinceFeeding = timeSinceLastFeeding.TotalDays;
            if (daysSinceFeeding >= 3 && daysSinceFeeding <= 7)
            {
                hungerDecrease += 10.0;
                happinessIncrease += 5.0;
                growthIncrease += 2.5;
            }

            // Penalties for over or under feeding
            if (daysSinceFeeding < 1.5)
            {
                // Overfeeding penalty
                hungerDecrease = 10.0;
                SoilQuality -= 10.0;
                Health -= 5.0;
                
                AddNotification(new BonsaiNotification(
                    "Overfeeding", 
                    "You've fed your bonsai too soon! The soil is becoming nutrient-heavy.", 
                    NotificationSeverity.Warning));
            }
            else if (daysSinceFeeding > 14)
            {
                // Severe underfeeding
                Health -= 3.0;
                
                AddNotification(new BonsaiNotification(
                    "Undernourished", 
                    "Your bonsai was very hungry! Regular feeding is important.", 
                    NotificationSeverity.Warning));
            }

            // Apply stat changes
            Hunger = Math.Max(0, Hunger - hungerDecrease);
            Happiness = Math.Min(100, Happiness + happinessIncrease);
            Growth = Math.Min(100, Growth + growthIncrease);
            SoilQuality = Math.Min(100, SoilQuality + 5.0);
            
            // Update last fed timestamp
            LastFed = DateTime.Now;
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Feeding,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Hunger -{hungerDecrease:0.0}, Growth +{growthIncrease:0.0}"
            });
            
            // Update stats and trigger notification
            OnStatsChanged();
            AddNotification(new BonsaiNotification(
                "Feeding", 
                $"{Name} absorbed the nutrients!", 
                NotificationSeverity.Information));
        }
        
        /// <summary>
        /// Handles pruning the bonsai
        /// </summary>
        public void Prune()
        {
            if (IsDead) return;

            // Calculate time since last pruning
            TimeSpan timeSinceLastPruning = DateTime.Now - LastPruned;

            // Base effects
            double pruningQualityIncrease = 30.0;
            double happinessChange = -5.0;  // Slight unhappiness from pruning
            
            // Bonus for optimal pruning interval (between 14-30 days)
            double daysSincePruning = timeSinceLastPruning.TotalDays;
            if (daysSincePruning >= 14 && daysSincePruning <= 30)
            {
                pruningQualityIncrease += 20.0;
                happinessChange = 5.0;  // Actually happy if timed right
                Growth += 7.5;  // Promotes healthy growth
            }

            // Penalties for over-pruning
            if (daysSincePruning < 7)
            {
                // Over-pruning penalty
                pruningQualityIncrease = 5.0;
                Health -= 5.0;
                happinessChange = -15.0;
                
                AddNotification(new BonsaiNotification(
                    "Over-pruning", 
                    "You've pruned your bonsai too soon! This is stressful for the tree.", 
                    NotificationSeverity.Warning));
            }

            // Apply stat changes
            PruningQuality = Math.Min(100, PruningQuality + pruningQualityIncrease);
            Happiness = Math.Max(0, Math.Min(100, Happiness + happinessChange));
            
            // Update last pruned timestamp
            LastPruned = DateTime.Now;
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Pruning,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Pruning Quality +{pruningQualityIncrease:0.0}"
            });
            
            // Update stats and trigger notification
            OnStatsChanged();
            AddNotification(new BonsaiNotification(
                "Pruning", 
                $"{Name} has been shaped through pruning!", 
                NotificationSeverity.Information));
        }
        
        /// <summary>
        /// Handles repotting the bonsai
        /// </summary>
        public void Repot()
        {
            if (IsDead) return;

            // Calculate time since last repotting
            TimeSpan timeSinceLastRepotting = DateTime.Now - LastRepotted;

            // Base effects
            double soilQualityIncrease = 50.0;
            double happinessChange = -10.0;  // Stress from repotting
            double healthChange = -5.0;  // Temporary shock
            
            // Bonus for optimal repotting interval (between 180-365 days)
            double daysSinceRepotting = timeSinceLastRepotting.TotalDays;
            if (daysSinceRepotting >= 180)
            {
                soilQualityIncrease += 30.0;
                healthChange = 5.0;  // Long-term health benefit if timed right
            }

            // Penalties for over-repotting
            if (daysSinceRepotting < 90)
            {
                // Over-repotting penalty
                soilQualityIncrease = 20.0;
                healthChange = -15.0;
                happinessChange = -20.0;
                
                AddNotification(new BonsaiNotification(
                    "Frequent Repotting", 
                    "You've repotted your bonsai too soon! This causes significant stress.", 
                    NotificationSeverity.Warning));
            }

            // Apply stat changes
            SoilQuality = Math.Min(100, soilQualityIncrease);
            Happiness = Math.Max(0, Math.Min(100, Happiness + happinessChange));
            Health = Math.Max(0, Math.Min(100, Health + healthChange));
            
            // Update last repotted timestamp
            LastRepotted = DateTime.Now;
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Repotting,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Soil Quality +{soilQualityIncrease:0.0}, Health {healthChange:+0.0;-0.0}"
            });
            
            // Update stats and trigger notification
            OnStatsChanged();
            AddNotification(new BonsaiNotification(
                "Repotting", 
                $"{Name} has been moved to fresh soil!", 
                NotificationSeverity.Information));
        }
        
        /// <summary>
        /// Handles playing a mini-game with the bonsai
        /// </summary>
        public void Play(double gameScore)
        {
            if (IsDead) return;

            // Scale happiness increase based on game score (0-100)
            double happinessIncrease = gameScore * 0.3;
            
            // Apply stat changes
            Happiness = Math.Min(100, Happiness + happinessIncrease);
            
            // Add to care history
            CareHistory.Add(new CareAction 
            { 
                ActionType = CareActionType.Playing,
                Timestamp = DateTime.Now,
                GameTimestamp = InGameTime,
                EffectDescription = $"Happiness +{happinessIncrease:0.0}"
            });
            
            // Update stats and trigger notification
            OnStatsChanged();
            AddNotification(new BonsaiNotification(
                "Playtime", 
                $"{Name} enjoyed the interaction!", 
                NotificationSeverity.Information));
        }
        
        /// <summary>
        /// Changes the time multiplier for in-game time progression
        /// </summary>
        public void SetTimeMultiplier(double multiplier)
        {
            TimeMultiplier = Math.Max(0, Math.Min(10, multiplier));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Degrades stats over time
        /// </summary>
        private void DegradeStats(double daysFraction)
        {
            // Don't degrade if the timespan is too short
            if (daysFraction < 0.001) return;
            
            // Degradation rates per day
            double hydrationDecayRate = 15.0;
            double happinessDecayRate = 5.0;
            double hungerIncreaseRate = 10.0;
            double soilQualityDecayRate = 3.0;
            double pruningQualityDecayRate = 2.0;
            
            // Apply degradation
            Hydration = Math.Max(0, Hydration - (hydrationDecayRate * daysFraction));
            Happiness = Math.Max(0, Happiness - (happinessDecayRate * daysFraction));
            Hunger = Math.Min(100, Hunger + (hungerIncreaseRate * daysFraction));
            SoilQuality = Math.Max(0, SoilQuality - (soilQualityDecayRate * daysFraction));
            PruningQuality = Math.Max(0, PruningQuality - (pruningQualityDecayRate * daysFraction));
        }
        
        /// <summary>
        /// Updates health and growth based on other stats
        /// </summary>
        private void UpdateDerivedStats()
        {
            // Health is derived from hydration, hunger, soil quality
            double newHealth = (Hydration * 0.3) + ((100 - Hunger) * 0.3) + (SoilQuality * 0.3) + (PruningQuality * 0.1);
            
            // Apply smoothing to avoid dramatic changes
            Health = (Health * 0.7) + (newHealth * 0.3);
            Health = Math.Max(0, Math.Min(100, Health));
            
            // Growth increases slowly over time if all conditions are good
            if (Health > 70 && Hunger < 30 && SoilQuality > 50)
            {
                Growth += 0.05;
                Growth = Math.Min(100, Growth);
            }
            
            // Update stage progress
            UpdateStageProgress();
        }

        /// <summary>
        /// Checks for stage advancement based on growth and age
        /// </summary>
        private void CheckForStageAdvancement()
        {
            // Check for stage advancement based on age and growth
            GrowthStage nextStage = CurrentStage;
            
            switch (CurrentStage)
            {
                case GrowthStage.Seedling:
                    if (Age >= 14 && Growth >= 30) nextStage = GrowthStage.Sapling;
                    break;
                case GrowthStage.Sapling:
                    if (Age >= 60 && Growth >= 60) nextStage = GrowthStage.YoungTree;
                    break;
                case GrowthStage.YoungTree:
                    if (Age >= 180 && Growth >= 80) nextStage = GrowthStage.MatureTree;
                    break;
                case GrowthStage.MatureTree:
                    if (Age >= 365 && Growth >= 95) nextStage = GrowthStage.ElderTree;
                    break;
            }
            
            // If stage has advanced
            if (nextStage != CurrentStage)
            {
                CurrentStage = nextStage;
                StageProgress = 0;
                
                AddNotification(new BonsaiNotification(
                    "Growth Milestone!", 
                    $"{Name} has reached a new stage: {CurrentStage}",
                    NotificationSeverity.Achievement));
                
                OnStageAdvanced();
            }
        }
        
        /// <summary>
        /// Updates progress within the current growth stage
        /// </summary>
        private void UpdateStageProgress()
        {
            int maxAgeForStage;
            int minAgeForStage;
            
            // Define age ranges for each stage
            switch (CurrentStage)
            {
                case GrowthStage.Seedling:
                    minAgeForStage = 0;
                    maxAgeForStage = 14;
                    break;
                case GrowthStage.Sapling:
                    minAgeForStage = 14;
                    maxAgeForStage = 60;
                    break;
                case GrowthStage.YoungTree:
                    minAgeForStage = 60;
                    maxAgeForStage = 180;
                    break;
                case GrowthStage.MatureTree:
                    minAgeForStage = 180;
                    maxAgeForStage = 365;
                    break;
                case GrowthStage.ElderTree:
                    minAgeForStage = 365;
                    maxAgeForStage = 1095;  // 3 years
                    break;
                default:
                    minAgeForStage = 0;
                    maxAgeForStage = 100;
                    break;
            }
            
            // Calculate progress percentage within stage
            int ageRange = maxAgeForStage - minAgeForStage;
            if (ageRange <= 0) return;
            
            int ageWithinStage = Age - minAgeForStage;
            StageProgress = (int)Math.Min(100, (ageWithinStage * 100.0) / ageRange);
        }
        
        /// <summary>
        /// Checks health conditions and updates sickness and death status
        /// </summary>
        private void CheckHealthConditions()
        {
            // Check for sickness
            if (Health < 30 || Hydration < 10 || Hunger > 90)
            {
                if (!IsSick)
                {
                    IsSick = true;
                    AddNotification(new BonsaiNotification(
                        "Bonsai Sickness", 
                        $"{Name} is not feeling well! Please attend to its needs.",
                        NotificationSeverity.Alert));
                }
            }
            else
            {
                // Recover from sickness if conditions improve
                if (IsSick && Health > 50 && Hydration > 40 && Hunger < 60)
                {
                    IsSick = false;
                    AddNotification(new BonsaiNotification(
                        "Recovery", 
                        $"{Name} is feeling better now!",
                        NotificationSeverity.Information));
                }
            }
            
            // Check for death
            if (Health <= 0 || (IsSick && Health < 10 && Age > 7))
            {
                IsDead = true;
                AddNotification(new BonsaiNotification(
                    "Bonsai Death", 
                    $"Unfortunately, {Name} has died. You can start a new bonsai pet.",
                    NotificationSeverity.Critical));
            }
            
            // Natural death from old age
            if (Age > 1825)  // 5 years
            {
                IsDead = true;
                AddNotification(new BonsaiNotification(
                    "Natural Death", 
                    $"{Name} has reached the end of its natural life cycle at the age of {Age} days.",
                    NotificationSeverity.Information));
            }
        }
        
        /// <summary>
        /// Generates notifications based on current stats
        /// </summary>
        private void GenerateNotifications()
        {
            // Check hydration status
            if (Hydration < 20)
            {
                AddNotification(new BonsaiNotification(
                    "Watering Needed", 
                    $"{Name} is getting thirsty! Water soon.",
                    Hydration < 10 ? NotificationSeverity.Alert : NotificationSeverity.Warning));
            }
            
            // Check hunger status
            if (Hunger > 70)
            {
                AddNotification(new BonsaiNotification(
                    "Feeding Needed", 
                    $"{Name} needs nutrients! Consider fertilizing.",
                    Hunger > 85 ? NotificationSeverity.Alert : NotificationSeverity.Warning));
            }
            
            // Check soil quality
            if (SoilQuality < 30)
            {
                AddNotification(new BonsaiNotification(
                    "Poor Soil", 
                    $"The soil quality is degrading. Consider repotting soon.",
                    NotificationSeverity.Warning));
            }
            
            // Check pruning quality
            if (PruningQuality < 30)
            {
                AddNotification(new BonsaiNotification(
                    "Pruning Needed", 
                    $"{Name} is getting unruly and needs pruning.",
                    NotificationSeverity.Warning));
            }
            
            // Check happiness
            if (Happiness < 30)
            {
                AddNotification(new BonsaiNotification(
                    "Unhappy Bonsai", 
                    $"{Name} seems unhappy. Try interacting more often.",
                    NotificationSeverity.Warning));
            }
        }
        
        /// <summary>
        /// Adds a notification to the active notifications list
        /// </summary>
        private void AddNotification(BonsaiNotification notification)
        {
            // Add to active notifications and remove duplicates
            if (!ActiveNotifications.Exists(n => n.Title == notification.Title))
            {
                ActiveNotifications.Add(notification);
                
                // Limit the number of active notifications
                while (ActiveNotifications.Count > 10)
                {
                    ActiveNotifications.RemoveAt(0);
                }
                
                // Trigger notification event
                OnNotificationTriggered(notification);
            }
        }
        
        /// <summary>
        /// Generates random likes and dislikes for the bonsai
        /// </summary>
        private void GenerateLikesAndDislikes(Random random)
        {
            Likes = new List<string>();
            Dislikes = new List<string>();
            
            string[] possibleLikes = {
                "Morning sunlight", "Gentle misting", "Calm breezes",
                "Classical music", "Being talked to", "Rain sounds",
                "Regular pruning", "Small amounts of fertilizer",
                "Fresh spring water", "Having visitors", "Being outdoors",
                "Careful wiring", "Natural light cycles", "The color blue"
            };
            
            string[] possibleDislikes = {
                "Direct afternoon sun", "Overwatering", "Strong winds",
                "Loud music", "Being moved often", "Tap water",
                "Over-pruning", "Too much fertilizer", "Being indoors too long",
                "Cold drafts", "Being neglected", "Irregular watering",
                "Dramatic temperature changes", "The color red"
            };
            
            // Generate 3-4 random likes
            int numberOfLikes = random.Next(3, 5);
            for (int i = 0; i < numberOfLikes; i++)
            {
                string like = possibleLikes[random.Next(possibleLikes.Length)];
                if (!Likes.Contains(like))
                {
                    Likes.Add(like);
                }
            }
            
            // Generate 2-3 random dislikes
            int numberOfDislikes = random.Next(2, 4);
            for (int i = 0; i < numberOfDislikes; i++)
            {
                string dislike = possibleDislikes[random.Next(possibleDislikes.Length)];
                if (!Dislikes.Contains(dislike) && !Likes.Contains(dislike))
                {
                    Dislikes.Add(dislike);
                }
            }
        }

        #endregion
        
        #region Event Triggers

        protected virtual void OnStatsChanged()
        {
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }
        
        protected virtual void OnNotificationTriggered(BonsaiNotification notification)
        {
            NotificationTriggered?.Invoke(this, notification);
        }
        
        protected virtual void OnStageAdvanced()
        {
            StageAdvanced?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Save/Load System
        
        /// <summary>
        /// Saves the bonsai pet data to a JSON file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                string jsonString = JsonSerializer.Serialize(this, options);
                File.WriteAllText(filePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving bonsai pet: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Loads bonsai pet data from a JSON file
        /// </summary>
        public static BonsaiPet LoadFromFile(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                var bonsai = JsonSerializer.Deserialize<BonsaiPet>(jsonString);
                
                // Initialize non-serialized properties
                if (bonsai != null)
                {
                    bonsai.ActiveNotifications = new List<BonsaiNotification>();
                }
                
                return bonsai;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading bonsai pet: {ex.Message}");
                throw;
            }
        }

        #endregion
    }

    #region Enums and Support Classes

    public enum GrowthStage
    {
        Seedling,
        Sapling,
        YoungTree,
        MatureTree,
        ElderTree
    }
    
    public enum BonsaiStyle
    {
        FormalUpright,    // Chokkan - straight, formal
        InformalUpright,  // Moyogi - curved, natural
        Windswept,        // Fukinagashi - wind-blown
        Cascade,          // Kengai - waterfall style
        Slanting,         // Shakan - leaning
        LiterallyDying    // Dead or dying
    }
    
    public enum CareActionType
    {
        Watering,
        Feeding,
        Pruning,
        Repotting,
        Playing,
        Medicine
    }
    
    public enum NotificationSeverity
    {
        Information,
        Warning,
        Alert,
        Critical,
        Achievement
    }
    
    public class CareAction
    {
        public CareActionType ActionType { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime GameTimestamp { get; set; }
        public string EffectDescription { get; set; }
    }
    
    public class BonsaiNotification
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        
        public BonsaiNotification(string title, string message, NotificationSeverity severity)
        {
            Title = title;
            Message = message;
            Severity = severity;
            Timestamp = DateTime.Now;
            IsRead = false;
        }
    }

    #endregion
}