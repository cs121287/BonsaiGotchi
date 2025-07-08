using System;
using System.Collections.Generic;
using System.Linq;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Models
{
    public class ShopManager
    {
        private readonly Bonsai _bonsai;
        public List<ShopItem> ShopItems { get; private set; } = new List<ShopItem>();

        public ShopManager(Bonsai bonsai)
        {
            _bonsai = bonsai ?? throw new ArgumentNullException(nameof(bonsai));
            InitializeShopItems();
        }

        private void InitializeShopItems()
        {
            ShopItems = new List<ShopItem>
            {
                // Food Items
                new ShopItem("burger", "Burger", "Tasty but unhealthy fast food", 2, "🍔", category: "Food"),
                new ShopItem("ice_cream", "Ice Cream", "Sweet frozen treat", 3, "🍦", category: "Food"),
                new ShopItem("vegetables", "Vegetables", "Healthy green nutrition", 4, "🥦", category: "Food"),
                new ShopItem("premium_nutrients", "Premium Nutrients", "High-quality plant food", 8, "🧪", category: "Food"),
                new ShopItem("special_treat", "Special Treat", "Rare delicious snack", 12, "🍭", category: "Food"),

                // Clean Activities
                new ShopItem("basic_clean", "Basic Cleaning", "Simple cleaning tools", 0, "🧹", category: "Activity", activityType: "Clean"),
                new ShopItem("power_clean", "Power Cleaning", "Advanced cleaning equipment", 15, "🚿", category: "Activity", activityType: "Clean"),
                new ShopItem("eco_clean", "Eco Cleaning", "Environmentally friendly cleaning", 25, "🌿", category: "Activity", activityType: "Clean"),

                // Exercise Activities
                new ShopItem("basic_exercise", "Basic Exercise", "Simple stretching routine", 0, "🏃", category: "Activity", activityType: "Exercise"),
                new ShopItem("cardio_workout", "Cardio Workout", "Heart-pumping exercise routine", 20, "💓", category: "Activity", activityType: "Exercise"),
                new ShopItem("strength_training", "Strength Training", "Muscle-building exercises", 30, "💪", category: "Activity", activityType: "Exercise"),

                // Training Activities
                new ShopItem("basic_training", "Basic Training", "Fundamental training exercises", 0, "🏋️", category: "Activity", activityType: "Training"),
                new ShopItem("advanced_training", "Advanced Training", "Intensive skill development", 25, "🥇", category: "Activity", activityType: "Training"),
                new ShopItem("expert_training", "Expert Training", "Master-level training program", 40, "🏆", category: "Activity", activityType: "Training"),

                // Play Activities
                new ShopItem("ball", "Ball", "Simple ball for playing", 0, "⚽", category: "Activity", activityType: "Play"),
                new ShopItem("puzzle_games", "Puzzle Games", "Mind-challenging games", 18, "🧩", category: "Activity", activityType: "Play"),
                new ShopItem("adventure_kit", "Adventure Kit", "Exciting exploration tools", 35, "🗺️", category: "Activity", activityType: "Play"),

                // Meditation Activities
                new ShopItem("basic_meditation", "Basic Meditation", "Simple mindfulness practice", 0, "🧘", category: "Activity", activityType: "Meditate"),
                new ShopItem("zen_meditation", "Zen Meditation", "Advanced meditation techniques", 22, "☯️", category: "Activity", activityType: "Meditate"),
                new ShopItem("transcendental", "Transcendental", "Deep spiritual meditation", 45, "🕉️", category: "Activity", activityType: "Meditate")
            };

            // Mark basic items as unlocked by default
            foreach (var item in ShopItems.Where(i => i.Price == 0))
            {
                item.IsUnlocked = true;
            }
        }

        public List<ShopItem> GetItemsByType(string activityType)
        {
            return ShopItems.Where(item =>
                item.Category == "Activity" &&
                string.Equals(item.ActivityType, activityType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<ShopItem> GetItemsByCategory(string category)
        {
            return ShopItems.Where(item =>
                string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public bool PurchaseItem(string itemId)
        {
            var item = ShopItems.FirstOrDefault(i => i.Id == itemId);
            if (item == null || item.IsUnlocked)
                return false;

            if (_bonsai.Currency.CanAfford(item.Price))
            {
                if (_bonsai.Currency.SpendBills(item.Price))
                {
                    item.IsUnlocked = true;
                    return true;
                }
            }
            return false;
        }

        public bool PurchaseAndAddToInventory(string itemId, int quantity = 1)
        {
            var item = ShopItems.FirstOrDefault(i => i.Id == itemId);
            if (item == null || quantity <= 0)
                return false;

            // For food items, we can buy multiple even if already "unlocked"
            if (item.Category == "Food")
            {
                int totalCost = item.Price * quantity;
                if (_bonsai.Currency.CanAfford(totalCost))
                {
                    if (_bonsai.Currency.SpendBills(totalCost))
                    {
                        _bonsai.Inventory.AddItem(itemId, quantity);
                        item.IsUnlocked = true; // Mark as unlocked so it shows "Buy More"
                        return true;
                    }
                }
            }
            else
            {
                // For non-food items, use regular purchase logic
                return PurchaseItem(itemId);
            }

            return false;
        }

        public ShopItem? GetItem(string itemId)
        {
            return ShopItems.FirstOrDefault(i => i.Id == itemId);
        }

        public bool IsItemUnlocked(string itemId)
        {
            var item = GetItem(itemId);
            return item?.IsUnlocked ?? false;
        }

        // Method to sync shop state with save data
        public void LoadUnlockedItems(List<string> unlockedItemIds)
        {
            foreach (string itemId in unlockedItemIds)
            {
                var item = GetItem(itemId);
                if (item != null)
                {
                    item.IsUnlocked = true;
                }
            }
        }
    }
}