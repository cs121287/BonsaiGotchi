using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BonsaiGotchiGame.Models
{
    public class Currency : INotifyPropertyChanged
    {
        private int _bonsaiBills;
        private DateTime _lastDailyRewardTime;

        public int BonsaiBills
        {
            get => _bonsaiBills;
            private set
            {
                if (_bonsaiBills != value)
                {
                    _bonsaiBills = Math.Max(0, value); // Ensure non-negative
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastDailyRewardTime
        {
            get => _lastDailyRewardTime;
            set
            {
                if (_lastDailyRewardTime != value)
                {
                    _lastDailyRewardTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public Currency()
        {
            _bonsaiBills = 10; // Starting currency
            _lastDailyRewardTime = DateTime.MinValue;
        }

        public Currency(int initialBills)
        {
            _bonsaiBills = Math.Max(0, initialBills);
            _lastDailyRewardTime = DateTime.MinValue;
        }

        public bool CanAfford(int cost)
        {
            return _bonsaiBills >= cost && cost >= 0;
        }

        public bool SpendBills(int amount)
        {
            if (amount < 0)
                return false;

            if (_bonsaiBills >= amount)
            {
                BonsaiBills = _bonsaiBills - amount;
                return true;
            }
            return false;
        }

        public void AddBills(int amount)
        {
            if (amount > 0)
            {
                BonsaiBills = _bonsaiBills + amount;
            }
        }

        public bool CheckDailyReward()
        {
            var now = DateTime.Now;
            var daysSinceLastReward = (now - _lastDailyRewardTime).TotalDays;

            if (daysSinceLastReward >= 1.0)
            {
                AddBills(1);
                _lastDailyRewardTime = now;
                return true;
            }
            return false;
        }

        // Method to load currency data from save file
        public void LoadFromSaveData(int bonsaiBills, DateTime lastDailyRewardTime)
        {
            _bonsaiBills = Math.Max(0, bonsaiBills);
            _lastDailyRewardTime = lastDailyRewardTime;
            OnPropertyChanged(nameof(BonsaiBills));
            OnPropertyChanged(nameof(LastDailyRewardTime));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}