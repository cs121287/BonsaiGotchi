using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BonsaiGotchiGame.Models
{
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
        
        // Formatted time and date for display
        public string GameTimeDisplay => $"{GameHour:D2}:{GameMinute:D2}";
        
        public string GameDateDisplay => $"Day {GameDay}, Month {GameMonth}, Year {GameYear}";

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
        }

        public void GiveWater()
        {
            Water += 30;
            Energy += 10;
            CurrentState = BonsaiState.Growing;
        }

        public void Prune()
        {
            Growth += 15;
            Energy -= 10;
            Health += 5;
            CurrentState = BonsaiState.Blooming;
        }

        public void Rest()
        {
            Energy += 40;
            CurrentState = BonsaiState.Sleeping;
        }

        public void ApplyFertilizer()
        {
            Health += 30;
            Growth += 10;
            CurrentState = BonsaiState.Growing;
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
                    }
                }
            }

            // Basic stat decay over time
            Water -= (int)(minutesPassed * 2);
            Energy -= (int)(minutesPassed * 1);
            Growth += (int)(minutesPassed * 0.5);

            // Health decreases if water is too low
            if (Water < 20)
            {
                Health -= (int)(minutesPassed * 2);
            }

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

            LastUpdateTime = DateTime.Now;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}