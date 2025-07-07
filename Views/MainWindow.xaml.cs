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
        private readonly object _updateLock = new object(); // Add lock for thread safety
        private readonly object _xpAnimLock = new object(); // Lock for XP animations
        private int _lastXpValue = 0;
        private Dictionary<int, DateTime> _recentXpGains = new Dictionary<int, DateTime>();

        // Currency and Shop System
        private ShopManager? _shopManager;
        private Dictionary<string, ShopItem> _currentActivityItems = new Dictionary<string, ShopItem>();
        private Button? _shopButton;

        // Dictionary to track cooldown timers without modifying button content
        private Dictionary<string, DispatcherTimer> _cooldownTimers = new Dictionary<string, DispatcherTimer>();
        private Dictionary<string, TextBlock> _cooldownLabels = new Dictionary<string, TextBlock>();

        // Food inventory tracking
        private Dictionary<string, int> _foodInventory = new Dictionary<string, int>();

        public Bonsai Bonsai
        {
            get => _bonsai;
            set
            {
                if (_bonsai != null)
                {
                    // Unsubscribe from old bonsai
                    _bonsai.PropertyChanged -= Bonsai_PropertyChanged;
                }

                _bonsai = value;
                _lastXpValue = _bonsai.XP; // Initialize last XP value

                if (_bonsai != null)
                {
                    // Subscribe to new bonsai
                    _bonsai.PropertyChanged += Bonsai_PropertyChanged;
                }

                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public BitmapImage? CurrentBackgroundImage
        {
            get => _currentBackgroundImage;
            set { _currentBackgroundImage = value; OnPropertyChanged(); }
        }

        public bool IsInteractionEnabled
        {
            get => _isInteractionEnabled;
            set { _isInteractionEnabled = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> JournalEntries => _journalEntries;

        private MainViewModel? _viewModel;

        public MainWindow()
        {
            // Initialize components
            InitializeComponent();

            try
            {
                // Create and set the ViewModel
                _viewModel = new MainViewModel();
                DataContext = _viewModel;

                // Initialize food inventory system
                InitializeFoodInventory();

                // Initialize currency system and shop manager
                if (_viewModel.Bonsai != null)
                {
                    // Initialize currency if it doesn't exist
                    if (_viewModel.Bonsai.Currency == null)
                    {
                        _viewModel.Bonsai.Currency = new Currency();
                    }

                    // Create and initialize shop manager
                    _shopManager = new ShopManager(_viewModel.Bonsai);

                    // Set default activities
                    InitializeDefaultActivities();

                    // Add shop button to UI
                    AddShopButton();
                }

                // Initialize background animator - will be fully set up in Window_Loaded
                if (_viewModel != null)
                {
                    _viewModel.BonsaiStateChanged += ViewModel_BonsaiStateChanged;
                }

                // Add initial journal entry
                AddJournalEntry("Welcome to BonsaiGotchi! Your bonsai awaits your care.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Initialization Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            UpdateFoodButtonTag(FeedBasicFertilizerButton, Bonsai.BASIC_FERTILIZER_ID);
            UpdateFoodButtonTag(FeedBurgerButton, Bonsai.BURGER_ID);
            UpdateFoodButtonTag(FeedIceCreamButton, Bonsai.ICE_CREAM_ID);
            UpdateFoodButtonTag(FeedVegetablesButton, Bonsai.VEGETABLES_ID);
            UpdateFoodButtonTag(FeedPremiumNutrientsButton, Bonsai.PREMIUM_NUTRIENTS_ID);
            UpdateFoodButtonTag(FeedSpecialTreatButton, Bonsai.SPECIAL_TREAT_ID);
        }

        private void UpdateFoodButtonTag(Button button, string itemId)
        {
            if (button == null || Bonsai == null || Bonsai.Inventory == null) return;

            int count = Bonsai.Inventory.GetItemCount(itemId);

            // For basic fertilizer (which is unlimited) or no items, hide the counter
            if (count == -1 || count == 0)
            {
                button.Tag = null;
            }
            else
            {
                button.Tag = count.ToString();
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
            // Update the inventory display when an item is purchased
            RefreshInventoryDisplay();

            // Add a journal entry about the purchase
            AddJournalEntry($"Purchased {e.ItemId} from the shop!");
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

        // Update a single food button display
        private void UpdateFoodButtonDisplay(Button button, string foodId)
        {
            if (button == null) return;

            try
            {
                // Check if we have inventory for this food
                if (_foodInventory.TryGetValue(foodId, out int count))
                {
                    // If count is -1, it means unlimited
                    if (count == -1)
                    {
                        button.IsEnabled = true;

                        // Clear any existing badge
                        if (button.Tag != null)
                        {
                            button.Tag = null;
                        }
                    }
                    else
                    {
                        // Update the button's Tag with the count for display
                        button.Tag = count > 0 ? count.ToString() : null;

                        // Enable/disable based on inventory
                        button.IsEnabled = count > 0;
                    }
                }
                else
                {
                    // No inventory record found, disable button
                    button.IsEnabled = false;
                    button.Tag = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating food button display: {ex.Message}");
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

                // Update button display
                Dispatcher.Invoke(() =>
                {
                    UpdateFoodButtonsDisplay();
                });
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

                        // Update button display
                        Dispatcher.Invoke(() =>
                        {
                            UpdateFoodButtonsDisplay();
                        });

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

        // Add a shop button to the UI
        private void AddShopButton()
        {
            try
            {
                _shopButton = new Button
                {
                    Content = "🛒",
                    Height = 34,
                    Width = 34,
                    Margin = new Thickness(5, 0, 0, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#795548")),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    ToolTip = "Shop"
                };

                _shopButton.Click += ShopButton_Click;

                // Add the button to the header next to the settings button
                if (HeaderBar != null && HeaderBar.Child is Grid headerGrid)
                {
                    // Add a new column to the grid
                    var newColumn = new ColumnDefinition { Width = GridLength.Auto };
                    headerGrid.ColumnDefinitions.Add(newColumn);

                    // Add the shop button to the new column
                    Grid.SetColumn(_shopButton, headerGrid.ColumnDefinitions.Count - 1);
                    headerGrid.Children.Add(_shopButton);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding shop button: {ex.Message}");
            }
        }

        // Shop button click handler
        private void ShopButton_Click(object sender, RoutedEventArgs e)
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

                // Initialize background animator
                if (_viewModel != null)
                {
                    _viewModel.InitializeBackgroundAnimator(BackgroundImage);

                    // Set up XP change monitoring
                    if (_viewModel.Bonsai != null)
                    {
                        _lastXpValue = _viewModel.Bonsai.XP;
                    }
                }
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
                // Update sprite animation based on state
                _spriteAnimator?.UpdateAnimation(state);

                // Update journal based on state changes
                AddJournalEntry($"Your bonsai's state changed to {state}.");

                // Apply visual effects based on state
                AnimationService.AnimateStatusMessage(StatusTextBlock);
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

                    // Update action button enabled state in UI thread
                    Dispatcher.Invoke(() => {
                        UpdateButtonState(action);
                    });
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

                            // Animate XP gain in UI thread
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ShowXPGainAnimation(xpGained);
                            }));
                        }
                        _lastXpValue = currentXP;
                    }
                }

                // Check for level up
                if (e.PropertyName == nameof(Bonsai.Level) && sender is Bonsai levelBonsai && levelBonsai.Level > _previousLevel)
                {
                    Dispatcher.Invoke(() => {
                        ShowLevelUpAnimation();

                        // Award 5 Bonsai Bills for level up
                        if (levelBonsai.Currency != null)
                        {
                            levelBonsai.Currency.AddBills(5);
                            AddJournalEntry($"You received 5 Bonsai Bills for reaching level {levelBonsai.Level}!");
                        }
                    });
                    _previousLevel = levelBonsai.Level;
                }

                // Check for mood state change
                if (e.PropertyName == nameof(Bonsai.MoodState) && sender is Bonsai moodBonsai && moodBonsai.MoodState != _previousMoodState)
                {
                    Dispatcher.Invoke(() => {
                        AddJournalEntry($"Your bonsai's mood changed to {moodBonsai.MoodState}.");
                        UpdateMoodEmoji();

                        // Update XP multiplier display when mood changes
                        AnimationService.AnimateProgressChange(XPProgressBar, XPProgressBar.Value, XPProgressBar.Value);
                    });
                    _previousMoodState = moodBonsai.MoodState;
                }

                // Check for growth stage change
                if (e.PropertyName == nameof(Bonsai.GrowthStage) && sender is Bonsai stageBonsai && stageBonsai.GrowthStage != _previousGrowthStage)
                {
                    Dispatcher.Invoke(() => {
                        AddJournalEntry($"Your bonsai has evolved into a {stageBonsai.GrowthStage}!");
                    });
                    _previousGrowthStage = stageBonsai.GrowthStage;
                }

                // Check for health condition change
                if (e.PropertyName == nameof(Bonsai.HealthCondition) && sender is Bonsai healthBonsai && healthBonsai.HealthCondition != _previousHealthCondition)
                {
                    Dispatcher.Invoke(() => {
                        if (healthBonsai.HealthCondition == HealthCondition.Healthy)
                        {
                            AddJournalEntry($"Your bonsai has recovered and is now healthy!");
                        }
                        else
                        {
                            AddJournalEntry($"Your bonsai has developed {healthBonsai.HealthCondition}! Take care of it!");
                        }
                    });
                    _previousHealthCondition = healthBonsai.HealthCondition;
                }

                // Update streak displays
                if (e.PropertyName == nameof(Bonsai.ConsecutiveDaysGoodCare))
                {
                    Dispatcher.Invoke(() => {
                        // Highlight the streak display when it changes
                        AnimationService.PulseElement(XPProgressBar, TimeSpan.FromSeconds(1));
                    });
                }

                // Update currency display
                if (e.PropertyName == nameof(Bonsai.Currency) ||
                    (e.PropertyName == "BonsaiBills" && sender is Currency))
                {
                    Dispatcher.Invoke(() => {
                        // Update the currency display in the UI
                        UpdateCurrencyDisplay();
                    });
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
                    // Stop the cooldown timer
                    _cooldownTimers[actionName].Stop();
                    _cooldownTimers.Remove(actionName);

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
            if (_viewModel?.Bonsai == null) return;

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
            if (_viewModel?.Bonsai == null) return;

            // Set level up display content
            NewLevelText.Text = $"Your bonsai reached level {_viewModel.Bonsai.Level}!";
            NewStageText.Text = $"Growth Stage: {_viewModel.Bonsai.GrowthStage}";

            // Set XP bonus text
            double xpBonus = _viewModel.Bonsai.XPMultiplier - 1.0;
            LevelUpXPText.Text = $"+{xpBonus:P0} XP Multiplier";

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
            LevelUpDisplay.Visibility = Visibility.Collapsed;
            IsInteractionEnabled = true;

            // Apply button animation
            AnimationService.AnimateButtonClick(sender as Button);
        }

        private void ShowXPGainAnimation(int xpAmount)
        {
            if (xpAmount <= 0) return;

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
                    if (XPGainCanvas != null)
                    {
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
            if (_viewModel?.Bonsai == null) return;

            string timeStamp = $"[Day {_viewModel.Bonsai.GameDay}, {_viewModel.Bonsai.GameHour:D2}:{_viewModel.Bonsai.GameMinute:D2}]";
            _journalEntries.Insert(0, $"{timeStamp} {entry}");

            // Limit journal entries to prevent memory issues
            if (_journalEntries.Count > 100)
            {
                _journalEntries.RemoveAt(_journalEntries.Count - 1);
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

            // Check if we have a burger in inventory
            if (UseFoodFromInventory("burger"))
            {
                _viewModel.Bonsai.FeedBurger();
                AddJournalEntry("You fed your bonsai a burger. It enjoyed it but doesn't seem healthier.");
                AnimationService.AnimateButtonClick(sender as Button);
            }
            else
            {
                // Show shop to buy a burger
                ShowShopForFood("burger");
            }
        }

        private void FeedIceCream_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            // Check if we have ice cream in inventory
            if (UseFoodFromInventory("ice_cream"))
            {
                _viewModel.Bonsai.FeedIceCream();
                AddJournalEntry("You fed your bonsai ice cream. It's very happy but might get sick!");
                AnimationService.AnimateButtonClick(sender as Button);
            }
            else
            {
                // Show shop to buy ice cream
                ShowShopForFood("ice_cream");
            }
        }

        private void FeedVegetables_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            // Check if we have vegetables in inventory
            if (UseFoodFromInventory("vegetables"))
            {
                _viewModel.Bonsai.FeedVegetables();
                AddJournalEntry("You fed your bonsai vegetables. It didn't like them but will be healthier.");
                AnimationService.AnimateButtonClick(sender as Button);
            }
            else
            {
                // Show shop to buy vegetables
                ShowShopForFood("vegetables");
            }
        }

        private void FeedPremiumNutrients_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            // Check if we have premium nutrients in inventory
            if (UseFoodFromInventory("premium_nutrients"))
            {
                _viewModel.Bonsai.FeedPremiumNutrients();
                AddJournalEntry("You fed your bonsai premium nutrients. It's getting stronger!");
                AnimationService.AnimateButtonClick(sender as Button);
            }
            else
            {
                // Show shop to buy premium nutrients
                ShowShopForFood("premium_nutrients");
            }
        }

        private void FeedSpecialTreat_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled || _viewModel?.Bonsai == null) return;

            // Check if we have special treats in inventory
            if (UseFoodFromInventory("special_treat"))
            {
                _viewModel.Bonsai.FeedSpecialTreat();
                AddJournalEntry("You gave your bonsai a special treat. It's extremely happy!");
                AnimationService.AnimateButtonClick(sender as Button);
            }
            else
            {
                // Show shop to buy special treats
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
                    // Unsubscribe from events
                    if (_viewModel != null)
                    {
                        _viewModel.BonsaiStateChanged -= ViewModel_BonsaiStateChanged;
                        _viewModel.Dispose();
                    }

                    if (_bonsai != null)
                    {
                        _bonsai.PropertyChanged -= Bonsai_PropertyChanged;
                    }

                    // Dispose of sprite animator
                    if (_spriteAnimator != null)
                    {
                        _spriteAnimator.Dispose();
                        _spriteAnimator = null;
                    }

                    // Clean up timers
                    _gameTimer.Stop();

                    foreach (var timer in _cooldownTimers.Values)
                    {
                        timer.Stop();
                    }
                    _cooldownTimers.Clear();
                    _cooldownLabels.Clear();

                    // Clear animations
                    if (XPGainCanvas != null)
                    {
                        XPGainCanvas.Children.Clear();
                    }

                    // Clean up shop manager resources
                    _shopManager = null;
                    _currentActivityItems.Clear();

                    // Clear inventory
                    _foodInventory.Clear();
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