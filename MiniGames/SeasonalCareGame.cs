using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BonsaiGotchi.MiniGames
{
    /// <summary>
    /// Mini-game for seasonal care and weather adaptation
    /// </summary>
    public class SeasonalCareGame : MiniGameBase
    {
        private Panel gamePanel;
        private Label weatherLabel;
        private Label seasonLabel;
        private Label instructionLabel;
        private ProgressBar timeBar;
        private Timer gameTimer;
        private Button[] careButtons;
        
        private Season currentSeason;
        private Weather currentWeather;
        private int secondsRemaining = 45;
        private int totalScore = 0;
        private int careTurns = 0;
        private int maxTurns = 5;
        private Dictionary<string, List<CareOption>> idealCareOptions;
        
        // Available care options
        private enum CareOption
        {
            Water,
            Prune,
            Fertilize,
            Shade,
            MoveSunlight,
            ReduceWater,
            ProtectFromWind,
            Insulate
        }
        
        public SeasonalCareGame() : base()
        {
            // Form setup
            Text = "Seasonal Care Game";
            Size = new Size(700, 550);
            
            // Set game-specific labels
            titleLabel.Text = "Seasonal Bonsai Care Challenge";
            instructionsLabel.Text = "Your bonsai will experience different weather conditions.\n" +
                                    "Select the appropriate care actions for each condition to earn points!";
            
            // Create game layout
            CreateGameLayout();
            
            // Initialize ideal care options for different conditions
            InitializeIdealCareOptions();
            
            // Start the game
            StartGame();
        }
        
        private void CreateGameLayout()
        {
            gamePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            
            // Weather and season indicators
            Panel conditionsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(10)
            };
            
            weatherLabel = new Label
            {
                Text = "Weather: Sunny",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Size = new Size(300, 30),
                Location = new Point(20, 10)
            };
            
            seasonLabel = new Label
            {
                Text = "Season: Spring",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Size = new Size(300, 30),
                Location = new Point(20, 50)
            };
            
            conditionsPanel.Controls.Add(weatherLabel);
            conditionsPanel.Controls.Add(seasonLabel);
            
            // Instructions label
            instructionLabel = new Label
            {
                Text = "Select the best care options for these conditions:",
                Font = new Font("Segoe UI", 12),
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            // Time bar
            timeBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 20,
                Value = 100,
                ForeColor = Color.FromArgb(52, 152, 219),
                Style = ProgressBarStyle.Continuous
            };
            
            // Care options panel
            Panel careOptionsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            // Create care option buttons
            careButtons = new Button[8]; // One for each CareOption enum value
            string[] careLabels = {
                "Water", "Prune", "Fertilize", "Provide Shade",
                "Move to Sunlight", "Reduce Watering", "Protect from Wind", "Insulate"
            };
            
            int buttonWidth = 150;
            int buttonHeight = 60;
            int buttonsPerRow = 4;
            int horizontalSpacing = 20;
            int verticalSpacing = 20;
            
            for (int i = 0; i < careButtons.Length; i++)
            {
                int row = i / buttonsPerRow;
                int col = i % buttonsPerRow;
                int x = col * (buttonWidth + horizontalSpacing) + 30;
                int y = row * (buttonHeight + verticalSpacing) + 20;
                
                careButtons[i] = new Button
                {
                    Text = careLabels[i],
                    Size = new Size(buttonWidth, buttonHeight),
                    Location = new Point(x, y),
                    BackColor = Color.FromArgb(52, 152, 219),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Tag = (CareOption)i
                };
                careButtons[i].FlatAppearance.BorderSize = 0;
                careButtons[i].Click += CareButton_Click;
                
                careOptionsPanel.Controls.Add(careButtons[i]);
            }
            
            // Status display
            Label turnLabel = new Label
            {
                Text = $"Turn 0/{maxTurns}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(120, 30),
                Location = new Point(20, 180)
            };
            careOptionsPanel.Controls.Add(turnLabel);
            
            Label scoreLabel = new Label
            {
                Text = "Score: 0",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(120, 30),
                Location = new Point(150, 180)
            };
            careOptionsPanel.Controls.Add(scoreLabel);
            
            // Assemble the layout
            gamePanel.Controls.Add(careOptionsPanel);
            gamePanel.Controls.Add(instructionLabel);
            gamePanel.Controls.Add(timeBar);
            gamePanel.Controls.Add(conditionsPanel);
            
            Controls.Add(gamePanel);
            closeButton.BringToFront();
            closeButton.Location = new Point(Width - closeButton.Width - 20, Height - closeButton.Height - 20);
        }
        
        private void InitializeIdealCareOptions()
        {
            idealCareOptions = new Dictionary<string, List<CareOption>>();
            
            // Spring conditions
            idealCareOptions["Spring-Sunny"] = new List<CareOption> { 
                CareOption.Water, CareOption.Fertilize 
            };
            idealCareOptions["Spring-Rain"] = new List<CareOption> { 
                CareOption.ReduceWater, CareOption.Prune 
            };
            idealCareOptions["Spring-Cloudy"] = new List<CareOption> { 
                CareOption.Water, CareOption.Fertilize 
            };
            idealCareOptions["Spring-Wind"] = new List<CareOption> { 
                CareOption.ProtectFromWind, CareOption.Water 
            };
            
            // Summer conditions
            idealCareOptions["Summer-Sunny"] = new List<CareOption> { 
                CareOption.Water, CareOption.Shade, CareOption.Water 
            };
            idealCareOptions["Summer-Rain"] = new List<CareOption> { 
                CareOption.ReduceWater, CareOption.MoveSunlight 
            };
            idealCareOptions["Summer-Cloudy"] = new List<CareOption> { 
                CareOption.Water, CareOption.Prune 
            };
            idealCareOptions["Summer-Wind"] = new List<CareOption> { 
                CareOption.Water, CareOption.ProtectFromWind 
            };
            
            // Autumn conditions
            idealCareOptions["Autumn-Sunny"] = new List<CareOption> { 
                CareOption.Water, CareOption.MoveSunlight 
            };
            idealCareOptions["Autumn-Rain"] = new List<CareOption> { 
                CareOption.ReduceWater, CareOption.Prune 
            };
            idealCareOptions["Autumn-Cloudy"] = new List<CareOption> { 
                CareOption.Water, CareOption.ReduceWater 
            };
            idealCareOptions["Autumn-Wind"] = new List<CareOption> { 
                CareOption.ProtectFromWind, CareOption.ReduceWater 
            };
            
            // Winter conditions
            idealCareOptions["Winter-Sunny"] = new List<CareOption> { 
                CareOption.ReduceWater, CareOption.Insulate 
            };
            idealCareOptions["Winter-Snow"] = new List<CareOption> { 
                CareOption.Insulate, CareOption.ReduceWater 
            };
            idealCareOptions["Winter-Cloudy"] = new List<CareOption> { 
                CareOption.ReduceWater, CareOption.Insulate 
            };
            idealCareOptions["Winter-Wind"] = new List<CareOption> { 
                CareOption.Insulate, CareOption.ProtectFromWind
            };
        }
        
        private void StartGame()
        {
            // Reset game state
            careTurns = 0;
            totalScore = 0;
            secondsRemaining = 45;
            
            // Generate first weather condition
            GenerateRandomWeatherCondition();
            UpdateUI();
            
            // Start timer
            gameTimer = new Timer
            {
                Interval = 1000 // 1 second
            };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();
        }
        
        private void GenerateRandomWeatherCondition()
        {
            // Generate random season (weighted toward current real season)
            int month = DateTime.Now.Month;
            Season realSeason = month switch
            {
                12 or 1 or 2 => Season.Winter,
                3 or 4 or 5 => Season.Spring,
                6 or 7 or 8 => Season.Summer,
                _ => Season.Autumn
            };
            
            // 50% chance of real season, 50% chance of random season
            if (random.NextDouble() < 0.5)
            {
                currentSeason = realSeason;
            }
            else
            {
                currentSeason = (Season)random.Next(0, 4);
            }
            
            // Generate appropriate weather for the season
            currentWeather = currentSeason switch
            {
                Season.Winter => (Weather)random.Next(new[] { (int)Weather.Sunny, (int)Weather.Cloudy, (int)Weather.Snow, (int)Weather.Wind }),
                Season.Spring => (Weather)random.Next(new[] { (int)Weather.Sunny, (int)Weather.Cloudy, (int)Weather.Rain, (int)Weather.Wind }),
                Season.Summer => (Weather)random.Next(new[] { (int)Weather.Sunny, (int)Weather.Cloudy, (int)Weather.Rain, (int)Weather.Wind }),
                Season.Autumn => (Weather)random.Next(new[] { (int)Weather.Sunny, (int)Weather.Cloudy, (int)Weather.Rain, (int)Weather.Wind }),
                _ => Weather.Sunny
            };
        }
        
        private void UpdateUI()
        {
            // Update season and weather labels
            seasonLabel.Text = $"Season: {currentSeason}";
            weatherLabel.Text = $"Weather: {currentWeather}";
            
            // Update instruction based on conditions
            instructionLabel.Text = $"Select the best care for your bonsai in {currentSeason} with {currentWeather} weather:";
            
            // Update turn counter
            (gamePanel.Controls.Find("turnLabel", true)[0] as Label).Text = $"Turn {careTurns}/{maxTurns}";
            
            // Update score display
            (gamePanel.Controls.Find("scoreLabel", true)[0] as Label).Text = $"Score: {totalScore}";
            
            // Update time bar
            timeBar.Value = (int)((double)secondsRemaining / 45 * 100);
        }
        
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            secondsRemaining--;
            
            // Update time bar
            timeBar.Value = (int)((double)secondsRemaining / 45 * 100);
            
            if (secondsRemaining <= 0 || careTurns >= maxTurns)
            {
                // End the game
                gameTimer.Stop();
                EndGame();
            }
        }
        
        private void CareButton_Click(object sender, EventArgs e)
        {
            if (sender is Button careButton && careButton.Tag is CareOption selectedOption)
            {
                // Calculate score for this choice
                int turnScore = CalculateScoreForOption(selectedOption);
                
                // Add to total score
                totalScore += turnScore;
                
                // Increment turn counter
                careTurns++;
                
                // Generate feedback
                string feedback = turnScore switch
                {
                    >= 20 => "Perfect choice! Excellent care!",
                    >= 10 => "Good choice! Your bonsai is happy.",
                    >= 5 => "Acceptable choice, but not ideal.",
                    _ => "Not the best choice for these conditions."
                };
                
                // Show feedback
                MessageBox.Show(
                    $"{feedback}\n\nYou earned {turnScore} points.",
                    "Care Result",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                
                // Check if game is over
                if (careTurns >= maxTurns)
                {
                    gameTimer.Stop();
                    EndGame();
                }
                else
                {
                    // Generate new conditions
                    GenerateRandomWeatherCondition();
                    UpdateUI();
                }
            }
        }
        
        private int CalculateScoreForOption(CareOption selectedOption)
        {
            // Get ideal options for current conditions
            string conditionKey = $"{currentSeason}-{currentWeather}";
            
            if (!idealCareOptions.ContainsKey(conditionKey))
            {
                // Fallback for any conditions we didn't explicitly define
                return 5; // Default score
            }
            
            List<CareOption> idealOptions = idealCareOptions[conditionKey];
            
            // Calculate score
            if (idealOptions.Contains(selectedOption))
            {
                // Ideal choice - high score
                return 20;
            }
            
            // Calculate score based on appropriateness
            switch (selectedOption)
            {
                case CareOption.Water:
                    // Appropriate in most conditions except rain
                    if (currentWeather == Weather.Rain)
                        return 0; // Bad choice when already raining
                    if (currentSeason == Season.Winter)
                        return 5; // Not ideal in winter
                    return 10;
                    
                case CareOption.ReduceWater:
                    // Good in rain or winter
                    if (currentWeather == Weather.Rain || currentSeason == Season.Winter)
                        return 15;
                    if (currentWeather == Weather.Sunny && currentSeason == Season.Summer)
                        return 0; // Bad in summer heat
                    return 5;
                    
                case CareOption.Shade:
                    // Good in sunny summer
                    if (currentWeather == Weather.Sunny && currentSeason == Season.Summer)
                        return 15;
                    if (currentSeason == Season.Winter)
                        return 0; // Bad in winter
                    return 5;
                    
                case CareOption.Insulate:
                    // Good in winter
                    if (currentSeason == Season.Winter)
                        return 15;
                    if (currentSeason == Season.Summer)
                        return 0; // Bad in summer
                    return 5;
                    
                case CareOption.ProtectFromWind:
                    // Good in windy conditions
                    if (currentWeather == Weather.Wind)
                        return 15;
                    return 5;
                    
                // Add more custom logic for other care options
                
                default:
                    return 5; // Default average score
            }
        }
        
        private void EndGame()
        {
            // Calculate final score (0-100)
            double normalizedScore = Math.Min(100, (double)totalScore / (maxTurns * 20) * 100);
            
            // Generate result message
            string resultMessage = normalizedScore switch
            {
                >= 80 => "Bonsai Master! Your seasonal care skills are excellent!",
                >= 60 => "Great job! Your bonsai will thrive with this level of care.",
                >= 40 => "Good effort. Your bonsai care knowledge is developing.",
                _ => "More practice needed. Study the seasonal needs of your bonsai."
            };
            
            // Complete the game
            CompleteGame(normalizedScore, resultMessage);
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Stop the timer
            gameTimer?.Stop();
            gameTimer?.Dispose();
            
            // Clean up button handlers
            foreach (Button button in careButtons)
            {
                button.Click -= CareButton_Click;
            }
            
            base.OnFormClosing(e);
        }
    }
    
    // These enum definitions would normally be in a shared location,
    // duplicating here for completeness
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
    
    public enum Weather
    {
        Sunny,
        Cloudy,
        Rain,
        Humid,
        Wind,
        Storm,
        Snow
    }
}