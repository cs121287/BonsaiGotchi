using System.Linq;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame
{
    // Partial class to add inventory-related functionality to ShopManager
    public partial class ShopManager
    {
        // Method to handle purchasing and updating inventory
        public bool PurchaseAndAddToInventory(string itemId)
        {
            ShopItem? item = ShopItems.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return false;

            // If it's already unlocked, check if we have enough currency to buy one unit
            if (item.IsUnlocked)
            {
                if (_bonsai.Currency.BonsaiBills >= item.Price)
                {
                    _bonsai.Currency.SpendBills(item.Price);
                    // Add one to inventory
                    _bonsai.Inventory.AddItem(itemId);
                    return true;
                }
            }
            // Otherwise unlock the item first, then add one to inventory
            else if (_bonsai.Currency.BonsaiBills >= item.Price)
            {
                _bonsai.Currency.SpendBills(item.Price);
                item.IsUnlocked = true;
                _bonsai.Inventory.AddItem(itemId);

                // Update button text
                item.ButtonText = $"Buy More ({item.Price})";
                return true;
            }

            return false;
        }
    }
}