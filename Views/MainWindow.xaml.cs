using BonsaiGotchiGame.Models;
using BonsaiGotchiGame.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace BonsaiGotchiGame
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly string _saveFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BonsaiGotchiGame", "save.json");
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

        public Bonsai Bonsai
        {
            get => _bonsai;
            set { _bonsai = value; OnPropertyChanged(); }
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

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Initialize game timer
            _gameTimer.Tick += OnGameTimerTick;
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Start();

            // Load game or create new
            LoadGameOrCreateNew();

            // Initialize background image
            UpdateBackgroundImage();

            // Initialize bonsai image
            UpdateBonsaiImage();

            // Set mood emoji
            UpdateMoodEmoji();

            // Set journal entries
            JournalEntriesList.ItemsSource = _journalEntries;

            // Store initial values for tracking changes
            _previousLevel = Bonsai.Level;
            _previousMoodState = Bonsai.MoodState;
            _previousGrowthStage = Bonsai.GrowthStage;
            _previousHealthCondition = Bonsai.HealthCondition;

            // Add initial journal entry
            AddJournalEntry($"Welcome to BonsaiGotchi! Your {Bonsai.GrowthStage} is ready to grow.");
        }

        private void OnGameTimerTick(object? sender, EventArgs e)
        {
            Bonsai.UpdateState();
            UpdateBackgroundImage();
            UpdateBonsaiImage();
            UpdateStatusMessage();
            UpdateMoodEmoji();

            // Check for level up
            if (Bonsai.Level > _previousLevel)
            {
                ShowLevelUpAnimation();
                _previousLevel = Bonsai.Level;
            }

            // Check for mood state change
            if (Bonsai.MoodState != _previousMoodState)
            {
                AddJournalEntry($"Your bonsai's mood changed to {Bonsai.MoodState}.");
                _previousMoodState = Bonsai.MoodState;
            }

            // Check for growth stage change
            if (Bonsai.GrowthStage != _previousGrowthStage)
            {
                AddJournalEntry($"Your bonsai has evolved into a {Bonsai.GrowthStage}!");
                _previousGrowthStage = Bonsai.GrowthStage;
            }

            // Check for health condition change
            if (Bonsai.HealthCondition != _previousHealthCondition)
            {
                if (Bonsai.HealthCondition == HealthCondition.Healthy)
                {
                    AddJournalEntry($"Your bonsai has recovered and is now healthy!");
                }
                else
                {
                    AddJournalEntry($"Your bonsai has developed {Bonsai.HealthCondition}! Take care of it!");
                }
                _previousHealthCondition = Bonsai.HealthCondition;
            }

            // Check for auto-save
            if (GameSettings.Instance.EnableAutoSave &&
                DateTime.Now.Minute % GameSettings.Instance.AutoSaveInterval == 0 &&
                DateTime.Now.Second == 0)
            {
                SaveGame();
            }
        }

        private void UpdateBackgroundImage()
        {
            string timeOfDay = Bonsai.GameHour switch
            {
                >= 6 and < 10 => "morning",
                >= 10 and < 16 => "day",
                >= 16 and < 20 => "evening",
                _ => "night"
            };

            string imagePath = $"/Images/background_{timeOfDay}.jpg";
            try
            {
                CurrentBackgroundImage = new BitmapImage(new Uri($"pack://application:,,,{imagePath}"));
            }
            catch (Exception)
            {
                // Use default if image not found
                CurrentBackgroundImage = null;
            }
        }

        private void UpdateBonsaiImage()
        {
            try
            {
                if (Bonsai == null) return;

                string state = Bonsai.CurrentState.ToString().ToLower();
                string stage = Bonsai.GrowthStage.ToString().ToLower();

                // Create a list of paths to try in order
                List<string> imagePaths = new List<string>
        {
            $"pack://application:,,,/BonsaiGotchiGame;component/Assets/Images/bonsai_{stage}_{state}.png",
            $"pack://application:,,,/BonsaiGotchiGame;component/Assets/Images/bonsai_{stage}.png",
            "pack://application:,,,/BonsaiGotchiGame;component/Assets/Images/fallback.png",
            "pack://application:,,,/BonsaiGotchiGame;component/Assets/Images/idle.png",
            "pack://application:,,,/Assets/Images/fallback.png",
            "pack://application:,,,/Assets/Images/idle.png"
        };

                bool imageLoaded = false;
                foreach (string path in imagePaths)
                {
                    try
                    {
                        // Load image with explicit options to ensure proper loading
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(path);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze(); // Make it thread-safe

                        BonsaiImage.Source = bmp;
                        imageLoaded = true;
                        break; // Successfully loaded an image
                    }
                    catch
                    {
                        // Continue to next path
                        continue;
                    }
                }

                // If no image could be loaded, create a fallback image
                if (!imageLoaded)
                {
                    // Create a simple colored rectangle as absolute fallback
                    Rectangle rect = new Rectangle
                    {
                        Fill = new SolidColorBrush(Colors.ForestGreen),
                        Width = 256,
                        Height = 256
                    };

                    // Draw a simple bonsai
                    DrawingVisual drawingVisual = new DrawingVisual();
                    using (DrawingContext dc = drawingVisual.RenderOpen())
                    {
                        // Draw pot
                        dc.DrawRectangle(
                            new SolidColorBrush(Colors.SaddleBrown),
                            new Pen(Brushes.Brown, 2),
                            new Rect(128 - 40, 200, 80, 40));

                        // Draw trunk
                        dc.DrawRectangle(
                            new SolidColorBrush(Colors.Brown),
                            null,
                            new Rect(128 - 10, 120, 20, 80));

                        // Draw foliage
                        dc.DrawEllipse(
                            new SolidColorBrush(Colors.ForestGreen),
                            new Pen(Brushes.DarkGreen, 2),
                            new Point(128, 80),
                            60, 50);
                    }

                    // Render to bitmap
                    RenderTargetBitmap rtb = new RenderTargetBitmap(256, 256, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(drawingVisual);
                    rtb.Freeze(); // Make it thread-safe

                    BonsaiImage.Source = rtb;
                }
            }
            catch (Exception ex)
            {
                // Last resort - at least don't crash
                System.Diagnostics.Debug.WriteLine($"Failed to load any bonsai image: {ex.Message}");

                // Create a simple fallback image instead of setting Background (which Image doesn't support)
                try
                {
                    // Create a solid green rectangle as an absolute fallback
                    DrawingVisual drawingVisual = new DrawingVisual();
                    using (DrawingContext dc = drawingVisual.RenderOpen())
                    {
                        dc.DrawRectangle(
                            new SolidColorBrush(Colors.LightGreen),
                            new Pen(Brushes.Green, 2),
                            new Rect(0, 0, 256, 256));

                        // Add text to indicate there's an issue
                        FormattedText text = new FormattedText(
                            "Image Error",
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Arial"),
                            16,
                            Brushes.DarkGreen,
                            VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                        dc.DrawText(text, new Point(128 - text.Width / 2, 128 - text.Height / 2));
                    }

                    // Convert to bitmap and use as source
                    RenderTargetBitmap rtb = new RenderTargetBitmap(256, 256, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(drawingVisual);
                    rtb.Freeze();
                    BonsaiImage.Source = rtb;
                }
                catch
                {
                    // If even that fails, just set Source to null to avoid crashing
                    BonsaiImage.Source = null;
                }
            }
        }

        private void UpdateMoodEmoji()
        {
            MoodEmoji.Text = Bonsai.MoodState switch
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

        private void UpdateStatusMessage()
        {
            if (Bonsai.HealthCondition != HealthCondition.Healthy)
            {
                StatusMessage = $"Your bonsai is suffering from {Bonsai.HealthCondition}!";
                return;
            }

            if (Bonsai.Water < 20)
            {
                StatusMessage = "Your bonsai is very thirsty! Water it soon.";
            }
            else if (Bonsai.Energy < 20)
            {
                StatusMessage = "Your bonsai is exhausted! Let it rest.";
            }
            else if (Bonsai.Hunger > 80)
            {
                StatusMessage = "Your bonsai is starving! Feed it soon.";
            }
            else if (Bonsai.Cleanliness < 30)
            {
                StatusMessage = "Your bonsai's area needs cleaning!";
            }
            else
            {
                StatusMessage = Bonsai.MoodState switch
                {
                    MoodState.Ecstatic => "Your bonsai is ecstatic! It's thriving beautifully.",
                    MoodState.Happy => "Your bonsai is happy and growing well.",
                    MoodState.Content => "Your bonsai is content and healthy.",
                    MoodState.Neutral => "Your bonsai is doing okay.",
                    MoodState.Unhappy => "Your bonsai seems a bit unhappy.",
                    MoodState.Sad => "Your bonsai is sad and needs attention.",
                    MoodState.Miserable => "Your bonsai is miserable! It needs urgent care!",
                    _ => "Your bonsai is growing."
                };
            }
        }

        private void ShowLevelUpAnimation()
        {
            // Set level up display content
            NewLevelText.Text = $"Your bonsai reached level {Bonsai.Level}!";
            NewStageText.Text = $"Growth Stage: {Bonsai.GrowthStage}";

            // Show the level up overlay
            LevelUpDisplay.Visibility = Visibility.Visible;

            // Disable interactions while showing level up
            IsInteractionEnabled = false;

            // Add journal entry
            AddJournalEntry($"Level Up! Your bonsai is now level {Bonsai.Level}!");

            // Play a sound if enabled
            if (GameSettings.Instance.PlaySounds)
            {
                // Play level up sound
                // System.Media.SystemSounds.Asterisk.Play();
            }
        }

        private void LevelUpContinue_Click(object sender, RoutedEventArgs e)
        {
            // Hide level up display and re-enable interactions
            LevelUpDisplay.Visibility = Visibility.Collapsed;
            IsInteractionEnabled = true;
        }

        private void LoadGameOrCreateNew()
        {
            try
            {
                if (File.Exists(_saveFilePath))
                {
                    string json = File.ReadAllText(_saveFilePath);
                    var savedBonsai = JsonSerializer.Deserialize<Bonsai>(json);
                    if (savedBonsai != null)
                    {
                        Bonsai = savedBonsai;
                        AddJournalEntry("Game loaded successfully!");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading saved game: {ex.Message}\nStarting a new game instead.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Create new game if loading failed
            Bonsai = new Bonsai("My Bonsai");
            Directory.CreateDirectory(Path.GetDirectoryName(_saveFilePath) ?? string.Empty);
        }

        private void AddJournalEntry(string entry)
        {
            string timeStamp = $"[Day {Bonsai.GameDay}, {Bonsai.GameHour:D2}:{Bonsai.GameMinute:D2}]";
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
            if (!IsInteractionEnabled) return;

            Bonsai.GiveWater();
            AddJournalEntry("You watered your bonsai.");
            UpdateStatusMessage();
            UpdateBonsaiImage();
        }

        private void Prune_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.Prune();
            AddJournalEntry("You pruned your bonsai.");
            UpdateStatusMessage();
            UpdateBonsaiImage();
        }

        private void Rest_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.Rest();
            AddJournalEntry("Your bonsai is resting.");
            UpdateStatusMessage();
            UpdateBonsaiImage();
        }

        private void Fertilize_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.ApplyFertilizer();
            AddJournalEntry("You applied fertilizer to your bonsai.");
            UpdateStatusMessage();
            UpdateBonsaiImage();
        }

        private void CleanArea_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.CleanArea();
            AddJournalEntry("You cleaned your bonsai's area.");
            UpdateStatusMessage();
        }

        private void Exercise_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.LightExercise();
            AddJournalEntry("Your bonsai did some light exercise.");
            UpdateStatusMessage();
        }

        private void Training_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.IntenseTraining();
            AddJournalEntry("Your bonsai completed an intense training session.");
            UpdateStatusMessage();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.Play();
            AddJournalEntry("You played with your bonsai.");
            UpdateStatusMessage();
        }

        private void Meditate_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.Meditate();
            AddJournalEntry("You meditated with your bonsai.");
            UpdateStatusMessage();
        }
        #endregion

        #region Feeding Button Handlers
        private void FeedBasicFertilizer_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.FeedBasicFertilizer();
            AddJournalEntry("You fed your bonsai basic fertilizer.");
            UpdateStatusMessage();
        }

        private void FeedBurger_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.FeedBurger();
            AddJournalEntry("You fed your bonsai a burger. It enjoyed it but doesn't seem healthier.");
            UpdateStatusMessage();
        }

        private void FeedIceCream_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.FeedIceCream();
            AddJournalEntry("You fed your bonsai ice cream. It's very happy but might get sick!");
            UpdateStatusMessage();
        }

        private void FeedVegetables_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.FeedVegetables();
            AddJournalEntry("You fed your bonsai vegetables. It didn't like them but will be healthier.");
            UpdateStatusMessage();
        }

        private void FeedPremiumNutrients_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.FeedPremiumNutrients();
            AddJournalEntry("You fed your bonsai premium nutrients. It's getting stronger!");
            UpdateStatusMessage();
        }

        private void FeedSpecialTreat_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInteractionEnabled) return;

            Bonsai.FeedSpecialTreat();
            AddJournalEntry("You gave your bonsai a special treat. It's extremely happy!");
            UpdateStatusMessage();
        }
        #endregion

        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            SaveGame();
            AddJournalEntry("Game saved successfully!");
        }

        private void SaveGame()
        {
            try
            {
                string json = JsonSerializer.Serialize(Bonsai);
                File.WriteAllText(_saveFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving game: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}