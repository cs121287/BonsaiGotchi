using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.ViewModels
{
    public class ShopViewModel : INotifyPropertyChanged
    {
        private readonly Bonsai _bonsai;
        private readonly ShopManager _shopManager;

        public Currency Currency => _bonsai.Currency;

        public ObservableCollection<ShopItem> FoodItems { get; }
        public ObservableCollection<ShopItem> CleanItems { get; }
        public ObservableCollection<ShopItem> ExerciseItems { get; }
        public ObservableCollection<ShopItem> TrainingItems { get; }
        public ObservableCollection<ShopItem> PlayItems { get; }
        public ObservableCollection<ShopItem> MeditationItems { get; }

        public ICommand PurchaseCommand { get; }

        public ShopViewModel(Bonsai bonsai, ShopManager shopManager)
        {
            _bonsai = bonsai;
            _shopManager = shopManager;

            // Initialize item collections - using Category and ActivityType instead of Type
            FoodItems = new ObservableCollection<ShopItem>(
                _shopManager.ShopItems.Where(i => i.Category == "Food"));

            CleanItems = new ObservableCollection<ShopItem>(
                _shopManager.ShopItems.Where(i => i.Category == "Activity" && i.ActivityType == "Clean"));

            ExerciseItems = new ObservableCollection<ShopItem>(
                _shopManager.ShopItems.Where(i => i.Category == "Activity" && i.ActivityType == "Exercise"));

            TrainingItems = new ObservableCollection<ShopItem>(
                _shopManager.ShopItems.Where(i => i.Category == "Activity" && i.ActivityType == "Training"));

            PlayItems = new ObservableCollection<ShopItem>(
                _shopManager.ShopItems.Where(i => i.Category == "Activity" && i.ActivityType == "Play"));

            MeditationItems = new ObservableCollection<ShopItem>(
                _shopManager.ShopItems.Where(i => i.Category == "Activity" && i.ActivityType == "Meditate"));

            // Set up purchase command
            PurchaseCommand = new RelayCommand<string>(PurchaseItem);

            // Subscribe to currency changes only
            if (_bonsai.Currency is INotifyPropertyChanged currencyNotifier)
            {
                currencyNotifier.PropertyChanged += Currency_PropertyChanged;
            }
        }

        private void PurchaseItem(string itemId)
        {
            // Find and purchase the item
            var item = _shopManager.ShopItems.FirstOrDefault(i => i.Id == itemId);
            if (item != null && !item.IsUnlocked && _bonsai.Currency.BonsaiBills >= item.Price)
            {
                _bonsai.Currency.SpendBills(item.Price);
                item.IsUnlocked = true;

                // Refresh UI
                OnPropertyChanged(nameof(Currency));
                RefreshItemLists();
            }
        }

        private void Currency_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Currency));
        }

        private void RefreshItemLists()
        {
            // This will refresh all the lists to reflect unlocked status
            OnPropertyChanged(nameof(FoodItems));
            OnPropertyChanged(nameof(CleanItems));
            OnPropertyChanged(nameof(ExerciseItems));
            OnPropertyChanged(nameof(TrainingItems));
            OnPropertyChanged(nameof(PlayItems));
            OnPropertyChanged(nameof(MeditationItems));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Fixed relay command implementation
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T>? _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter == null && typeof(T).IsValueType)
                return false;

            return _canExecute == null || _canExecute((T)(parameter ?? default(T)!));
        }

        public void Execute(object? parameter)
        {
            _execute((T)(parameter ?? default(T)!));
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}