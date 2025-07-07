using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BonsaiGotchiGame.Models
{
    public class Inventory : INotifyPropertyChanged
    {
        private readonly Dictionary<string, int> _items = new Dictionary<string, int>();

        // Property to access all items (read-only)
        public IReadOnlyDictionary<string, int> Items => _items;

        // Add item to inventory
        public void AddItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return;

            if (_items.ContainsKey(itemId))
            {
                _items[itemId] += quantity;
            }
            else
            {
                _items[itemId] = quantity;
            }

            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(itemId); // Allow binding to specific item quantities
        }

        // Check if we have the specified item in sufficient quantity
        public bool HasItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;

            // Special case: basic fertilizer is always available
            if (itemId == Bonsai.BASIC_FERTILIZER_ID)
                return true;

            return _items.TryGetValue(itemId, out int count) && count >= quantity;
        }

        // Get the quantity of an item
        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;

            // Special case: basic fertilizer is unlimited
            if (itemId == Bonsai.BASIC_FERTILIZER_ID)
                return -1; // -1 indicates unlimited

            return _items.TryGetValue(itemId, out int count) ? count : 0;
        }

        // Use an item from inventory
        public bool UseItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;

            // Special case: basic fertilizer is always available
            if (itemId == Bonsai.BASIC_FERTILIZER_ID)
                return true;

            if (_items.TryGetValue(itemId, out int count))
            {
                if (count >= quantity)
                {
                    _items[itemId] = count - quantity;

                    // If count reaches 0, consider removing the entry
                    if (_items[itemId] <= 0)
                    {
                        _items.Remove(itemId);
                    }

                    OnPropertyChanged(nameof(Items));
                    OnPropertyChanged(itemId);
                    return true;
                }
            }

            return false;
        }

        // Property changed event for binding
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}