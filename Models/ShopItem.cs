namespace BonsaiGotchiGame.Models
{
    public class ShopItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public string IconEmoji { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public ShopItemType Type { get; set; }
        public string ActivityType { get; set; } = string.Empty; // For activity upgrades
        public bool IsUnlocked { get; set; } // Whether player owns this

        public ShopItem(string id, string name, string description, int price, string iconEmoji,
                        string? imagePath = null, ShopItemType type = ShopItemType.Food,
                        string? activityType = null)
        {
            Id = id;
            Name = name;
            Description = description;
            Price = price;
            IconEmoji = iconEmoji;
            ImagePath = imagePath ?? string.Empty;
            Type = type;
            ActivityType = activityType ?? string.Empty;
            IsUnlocked = false; // Default to locked
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