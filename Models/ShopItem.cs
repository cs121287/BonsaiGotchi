using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BonsaiGotchiGame.Models
{
    public class ShopItem : INotifyPropertyChanged
    {
        private bool _isUnlocked;
        private string _buttonText = string.Empty;

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public string IconEmoji { get; set; } = string.Empty;
        public string Icon => IconEmoji; // Alias for compatibility
        public string ImagePath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Food, Activity, Decoration
        public string ActivityType { get; set; } = string.Empty; // For activity upgrades

        public bool IsUnlocked
        {
            get => _isUnlocked;
            set
            {
                if (_isUnlocked != value)
                {
                    _isUnlocked = value;
                    OnPropertyChanged();
                    UpdateButtonText();
                }
            }
        }

        public string ButtonText
        {
            get => _buttonText;
            set
            {
                if (_buttonText != value)
                {
                    _buttonText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ShopItem() { }

        public ShopItem(string id, string name, string description, int price, string iconEmoji,
                        string? imagePath = null, string category = "Food",
                        string? activityType = null)
        {
            Id = id;
            Name = name;
            Description = description;
            Price = price;
            IconEmoji = iconEmoji;
            ImagePath = imagePath ?? string.Empty;
            Category = category;
            ActivityType = activityType ?? string.Empty;
            IsUnlocked = false;
            UpdateButtonText();
        }

        private void UpdateButtonText()
        {
            if (Category == "Food")
            {
                ButtonText = IsUnlocked ? $"Buy More ({Price})" : $"Buy ({Price})";
            }
            else
            {
                ButtonText = IsUnlocked ? "Owned" : $"Buy ({Price})";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum ShopItemType
    {
        Food,
        CleanActivity,
        ExerciseActivity,
        TrainingActivity,
        PlayActivity,
        MeditateActivity
    }
}