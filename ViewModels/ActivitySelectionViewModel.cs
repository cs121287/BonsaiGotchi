using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.ViewModels
{
    public class ActivitySelectionViewModel : INotifyPropertyChanged
    {
        private readonly Bonsai _bonsai;
        private readonly ShopManager _shopManager;
        private string _activityType;
        private ShopItem? _selectedActivity;

        public string ActivityTitle => $"Select {_activityType} Activity";

        public ObservableCollection<ShopItem> ActivityOptions { get; }

        public ShopItem? SelectedActivity
        {
            get => _selectedActivity;
            set
            {
                if (_selectedActivity != value)
                {
                    // Remove IsSelected implementation since ShopItem doesn't have this property
                    // We'll track selection state separately
                    _selectedActivity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanSelectActivity));
                }
            }
        }

        public bool CanSelectActivity => SelectedActivity != null && SelectedActivity.IsUnlocked;

        public ICommand SelectActivityCommand { get; }
        public ICommand OpenShopCommand { get; }

        // Add required modifier to ensure these are set
        public required Action<bool> CloseWithResult { get; set; }

        public ActivitySelectionViewModel(Bonsai bonsai, ShopManager shopManager, string activityType)
        {
            _bonsai = bonsai;
            _shopManager = shopManager;
            _activityType = activityType;

            // Get the activity options for this type
            var options = _shopManager.GetItemsByType(activityType); // Use existing method instead of filtering here

            ActivityOptions = new ObservableCollection<ShopItem>(options);

            // Select the first unlocked option by default
            SelectedActivity = ActivityOptions.FirstOrDefault(a => a.IsUnlocked);

            // Commands
            SelectActivityCommand = new RelayCommand(SelectActivity, () => CanSelectActivity);
            OpenShopCommand = new RelayCommand(OpenShop);
        }

        private void SelectActivity()
        {
            // This will be implemented to return the selected activity
            CloseWithResult(true);
        }

        private void OpenShop()
        {
            // Open shop window
            var shopWindow = new ShopWindow(_bonsai, _shopManager);
            shopWindow.ShowDialog();

            // Refresh the collection with updated items
            var updatedOptions = _shopManager.GetItemsByType(_activityType);
            ActivityOptions.Clear();
            foreach (var item in updatedOptions)
            {
                ActivityOptions.Add(item);
            }

            // Re-select an item if possible
            if (SelectedActivity == null || !SelectedActivity.IsUnlocked)
            {
                SelectedActivity = ActivityOptions.FirstOrDefault(a => a.IsUnlocked);
            }

            OnPropertyChanged(nameof(ActivityOptions));
            OnPropertyChanged(nameof(CanSelectActivity));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Corrected RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}