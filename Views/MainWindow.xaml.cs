using BonsaiGotchiGame.Models;
using BonsaiGotchiGame.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Path = System.IO.Path;
using System.Collections.Generic;
using BonsaiGotchiGame.ViewModels;
using BonsaiGotchiGame.Services;
using System.Linq;

namespace BonsaiGotchiGame
{
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        private readonly string _saveFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BonsaiGotchiGame", "save.json");
        private readonly DispatcherTimer _gameTimer = new DispatcherTimer();
        private readonly ObservableCollection<string> _journalEntries = new ObservableCollection<string>();
        private string _statusMessage = "Your bonsai is doing well.";
        private Bonsai _bonsai = new Bonsai("My Bonsai");
        private BitmapImage? _currentBackgroundImage;
        private bool _isInteractionEnabled = true;
        private int _previousLevel = 1;
        private MoodState _previousMoodState = MoodState.Content;
        private GrowthStage _previousGrowthStage = GrowthStage.Seedling;
        private HealthCondition _previousHealthCondition = HealthCondition.Healthy;
        private SpriteAnimator? _spriteAnimator;
        private bool _disposed = false;
        private readonly object _updateLock = new object();
        private readonly object _xpAnimLock = new object();
        private readonly object _eventHandlerLock = new object();
        private int _lastXpValue = 0;
        private Dictionary<int, DateTime> _recentXpGains = new Dictionary<int, DateTime>();

        // Background system
        private BackgroundService? _backgroundService;
        private int _lastHour = -1; // Track the last hour we updated the background

        // Currency and Shop System
        private ShopManager? _shopManager;
        private Dictionary<string, ShopItem> _currentActivityItems = new Dictionary<string, ShopItem>();

        // Dictionary to track cooldown timers without modifying button content
        private Dictionary<string, DispatcherTimer> _cooldownTimers = new Dictionary<string, DispatcherTimer>();
        private Dictionary<string, TextBlock> _cooldownLabels = new Dictionary<string, TextBlock>();

        // Food inventory tracking
        private Dictionary<string, int> _foodInventory = new Dictionary<string, int>();

        public Bonsai Bonsai
        {
            get => _viewModel?.Bonsai ?? _bonsai;
            set
            {
                lock (_eventHandlerLock)
                {
                    if (_bonsai != null)
                    {
                        // Unsubscribe from old bonsai
                        _bonsai.PropertyChanged -= Bonsai_PropertyChanged;
                    }

                    _bonsai = value;
                    _lastXpValue = _bonsai?.XP ?? 0;

                    if (_bonsai != null)
                    {
                        // Subscribe to new bonsai
                        _bonsai.PropertyChanged += Bonsai_PropertyChanged;
                    }
                }

                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _viewModel?.StatusMessage ?? _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage? CurrentBackgroundImage
        {
            get => _currentBackgroundImage;
            set
            {
                if (_currentBackgroundImage != value)
                {
                    _currentBackgroundImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsInteractionEnabled
        {
            get => _viewModel?.IsInteractionEnabled ?? _isInteractionEnabled;
            set
            {
                _isInteractionEnabled = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> JournalEntries => _journalEntries;

        private MainViewModel? _viewModel;

        public MainWindow()
        {
            // Initialize components
            InitializeComponent();

            try
            {
                // Initialize background service first
                _backgroundService = new BackgroundService();

                // Create and set the ViewModel
                _viewModel = new MainViewModel();

                // Set DataContext to ViewModel for most bindings
                DataContext = _viewModel;

                // Initialize food inventory system
                InitializeFoodInventory();

                // Initialize currency system and shop manager
                if (_viewModel.Bonsai != null)
                {
                    // Set the bonsai from ViewModel
                    Bonsai = _viewModel.Bonsai;

                    // Initialize currency if it doesn't exist
                    if (_viewModel.Bonsai.Currency == null)
                    {
                        _viewModel.Bonsai.Currency = new Currency();
                    }

                    // Create and initialize shop manager
                    _shopManager = new ShopManager(_viewModel.Bonsai);

                    // Set default activities
                    InitializeDefaultActivities();
                }

                // Initialize background animator - will be fully set up in Window_Loaded
                if (_viewModel != null)
                {
                    _viewModel.BonsaiStateChanged += ViewModel_BonsaiStateChanged;

                    // Subscribe to ViewModel property changes
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                }

                // Set up the journal binding manually since it's on MainWindow
                if (JournalEntriesList != null)
                {
                    JournalEntriesList.ItemsSource = _journalEntries;
                }

                // Add initial journal entry
                AddJournalEntry("Welcome to BonsaiGotchi! Your bonsai awaits your care.");
                AddJournalEntry("Your journey with your bonsai begins now.");

                // Set initial background
                UpdateBackgroundImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Initialization Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateBackgroundImage()
        {
            try
            {
                if (_backgroundService == null || _viewModel?.Bonsai == null)
                    return;

                int currentHour = _viewModel.Bonsai.GameHour;

                // Only update if the hour has changed or this is the first update
                if (currentHour != _lastHour)
                {
                    var backgroundImage = _backgroundService.GetBackgroundImage(currentHour);

                    if (backgroundImage != null)
                    {
                        CurrentBackgroundImage = backgroundImage;

                        // Also update the BackgroundImage control directly
                        if (BackgroundImage != null)
                        {
                            BackgroundImage.Source = backgroundImage;
                        }

                        _lastHour = currentHour;

                        // Add debug output
                        Console.WriteLine($"Background updated for hour {currentHour}");

                        // Log the background change
                        string timeOfDay = GetTimeOfDayDescription(currentHour);
                        AddJournalEntry($"The time of day has changed to {timeOfDay}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating background image: {ex.Message}");
            }
        }

        private string GetTimeOfDayDescription(int hour)
        {
            return hour switch
            {
                >= 6 and < 12 => "morning",
                >= 12 and < 18 => "afternoon",
                >= 18 and < 22 => "evening",
                _ => "night"
            };
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                // Forward ViewModel property changes to our properties for binding
                switch (e.PropertyName)
                {
                    case nameof(MainViewModel.StatusMessage):
                        OnPropertyChanged(nameof(StatusMessage));
                        break;
                    case nameof(MainViewModel.CurrentBackgroundImage):
                        // Update our background image when ViewModel changes
                        if (_viewModel?.CurrentBackgroundImage != null)
                        {
                            CurrentBackgroundImage = _viewModel.CurrentBackgroundImage;
                            if (BackgroundImage != null)
                            {
                                BackgroundImage.Source = _viewModel.CurrentBackgroundImage;
                            }
                        }
                        break;
                    case nameof(MainViewModel.IsInteractionEnabled):
                        OnPropertyChanged(nameof(IsInteractionEnabled));
                        break;
                    case nameof(MainViewModel.Bonsai):
                        Bonsai = _viewModel?.Bonsai ?? new Bonsai();
                        OnPropertyChanged(nameof(Bonsai));
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ViewModel_PropertyChanged: {ex.Message}");
            }
        }

        // Initialize food inventory system
        private void InitializeFoodInventory()
        {
            try
            {
                // Initialize with some starter food items
                _foodInventory["basic_fertilizer"] = -1; // -1 indicates unlimited
                _foodInventory["burger"] = 3;
                _foodInventory["ice_cream"] = 2;
                _foodInventory["vegetables"] = 5;
                _foodInventory["premium_nutrients"] = 1;
                _foodInventory["special_treat"] = 0;

                // Update food button displays
                UpdateFoodButtonsDisplay();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing food inventory: {ex.Message}");
            }
        }

        // Initialize inventory from Bonsai object
        private void InitializeInventory()
        {
            // Initialize food inventory counts for UI display
            UpdateFoodButtonInventoryCounts();
        }

        private void UpdateFoodButtonInventoryCounts()
        {
            // Update inventory counts on food buttons
            UpdateFoodButtonDisplay(FeedBasicFertilizerButton, Bonsai.BASIC_FERTILIZER_ID);
            UpdateFoodButtonDisplay(FeedBurgerButton, Bonsai.BURGER_ID);
            UpdateFoodButtonDisplay(FeedIceCreamButton, Bonsai.ICE_CREAM_ID);
            UpdateFoodButtonDisplay(FeedVegetablesButton, Bonsai.VEGETABLES_ID);
            UpdateFoodButtonDisplay(FeedPremiumNutrientsButton, Bonsai.PREMIUM_NUTRIENTS_ID);
            UpdateFoodButtonDisplay(FeedSpecialTreatButton, Bonsai.SPECIAL_TREAT_ID);
        }

        // Add this method to fix food button display logic
        private void UpdateFoodButtonDisplay(Button? button, string foodId)
        {
            if (button == null) return;

            try
            {
                int count = _viewModel?.Bonsai?.Inventory?.GetItemCount(foodId) ?? 0;

                // Always enable the button (clicking with 0 items will show shop or do nothing)
                button.IsEnabled = true;

                // Show count in tag for display, but show 0 if we have 0 items
                if (count == -1) // unlimited (basic fertilizer)
                {
                    button.Tag = null; // Don't show count for unlimited items
                }
                else if (count > 0)
                {
                    button.Tag = count.ToString();
                }
                else
                {
                    button.Tag = "0"; // Show 0 when we have no items
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating food button display: {ex.Message}");
                button.IsEnabled = true; // Always keep enabled as fallback
                button.Tag = "0";
            }
        }

        // This should be called from ShopWindow_ItemPurchased
        private void RefreshInventoryDisplay()
        {
            UpdateFoodButtonInventoryCounts();
        }

        // Shop window event handler - fixed signature with nullable sender
        private void ShopWindow_ItemPurchased(object? sender, ShopWindow.ItemPurchasedEventArgs e)
        {
            try
            {
                // Sync the food inventory first
                SyncFoodInventoryWithBonsai();

                // Update the inventory display when an item is purchased
                RefreshInventoryDisplay();

                // Add a journal entry about the purchase
                AddJournalEntry($"Purchased {e.ItemId} from the shop!");

                // Add debugging info
                Console.WriteLine($"Item purchased: {e.ItemId}, Category: {e.Category}");
                if (_viewModel?.Bonsai?.Inventory != null)
                {
                    Console.WriteLine($"Current inventory count: {_viewModel.Bonsai.Inventory.GetItemCount(e.ItemId)}");
                }

                // Force update of all food buttons
                UpdateFoodButtonsDisplay();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling shop purchase: {ex.Message}");
            }
        }

        // Update food button displays with inventory counts
        private void UpdateFoodButtonsDisplay()
        {
            try
            {
                // Update each food button with its inventory count
                UpdateFoodButtonDisplay(FeedBasicFertilizerButton, "basic_fertilizer");
                UpdateFoodButtonDisplay(FeedBurgerButton, "burger");
                UpdateFoodButtonDisplay(FeedIceCreamButton, "ice_cream");
                UpdateFoodButtonDisplay(FeedVegetablesButton, "vegetables");
                UpdateFoodButtonDisplay(FeedPremiumNutrientsButton, "premium_nutrients");
                UpdateFoodButtonDisplay(FeedSpecialTreatButton, "special_treat");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating food buttons: {ex.Message}");
            }
        }

        // Add a food item to inventory
        private void AddFoodToInventory(string foodId, int quantity = 1)
        {
            try
            {
                // Special case: basic_fertilizer is always unlimited
                if (foodId == "basic_fertilizer") return;

                // Add to inventory
                if (_foodInventory.ContainsKey(foodId))
                {
                    _foodInventory[foodId] += quantity;
                }
                else
                {
                    _foodInventory[foodId] = quantity;
                }

                // Update button display safely on UI thread
                if (Dispatcher.CheckAccess())
                {
                    UpdateFoodButtonsDisplay();
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!_disposed)
                            UpdateFoodButtonsDisplay();
                    }), DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding food to inventory: {ex.Message}");
            }
        }

        // Use a food item from inventory
        private bool UseFoodFromInventory(string foodId)
        {
            try
            {
                // Special case: basic_fertilizer is always unlimited
                if (foodId == "basic_fertilizer") return true;

                // Check if we have the item
                if (_foodInventory.TryGetValue(foodId, out int count))
                {
                    if (count > 0)
                    {
                        // Reduce count
                        _foodInventory[foodId]--;

                        // Update button display safely on UI thread
                        if (Dispatcher.CheckAccess())
                        {
                            UpdateFoodButtonsDisplay();
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (!_disposed)
                                    UpdateFoodButtonsDisplay();
                            }), DispatcherPriority.Background);
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error using food from inventory: {ex.Message}");
                return false;
            }
        }

        // Initialize default activities
        private void InitializeDefaultActivities()
        {
            if (_shopManager != null)
            {
                try
                {
                    // Set the default activity for each type
                    _currentActivityItems["Clean"] = _shopManager.ShopItems.First(i => i.Id == "basic_clean");
                    _currentActivityItems["Exercise"] = _shopManager.ShopItems.First(i => i.Id == "basic_exercise");
                    _currentActivityItems["Training"] = _shopManager.ShopItems.First(i => i.Id == "basic_training");
                    _currentActivityItems["Play"] = _shopManager.ShopItems.First(i => i.Id == "ball");
                    _currentActivityItems["Meditate"] = _shopManager.ShopItems.First(i => i.Id == "basic_meditation");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing default activities: {ex.Message}");
                }
            }
        }

        private void SyncFoodInventoryWithBonsai()
        {
            if (_viewModel?.Bonsai?.Inventory == null) return;

            // Clear the existing food inventory dictionary
            _foodInventory.Clear();

            // Set basic fertilizer as unlimited
            _foodInventory["basic_fertilizer"] = -1;

            // Copy inventory items from Bonsai to the local dictionary
            foreach (var item in _viewModel.Bonsai.Inventory.Items)
            {
                if (item.Key == Bonsai.BASIC_FERTILIZER_ID) continue; // Skip basic fertilizer

                _foodInventory[item.Key] = item.Value;
            }

            Console.WriteLine("Food inventory synced with Bonsai inventory");
        }

        // Shop button click handler
        private void Shop_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.Bonsai == null || _shopManager == null) return;

            var shopWindow = new ShopWindow(_viewModel.Bonsai, _shopManager);
            shopWindow.Owner = this;

            // Handle the item purchased event
            shopWindow.ItemPurchased += ShopWindow_ItemPurchased;

            shopWindow.ShowDialog();

            AnimationService.AnimateButtonClick(sender as Button);
        }

        // Window event handlers
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set up sprite animator now that the window is loaded
                InitializeSpriteAnimator();

                // Initialize background animator in ViewModel
                if (_viewModel != null)
                {
                    _viewModel.InitializeBackgroundAnimator(BackgroundImage);

                    // Set up XP change monitoring
                    if (_viewModel.Bonsai != null)
                    {
                        _lastXpValue = _viewModel.Bonsai.XP;
                    }
                }

                // Add this to sync inventory
                SyncFoodInventoryWithBonsai();

                // Initialize inventory system and display counts
                InitializeInventory();

                // Set initial mood emoji
                UpdateMoodEmoji();

                // Check for daily rewards
                if (_viewModel?.Bonsai?.Currency != null)
                {
                    if (_viewModel.Bonsai.Currency.CheckDailyReward())
                    {
                        AddJournalEntry("You received 1 Bonsai Bill as your daily reward!");
                    }
                }

                // Update food button displays
                UpdateFoodButtonsDisplay();

                // Add some sample journal entries to test the journal
                AddJournalEntry("Game loaded successfully!");
                AddJournalEntry("Your bonsai is ready for your care and attention.");

                // Force property change notifications to update UI
                OnPropertyChanged(nameof(Bonsai));
                OnPropertyChanged(nameof(StatusMessage));
                OnPropertyChanged(nameof(IsInteractionEnabled));

                // Update the background image immediately
                UpdateBackgroundImage();

                // Set up a timer to periodically check for background updates
                var backgroundTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(30) // Check every 30 seconds
                };
                backgroundTimer.Tick += (s, args) => UpdateBackgroundImage();
                backgroundTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Window_Loaded: {ex.Message}");
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                // Clean up resources when window is closed
                if (_viewModel != null)
                {
                    _viewModel.SaveBonsai();
                }

                Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Window_Closed: {ex.Message}");
            }
        }

        private void InitializeSpriteAnimator()
        {
            if (_spriteAnimator != null)
            {
                _spriteAnimator.Dispose();
            }

            _spriteAnimator = new SpriteAnimator(BonsaiImage);

            // Set initial animation state if bonsai is available
            if (_viewModel?.Bonsai != null)
            {
                _spriteAnimator.UpdateAnimation(_viewModel.Bonsai.CurrentState);
            }
        }

        private void ViewModel_BonsaiStateChanged(object? sender, BonsaiState state)
        {
            try
            {
                // Update sprite animation based on state with null check
                _spriteAnimator?.UpdateAnimation(state);

                // Update journal based on state changes
                AddJournalEntry($"Your bonsai's state changed to {state}.");

                // Apply visual effects based on state with null check
                if (StatusTextBlock != null)
                {
                    AnimationService.AnimateStatusMessage(StatusTextBlock);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling state change: {ex.Message}");
            }
        }

        private void Bonsai_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                // When cooldown-related properties change, update UI accordingly
                if (e.PropertyName?.StartsWith("Can") == true)
                {
                    string action = e.PropertyName.Substring(3); // Remove "Can" prefix

                    // Update action button enabled state in UI thread safely
                    if (Dispatcher.CheckAccess())
                    {
                        UpdateButtonState(action);
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed)
                                UpdateButtonState(action);
                        }), DispatcherPriority.Background);
                    }
                }

                // Check for time changes to update background
                if (e.PropertyName == nameof(Bonsai.GameHour))
                {
                    if (Dispatcher.CheckAccess())
                    {
                        UpdateBackgroundImage();
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed)
                                UpdateBackgroundImage();
                        }), DispatcherPriority.Background);
                    }
                }

                // Check for XP changes to trigger animations
                if (e.PropertyName == nameof(Bonsai.XP) && sender is Bonsai bonsai)
                {
                    int currentXP = bonsai.XP;
                    lock (_xpAnimLock)
                    {
                        int xpGained = currentXP - _lastXpValue;
                        if (xpGained > 0)
                        {
                            // Store the time of this XP gain with a unique key
                            int uniqueKey = DateTime.Now.GetHashCode() + xpGained;
                            _recentXpGains[uniqueKey] = DateTime.Now;

                            // Animate XP gain in UI thread safely
                            if (Dispatcher.CheckAccess())
                            {
                                ShowXPGainAnimation(xpGained);
                            }
                            else
                            {
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    if (!_disposed)
                                        ShowXPGainAnimation(xpGained);
                                }), DispatcherPriority.Background);
                            }
                        }
                        _lastXpValue = currentXP;
                    }
                }

                // Check for level up
                if (e.PropertyName == nameof(Bonsai.Level) && sender is Bonsai levelBonsai && levelBonsai.Level > _previousLevel)
                {
                    if (Dispatcher.CheckAccess())
                    {
                        ShowLevelUpAnimation();

                        // Award 5 Bonsai Bills for level up
                        if (levelBonsai.Currency != null)
                        {
                            levelBonsai.Currency.AddBills(5);
                            AddJournalEntry($"You received 5 Bonsai Bills for reaching level {levelBonsai.Level}!");
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed)
                            {
                                ShowLevelUpAnimation();

                                // Award 5 Bonsai Bills for level up
                                if (levelBonsai.Currency != null)
                                {
                                    levelBonsai.Currency.AddBills(5);
                                    AddJournalEntry($"You received 5 Bonsai Bills for reaching level {levelBonsai.Level}!");
                                }
                            }
                        }), DispatcherPriority.Background);
                    }
                    _previousLevel = levelBonsai.Level;
                }

                // Check for mood state change
                if (e.PropertyName == nameof(Bonsai.MoodState) && sender is Bonsai moodBonsai && moodBonsai.MoodState != _previousMoodState)
                {
                    if (Dispatcher.CheckAccess())
                    {
                        AddJournalEntry($"Your bonsai's mood changed to {moodBonsai.MoodState}.");
                        UpdateMoodEmoji();

                        // Update XP multiplier display when mood changes
                        if (XPProgressBar != null)
                        {
                            AnimationService.AnimateProgressChange(XPProgressBar, XPProgressBar.Value, XPProgressBar.Value);
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed)
                            {
                                AddJournalEntry($"Your bonsai's mood changed to {moodBonsai.MoodState}.");
                                UpdateMoodEmoji();

                                // Update XP multiplier display when mood changes
                                if (XPProgressBar != null)
                                {
                                    AnimationService.AnimateProgressChange(XPProgressBar, XPProgressBar.Value, XPProgressBar.Value);
                                }
                            }
                        }), DispatcherPriority.Background);
                    }
                    _previousMoodState = moodBonsai.MoodState;
                }

                // Check for growth stage change
                if (e.PropertyName == nameof(Bonsai.GrowthStage) && sender is Bonsai stageBonsai && stageBonsai.GrowthStage != _previousGrowthStage)
                {
                    if (Dispatcher.CheckAccess())
                    {
                        AddJournalEntry($"Your bonsai has evolved into a {stageBonsai.GrowthStage}!");
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed)
                                AddJournalEntry($"Your bonsai has evolved into a {stageBonsai.GrowthStage}!");
                        }), DispatcherPriority.Background);
                    }
                    _previousGrowthStage = stageBonsai.GrowthStage;
                }

                // Check for health condition change
                if (e.PropertyName == nameof(Bonsai.HealthCondition) && sender is Bonsai healthBonsai && healthBonsai.HealthCondition != _previousHealthCondition)
                {
                    if (Dispatcher.CheckAccess())
                    {
                        if (healthBonsai.HealthCondition == HealthCondition.Healthy)
                        {
                            AddJournalEntry($"Your bonsai has recovered and is now healthy!");
                        }
                        else
                        {
                            AddJournalEntry($"Your bonsai has developed {healthBonsai.HealthCondition}! Take care of it!");
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed)
                            {
                                if (healthBonsai.HealthCondition == HealthCondition.Healthy)
                                {
                                    AddJournalEntry($"Your bonsai has recovered and is now healthy!");
                                }
                                else
                                {
                                    AddJournalEntry($"Your bonsai has developed {healthBonsai.HealthCondition}! Take care of it!");
                                }
                            }
                        }), DispatcherPriority.Background);
                    }
                    _previousHealthCondition = healthBonsai.HealthCondition;
                }

                // Update streak displays
                if (e.PropertyName == nameof(Bonsai.ConsecutiveDaysGoodCare))
                {
                    if (Dispatcher.CheckAccess())
                    {
                        // Highlight the streak display when it changes
                        if (XPProgressBar != null)
                        {
                            AnimationService.PulseElement(XPProgressBar, TimeSpan.FromSeconds(1));
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed && XPProgressBar != null)
                            {
                                // Highlight the streak display when it changes
                                AnimationService.PulseElement(XPProgressBar, TimeSpan.FromSeconds(1));
                            }
                        }), DispatcherPriority.Background);
                    }
                }

                // Update currency display
                if (e.PropertyName == nameof(Bonsai.Currency) ||
                    (e.PropertyName == "BonsaiBills" && sender is Currency))
                {
                    if (Dispatcher.CheckAccess())
                    {
                        // Update the currency display in the UI
                        UpdateCurrencyDisplay();
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed)
                            {
                                // Update the currency display in the UI
                                UpdateCurrencyDisplay();
                            }
                        }), DispatcherPriority.Background);
                    }
                }

                // Update inventory displays
                if (e.PropertyName == nameof(Bonsai.Inventory) || e.PropertyName?.Contains("Inventory") == true)
                {
                    if (Dispatcher.CheckAccess())
                    {
                        UpdateFoodButtonsDisplay();
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed)
                                UpdateFoodButtonsDisplay();
                        }), DispatcherPriority.Background);
                    }
                }

                // Force UI updates for key properties
                if (e.PropertyName == nameof(Bonsai.Water) || e.PropertyName == nameof(Bonsai.Health) ||
                    e.PropertyName == nameof(Bonsai.Energy) || e.PropertyName == nameof(Bonsai.Hunger) ||
                    e.PropertyName == nameof(Bonsai.Cleanliness) || e.PropertyName == nameof(Bonsai.Growth) ||
                    e.PropertyName == nameof(Bonsai.Level) || e.PropertyName == nameof(Bonsai.XP) ||
                    e.PropertyName == nameof(Bonsai.GameHour) || e.PropertyName == nameof(Bonsai.GameMinute) ||
                    e.PropertyName == nameof(Bonsai.GameDay) || e.PropertyName == nameof(Bonsai.GameMonth) ||
                    e.PropertyName == nameof(Bonsai.GameYear))
                {
                    if (Dispatcher.CheckAccess())
                    {
                        OnPropertyChanged(nameof(Bonsai));
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_disposed)
                                OnPropertyChanged(nameof(Bonsai));
                        }), DispatcherPriority.Background);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in property changed handler: {ex.Message}");
            }
        }

        // Update currency display in the UI
        private void UpdateCurrencyDisplay()
        {
            try
            {
                if (_viewModel?.Bonsai?.Currency == null) return;

                // If we have a currency display TextBlock, update it
                if (CurrencyDisplay != null)
                {
                    CurrencyDisplay.Text = _viewModel.Bonsai.Currency.BonsaiBills.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating currency display: {ex.Message}");
            }
        }

        private void UpdateButtonState(string actionName)
        {
            try
            {
                // Map action name to button
                Button? button = GetButtonForAction(actionName);
                if (button == null) return;

                // Get current cooldown state
                bool isEnabled = GetCanActionValue(actionName);

                // Update button state - only the enabled property, don't modify content
                button.IsEnabled = isEnabled;

                // If action was previously on cooldown and is now enabled
                if (!(!isEnabled || !_cooldownTimers.ContainsKey(actionName)))
                {
                    // Stop the cooldown timer safely
                    if (_cooldownTimers.TryGetValue(actionName, out var timer))
                    {
                        timer.Stop();
                        _cooldownTimers.Remove(actionName);
                    }

                    // Remove cooldown label if it exists
                    if (_cooldownLabels.ContainsKey(actionName))
                    {
                        _cooldownLabels.Remove(actionName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating button state: {ex.Message}");
            }
        }

        private Button? GetButtonForAction(string actionName)
        {
            return actionName switch
            {
                "Water" => WaterButton,
                "Prune" => PruneButton,
                "Rest" => RestButton,
                "Fertilize" => FertilizeButton,
                "CleanArea" => CleanAreaButton,
                "Exercise" => ExerciseButton,
                "Train" => TrainingButton,
                "Play" => PlayButton,
                "Meditate" => MeditateButton,
                _ => null
            };
        }

        private bool GetCanActionValue(string actionName)
        {
            if (_viewModel?.Bonsai == null) return false;

            return actionName switch
            {
                "Water" => _viewModel.Bonsai.CanWater,
                "Prune" => _viewModel.Bonsai.CanPrune,
                "Rest" => _viewModel.Bonsai.CanRest,
                "Fertilize" => _viewModel.Bonsai.CanFertilize,
                "CleanArea" => _viewModel.Bonsai.CanCleanArea,
                "Exercise" => _viewModel.Bonsai.CanExercise,
                "Train" => _viewModel.Bonsai.CanTrain,
                "Play" => _viewModel.Bonsai.CanPlay,
                "Meditate" => _viewModel.Bonsai.CanMeditate,
                _ => true
            };
        }

        private void UpdateMoodEmoji()
        {
            if (_viewModel?.Bonsai == null || MoodEmoji == null) return;

            MoodEmoji.Text = _viewModel.Bonsai.MoodState switch
            {
                MoodState.Ecstatic => "😍",
                MoodState.Happy => "😊",
                MoodState.Content => "🙂",
                MoodState.Neutral => "😐",
                MoodState.Unhappy => "🙁",
                MoodState.Sad => "😢",
                MoodState.Miserable => "😭",
                _ => "🙂"
            };
        }

        private void ShowLevelUpAnimation()
        {
            if (_viewModel?.Bonsai == null || LevelUpDisplay == null) return;

            // Set level up display content
            if (NewLevelText != null)
            {
                NewLevelText.Text = $"Your bonsai reached level {_viewModel.Bonsai.Level}!";
            }
            if (NewStageText != null)
            {
                NewStageText.Text = $"Growth Stage: {_viewModel.Bonsai.GrowthStage}";
            }

            // Set XP bonus text
            if (LevelUpXPText != null)
            {
                double xpBonus = _viewModel.Bonsai.XPMultiplier - 1.0;
                LevelUpXPText.Text = $"+{xpBonus:P0} XP Multiplier";
            }

            // Show the level up overlay
            LevelUpDisplay.Visibility = Visibility.Visible;

            // Disable interactions while showing level up
            IsInteractionEnabled = false;

            // Add journal entry
            AddJournalEntry($"Level Up! Your bonsai is now level {_viewModel.Bonsai.Level}!");

            // Play a sound if enabled
            if (GameSettings.Instance?.PlaySounds ?? true)
            {
                // Play level up sound - placeholder for now
                // System.Media.SystemSounds.Asterisk.Play();
            }
        }

        private void LevelUpContinue_Click(object sender, RoutedEventArgs e)
        {
            // Hide level up display and re-enable interactions
            if (LevelUpDisplay != null)
            {
                LevelUpDisplay.Visibility = Visibility.Collapsed;
            }
            IsInteractionEnabled = true;

            // Apply button animation
            AnimationService.AnimateButtonClick(sender as Button);
        }

        private void ShowXPGainAnimation(int xpAmount)
        {
            if (xpAmount <= 0 || XPGainCanvas == null) return;

            try
            {
                lock (_xpAnimLock)
                {
                    // Create XP gain notification text
                    var xpText = new TextBlock
                    {
                        Text = $"+{xpAmount} XP",
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Colors.Purple),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = Colors.White,
                            ShadowDepth = 0,
                            BlurRadius = 5
                        }
                    };

                    // Place text in the canvas above the bonsai
                    Canvas.SetLeft(xpText, 512 - (xpText.Text.Length * 8)); // Centered horizontally
                    Canvas.SetTop(xpText, 300); // Above the bonsai
                    XPGainCanvas.Children.Add(xpText);

                    // Create animation storyboard
                    var storyboard = new Storyboard();

                    // Create opacity animation (fade in then out)
                    var opacityAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300),
                        AutoReverse = false
                    };
                    Storyboard.SetTarget(opacityAnimation, xpText);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(TextBlock.OpacityProperty));
                    storyboard.Children.Add(opacityAnimation);

                    // Create fade out animation
                    var fadeOutAnimation = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        BeginTime = TimeSpan.FromMilliseconds(1500),
                        Duration = TimeSpan.FromMilliseconds(500)
                    };
                    Storyboard.SetTarget(fadeOutAnimation, xpText);
                    Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(TextBlock.OpacityProperty));
                    storyboard.Children.Add(fadeOutAnimation);

                    // Create upward movement animation
                    var moveUpAnimation = new DoubleAnimation
                    {
                        From = 300,
                        To = 200,
                        Duration = TimeSpan.FromMilliseconds(2000),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(moveUpAnimation, xpText);
                    Storyboard.SetTargetProperty(moveUpAnimation, new PropertyPath("(Canvas.Top)"));
                    storyboard.Children.Add(moveUpAnimation);

                    // Clean up after animation completes
                    storyboard.Completed += (s, e) =>
                    {
                        if (XPGainCanvas != null && XPGainCanvas.Children.Contains(xpText))
                        {
                            XPGainCanvas.Children.Remove(xpText);
                        }
                    };

                    // Start the animation
                    storyboard.Begin();

                    // Also animate the XP progress bar
                    if (XPProgressBar != null)
                    {
                        AnimationService.AnimateProgressChange(XPProgressBar, XPProgressBar.Value - 5, XPProgressBar.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing XP gain animation: {ex.Message}");
            }
        }

        private void CleanupOldXPGains()
        {
            lock (_xpAnimLock)
            {
                // Remove XP gain records older than 5 seconds
                var now = DateTime.Now;
                var keysToRemove = new List<int>();

                foreach (var entry in _recentXpGains)
                {
                    if ((now - entry.Value).TotalSeconds > 5)
                    {
                        keysToRemove.Add(entry.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _recentXpGains.Remove(key);
                }
            }
        }

        private void AddJournalEntry(string entry)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entry))
                    return;

                // Use the current bonsai for time stamp
                string timeStamp;
                if (_viewModel?.Bonsai != null)
                {
                    timeStamp = $"[Day {_viewModel.Bonsai.GameDay}, {_viewModel.Bonsai.GameHour:D2}:{_viewModel.Bonsai.GameMinute:D2}]";
                }
                else
                {
                    // Fallback to real time if bonsai time is not available
                    timeStamp = $"[{DateTime.Now:HH:mm}]";
                }

                string fullEntry = $"{timeStamp} {entry}";

                // Add to journal entries on UI thread
                if (Dispatcher.CheckAccess())
                {
                    _journalEntries.Insert(0, fullEntry);

                    // Limit journal entries to prevent memory issues
                    if (_journalEntries.Count > 100)
                    {
                        _journalEntries.RemoveAt(_journalEntries.Count - 1);
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!_disposed)
                        {
                            _journalEntries.Insert(0, fullEntry);

                            // Limit journal entries to prevent memory issues
                            if (_journalEntries.Count > 100)
                            {
                                _journalEntries.RemoveAt(_journalEntries.Count - 1);
                            }
                        }
                    }), DispatcherPriority.Background);
                }

                // Debug output
                Console.WriteLine($"Journal entry added: {fullEntry}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding journal entry: {ex.Message}");
            }
        }

        #region Action Button Handlers
        private void Water_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel == null) return;

            _viewModel.Water();
            AddJournalEntry("You watered your bonsai.");
            AnimationService.AnimateButtonClick(sender as Button);
        }

        private void Prune_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel == null) return;

            _viewModel.Prune();
            AddJournalEntry("You pruned your bonsai.");
            AnimationService.AnimateButtonClick(sender as Button);
        }

        private void Rest_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel == null) return;

            _viewModel.Rest();
            AddJournalEntry("Your bonsai is resting.");
            AnimationService.AnimateButtonClick(sender as Button);
        }

        private void Fertilize_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel == null) return;

            _viewModel.Fertilize();
            AddJournalEntry("You applied fertilizer to your bonsai.");
            AnimationService.AnimateButtonClick(sender as Button);
        }

        private void CleanArea_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null || _shopManager == null) return;

            // Show activity selection window
            var selectionWindow = new ActivitySelectionWindow(_viewModel.Bonsai, _shopManager, "Clean");
            selectionWindow.Owner = this;

            if (selectionWindow.ShowDialog() == true)
            {
                // User selected an activity
                var selectedActivity = selectionWindow.SelectedActivity;

                if (selectedActivity != null && selectedActivity.IsUnlocked)
                {
                    // Store the current activity
                    _currentActivityItems["Clean"] = selectedActivity;

                    // Perform the activity
                    _viewModel.Bonsai.CleanArea();

                    // Add journal entry with the specific activity type
                    AddJournalEntry($"You cleaned your bonsai's area using {selectedActivity.Name}.");

                    // Apply button animation
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                // If no selection was made, use default activity if available
                if (_currentActivityItems.TryGetValue("Clean", out var defaultActivity) && defaultActivity.IsUnlocked)
                {
                    _viewModel.Bonsai.CleanArea();
                    AddJournalEntry($"You cleaned your bonsai's area using {defaultActivity.Name}.");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
        }

        private void Exercise_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null || _shopManager == null) return;

            // Show activity selection window
            var selectionWindow = new ActivitySelectionWindow(_viewModel.Bonsai, _shopManager, "Exercise");
            selectionWindow.Owner = this;

            if (selectionWindow.ShowDialog() == true)
            {
                // User selected an activity
                var selectedActivity = selectionWindow.SelectedActivity;

                if (selectedActivity != null && selectedActivity.IsUnlocked)
                {
                    // Store the current activity
                    _currentActivityItems["Exercise"] = selectedActivity;

                    // Perform the activity
                    _viewModel.Bonsai.LightExercise();

                    // Add journal entry with the specific activity type
                    AddJournalEntry($"Your bonsai did some exercise using {selectedActivity.Name}.");

                    // Apply button animation
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                // If no selection was made, use default activity if available
                if (_currentActivityItems.TryGetValue("Exercise", out var defaultActivity) && defaultActivity.IsUnlocked)
                {
                    _viewModel.Bonsai.LightExercise();
                    AddJournalEntry($"Your bonsai did some exercise using {defaultActivity.Name}.");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
        }

        private void Training_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null || _shopManager == null) return;

            // Show activity selection window
            var selectionWindow = new ActivitySelectionWindow(_viewModel.Bonsai, _shopManager, "Training");
            selectionWindow.Owner = this;

            if (selectionWindow.ShowDialog() == true)
            {
                // User selected an activity
                var selectedActivity = selectionWindow.SelectedActivity;

                if (selectedActivity != null && selectedActivity.IsUnlocked)
                {
                    // Store the current activity
                    _currentActivityItems["Training"] = selectedActivity;

                    // Perform the activity
                    _viewModel.Bonsai.IntenseTraining();

                    // Add journal entry with the specific activity type
                    AddJournalEntry($"Your bonsai completed an intense training session using {selectedActivity.Name}.");

                    // Apply button animation
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                // If no selection was made, use default activity if available
                if (_currentActivityItems.TryGetValue("Training", out var defaultActivity) && defaultActivity.IsUnlocked)
                {
                    _viewModel.Bonsai.IntenseTraining();
                    AddJournalEntry($"Your bonsai completed an intense training session using {defaultActivity.Name}.");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null || _shopManager == null) return;

            // Show activity selection window
            var selectionWindow = new ActivitySelectionWindow(_viewModel.Bonsai, _shopManager, "Play");
            selectionWindow.Owner = this;

            if (selectionWindow.ShowDialog() == true)
            {
                // User selected an activity
                var selectedActivity = selectionWindow.SelectedActivity;

                if (selectedActivity != null && selectedActivity.IsUnlocked)
                {
                    // Store the current activity
                    _currentActivityItems["Play"] = selectedActivity;

                    // Perform the activity
                    _viewModel.Bonsai.Play();

                    // Add journal entry with the specific activity type
                    AddJournalEntry($"You played with your bonsai using {selectedActivity.Name}.");

                    // Apply button animation
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                // If no selection was made, use default activity if available
                if (_currentActivityItems.TryGetValue("Play", out var defaultActivity) && defaultActivity.IsUnlocked)
                {
                    _viewModel.Bonsai.Play();
                    AddJournalEntry($"You played with your bonsai using {defaultActivity.Name}.");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
        }

        private void Meditate_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null || _shopManager == null) return;

            // Show activity selection window
            var selectionWindow = new ActivitySelectionWindow(_viewModel.Bonsai, _shopManager, "Meditate");
            selectionWindow.Owner = this;

            if (selectionWindow.ShowDialog() == true)
            {
                // User selected an activity
                var selectedActivity = selectionWindow.SelectedActivity;

                if (selectedActivity != null && selectedActivity.IsUnlocked)
                {
                    // Store the current activity
                    _currentActivityItems["Meditate"] = selectedActivity;

                    // Perform the activity
                    _viewModel.Bonsai.Meditate();

                    // Add journal entry with the specific activity type
                    AddJournalEntry($"You meditated with your bonsai using {selectedActivity.Name}.");

                    // Apply button animation
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                // If no selection was made, use default activity if available
                if (_currentActivityItems.TryGetValue("Meditate", out var defaultActivity) && defaultActivity.IsUnlocked)
                {
                    _viewModel.Bonsai.Meditate();
                    AddJournalEntry($"You meditated with your bonsai using {defaultActivity.Name}.");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
        }
        #endregion

        #region Feeding Button Handlers
        private void FeedBasicFertilizer_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            // Basic fertilizer is always free
            _viewModel.Bonsai.FeedBasicFertilizer();
            AddJournalEntry("You fed your bonsai basic fertilizer.");
            AnimationService.AnimateButtonClick(sender as Button);
        }

        private void FeedBurger_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            int currentCount = _viewModel.Bonsai.Inventory.GetItemCount("burger");

            if (currentCount > 0)
            {
                // Use the item if we have it
                if (_viewModel.Bonsai.FeedBurger())
                {
                    AddJournalEntry("You fed your bonsai a burger. It enjoyed it but doesn't seem healthier.");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                // Show shop to buy the item
                ShowShopForFood("burger");
            }
        }

        private void FeedIceCream_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            int currentCount = _viewModel.Bonsai.Inventory.GetItemCount("ice_cream");

            if (currentCount > 0)
            {
                if (_viewModel.Bonsai.FeedIceCream())
                {
                    AddJournalEntry("You fed your bonsai ice cream. It's very happy but might get sick!");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                ShowShopForFood("ice_cream");
            }
        }

        private void FeedVegetables_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            int currentCount = _viewModel.Bonsai.Inventory.GetItemCount("vegetables");

            if (currentCount > 0)
            {
                if (_viewModel.Bonsai.FeedVegetables())
                {
                    AddJournalEntry("You fed your bonsai vegetables. It didn't like them but will be healthier.");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                ShowShopForFood("vegetables");
            }
        }

        private void FeedPremiumNutrients_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            int currentCount = _viewModel.Bonsai.Inventory.GetItemCount("premium_nutrients");

            if (currentCount > 0)
            {
                if (_viewModel.Bonsai.FeedPremiumNutrients())
                {
                    AddJournalEntry("You fed your bonsai premium nutrients. It's getting stronger!");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                ShowShopForFood("premium_nutrients");
            }
        }

        private void FeedSpecialTreat_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            int currentCount = _viewModel.Bonsai.Inventory.GetItemCount("special_treat");

            if (currentCount > 0)
            {
                if (_viewModel.Bonsai.FeedSpecialTreat())
                {
                    AddJournalEntry("You gave your bonsai a special treat. It's extremely happy!");
                    AnimationService.AnimateButtonClick(sender as Button);
                }
            }
            else
            {
                ShowShopForFood("special_treat");
            }
        }

        // Helper method to show shop for food items
        private void ShowShopForFood(string foodId)
        {
            if (_viewModel?.Bonsai == null || _shopManager == null) return;

            var shopWindow = new ShopWindow(_viewModel.Bonsai, _shopManager);
            shopWindow.Owner = this;

            // Listen for item purchased events
            shopWindow.ItemPurchased += ShopWindow_ItemPurchased;

            shopWindow.ShowDialog();
        }
        #endregion

        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            _viewModel.SaveBonsai();
            AddJournalEntry("Game saved successfully!");
            AnimationService.AnimateButtonClick(sender as Button);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };

            if (settingsWindow.ShowDialog() == true && _viewModel != null)
            {
                // Settings may have changed, update ViewModel
                _viewModel.UpdateSettings();
            }

            AnimationService.AnimateButtonClick(sender as Button);
        }

        // Window events
        protected override void OnClosing(CancelEventArgs e)
        {
            // Try to save the game before closing
            if (_viewModel != null)
            {
                _viewModel.SaveBonsai();
            }

            base.OnClosing(e);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events with thread safety
                    lock (_eventHandlerLock)
                    {
                        if (_viewModel != null)
                        {
                            _viewModel.BonsaiStateChanged -= ViewModel_BonsaiStateChanged;
                            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                            _viewModel.Dispose();
                        }

                        if (_bonsai != null)
                        {
                            _bonsai.PropertyChanged -= Bonsai_PropertyChanged;
                        }
                    }

                    // Dispose of sprite animator safely
                    if (_spriteAnimator != null)
                    {
                        _spriteAnimator.Dispose();
                        _spriteAnimator = null;
                    }

                    // Clean up timers safely
                    _gameTimer.Stop();

                    foreach (var timer in _cooldownTimers.Values)
                    {
                        timer?.Stop();
                    }
                    _cooldownTimers.Clear();
                    _cooldownLabels.Clear();

                    // Clean up animations safely
                    if (XPGainCanvas != null)
                    {
                        XPGainCanvas.Children.Clear();
                    }

                    // Clean up shop manager resources
                    _shopManager = null;
                    _currentActivityItems.Clear();

                    // Clear inventory
                    _foodInventory.Clear();

                    // Clear XP gains tracking
                    lock (_xpAnimLock)
                    {
                        _recentXpGains.Clear();
                    }

                    // Dispose background service
                    _backgroundService = null;
                }

                _disposed = true;
            }
        }

        ~MainWindow()
        {
            Dispose(false);
        }
        #endregion
    }
}
