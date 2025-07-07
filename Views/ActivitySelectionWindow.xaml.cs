using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame
{
    public partial class ActivitySelectionWindow : Window
    {
        private BonsaiGotchiGame.Models.Bonsai _bonsai;
        private ShopManager _shopManager;
        private string _activityType;

        public ShopItem? SelectedActivity { get; private set; }
        private ObservableCollection<SelectableShopItem> _selectableItems;

        public ActivitySelectionWindow(BonsaiGotchiGame.Models.Bonsai bonsai, ShopManager shopManager, string activityType)
        {
            InitializeComponent();

            _bonsai = bonsai;
            _shopManager = shopManager;
            _activityType = activityType;
            _selectableItems = new ObservableCollection<SelectableShopItem>(); // Initialize here to fix the error

            // Set header text
            HeaderText.Text = $"Select {activityType} Item";

            // Load available items
            LoadItems();
        }

        private void LoadItems()
        {
            var availableItems = _shopManager.GetItemsByType(_activityType);

            _selectableItems.Clear(); // Clear existing items

            foreach (var item in availableItems)
            {
                _selectableItems.Add(new SelectableShopItem(item));
            }

            // Select the first item by default
            if (_selectableItems.Count > 0)
            {
                _selectableItems[0].IsSelected = true;
            }

            ItemsListControl.ItemsSource = _selectableItems;
        }

        private void ShopLink_Click(object sender, RoutedEventArgs e)
        {
            // Open shop window
            var shopWindow = new ShopWindow(_bonsai, _shopManager);
            shopWindow.Owner = this;
            shopWindow.ShowDialog();

            // Refresh the items list after shop closes
            LoadItems();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected item
            var selectedItem = _selectableItems.FirstOrDefault(i => i.IsSelected);

            if (selectedItem != null)
            {
                SelectedActivity = selectedItem.Item;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select an item first.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class SelectableShopItem
    {
        public ShopItem Item { get; }
        public bool IsSelected { get; set; }

        public string Id => Item.Id;
        public string Name => Item.Name;
        public string Description => Item.Description;
        public string Icon => Item.Icon;

        public SelectableShopItem(ShopItem item)
        {
            Item = item;
            IsSelected = false;
        }
    }
}