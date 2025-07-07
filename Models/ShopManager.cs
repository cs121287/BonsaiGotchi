using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame
{
    public partial class ShopManager
    {
        private readonly BonsaiGotchiGame.Models.Bonsai _bonsai;
        public List<ShopItem> ShopItems { get; private set; }

        public ShopManager(BonsaiGotchiGame.Models.Bonsai bonsai)
        {
            _bonsai = bonsai;
            ShopItems = InitializeShopItems();
        }

        private List<ShopItem> InitializeShopItems()
        {
            var items = new List<ShopItem>
            {
                // Basic items that are always available
                new ShopItem
                {
                    Id = "basic_clean",
                    Name = "Basic Cleaning Kit",
                    Description = "A simple kit for cleaning your bonsai's area.",
                    Icon = "🧹",
                    Category = "Activity",
                    Price = 0,
                    IsUnlocked = true,
                    ActivityType = "Clean"
                },
                new ShopItem
                {
                    Id = "basic_exercise",
                    Name = "Basic Exercise",
                    Description = "Simple stretching exercises for your bonsai.",
                    Icon = "🏃",
                    Category = "Activity",
                    Price = 0,
                    IsUnlocked = true,
                    ActivityType = "Exercise"
                },
                new ShopItem
                {
                    Id = "basic_training",
                    Name = "Basic Training",
                    Description = "Standard training techniques for bonsai health.",
                    Icon = "🏋️",
                    Category = "Activity",
                    Price = 0,
                    IsUnlocked = true,
                    ActivityType = "Training"
                },
                new ShopItem
                {
                    Id = "ball",
                    Name = "Small Ball",
                    Description = "A simple ball for your bonsai to play with.",
                    Icon = "⚽",
                    Category = "Activity",
                    Price = 0,
                    IsUnlocked = true,
                    ActivityType = "Play"
                },
                new ShopItem
                {
                    Id = "basic_meditation",
                    Name = "Basic Meditation",
                    Description = "Simple meditation techniques to calm your bonsai.",
                    Icon = "🧘",
                    Category = "Activity",
                    Price = 0,
                    IsUnlocked = true,
                    ActivityType = "Meditate"
                },

                // Food items
                new ShopItem
                {
                    Id = "burger",
                    Name = "Burger",
                    Description = "Tasty but not healthy. +3 Mood, -5 Health, -30 Hunger",
                    Icon = "🍔",
                    Category = "Food",
                    Price = 5,
                    IsUnlocked = false
                },
                new ShopItem
                {
                    Id = "ice_cream",
                    Name = "Ice Cream",
                    Description = "Very tasty but very unhealthy. +15 Mood, -10 Health, -15 Hunger",
                    Icon = "🍦",
                    Category = "Food",
                    Price = 8,
                    IsUnlocked = false
                },
                new ShopItem
                {
                    Id = "vegetables",
                    Name = "Vegetables",
                    Description = "Healthy but not tasty. -10 Mood initially, +15 Health, -25 Hunger",
                    Icon = "🥦",
                    Category = "Food",
                    Price = 10,
                    IsUnlocked = false
                },
                new ShopItem
                {
                    Id = "premium_nutrients",
                    Name = "Premium Nutrients",
                    Description = "High quality food. +10 Mood, +20 Health, -40 Hunger",
                    Icon = "🧪",
                    Category = "Food",
                    Price = 20,
                    IsUnlocked = false
                },
                new ShopItem
                {
                    Id = "special_treat",
                    Name = "Special Treat",
                    Description = "Super tasty treat. +25 Mood, -5 Health, -10 Hunger, +30 Energy",
                    Icon = "🍭",
                    Category = "Food",
                    Price = 15,
                    IsUnlocked = false
                },

                // Premium activity items
                new ShopItem
                {
                    Id = "premium_cleaning",
                    Name = "Premium Cleaning Kit",
                    Description = "A high-quality cleaning kit that makes your bonsai extra clean. +15% effectiveness.",
                    Icon = "✨",
                    Category = "Activity",
                    Price = 25,
                    IsUnlocked = false,
                    ActivityType = "Clean"
                },
                new ShopItem
                {
                    Id = "treadmill",
                    Name = "Bonsai Treadmill",
                    Description = "A tiny treadmill for your bonsai to exercise on. +20% effectiveness.",
                    Icon = "🏃‍♂️",
                    Category = "Activity",
                    Price = 30,
                    IsUnlocked = false,
                    ActivityType = "Exercise"
                },
                new ShopItem
                {
                    Id = "gym_equipment",
                    Name = "Mini Gym Set",
                    Description = "A complete set of training equipment for your bonsai. +25% effectiveness.",
                    Icon = "🏋️‍♂️",
                    Category = "Activity",
                    Price = 40,
                    IsUnlocked = false,
                    ActivityType = "Training"
                },
                new ShopItem
                {
                    Id = "video_game",
                    Name = "Bonsai Video Game",
                    Description = "A fun video game for your bonsai to play with. +30% mood boost.",
                    Icon = "🎮",
                    Category = "Activity",
                    Price = 35,
                    IsUnlocked = false,
                    ActivityType = "Play"
                },
                new ShopItem
                {
                    Id = "zen_garden",
                    Name = "Zen Garden",
                    Description = "A peaceful zen garden for meditation. +25% effectiveness.",
                    Icon = "🏞️",
                    Category = "Activity",
                    Price = 50,
                    IsUnlocked = false,
                    ActivityType = "Meditate"
                },

                // Decorations
                new ShopItem
                {
                    Id = "small_pot",
                    Name = "Decorative Pot",
                    Description = "A small decorative pot for your bonsai. +5% mood boost.",
                    Icon = "🪴",
                    Category = "Decoration",
                    Price = 15,
                    IsUnlocked = false
                },
                new ShopItem
                {
                    Id = "lantern",
                    Name = "Japanese Lantern",
                    Description = "A beautiful lantern for your bonsai's home. +8% mood boost.",
                    Icon = "🏮",
                    Category = "Decoration",
                    Price = 25,
                    IsUnlocked = false
                },
                new ShopItem
                {
                    Id = "fountain",
                    Name = "Mini Fountain",
                    Description = "A small water fountain that creates a peaceful atmosphere. +10% mood boost.",
                    Icon = "⛲",
                    Category = "Decoration",
                    Price = 40,
                    IsUnlocked = false
                }
            };

            // Set computed properties for each item
            foreach (var item in items)
            {
                item.CanPurchase = !item.IsUnlocked;
                item.ButtonText = item.IsUnlocked ? "Owned" : $"Buy ({item.Price})";
            }

            return items;
        }

        public List<ShopItem> GetItemsByType(string activityType)
        {
            return ShopItems.Where(item =>
                item.Category == "Activity" &&
                item.ActivityType == activityType &&
                item.IsUnlocked).ToList();
        }
    }

    public class ShopItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "❓";
        public string Category { get; set; } = "Activity";
        public int Price { get; set; }
        public bool IsUnlocked { get; set; }
        public string ButtonText { get; set; } = "Buy";
        public bool CanPurchase { get; set; } = true;
        public string ActivityType { get; set; } = string.Empty;
    }
}