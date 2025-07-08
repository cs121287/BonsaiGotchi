using BonsaiGotchiGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BonsaiGotchiGame
{
    public partial class ShopWindow : Window
    {
        private readonly BonsaiGotchiGame.Models.Bonsai _bonsai;
        private readonly ShopManager _shopManager;
        private TabItem? _foodTab;
        private TabItem? _activitiesTab;
        private TabItem? _decorationsTab;

        // Define the event arguments class
        public class ItemPurchasedEventArgs : EventArgs
        {
            public string ItemId { get; }
            public string Category { get; }

            public ItemPurchasedEventArgs(string itemId, string category)
            {
                ItemId = itemId;
                Category = category;
            }
        }

        // Define the event
        public event EventHandler<ItemPurchasedEventArgs>? ItemPurchased;

        public ShopWindow(BonsaiGotchiGame.Models.Bonsai bonsai, ShopManager shopManager)
        {
            InitializeComponent();
            _bonsai = bonsai;
            _shopManager = shopManager;

            // Initialize tabs after controls are loaded
            this.Loaded += ShopWindow_Loaded;

            // Setup shop items
            LoadShopItems();

            // Update currency display
            UpdateCurrencyDisplay();
        }

        private void ShopWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Store references to tab items after the UI is loaded
            _foodTab = ShopTabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.Header?.ToString() == "Food");
            _activitiesTab = ShopTabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.Header?.ToString() == "Activities");
            _decorationsTab = ShopTabControl.Items.OfType<TabItem>().FirstOrDefault(t => t.Header?.ToString() == "Decorations");

            // Verify food items are properly loaded
            Console.WriteLine($"Food items count: {FoodItemsControl.Items.Count}");
            foreach (var item in FoodItemsControl.Items)
            {
                Console.WriteLine($"Food item: {(item as ShopItem)?.Name}, Price: {(item as ShopItem)?.Price}");
            }

            // Add item container generation callback for debugging
            FoodItemsControl.ItemContainerGenerator.StatusChanged += (s, args) => {
                if (FoodItemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    Console.WriteLine("Food item containers generated");
                }
            };
        }

        private void LoadShopItems()
        {
            // Load food items
            FoodItemsControl.ItemsSource = _shopManager.ShopItems.Where(item => item.Category == "Food");

            // Load activity items
            ActivityItemsControl.ItemsSource = _shopManager.ShopItems.Where(item => item.Category == "Activity");

            // Load decoration items
            DecorationItemsControl.ItemsSource = _shopManager.ShopItems.Where(item => item.Category == "Decoration");
        }

        private void UpdateCurrencyDisplay()
        {
            if (_bonsai.Currency != null)
            {
                CurrencyDisplay.Text = _bonsai.Currency.BonsaiBills.ToString();
            }
        }

        // This method is needed to select a specific item in the shop
        public void SelectItem(string itemId)
        {
            // Find the item
            ShopItem? item = _shopManager.ShopItems.FirstOrDefault(i => i.Id == itemId);

            if (item != null)
            {
                // Determine which tab to select
                switch (item.Category.ToLower())
                {
                    case "food":
                        if (_foodTab != null)
                        {
                            ShopTabControl.SelectedItem = _foodTab;
                            HighlightItem(FoodItemsControl, item);
                        }
                        break;

                    case "activity":
                        if (_activitiesTab != null)
                        {
                            ShopTabControl.SelectedItem = _activitiesTab;
                            HighlightItem(ActivityItemsControl, item);
                        }
                        break;

                    case "decoration":
                        if (_decorationsTab != null)
                        {
                            ShopTabControl.SelectedItem = _decorationsTab;
                            HighlightItem(DecorationItemsControl, item);
                        }
                        break;
                }
            }
        }

        private void HighlightItem(ItemsControl itemsControl, ShopItem itemToHighlight)
        {
            // We need to wait for the UI to update, so we use dispatcher
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (itemsControl.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    for (int i = 0; i < itemsControl.Items.Count; i++)
                    {
                        var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                        if (container != null)
                        {
                            var item = itemsControl.Items[i] as ShopItem;
                            if (item != null && item.Id == itemToHighlight.Id)
                            {
                                // Highlight this item
                                container.BringIntoView();

                                // Add a visual effect instead of setting background
                                container.Effect = new DropShadowEffect
                                {
                                    Color = Colors.Gold,
                                    ShadowDepth = 0,
                                    BlurRadius = 15,
                                    Opacity = 0.7
                                };

                                // Add a border or other visual indicator
                                var border = FindVisualChild<Border>(container);
                                if (border != null)
                                {
                                    border.BorderBrush = new SolidColorBrush(Colors.Gold);
                                    border.BorderThickness = new Thickness(2);
                                    border.Background = new SolidColorBrush(Color.FromArgb(40, 255, 215, 0)); // Light gold with transparency
                                }

                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Try again later if containers are not generated yet
                    Dispatcher.BeginInvoke(new Action(() => HighlightItem(itemsControl, itemToHighlight)),
                        System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }));
        }

        // Helper method to find child elements
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                {
                    return result;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var shopItem = button?.DataContext as ShopItem;

                // Debug output to console to track what's happening
                Console.WriteLine($"Button clicked: {button?.Content}");
                Console.WriteLine($"ShopItem: {shopItem?.Id}, Category: {shopItem?.Category}, Price: {shopItem?.Price}");

                if (shopItem == null)
                {
                    Console.WriteLine("Error: ShopItem is null - DataContext issue");
                    MessageBox.Show("Could not identify the item to purchase.", "Shop Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_bonsai?.Currency == null)
                {
                    Console.WriteLine("Error: Bonsai Currency is null");
                    MessageBox.Show("Currency system not initialized.", "Shop Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Console.WriteLine($"Currency available: {_bonsai.Currency.BonsaiBills}, Item price: {shopItem.Price}");

                if (_bonsai.Currency.BonsaiBills >= shopItem.Price)
                {
                    bool purchased = false;

                    // For food items, use the PurchaseAndAddToInventory method
                    if (shopItem.Category == "Food")
                    {
                        Console.WriteLine($"Attempting to purchase food item: {shopItem.Id}");

                        // Try direct inventory addition if PurchaseAndAddToInventory fails
                        purchased = _shopManager.PurchaseAndAddToInventory(shopItem.Id);

                        if (!purchased)
                        {
                            // Manual purchase fallback
                            Console.WriteLine("Regular purchase failed, trying manual approach");
                            _bonsai.Currency.SpendBills(shopItem.Price);
                            _bonsai.Inventory.AddItem(shopItem.Id, 1);
                            purchased = true;
                        }

                        Console.WriteLine($"Purchase result: {purchased}");
                        Console.WriteLine($"New inventory count: {_bonsai.Inventory.GetItemCount(shopItem.Id)}");

                        // Update the button text to show "Buy More" if needed
                        if (purchased && button != null)
                        {
                            button.Content = $"Buy More ({shopItem.Price})";
                        }
                    }
                    else
                    {
                        // For non-food items, just unlock them
                        _bonsai.Currency.SpendBills(shopItem.Price);
                        shopItem.IsUnlocked = true;
                        purchased = true;

                        // Update UI for non-food items
                        if (button != null)
                        {
                            button.Content = "Owned";
                            button.IsEnabled = false;
                        }
                    }

                    if (purchased)
                    {
                        // Update UI
                        UpdateCurrencyDisplay();

                        // Play purchase sound if enabled
                        if (GameSettings.Instance?.PlaySounds ?? true)
                        {
                            System.Media.SystemSounds.Asterisk.Play();
                        }

                        // Raise the ItemPurchased event
                        ItemPurchased?.Invoke(this, new ItemPurchasedEventArgs(shopItem.Id, shopItem.Category));
                    }
                    else
                    {
                        Console.WriteLine("Purchase failed completely");
                        MessageBox.Show("Failed to purchase item. Please try again.", "Purchase Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Not enough Bonsai Bills to purchase this item!",
                        "Insufficient Funds", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in BuyButton_Click: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"An error occurred while processing your purchase: {ex.Message}",
                    "Shop Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}