using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BonsaiGotchi.BreedingSystem;
using BonsaiGotchi.EnvironmentSystem;
using BonsaiGotchi.MiniGames;
using BonsaiGotchi.EnvironmentUI;
using BonsaiGotchi.CollectionUI;

namespace BonsaiGotchi
{
    public partial class BonsaiGotchiForm : Form
    {
        #region Fields

        // Tree generation
        private readonly BonsaiTreeGenerator.BonsaiTreeGenerator treeGenerator;
        private readonly Dictionary<string, Color> colorMapping;
        private BonsaiTreeGenerator.BonsaiTree currentTree;
        private bool isGenerating = false;
        private readonly Random random = new Random();

        // Core bonsai pet
        private BonsaiPet currentBonsai;

        // UI components
        private Panel controlPanel;
        private Panel needsPanel;
        private Panel notificationPanel;
        private RichTextBox treeDisplay;
        private ListView notificationListView;
        private Button waterButton;
        private Button feedButton;
        private Button pruneButton;
        private Button repotButton;
        private Button playButton;
        
        // Phase 2-3 buttons
        private Button playMiniGameButton;
        private Button pestControlButton;
        private Button treatDiseaseButton;
        private Button playMusicButton;
        private Button adjustLightButton;
        private Button collectionButton;
        private Button environmentButton;

        // Update tracking
        private DateTime lastUpdateTime;
        private Timer gameTimer;
        private Timer uiUpdateTimer;
        private bool isFormReady = false;
        private bool disposed = false;

        // Constants
        private const int TREE_WIDTH = 60;
        private const int TREE_HEIGHT = 30;
        
        // Integration manager
        private BonsaiGotchiIntegration integration;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the BonsaiGotchi form
        /// </summary>
        public BonsaiGotchiForm()
        {
            // Initialize the tree generator and color mapping
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
                
            // Initialize the integration manager
            integration = new BonsaiGotchiIntegration(this);

            // Initialize lastUpdateTime for game timer
            lastUpdateTime = DateTime.Now;
            
            // Create a new bonsai or load an existing one
            CreateNewBonsai("Bonsy");
            
            // Initialize timers
            InitializeTimers();
        }
        
        /// <summary>
        /// Set up the user interface
        /// </summary>
        private void SetupUserInterface()
        {
            // Create main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // Set column styles
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            // Create left panel (needs display)
            needsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // Create center panel (tree display)
            Panel treePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // Create tree display
            treeDisplay = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10F, FontStyle.Regular),
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None
            };
            treePanel.Controls.Add(treeDisplay);

            // Create right panel (notifications)
            notificationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // Create control panel at the bottom
            controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(240, 242, 245)
            };
            
            // Setup controls in control panel
            SetupControlPanel();

            // Setup needs display
            SetupNeedsPanel();

            // Setup notification panel
            SetupNotificationPanel();

            // Add panels to main layout
            mainLayout.Controls.Add(needsPanel, 0, 0);
            mainLayout.Controls.Add(treePanel, 1, 0);
            mainLayout.Controls.Add(notificationPanel, 2, 0);

            // Add main layout and control panel to form
            Controls.Add(controlPanel);
            Controls.Add(mainLayout);
        }
        
        /// <summary>
        /// Set up the control panel with buttons
        /// </summary>
        private void SetupControlPanel()
        {
            // Basic care buttons (Phase 1)
            waterButton = CreateButton("Water", 16, 20, Color.FromArgb(52, 152, 219));
            feedButton = CreateButton("Feed", 146, 20, Color.FromArgb(46, 204, 113));
            pruneButton = CreateButton("Prune", 276, 20, Color.FromArgb(230, 126, 34));
            repotButton = CreateButton("Repot", 406, 20, Color.FromArgb(155, 89, 182));
            playButton = CreateButton("Play", 536, 20, Color.FromArgb(52, 73, 94));

            waterButton.Click += WaterButton_Click;
            feedButton.Click += FeedButton_Click;
            pruneButton.Click += PruneButton_Click;
            repotButton.Click += RepotButton_Click;
            playButton.Click += PlayButton_Click;
            
            // Add time control panel
            SetupTimeControls();
            
            // Add enhanced care buttons (Phase 2)
            SetupEnhancedCareButtons();
            
            // Add advanced feature buttons (Phase 3)
            SetupAdvancedFeatureButtons();
            
            // Add menu strip
            SetupMenuStrip();
        }
        
        /// <summary>
        /// Set up time control buttons
        /// </summary>
        private void SetupTimeControls()
        {
            // Time controls
            GroupBox timeControlBox = new GroupBox
            {
                Text = "Time Speed",
                Location = new Point(666, 10),
                Size = new Size(300, 60),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };

            Button normalSpeedButton = new Button
            {
                Text = "1×",
                Size = new Size(60, 28),
                Location = new Point(15, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Tag = 1.0
            };
            normalSpeedButton.FlatAppearance.BorderSize = 0;

            Button fastSpeedButton = new Button
            {
                Text = "2×",
                Size = new Size(60, 28),
                Location = new Point(85, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Tag = 2.0
            };
            fastSpeedButton.FlatAppearance.BorderSize = 0;

            Button fasterSpeedButton = new Button
            {
                Text = "5×",
                Size = new Size(60, 28),
                Location = new Point(155, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Tag = 5.0
            };
            fasterSpeedButton.FlatAppearance.BorderSize = 0;

            Button fastestSpeedButton = new Button
            {
                Text = "10×",
                Size = new Size(60, 28),
                Location = new Point(225, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Tag = 10.0
            };
            fastestSpeedButton.FlatAppearance.BorderSize = 0;

            // Add event handlers
            EventHandler speedButtonClick = (s, e) =>
            {
                if (s is Button button && button.Tag is double speed && currentBonsai != null)
                {
                    currentBonsai.SetTimeMultiplier(speed);
                    integration.EnvironmentManager.SetTimeMultiplier(speed);
                }
            };

            normalSpeedButton.Click += speedButtonClick;
            fastSpeedButton.Click += speedButtonClick;
            fasterSpeedButton.Click += speedButtonClick;
            fastestSpeedButton.Click += speedButtonClick;

            // Add controls to group box
            timeControlBox.Controls.Add(normalSpeedButton);
            timeControlBox.Controls.Add(fastSpeedButton);
            timeControlBox.Controls.Add(fasterSpeedButton);
            timeControlBox.Controls.Add(fastestSpeedButton);

            // Add group box to control panel
            controlPanel.Controls.Add(timeControlBox);
        }
        
        /// <summary>
        /// Set up enhanced care buttons (Phase 2)
        /// </summary>
        private void SetupEnhancedCareButtons()
        {
            // Calculate positions for enhanced care buttons
            int startY = 65;
            int buttonWidth = 120;
            int buttonHeight = 35;
            int spacing = 10;
            
            // Mini-games button
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
            
            // Add buttons to control panel
            controlPanel.Controls.Add(playMiniGameButton);
            controlPanel.Controls.Add(pestControlButton);
            controlPanel.Controls.Add(treatDiseaseButton);
            controlPanel.Controls.Add(playMusicButton);
        }
        
        /// <summary>
        /// Set up advanced feature buttons (Phase 3)
        /// </summary>
        private void SetupAdvancedFeatureButtons()
        {
            // Calculate positions for advanced features buttons
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
        /// Set up the menu strip
        /// </summary>
        private void SetupMenuStrip()
        {
            MenuStrip menuStrip = new MenuStrip();
            
            // File menu
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("New Bonsai", null, (s, e) => CreateNewBonsaiWithDialog());
            fileMenu.DropDownItems.Add("Save", null, (s, e) => SaveBonsai());
            fileMenu.DropDownItems.Add("Load", null, (s, e) => LoadBonsai());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());
            
            // Help menu
            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");
            helpMenu.DropDownItems.Add("About", null, (s, e) => ShowAboutDialog());
            
            // Add menus to menu strip
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(helpMenu);
            
            // Add menu strip to form
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }
        
        /// <summary>
        /// Create a button with standard styling
        /// </summary>
        private Button CreateButton(string text, int x, int y, Color color)
        {
            Button button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(120, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            controlPanel.Controls.Add(button);
            return button;
        }
        
        /// <summary>
        /// Set up the needs panel with stat displays
        /// </summary>
        private void SetupNeedsPanel()
        {
            // Create title
            Label titleLabel = new Label
            {
                Text = "Bonsai Stats",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Create flow layout for stats
            FlowLayoutPanel statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            // Add basic stats (Phase 1)
            AddStatDisplay(statsPanel, "Name:", "Bonsy", "nameLabel");
            AddStatDisplay(statsPanel, "Age:", "0 days", "ageLabel");
            AddStatDisplay(statsPanel, "Stage:", "Seedling", "stageLabel");
            AddProgressBar(statsPanel, "Health:", 100, "healthBar", Color.FromArgb(231, 76, 60));
            AddProgressBar(statsPanel, "Happiness:", 100, "happinessBar", Color.FromArgb(46, 204, 113));
            AddProgressBar(statsPanel, "Hunger:", 0, "hungerBar", Color.FromArgb(230, 126, 34));
            AddProgressBar(statsPanel, "Growth:", 0, "growthBar", Color.FromArgb(52, 152, 219));
            
            // Add enhanced stats (Phase 2)
            AddProgressBar(statsPanel, "Hydration:", 100, "hydrationBar", Color.FromArgb(52, 152, 219));
            AddProgressBar(statsPanel, "Soil Quality:", 100, "soilQualityBar", Color.FromArgb(155, 89, 182));
            AddProgressBar(statsPanel, "Stress:", 0, "stressBar", Color.FromArgb(231, 76, 60));
            AddProgressBar(statsPanel, "Pest Level:", 0, "pestBar", Color.FromArgb(241, 196, 15));
            AddProgressBar(statsPanel, "Disease:", 0, "diseaseBar", Color.FromArgb(142, 68, 173));
            
            // Add environmental stats (Phase 3)
            AddStatDisplay(statsPanel, "Season:", "Spring", "seasonLabel");
            AddStatDisplay(statsPanel, "Weather:", "Sunny", "weatherLabel");
            AddStatDisplay(statsPanel, "Time of Day:", "Day", "timeOfDayLabel");

            // Add controls to panel
            needsPanel.Controls.Add(statsPanel);
            needsPanel.Controls.Add(titleLabel);
        }
        
        /// <summary>
        /// Add a stat display to the panel
        /// </summary>
        private void AddStatDisplay(Control parent, string labelText, string valueText, string valueName)
        {
            // Create panel
            Panel statPanel = new Panel
            {
                Width = parent.Width - 20,
                Height = 25,
                Margin = new Padding(0, 5, 0, 0)
            };

            // Create label
            Label label = new Label
            {
                Text = labelText,
                Location = new Point(0, 0),
                Size = new Size(100, 25),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // Create value label
            Label value = new Label
            {
                Text = valueText,
                Location = new Point(100, 0),
                Size = new Size(parent.Width - 120, 25),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F),
                Name = valueName
            };

            // Add to panel
            statPanel.Controls.Add(label);
            statPanel.Controls.Add(value);
            parent.Controls.Add(statPanel);
        }
        
        /// <summary>
        /// Add a progress bar stat to the panel
        /// </summary>
        private void AddProgressBar(Control parent, string labelText, int value, string barName, Color barColor)
        {
            // Create panel
            Panel statPanel = new Panel
            {
                Width = parent.Width - 20,
                Height = 45,
                Margin = new Padding(0, 5, 0, 0)
            };

            // Create label
            Label label = new Label
            {
                Text = labelText,
                Location = new Point(0, 0),
                Size = new Size(parent.Width - 20, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // Create progress bar
            ProgressBar bar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = value,
                Location = new Point(0, 25),
                Size = new Size(parent.Width - 40, 20),
                Name = barName,
                Style = ProgressBarStyle.Continuous
            };

            // Set bar color using reflection (simplified version)
            bar.ForeColor = barColor;
            
            // Add to panel
            statPanel.Controls.Add(label);
            statPanel.Controls.Add(bar);
            parent.Controls.Add(statPanel);
        }
        
        /// <summary>
        /// Set up the notification panel
        /// </summary>
        private void SetupNotificationPanel()
        {
            // Create title
            Label titleLabel = new Label
            {
                Text = "Notifications",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Create notification list
            notificationListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };

            // Add columns
            notificationListView.Columns.Add("Time", 70);
            notificationListView.Columns.Add("Message", 180);

            // Add to panel
            notificationPanel.Controls.Add(notificationListView);
            notificationPanel.Controls.Add(titleLabel);
        }
        
        /// <summary>
        /// Initialize the timers
        /// </summary>
        private void InitializeTimers()
        {
            // Game timer - updates bonsai state (real-time)
            gameTimer = new Timer
            {
                Interval = 5000 // Update bonsai state every 5 seconds
            };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            // UI update timer - refreshes UI more frequently
            uiUpdateTimer = new Timer
            {
                Interval = 1000 // Update UI every second
            };
            uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            uiUpdateTimer.Start();
        }

        #endregion

        #region Tree Generation

        /// <summary>
        /// Generate a new bonsai tree asynchronously
        /// </summary>
        private async Task GenerateTreeAsync()
        {
            if (isGenerating || !isFormReady || currentBonsai == null) return;

            try
            {
                isGenerating = true;

                // Generate tree based on bonsai properties
                BonsaiTreeGenerator.GenerationOptions options = GetGenerationOptionsFromBonsai();
                
                // Run tree generation in a background task
                var tree = await Task.Run(() => treeGenerator.GenerateTree(options));

                // Update the current tree
                currentTree = tree;

                // Apply special effects based on bonsai condition
                if (currentBonsai.IsSick)
                {
                    ModifyTreeForSickness();
                }
                else if (currentBonsai.IsDead)
                {
                    ModifyTreeForDeath();
                }

                // Apply environmental effects to tree appearance
                ApplyEnvironmentalEffects();

                // Display the tree
                DisplayTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating tree: {ex.Message}",
                    "Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isGenerating = false;
            }
        }
        
        /// <summary>
        /// Get generation options based on current bonsai state
        /// </summary>
        private BonsaiTreeGenerator.GenerationOptions GetGenerationOptionsFromBonsai()
        {
            // Default options
            var options = new BonsaiTreeGenerator.GenerationOptions();
            
            if (currentBonsai != null)
            {
                // Set tree seed for consistent generation
                options.Seed = currentBonsai.TreeSeed;
                
                // Adjust options based on bonsai stage
                switch (currentBonsai.CurrentStage)
                {
                    case GrowthStage.Seedling:
                        options.TreeHeight = 8;
                        options.TrunkWidth = 1;
                        options.BranchCount = 2;
                        options.LeafDensity = 0.3;
                        break;
                    case GrowthStage.Sapling:
                        options.TreeHeight = 12;
                        options.TrunkWidth = 2;
                        options.BranchCount = 4;
                        options.LeafDensity = 0.5;
                        break;
                    case GrowthStage.YoungTree:
                        options.TreeHeight = 16;
                        options.TrunkWidth = 3;
                        options.BranchCount = 6;
                        options.LeafDensity = 0.7;
                        break;
                    case GrowthStage.MatureTree:
                        options.TreeHeight = 20;
                        options.TrunkWidth = 4;
                        options.BranchCount = 8;
                        options.LeafDensity = 0.85;
                        break;
                    case GrowthStage.ElderTree:
                        options.TreeHeight = 24;
                        options.TrunkWidth = 5;
                        options.BranchCount = 10;
                        options.LeafDensity = 1.0;
                        break;
                }
                
                // Adjust options based on bonsai style
                switch (currentBonsai.Style)
                {
                    case BonsaiStyle.FormalUpright:
                        options.TreeTilt = 0;
                        options.TrunkCurve = 0.2;
                        options.BranchSymmetry = 0.8;
                        break;
                    case BonsaiStyle.InformalUpright:
                        options.TreeTilt = 0.1;
                        options.TrunkCurve = 0.5;
                        options.BranchSymmetry = 0.6;
                        break;
                    case BonsaiStyle.Windswept:
                        options.TreeTilt = 0.3;
                        options.TrunkCurve = 0.7;
                        options.BranchSymmetry = 0.2;
                        options.WindEffect = 0.8;
                        break;
                    case BonsaiStyle.Cascade:
                        options.TreeTilt = -0.5;
                        options.TrunkCurve = 0.9;
                        options.BranchSymmetry = 0.4;
                        options.GrowthDirection = -0.5;
                        break;
                    case BonsaiStyle.Slanting:
                        options.TreeTilt = 0.4;
                        options.TrunkCurve = 0.3;
                        options.BranchSymmetry = 0.5;
                        break;
                }
                
                // Scale based on growth percentage
                double growthFactor = currentBonsai.Growth / 100.0;
                options.TreeHeight = (int)(options.TreeHeight * (0.7 + (0.3 * growthFactor)));
                options.BranchCount = (int)(options.BranchCount * (0.7 + (0.3 * growthFactor)));
                options.LeafDensity *= (0.7 + (0.3 * growthFactor));
                
                // Health affects overall appearance
                double healthFactor = currentBonsai.Health / 100.0;
                options.LeafDensity *= healthFactor;
                options.LeafQuality = healthFactor;
            }
            
            return options;
        }
        
        /// <summary>
        /// Modify tree appearance for sick bonsai
        /// </summary>
        private void ModifyTreeForSickness()
        {
            if (currentTree == null) return;
            
            // Make some leaves yellow/brown to show sickness
            for (int y = 0; y < currentTree.Height; y++)
            {                for (int x = 0; x < currentTree.Width; x++)
                {
                    // Only modify leaf characters
                    if ("●○◆◇◈◉◊⬢⬡⬟⬠⬣".Contains(currentTree.GetCharAt(x, y)))
                    {
                        // 50% chance to change leaf color to yellow/brown
                        if (random.NextDouble() < 0.5)
                        {
                            // Yellow/brown colors for sick leaves
                            Color sickLeafColor = random.NextDouble() < 0.5 ?
                                Color.FromArgb(205, 133, 63) : // Brown
                                Color.FromArgb(218, 165, 32);  // Golden yellow
                                
                            currentTree.SetColorAt(x, y, sickLeafColor);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Modify tree appearance for dead bonsai
        /// </summary>
        private void ModifyTreeForDeath()
        {
            if (currentTree == null) return;
            
            // Make tree appear dead - brown/gray leaves and trunk
            for (int y = 0; y < currentTree.Height; y++)
            {
                for (int x = 0; x < currentTree.Width; x++)
                {
                    char c = currentTree.GetCharAt(x, y);
                    
                    // Dead leaves (brown/black)
                    if ("●○◆◇◈◉◊⬢⬡⬟⬠⬣".Contains(c))
                    {
                        Color deadLeafColor = random.NextDouble() < 0.7 ?
                            Color.FromArgb(101, 67, 33) : // Dark brown
                            Color.FromArgb(64, 64, 64);   // Dark gray
                            
                        currentTree.SetColorAt(x, y, deadLeafColor);
                    }
                    // Dead trunk/branches (gray)
                    else if ("║╣╠╦╩╬╧╨╤╥┃┏┓┗┛┣┫┳┻".Contains(c))
                    {
                        currentTree.SetColorAt(x, y, Color.FromArgb(105, 105, 105)); // Dim gray
                    }
                }
            }
            
            // Remove some leaves to make it look more sparse
            for (int y = 0; y < currentTree.Height; y++)
            {
                for (int x = 0; x < currentTree.Width; x++)
                {
                    if ("●○◆◇◈◉◊⬢⬡⬟⬠⬣".Contains(currentTree.GetCharAt(x, y)))
                    {
                        // 70% chance to remove leaf
                        if (random.NextDouble() < 0.7)
                        {
                            currentTree.SetCharAt(x, y, ' ');
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply environmental effects to the tree appearance
        /// </summary>
        private void ApplyEnvironmentalEffects()
        {
            if (currentTree == null || integration?.EnvironmentManager == null) return;
            
            // Apply seasonal effects
            ApplySeasonalAppearance(integration.EnvironmentManager.CurrentSeason);
            
            // Apply weather effects
            ApplyWeatherAppearance(integration.EnvironmentManager.CurrentWeather);
            
            // Apply time of day effects
            ApplyTimeOfDayAppearance(integration.EnvironmentManager.CurrentTimeOfDay);
        }
        
        /// <summary>
        /// Apply seasonal appearance changes
        /// </summary>
        private void ApplySeasonalAppearance(Season season)
        {
            if (currentTree == null) return;
            
            switch (season)
            {
                case Season.Spring:
                    // Spring: bright green leaves, some blossoms
                    ApplySpringAppearance();
                    break;
                case Season.Summer:
                    // Summer: deep green leaves
                    ApplySummerAppearance();
                    break;
                case Season.Autumn:
                    // Autumn: red/orange/yellow leaves
                    ApplyAutumnAppearance();
                    break;
                case Season.Winter:
                    // Winter: sparse leaves, maybe snow
                    ApplyWinterAppearance();
                    break;
            }
        }
        
        /// <summary>
        /// Apply spring appearance to the tree
        /// </summary>
        private void ApplySpringAppearance()
        {
            for (int y = 0; y < currentTree.Height; y++)
            {
                for (int x = 0; x < currentTree.Width; x++)
                {
                    // Only modify leaf characters
                    if ("●○◆◇◈◉◊⬢⬡⬟⬠⬣".Contains(currentTree.GetCharAt(x, y)))
                    {
                        // Bright green for spring leaves
                        Color springLeafColor = Color.FromArgb(0, 180, 0); // Bright green
                        
                        // 10% chance for blossoms
                        if (random.NextDouble() < 0.1)
                        {
                            springLeafColor = Color.FromArgb(255, 182, 193); // Pink blossom
                        }
                        
                        currentTree.SetColorAt(x, y, springLeafColor);
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply summer appearance to the tree
        /// </summary>
        private void ApplySummerAppearance()
        {
            for (int y = 0; y < currentTree.Height; y++)
            {
                for (int x = 0; x < currentTree.Width; x++)
                {
                    // Only modify leaf characters
                    if ("●○◆◇◈◉◊⬢⬡⬟⬠⬣".Contains(currentTree.GetCharAt(x, y)))
                    {
                        // Deep green for summer leaves
                        Color summerLeafColor = Color.FromArgb(34, 139, 34); // Forest green
                        currentTree.SetColorAt(x, y, summerLeafColor);
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply autumn appearance to the tree
        /// </summary>
        private void ApplyAutumnAppearance()
        {
            for (int y = 0; y < currentTree.Height; y++)
            {
                for (int x = 0; x < currentTree.Width; x++)
                {
                    // Only modify leaf characters
                    if ("●○◆◇◈◉◊⬢⬡⬟⬠⬣".Contains(currentTree.GetCharAt(x, y)))
                    {
                        // Random autumn colors
                        Color[] autumnColors = {
                            Color.FromArgb(178, 34, 34),   // Firebrick red
                            Color.FromArgb(255, 140, 0),   // Dark orange
                            Color.FromArgb(255, 215, 0),   // Gold
                            Color.FromArgb(139, 69, 19)    // Saddle brown
                        };
                        
                        Color autumnLeafColor = autumnColors[random.Next(autumnColors.Length)];
                        currentTree.SetColorAt(x, y, autumnLeafColor);
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply winter appearance to the tree
        /// </summary>
        private void ApplyWinterAppearance()
        {
            // First, remove many leaves to create sparse appearance
            for (int y = 0; y < currentTree.Height; y++)
            {
                for (int x = 0; x < currentTree.Width; x++)
                {
                    if ("●○◆◇◈◉◊⬢⬡⬟⬠⬣".Contains(currentTree.GetCharAt(x, y)))
                    {
                        // 70% chance to remove leaf
                        if (random.NextDouble() < 0.7)
                        {
                            currentTree.SetCharAt(x, y, ' ');
                        }
                        else
                        {
                            // Remaining leaves are brown/dark green
                            Color winterLeafColor = random.NextDouble() < 0.5 ?
                                Color.FromArgb(0, 100, 0) :      // Dark green
                                Color.FromArgb(139, 69, 19);     // Saddle brown
                                
                            currentTree.SetColorAt(x, y, winterLeafColor);
                        }
                    }
                }
            }
            
            // Add snow effect if weather is snow
            if (integration?.EnvironmentManager?.CurrentWeather == Weather.Snow)
            {
                ApplySnowEffect();
            }
        }
        
        /// <summary>
        /// Apply snow effect to the tree
        /// </summary>
        private void ApplySnowEffect()
        {
            // Add snow on top of branches and trunk
            bool[] columnHasSnow = new bool[currentTree.Width];
            
            // First pass: mark columns that have tree parts
            for (int x = 0; x < currentTree.Width; x++)
            {
                columnHasSnow[x] = false;
                for (int y = 0; y < currentTree.Height; y++)
                {
                    char c = currentTree.GetCharAt(x, y);
                    if (c != ' ' && c != '\n' && c != '\r')
                    {
                        columnHasSnow[x] = true;
                        break;
                    }
                }
            }
            
            // Second pass: add snow on top parts
            for (int x = 0; x < currentTree.Width; x++)
            {
                if (columnHasSnow[x])
                {
                    for (int y = 0; y < currentTree.Height; y++)
                    {
                        char c = currentTree.GetCharAt(x, y);
                        if (c != ' ' && c != '\n' && c != '\r')
                        {
                            // Add snow on this position and stop checking this column
                            currentTree.SetCharAt(x, y, '❄');
                            currentTree.SetColorAt(x, y, Color.White);
                            break;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Apply weather appearance to the tree
        /// </summary>
        private void ApplyWeatherAppearance(Weather weather)
        {
            switch (weather)
            {
                case Weather.Rain:
                    ApplyRainEffect();
                    break;
                case Weather.Wind:
                    ApplyWindEffect();
                    break;
                case Weather.Storm:
                    ApplyStormEffect();
                    break;
                // Other weather conditions don't need special effects
            }
        }
        
        /// <summary>
        /// Apply rain effect to the tree display
        /// </summary>
        private void ApplyRainEffect()
        {
            // Add rain drops in random positions
            int raindrops = 20;
            for (int i = 0; i < raindrops; i++)
            {
                int x = random.Next(currentTree.Width);
                int y = random.Next(currentTree.Height / 2); // Rain mostly at top
                
                // Only add rain where there's empty space
                if (currentTree.GetCharAt(x, y) == ' ')
                {
                    currentTree.SetCharAt(x, y, '|');
                    currentTree.SetColorAt(x, y, Color.LightBlue);
                }
            }
        }
        
        /// <summary>
        /// Apply wind effect to the tree
        /// </summary>
        private void ApplyWindEffect()
        {
            // Shift some leaves to simulate wind
            for (int y = 0; y < currentTree.Height; y++)
            {
                for (int x = 0; x < currentTree.Width - 1; x++)
                {
                    // Only affect leaves
                    if ("●○◆◇◈◉◊⬢⬡⬟⬠⬣".Contains(currentTree.GetCharAt(x, y)))
                    {
                        // 30% chance to shift leaf
                        if (random.NextDouble() < 0.3 && currentTree.GetCharAt(x + 1, y) == ' ')
                        {
                            char leaf = currentTree.GetCharAt(x, y);
                            Color leafColor = currentTree.GetColorAt(x, y);
                            
                            currentTree.SetCharAt(x, y, ' ');
                            currentTree.SetCharAt(x + 1, y, leaf);
                            currentTree.SetColorAt(x + 1, y, leafColor);
                        }
                    }
                }
            }
            
            // Add some wind symbols
            int windSymbols = 15;
            for (int i = 0; i < windSymbols; i++)
            {
                int x = random.Next(currentTree.Width);
                int y = random.Next(currentTree.Height);
                
                // Only add wind where there's empty space
                if (currentTree.GetCharAt(x, y) == ' ')
                {
                    currentTree.SetCharAt(x, y, '~');
                    currentTree.SetColorAt(x, y, Color.LightGray);
                }
            }
        }
        
        /// <summary>
        /// Apply storm effect to the tree
        /// </summary>
        private void ApplyStormEffect()
        {
            // Apply rain effect first
            ApplyRainEffect();
            
            // Apply wind effect
            ApplyWindEffect();
            
            // Add lightning bolt
            int lightningX = random.Next(currentTree.Width / 4, 3 * currentTree.Width / 4);
            
            for (int y = 0; y < currentTree.Height / 2; y++)
            {
                // Only add lightning where there's empty space
                if (currentTree.GetCharAt(lightningX, y) == ' ')
                {
                    currentTree.SetCharAt(lightningX, y, '/');
                    currentTree.SetColorAt(lightningX, y, Color.Yellow);
                }
                
                // Zig-zag pattern
                lightningX += random.Next(-1, 2);
                lightningX = Math.Max(0, Math.Min(currentTree.Width - 1, lightningX));
            }
        }
        
        /// <summary>
        /// Apply time of day appearance to the tree
        /// </summary>
        private void ApplyTimeOfDayAppearance(TimeOfDay timeOfDay)
        {
            // The rich text box background is already white, so no need to change for day
            // For other times, we'll adjust the background color of the text box
            
            Color bgColor = timeOfDay switch
            {
                TimeOfDay.Morning => Color.FromArgb(255, 248, 220),  // Light cream for morning
                TimeOfDay.Day => Color.White,                         // White for day
                TimeOfDay.Evening => Color.FromArgb(255, 222, 173),   // Light orange for evening
                TimeOfDay.Night => Color.FromArgb(25, 25, 112),       // Midnight blue for night
                _ => Color.White
            };
            
            // Update tree display background color
            treeDisplay.BackColor = bgColor;
            
            // For night time, make the text brighter
            if (timeOfDay == TimeOfDay.Night)
            {
                // Lighten all colors for visibility at night
                for (int y = 0; y < currentTree.Height; y++)
                {
                    for (int x = 0; x < currentTree.Width; x++)
                    {
                        char c = currentTree.GetCharAt(x, y);
                        if (c != ' ' && c != '\n' && c != '\r')
                        {
                            Color original = currentTree.GetColorAt(x, y);
                            Color lighter = LightenColor(original, 0.3f);
                            currentTree.SetColorAt(x, y, lighter);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Lighten a color by the specified amount
        /// </summary>
        private Color LightenColor(Color color, float amount)
        {
            return Color.FromArgb(
                color.A,
                (int)Math.Min(255, color.R + 255 * amount),
                (int)Math.Min(255, color.G + 255 * amount),
                (int)Math.Min(255, color.B + 255 * amount)
            );
        }
        
        /// <summary>
        /// Display the tree in the richtext box
        /// </summary>
        private void DisplayTree()
        {
            if (currentTree == null || treeDisplay == null) return;

            SafeInvoke(() =>
            {
                // Clear existing content
                treeDisplay.Clear();

                // Create a string builder for the tree text
                System.Text.StringBuilder treeText = new System.Text.StringBuilder();

                // Build the tree text
                for (int y = 0; y < currentTree.Height; y++)
                {
                    for (int x = 0; x < currentTree.Width; x++)
                    {
                        treeText.Append(currentTree.GetCharAt(x, y));
                    }
                    treeText.AppendLine();
                }

                // Set the text
                treeDisplay.Text = treeText.ToString();

                // Apply colors
                for (int y = 0; y < currentTree.Height; y++)
                {
                    for (int x = 0; x < currentTree.Width; x++)
                    {
                        char c = currentTree.GetCharAt(x, y);
                        if (c != ' ' && c != '\n' && c != '\r')
                        {
                            int position = y * (currentTree.Width + 1) + x; // +1 for newline
                            treeDisplay.SelectionStart = position;
                            treeDisplay.SelectionLength = 1;
                            treeDisplay.SelectionColor = currentTree.GetColorAt(x, y);
                        }
                    }
                }

                // Reset selection
                treeDisplay.SelectionStart = 0;
                treeDisplay.SelectionLength = 0;
            });
        }

        #endregion

        #region Bonsai Management

        /// <summary>
        /// Create a new bonsai with the given name
        /// </summary>
        private void CreateNewBonsai(string name)
        {
            // Create a new bonsai pet
            currentBonsai = new BonsaiPet(name, random);
            
            // Register event handlers
            currentBonsai.StatsChanged += CurrentBonsai_StatsChanged;
            currentBonsai.NotificationTriggered += CurrentBonsai_NotificationTriggered;
            currentBonsai.StageAdvanced += CurrentBonsai_StageAdvanced;
            
            // Set as active bonsai in the integration manager
            integration.SetActiveBonsai(currentBonsai);
            
            // Update UI
            UpdateStatsDisplay();
            UpdateNeedsDisplay();
            
            // Generate tree
            _ = GenerateTreeAsync();
        }
        
        /// <summary>
        /// Create a new bonsai with dialog for naming
        /// </summary>
        private void CreateNewBonsaiWithDialog()
        {
            // Show dialog to enter name
            using (Form nameForm = new Form())
            {
                nameForm.Text = "New Bonsai";
                nameForm.Size = new Size(300, 150);
                nameForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                nameForm.StartPosition = FormStartPosition.CenterParent;
                nameForm.MaximizeBox = false;
                nameForm.MinimizeBox = false;

                Label nameLabel = new Label
                {
                    Text = "Enter a name for your new bonsai:",
                    Location = new Point(20, 20),
                    Size = new Size(260, 20)
                };

                TextBox nameTextBox = new TextBox
                {
                    Location = new Point(20, 50),
                    Size = new Size(260, 20),
                    Text = "Bonsy"
                };

                Button okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(110, 80),
                    Size = new Size(80, 25)
                };

                nameForm.Controls.Add(nameLabel);
                nameForm.Controls.Add(nameTextBox);
                nameForm.Controls.Add(okButton);
                nameForm.AcceptButton = okButton;

                if (nameForm.ShowDialog(this) == DialogResult.OK)
                {
                    string name = nameTextBox.Text.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        name = "Bonsy";
                    }

                    CreateNewBonsai(name);
                }
            }
        }
        
        /// <summary>
        /// Save the current bonsai to file
        /// </summary>
        private void SaveBonsai()
        {
            if (currentBonsai == null)
            {
                MessageBox.Show("No bonsai to save. Please adopt a bonsai first.", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Bonsai Files (*.bonsai)|*.bonsai|All Files (*.*)|*.*";
                saveDialog.Title = "Save Bonsai";
                saveDialog.DefaultExt = "bonsai";
                saveDialog.FileName = $"{currentBonsai.Name}.bonsai";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Integrated save that includes all phase data
                        SaveIntegratedBonsai(saveDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving bonsai: {ex.Message}", "Save Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        
        /// <summary>
        /// Save bonsai with integrated phase data
        /// </summary>
        private async void SaveIntegratedBonsai(string filePath)
        {
            try
            {
                SetUIGenerating(true);
                
                // Save the bonsai itself
                currentBonsai.SaveToFile(filePath);
                
                // Save additional phase data
                string basePath = Path.GetDirectoryName(filePath);
                await integration.SaveAllDataAsync(basePath);
                
                SetUIGenerating(false);
                
                MessageBox.Show($"Your bonsai {currentBonsai.Name} has been saved successfully!",
                    "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetUIGenerating(false);
                ShowError("Failed to save bonsai", ex);
            }
        }
        
        /// <summary>
        /// Load a bonsai from file
        /// </summary>
        private void LoadBonsai()
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Bonsai Files (*.bonsai)|*.bonsai|All Files (*.*)|*.*";
                openDialog.Title = "Load Bonsai";
                
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    // Integrated load that includes all phase data
                    _ = LoadIntegratedBonsaiAsync(openDialog.FileName);
                }
            }
        }
        
        /// <summary>
        /// Load bonsai with integrated phase data
        /// </summary>
        private async Task LoadIntegratedBonsaiAsync(string filePath)
        {
            try
            {
                SetUIGenerating(true);
                
                // Load the bonsai itself
                currentBonsai = BonsaiPet.LoadFromFile(filePath);
                
                // Hook up event handlers
                currentBonsai.StatsChanged += CurrentBonsai_StatsChanged;
                currentBonsai.NotificationTriggered += CurrentBonsai_NotificationTriggered;
                currentBonsai.StageAdvanced += CurrentBonsai_StageAdvanced;
                
                // Load additional phase data
                string basePath = Path.GetDirectoryName(filePath);
                await integration.LoadAllDataAsync(basePath);
                
                // Set as active bonsai in integration manager
                integration.SetActiveBonsai(currentBonsai);
                
                // Reset lastUpdateTime to prevent time jump
                lastUpdateTime = DateTime.Now;
                
                // Update the UI
                UpdateStatsDisplay();
                UpdateNeedsDisplay();
                await GenerateTreeAsync();
                
                // Clear notifications
                notificationListView?.Items.Clear();
                
                // Add welcome back notification
                AddNotificationToListView(new BonsaiNotification(
                    "Welcome Back!", 
                    $"Your bonsai {currentBonsai.Name} has been loaded successfully!",
                    NotificationSeverity.Information));
                    
                SetUIGenerating(false);
            }
            catch (Exception ex)
            {
                SetUIGenerating(false);
                ShowError("Failed to load bonsai", ex);
            }
        }
        
        /// <summary>
        /// Show the about dialog
        /// </summary>
        private void ShowAboutDialog()
        {
            MessageBox.Show(
                "BonsaiGotchi v1.0\n\n" +
                "A virtual bonsai pet game inspired by Tamagotchi.\n" +
                "Care for your bonsai and watch it grow!\n\n" +
                "Created by cs121287",
                "About BonsaiGotchi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle bonsai stats changed event
        /// </summary>
        private void CurrentBonsai_StatsChanged(object sender, EventArgs e)
        {
            UpdateStatsDisplay();
            UpdateNeedsDisplay();

            // Check if tree appearance needs updating
            if (sender is BonsaiPet bonsai && (bonsai.IsSick || bonsai.IsDead))
            {
                _ = GenerateTreeAsync();
            }
        }
        
        /// <summary>
        /// Handle bonsai notification event
        /// </summary>
        private void CurrentBonsai_NotificationTriggered(object sender, NotificationEventArgs e)
        {
            AddNotificationToListView(e.Notification);
        }
        
        /// <summary>
        /// Handle bonsai stage advanced event
        /// </summary>
        private void CurrentBonsai_StageAdvanced(object sender, StageAdvancedEventArgs e)
        {
            SafeInvoke(() =>
            {
                MessageBox.Show(
                    $"Your bonsai {currentBonsai.Name} has grown to the {e.NewStage} stage!",
                    "Stage Advanced",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                
                // Update tree appearance
                _ = GenerateTreeAsync();
            });
        }
        
        /// <summary>
        /// Game timer tick event - update bonsai state
        /// </summary>
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (currentBonsai == null) return;
                
                // Calculate elapsed time since last update
                TimeSpan elapsed = DateTime.Now - lastUpdateTime;
                lastUpdateTime = DateTime.Now;
                
                // Update bonsai state
                currentBonsai.Update(elapsed);
                
                // Update with environmental effects
                integration.UpdateBonsaiWithEnvironment(currentBonsai, elapsed);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Game timer error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// UI update timer tick event - refresh display
        /// </summary>
        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Update in-game time display
                if (currentBonsai != null)
                {
                    UpdateStatsDisplay();
                }
                
                // Update environmental displays
                UpdateEnvironmentDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI timer error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Water button click handler
        /// </summary>
        private void WaterButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null || currentBonsai.IsDead) return;
            
            // Use enhanced water method if available
            if (currentBonsai.GetType().GetMethod("EnhancedWater") != null)
            {
                currentBonsai.EnhancedWater();
            }
            else
            {
                currentBonsai.Water();
            }
            
            // Show animation effect
            StartRainEffect();
        }
        
        /// <summary>
        /// Feed button click handler
        /// </summary>
        private void FeedButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null || currentBonsai.IsDead) return;
            
            // Use enhanced feed method if available
            if (currentBonsai.GetType().GetMethod("EnhancedFeed") != null)
            {
                currentBonsai.EnhancedFeed();
            }
            else
            {
                currentBonsai.Feed();
            }
        }
        
        /// <summary>
        /// Prune button click handler
        /// </summary>
        private void PruneButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null || currentBonsai.IsDead) return;
            
            // Use enhanced prune method if available
            if (currentBonsai.GetType().GetMethod("EnhancedPrune") != null)
            {
                currentBonsai.EnhancedPrune();
            }
            else
            {
                currentBonsai.Prune();
            }
            
            // Update tree appearance
            _ = GenerateTreeAsync();
        }
        
        /// <summary>
        /// Repot button click handler
        /// </summary>
        private void RepotButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null || currentBonsai.IsDead) return;
            
            currentBonsai.Repot();
        }
        
        /// <summary>
        /// Play button click handler
        /// </summary>
        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null || currentBonsai.IsDead) return;
            
            currentBonsai.Play();
        }
        
        /// <summary>
        /// Play mini-game button click handler
        /// </summary>
        private void PlayMiniGameButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null || currentBonsai.IsDead) return;
            
            // Create menu for mini-game selection
            ContextMenuStrip gameMenu = new ContextMenuStrip();
            
            // Add mini-game options
            gameMenu.Items.Add("Leaf Counting").Click += (s, args) => LaunchMiniGame(MiniGameManager.GameType.LeafCounting);
            gameMenu.Items.Add("Pest Removal").Click += (s, args) => LaunchMiniGame(MiniGameManager.GameType.PestRemoval);
            gameMenu.Items.Add("Pruning Puzzle").Click += (s, args) => LaunchMiniGame(MiniGameManager.GameType.PruningPuzzle);
            gameMenu.Items.Add("Seasonal Care").Click += (s, args) => LaunchMiniGame(MiniGameManager.GameType.SeasonalCare);
            
            // Show the menu
            gameMenu.Show(playMiniGameButton, new Point(0, playMiniGameButton.Height));
        }
        
        /// <summary>
        /// Launch a mini-game
        /// </summary>
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
                            _ = GenerateTreeAsync();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to launch mini-game", ex);
            }
        }
        
        /// <summary>
        /// Pest control button click handler
        /// </summary>
        private void PestControlButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null || currentBonsai.IsDead) return;
            
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
            if (score > 0)
            {
                currentBonsai.RemovePests(score);
            }
        }
        
        /// <summary>
        /// Treat disease button click handler
        /// </summary>
        private void TreatDiseaseButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null || currentBonsai.IsDead) return;
            
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
            using (Form treatmentForm = new Form())
            {
                treatmentForm.Text = "Treat Disease";
                treatmentForm.Size = new Size(400, 300);
                treatmentForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                treatmentForm.StartPosition = FormStartPosition.CenterParent;
                treatmentForm.MaximizeBox = false;
                treatmentForm.MinimizeBox = false;
                
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
                
                treatmentForm.ShowDialog();
            }
        }
        
        /// <summary>
        /// Play music button click handler
        /// </summary>
        private void PlayMusicButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null || currentBonsai.IsDead) return;
            
            // Create music selection options
            using (Form musicForm = new Form())
            {
                musicForm.Text = "Play Music";
                musicForm.Size = new Size(320, 250);
                musicForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                musicForm.StartPosition = FormStartPosition.CenterParent;
                musicForm.MaximizeBox = false;
                musicForm.MinimizeBox = false;
                
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
                
                musicForm.ShowDialog();
            }
        }
        
        /// <summary>
        /// Collection button click handler
        /// </summary>
        private void CollectionButton_Click(object sender, EventArgs e)
        {
            if (currentBonsai == null) return;
            
            // Open the collection manager
            var collectionForm = new CollectionManagerForm(integration.BreedingManager, random);
            collectionForm.ShowDialog();
        }
        
        /// <summary>
        /// Environment button click handler
        /// </summary>
        private void EnvironmentButton_Click(object sender, EventArgs e)
        {
            // Open the environment monitor
            var environmentForm = new EnvironmentMonitorForm(integration.EnvironmentManager);
            environmentForm.ShowDialog();
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Update stats display
        /// </summary>
        private void UpdateStatsDisplay()
        {
            if (currentBonsai == null) return;

            SafeInvoke(() =>
            {
                try
                {
                    // Update basic stats (Phase 1)
                    UpdateLabel("nameLabel", currentBonsai.Name);
                    UpdateLabel("ageLabel", $"{currentBonsai.Age} days");
                    UpdateLabel("stageLabel", currentBonsai.CurrentStage.ToString());

                    UpdateProgressBar("healthBar", (int)Math.Round(currentBonsai.Health));
                    UpdateProgressBar("happinessBar", (int)Math.Round(currentBonsai.Happiness));
                    UpdateProgressBar("hungerBar", (int)Math.Round(currentBonsai.Hunger));
                    UpdateProgressBar("growthBar", (int)Math.Round(currentBonsai.Growth));
                    
                    // Update enhanced stats (Phase 2)
                    UpdateProgressBar("hydrationBar", (int)Math.Round(currentBonsai.Hydration));
                    UpdateProgressBar("soilQualityBar", (int)Math.Round(currentBonsai.SoilQuality));
                    UpdateProgressBar("stressBar", (int)Math.Round(currentBonsai.StressLevel));
                    UpdateProgressBar("pestBar", (int)Math.Round(currentBonsai.PestInfestation));
                    UpdateProgressBar("diseaseBar", (int)Math.Round(currentBonsai.DiseaseLevel));
                    
                    // Update environmental stats (Phase 3)
                    if (integration?.EnvironmentManager != null)
                    {
                        UpdateLabel("seasonLabel", integration.EnvironmentManager.CurrentSeason.ToString());
                        UpdateLabel("weatherLabel", integration.EnvironmentManager.CurrentWeather.ToString());
                        UpdateLabel("timeOfDayLabel", integration.EnvironmentManager.CurrentTimeOfDay.ToString());
                    }
                    
                    // Enable/disable buttons based on bonsai status
                    bool enableButtons = !currentBonsai.IsDead;
                    waterButton.Enabled = enableButtons;
                    feedButton.Enabled = enableButtons;
                    pruneButton.Enabled = enableButtons;
                    repotButton.Enabled = enableButtons;
                    playButton.Enabled = enableButtons;
                    
                    // Phase 2-3 buttons
                    if (playMiniGameButton != null) playMiniGameButton.Enabled = enableButtons;
                    if (pestControlButton != null) pestControlButton.Enabled = enableButtons && currentBonsai.PestInfestation > 10;
                    if (treatDiseaseButton != null) treatDiseaseButton.Enabled = enableButtons && currentBonsai.DiseaseLevel > 10;
                    if (playMusicButton != null) playMusicButton.Enabled = enableButtons;
                    if (collectionButton != null) collectionButton.Enabled = enableButtons;
                    if (environmentButton != null) environmentButton.Enabled = enableButtons;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating stats display: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Update needs display with visual indicators
        /// </summary>
        private void UpdateNeedsDisplay()
        {
            if (currentBonsai == null) return;

            SafeInvoke(() =>
            {
                try
                {
                    // Update progress bars with color indicators
                    UpdateProgressBarWithColor("healthBar", (int)Math.Round(currentBonsai.Health), GetHealthColor(currentBonsai.Health));
                    UpdateProgressBarWithColor("happinessBar", (int)Math.Round(currentBonsai.Happiness), GetHappinessColor(currentBonsai.Happiness));
                    UpdateProgressBarWithColor("hungerBar", (int)Math.Round(currentBonsai.Hunger), GetHungerColor(currentBonsai.Hunger));
                    
                    // Update enhanced needs (Phase 2)
                    if (currentBonsai.GetType().GetProperty("StressLevel") != null)
                    {
                        UpdateProgressBarWithColor("stressBar", (int)Math.Round(currentBonsai.StressLevel), GetStressColor(currentBonsai.StressLevel));
                        UpdateProgressBarWithColor("pestBar", (int)Math.Round(currentBonsai.PestInfestation), GetPestColor(currentBonsai.PestInfestation));
                        UpdateProgressBarWithColor("diseaseBar", (int)Math.Round(currentBonsai.DiseaseLevel), GetDiseaseColor(currentBonsai.DiseaseLevel));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating needs display: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Update environment display
        /// </summary>
        private void UpdateEnvironmentDisplay()
        {
            if (integration?.EnvironmentManager == null) return;
            
            SafeInvoke(() =>
            {
                try
                {
                    // Update environment stats (Phase 3)
                    UpdateLabel("seasonLabel", integration.EnvironmentManager.CurrentSeason.ToString());
                    UpdateLabel("weatherLabel", integration.EnvironmentManager.CurrentWeather.ToString());
                    UpdateLabel("timeOfDayLabel", integration.EnvironmentManager.CurrentTimeOfDay.ToString());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating environment display: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Update a label control by name
        /// </summary>
        private void UpdateLabel(string name, string value)
        {
            try
            {
                Control[] controls = needsPanel.Controls.Find(name, true);
                if (controls.Length > 0 && controls[0] is Label label)
                {
                    label.Text = value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating label {name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update a progress bar control by name
        /// </summary>
        private void UpdateProgressBar(string name, int value)
        {
            try
            {
                Control[] controls = needsPanel.Controls.Find(name, true);
                if (controls.Length > 0 && controls[0] is ProgressBar progressBar)
                {
                    progressBar.Value = Math.Max(0, Math.Min(100, value));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating progress bar {name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update a progress bar control with color
        /// </summary>
        private void UpdateProgressBarWithColor(string name, int value, Color color)
        {
            try
            {
                Control[] controls = needsPanel.Controls.Find(name, true);
                if (controls.Length > 0 && controls[0] is ProgressBar progressBar)
                {
                    progressBar.Value = Math.Max(0, Math.Min(100, value));
                    progressBar.ForeColor = color;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating progress bar {name} with color: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add a notification to the notification list view
        /// </summary>
        private void AddNotificationToListView(BonsaiNotification notification)
        {
            SafeInvoke(() =>
            {
                if (notificationListView == null) return;

                try
                {
                    // Create list view item
                    ListViewItem item = new ListViewItem(DateTime.Now.ToString("HH:mm"));
                    item.SubItems.Add(notification.Message);
                    item.Tag = notification;

                    // Set color based on severity
                    switch (notification.Severity)
                    {
                        case NotificationSeverity.Critical:
                            item.BackColor = Color.LightCoral;
                            break;
                        case NotificationSeverity.Alert:
                            item.BackColor = Color.LightSalmon;
                            break;
                        case NotificationSeverity.Warning:
                            item.BackColor = Color.Khaki;
                            break;
                        case NotificationSeverity.Achievement:
                            item.BackColor = Color.LightGreen;
                            break;
                        case NotificationSeverity.Information:
                        default:
                            break;
                    }

                    // Add item and ensure it's visible
                    notificationListView.Items.Add(item);
                    item.EnsureVisible();

                    // Limit to 50 notifications
                    while (notificationListView.Items.Count > 50)
                    {
                        notificationListView.Items.RemoveAt(0);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding notification: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// Notify about environmental event
        /// </summary>
        public void NotifyEnvironmentalEvent(EnvironmentalEvent envEvent, bool started)
        {
            if (envEvent == null) return;
            
            // Create notification
            string action = started ? "started" : "ended";
            string message = $"Environmental event {action}: {envEvent.GetDescription()}";
            
            NotificationSeverity severity;
            
            // Determine severity based on event type and intensity
            if (envEvent.Intensity > 70) // High intensity events are more severe
            {
                severity = NotificationSeverity.Alert;
            }
            else if (IsHarmfulEvent(envEvent.Type))
            {
                severity = NotificationSeverity.Warning;
            }
            else
            {
                severity = NotificationSeverity.Information;
            }
            
            // Add notification
            AddNotificationToListView(new BonsaiNotification(
                started ? "Environmental Event Started" : "Environmental Event Ended",
                message,
                severity));
        }
        
        /// <summary>
        /// Check if an event type is harmful
        /// </summary>
        private bool IsHarmfulEvent(EventType eventType)
        {
            return eventType switch
            {
                EventType.Drought => true,
                EventType.Heatwave => true,
                EventType.ColdSnap => true,
                EventType.Storm => true,
                EventType.Frost => true,
                EventType.Insects => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Start a rain effect animation
        /// </summary>
        private void StartRainEffect()
        {
            if (treeDisplay == null) return;
            
            // Create a temporary timer for animation
            Timer animationTimer = new Timer();
            animationTimer.Interval = 50; // 50ms between frames
            
            int frameCount = 0;
            int maxFrames = 20; // 1 second total
            
            animationTimer.Tick += (s, e) =>
            {
                frameCount++;
                
                if (frameCount < maxFrames)
                {
                    // Add random raindrops
                    if (currentTree != null)
                    {
                        SafeInvoke(() =>
                        {
                            // Add rain drops in random positions
                            int raindrops = 20;
                            for (int i = 0; i < raindrops; i++)
                            {
                                int x = random.Next(currentTree.Width);
                                int y = random.Next(currentTree.Height / 2); // Rain mostly at top
                                
                                // Only add rain where there's empty space
                                if (currentTree.GetCharAt(x, y) == ' ')
                                {
                                    currentTree.SetCharAt(x, y, '|');
                                    currentTree.SetColorAt(x, y, Color.LightBlue);
                                }
                            }
                            
                            // Display the modified tree
                            DisplayTree();
                        });
                    }
                }
                else
                {
                    // Stop animation and restore normal display
                    animationTimer.Stop();
                    animationTimer.Dispose();
                    _ = GenerateTreeAsync();
                }
            };
            
            // Start animation
            animationTimer.Start();
        }
        
        /// <summary>
        /// Update the current bonsai with environmental effects
        /// </summary>
        public void UpdateCurrentBonsaiWithEnvironment()
        {
            if (currentBonsai == null) return;
            
            // Update the tree appearance based on new environment
            _ = GenerateTreeAsync();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Safely invoke an action on the UI thread
        /// </summary>
        private void SafeInvoke(Action action)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;

                if (InvokeRequired)
                    Invoke(action);
                else
                    action();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SafeInvoke: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Set UI state during generation
        /// </summary>
        private void SetUIGenerating(bool generating)
        {
            isGenerating = generating;
            SafeInvoke(() => Cursor = generating ? Cursors.WaitCursor : Cursors.Default);
        }
        
        /// <summary>
        /// Get color for health bar
        /// </summary>
        private Color GetHealthColor(double health)
        {
            if (health > 66) return Color.FromArgb(46, 204, 113); // Green for good
            if (health > 33) return Color.FromArgb(241, 196, 15); // Yellow for caution
            return Color.FromArgb(231, 76, 60); // Red for danger
        }
        
        /// <summary>
        /// Get color for happiness bar
        /// </summary>
        private Color GetHappinessColor(double happiness)
        {
            if (happiness > 66) return Color.FromArgb(46, 204, 113); // Green for good
            if (happiness > 33) return Color.FromArgb(241, 196, 15); // Yellow for caution
            return Color.FromArgb(231, 76, 60); // Red for danger
        }
        
        /// <summary>
        /// Get color for hunger bar
        /// </summary>
        private Color GetHungerColor(double hunger)
        {
            if (hunger < 33) return Color.FromArgb(46, 204, 113); // Green for good
            if (hunger < 66) return Color.FromArgb(241, 196, 15); // Yellow for caution
            return Color.FromArgb(231, 76, 60); // Red for danger
        }
        
        /// <summary>
        /// Get color for stress bar
        /// </summary>
        private Color GetStressColor(double stress)
        {
            if (stress < 33) return Color.FromArgb(46, 204, 113); // Green for good
            if (stress < 66) return Color.FromArgb(241, 196, 15); // Yellow for caution
            return Color.FromArgb(231, 76, 60); // Red for danger
        }
        
        /// <summary>
        /// Get color for pest bar
        /// </summary>
        private Color GetPestColor(double pestLevel)
        {
            if (pestLevel < 33) return Color.FromArgb(46, 204, 113); // Green for good
            if (pestLevel < 66) return Color.FromArgb(241, 196, 15); // Yellow for caution
            return Color.FromArgb(231, 76, 60); // Red for danger
        }
        
        /// <summary>
        /// Get color for disease bar
        /// </summary>
        private Color GetDiseaseColor(double diseaseLevel)
        {
            if (diseaseLevel < 33) return Color.FromArgb(46, 204, 113); // Green for good
            if (diseaseLevel < 66) return Color.FromArgb(241, 196, 15); // Yellow for caution
            return Color.FromArgb(231, 76, 60); // Red for danger
        }
        
        /// <summary>
        /// Show an error message
        /// </summary>
        private void ShowError(string message, Exception ex)
        {
            SafeInvoke(() => 
            {
                MessageBox.Show(
                    $"{message}: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            });
        }

        #endregion

        #region Form Lifecycle

        /// <summary>
        /// Form load event handler
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            isFormReady = true;
            _ = GenerateTreeAsync();
        }
        
        /// <summary>
        /// Form closing event handler
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Ask for confirmation if there's a living bonsai
            if (currentBonsai != null && !currentBonsai.IsDead)
            {
                DialogResult result = MessageBox.Show(
                    "Do you want to save your bonsai before exiting?",
                    "Save Bonsai?",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    SaveBonsai();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            
            base.OnFormClosing(e);
        }
        
        /// <summary>
        /// Release resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                // Stop timers
                gameTimer?.Stop();
                gameTimer?.Dispose();
                uiUpdateTimer?.Stop();
                uiUpdateTimer?.Dispose();
                
                // Dispose integration manager
                integration?.Dispose();
                
                disposed = true;
            }
            
            base.Dispose(disposing);
        }

        #endregion
    }
}