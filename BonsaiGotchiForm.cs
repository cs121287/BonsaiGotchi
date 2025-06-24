using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using BonsaiTreeGenerator;

namespace BonsaiGotchi
{
    /// <summary>
    /// Main form for the BonsaiGotchi game
    /// Combines bonsai tree generation with Tamagotchi mechanics
    /// </summary>
    public partial class BonsaiGotchiForm : Form
    {
        #region Private Fields

        // Core components
        private readonly Random random = new();
        private readonly BonsaiTreeGenerator.BonsaiTreeGenerator treeGenerator;
        private readonly object lockObject = new();
        private BonsaiPet currentBonsai;

        // UI Components
        private TableLayoutPanel? mainContainer;
        private Panel? treePanel;
        private Panel? statsPanel;
        private Panel? notificationPanel;
        private Panel? needsPanel;
        private Panel? controlPanel;
        private Panel? timeControlPanel;
        private RichTextBox? treeDisplay;
        private RichTextBox? statsDisplay;
        private Button? adoptButton;
        private Button? saveButton;
        private Button? loadButton;
        private Button? waterButton;
        private Button? feedButton;
        private Button? pruneButton;
        private Button? repotButton;
        private Button? playButton;
        private Label? statusLabel;
        private Label? timeLabel;
        private Label? ageLabel;
        private ProgressBar? progressBar;
        private ProgressBar? healthBar;
        private ProgressBar? happinessBar;
        private ProgressBar? hungerBar;
        private ProgressBar? growthBar;
        private ListView? notificationListView;

        // Time control buttons
        private Button? timeNormalButton;
        private Button? time2xButton;
        private Button? time5xButton;
        private Button? time10xButton;

        // Timer components
        private System.Windows.Forms.Timer? gameTimer;
        private System.Windows.Forms.Timer? uiUpdateTimer;
        private DateTime lastUpdateTime;

        // Configuration constants
        private const int TREE_WIDTH = 90;
        private const int TREE_HEIGHT = 35;
        private const int MAX_GENERATION_TIME_MS = 100000; // 100 seconds timeout

        // Threading and state management
        private CancellationTokenSource cancellationTokenSource = new();
        private volatile bool isGenerating = false;
        private volatile bool isFormReady = false;
        private bool disposed = false;

        // Current tree data
        private char[,]? currentTree;
        private readonly Dictionary<char, Color>? colorMapping;

        // Enhanced rain effect
        private System.Windows.Forms.Timer? rainTimer;
        private List<RainDrop>? rainDrops;
        private int rainFrameCount = 0;
        private readonly int RAIN_DROP_COUNT = 120;
        private readonly int RAIN_INTERVAL = 50;
        private readonly int RAIN_DURATION_FRAMES = 10;

        #endregion

        #region Constructor and Form Setup

        public BonsaiGotchiForm()
        {
            try
            {
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

                // Initialize the bonsai pet
                CreateNewBonsai("Bonsy");
                lastUpdateTime = DateTime.Now;
                
                // Start timers
                InitializeTimers();
            }
            catch (Exception ex)
            {
                ShowError("Failed to initialize BonsaiGotchi", ex);
            }
        }

        private void SetupUserInterface()
        {
            try
            {
                Text = "BonsaiGotchi - Virtual Bonsai Pet";

                // Start maximized
                WindowState = FormWindowState.Maximized;
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.FromArgb(248, 249, 250);

                CreateMainLayout();
                CreateTreeDisplay();
                CreateStatsPanel();
                CreateNeedsPanel();
                CreateNotificationPanel();
                CreateControlPanel();
                CreateTimeControlPanel();
            }
            catch (Exception ex)
            {
                ShowError("Failed to setup user interface", ex);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                isFormReady = true;
                _ = GenerateTreeAsync();
            }
            catch (Exception ex)
            {
                ShowError("Failed to load initial tree", ex);
            }
        }

        #endregion

        #region UI Creation Methods

        private void CreateMainLayout()
        {
            mainContainer?.Dispose();

            mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.FromArgb(248, 249, 250),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(12)
            };

            // Configure column styles - tree takes more space
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F)); // Tree area
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F)); // Stats area

            // Configure row styles
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 68F)); // Tree and stats
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 20F)); // Notifications
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 12F)); // Controls

            Controls.Add(mainContainer);
        }

        private void CreateTreeDisplay()
        {
            if (mainContainer == null) return;

            treePanel?.Dispose();

            treePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0, 0, 6, 0),
                AutoScroll = false,
                Padding = new Padding(2)
            };

            // Add subtle shadow effect
            treePanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, treePanel.ClientRectangle,
                    Color.FromArgb(200, 200, 200), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(200, 200, 200), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(200, 200, 200), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(200, 200, 200), 1, ButtonBorderStyle.Solid);
            };

            // Create the main tree display
            treeDisplay?.Dispose();
            treeDisplay = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9F, FontStyle.Regular),
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None,
                WordWrap = false,
                DetectUrls = false,
                EnableAutoDragDrop = false,
                HideSelection = false,
                Margin = new Padding(8)
            };

            // Enable double buffering for the tree display
            typeof(Control).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, treeDisplay, [true]);

            // Create tree title
            var treeTitle = new Label
            {
                Text = "YOUR BONSAI PET",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(236, 240, 241),
                Padding = new Padding(0, 12, 0, 0)
            };

            // Create age label
            ageLabel = new Label
            {
                Text = "Age: 0 days (Seedling)",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(236, 240, 241),
                Padding = new Padding(0, 0, 0, 6)
            };

            // Create progress bar
            progressBar?.Dispose();
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 6,
                Style = ProgressBarStyle.Continuous,
                Visible = false,
                ForeColor = Color.FromArgb(46, 204, 113)
            };

            treePanel.Controls.AddRange([treeDisplay, progressBar, ageLabel, treeTitle]);
            mainContainer.Controls.Add(treePanel, 0, 0);
        }

        private void CreateStatsPanel()
        {
            if (mainContainer == null) return;

            var statsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(6, 0, 0, 0)
            };

            // Stats display panel
            statsPanel?.Dispose();
            statsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(2)
            };

            // Add subtle border
            statsPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, statsPanel.ClientRectangle,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid);
            };

            // Stats title
            var statsTitle = new Label
            {
                Text = "BONSAI STATS",
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(236, 240, 241),
                Padding = new Padding(0, 12, 0, 0)
            };

            // Game time label
            timeLabel = new Label
            {
                Text = "Game Time: Day 1, 8:00 AM",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(236, 240, 241),
                Padding = new Padding(0, 0, 0, 6)
            };

            statsDisplay?.Dispose();
            statsDisplay = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 13F),
                DetectUrls = false,
                EnableAutoDragDrop = false,
                Padding = new Padding(16, 12, 16, 12)
            };

            statsPanel.Controls.AddRange([statsDisplay, timeLabel, statsTitle]);
            statsContainer.Controls.Add(statsPanel);

            mainContainer.Controls.Add(statsContainer, 1, 0);
        }

        private void CreateNeedsPanel()
        {
            if (mainContainer == null) return;

            // Needs panel (progress bars for health, happiness, hunger, etc.)
            needsPanel?.Dispose();
            needsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 180,
                BackColor = Color.White,
                Padding = new Padding(16, 8, 16, 16)
            };

            // Add subtle border
            needsPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, needsPanel.ClientRectangle,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid);
            };

            // Needs panel title
            var needsTitle = new Label
            {
                Text = "BONSAI NEEDS",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Top,
                Height = 30,
                Margin = new Padding(0, 0, 0, 5)
            };

            // Health bar with label
            var healthLabel = new Label
            {
                Text = "Health:",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(120, 25),
                Location = new Point(0, 40)
            };

            healthBar = new ProgressBar
            {
                Size = new Size(needsPanel.Width - 140, 25),
                Location = new Point(120, 40),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 80,
                ForeColor = Color.FromArgb(46, 204, 113)
            };

            // Happiness bar with label
            var happinessLabel = new Label
            {
                Text = "Happiness:",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(120, 25),
                Location = new Point(0, 75)
            };

            happinessBar = new ProgressBar
            {
                Size = new Size(needsPanel.Width - 140, 25),
                Location = new Point(120, 75),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 70,
                ForeColor = Color.FromArgb(52, 152, 219)
            };

            // Hunger bar with label
            var hungerLabel = new Label
            {
                Text = "Hunger:",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(120, 25),
                Location = new Point(0, 110)
            };

            hungerBar = new ProgressBar
            {
                Size = new Size(needsPanel.Width - 140, 25),
                Location = new Point(120, 110),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 20,
                ForeColor = Color.FromArgb(231, 76, 60) // Red for hunger (reversed scale)
            };

            // Growth bar with label
            var growthLabel = new Label
            {
                Text = "Growth:",
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(120, 25),
                Location = new Point(0, 145)
            };

            growthBar = new ProgressBar
            {
                Size = new Size(needsPanel.Width - 140, 25),
                Location = new Point(120, 145),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                ForeColor = Color.FromArgb(155, 89, 182) // Purple for growth
            };

            needsPanel.Controls.AddRange([needsTitle, healthLabel, healthBar, happinessLabel, happinessBar, hungerLabel, hungerBar, growthLabel, growthBar]);
            statsPanel?.Controls.Add(needsPanel);
        }

        private void CreateNotificationPanel()
        {
            if (mainContainer == null) return;

            notificationPanel?.Dispose();
            notificationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0, 6, 0, 6)
            };

            // Add subtle border
            notificationPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, notificationPanel.ClientRectangle,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(220, 220, 220), 1, ButtonBorderStyle.Solid);
            };

            // Notification panel title
            var notificationTitle = new Label
            {
                Text = "NOTIFICATIONS",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(236, 240, 241)
            };

            // Notification list view
            notificationListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White
            };

            // Set up columns
            notificationListView.Columns.Add("Time", 100);
            notificationListView.Columns.Add("Title", 150);
            notificationListView.Columns.Add("Message", 550);

            // Add image list for icons
            var imageList = new ImageList();
            imageList.Images.Add("Information", SystemIcons.Information);
            imageList.Images.Add("Warning", SystemIcons.Warning);
            imageList.Images.Add("Error", SystemIcons.Error);
            imageList.Images.Add("Achievement", SystemIcons.Asterisk);
            notificationListView.SmallImageList = imageList;

            notificationPanel.Controls.AddRange([notificationListView, notificationTitle]);
            mainContainer.Controls.Add(notificationPanel, 0, 1);
            mainContainer.SetColumnSpan(notificationPanel, 2);
        }

        private void CreateControlPanel()
        {
            if (mainContainer == null) return;

            controlPanel?.Dispose();
            controlPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(236, 240, 241),
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0, 8, 0, 0),
                Padding = new Padding(16, 12, 16, 12)
            };

            // Add subtle top border
            controlPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(189, 195, 199), 1);
                e.Graphics.DrawLine(pen, 0, 0, controlPanel.Width, 0);
            };

            // Adopt button
            adoptButton?.Dispose();
            adoptButton = new Button
            {
                Text = "Adopt New Bonsai",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(160, 45),
                Location = new Point(16, 12),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            adoptButton.FlatAppearance.BorderSize = 0;
            adoptButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);
            adoptButton.Click += AdoptButton_Click;

            // Save button
            saveButton?.Dispose();
            saveButton = new Button
            {
                Text = "Save Bonsai",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(140, 45),
                Location = new Point(186, 12),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            saveButton.Click += SaveButton_Click;

            // Load button
            loadButton?.Dispose();
            loadButton = new Button
            {
                Text = "Load Bonsai",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(140, 45),
                Location = new Point(336, 12),
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            loadButton.FlatAppearance.BorderSize = 0;
            loadButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(142, 68, 173);
            loadButton.Click += LoadButton_Click;

            // Care buttons
            int buttonWidth = 130;
            int buttonSpacing = 10;
            int startX = 500;

            // Water button
            waterButton?.Dispose();
            waterButton = new Button
            {
                Text = "Water",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(buttonWidth, 45),
                Location = new Point(startX, 12),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            waterButton.FlatAppearance.BorderSize = 0;
            waterButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            waterButton.Click += WaterButton_Click;

            // Feed button
            feedButton?.Dispose();
            feedButton = new Button
            {
                Text = "Feed",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(buttonWidth, 45),
                Location = new Point(startX + buttonWidth + buttonSpacing, 12),
                BackColor = Color.FromArgb(230, 126, 34),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            feedButton.FlatAppearance.BorderSize = 0;
            feedButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(211, 84, 0);
            feedButton.Click += FeedButton_Click;

            // Prune button
            pruneButton?.Dispose();
            pruneButton = new Button
            {
                Text = "Prune",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(buttonWidth, 45),
                Location = new Point(startX + (buttonWidth + buttonSpacing) * 2, 12),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            pruneButton.FlatAppearance.BorderSize = 0;
            pruneButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);
            pruneButton.Click += PruneButton_Click;

            // Repot button
            repotButton?.Dispose();
            repotButton = new Button
            {
                Text = "Repot",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(buttonWidth, 45),
                Location = new Point(startX + (buttonWidth + buttonSpacing) * 3, 12),
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            repotButton.FlatAppearance.BorderSize = 0;
            repotButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(142, 68, 173);
            repotButton.Click += RepotButton_Click;

            // Play button
            playButton?.Dispose();
            playButton = new Button
            {
                Text = "Play",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(buttonWidth, 45),
                Location = new Point(startX + (buttonWidth + buttonSpacing) * 4, 12),
                BackColor = Color.FromArgb(26, 188, 156),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            playButton.FlatAppearance.BorderSize = 0;
            playButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 160, 133);
            playButton.Click += PlayButton_Click;

            // Status label with copyright
            statusLabel?.Dispose();
            statusLabel = new Label
            {
                Text = $"© {DateTime.Now.Year} BonsaiGotchi",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(127, 140, 141),
                Location = new Point(16, 65),
                Size = new Size(400, 25),
                TextAlign = ContentAlignment.MiddleLeft
            };

            controlPanel.Controls.AddRange([adoptButton, saveButton, loadButton, waterButton, feedButton, pruneButton, repotButton, playButton, statusLabel]);
            mainContainer.Controls.Add(controlPanel, 0, 2);
            mainContainer.SetColumnSpan(controlPanel, 2);
        }
        
        private void CreateTimeControlPanel()
        {
            timeControlPanel?.Dispose();
            timeControlPanel = new Panel
            {
                Dock = DockStyle.None,
                Size = new Size(330, 45),
                Location = new Point(controlPanel.Width - 350, 12),
                BackColor = Color.Transparent
            };
            
            // Time control buttons
            int buttonWidth = 70;
            int spacing = 5;
            
            // Normal speed (1x)
            timeNormalButton = new Button
            {
                Text = "1×",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(buttonWidth, 38),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = 1.0
            };
            timeNormalButton.FlatAppearance.BorderSize = 0;
            timeNormalButton.Click += TimeMultiplierButton_Click;
            
            // 2× speed
            time2xButton = new Button
            {
                Text = "2×",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(buttonWidth, 38),
                Location = new Point(buttonWidth + spacing, 0),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = 2.0
            };
            time2xButton.FlatAppearance.BorderSize = 0;
            time2xButton.Click += TimeMultiplierButton_Click;
            
            // 5× speed
            time5xButton = new Button
            {
                Text = "5×",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(buttonWidth, 38),
                Location = new Point((buttonWidth + spacing) * 2, 0),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = 5.0
            };
            time5xButton.FlatAppearance.BorderSize = 0;
            time5xButton.Click += TimeMultiplierButton_Click;
            
            // 10× speed
            time10xButton = new Button
            {
                Text = "10×",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Size = new Size(buttonWidth, 38),
                Location = new Point((buttonWidth + spacing) * 3, 0),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = 10.0
            };
            time10xButton.FlatAppearance.BorderSize = 0;
            time10xButton.Click += TimeMultiplierButton_Click;
            
            timeControlPanel.Controls.AddRange([timeNormalButton, time2xButton, time5xButton, time10xButton]);
            
            // Add to control panel
            controlPanel?.Controls.Add(timeControlPanel);
            
            // Update button highlighting based on current multiplier
            UpdateTimeControlButtonsUI(1.0); // Default to 1×
        }

        #endregion

        #region Tree Generation Engine

        private void InitializeTimers()
        {
            // Game timer - updates bonsai state (real-time)
            gameTimer?.Dispose();
            gameTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // Update bonsai state every 5 seconds
            };
            gameTimer.Tick += GameTimer_Tick;
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

        private async Task GenerateTreeAsync()
        {
            if (!isFormReady || isGenerating) return;

            try
            {
                lock (lockObject)
                {
                    if (isGenerating) return;
                    isGenerating = true;
                }

                // Cancel any existing generation
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();

                SetUIGenerating(true);

                using var timeoutCts = new CancellationTokenSource(MAX_GENERATION_TIME_MS);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationTokenSource.Token, timeoutCts.Token);

                // Create progress reporter
                var progress = new Progress<int>(UpdateProgress);

                // Generate the tree with a consistent seed based on the bonsai's properties
                // This ensures the same bonsai always looks the same, but different from others
                Random treeRandom = new(currentBonsai.TreeSeed);
                
                // Adjust tree appearance based on bonsai's age and stage
                AdjustTreeAppearanceBasedOnStage(treeRandom);

                // Generate the bonsai tree
                currentTree = await treeGenerator.GenerateTreeAsync(
                    TREE_WIDTH, TREE_HEIGHT, progress, combinedCts.Token);

                if (!combinedCts.Token.IsCancellationRequested && !IsDisposed && isFormReady)
                {
                    await ApplyTreeToUI(currentTree, combinedCts.Token);
                    UpdateStatsDisplay();
                    UpdateNeedsDisplay();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, no error message needed
            }
            catch (Exception ex)
            {
                ShowError("Failed to generate bonsai tree", ex);
            }
            finally
            {
                SetUIGenerating(false);
                lock (lockObject)
                {
                    isGenerating = false;
                }
            }
        }
        
        private void AdjustTreeAppearanceBasedOnStage(Random treeRandom)
        {
            // This would modify parameters in the tree generator based on the bonsai's current stage
            // For now, just use a different seed for each stage so the tree grows and changes
            int stageSeed = currentBonsai.TreeSeed;
            
            switch (currentBonsai.CurrentStage)
            {
                case GrowthStage.Seedling:
                    // Small, simple tree
                    break;
                    
                case GrowthStage.Sapling:
                    // Slightly larger
                    stageSeed += 1000;
                    break;
                    
                case GrowthStage.YoungTree:
                    // Medium size with more branches
                    stageSeed += 2000;
                    break;
                    
                case GrowthStage.MatureTree:
                    // Full-sized tree
                    stageSeed += 3000;
                    break;
                    
                case GrowthStage.ElderTree:
                    // Majestic tree
                    stageSeed += 4000;
                    break;
            }
            
            // Apply health effects
            if (currentBonsai.Health < 30)
            {
                // Tree appears sickly
                stageSeed += 500;
            }
            
            if (currentBonsai.IsDead)
            {
                // Dead tree appearance
                stageSeed = -stageSeed;
            }
            
            // Set the modified seed for consistent generation
            random = new Random(stageSeed);
        }

        private async Task ApplyTreeToUI(char[,] tree, CancellationToken cancellationToken)
        {
            if (!isFormReady || treeDisplay == null || treeDisplay.IsDisposed) return;

            await Task.Run(() =>
            {
                SafeInvoke(() =>
                {
                    if (isFormReady && treeDisplay != null && !treeDisplay.IsDisposed && treeDisplay.IsHandleCreated)
                    {
                        // Suspend layout to prevent flicker during updates
                        treeDisplay.SuspendLayout();

                        try
                        {
                            // Convert tree array to string
                            var treeText = ConvertTreeToString(tree);

                            // Clear and set text in one operation
                            treeDisplay.Clear();
                            treeDisplay.Text = treeText;

                            // Apply color formatting efficiently
                            ApplyColorFormattingOptimized();
                        }
                        finally
                        {
                            // Resume layout and refresh
                            treeDisplay.ResumeLayout(true);
                            treeDisplay.Refresh();
                        }
                    }
                });
            }, cancellationToken);
        }

        private static string ConvertTreeToString(char[,] tree)
        {
            var treeText = new StringBuilder();
            int height = tree.GetLength(0);
            int width = tree.GetLength(1);

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    treeText.Append(tree[row, col]);
                }
                if (row < height - 1)
                {
                    treeText.AppendLine();
                }
            }

            return treeText.ToString();
        }
        
        private void ApplyColorFormattingOptimized()
        {
            try
            {
                if (!isFormReady || treeDisplay == null || treeDisplay.IsDisposed ||
                    !treeDisplay.IsHandleCreated || colorMapping == null) return;

                // Set default color (black background)
                treeDisplay.SelectAll();
                treeDisplay.SelectionBackColor = Color.Black;
                treeDisplay.SelectionStart = 0;

                // Apply bonsai colors in optimized batches
                string text = treeDisplay.Text;
                var colorRanges = new Dictionary<Color, List<(int start, int length)>>();

                // Group consecutive characters of the same color
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (colorMapping.TryGetValue(c, out Color color))
                    {
                        if (!colorRanges.ContainsKey(color))
                            colorRanges[color] = [];

                        colorRanges[color].Add((i, 1));
                    }
                }

                // Apply colors in batches
                foreach (var kvp in colorRanges)
                {
                    Color color = kvp.Key;
                    
                    // Modify colors based on bonsai health if needed
                    if (currentBonsai.Health < 50 && IsLeafColor(color))
                    {
                        // Make leaves more yellowish/brown for unhealthy trees
                        color = AdjustColorForHealth(color, currentBonsai.Health);
                    }
                    
                    foreach (var (start, length) in kvp.Value)
                    {
                        treeDisplay.SelectionStart = start;
                        treeDisplay.SelectionLength = length;
                        treeDisplay.SelectionColor = color;
                    }
                }

                // Reset selection
                treeDisplay.SelectionStart = 0;
                treeDisplay.SelectionLength = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Optimized color formatting error: {ex.Message}");
            }
        }
        
        private bool IsLeafColor(Color color)
        {
            // Check if the color is one of the leaf colors (green tones)
            return color.G > 100 && color.G > color.R && color.G > color.B;
        }
        
        private Color AdjustColorForHealth(Color originalColor, double health)
        {
            // Health is 0-100, where 100 is perfectly healthy
            // Lower health makes greens more yellow/brown
            
            if (health <= 0)
            {
                // Dead - brown color
                return Color.FromArgb(originalColor.A, 139, 69, 19);
            }
            
            if (health < 30)
            {
                // Very unhealthy - yellowish brown
                return Color.FromArgb(originalColor.A, 
                    Math.Min(255, originalColor.R + 100), 
                    Math.Max(0, originalColor.G - 50), 
                    Math.Max(0, originalColor.B - 20));
            }
            
            if (health < 70)
            {
                // Somewhat unhealthy - yellowish
                return Color.FromArgb(originalColor.A, 
                    Math.Min(255, originalColor.R + 50), 
                    originalColor.G, 
                    originalColor.B);
            }
            
            // Healthy - keep original color
            return originalColor;
        }

        #endregion

        #region Bonsai & Stats Management
        
        private void CreateNewBonsai(string name)
        {
            // Create a new bonsai pet with the given name
            currentBonsai = new BonsaiPet(name, random);
            
            // Hook up event handlers
            currentBonsai.StatsChanged += CurrentBonsai_StatsChanged;
            currentBonsai.NotificationTriggered += CurrentBonsai_NotificationTriggered;
            currentBonsai.StageAdvanced += CurrentBonsai_StageAdvanced;
            
            // Update UI
            UpdateStatsDisplay();
            UpdateNeedsDisplay();
        }

        private void UpdateStatsDisplay()
        {
            try
            {
                if (statsDisplay == null || statsDisplay.IsDisposed || currentBonsai == null) return;

                // Suspend layout to prevent flicker
                statsDisplay.SuspendLayout();

                try
                {
                    statsDisplay.Clear();

                    // Center alignment for all stats text
                    statsDisplay.SelectionAlignment = HorizontalAlignment.Center;

                    // Bonsai name - larger font size and centered
                    statsDisplay.SelectionFont = new Font("Segoe UI", 18F, FontStyle.Bold);
                    statsDisplay.SelectionColor = Color.FromArgb(52, 73, 94);
                    statsDisplay.AppendText($"{currentBonsai.Name}\n\n");

                    // Age section
                    statsDisplay.SelectionFont = new Font("Segoe UI", 14F, FontStyle.Bold);
                    statsDisplay.SelectionColor = Color.FromArgb(149, 165, 166);
                    statsDisplay.AppendText("STAGE\n");
                    statsDisplay.SelectionFont = new Font("Segoe UI", 13F, FontStyle.Regular);
                    statsDisplay.SelectionColor = Color.FromArgb(44, 62, 80);
                    statsDisplay.AppendText($"{currentBonsai.CurrentStage} ({currentBonsai.StageProgress}%)\n\n");

                    // Secondary stats
                    statsDisplay.SelectionFont = new Font("Segoe UI", 14F, FontStyle.Bold);
                    statsDisplay.SelectionColor = Color.FromArgb(52, 152, 219);
                    statsDisplay.AppendText("ADDITIONAL STATS\n");
                    statsDisplay.SelectionFont = new Font("Segoe UI", 13F, FontStyle.Regular);
                    statsDisplay.SelectionColor = Color.FromArgb(44, 62, 80);
                    
                    // Format additional stats
                    statsDisplay.AppendText($"Hydration: {currentBonsai.Hydration:0.0}%\n");
                    statsDisplay.AppendText($"Soil Quality: {currentBonsai.SoilQuality:0.0}%\n");
                    statsDisplay.AppendText($"Pruning Quality: {currentBonsai.PruningQuality:0.0}%\n\n");

                    // Likes section
                    statsDisplay.SelectionFont = new Font("Segoe UI", 14F, FontStyle.Bold);
                    statsDisplay.SelectionColor = Color.FromArgb(46, 204, 113);
                    statsDisplay.AppendText("LIKES\n");
                    statsDisplay.SelectionFont = new Font("Segoe UI", 13F, FontStyle.Regular);
                    statsDisplay.SelectionColor = Color.FromArgb(44, 62, 80);
                    foreach (string like in currentBonsai.Likes)
                    {
                        statsDisplay.AppendText($"{like}\n");
                    }
                    statsDisplay.AppendText("\n");

                    // Dislikes section
                    statsDisplay.SelectionFont = new Font("Segoe UI", 14F, FontStyle.Bold);
                    statsDisplay.SelectionColor = Color.FromArgb(231, 76, 60);
                    statsDisplay.AppendText("DISLIKES\n");
                    statsDisplay.SelectionFont = new Font("Segoe UI", 13F, FontStyle.Regular);
                    statsDisplay.SelectionColor = Color.FromArgb(44, 62, 80);
                    foreach (string dislike in currentBonsai.Dislikes)
                    {
                        statsDisplay.AppendText($"{dislike}\n");
                    }
                    statsDisplay.AppendText("\n");

                    // Care instructions
                    statsDisplay.SelectionFont = new Font("Segoe UI", 12F, FontStyle.Italic);
                    statsDisplay.SelectionColor = Color.FromArgb(127, 140, 141);
                    statsDisplay.AppendText("Take good care of your bonsai! Water it regularly and treat it with love.");
                    
                    // Update age and time labels
                    ageLabel.Text = $"Age: {currentBonsai.Age} days ({currentBonsai.CurrentStage})";
                    UpdateTimeLabel();
                }
                finally
                {
                    statsDisplay.ResumeLayout(true);
                    statsDisplay.Refresh();
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to update stats display", ex);
            }
        }
        
        private void UpdateNeedsDisplay()
        {
            if (currentBonsai == null) return;
            
            // Update progress bars
            SafeInvoke(() =>
            {
                if (healthBar != null && !healthBar.IsDisposed)
                {
                    healthBar.Value = (int)Math.Round(currentBonsai.Health);
                    
                    // Change colors based on value
                    if (currentBonsai.Health < 30)
                        healthBar.ForeColor = Color.FromArgb(231, 76, 60); // Red
                    else if (currentBonsai.Health < 70)
                        healthBar.ForeColor = Color.FromArgb(230, 126, 34); // Orange
                    else
                        healthBar.ForeColor = Color.FromArgb(46, 204, 113); // Green
                }
                
                if (happinessBar != null && !happinessBar.IsDisposed)
                {
                    happinessBar.Value = (int)Math.Round(currentBonsai.Happiness);
                    
                    // Change colors based on value
                    if (currentBonsai.Happiness < 30)
                        happinessBar.ForeColor = Color.FromArgb(231, 76, 60); // Red
                    else if (currentBonsai.Happiness < 70)
                        happinessBar.ForeColor = Color.FromArgb(230, 126, 34); // Orange
                    else
                        happinessBar.ForeColor = Color.FromArgb(52, 152, 219); // Blue
                }
                
                if (hungerBar != null && !hungerBar.IsDisposed)
                {
                    hungerBar.Value = (int)Math.Round(currentBonsai.Hunger);
                    
                    // Change colors based on value (hunger is reverse - high is bad)
                    if (currentBonsai.Hunger > 70)
                        hungerBar.ForeColor = Color.FromArgb(231, 76, 60); // Red
                    else if (currentBonsai.Hunger > 30)
                        hungerBar.ForeColor = Color.FromArgb(230, 126, 34); // Orange
                    else
                        hungerBar.ForeColor = Color.FromArgb(46, 204, 113); // Green
                }
                
                if (growthBar != null && !growthBar.IsDisposed)
                {
                    growthBar.Value = (int)Math.Round(currentBonsai.Growth);
                    growthBar.ForeColor = Color.FromArgb(155, 89, 182); // Purple
                }
            });
        }
        
        private void UpdateTimeLabel()
        {
            if (timeLabel == null || currentBonsai == null) return;
            
            // Format the in-game time
            string timeFormat = $"Game Time: Day {currentBonsai.InGameTime.Day}, {currentBonsai.InGameTime:h:mm tt}";
            timeLabel.Text = timeFormat;
        }
        
        private void AddNotificationToListView(BonsaiNotification notification)
        {
            if (notificationListView == null || notification == null) return;
            
            // Create list item
            var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add(notification.Title);
            item.SubItems.Add(notification.Message);
            
            // Set appropriate icon based on severity
            switch (notification.Severity)
            {
                case NotificationSeverity.Information:
                    item.ImageKey = "Information";
                    break;
                case NotificationSeverity.Warning:
                    item.ImageKey = "Warning";
                    break;
                case NotificationSeverity.Alert:
                case NotificationSeverity.Critical:
                    item.ImageKey = "Error";
                    break;
                case NotificationSeverity.Achievement:
                    item.ImageKey = "Achievement";
                    break;
            }
            
            // Insert at the top
            notificationListView.Items.Insert(0, item);
            
            // Limit the number of notifications shown
            while (notificationListView.Items.Count > 100)
            {
                notificationListView.Items.RemoveAt(notificationListView.Items.Count - 1);
            }
        }
        
        private void UpdateTimeControlButtonsUI(double activeMultiplier)
        {
            // Reset all button colors
            Color defaultColor = Color.FromArgb(52, 73, 94);
            Color activeColor = Color.FromArgb(46, 204, 113);
            
            if (timeNormalButton != null) timeNormalButton.BackColor = defaultColor;
            if (time2xButton != null) time2xButton.BackColor = defaultColor;
            if (time5xButton != null) time5xButton.BackColor = defaultColor;
            if (time10xButton != null) time10xButton.BackColor = defaultColor;
            
            // Highlight the active button
            if (activeMultiplier == 1.0 && timeNormalButton != null)
                timeNormalButton.BackColor = activeColor;
            else if (activeMultiplier == 2.0 && time2xButton != null)
                time2xButton.BackColor = activeColor;
            else if (activeMultiplier == 5.0 && time5xButton != null)
                time5xButton.BackColor = activeColor;
            else if (activeMultiplier == 10.0 && time10xButton != null)
                time10xButton.BackColor = activeColor;
        }

        #endregion

        #region Event Handlers
        
        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (currentBonsai == null) return;
                
                // Calculate elapsed time since last update
                TimeSpan elapsed = DateTime.Now - lastUpdateTime;
                lastUpdateTime = DateTime.Now;
                
                // Update bonsai state
                currentBonsai.Update(elapsed);
                
                // Check if tree appearance needs updating due to status changes
                if (currentBonsai.IsSick || currentBonsai.IsDead)
                {
                    _ = GenerateTreeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Game timer error: {ex.Message}");
            }
        }
        
        private void UiUpdateTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (currentBonsai == null) return;
                
                // Update the time display
                UpdateTimeLabel();
                
                // Update needs display
                UpdateNeedsDisplay();
                
                // Update age and stage display
                ageLabel.Text = $"Age: {currentBonsai.Age} days ({currentBonsai.CurrentStage})";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI update timer error: {ex.Message}");
            }
        }
        
        private void CurrentBonsai_StatsChanged(object? sender, EventArgs e)
        {
            // Handle stats changes
            SafeInvoke(() =>
            {
                UpdateNeedsDisplay();
                if (currentBonsai.IsDead)
                {
                    // Disable all care buttons
                    waterButton.Enabled = false;
                    feedButton.Enabled = false;
                    pruneButton.Enabled = false;
                    repotButton.Enabled = false;
                    playButton.Enabled = false;
                    
                    // Show death message
                    MessageBox.Show($"Oh no! {currentBonsai.Name} has died. You can adopt a new bonsai to continue playing.",
                        "Bonsai Death", MessageBoxButtons.OK, MessageBoxIcon.Sad);
                }
            });
        }
        
        private void CurrentBonsai_NotificationTriggered(object? sender, BonsaiNotification e)
        {
            // Add notification to the list
            SafeInvoke(() => AddNotificationToListView(e));
            
            // Show system notification for important alerts
            if (e.Severity == NotificationSeverity.Critical || e.Severity == NotificationSeverity.Alert)
            {
                SafeInvoke(() => MessageBox.Show(e.Message, e.Title, MessageBoxButtons.OK, 
                    e.Severity == NotificationSeverity.Critical ? MessageBoxIcon.Error : MessageBoxIcon.Warning));
            }
        }
        
        private void CurrentBonsai_StageAdvanced(object? sender, EventArgs e)
        {
            // Handle stage advancement
            SafeInvoke(() =>
            {
                // Update the tree display to match the new stage
                _ = GenerateTreeAsync();
                
                // Show achievement message
                MessageBox.Show($"Congratulations! {currentBonsai.Name} has grown to a new stage: {currentBonsai.CurrentStage}!",
                    "Growth Milestone", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        private async void AdoptButton_Click(object? sender, EventArgs e)
        {
            try
            {
                using var nameDialog = new Form
                {
                    Width = 300,
                    Height = 150,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = "Name Your New Bonsai",
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var nameLabel = new Label
                {
                    Text = "Enter a name for your bonsai:",
                    Left = 20,
                    Top = 20,
                    Width = 260
                };

                var nameTextBox = new TextBox
                {
                    Left = 20,
                    Top = 50,
                    Width = 260,
                    Text = "Bonsy"
                };

                var confirmButton = new Button
                {
                    Text = "Adopt",
                    Left = 110,
                    Top = 80,
                    DialogResult = DialogResult.OK
                };

                nameDialog.Controls.AddRange([nameLabel, nameTextBox, confirmButton]);
                nameDialog.AcceptButton = confirmButton;

                if (nameDialog.ShowDialog() == DialogResult.OK)
                {
                    string name = nameTextBox.Text.Trim();
                    if (string.IsNullOrEmpty(name)) name = "Bonsy";

                    CreateNewBonsai(name);
                    await GenerateTreeAsync();

                    // Clear notifications
                    notificationListView?.Items.Clear();
                    
                    // Add welcome notification
                    AddNotificationToListView(new BonsaiNotification(
                        "Welcome!", 
                        $"Welcome to BonsaiGotchi! Your new bonsai {name} needs your care to grow.",
                        NotificationSeverity.Information));
                    
                    // Enable all buttons
                    waterButton.Enabled = true;
                    feedButton.Enabled = true;
                    pruneButton.Enabled = true;
                    repotButton.Enabled = true;
                    playButton.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to adopt new bonsai", ex);
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (currentBonsai == null)
                {
                    MessageBox.Show("No bonsai to save. Please adopt a bonsai first.", "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var saveDialog = new SaveFileDialog
                {
                    Filter = "BonsaiGotchi Files (*.bonsai)|*.bonsai|All files (*.*)|*.*",
                    DefaultExt = "bonsai",
                    FileName = $"{currentBonsai.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.bonsai",
                    Title = "Save Your Bonsai Pet"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    SetUIGenerating(true);
                    currentBonsai.SaveToFile(saveDialog.FileName);
                    SetUIGenerating(false);

                    MessageBox.Show($"Your bonsai {currentBonsai.Name} saved successfully!",
                        "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                SetUIGenerating(false);
                ShowError("Failed to save bonsai", ex);
            }
        }

        private async void LoadButton_Click(object? sender, EventArgs e)
        {
            try
            {
                using var openDialog = new OpenFileDialog
                {
                    Filter = "BonsaiGotchi Files (*.bonsai)|*.bonsai|All files (*.*)|*.*",
                    DefaultExt = "bonsai",
                    Title = "Load Bonsai Pet"
                };

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    SetUIGenerating(true);
                    
                    try
                    {
                        // Load the bonsai data
                        currentBonsai = BonsaiPet.LoadFromFile(openDialog.FileName);
                        
                        // Hook up event handlers
                        currentBonsai.StatsChanged += CurrentBonsai_StatsChanged;
                        currentBonsai.NotificationTriggered += CurrentBonsai_NotificationTriggered;
                        currentBonsai.StageAdvanced += CurrentBonsai_StageAdvanced;
                        
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
            }
            catch (Exception ex)
            {
                SetUIGenerating(false);
                ShowError("Failed to load bonsai", ex);
            }
        }

        private void WaterButton_Click(object? sender, EventArgs e)
        {
            try
            {
                StartRainEffect();
                
                // Water the bonsai
                currentBonsai?.Water();
            }
            catch (Exception ex)
            {
                ShowError("Failed to water bonsai", ex);
            }
        }

        private void FeedButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Feed the bonsai
                currentBonsai?.Feed();
                
                // Visual feedback
                MessageBox.Show($"You've fertilized {currentBonsai?.Name}! The soil is now enriched with nutrients.",
                    "Feeding Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Failed to feed bonsai", ex);
            }
        }

        private void PruneButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Prune the bonsai
                currentBonsai?.Prune();
                
                // Visual feedback
                MessageBox.Show($"You've carefully pruned {currentBonsai?.Name}! The shape is now more refined.",
                    "Pruning Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Update the tree to show pruning effect
                _ = GenerateTreeAsync();
            }
            catch (Exception ex)
            {
                ShowError("Failed to prune bonsai", ex);
            }
        }

        private void RepotButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Confirm repotting
                var result = MessageBox.Show(
                    $"Are you sure you want to repot {currentBonsai?.Name}? This can be stressful for the tree if done too frequently.",
                    "Confirm Repotting",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // Repot the bonsai
                    currentBonsai?.Repot();
                    
                    // Update the tree to show new pot
                    _ = GenerateTreeAsync();
                }
            }
            catch (Exception ex)
            {
                ShowError("Failed to repot bonsai", ex);
            }
        }

        private void PlayButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Simple mini-game - Leaf counting
                Random miniGameRandom = new();
                int leafCount = miniGameRandom.Next(5, 20);
                
                using var gameForm = new Form
                {
                    Width = 400,
                    Height = 250,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = "Play with Your Bonsai",
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var gameLabel = new Label
                {
                    Text = $"How many leaves can you see on your bonsai?\nGuess the number between 1-20!",
                    Left = 20,
                    Top = 20,
                    Width = 360,
                    Height = 40
                };

                var guessNumeric = new NumericUpDown
                {
                    Left = 150,
                    Top = 80,
                    Width = 100,
                    Minimum = 1,
                    Maximum = 20,
                    Value = 10
                };

                var guessButton = new Button
                {
                    Text = "Submit Guess",
                    Left = 150,
                    Top = 120,
                    Width = 100,
                    Height = 30
                };
                
                var resultLabel = new Label
                {
                    Text = "",
                    Left = 20,
                    Top = 170,
                    Width = 360,
                    Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                guessButton.Click += (s, args) =>
                {
                    int guess = (int)guessNumeric.Value;
                    int difference = Math.Abs(guess - leafCount);
                    
                    double score;
                    if (difference == 0)
                    {
                        // Perfect!
                        score = 100;
                        resultLabel.Text = $"Perfect guess! There were exactly {leafCount} leaves!";
                    }
                    else if (difference <= 2)
                    {
                        // Very close
                        score = 90;
                        resultLabel.Text = $"Very close! There were {leafCount} leaves.";
                    }
                    else if (difference <= 5)
                    {
                        // Close
                        score = 70;
                        resultLabel.Text = $"Good try! There were {leafCount} leaves.";
                    }
                    else
                    {
                        // Not close
                        score = 50;
                        resultLabel.Text = $"Not quite! There were {leafCount} leaves.";
                    }
                    
                    // Apply score to bonsai
                    currentBonsai?.Play(score);
                    
                    // Disable controls after guess
                    guessButton.Enabled = false;
                    guessNumeric.Enabled = false;
                    
                    // Auto-close after delay
                    var closeTimer = new System.Windows.Forms.Timer { Interval = 2000 };
                    closeTimer.Tick += (sender, e) => { gameForm.Close(); closeTimer.Stop(); };
                    closeTimer.Start();
                };

                gameForm.Controls.AddRange([gameLabel, guessNumeric, guessButton, resultLabel]);
                gameForm.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("Mini-game error", ex);
            }
        }
        
        private void TimeMultiplierButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is double multiplier && currentBonsai != null)
            {
                // Set the new time multiplier
                currentBonsai.SetTimeMultiplier(multiplier);
                
                // Update button UI
                UpdateTimeControlButtonsUI(multiplier);
                
                // Add notification
                AddNotificationToListView(new BonsaiNotification(
                    "Time Speed Changed", 
                    $"Game time now runs at {multiplier}× speed.",
                    NotificationSeverity.Information));
            }
        }

        #endregion

        #region Enhanced Rain Effect

        private void StartRainEffect()
        {
            try
            {
                if (treeDisplay == null || treeDisplay.IsDisposed) return;

                // Initialize dramatic rain drops
                rainDrops = [];
                for (int i = 0; i < RAIN_DROP_COUNT; i++)
                {
                    rainDrops.Add(new RainDrop
                    {
                        X = random.Next(TREE_WIDTH),
                        Y = random.Next(-20, 0),
                        Speed = random.Next(2, 5),
                        Character = GetRandomRainCharacter(),
                        Intensity = random.NextDouble()
                    });
                }

                rainFrameCount = 0;

                // Start enhanced rain timer
                rainTimer?.Dispose();
                rainTimer = new System.Windows.Forms.Timer
                {
                    Interval = RAIN_INTERVAL
                };
                rainTimer.Tick += RainTimer_Tick;
                rainTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Enhanced rain effect error: {ex.Message}");
            }
        }

        private char GetRandomRainCharacter()
        {
            // Various rain drop characters for more dramatic effect
            char[] rainChars = ['|', '¦', '│', '┃', '║', '∣', '⎮', '❘'];
            return rainChars[random.Next(rainChars.Length)];
        }

        private void RainTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (rainDrops == null || currentTree == null) return;

                rainFrameCount++;

                // Longer dramatic rain duration - 40 frames at 50ms each = 2 seconds
                if (rainFrameCount >= RAIN_DURATION_FRAMES)
                {
                    rainTimer?.Stop();
                    rainTimer?.Dispose();
                    rainTimer = null;

                    // Restore original tree with optimized rendering
                    if (currentTree != null)
                    {
                        var treeText = ConvertTreeToString(currentTree);
                        SafeInvoke(() =>
                        {
                            if (treeDisplay != null && !treeDisplay.IsDisposed)
                            {
                                treeDisplay.SuspendLayout();
                                try
                                {
                                    treeDisplay.Clear();
                                    treeDisplay.Text = treeText;
                                    ApplyColorFormattingOptimized();
                                }
                                finally
                                {
                                    treeDisplay.ResumeLayout(true);
                                    treeDisplay.Refresh();
                                }
                            }
                        });
                    }
                    return;
                }

                // Create dramatic rain frame
                var rainCanvas = (char[,])currentTree.Clone();

                // Update and draw enhanced rain drops
                foreach (var drop in rainDrops)
                {
                    drop.Y += drop.Speed;

                    // Reset drop if it goes off screen - with varied starting positions
                    if (drop.Y >= TREE_HEIGHT)
                    {
                        drop.Y = random.Next(-20, -5); // Higher starting position
                        drop.X = random.Next(TREE_WIDTH);
                        drop.Speed = random.Next(2, 5); // Re-randomize speed
                        drop.Character = GetRandomRainCharacter(); // New character
                        drop.Intensity = random.NextDouble(); // New intensity
                    }

                    // Draw multiple drops per position for intensity
                    for (int offset = 0; offset < 3; offset++)
                    {
                        int dropY = drop.Y - offset;
                        if (dropY >= 0 && dropY < TREE_HEIGHT && drop.X >= 0 && drop.X < TREE_WIDTH)
                        {
                            if (rainCanvas[dropY, drop.X] == ' ')
                            {
                                // Use different characters based on position for trail effect
                                char rainChar = offset == 0 ? drop.Character :
                                               offset == 1 ? '˙' :
                                               '·';
                                rainCanvas[dropY, drop.X] = rainChar;
                            }
                        }
                    }
                }

                // Add splash effects at ground level
                for (int x = 0; x < TREE_WIDTH; x++)
                {
                    if (random.NextDouble() > 0.7) // 30% chance of splash
                    {
                        int splashY = TREE_HEIGHT - 1;
                        if (splashY >= 0 && rainCanvas[splashY, x] == ' ')
                        {
                            rainCanvas[splashY, x] = random.NextDouble() > 0.5 ? '∶' : '˙';
                        }
                    }
                }

                // Update display with enhanced rendering
                var rainText = ConvertTreeToString(rainCanvas);
                SafeInvoke(() =>
                {
                    if (treeDisplay != null && !treeDisplay.IsDisposed)
                    {
                        treeDisplay.SuspendLayout();
                        try
                        {
                            treeDisplay.Clear();
                            treeDisplay.Text = rainText;

                            // Apply original tree colors first
                            ApplyColorFormattingOptimized();

                            // Then apply enhanced rain colors
                            ApplyEnhancedRainColors();
                        }
                        finally
                        {
                            treeDisplay.ResumeLayout(true);
                            treeDisplay.Refresh();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Enhanced rain timer error: {ex.Message}");
            }
        }

        private void ApplyEnhancedRainColors()
        {
            try
            {
                if (treeDisplay == null || treeDisplay.IsDisposed) return;

                string text = treeDisplay.Text;
                char[] rainChars = ['|', '¦', '│', '┃', '║', '∣', '⎮', '❘', '˙', '·', '∶'];

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (rainChars.Contains(c))
                    {
                        treeDisplay.SelectionStart = i;
                        treeDisplay.SelectionLength = 1;

                        // Enhanced rain colors - blues with intensity variation
                        Color rainColor = c switch
                        {
                            '|' or '¦' or '│' or '┃' or '║' or '∣' or '⎮' or '❘' => Color.FromArgb(52, 152, 219), // Main rain blue
                            '˙' or '·' => Color.FromArgb(174, 214, 241), // Light splash blue
                            '∶' => Color.FromArgb(93, 173, 226), // Medium splash blue
                            _ => Color.FromArgb(52, 152, 219)
                        };

                        treeDisplay.SelectionColor = rainColor;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Enhanced rain color error: {ex.Message}");
            }
        }

        private class RainDrop
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Speed { get; set; }
            public char Character { get; set; } = '|';
            public double Intensity { get; set; } = 1.0;
        }

        #endregion

        #region Helper Methods

        private void UpdateProgress(int progress)
        {
            SafeInvoke(() =>
            {
                if (progressBar != null && !progressBar.IsDisposed && progressBar.IsHandleCreated)
                {
                    progressBar.Value = Math.Min(progress, 100);
                }
            });
        }

        private void SetUIGenerating(bool generating)
        {
            SafeInvoke(() =>
            {
                if (adoptButton != null && !adoptButton.IsDisposed && adoptButton.IsHandleCreated)
                {
                    adoptButton.Enabled = !generating;
                    adoptButton.Text = generating ? "Growing..." : "Adopt New Bonsai";
                }

                if (saveButton != null && !saveButton.IsDisposed && saveButton.IsHandleCreated)
                {
                    saveButton.Enabled = !generating;
                }
                
                if (loadButton != null && !loadButton.IsDisposed && loadButton.IsHandleCreated)
                {
                    loadButton.Enabled = !generating;
                }

                if (progressBar != null && !progressBar.IsDisposed && progressBar.IsHandleCreated)
                {
                    progressBar.Visible = generating;
                    if (generating)
                    {
                        progressBar.Value = 0;
                    }
                }
            });
        }

        private void SafeInvoke(Action action)
        {
            try
            {
                if (!isFormReady) return;

                if (InvokeRequired)
                {
                    try
                    {
                        BeginInvoke(action);
                    }
                    catch (InvalidOperationException)
                    {
                        // Handle may not be created yet, ignore
                    }
                }
                else
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeInvoke error: {ex.Message}");
            }
        }

        private void ShowError(string message, Exception ex)
        {
            SafeInvoke(() =>
            {
                var errorMessage = $"{message}\n\nError: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
                }

                MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"Error: {message} - {ex}");
            });
        }

        #endregion

        #region Resource Management

        private void ReleaseFormResources()
        {
            try
            {
                isFormReady = false;

                // Stop timers
                gameTimer?.Stop();
                gameTimer?.Dispose();
                
                uiUpdateTimer?.Stop();
                uiUpdateTimer?.Dispose();
                
                // Stop rain timer
                rainTimer?.Stop();
                rainTimer?.Dispose();
                
                // Stop threads
                cancellationTokenSource?.Cancel();
                Thread.Sleep(100);
                cancellationTokenSource?.Dispose();

                // Dispose UI components
                treeDisplay?.Dispose();
                treePanel?.Dispose();
                statsPanel?.Dispose();
                statsDisplay?.Dispose();
                needsPanel?.Dispose();
                notificationPanel?.Dispose();
                notificationListView?.Dispose();
                timeControlPanel?.Dispose();
                adoptButton?.Dispose();
                saveButton?.Dispose();
                loadButton?.Dispose();
                waterButton?.Dispose();
                feedButton?.Dispose();
                pruneButton?.Dispose();
                repotButton?.Dispose();
                playButton?.Dispose();
                timeNormalButton?.Dispose();
                time2xButton?.Dispose();
                time5xButton?.Dispose();
                time10xButton?.Dispose();
                statusLabel?.Dispose();
                timeLabel?.Dispose();
                ageLabel?.Dispose();
                progressBar?.Dispose();
                healthBar?.Dispose();
                happinessBar?.Dispose();
                hungerBar?.Dispose();
                growthBar?.Dispose();
                mainContainer?.Dispose();

                disposed = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resource cleanup error: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                ReleaseFormResources();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}