using System;
using System.Windows.Forms;

namespace BonsaiGotchi
{
    /// <summary>
    /// Partial class for main form initialization with enhanced features
    /// </summary>
    public partial class BonsaiGotchiForm
    {
        /// <summary>
        /// Initialize the form with both basic and enhanced features
        /// </summary>
        private void InitializeWithEnhancements()
        {
            try
            {
                // Standard initialization
                treeGenerator = new BonsaiTreeGenerator.BonsaiTreeGenerator(random);
                colorMapping = BonsaiTreeGenerator.BonsaiTree.GetColorMapping();

                InitializeComponent();
                SetupUserInterface();

                // Enable double buffering for smooth rendering
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                        ControlStyles.UserPaint |
                        ControlStyles.DoubleBuffer |
                        ControlStyles.ResizeRedraw |
                        ControlStyles.OptimizedDoubleBuffer, true);

                // Reduce flicker during updates
                typeof(Control).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null, this, [true]);

                // Add enhanced functionality
                SetupEnhancedFunctionality();
                
                // Initialize lastUpdateTime for game timer
                lastUpdateTime = DateTime.Now;
                
                // Start timers
                InitializeTimers();
            }
            catch (Exception ex)
            {
                ShowError("Failed to initialize BonsaiGotchi", ex);
            }
        }
        
        /// <summary>
        /// Modified version of the game timer initialization that uses enhanced updates
        /// </summary>
        private void InitializeEnhancedTimers()
        {
            // Game timer - updates bonsai state (real-time)
            gameTimer?.Dispose();
            gameTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // Update bonsai state every 5 seconds
            };
            gameTimer.Tick += EnhancedGameTimer_Tick;
            gameTimer.Start();

            // UI update timer - refreshes UI more frequently
            uiUpdateTimer?.Dispose();
            uiUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // Update UI every second
            };
            uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            uiUpdateTimer.Start();
        }
        
        /// <summary>
        /// Modified version of OnLoad that initializes enhanced features
        /// </summary>
        protected override void OnLoadWithEnhancements(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                isFormReady = true;
                
                // Create a welcome notification
                if (notificationListView != null && currentBonsai != null)
                {
                    AddNotificationToListView(new BonsaiNotification(
                        "Welcome to BonsaiGotchi!", 
                        $"Your bonsai {currentBonsai.Name} is ready for care and attention.", 
                        NotificationSeverity.Information));
                }
                
                _ = GenerateTreeAsync();
            }
            catch (Exception ex)
            {
                ShowError("Failed to load enhanced bonsai", ex);
            }
        }
    }
}