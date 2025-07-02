using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace BonsaiGotchiGame.Models
{
    public enum BonsaiState
    {
        Idle,
        Growing,
        Blooming,
        Sleeping,
        Thirsty,
        Wilting,
        Unhealthy
    }

    public enum GrowthStage
    {
        Seedling,
        Sapling,
        YoungBonsai,
        MatureBonsai,
        ElderBonsai,
        AncientBonsai,
        LegendaryBonsai
    }

    public enum HealthCondition
    {
        Healthy,
        RootRot,
        LeafSpot,
        PestInfestation,
        NutrientDeficiency,
        Sunburn,
        Overtraining
    }

    public enum MoodState
    {
        Ecstatic,
        Happy,
        Content,
        Neutral,
        Unhappy,
        Sad,
        Miserable
    }

    public class Bonsai : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _water;
        private int _health;
        private int _growth;
        private int _energy;
        private int _age;
        private BonsaiState _currentState;
        private DateTime _lastUpdateTime;

        // New game time properties
        private int _gameHour;
        private int _gameMinute;
        private int _gameDay;
        private int _gameMonth;
        private int _gameYear;

        // New XP system properties
        private int _xp;
        private int _level;
        private int _mood;
        private int _hunger;
        private int _cleanliness;
        private GrowthStage _growthStage;
        private MoodState _moodState;
        private HealthCondition _healthCondition;
        private int _consecutiveDaysGoodCare;
        private Dictionary<string, DateTime> _actionCooldowns;
        private Dictionary<string, bool> _activeEffects;

        // Cooldowns for actions (in hours)
        private readonly Dictionary<string, int> _actionCooldownTimes = new Dictionary<string, int>
        {
            { "Water", 8 },
            { "Prune", 12 },
            { "Rest", 2 },
            { "Fertilize", 24 },
            { "CleanArea", 12 },
            { "LightExercise", 4 },
            { "IntenseTraining", 8 },
            { "Play", 6 },
            { "Meditation", 4 }
        };

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public int Water
        {
            get => _water;
            set { _water = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
        }

        public int Health
        {
            get => _health;
            set { _health = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
        }

        public int Growth
        {
            get => _growth;
            set { _growth = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
        }

        public int Energy
        {
            get => _energy;
            set { _energy = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
        }

        public int Age
        {
            get => _age;
            set { _age = value; OnPropertyChanged(); }
        }

        public BonsaiState CurrentState
        {
            get => _currentState;
            set { _currentState = value; OnPropertyChanged(); }
        }

        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set { _lastUpdateTime = value; OnPropertyChanged(); }
        }

        // Add properties for in-game time
        public int GameHour
        {
            get => _gameHour;
            set
            {
                _gameHour = value % 24;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GameTimeDisplay));
            }
        }

        public int GameMinute
        {
            get => _gameMinute;
            set
            {
                _gameMinute = value % 60;
                if (_gameMinute == 0 && value != 0) GameHour += 1;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GameTimeDisplay));
            }
        }

        public int GameDay
        {
            get => _gameDay;
            set
            {
                _gameDay = (value - 1) % 30 + 1;
                if (_gameDay == 1 && value > 1) GameMonth += 1;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GameDateDisplay));
            }
        }

        public int GameMonth
        {
            get => _gameMonth;
            set
            {
                _gameMonth = (value - 1) % 12 + 1;
                if (_gameMonth == 1 && value > 1) GameYear += 1;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GameDateDisplay));
            }
        }

        public int GameYear
        {
            get => _gameYear;
            set
            {
                _gameYear = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GameDateDisplay));
            }
        }

        // New XP system properties
        public int XP
        {
            get => _xp;
            set
            {
                _xp = value;
                OnPropertyChanged();
                CheckForLevelUp();
            }
        }

        public int Level
        {
            get => _level;
            private set
            {
                _level = value;
                OnPropertyChanged();
                UpdateGrowthStage();
            }
        }

        public int Mood
        {
            get => _mood;
            set
            {
                _mood = Math.Clamp(value, 0, 100);
                OnPropertyChanged();
                UpdateMoodState();
            }
        }

        public int Hunger
        {
            get => _hunger;
            set { _hunger = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
        }

        public int Cleanliness
        {
            get => _cleanliness;
            set { _cleanliness = Math.Clamp(value, 0, 100); OnPropertyChanged(); }
        }

        public GrowthStage GrowthStage
        {
            get => _growthStage;
            private set { _growthStage = value; OnPropertyChanged(); }
        }

        public MoodState MoodState
        {
            get => _moodState;
            private set { _moodState = value; OnPropertyChanged(); }
        }

        public HealthCondition HealthCondition
        {
            get => _healthCondition;
            set { _healthCondition = value; OnPropertyChanged(); }
        }

        public int ConsecutiveDaysGoodCare
        {
            get => _consecutiveDaysGoodCare;
            set { _consecutiveDaysGoodCare = value; OnPropertyChanged(); }
        }

        // Formatted time and date for display
        public string GameTimeDisplay => $"{GameHour:D2}:{GameMinute:D2}";

        public string GameDateDisplay => $"Day {GameDay}, Month {GameMonth}, Year {GameYear}";

        // XP System displays
        public string LevelDisplay => $"Level {Level} ({XP}/{GetXPForNextLevel()} XP)";

        public string MoodDisplay => MoodState.ToString();

        public string GrowthStageDisplay => GrowthStage.ToString();

        public string HealthConditionDisplay => HealthCondition.ToString();

        public double XPMultiplier => CalculateXPMultiplier();

        public int XPToNextLevel => GetXPForNextLevel() - XP;

        // Action availability properties
        public bool CanWater => !IsActionOnCooldown("Water");
        public bool CanPrune => !IsActionOnCooldown("Prune");
        public bool CanRest => !IsActionOnCooldown("Rest");
        public bool CanFertilize => !IsActionOnCooldown("Fertilize");
        public bool CanCleanArea => !IsActionOnCooldown("CleanArea");
        public bool CanExercise => !IsActionOnCooldown("LightExercise") && Energy > 30;
        public bool CanTrain => !IsActionOnCooldown("IntenseTraining") && Energy > 50;
        public bool CanPlay => !IsActionOnCooldown("Play") && Energy > 30;
        public bool CanMeditate => !IsActionOnCooldown("Meditation");

        public Bonsai(string name = "Bonsai")
        {
            Name = name;
            Water = 50;
            Health = 100;
            Growth = 10;
            Energy = 100;
            Age = 0;
            CurrentState = BonsaiState.Idle;
            LastUpdateTime = DateTime.Now;

            // Initialize game time (start at 6:00 AM on day 1)
            GameHour = 6;
            GameMinute = 0;
            GameDay = 1;
            GameMonth = 1;
            GameYear = 1;

            // Initialize new XP system properties
            XP = 0;
            Level = 1;
            Mood = 70; // Start content
            Hunger = 30; // Somewhat hungry
            Cleanliness = 100; // Clean
            GrowthStage = GrowthStage.Seedling;
            MoodState = MoodState.Content;
            HealthCondition = HealthCondition.Healthy;
            ConsecutiveDaysGoodCare = 0;

            // Initialize cooldown tracking
            _actionCooldowns = new Dictionary<string, DateTime>();
            _activeEffects = new Dictionary<string, bool>();

            // Initial update
            UpdateMoodState();
            UpdateGrowthStage();
        }

        public void GiveWater()
        {
            Water += 30;
            Energy += 10;
            CurrentState = BonsaiState.Growing;

            // XP system updates
            AddExperience(5);
            Mood += 5;
            Hunger += 2; // Slightly increases hunger

            // Set cooldown
            SetActionCooldown("Water");

            // Property updates
            OnPropertyChanged(nameof(CanWater));
        }

        public void Prune()
        {
            Growth += 15;
            Energy -= 10;
            Health += 5;
            CurrentState = BonsaiState.Blooming;

            // XP system updates
            AddExperience(10);
            Mood -= 5; // Initially makes bonsai unhappy
            Hunger += 5;

            // Set active effect to increase mood later
            _activeEffects["PruningMoodBoost"] = true;

            // Set cooldown
            SetActionCooldown("Prune");

            // Property updates
            OnPropertyChanged(nameof(CanPrune));
        }

        public void Rest()
        {
            Energy += 40;
            CurrentState = BonsaiState.Sleeping;

            // XP system updates
            AddExperience(5);
            Mood += 15;
            Hunger -= 5;

            // Set cooldown
            SetActionCooldown("Rest");

            // Property updates
            OnPropertyChanged(nameof(CanRest));
        }

        public void ApplyFertilizer()
        {
            Health += 30;
            Growth += 10;
            CurrentState = BonsaiState.Growing;

            // XP system updates
            AddExperience(15);
            Mood += 2;
            Hunger -= 10;

            // Set cooldown
            SetActionCooldown("Fertilize");

            // Property updates
            OnPropertyChanged(nameof(CanFertilize));
        }

        // New activity methods for XP system
        public void CleanArea()
        {
            // XP system updates
            AddExperience(8);
            Mood += 8;
            Health += 3;
            Energy -= 5;
            Cleanliness += 40;

            // Set cooldown
            SetActionCooldown("CleanArea");

            // Property updates
            OnPropertyChanged(nameof(CanCleanArea));
        }

        public void LightExercise()
        {
            // Check if energy is sufficient
            if (Energy < 30)
                return;

            // XP system updates
            AddExperience(10);
            Mood += 5;
            Health += 10;
            Energy -= 20;
            Hunger += 15;

            // Set cooldown
            SetActionCooldown("LightExercise");

            // Property updates
            OnPropertyChanged(nameof(CanExercise));
        }

        public void IntenseTraining()
        {
            // Check if energy is sufficient
            if (Energy < 50)
                return;

            // XP system updates
            AddExperience(25);
            Mood -= 5; // Initially feels tiring
            Health += 20;
            Energy -= 40;
            Hunger += 30;

            // Set active effect for later mood boost
            _activeEffects["TrainingMoodBoost"] = true;

            // Risk of injury if energy was low
            if (Energy < 30)
            {
                // 30% chance of overtraining when low on energy
                if (new Random().Next(100) < 30)
                {
                    HealthCondition = HealthCondition.Overtraining;
                }
            }

            // Set cooldown
            SetActionCooldown("IntenseTraining");

            // Property updates
            OnPropertyChanged(nameof(CanTrain));
        }

        public void Play()
        {
            // Check if energy is sufficient
            if (Energy < 30)
                return;

            // XP system updates
            AddExperience(15);
            Mood += 20;
            Health += 5;
            Energy -= 25;
            Hunger += 20;

            // Set cooldown
            SetActionCooldown("Play");

            // Property updates
            OnPropertyChanged(nameof(CanPlay));
        }

        public void Meditate()
        {
            // XP system updates
            AddExperience(8);
            Mood += 25;
            Health += 8;
            Energy += 20;

            // Set cooldown
            SetActionCooldown("Meditation");

            // Property updates
            OnPropertyChanged(nameof(CanMeditate));
        }

        // Feeding methods
        public void FeedBasicFertilizer()
        {
            AddExperience(5);
            Health += 5;
            Hunger -= 20;
            Energy += 5;
        }

        public void FeedBurger()
        {
            AddExperience(3);
            Mood += 3;
            Health -= 5;
            Hunger -= 30;
            Energy += 15;

            // Small chance of illness
            if (new Random().Next(100) < 5)
            {
                HealthCondition = HealthCondition.NutrientDeficiency;
            }
        }

        public void FeedIceCream()
        {
            AddExperience(4);
            Mood += 15;
            Health -= 10;
            Hunger -= 15;
            Energy += 20;

            // Higher chance of illness
            if (new Random().Next(100) < 15)
            {
                HealthCondition = HealthCondition.NutrientDeficiency;
            }
        }

        public void FeedVegetables()
        {
            AddExperience(8);
            Mood -= 10; // Initially unhappy
            Health += 15;
            Hunger -= 25;
            Energy += 10;

            // Set active effect for later mood boost
            _activeEffects["VegetablesMoodBoost"] = true;

            // Reduce illness chance
            if (HealthCondition != HealthCondition.Healthy && new Random().Next(100) < 10)
            {
                HealthCondition = HealthCondition.Healthy;
            }
        }

        public void FeedPremiumNutrients()
        {
            AddExperience(15);
            Mood += 10;
            Health += 20;
            Hunger -= 40;
            Energy += 15;

            // Reduce illness chance
            if (HealthCondition != HealthCondition.Healthy && new Random().Next(100) < 5)
            {
                HealthCondition = HealthCondition.Healthy;
            }
        }

        public void FeedSpecialTreat()
        {
            AddExperience(20);
            Mood += 25;
            Health -= 5;
            Hunger -= 10;
            Energy += 30;

            // Chance of illness
            if (new Random().Next(100) < 10)
            {
                HealthCondition = HealthCondition.NutrientDeficiency;
            }
        }

        public void UpdateState()
        {
            var timeSinceLastUpdate = DateTime.Now - LastUpdateTime;

            // Get the time progression speed multiplier from settings
            int timeSpeedMultiplier = GameSettings.Instance?.TimeProgressionSpeed ?? 1;

            // Determine minutes passed and update stats accordingly
            // Multiply by the speed setting to adjust time progression rate
            var minutesPassed = timeSinceLastUpdate.TotalMinutes * timeSpeedMultiplier;

            // Update in-game time (each real minute * speed = game minutes)
            int gameMinutesToAdd = (int)(minutesPassed * 60); // Convert to game minutes
            if (gameMinutesToAdd > 0)
            {
                // Update game minutes and handle rollover
                int newMinutes = GameMinute + gameMinutesToAdd;
                GameMinute = newMinutes % 60;

                // Hours from minutes rollover
                int hoursToAdd = newMinutes / 60;
                if (hoursToAdd > 0)
                {
                    int newHours = GameHour + hoursToAdd;
                    GameHour = newHours % 24;

                    // Days from hours rollover
                    int daysToAdd = newHours / 24;
                    if (daysToAdd > 0)
                    {
                        GameDay += daysToAdd;

                        // Daily evaluation of care
                        EvaluateDailyCare();
                    }
                }
            }

            // Basic stat decay over time
            Water -= (int)(minutesPassed * 0.08); // -4.8 per hour
            Energy -= (int)(minutesPassed * 0.03); // -1.8 per hour
            Mood -= (int)(minutesPassed * 0.017); // -1 per hour
            Hunger += (int)(minutesPassed * 0.05); // +3 per hour
            Cleanliness -= (int)(minutesPassed * 0.002); // -3 per day

            // Passive growth when conditions are good
            if (Water > 40 && Health > 60 && Energy > 30 && Hunger < 70)
            {
                Growth += (int)(minutesPassed * 0.01); // +0.6 per hour when healthy
            }

            // Health decreases if water is too low or hunger too high
            if (Water < 20)
            {
                Health -= (int)(minutesPassed * 0.033); // -2 per hour

                // Risk of health condition
                if (Health < 30 && HealthCondition == HealthCondition.Healthy && new Random().Next(100) < 10)
                {
                    HealthCondition = HealthCondition.NutrientDeficiency;
                }
            }

            if (Hunger > 80)
            {
                Health -= (int)(minutesPassed * 0.017); // -1 per hour
                Mood -= (int)(minutesPassed * 0.033); // -2 per hour
            }

            // Low cleanliness affects health and mood
            if (Cleanliness < 30)
            {
                Health -= (int)(minutesPassed * 0.008); // -0.5 per hour
                Mood -= (int)(minutesPassed * 0.017); // -1 per hour

                // Risk of health condition
                if (HealthCondition == HealthCondition.Healthy && new Random().Next(100) < 5)
                {
                    HealthCondition = new Random().Next(2) == 0 ?
                        HealthCondition.LeafSpot : HealthCondition.PestInfestation;
                }
            }

            // Update health condition effects
            UpdateHealthConditionEffects(minutesPassed);

            // Process active effects like delayed mood changes
            ProcessActiveEffects();

            // Update age (1 day per real hour, affected by time speed)
            Age += (int)(timeSinceLastUpdate.TotalHours * timeSpeedMultiplier);

            // Update state based on stats
            if (Health < 30)
                CurrentState = BonsaiState.Unhealthy;
            else if (Energy < 20)
                CurrentState = BonsaiState.Wilting;
            else if (Water < 30)
                CurrentState = BonsaiState.Thirsty;
            else if (CurrentState != BonsaiState.Blooming && CurrentState != BonsaiState.Growing && CurrentState != BonsaiState.Sleeping)
                CurrentState = BonsaiState.Idle;

            // Update mood state
            UpdateMoodState();

            // Update XP based on consistent good care
            if (Health > 70 && Water > 70 && Energy > 70 && Cleanliness > 70 && Hunger < 30)
            {
                AddExperience(1); // Small XP for good maintenance
            }

            LastUpdateTime = DateTime.Now;

            // Update all action availability properties
            UpdateActionAvailability();
        }

        // XP System helper methods
        private void AddExperience(int baseXP)
        {
            // Apply mood multiplier
            double moodMultiplier = CalculateXPMultiplier();

            // Apply streak bonus (5% per day of consecutive good care, up to 50%)
            double streakBonus = 1.0 + Math.Min(0.5, ConsecutiveDaysGoodCare * 0.05);

            // Calculate final XP gain
            int finalXP = (int)(baseXP * moodMultiplier * streakBonus);

            // Add XP
            XP += finalXP;
        }

        private void CheckForLevelUp()
        {
            int nextLevel = Level + 1;
            int requiredXP = GetXPForNextLevel();

            if (XP >= requiredXP)
            {
                Level = nextLevel;

                // Update growth stage based on new level
                UpdateGrowthStage();

                // Mood boost on level up
                Mood += 20;

                // Any level-up rewards would be triggered here
            }

            OnPropertyChanged(nameof(LevelDisplay));
            OnPropertyChanged(nameof(XPToNextLevel));
        }

        private int GetXPForNextLevel()
        {
            // XP formula: 100 * level^1.5
            return (int)(100 * Math.Pow(Level, 1.5));
        }

        private double CalculateXPMultiplier()
        {
            // Base multiplier from mood state
            return MoodState switch
            {
                MoodState.Ecstatic => 1.3,
                MoodState.Happy => 1.2,
                MoodState.Content => 1.1,
                MoodState.Neutral => 1.0,
                MoodState.Unhappy => 0.9,
                MoodState.Sad => 0.8,
                MoodState.Miserable => 0.7,
                _ => 1.0
            };
        }

        private void UpdateMoodState()
        {
            MoodState = Mood switch
            {
                >= 90 => MoodState.Ecstatic,
                >= 75 => MoodState.Happy,
                >= 60 => MoodState.Content,
                >= 40 => MoodState.Neutral,
                >= 25 => MoodState.Unhappy,
                >= 10 => MoodState.Sad,
                _ => MoodState.Miserable
            };

            OnPropertyChanged(nameof(MoodDisplay));
            OnPropertyChanged(nameof(XPMultiplier));
        }

        private void UpdateGrowthStage()
        {
            GrowthStage = Level switch
            {
                <= 5 => GrowthStage.Seedling,
                <= 15 => GrowthStage.Sapling,
                <= 30 => GrowthStage.YoungBonsai,
                <= 50 => GrowthStage.MatureBonsai,
                <= 75 => GrowthStage.ElderBonsai,
                <= 100 => GrowthStage.AncientBonsai,
                _ => GrowthStage.LegendaryBonsai
            };

            OnPropertyChanged(nameof(GrowthStageDisplay));
        }

        private void EvaluateDailyCare()
        {
            // Check if all vital stats were good
            bool goodCareDay = Health > 70 && Water > 70 && Energy > 70 && Hunger < 30 && Cleanliness > 70;

            if (goodCareDay)
            {
                ConsecutiveDaysGoodCare++;
                AddExperience(10); // Bonus XP for good daily care
            }
            else
            {
                ConsecutiveDaysGoodCare = 0;
            }

            // Ensure growth progresses at least a little each day
            Growth += 1;
        }

        private void UpdateHealthConditionEffects(double minutesPassed)
        {
            switch (HealthCondition)
            {
                case HealthCondition.RootRot:
                    Health -= (int)(minutesPassed * 0.05); // -3 per hour
                    Mood -= (int)(minutesPassed * 0.025); // -1.5 per hour
                    break;

                case HealthCondition.LeafSpot:
                    Health -= (int)(minutesPassed * 0.033); // -2 per hour
                    Mood -= (int)(minutesPassed * 0.017); // -1 per hour

                    // Random chance to recover
                    if (new Random().Next(1000) < minutesPassed && Health > 80)
                    {
                        HealthCondition = HealthCondition.Healthy;
                    }
                    break;

                case HealthCondition.PestInfestation:
                    Health -= (int)(minutesPassed * 0.05); // -3 per hour
                    Mood -= (int)(minutesPassed * 0.033); // -2 per hour
                    Cleanliness -= (int)(minutesPassed * 0.033); // -2 per hour
                    break;

                case HealthCondition.NutrientDeficiency:
                    Health -= (int)(minutesPassed * 0.025); // -1.5 per hour
                    Mood -= (int)(minutesPassed * 0.017); // -1 per hour

                    // Recover with good feeding
                    if (Hunger < 20 && new Random().Next(100) < 10)
                    {
                        HealthCondition = HealthCondition.Healthy;
                    }
                    break;

                case HealthCondition.Sunburn:
                    Health -= (int)(minutesPassed * 0.017); // -1 per hour
                    Mood -= (int)(minutesPassed * 0.017); // -1 per hour

                    // Random chance to recover after some time
                    if (new Random().Next(1000) < minutesPassed * 2)
                    {
                        HealthCondition = HealthCondition.Healthy;
                    }
                    break;

                case HealthCondition.Overtraining:
                    Energy -= (int)(minutesPassed * 0.033); // -2 per hour
                    Mood -= (int)(minutesPassed * 0.033); // -2 per hour

                    // Recover with rest
                    if (Energy > 90 && new Random().Next(100) < 20)
                    {
                        HealthCondition = HealthCondition.Healthy;
                    }
                    break;
            }

            OnPropertyChanged(nameof(HealthConditionDisplay));
        }

        private void ProcessActiveEffects()
        {
            // Handle delayed mood changes and other effects
            if (_activeEffects.TryGetValue("PruningMoodBoost", out bool pruningActive) && pruningActive)
            {
                // 2 hours after pruning, mood improves
                if ((DateTime.Now - GetActionCooldownTime("Prune")).TotalHours > 2)
                {
                    Mood += 10;
                    _activeEffects["PruningMoodBoost"] = false;
                }
            }

            if (_activeEffects.TryGetValue("TrainingMoodBoost", out bool trainingActive) && trainingActive)
            {
                // After training cooldown, mood improves from accomplishment
                if (!IsActionOnCooldown("IntenseTraining"))
                {
                    Mood += 15;
                    _activeEffects["TrainingMoodBoost"] = false;
                }
            }

            if (_activeEffects.TryGetValue("VegetablesMoodBoost", out bool veggiesActive) && veggiesActive)
            {
                // 4 hours after eating vegetables, mood improves from health benefits
                if ((DateTime.Now - LastUpdateTime).TotalHours > 4)
                {
                    Mood += 5;
                    Health += 5;
                    _activeEffects["VegetablesMoodBoost"] = false;
                }
            }
        }

        private void SetActionCooldown(string action)
        {
            _actionCooldowns[action] = DateTime.Now;
        }

        private bool IsActionOnCooldown(string action)
        {
            if (!_actionCooldowns.TryGetValue(action, out DateTime lastUsed))
                return false;

            if (!_actionCooldownTimes.TryGetValue(action, out int cooldownHours))
                return false;

            return (DateTime.Now - lastUsed).TotalHours < cooldownHours;
        }

        private DateTime GetActionCooldownTime(string action)
        {
            return _actionCooldowns.TryGetValue(action, out DateTime time) ? time : DateTime.MinValue;
        }

        private void UpdateActionAvailability()
        {
            // Update all action availability properties
            OnPropertyChanged(nameof(CanWater));
            OnPropertyChanged(nameof(CanPrune));
            OnPropertyChanged(nameof(CanRest));
            OnPropertyChanged(nameof(CanFertilize));
            OnPropertyChanged(nameof(CanCleanArea));
            OnPropertyChanged(nameof(CanExercise));
            OnPropertyChanged(nameof(CanTrain));
            OnPropertyChanged(nameof(CanPlay));
            OnPropertyChanged(nameof(CanMeditate));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}