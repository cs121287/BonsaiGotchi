using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BonsaiGotchiGame.Models
{
    public class Currency : INotifyPropertyChanged
    {
        private int _bonsaiBills = 10; // Start with 10 bills
        private DateTime _lastDailyRewardTime = DateTime.MinValue;

        public int BonsaiBills
        {
            get => _bonsaiBills;
            private set
            {
                if (_bonsaiBills != value)
                {
                    _bonsaiBills = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastDailyRewardTime
        {
            get => _lastDailyRewardTime;
            private set
            {
                _lastDailyRewardTime = value;
                OnPropertyChanged();
            }
        }

        // Add bills to wallet
        public void AddBills(int amount)
        {
            if (amount <= 0) return;

            try
            {
                checked
                {
                    BonsaiBills += amount;
                }
            }
            catch (OverflowException)
            {
                // Cap at max int if overflow occurs
                BonsaiBills = int.MaxValue;
            }
        }

        // Remove bills from wallet (returns true if successful)
        public bool SpendBills(int amount)
        {
            if (amount <= 0) return true;
            if (BonsaiBills < amount) return false;

            BonsaiBills -= amount;
            return true;
        }

        // Check for daily reward and collect if available
        public bool CheckDailyReward()
        {
            // Daily reward is available if:
            // 1. It's the first time claiming (LastDailyRewardTime is min value)
            // 2. It's a different calendar day from the last claim

            bool isNewDay = LastDailyRewardTime == DateTime.MinValue ||
                            DateTime.Now.Date > LastDailyRewardTime.Date;

            if (isNewDay)
            {
                // Award 1 bill per day
                AddBills(1);
                LastDailyRewardTime = DateTime.Now;
                return true;
            }

            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}