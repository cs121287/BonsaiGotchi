using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;

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

    public partial class Bonsai : INotifyPropertyChanged
    {
        private readonly object _updateLock = new object();
        private readonly object _xpLock = new object();
        private readonly object _currencyLock = new object();
        private readonly object _propertyLock = new object();
        protected static readonly Random _random = new Random();

        // Batched property change notifications for performance
        private readonly HashSet<string> _pendingPropertyChanges = new HashSet<string>();
        private readonly Timer? _propertyChangeTimer;
        private bool _isUpdatingProperties = false;
        private const int PropertyChangeDelayMs = 50; // Batch property changes

        private string _name = string.Empty;
        private int _water;
        private int _health;
        private int _growth;
        private int _energy;
        private int _age;
        private BonsaiState _currentState;
        private DateTime _lastUpdateTime;

        // Game time properties
        private int _gameHour;
        private int _gameMinute;
        private int _gameDay;
        private int _gameMonth;
        private int _gameYear;

        // XP system properties
        private int _xp;
        private int _level;
        private int _mood;
        private int _hunger;
        private int _cleanliness;
        private GrowthStage _growthStage;
        private MoodState _moodState;
        private HealthCondition _healthCondition;
        private int _consecutiveDaysGoodCare;
        private Dictionary<string, DateTime> _actionCooldowns = new Dictionary<string, DateTime>();
        protected Dictionary<string, bool> _activeEffects = new Dictionary<string, bool>();

        // Cooldown timings
        private readonly Dictionary<string, int> _actionCooldownTimes = new Dictionary<string, int>
        {
            { "Water", 2 },
            { "Prune", 3 },
            { "Rest", 1 },
            { "Fertilize", 5 },
            { "CleanArea", 4 },
            { "LightExercise", 2 },
            { "IntenseTraining", 3 },
            { "Play", 2 },
            { "Meditation", 2 }
        };

        private HashSet<string> _previousCooldownActions = new HashSet<string>();

        // Cached property values for performance
        private bool _canWater = true;
        private bool _canPrune = true;
        private bool _canRest = true;
        private bool _canFertilize = true;
        private bool _canCleanArea = true;
        private bool _canExercise = true;
        private bool _canTrain = true;
        private bool _canPlay = true;
        private bool _canMeditate = true;

        private int _lastXpGain = 0;
        private Inventory? _inventory;
        private Currency? _currency;

        // Constants for food item IDs
        public const string BASIC_FERTILIZER_ID = "basic_fertilizer";
        public const string BURGER_ID = "burger";
        public const string ICE_CREAM_ID = "ice_cream";
        public const string VEGETABLES_ID = "vegetables";
        public const string PREMIUM_NUTRIENTS_ID = "premium_nutrients";
        public const string SPECIAL_TREAT_ID = "special_treat";

        public Inventory Inventory
        {
            get => _inventory ??= new Inventory();
        }

        public Currency Currency
        {
            get
            {
                lock (_currencyLock)
                {
                    if (_currency == null)
                    {
                        _currency = new Currency();
                        QueuePropertyChanged();
                    }
                    return _currency;
                }
            }
            set
            {
                lock (_currencyLock)
                {
                    if (_currency != value)
                    {
                        _currency = value ?? new Currency();
                        QueuePropertyChanged();
                    }
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                lock (_propertyLock)
                {
                    if (_name != value)
                    {
                        ValidateNameInput(value);
                        _name = value ?? string.Empty;
                        QueuePropertyChanged();
                    }
                }
            }
        }

        public int Water
        {
            get => _water;
            set
            {
                lock (_propertyLock)
                {
                    int clampedValue = Math.Clamp(value, 0, 100);
                    ValidateStatChange(_water, clampedValue, "Water");
                    if (_water != clampedValue)
                    {
                        _water = clampedValue;
                        QueuePropertyChanged();
                        ValidateOverallState();
                    }
                }
            }
        }

        public int Health
        {
            get => _health;
            set
            {
                lock (_propertyLock)
                {
                    int clampedValue = Math.Clamp(value, 0, 100);
                    ValidateStatChange(_health, clampedValue, "Health");
                    if (_health != clampedValue)
                    {
                        _health = clampedValue;
                        QueuePropertyChanged();
                        ValidateOverallState();
                    }
                }
            }
        }

        public int Growth
        {
            get => _growth;
            set
            {
                lock (_propertyLock)
                {
                    int clampedValue = Math.Clamp(value, 0, 100);
                    ValidateStatChange(_growth, clampedValue, "Growth");
                    if (_growth != clampedValue)
                    {
                        _growth = clampedValue;
                        QueuePropertyChanged();
                    }
                }
            }
        }

        public int Energy
        {
            get => _energy;
            set
            {
                lock (_propertyLock)
                {
                    int clampedValue = Math.Clamp(value, 0, 100);
                    ValidateStatChange(_energy, clampedValue, "Energy");
                    if (_energy != clampedValue)
                    {
                        _energy = clampedValue;
                        QueuePropertyChanged();
                        ValidateOverallState();
                    }
                }
            }
        }

        public int Age
        {
            get => _age;
            set
            {
                lock (_propertyLock)
                {
                    int validatedValue = Math.Max(0, value);
                    if (_age != validatedValue)
                    {
                        _age = validatedValue;
                        QueuePropertyChanged();
                    }
                }
            }
        }

        public BonsaiState CurrentState
        {
            get => _currentState;
            set
            {
                lock (_propertyLock)
                {
                    if (_currentState != value && Enum.IsDefined(typeof(BonsaiState), value))
                    {
                        _currentState = value;
                        QueuePropertyChanged();
                    }
                }
            }
        }

        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set
            {
                lock (_propertyLock)
                {
                    DateTime validatedTime = value > DateTime.Now.AddMinutes(5) ? DateTime.Now : value;
                    if (_lastUpdateTime != validatedTime)
                    {
                        _lastUpdateTime = validatedTime;
                        QueuePropertyChanged();
                    }
                }
            }
        }

        // Game time properties with batched notifications
        public int GameHour
        {
            get => _gameHour;
            set
            {
                lock (_propertyLock)
                {
                    int validatedHour = Math.Clamp(value, 0, 23);
                    if (_gameHour != validatedHour)
                    {
                        _gameHour = validatedHour;
                        QueuePropertyChanged();
                        QueuePropertyChanged(nameof(GameTimeDisplay));
                    }
                }
            }
        }

        public int GameMinute
        {
            get => _gameMinute;
            set
            {
                lock (_propertyLock)
                {
                    int validatedMinute = Math.Clamp(value, 0, 59);
                    bool hourRollover = validatedMinute < _gameMinute && value >= 60;

                    _gameMinute = validatedMinute;
                    QueuePropertyChanged();
                    QueuePropertyChanged(nameof(GameTimeDisplay));

                    if (hourRollover)
                    {
                        GameHour = (GameHour + 1) % 24;
                    }
                }
            }
        }

        public int GameDay
        {
            get => _gameDay;
            set
            {
                lock (_propertyLock)
                {
                    int validatedDay = Math.Clamp(value, 1, 30);
                    bool monthRollover = validatedDay < _gameDay && value > 30;

                    _gameDay = validatedDay;
                    QueuePropertyChanged();
                    QueuePropertyChanged(nameof(GameDateDisplay));

                    if (monthRollover)
                    {
                        GameMonth = GameMonth < 12 ? GameMonth + 1 : 1;
                        if (GameMonth == 1)
                        {
                            GameYear++;
                        }
                    }
                }
            }
        }

        public int GameMonth
        {
            get => _gameMonth;
            set
            {
                lock (_propertyLock)
                {
                    int validatedMonth = Math.Clamp(value, 1, 12);
                    if (_gameMonth != validatedMonth)
                    {
                        _gameMonth = validatedMonth;
                        QueuePropertyChanged();
                        QueuePropertyChanged(nameof(GameDateDisplay));
                    }
                }
            }
        }

        public int GameYear
        {
            get => _gameYear;
            set
            {
                lock (_propertyLock)
                {
                    int validatedYear = Math.Max(1, value);
                    if (_gameYear != validatedYear)
                    {
                        _gameYear = validatedYear;
                        QueuePropertyChanged();
                        QueuePropertyChanged(nameof(GameDateDisplay));
                    }
                }
            }
        }

        // XP system properties with optimized notifications
        public int XP
        {
            get => _xp;
            set
            {
                lock (_xpLock)
                {
                    int oldValue = _xp;
                    int validatedXP = Math.Max(0, value);

                    if (_xp != validatedXP)
                    {
                        _xp = validatedXP;
                        _lastXpGain = _xp - oldValue;

                        QueuePropertyChanged();
                        QueuePropertyChanged(nameof(XPToNextLevel));
                        QueuePropertyChanged(nameof(LevelDisplay));
                        QueuePropertyChanged(nameof(XPPercentage));

                        CheckForLevelUp();
                    }
                }
            }
        }

        public int Level
        {
            get => _level;
            private set
            {
                lock (_propertyLock)
                {
                    int validatedLevel = Math.Max(1, value);
                    if (_level != validatedLevel)
                    {
                        _level = validatedLevel;
                        QueuePropertyChanged();

                        UpdateGrowthStage();
                        QueuePropertyChanged(nameof(LevelDisplay));
                        QueuePropertyChanged(nameof(XPToNextLevel));
                        QueuePropertyChanged(nameof(XPPercentage));
                    }
                }
            }
        }

        public int Mood
        {
            get => _mood;
            set
            {
                lock (_propertyLock)
                {
                    int oldValue = _mood;
                    int validatedMood = Math.Clamp(value, 0, 100);
                    ValidateStatChange(oldValue, validatedMood, "Mood");

                    if (_mood != validatedMood)
                    {
                        _mood = validatedMood;
                        QueuePropertyChanged();
                        UpdateMoodState();
                        QueuePropertyChanged(nameof(XPMultiplier));
                    }
                }
            }
        }

        public int Hunger
        {
            get => _hunger;
            set
            {
                lock (_propertyLock)
                {
                    int validatedHunger = Math.Clamp(value, 0, 100);
                    ValidateStatChange(_hunger, validatedHunger, "Hunger");
                    if (_hunger != validatedHunger)
                    {
                        _hunger = validatedHunger;
                        QueuePropertyChanged();
                        ValidateOverallState();
                    }
                }
            }
        }

        public int Cleanliness
        {
            get => _cleanliness;
            set
            {
                lock (_propertyLock)
                {
                    int validatedCleanliness = Math.Clamp(value, 0, 100);
                    ValidateStatChange(_cleanliness, validatedCleanliness, "Cleanliness");
                    if (_cleanliness != validatedCleanliness)
                    {
                        _cleanliness = validatedCleanliness;
                        QueuePropertyChanged();
                        ValidateOverallState();
                    }
                }
            }
        }

        public GrowthStage GrowthStage
        {
            get => _growthStage;
            private set
            {
                lock (_propertyLock)
                {
                    if (_growthStage != value && Enum.IsDefined(typeof(GrowthStage), value))
                    {
                        _growthStage = value;
                        QueuePropertyChanged();
                        QueuePropertyChanged(nameof(GrowthStageDisplay));
                    }
                }
            }
        }

        public MoodState MoodState
        {
            get => _moodState;
            private set
            {
                lock (_propertyLock)
                {
                    if (_moodState != value && Enum.IsDefined(typeof(MoodState), value))
                    {
                        _moodState = value;
                        QueuePropertyChanged();
                        QueuePropertyChanged(nameof(MoodDisplay));
                        QueuePropertyChanged(nameof(XPMultiplier));
                    }
                }
            }
        }

        public HealthCondition HealthCondition
        {
            get => _healthCondition;
            set
            {
                lock (_propertyLock)
                {
                    if (_healthCondition != value && Enum.IsDefined(typeof(HealthCondition), value))
                    {
                        _healthCondition = value;
                        QueuePropertyChanged();
                        QueuePropertyChanged(nameof(HealthConditionDisplay));
                    }
                }
            }
        }

        public int ConsecutiveDaysGoodCare
        {
            get => _consecutiveDaysGoodCare;
            set
            {
                lock (_propertyLock)
                {
                    int validatedDays = Math.Max(0, value);
                    if (_consecutiveDaysGoodCare != validatedDays)
                    {
                        _consecutiveDaysGoodCare = validatedDays;
                        QueuePropertyChanged();
                        QueuePropertyChanged(nameof(XPMultiplier));
                        QueuePropertyChanged(nameof(StreakBonusText));
                    }
                }
            }
        }

        // Computed properties (cached for performance)
        public string GameTimeDisplay => $"{GameHour:D2}:{GameMinute:D2}";
        public string GameDateDisplay => $"Day {GameDay}, Month {GameMonth}, Year {GameYear}";
        public string LevelDisplay => $"Level {Level} ({XP}/{GetXPForNextLevel()} XP)";
        public string MoodDisplay => MoodState.ToString();
        public string GrowthStageDisplay => GrowthStage.ToString();
        public string HealthConditionDisplay => HealthCondition.ToString();

        public double XPMultiplier
        {
            get
            {
                try
                {
                    double moodMultiplier = GetMoodMultiplier();
                    double streakBonus = GetStreakBonus();
                    return Math.Max(0.1, moodMultiplier * (1.0 + streakBonus));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error calculating XP multiplier: {ex.Message}");
                    return 1.0;
                }
            }
        }

        public string StreakBonusText => $"+{GetStreakBonus():P0} from {ConsecutiveDaysGoodCare} day streak";
        public int XPToNextLevel => Math.Max(0, GetXPForNextLevel() - XP);

        public double XPPercentage
        {
            get
            {
                try
                {
                    int maxXP = GetXPForNextLevel();
                    if (maxXP <= 0) return 100;
                    return Math.Min(100, (double)XP / maxXP * 100);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error calculating XP percentage: {ex.Message}");
                    return 0;
                }
            }
        }

        public int LastXPGain => _lastXpGain;

        // Cached action availability properties for performance
        public bool CanWater
        {
            get
            {
                lock (_updateLock)
                {
                    bool result = !IsActionOnCooldown("Water");
                    if (_canWater != result)
                    {
                        _canWater = result;
                        QueuePropertyChanged();
                    }
                    return _canWater;
                }
            }
        }

        public bool CanPrune
        {
            get
            {
                lock (_updateLock)
                {
                    bool result = !IsActionOnCooldown("Prune");
                    if (_canPrune != result)
                    {
                        _canPrune = result;
                        QueuePropertyChanged();
                    }
                    return _canPrune;
                }
            }
        }

        public bool CanRest
        {
            get
            {
                lock (_updateLock)
                {
                    bool result = !IsActionOnCooldown("Rest");
                    if (_canRest != result)
                    {
                        _canRest = result;
                        QueuePropertyChanged();
                    }
                    return _canRest;
                }
            }
        }

        public bool CanFertilize
        {
            get
            {
                lock (_updateLock)
                {
                    bool result = !IsActionOnCooldown("Fertilize");
                    if (_canFertilize != result)
                    {
                        _canFertilize = result;
                        QueuePropertyChanged();
                    }
                    return _canFertilize;
                }
            }
        }

        public bool CanCleanArea
        {
            get
            {
                lock (_updateLock)
                {
                    bool result = !IsActionOnCooldown("CleanArea");
                    if (_canCleanArea != result)
                    {
                        _canCleanArea = result;
                        QueuePropertyChanged();
                    }
                    return _canCleanArea;
                }
            }
        }

        public bool CanExercise
        {
            get
            {
                lock (_updateLock)
                {
                    bool result = !IsActionOnCooldown("LightExercise") && Energy > 30;
                    if (_canExercise != result)
                    {
                        _canExercise = result;
                        QueuePropertyChanged();
                    }
                    return _canExercise;
                }
            }
        }

        public bool CanTrain
        {
            get
            {
                lock (_updateLock)
                {
                    bool result = !IsActionOnCooldown("IntenseTraining") && Energy > 50;
                    if (_canTrain != result)
                    {
                        _canTrain = result;
                        QueuePropertyChanged();
                    }
                    return _canTrain;
                }
            }
        }

        public bool CanPlay
        {
            get
            {
                lock (_updateLock)
                {
                    bool result = !IsActionOnCooldown("Play") && Energy > 30;
                    if (_canPlay != result)
                    {
                        _canPlay = result;
                        QueuePropertyChanged();
                    }
                    return _canPlay;
                }
            }
        }

        public bool CanMeditate
        {
            get
            {
                lock (_updateLock)
                {
                    bool result = !IsActionOnCooldown("Meditation");
                    if (_canMeditate != result)
                    {
                        _canMeditate = result;
                        QueuePropertyChanged();
                    }
                    return _canMeditate;
                }
            }
        }

        public Bonsai(string name = "Bonsai")
        {
            try
            {
                // Initialize property change timer for batching
                _propertyChangeTimer = new Timer(FlushPropertyChanges, null, Timeout.Infinite, Timeout.Infinite);

                Name = name ?? "Bonsai";
                Water = 50;
                Health = 100;
                Growth = 10;
                Energy = 100;
                Age = 0;
                CurrentState = BonsaiState.Idle;
                LastUpdateTime = DateTime.Now;

                // Initialize game time
                GameHour = 6;
                GameMinute = 0;
                GameDay = 1;
                GameMonth = 1;
                GameYear = 1;

                // Initialize XP system
                XP = 0;
                Level = 1;
                Mood = 70;
                Hunger = 30;
                Cleanliness = 100;
                GrowthStage = GrowthStage.Seedling;
                MoodState = MoodState.Content;
                HealthCondition = HealthCondition.Healthy;
                ConsecutiveDaysGoodCare = 0;

                // Initialize collections
                _actionCooldowns = new Dictionary<string, DateTime>();
                _activeEffects = new Dictionary<string, bool>();
                _previousCooldownActions = new HashSet<string>();

                // Initialize systems
                _inventory = new Inventory();
                _currency = new Currency();

                // Add starter items
                _inventory.AddItem(BURGER_ID, 3);
                _inventory.AddItem(ICE_CREAM_ID, 2);
                _inventory.AddItem(VEGETABLES_ID, 5);
                _inventory.AddItem(PREMIUM_NUTRIENTS_ID, 1);
                _inventory.AddItem(SPECIAL_TREAT_ID, 0);

                // Initial updates
                UpdateMoodState();
                UpdateGrowthStage();
                RefreshAllActionAvailability();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Bonsai constructor: {ex.Message}");
                InitializeSafeDefaults();
            }
        }

        private void InitializeSafeDefaults()
        {
            _name = "Bonsai";
            _water = 50;
            _health = 100;
            _energy = 100;
            _level = 1;
            _currency = new Currency();
            _inventory = new Inventory();
        }

        // Optimized property change notification system
        private void QueuePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            lock (_pendingPropertyChanges)
            {
                _pendingPropertyChanges.Add(propertyName);

                // Start or restart the timer
                if (!_isUpdatingProperties && _propertyChangeTimer != null)
                {
                    _propertyChangeTimer.Change(PropertyChangeDelayMs, Timeout.Infinite);
                }
            }
        }

        private void FlushPropertyChanges(object? state)
        {
            HashSet<string> propertiesToNotify;

            lock (_pendingPropertyChanges)
            {
                if (_pendingPropertyChanges.Count == 0 || _isUpdatingProperties)
                    return;

                _isUpdatingProperties = true;
                propertiesToNotify = new HashSet<string>(_pendingPropertyChanges);
                _pendingPropertyChanges.Clear();
            }

            try
            {
                // Fire all pending property change notifications
                foreach (string propertyName in propertiesToNotify)
                {
                    OnPropertyChanged(propertyName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error flushing property changes: {ex.Message}");
            }
            finally
            {
                lock (_pendingPropertyChanges)
                {
                    _isUpdatingProperties = false;
                }
            }
        }

        // Validation methods
        private void ValidateNameInput(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Bonsai name cannot be empty or whitespace only.");

            if (name.Length > 50)
                throw new ArgumentException("Bonsai name cannot exceed 50 characters.");
        }

        private void ValidateStatChange(int oldValue, int newValue, string statName)
        {
            int change = Math.Abs(newValue - oldValue);
            if (change > 50 && oldValue > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Large {statName} change detected: {oldValue} -> {newValue}");
            }
        }

        private void ValidateOverallState()
        {
            if (Health <= 0 && Water > 90)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Inconsistent state - zero health with high water");
            }

            if (Energy <= 0 && Mood > 80)
            {
                System.Diagnostics.Debug.WriteLine("Warning: Inconsistent state - zero energy with high mood");
            }
        }

        // Public methods to help with cooldown visualization
        public TimeSpan GetRemainingCooldown(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return TimeSpan.Zero;

            lock (_updateLock)
            {
                try
                {
                    string cooldownKey = ConvertActionNameToCooldownKey(action);

                    if (!_actionCooldowns.TryGetValue(cooldownKey, out DateTime lastUsed))
                        return TimeSpan.Zero;

                    if (!_actionCooldownTimes.TryGetValue(cooldownKey, out int cooldownMinutes))
                        return TimeSpan.Zero;

                    DateTime cooldownEnd = lastUsed.AddMinutes(cooldownMinutes);
                    TimeSpan remaining = cooldownEnd - DateTime.Now;

                    return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting remaining cooldown: {ex.Message}");
                    return TimeSpan.Zero;
                }
            }
        }

        public TimeSpan GetTotalCooldownTime(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return TimeSpan.Zero;

            try
            {
                string cooldownKey = ConvertActionNameToCooldownKey(action);

                if (!_actionCooldownTimes.TryGetValue(cooldownKey, out int cooldownMinutes))
                    return TimeSpan.Zero;

                return TimeSpan.FromMinutes(cooldownMinutes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting total cooldown time: {ex.Message}");
                return TimeSpan.Zero;
            }
        }

        private string ConvertActionNameToCooldownKey(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return string.Empty;

            return action switch
            {
                "Exercise" => "LightExercise",
                "Train" => "IntenseTraining",
                "Meditate" => "Meditation",
                _ => action
            };
        }

        // Action methods with improved error handling
        public void GiveWater()
        {
            if (!CanWater) return;

            try
            {
                lock (_updateLock)
                {
                    Water += 30;
                    Energy += 10;
                    CurrentState = BonsaiState.Growing;

                    AddExperience(5);
                    Mood += 5;
                    Hunger += 2;

                    SetActionCooldown("Water");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GiveWater: {ex.Message}");
            }
        }

        public void Prune()
        {
            if (!CanPrune) return;

            try
            {
                lock (_updateLock)
                {
                    Growth += 15;
                    Energy -= 10;
                    Health += 5;
                    CurrentState = BonsaiState.Blooming;

                    AddExperience(10);
                    Mood -= 5;
                    Hunger += 5;

                    _activeEffects["PruningMoodBoost"] = true;
                    SetActionCooldown("Prune");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Prune: {ex.Message}");
            }
        }

        public void Rest()
        {
            if (!CanRest) return;

            try
            {
                lock (_updateLock)
                {
                    Energy += 40;
                    CurrentState = BonsaiState.Sleeping;

                    AddExperience(5);
                    Mood += 15;
                    Hunger -= 5;

                    SetActionCooldown("Rest");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Rest: {ex.Message}");
            }
        }

        public void ApplyFertilizer()
        {
            if (!CanFertilize) return;

            try
            {
                lock (_updateLock)
                {
                    Health += 30;
                    Growth += 10;
                    CurrentState = BonsaiState.Growing;

                    AddExperience(15);
                    Mood += 2;
                    Hunger -= 10;

                    SetActionCooldown("Fertilize");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyFertilizer: {ex.Message}");
            }
        }

        public void CleanArea()
        {
            if (!CanCleanArea) return;

            try
            {
                lock (_updateLock)
                {
                    AddExperience(8);
                    Mood += 8;
                    Health += 3;
                    Energy -= 5;
                    Cleanliness += 40;

                    SetActionCooldown("CleanArea");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CleanArea: {ex.Message}");
            }
        }

        public void LightExercise()
        {
            if (!CanExercise) return;

            try
            {
                lock (_updateLock)
                {
                    AddExperience(10);
                    Mood += 5;
                    Health += 10;
                    Energy -= 20;
                    Hunger += 15;

                    SetActionCooldown("LightExercise");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LightExercise: {ex.Message}");
            }
        }

        public void IntenseTraining()
        {
            if (!CanTrain) return;

            try
            {
                lock (_updateLock)
                {
                    AddExperience(25);
                    Mood -= 5;
                    Health += 20;
                    Energy -= 40;
                    Hunger += 30;

                    _activeEffects["TrainingMoodBoost"] = true;

                    if (Energy < 30 && _random.Next(100) < 30)
                    {
                        HealthCondition = HealthCondition.Overtraining;
                    }

                    SetActionCooldown("IntenseTraining");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in IntenseTraining: {ex.Message}");
            }
        }

        public void Play()
        {
            if (!CanPlay) return;

            try
            {
                lock (_updateLock)
                {
                    AddExperience(15);
                    Mood += 20;
                    Health += 5;
                    Energy -= 25;
                    Hunger += 20;

                    SetActionCooldown("Play");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Play: {ex.Message}");
            }
        }

        public void Meditate()
        {
            if (!CanMeditate) return;

            try
            {
                lock (_updateLock)
                {
                    AddExperience(8);
                    Mood += 25;
                    Health += 8;
                    Energy += 20;

                    SetActionCooldown("Meditation");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Meditate: {ex.Message}");
            }
        }

        // Feeding methods
        public void FeedBasicFertilizer()
        {
            try
            {
                AddExperience(5);
                Health += 5;
                Hunger -= 20;
                Energy += 5;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FeedBasicFertilizer: {ex.Message}");
            }
        }

        public bool FeedBurger()
        {
            try
            {
                if (Inventory.UseItem(BURGER_ID))
                {
                    AddExperience(3);
                    Mood += 3;
                    Health -= 5;
                    Hunger -= 30;
                    Energy += 15;

                    if (_random.Next(100) < 5)
                    {
                        HealthCondition = HealthCondition.NutrientDeficiency;
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FeedBurger: {ex.Message}");
                return false;
            }
        }

        public bool FeedIceCream()
        {
            try
            {
                if (Inventory.UseItem(ICE_CREAM_ID))
                {
                    AddExperience(4);
                    Mood += 15;
                    Health -= 10;
                    Hunger -= 15;
                    Energy += 20;

                    if (_random.Next(100) < 15)
                    {
                        HealthCondition = HealthCondition.NutrientDeficiency;
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FeedIceCream: {ex.Message}");
                return false;
            }
        }

        public bool FeedVegetables()
        {
            try
            {
                if (Inventory.UseItem(VEGETABLES_ID))
                {
                    AddExperience(8);
                    Mood -= 10;
                    Health += 15;
                    Hunger -= 25;
                    Energy += 10;

                    _activeEffects["VegetablesMoodBoost"] = true;

                    if (HealthCondition != HealthCondition.Healthy && _random.Next(100) < 10)
                    {
                        HealthCondition = HealthCondition.Healthy;
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FeedVegetables: {ex.Message}");
                return false;
            }
        }

        public bool FeedPremiumNutrients()
        {
            try
            {
                if (Inventory.UseItem(PREMIUM_NUTRIENTS_ID))
                {
                    AddExperience(15);
                    Mood += 10;
                    Health += 20;
                    Hunger -= 40;
                    Energy += 15;

                    if (HealthCondition != HealthCondition.Healthy && _random.Next(100) < 5)
                    {
                        HealthCondition = HealthCondition.Healthy;
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FeedPremiumNutrients: {ex.Message}");
                return false;
            }
        }

        public bool FeedSpecialTreat()
        {
            try
            {
                if (Inventory.UseItem(SPECIAL_TREAT_ID))
                {
                    AddExperience(20);
                    Mood += 25;
                    Health -= 5;
                    Hunger -= 10;
                    Energy += 30;

                    if (_random.Next(100) < 10)
                    {
                        HealthCondition = HealthCondition.NutrientDeficiency;
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in FeedSpecialTreat: {ex.Message}");
                return false;
            }
        }

        // Core update method with performance optimizations
        public void UpdateState()
        {
            lock (_updateLock)
            {
                try
                {
                    var timeSinceLastUpdate = DateTime.Now - LastUpdateTime;

                    if (timeSinceLastUpdate.TotalSeconds < 0)
                    {
                        LastUpdateTime = DateTime.Now;
                        return;
                    }

                    int timeSpeedMultiplier = 1;
                    try
                    {
                        timeSpeedMultiplier = GameSettings.Instance?.TimeProgressionSpeed ?? 1;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error getting time speed multiplier: {ex.Message}");
                    }

                    var minutesPassed = timeSinceLastUpdate.TotalMinutes * timeSpeedMultiplier;
                    minutesPassed = Math.Min(minutesPassed, 1440); // Max 24 hours

                    // Batch all stat updates to reduce property change notifications
                    bool hasChanges = false;

                    // Update game time
                    hasChanges |= UpdateGameTime(minutesPassed);

                    // Update stats with overflow protection
                    hasChanges |= UpdateStats(minutesPassed);

                    // Update health conditions
                    UpdateHealthConditionEffects(minutesPassed);

                    // Process active effects
                    ProcessActiveEffects();

                    // Check cooldowns
                    CheckForExpiredCooldowns();

                    // Update age
                    Age += Math.Max(0, (int)Math.Min(timeSinceLastUpdate.TotalHours * timeSpeedMultiplier, 365));

                    // Update state based on stats
                    UpdateCurrentStateBasedOnStats();

                    // Update mood state
                    UpdateMoodState();

                    // Passive XP gain
                    AddPassiveExperience(timeSinceLastUpdate);

                    LastUpdateTime = DateTime.Now;

                    // Force refresh all action availability if there were changes
                    if (hasChanges)
                    {
                        RefreshAllActionAvailability();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in UpdateState: {ex.Message}");
                    LastUpdateTime = DateTime.Now;
                }
            }
        }

        private bool UpdateGameTime(double minutesPassed)
        {
            bool hasChanges = false;
            int gameMinutesToAdd = Math.Max(0, (int)(minutesPassed * 60));

            if (gameMinutesToAdd > 0)
            {
                try
                {
                    int newMinutes = GameMinute + gameMinutesToAdd;
                    GameMinute = newMinutes % 60;

                    int hoursToAdd = newMinutes / 60;
                    if (hoursToAdd > 0)
                    {
                        int newHours = GameHour + hoursToAdd;
                        GameHour = newHours % 24;

                        int daysToAdd = newHours / 24;
                        if (daysToAdd > 0)
                        {
                            GameDay += daysToAdd;
                            EvaluateDailyCare();
                            hasChanges = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating game time: {ex.Message}");
                }
            }

            return hasChanges;
        }

        private bool UpdateStats(double minutesPassed)
        {
            bool hasChanges = false;

            try
            {
                int oldWater = Water;
                int oldEnergy = Energy;
                int oldMood = Mood;
                int oldHunger = Hunger;
                int oldCleanliness = Cleanliness;
                int oldGrowth = Growth;
                int oldHealth = Health;

                // Stat decay with bounds checking
                Water = Math.Max(0, Water - (int)Math.Min(minutesPassed * 0.08, 100));
                Energy = Math.Max(0, Energy - (int)Math.Min(minutesPassed * 0.03, 100));
                Mood = Math.Max(0, Mood - (int)Math.Min(minutesPassed * 0.017, 100));
                Hunger = Math.Min(100, Hunger + (int)Math.Min(minutesPassed * 0.05, 100));
                Cleanliness = Math.Max(0, Cleanliness - (int)Math.Min(minutesPassed * 0.002, 100));

                // Passive growth when conditions are good
                if (Water > 40 && Health > 60 && Energy > 30 && Hunger < 70)
                {
                    Growth += (int)Math.Min(minutesPassed * 0.01, 10);
                }

                // Health effects
                if (Water < 20)
                {
                    Health -= (int)Math.Min(minutesPassed * 0.033, 50);
                    if (Health < 30 && HealthCondition == HealthCondition.Healthy && _random.Next(100) < 10)
                    {
                        HealthCondition = HealthCondition.NutrientDeficiency;
                    }
                }

                if (Hunger > 80)
                {
                    Health -= (int)Math.Min(minutesPassed * 0.017, 50);
                    Mood -= (int)Math.Min(minutesPassed * 0.033, 50);
                }

                if (Cleanliness < 30)
                {
                    Health -= (int)Math.Min(minutesPassed * 0.008, 50);
                    Mood -= (int)Math.Min(minutesPassed * 0.017, 50);

                    if (HealthCondition == HealthCondition.Healthy && _random.Next(100) < 5)
                    {
                        HealthCondition = _random.Next(2) == 0 ?
                            HealthCondition.LeafSpot : HealthCondition.PestInfestation;
                    }
                }

                // Check if any stats changed
                hasChanges = oldWater != Water || oldEnergy != Energy || oldMood != Mood ||
                           oldHunger != Hunger || oldCleanliness != Cleanliness ||
                           oldGrowth != Growth || oldHealth != Health;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating stats: {ex.Message}");
            }

            return hasChanges;
        }

        private void UpdateCurrentStateBasedOnStats()
        {
            if (Health < 30)
                CurrentState = BonsaiState.Unhealthy;
            else if (Energy < 20)
                CurrentState = BonsaiState.Wilting;
            else if (Water < 30)
                CurrentState = BonsaiState.Thirsty;
            else if (CurrentState != BonsaiState.Blooming && CurrentState != BonsaiState.Growing && CurrentState != BonsaiState.Sleeping)
                CurrentState = BonsaiState.Idle;
        }

        private void AddPassiveExperience(TimeSpan timeSinceLastUpdate)
        {
            if (Health > 70 && Water > 70 && Energy > 70 && Cleanliness > 70 && Hunger < 30)
            {
                try
                {
                    double minutesPassedSinceLastUpdate = timeSinceLastUpdate.TotalMinutes;
                    if (minutesPassedSinceLastUpdate >= 1.0)
                    {
                        int passiveXP = (int)Math.Floor(Math.Min(minutesPassedSinceLastUpdate * 0.1, 100));
                        if (passiveXP > 0)
                            AddExperience(passiveXP);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in passive XP calculation: {ex.Message}");
                }
            }
        }

        private void RefreshAllActionAvailability()
        {
            try
            {
                // Access all properties to trigger cache updates
                var temp = CanWater;
                temp = CanPrune;
                temp = CanRest;
                temp = CanFertilize;
                temp = CanCleanArea;
                temp = CanExercise;
                temp = CanTrain;
                temp = CanPlay;
                temp = CanMeditate;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing action availability: {ex.Message}");
            }
        }

        // XP System helper methods
        private void AddExperience(int baseXP)
        {
            if (baseXP <= 0) return;

            lock (_xpLock)
            {
                try
                {
                    double moodMultiplier = GetMoodMultiplier();
                    double streakBonus = GetStreakBonus();

                    int finalXP;
                    checked
                    {
                        finalXP = Math.Max(1, (int)Math.Min(baseXP * moodMultiplier * (1.0 + streakBonus), 10000));
                        _lastXpGain = finalXP;
                        XP += finalXP;
                    }
                }
                catch (OverflowException)
                {
                    _lastXpGain = 0;
                    XP = int.MaxValue - 100;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in AddExperience: {ex.Message}");
                }
            }
        }

        private void CheckForLevelUp()
        {
            try
            {
                int nextLevel = Level + 1;
                int requiredXP = GetXPForNextLevel();

                if (XP >= requiredXP)
                {
                    Level = nextLevel;
                    UpdateGrowthStage();
                    Mood += 20;

                    try
                    {
                        Currency.AddBills(5);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adding level up currency: {ex.Message}");
                    }
                }

                QueuePropertyChanged(nameof(LevelDisplay));
                QueuePropertyChanged(nameof(XPToNextLevel));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CheckForLevelUp: {ex.Message}");
            }
        }

        private int GetXPForNextLevel()
        {
            try
            {
                checked
                {
                    return Math.Max(100, (int)Math.Min(100 * Math.Pow(Level, 1.5), int.MaxValue / 2));
                }
            }
            catch (OverflowException)
            {
                return int.MaxValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating XP for next level: {ex.Message}");
                return 100;
            }
        }

        private double GetMoodMultiplier()
        {
            try
            {
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting mood multiplier: {ex.Message}");
                return 1.0;
            }
        }

        private double GetStreakBonus()
        {
            try
            {
                return Math.Min(0.5, ConsecutiveDaysGoodCare * 0.05);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting streak bonus: {ex.Message}");
                return 0.0;
            }
        }

        private void UpdateMoodState()
        {
            try
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

                QueuePropertyChanged(nameof(MoodDisplay));
                QueuePropertyChanged(nameof(XPMultiplier));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating mood state: {ex.Message}");
            }
        }

        private void UpdateGrowthStage()
        {
            try
            {
                GrowthStage newStage = Level switch
                {
                    <= 5 => GrowthStage.Seedling,
                    <= 15 => GrowthStage.Sapling,
                    <= 30 => GrowthStage.YoungBonsai,
                    <= 50 => GrowthStage.MatureBonsai,
                    <= 75 => GrowthStage.ElderBonsai,
                    <= 100 => GrowthStage.AncientBonsai,
                    _ => GrowthStage.LegendaryBonsai
                };

                if (newStage != GrowthStage)
                {
                    GrowthStage = newStage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating growth stage: {ex.Message}");
            }
        }

        private void EvaluateDailyCare()
        {
            try
            {
                bool goodCareDay = Health > 70 && Water > 70 && Energy > 70 && Hunger < 30 && Cleanliness > 70;

                if (goodCareDay)
                {
                    ConsecutiveDaysGoodCare++;
                    AddExperience(10);

                    if (ConsecutiveDaysGoodCare % 5 == 0)
                    {
                        AddExperience(ConsecutiveDaysGoodCare * 2);
                    }
                }
                else
                {
                    if (ConsecutiveDaysGoodCare > 0)
                    {
                        ConsecutiveDaysGoodCare = 0;
                    }
                }

                Growth += 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EvaluateDailyCare: {ex.Message}");
            }
        }

        private void UpdateHealthConditionEffects(double minutesPassed)
        {
            if (minutesPassed <= 0) return;

            try
            {
                switch (HealthCondition)
                {
                    case HealthCondition.RootRot:
                        Health -= (int)Math.Min(minutesPassed * 0.05, 50);
                        Mood -= (int)Math.Min(minutesPassed * 0.025, 50);
                        break;

                    case HealthCondition.LeafSpot:
                        Health -= (int)Math.Min(minutesPassed * 0.033, 50);
                        Mood -= (int)Math.Min(minutesPassed * 0.017, 50);

                        if (_random.Next(1000) < minutesPassed && Health > 80)
                        {
                            HealthCondition = HealthCondition.Healthy;
                        }
                        break;

                    case HealthCondition.PestInfestation:
                        Health -= (int)Math.Min(minutesPassed * 0.05, 50);
                        Mood -= (int)Math.Min(minutesPassed * 0.033, 50);
                        Cleanliness -= (int)Math.Min(minutesPassed * 0.033, 50);
                        break;

                    case HealthCondition.NutrientDeficiency:
                        Health -= (int)Math.Min(minutesPassed * 0.025, 50);
                        Mood -= (int)Math.Min(minutesPassed * 0.017, 50);

                        if (Hunger < 20 && _random.Next(100) < 10)
                        {
                            HealthCondition = HealthCondition.Healthy;
                        }
                        break;

                    case HealthCondition.Sunburn:
                        Health -= (int)Math.Min(minutesPassed * 0.017, 50);
                        Mood -= (int)Math.Min(minutesPassed * 0.017, 50);

                        if (_random.Next(1000) < minutesPassed * 2)
                        {
                            HealthCondition = HealthCondition.Healthy;
                        }
                        break;

                    case HealthCondition.Overtraining:
                        Energy -= (int)Math.Min(minutesPassed * 0.033, 50);
                        Mood -= (int)Math.Min(minutesPassed * 0.033, 50);

                        if (Energy > 90 && _random.Next(100) < 20)
                        {
                            HealthCondition = HealthCondition.Healthy;
                        }
                        break;
                }

                QueuePropertyChanged(nameof(HealthConditionDisplay));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateHealthConditionEffects: {ex.Message}");
            }
        }

        private void ProcessActiveEffects()
        {
            try
            {
                if (_activeEffects.TryGetValue("PruningMoodBoost", out bool pruningActive) && pruningActive)
                {
                    DateTime pruneTime = GetActionCooldownTime("Prune");
                    if (pruneTime != DateTime.MinValue && (DateTime.Now - pruneTime).TotalHours > 2)
                    {
                        Mood += 10;
                        _activeEffects["PruningMoodBoost"] = false;
                    }
                }

                if (_activeEffects.TryGetValue("TrainingMoodBoost", out bool trainingActive) && trainingActive)
                {
                    if (!IsActionOnCooldown("IntenseTraining"))
                    {
                        Mood += 15;
                        _activeEffects["TrainingMoodBoost"] = false;
                    }
                }

                if (_activeEffects.TryGetValue("VegetablesMoodBoost", out bool veggiesActive) && veggiesActive)
                {
                    if ((DateTime.Now - LastUpdateTime).TotalHours > 4)
                    {
                        Mood += 5;
                        Health += 5;
                        _activeEffects["VegetablesMoodBoost"] = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ProcessActiveEffects: {ex.Message}");
            }
        }

        private void SetActionCooldown(string action)
        {
            if (string.IsNullOrEmpty(action)) return;

            try
            {
                lock (_updateLock)
                {
                    _actionCooldowns[action] = DateTime.Now;

                    // Force update the corresponding Can property
                    switch (action)
                    {
                        case "Water":
                            _canWater = false;
                            QueuePropertyChanged(nameof(CanWater));
                            break;
                        case "Prune":
                            _canPrune = false;
                            QueuePropertyChanged(nameof(CanPrune));
                            break;
                        case "Rest":
                            _canRest = false;
                            QueuePropertyChanged(nameof(CanRest));
                            break;
                        case "Fertilize":
                            _canFertilize = false;
                            QueuePropertyChanged(nameof(CanFertilize));
                            break;
                        case "CleanArea":
                            _canCleanArea = false;
                            QueuePropertyChanged(nameof(CanCleanArea));
                            break;
                        case "LightExercise":
                            _canExercise = false;
                            QueuePropertyChanged(nameof(CanExercise));
                            break;
                        case "IntenseTraining":
                            _canTrain = false;
                            QueuePropertyChanged(nameof(CanTrain));
                            break;
                        case "Play":
                            _canPlay = false;
                            QueuePropertyChanged(nameof(CanPlay));
                            break;
                        case "Meditation":
                            _canMeditate = false;
                            QueuePropertyChanged(nameof(CanMeditate));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SetActionCooldown: {ex.Message}");
            }
        }

        private bool IsActionOnCooldown(string action)
        {
            if (string.IsNullOrEmpty(action)) return false;

            try
            {
                if (!_actionCooldowns.TryGetValue(action, out DateTime lastUsed))
                    return false;

                if (!_actionCooldownTimes.TryGetValue(action, out int cooldownMinutes))
                    return false;

                return (DateTime.Now - lastUsed).TotalMinutes < cooldownMinutes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in IsActionOnCooldown: {ex.Message}");
                return false;
            }
        }

        private DateTime GetActionCooldownTime(string action)
        {
            if (string.IsNullOrEmpty(action)) return DateTime.MinValue;

            try
            {
                return _actionCooldowns.TryGetValue(action, out DateTime time) ? time : DateTime.MinValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetActionCooldownTime: {ex.Message}");
                return DateTime.MinValue;
            }
        }

        private void CheckForExpiredCooldowns()
        {
            try
            {
                UpdateCooldownState("Water", ref _canWater, nameof(CanWater));
                UpdateCooldownState("Prune", ref _canPrune, nameof(CanPrune));
                UpdateCooldownState("Rest", ref _canRest, nameof(CanRest));
                UpdateCooldownState("Fertilize", ref _canFertilize, nameof(CanFertilize));
                UpdateCooldownState("CleanArea", ref _canCleanArea, nameof(CanCleanArea));

                UpdateCooldownStateWithEnergyCheck("LightExercise", ref _canExercise, nameof(CanExercise), 30);
                UpdateCooldownStateWithEnergyCheck("IntenseTraining", ref _canTrain, nameof(CanTrain), 50);
                UpdateCooldownStateWithEnergyCheck("Play", ref _canPlay, nameof(CanPlay), 30);

                UpdateCooldownState("Meditation", ref _canMeditate, nameof(CanMeditate));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CheckForExpiredCooldowns: {ex.Message}");
            }
        }

        private void UpdateCooldownState(string action, ref bool canField, string propertyName)
        {
            try
            {
                bool onCooldown = IsActionOnCooldown(action);
                if (canField == onCooldown)
                {
                    canField = !onCooldown;
                    QueuePropertyChanged(propertyName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateCooldownState: {ex.Message}");
            }
        }

        private void UpdateCooldownStateWithEnergyCheck(string action, ref bool canField, string propertyName, int energyRequired)
        {
            try
            {
                bool canPerform = !IsActionOnCooldown(action) && Energy > energyRequired;
                if (canField != canPerform)
                {
                    canField = canPerform;
                    QueuePropertyChanged(propertyName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateCooldownStateWithEnergyCheck: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnPropertyChanged: {ex.Message}");
            }
        }

        // Dispose pattern for cleanup
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _propertyChangeTimer?.Dispose();

                    lock (_pendingPropertyChanges)
                    {
                        _pendingPropertyChanges.Clear();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing Bonsai: {ex.Message}");
                }
            }
        }

        ~Bonsai()
        {
            Dispose(false);
        }
    }
}

