using System;
using System.Drawing;
using System.Windows.Forms;
using BonsaiGotchi.MiniGames;

namespace BonsaiGotchi
{
    /// <summary>
    /// Extension of the main form to add Phase 2 elements - Enhanced Gameplay
    /// </summary>
    public partial class BonsaiGotchiForm
    {
        #region Enhanced UI Components
        
        // Enhanced gameplay buttons
        private Button? playMiniGameButton;
        private Button? pestControlButton;
        private Button? treatDiseaseButton;
        private Button? playMusicButton;
        private Button? adjustLightButton;
        
        // Enhanced stats display
        private Label? seasonWeatherLabel;
        private Label? moodLabel;
        private Label? pestDiseaseLabel;
        private ProgressBar? stressBar;
        private PictureBox? moodIndicator;
        private Timer? seasonWeatherTimer;
        
        #endregion
        
        #region Add Enhanced UI Elements
        
        /// <summary>
        /// Adds the enhanced gameplay UI elements
        /// </summary>
        private void AddEnhancedGameplayElements()
        {
            // Create enhanced stats display elements
            AddEnhancedStatsElements();
            
            // Create enhanced control panel elements
            AddEnhancedControlElements();
            
            // Initialize season and weather timer
            InitializeSeasonWeatherTimer();
        }
        
        /// <summary>
        /// Adds enhanced stats display elements
        /// </summary>
        private void AddEnhancedStatsElements()
        {
            if (needsPanel == null) return;
            
            // Add season and weather label
            seasonWeatherLabel = new Label
            {
                Text = "Season: Spring | Weather: Sunny",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                AutoSize = true,
                Location = new Point(0, 180)
            };
            needsPanel.Controls.Add(seasonWeatherLabel);
            
            // Add mood indicator label
            moodLabel = new Label
            {
                Text = "Mood: Content",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                AutoSize = true,
                Location = new Point(0, 205)
            };
            needsPanel.Controls.Add(moodLabel);
            
            // Add pest/disease status
            pestDiseaseLabel = new Label
            {
                Text = "Pests: 0% | Disease: 0%",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                AutoSize = true,
                Location = new Point(0, 230)
            };
            needsPanel.Controls.Add(pestDiseaseLabel);
            
            // Add stress bar
            Label stressLabel = new Label
            {
                Text = "Stress:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                AutoSize = true,
                Location = new Point(0, 255)
            };
            needsPanel.Controls.Add(stressLabel);
            
            stressBar = new ProgressBar
            {
                Size = new Size(needsPanel.Width - 80, 15),
                Location = new Point(80, 257),
                Minimum = 0,
                Maximum = 100,
                Value = 20,
                ForeColor = Color.FromArgb(231, 76, 60) // Red for stress
            };
            needsPanel.Controls.Add(stressBar);
            
            // Add mood indicator icon
            moodIndicator = new PictureBox
            {
                Size = new Size(30, 30),
                Location = new Point(needsPanel.Width - 50, 200),
                BackColor = Color.FromArgb(46, 204, 113) // Green for good mood
            };
            needsPanel.Controls.Add(moodIndicator);
        }
        
        /// <summary>
        /// Adds enhanced control panel elements
        /// </summary>
        private void AddEnhancedControlElements()
        {
            if (controlPanel == null) return;
            
            // Calculate positions for new buttons
            int startY = 65;
            int buttonWidth = 120;
            int buttonHeight = 35;
            int spacing = 10;
            
            // Play mini-game button
            playMiniGameButton = new Button
            {
                Text = "Mini Games",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(16, startY),
                BackColor = Color.FromArgb(26, 188, 156),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            playMiniGameButton.FlatAppearance.BorderSize = 0;
            playMiniGameButton.Click += PlayMiniGameButton_Click;
            
            // Pest control button
            pestControlButton = new Button
            {
                Text = "Pest Control",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(16 + buttonWidth + spacing, startY),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            pestControlButton.FlatAppearance.BorderSize = 0;
            pestControlButton.Click += PestControlButton_Click;
            
            // Disease treatment button
            treatDiseaseButton = new Button
            {
                Text = "Treat Disease",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(16 + (buttonWidth + spacing) * 2, startY),
                BackColor = Color.FromArgb(142, 68, 173),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            treatDiseaseButton.FlatAppearance.BorderSize = 0;
            treatDiseaseButton.Click += TreatDiseaseButton_Click;
            
            // Play music button
            playMusicButton = new Button
            {
                Text = "Play Music",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(16 + (buttonWidth + spacing) * 3, startY),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            playMusicButton.FlatAppearance.BorderSize = 0;
            playMusicButton.Click += PlayMusicButton_Click;
            
            // Adjust light button
            adjustLightButton = new Button
            {
                Text = "Adjust Light",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(16 + (buttonWidth + spacing) * 4, startY),
                BackColor = Color.FromArgb(241, 196, 15),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            adjustLightButton.FlatAppearance.BorderSize = 0;
            adjustLightButton.Click += AdjustLightButton_Click;
            
            // Add buttons to control panel
            controlPanel.Controls.Add(playMiniGameButton);
            controlPanel.Controls.Add(pestControlButton);
            controlPanel.Controls.Add(treatDiseaseButton);
            controlPanel.Controls.Add(playMusicButton);
            controlPanel.Controls.Add(adjustLightButton);
        }
        
        /// <summary>
        /// Initializes the timer for season and weather changes
        /// </summary>
        private void InitializeSeasonWeatherTimer()
        {
            // Create timer for season and weather updates (checks every minute)
            seasonWeatherTimer = new Timer
            {
                Interval = 60000 // 1 minute
            };
            seasonWeatherTimer.Tick += SeasonWeatherTimer_Tick;
            seasonWeatherTimer.Start();
            
            // Do an initial update
            UpdateSeasonWeatherDisplay();
        }
        
        #endregion
        
        #region Enhanced Update Methods
        
        /// <summary>
        /// Enhanced game timer update that includes new stats
        /// </summary>
        private void UpdateEnhancedGameStats()
        {
            if (currentBonsai == null) return;
            
            try
            {
                // Calculate elapsed time since last update
                TimeSpan elapsed = DateTime.Now - lastUpdateTime;
                lastUpdateTime = DateTime.Now;
                
                // Use the enhanced update method
                currentBonsai.UpdateEnhanced(elapsed);
                
                // Update UI elements
                UpdateEnhancedUI();
                
                // Check for any significant status changes that need immediate attention
                CheckForSignificantChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Enhanced game update error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates the enhanced UI elements
        /// </summary>
        private void UpdateEnhancedUI()
        {
            if (currentBonsai == null) return;
            
            SafeInvoke(() =>
            {
                // Update season and weather display
                UpdateSeasonWeatherDisplay();
                
                // Update mood display
                if (moodLabel != null)
                {
                    moodLabel.Text = $"Mood: {currentBonsai.CurrentMood}";
                    
                    // Update mood indicator color
                    if (moodIndicator != null)
                    {
                        moodIndicator.BackColor = GetMoodColor(currentBonsai.CurrentMood);
                    }
                }
                
                // Update pest and disease display
                if (pestDiseaseLabel != null)
                {
                    pestDiseaseLabel.Text = $"Pests: {currentBonsai.PestInfestation:0}% | Disease: {currentBonsai.DiseaseLevel:0}%";
                    
                    // Change color based on severity
                    if (currentBonsai.HasPests || currentBonsai.HasDisease)
                    {
                        pestDiseaseLabel.ForeColor = Color.FromArgb(231, 76, 60); // Red for warning
                    }
                    else
                    {
                        pestDiseaseLabel.ForeColor = Color.FromArgb(52, 73, 94); // Normal color
                    }
                }
                
                // Update stress bar
                if (stressBar != null)
                {
                    stressBar.Value = (int)Math.Round(currentBonsai.StressLevel);
                    
                    // Change color based on stress level
                    if (currentBonsai.StressLevel > 70)
                        stressBar.ForeColor = Color.FromArgb(231, 76, 60); // Red
                    else if (currentBonsai.StressLevel > 40)
                        stressBar.ForeColor = Color.FromArgb(230, 126, 34); // Orange
                    else
                        stressBar.ForeColor = Color.FromArgb(241, 196, 15); // Yellow
                }
                
                // Update button states based on conditions
                UpdateEnhancedButtonStates();
            });
        }
        
        /// <summary>
        /// Updates the season and weather display
        /// </summary>
        private void UpdateSeasonWeatherDisplay()
        {
            if (currentBonsai == null || seasonWeatherLabel == null) return;
            
            seasonWeatherLabel.Text = $"Season: {currentBonsai.CurrentSeason} | Weather: {currentBonsai.CurrentWeather}";
            
            // Add visual indicator based on weather
            string weatherIcon = currentBonsai.CurrentWeather switch
            {
                Weather.Sunny => "☀️",
                Weather.Cloudy => "☁️",
                Weather.Rain => "🌧️",
                Weather.Humid => "💧",
                Weather.Wind => "💨",
                Weather.Storm => "⛈️",
                Weather.Snow => "❄️",
                _ => ""
            };
            
            if (!string.IsNullOrEmpty(weatherIcon))
            {
                seasonWeatherLabel.Text += $" {weatherIcon}";
            }
        }
        
        /// <summary>
        /// Updates the enabled/disabled state of enhanced buttons based on conditions
        /// </summary>
        private void UpdateEnhancedButtonStates()
        {
            if (currentBonsai == null) return;
            
            // Pest control button - only enabled if pests are present
            if (pestControlButton != null)
            {
                pestControlButton.Enabled = currentBonsai.PestInfestation > 10;
            }
            
            // Disease treatment button - only enabled if disease is present
            if (treatDiseaseButton != null)
            {
                treatDiseaseButton.Enabled = currentBonsai.DiseaseLevel > 10;
            }
            
            // Disable all buttons if the bonsai is dead
            if (currentBonsai.IsDead)
            {
                if (pestControlButton != null) pestControlButton.Enabled = false;
                if (treatDiseaseButton != null) treatDiseaseButton.Enabled = false;
                if (playMusicButton != null) playMusicButton.Enabled = false;
                if (adjustLightButton != null) adjustLightButton.Enabled = false;
                if (playMiniGameButton != null) playMiniGameButton.Enabled = false;
            }
        }
        
        /// <summary>
        /// Checks for significant changes that need immediate user attention
        /// </summary>
        private void CheckForSignificantChanges()
        {
            if (currentBonsai == null) return;
            
            // Check for critical pest infestation
            if (currentBonsai.PestInfestation > 80)
            {
                SafeInvoke(() => MessageBox.Show(
                    $"{currentBonsai.Name} is suffering from a severe pest infestation! Immediate treatment is required.",
                    "Critical Pest Infestation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning));
            }
            
            // Check for critical disease
            if (currentBonsai.DiseaseLevel > 80)
            {
                SafeInvoke(() => MessageBox.Show(
                    $"{currentBonsai.Name} has a severe disease! Treat it immediately before permanent damage occurs.",
                    "Critical Disease Alert",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning));
            }
            
            // Check for critical stress
            if (currentBonsai.StressLevel > 90)
            {
                SafeInvoke(() => MessageBox.Show(
                    $"{currentBonsai.Name} is extremely stressed! Address all its needs immediately.",
                    "Critical Stress Level",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning));
            }
        }
        
        #endregion
        
        #region Enhanced Button Event Handlers
        
        private void PlayMiniGameButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Create menu for mini-game selection
                ContextMenuStrip gameMenu = new ContextMenuStrip();
                
                // Add mini-game options
                gameMenu.Items.Add("Leaf Counting").Click += (s, e) => LaunchMiniGame(MiniGameManager.GameType.LeafCounting);
                gameMenu.Items.Add("Pest Removal").Click += (s, e) => LaunchMiniGame(MiniGameManager.GameType.PestRemoval);
                gameMenu.Items.Add("Pruning Puzzle").Click += (s, e) => LaunchMiniGame(MiniGameManager.GameType.PruningPuzzle);
                gameMenu.Items.Add("Seasonal Care").Click += (s, e) => LaunchMiniGame(MiniGameManager.GameType.SeasonalCare);
                
                // Show the menu
                gameMenu.Show(playMiniGameButton, new Point(0, playMiniGameButton.Height));
            }
            catch (Exception ex)
            {
                ShowError("Failed to open mini-game menu", ex);
            }
        }
        
        private void LaunchMiniGame(MiniGameManager.GameType gameType)
        {
            try
            {
                if (currentBonsai == null) return;
                
                // Launch the mini-game
                double score = MiniGameManager.LaunchGame(gameType, this);
                
                // Apply the results to the bonsai
                if (score > 0)
                {
                    currentBonsai.Play(score);
                    
                    // Additional effects based on the game type
                    switch (gameType)
                    {
                        case MiniGameManager.GameType.PestRemoval:
                            // Pest removal has greater effect if score is high
                            currentBonsai.RemovePests(score);
                            break;
                            
                        case MiniGameManager.GameType.PruningPuzzle:
                            // Pruning effect
                            currentBonsai.Prune();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to launch mini-game", ex);
            }
        }
        
        private void PestControlButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentBonsai == null) return;
                
                if (currentBonsai.PestInfestation <= 10)
                {
                    MessageBox.Show(
                        $"{currentBonsai.Name} doesn't have any significant pest problems right now.",
                        "No Pests",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                
                // Launch the pest removal game
                double score = MiniGameManager.LaunchGame(MiniGameManager.GameType.PestRemoval, this);
                
                // Apply the results
                currentBonsai.RemovePests(score);
                
                // Update UI
                UpdateEnhancedUI();
            }
            catch (Exception ex)
            {
                ShowError("Failed to start pest control", ex);
            }
        }
        
        private void TreatDiseaseButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentBonsai == null) return;
                
                if (currentBonsai.DiseaseLevel <= 10)
                {
                    MessageBox.Show(
                        $"{currentBonsai.Name} is healthy and doesn't need treatment right now.",
                        "No Disease",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                
                // Create treatment options
                using Form treatmentForm = new Form
                {
                    Text = "Treat Disease",
                    Size = new Size(400, 300),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                
                Label titleLabel = new Label
                {
                    Text = "Select Treatment Method",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    Size = new Size(360, 30),
                    Location = new Point(20, 20),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                
                Button basicTreatment = new Button
                {
                    Text = "Basic Treatment\n(60% effective)",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(160, 60),
                    Location = new Point(40, 70),
                    Tag = 60.0
                };
                
                Button advancedTreatment = new Button
                {
                    Text = "Advanced Treatment\n(85% effective)",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(160, 60),
                    Location = new Point(210, 70),
                    Tag = 85.0
                };
                
                Button naturalRemedy = new Button
                {
                    Text = "Natural Remedy\n(40% effective,\nbut no stress)",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(160, 60),
                    Location = new Point(40, 150),
                    Tag = 40.0
                };
                
                Button professionalHelp = new Button
                {
                    Text = "Professional Help\n(95% effective,\nbut expensive)",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(160, 60),
                    Location = new Point(210, 150),
                    Tag = 95.0
                };
                
                // Attach event handlers
                EventHandler treatmentClick = (s, args) =>
                {
                    if (s is Button btn && btn.Tag is double effectiveness)
                    {
                        currentBonsai.TreatDisease(effectiveness);
                        treatmentForm.Close();
                    }
                };
                
                basicTreatment.Click += treatmentClick;
                advancedTreatment.Click += treatmentClick;
                naturalRemedy.Click += treatmentClick;
                professionalHelp.Click += treatmentClick;
                
                treatmentForm.Controls.Add(titleLabel);
                treatmentForm.Controls.Add(basicTreatment);
                treatmentForm.Controls.Add(advancedTreatment);
                treatmentForm.Controls.Add(naturalRemedy);
                treatmentForm.Controls.Add(professionalHelp);
                
                treatmentForm.ShowDialog(this);
                
                // Update UI after treatment
                UpdateEnhancedUI();
            }
            catch (Exception ex)
            {
                ShowError("Failed to treat disease", ex);
            }
        }
        
        private void PlayMusicButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentBonsai == null) return;
                
                // Create music selection options
                using Form musicForm = new Form
                {
                    Text = "Play Music",
                    Size = new Size(320, 250),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                
                Label titleLabel = new Label
                {
                    Text = "Select Music Type",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    Size = new Size(280, 30),
                    Location = new Point(20, 20),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                
                Button classicalButton = new Button
                {
                    Text = "Classical",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(120, 40),
                    Location = new Point(40, 70),
                    Tag = MusicType.Classical
                };
                
                Button natureButton = new Button
                {
                    Text = "Nature Sounds",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(120, 40),
                    Location = new Point(170, 70),
                    Tag = MusicType.Nature
                };
                
                Button upbeatButton = new Button
                {
                    Text = "Upbeat",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(120, 40),
                    Location = new Point(40, 130),
                    Tag = MusicType.Upbeat
                };
                
                Button meditationButton = new Button
                {
                    Text = "Meditation",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(120, 40),
                    Location = new Point(170, 130),
                    Tag = MusicType.Meditation
                };
                
                // Attach event handlers
                EventHandler musicClick = (s, args) =>
                {
                    if (s is Button btn && btn.Tag is MusicType musicType)
                    {
                        currentBonsai.PlayMusic(musicType);
                        musicForm.Close();
                    }
                };
                
                classicalButton.Click += musicClick;
                natureButton.Click += musicClick;
                upbeatButton.Click += musicClick;
                meditationButton.Click += musicClick;
                
                musicForm.Controls.Add(titleLabel);
                musicForm.Controls.Add(classicalButton);
                musicForm.Controls.Add(natureButton);
                musicForm.Controls.Add(upbeatButton);
                musicForm.Controls.Add(meditationButton);
                
                musicForm.ShowDialog(this);
                
                // Update UI after playing music
                UpdateEnhancedUI();
            }
            catch (Exception ex)
            {
                ShowError("Failed to play music", ex);
            }
        }
        
        private void AdjustLightButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentBonsai == null) return;
                
                // Create light exposure selection options
                using Form lightForm = new Form
                {
                    Text = "Adjust Light Exposure",
                    Size = new Size(320, 220),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                
                Label titleLabel = new Label
                {
                    Text = "Select Light Exposure",
                    Font = new Font("Segoe UI", 14, FontStyle.Bold),
                    Size = new Size(280, 30),
                    Location = new Point(20, 20),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                
                Button directButton = new Button
                {
                    Text = "Direct Sunlight",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(250, 40),
                    Location = new Point(35, 60),
                    Tag = LightExposure.Direct
                };
                
                Button filteredButton = new Button
                {
                    Text = "Filtered Light",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(250, 40),
                    Location = new Point(35, 110),
                    Tag = LightExposure.Filtered
                };
                
                Button indirectButton = new Button
                {
                    Text = "Indirect Light",
                    Font = new Font("Segoe UI", 10),
                    Size = new Size(250, 40),
                    Location = new Point(35, 160),
                    Tag = LightExposure.Indirect
                };
                
                // Attach event handlers
                EventHandler lightClick = (s, args) =>
                {
                    if (s is Button btn && btn.Tag is LightExposure exposure)
                    {
                        currentBonsai.AdjustLight(exposure);
                        lightForm.Close();
                    }
                };
                
                directButton.Click += lightClick;
                filteredButton.Click += lightClick;
                indirectButton.Click += lightClick;
                
                lightForm.Controls.Add(titleLabel);
                lightForm.Controls.Add(directButton);
                lightForm.Controls.Add(filteredButton);
                lightForm.Controls.Add(indirectButton);
                
                lightForm.ShowDialog(this);
                
                // Update UI after adjusting light
                UpdateEnhancedUI();
            }
            catch (Exception ex)
            {
                ShowError("Failed to adjust light exposure", ex);
            }
        }
        
        #endregion
        
        #region Enhanced Timer and Helper Methods
        
        private void SeasonWeatherTimer_Tick(object sender, EventArgs e)
        {
            // Update season and weather display
            UpdateSeasonWeatherDisplay();
        }
        
        private Color GetMoodColor(EmotionalState mood)
        {
            return mood switch
            {
                EmotionalState.Depressed => Color.FromArgb(128, 0, 0),    // Dark red
                EmotionalState.Sad => Color.FromArgb(231, 76, 60),        // Red
                EmotionalState.Anxious => Color.FromArgb(243, 156, 18),   // Orange
                EmotionalState.Neutral => Color.FromArgb(189, 195, 199),  // Gray
                EmotionalState.Content => Color.FromArgb(52, 152, 219),   // Blue
                EmotionalState.Happy => Color.FromArgb(46,