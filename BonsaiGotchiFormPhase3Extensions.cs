using System;
using System.Drawing;
using System.Windows.Forms;

namespace BonsaiGotchi
{
    /// <summary>
    /// Extensions to the BonsaiGotchiForm for Phase 3 features
    /// </summary>
    public partial class BonsaiGotchiForm
    {
        // Phase 3 integration manager
        private Phase3IntegrationManager phase3Manager;
        
        // Phase 3 UI components
        private Button? collectionButton;
        private Button? environmentButton;
        
        /// <summary>
        /// Initialize Phase 3 features
        /// </summary>
        private void InitializePhase3()
        {
            try
            {
                // Create the integration manager
                phase3Manager = new Phase3IntegrationManager(this, currentBonsai);
                
                // Add Phase 3 UI elements
                AddPhase3Controls();
                
                // Connect the game timer to also update environment effects
                gameTimer.Tick -= GameTimer_Tick;
                gameTimer.Tick += Phase3GameTimer_Tick;
            }
            catch (Exception ex)
            {
                ShowError("Failed to initialize Phase 3 features", ex);
            }
        }
        
        /// <summary>
        /// Add Phase 3 controls to the UI
        /// </summary>
        private void AddPhase3Controls()
        {
            if (controlPanel == null) return;
            
            // Add a second row of controls
            int startY = 110;
            int buttonWidth = 120;
            int buttonHeight = 35;
            int spacing = 10;
            
            // Collection manager button
            collectionButton = new Button
            {
                Text = "Collection",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(16, startY),
                BackColor = Color.FromArgb(155, 89, 182), // Purple
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            collectionButton.FlatAppearance.BorderSize = 0;
            collectionButton.Click += CollectionButton_Click;
            
            // Environment button
            environmentButton = new Button
            {
                Text = "Environment",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(16 + buttonWidth + spacing, startY),
                BackColor = Color.FromArgb(52, 152, 219), // Blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            environmentButton.FlatAppearance.BorderSize = 0;
            environmentButton.Click += EnvironmentButton_Click;
            
            // Add buttons to control panel
            controlPanel.Controls.Add(collectionButton);
            controlPanel.Controls.Add(environmentButton);
        }
        
        /// <summary>
        /// Phase 3 enhanced game timer tick handler
        /// </summary>
        private void Phase3GameTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (currentBonsai == null) return;
                
                // Calculate elapsed time since last update
                TimeSpan elapsed = DateTime.Now - lastUpdateTime;
                lastUpdateTime = DateTime.Now;
                
                // Update bonsai basic state
                currentBonsai.Update(elapsed);
                
                // Update with environmental effects
                if (phase3Manager != null)
                {
                    phase3Manager.UpdateBonsaiWithEnvironment(currentBonsai, elapsed);
                }
                
                // Check if tree appearance needs updating due to status changes
                if (currentBonsai.IsSick || currentBonsai.IsDead)
                {
                    _ = GenerateTreeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Phase 3 game timer error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Collection button click handler
        /// </summary>
        private void CollectionButton_Click(object sender, EventArgs e)
        {
            if (phase3Manager == null) return;
            
            try
            {
                phase3Manager.ShowCollectionManager();
            }
            catch (Exception ex)
            {
                ShowError("Failed to open collection manager", ex);
            }
        }
        
        /// <summary>
        /// Environment button click handler
        /// </summary>
        private void EnvironmentButton_Click(object sender, EventArgs e)
        {
            if (phase3Manager == null) return;
            
            try
            {
                phase3Manager.ShowEnvironmentMonitor();
            }
            catch (Exception ex)
            {
                ShowError("Failed to open environment monitor", ex);
            }
        }
        
        /// <summary>
        /// Enhanced save method that also saves Phase 3 data
        /// </summary>
        private void SaveWithPhase3Data(string filePath)
        {
            try
            {
                if (currentBonsai == null)
                {
                    MessageBox.Show("No bonsai to save. Please adopt a bonsai first.", "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Save the bonsai
                SetUIGenerating(true);
                currentBonsai.SaveToFile(filePath);
                
                // Also save Phase 3 data
                if (phase3Manager != null)
                {
                    string basePath = Path.GetDirectoryName(filePath);
                    _ = phase3Manager.SaveDataAsync(basePath);
                }
                
                SetUIGenerating(false);

                MessageBox.Show($"Your bonsai {currentBonsai.Name} saved successfully!",
                    "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetUIGenerating(false);
                ShowError("Failed to save bonsai", ex);
            }
        }
        
        /// <summary>
        /// Enhanced load method that also loads Phase 3 data
        /// </summary>
        private async Task LoadWithPhase3DataAsync(string filePath)
        {
            try
            {
                SetUIGenerating(true);
                
                try
                {
                    // Load the bonsai data
                    currentBonsai = BonsaiPet.LoadFromFile(filePath);
                    
                    // Hook up event handlers
                    currentBonsai.StatsChanged += CurrentBonsai_StatsChanged;
                    currentBonsai.NotificationTriggered += CurrentBonsai_NotificationTriggered;
                    currentBonsai.StageAdvanced += CurrentBonsai_StageAdvanced;
                    
                    // Reset lastUpdateTime to prevent time jump
                    lastUpdateTime = DateTime.Now;
                    
                    // Also load Phase 3 data
                    if (phase3Manager != null)
                    {
                        string basePath = Path.GetDirectoryName(filePath);
                        await phase3Manager.LoadDataAsync(basePath);
                        
                        // Update current bonsai
                        phase3Manager.SetActiveBonsai(currentBonsai);
                    }
                    
                    // Update the UI
                    UpdateStatsDisplay();
                    UpdateNeedsDisplay();
                    UpdateEnhancedUI(); // Use the enhanced UI update if available
                    await GenerateTreeAsync();
                    
                    // Clear notifications
                    notificationListView?.Items.Clear();
                    
                    // Add welcome back notification
                    AddNotificationToListView(new BonsaiNotification(
                        "Welcome Back!", 
                        $"Your bonsai {currentBonsai.Name} has been loaded successfully!",
                        NotificationSeverity.Information));
                    
                    // Enable or disable buttons based on bonsai status
                    bool enableButtons = !currentBonsai.IsDead;
                    waterButton.Enabled = enableButtons;
                    feedButton.Enabled = enableButtons;
                    pruneButton.Enabled = enableButtons;
                    repotButton.Enabled = enableButtons;
                    playButton.Enabled = enableButtons;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load bonsai: {ex.Message}",
                        "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetUIGenerating(false);
                }
            }
            catch (Exception ex)
            {
                SetUIGenerating(false);
                ShowError("Failed to load bonsai", ex);
            }
        }
        
        /// <summary>
        /// Override the default dispose to clean up Phase 3 resources
        /// </summary>
        protected override void DisposeWithPhase3(bool disposing)
        {
            if (!disposed && disposing)
            {
                // Dispose Phase 3 resources
                phase3Manager?.Dispose();
                
                // Call the original dispose logic
                ReleaseFormResources();
            }
            
            base.Dispose(disposing);
        }
    }
}