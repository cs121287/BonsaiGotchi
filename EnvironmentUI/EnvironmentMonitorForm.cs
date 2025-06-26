using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using BonsaiGotchi.EnvironmentSystem;

namespace BonsaiGotchi.EnvironmentUI
{
    /// <summary>
    /// Form for monitoring and adjusting environmental factors
    /// </summary>
    public partial class EnvironmentMonitorForm : Form
    {
        private readonly EnvironmentManager environmentManager;
        
        // UI components
        private TabControl tabControl;
        private Panel currentConditionsPanel;
        private Panel forecastPanel;
        private Panel eventPanel;
        private Panel climateControlPanel;
        
        // Environment display elements
        private Label seasonLabel;
        private Label weatherLabel;
        private Label timeLabel;
        private Label temperatureLabel;
        private Label humidityLabel;
        private Label lightQualityLabel;
        private Label soilQualityLabel;
        private Label airQualityLabel;
        private PictureBox weatherIcon;
        
        // Environment gauges
        private List<ProgressBar> environmentGauges = new List<ProgressBar>();
        
        // Event components
        private ListView eventListView;
        
        // Image caches
        private Dictionary<Weather, Image> weatherIcons = new Dictionary<Weather, Image>();
        private Dictionary<TimeOfDay, Image> timeIcons = new Dictionary<TimeOfDay, Image>();
        private Dictionary<Season, Image> seasonIcons = new Dictionary<Season, Image>();
        
        /// <summary>
        /// Initialize the environment monitor form
        /// </summary>
        public EnvironmentMonitorForm(EnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
            
            InitializeComponent();
            InitializeEnvironmentUI();
            InitializeIconCache();
            
            // Subscribe to environment events
            SubscribeToEnvironmentEvents();
            
            // Update display with current values
            UpdateEnvironmentDisplay();
        }
        
        /// <summary>
        /// Initialize the form's components
        /// </summary>
        private void InitializeComponent()
        {
            // Form setup
            Text = "Environment Monitor";
            Size = new Size(800, 600);
            MinimumSize = new Size(750, 550);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 249, 250);
            
            // Create tab control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(10, 5),
                Font = new Font("Segoe UI", 11F, FontStyle.Regular)
            };
            
            // Create tabs
            TabPage currentConditionsTab = new TabPage("Current Conditions")
            {
                BackColor = Color.FromArgb(248, 249, 250)
            };
            
            TabPage forecastTab = new TabPage("Weather Forecast")
            {
                BackColor = Color.FromArgb(248, 249, 250)
            };
            
            TabPage eventTab = new TabPage("Environmental Events")
            {
                BackColor = Color.FromArgb(248, 249, 250)
            };
            
            TabPage climateControlTab = new TabPage("Climate Control")
            {
                BackColor = Color.FromArgb(248, 249, 250)
            };
            
            tabControl.TabPages.Add(currentConditionsTab);
            tabControl.TabPages.Add(forecastTab);
            tabControl.TabPages.Add(eventTab);
            tabControl.TabPages.Add(climateControlTab);
            
            // Set up tabs
            SetupCurrentConditionsTab(currentConditionsTab);
            SetupForecastTab(forecastTab);
            SetupEventTab(eventTab);
            SetupClimateControlTab(climateControlTab);
            
            Controls.Add(tabControl);
            
            // Add event handlers
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
        }
        
        /// <summary>
        /// Initialize the environment UI
        /// </summary>
        private void InitializeEnvironmentUI()
        {
            // Setup timer to update UI periodically
            Timer updateTimer = new Timer
            {
                Interval = 5000, // Update every 5 seconds
                Enabled = true
            };
            updateTimer.Tick += (s, e) => UpdateEnvironmentDisplay();
        }
        
        /// <summary>
        /// Initialize icon cache
        /// </summary>
        private void InitializeIconCache()
        {
            // In a real application, you would load actual icons from resources
            // Here we'll create simple colored rectangles as placeholders
            
            // Weather icons
            weatherIcons[Weather.Sunny] = CreateIconPlaceholder(Color.Yellow);
            weatherIcons[Weather.Cloudy] = CreateIconPlaceholder(Color.LightGray);
            weatherIcons[Weather.Rain] = CreateIconPlaceholder(Color.SteelBlue);
            weatherIcons[Weather.Humid] = CreateIconPlaceholder(Color.LightBlue);
            weatherIcons[Weather.Wind] = CreateIconPlaceholder(Color.WhiteSmoke);
            weatherIcons[Weather.Storm] = CreateIconPlaceholder(Color.DarkSlateBlue);
            weatherIcons[Weather.Snow] = CreateIconPlaceholder(Color.White);
            
            // Time icons
            timeIcons[TimeOfDay.Morning] = CreateIconPlaceholder(Color.LightYellow);
            timeIcons[TimeOfDay.Day] = CreateIconPlaceholder(Color.Yellow);
            timeIcons[TimeOfDay.Evening] = CreateIconPlaceholder(Color.Orange);
            timeIcons[TimeOfDay.Night] = CreateIconPlaceholder(Color.MidnightBlue);
            
            // Season icons
            seasonIcons[Season.Spring] = CreateIconPlaceholder(Color.LightGreen);
            seasonIcons[Season.Summer] = CreateIconPlaceholder(Color.ForestGreen);
            seasonIcons[Season.Autumn] = CreateIconPlaceholder(Color.Orange);
            seasonIcons[Season.Winter] = CreateIconPlaceholder(Color.LightBlue);
        }
        
        /// <summary>
        /// Create a placeholder icon
        /// </summary>
        private Image CreateIconPlaceholder(Color color)
        {
            Bitmap icon = new Bitmap(32, 32);
            using Graphics g = Graphics.FromImage(icon);
            using SolidBrush brush = new SolidBrush(color);
            
            g.Clear(Color.Transparent);
            g.FillRectangle(brush, 0, 0, 32, 32);
            
            return icon;
        }
        
        #region Tab Setup
        
        /// <summary>
        /// Set up the current conditions tab
        /// </summary>
        private void SetupCurrentConditionsTab(TabPage tab)
        {
            // Create main container
            currentConditionsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };
            
            // Create top section with season, weather, and time
            TableLayoutPanel topSection = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 150,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10)
            };
            
            topSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            topSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            topSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            
            // Season panel
            GroupBox seasonBox = new GroupBox
            {
                Text = "Season",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            
            seasonLabel = new Label
            {
                Text = "Spring",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            
            // Progress bar for season progress
            ProgressBar seasonProgress = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 15,
                Style = ProgressBarStyle.Continuous,
                Value = 30
            };
            
            seasonBox.Controls.Add(seasonLabel);
            seasonBox.Controls.Add(seasonProgress);
            environmentGauges.Add(seasonProgress);
            
            // Weather panel
            GroupBox weatherBox = new GroupBox
            {
                Text = "Weather",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            
            TableLayoutPanel weatherLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            weatherLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            weatherLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            
            weatherIcon = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            
            weatherLabel = new Label
            {
                Text = "Sunny",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            
            weatherLayout.Controls.Add(weatherIcon, 0, 0);
            weatherLayout.Controls.Add(weatherLabel, 1, 0);
            weatherBox.Controls.Add(weatherLayout);
            
            // Time panel
            GroupBox timeBox = new GroupBox
            {
                Text = "Time of Day",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            
            timeLabel = new Label
            {
                Text = "Day 1, 12:00 PM",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            
            // Progress bar for day progress
            ProgressBar dayProgress = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 15,
                Style = ProgressBarStyle.Continuous,
                Value = 50
            };
            
            timeBox.Controls.Add(timeLabel);
            timeBox.Controls.Add(dayProgress);
            environmentGauges.Add(dayProgress);
            
            // Add to top section
            topSection.Controls.Add(seasonBox, 0, 0);
            topSection.Controls.Add(weatherBox, 1, 0);
            topSection.Controls.Add(timeBox, 2, 0);
            
            // Create environmental factors section
            GroupBox environmentalFactors = new GroupBox
            {
                Text = "Environmental Factors",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Padding = new Padding(15)
            };
            
            TableLayoutPanel factorsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                RowStyles = {
                    new RowStyle(SizeType.Percent, 20F),
                    new RowStyle(SizeType.Percent, 20F),
                    new RowStyle(SizeType.Percent, 20F),
                    new RowStyle(SizeType.Percent, 20F),
                    new RowStyle(SizeType.Percent, 20F)
                }
            };
            
            // Temperature
            temperatureLabel = new Label
            {
                Text = "Temperature: 72°F",
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            ProgressBar temperatureBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Value = 50
            };
            environmentGauges.Add(temperatureBar);
            
            // Humidity
            humidityLabel = new Label
            {
                Text = "Humidity: 50%",
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            ProgressBar humidityBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Value = 50
            };
            environmentGauges.Add(humidityBar);
            
            // Light quality
            lightQualityLabel = new Label
            {
                Text = "Light Quality: 100%",
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            ProgressBar lightQualityBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Value = 100
            };
            environmentGauges.Add(lightQualityBar);
            
            // Soil quality
            soilQualityLabel = new Label
            {
                Text = "Soil Quality: 100%",
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            ProgressBar soilQualityBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Value = 100
            };
            environmentGauges.Add(soilQualityBar);
            
            // Air quality
            airQualityLabel = new Label
            {
                Text = "Air Quality: 100%",
                Font = new Font("Segoe UI", 11F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            ProgressBar airQualityBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Style = ProgressBarStyle.Continuous,
                Value = 100
            };
            environmentGauges.Add(airQualityBar);
            
            // Add to factors layout
            factorsLayout.Controls.Add(temperatureLabel, 0, 0);
            factorsLayout.Controls.Add(temperatureBar, 1, 0);
            factorsLayout.Controls.Add(humidityLabel, 0, 1);
            factorsLayout.Controls.Add(humidityBar, 1, 1);
            factorsLayout.Controls.Add(lightQualityLabel, 0, 2);
            factorsLayout.Controls.Add(lightQualityBar, 1, 2);
            factorsLayout.Controls.Add(soilQualityLabel, 0, 3);
            factorsLayout.Controls.Add(soilQualityBar, 1, 3);
            factorsLayout.Controls.Add(airQualityLabel, 0, 4);
            factorsLayout.Controls.Add(airQualityBar, 1, 4);
            
            // Add to environmental factors
            environmentalFactors.Controls.Add(factorsLayout);
            
            // Add to main layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                RowStyles = {
                    new RowStyle(SizeType.Absolute, 150),
                    new RowStyle(SizeType.Percent, 100)
                },
                Padding = new Padding(10)
            };
            
            mainLayout.Controls.Add(topSection, 0, 0);
            mainLayout.Controls.Add(environmentalFactors, 0, 1);
            
            currentConditionsPanel.Controls.Add(mainLayout);
            tab.Controls.Add(currentConditionsPanel);
        }
        
        /// <summary>
        /// Set up the forecast tab
        /// </summary>
        private void SetupForecastTab(TabPage tab)
        {
            // Create forecast panel
            forecastPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };
            
            // Create forecast instructions
            Label instructions = new Label
            {
                Text = "Weather Forecast for the Next 3 Days",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            // Create forecast list
            TableLayoutPanel forecastLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4, // Current + 3 days
                Padding = new Padding(10),
                AutoScroll = true
            };
            
            forecastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            forecastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            forecastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            forecastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            
            // Create forecast day panels
            for (int i = 0; i < 4; i++)
            {
                Panel dayPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(0, 5, 0, 5)
                };
                
                Label dayLabel = new Label
                {
                    Text = i == 0 ? "Today" : $"Day {i}",
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    Height = 30,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(52, 152, 219),
                    ForeColor = Color.White
                };
                
                TableLayoutPanel forecastDetails = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3,
                    RowCount = 2,
                    Padding = new Padding(10)
                };
                
                // Weather icon
                PictureBox dayWeatherIcon = new PictureBox
                {
                    Size = new Size(50, 50),
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Dock = DockStyle.Fill,
                    Tag = $"day{i}WeatherIcon"
                };
                
                // Weather label
                Label dayWeatherLabel = new Label
                {
                    Text = "Weather: Sunny",
                    Font = new Font("Segoe UI", 11F),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = $"day{i}WeatherLabel"
                };
                
                // Temperature label
                Label dayTempLabel = new Label
                {
                    Text = "Temperature: 72°F",
                    Font = new Font("Segoe UI", 11F),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = $"day{i}TempLabel"
                };
                
                // Probability label
                Label probabilityLabel = new Label
                {
                    Text = "Forecast Accuracy: 100%",
                    Font = new Font("Segoe UI", 11F),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = $"day{i}ProbabilityLabel"
                };
                
                // Add to forecast details
                forecastDetails.Controls.Add(dayWeatherIcon, 0, 0);
                forecastDetails.Controls.Add(dayWeatherLabel, 1, 0);
                forecastDetails.Controls.Add(dayTempLabel, 2, 0);
                forecastDetails.Controls.Add(probabilityLabel, 1, 1);
                forecastDetails.SetColumnSpan(probabilityLabel, 2);
                
                // Add to day panel
                dayPanel.Controls.Add(forecastDetails);
                dayPanel.Controls.Add(dayLabel);
                
                // Add to forecast layout
                forecastLayout.Controls.Add(dayPanel, 0, i);
            }
            
            // Add to forecast panel
            forecastPanel.Controls.Add(forecastLayout);
            forecastPanel.Controls.Add(instructions);
            
            // Add to tab
            tab.Controls.Add(forecastPanel);
        }
        
        /// <summary>
        /// Set up the events tab
        /// </summary>
        private void SetupEventTab(TabPage tab)
        {
            // Create events panel
            eventPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };
            
            // Create split container
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 350,
                Panel1MinSize = 200,
                Panel2MinSize = 150
            };
            
            // Active events group
            GroupBox activeEventsGroup = new GroupBox
            {
                Text = "Active Environmental Events",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Padding = new Padding(10)
            };
            
            // Active events list view
            eventListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                GridLines = true,
                Font = new Font("Segoe UI", 10F)
            };
            
            // Add columns
            eventListView.Columns.Add("Event", 200);
            eventListView.Columns.Add("Intensity", 100);
            eventListView.Columns.Add("Start Time", 150);
            eventListView.Columns.Add("Duration", 100);
            eventListView.Columns.Add("Ends In", 100);
            
            activeEventsGroup.Controls.Add(eventListView);
            splitContainer.Panel1.Controls.Add(activeEventsGroup);
            
            // Upcoming events group
            GroupBox upcomingEventsGroup = new GroupBox
            {
                Text = "Upcoming Environmental Events",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Padding = new Padding(10)
            };
            
            // Upcoming events list view
            ListView upcomingEventsListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                GridLines = true,
                Font = new Font("Segoe UI", 10F)
            };
            
            // Add columns
            upcomingEventsListView.Columns.Add("Event", 200);
            upcomingEventsListView.Columns.Add("Intensity", 100);
            upcomingEventsListView.Columns.Add("Starts In", 150);
            upcomingEventsListView.Columns.Add("Duration", 100);
            
            upcomingEventsGroup.Controls.Add(upcomingEventsListView);
            splitContainer.Panel2.Controls.Add(upcomingEventsGroup);
            
            // Add to event panel
            eventPanel.Controls.Add(splitContainer);
            
            // Add to tab
            tab.Controls.Add(eventPanel);
        }
        
        /// <summary>
        /// Set up the climate control tab
        /// </summary>
        private void SetupClimateControlTab(TabPage tab)
        {
            // Create climate control panel
            climateControlPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };
            
            // Create climate zone selector
            GroupBox climateZoneGroup = new GroupBox
            {
                Text = "Climate Zone",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Height = 150,
                Padding = new Padding(10)
            };
            
            Label climateDescription = new Label
            {
                Text = "Select a climate zone for your bonsai. Different zones have different weather patterns and challenges.",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(20, 30)
            };
            
            // Create climate zone buttons
            FlowLayoutPanel climateButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };
            
            string[] climateNames = { "Temperate", "Tropical", "Desert", "Alpine" };
            foreach (string climateName in climateNames)
            {
                Button climateButton = new Button
                {
                    Text = climateName,
                    Size = new Size(120, 40),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(10, 5, 10, 5),
                    Tag = climateName
                };
                
                // Set colors based on climate
                switch (climateName)
                {
                    case "Temperate":
                        climateButton.BackColor = Color.FromArgb(52, 152, 219);
                        break;
                    case "Tropical":
                        climateButton.BackColor = Color.FromArgb(46, 204, 113);
                        break;
                    case "Desert":
                        climateButton.BackColor = Color.FromArgb(243, 156, 18);
                        break;
                    case "Alpine":
                        climateButton.BackColor = Color.FromArgb(155, 89, 182);
                        break;
                }
                
                climateButton.ForeColor = Color.White;
                climateButton.FlatAppearance.BorderSize = 0;
                climateButton.Click += ClimateButton_Click;
                
                climateButtonsPanel.Controls.Add(climateButton);
            }
            
            climateZoneGroup.Controls.Add(climateDescription);
            climateZoneGroup.Controls.Add(climateButtonsPanel);
            
            // Create time control group
            GroupBox timeControlGroup = new GroupBox
            {
                Text = "Time Controls",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Height = 150,
                Top = 160,
                Padding = new Padding(10)
            };
            
            Label timeControlDescription = new Label
            {
                Text = "Control the speed at which time passes in the game world.",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(20, 30)
            };
            
            // Create speed slider
            Label speedLabel = new Label
            {
                Text = "Time Speed Multiplier: 1x",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Location = new Point(20, 60)
            };
            
            TrackBar speedSlider = new TrackBar
            {
                Minimum = 1,
                Maximum = 100,
                LargeChange = 10,
                SmallChange = 1,
                Value = 1,
                Width = 400,
                Location = new Point(20, 85),
                TickFrequency = 10,
                TickStyle = TickStyle.Both
            };
            
            speedSlider.ValueChanged += (s, e) =>
            {
                speedLabel.Text = $"Time Speed Multiplier: {speedSlider.Value}x";
                if (environmentManager != null)
                {
                    environmentManager.SetTimeMultiplier(speedSlider.Value);
                }
            };
            
            Button applyButton = new Button
            {
                Text = "Apply",
                Size = new Size(80, 30),
                Location = new Point(440, 85),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            applyButton.FlatAppearance.BorderSize = 0;
            
            timeControlGroup.Controls.Add(timeControlDescription);
            timeControlGroup.Controls.Add(speedLabel);
            timeControlGroup.Controls.Add(speedSlider);
            timeControlGroup.Controls.Add(applyButton);
            
            // Add to climate control panel
            climateControlPanel.Controls.Add(timeControlGroup);
            climateControlPanel.Controls.Add(climateZoneGroup);
            
            // Add to tab
            tab.Controls.Add(climateControlPanel);
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle tab selection changes
        /// </summary>
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabControl.TabPages["Weather Forecast"])
            {
                UpdateForecastDisplay();
            }
            else if (tabControl.SelectedTab == tabControl.TabPages["Environmental Events"])
            {
                UpdateEventDisplay();
            }
        }
        
        /// <summary>
        /// Handle climate button click
        /// </summary>
        private void ClimateButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && environmentManager != null)
            {
                string climateZone = button.Tag.ToString();
                
                // Set climate based on button tag
                ClimateZone selectedClimate = climateZone switch
                {
                    "Temperate" => ClimateZone.Temperate,
                    "Tropical" => ClimateZone.Tropical,
                    "Desert" => ClimateZone.Desert,
                    "Alpine" => ClimateZone.Alpine,
                    _ => ClimateZone.Temperate
                };
                
                // Apply the climate change
                environmentManager.SetClimate(selectedClimate);
                
                // Update display
                UpdateEnvironmentDisplay();
                
                // Show feedback
                MessageBox.Show(
                    $"Climate zone changed to {climateZone}. The weather patterns and environmental conditions will now reflect this climate.",
                    "Climate Changed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        
        /// <summary>
        /// Subscribe to environment manager events
        /// </summary>
        private void SubscribeToEnvironmentEvents()
        {
            if (environmentManager != null)
            {
                environmentManager.SeasonChanged += EnvironmentManager_SeasonChanged;
                environmentManager.WeatherChanged += EnvironmentManager_WeatherChanged;
                environmentManager.TimeOfDayChanged += EnvironmentManager_TimeOfDayChanged;
                environmentManager.EnvironmentalEventStarted += EnvironmentManager_EventStarted;
                environmentManager.EnvironmentalEventEnded += EnvironmentManager_EventEnded;
            }
        }
        
        /// <summary>
        /// Handle season changed event
        /// </summary>
        private void EnvironmentManager_SeasonChanged(object sender, SeasonChangedEventArgs e)
        {
            // Update the UI when season changes
            UpdateEnvironmentDisplay();
            
            // Show notification
            MessageBox.Show(
                $"The season has changed to {e.Season}.\nThe weather patterns and environmental conditions will now reflect this season.",
                "Season Changed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// Handle weather changed event
        /// </summary>
        private void EnvironmentManager_WeatherChanged(object sender, WeatherChangedEventArgs e)
        {
            // Update the UI when weather changes
            UpdateEnvironmentDisplay();
        }
        
        /// <summary>
        /// Handle time of day changed event
        /// </summary>
        private void EnvironmentManager_TimeOfDayChanged(object sender, TimeOfDayChangedEventArgs e)
        {
            // Update the UI when time of day changes
            UpdateEnvironmentDisplay();
        }
        
        /// <summary>
        /// Handle environmental event started
        /// </summary>
        private void EnvironmentManager_EventStarted(object sender, EnvironmentalEventArgs e)
        {
            // Update the environmental events display
            UpdateEventDisplay();
            
            // Show notification
            MessageBox.Show(
                $"A {e.Event.GetDescription()} has begun!\nThis event will affect environmental conditions for your bonsai.",
                "Environmental Event Started",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        
        /// <summary>
        /// Handle environmental event ended
        /// </summary>
        private void EnvironmentManager_EventEnded(object sender, EnvironmentalEventArgs e)
        {
            // Update the environmental events display
            UpdateEventDisplay();
        }
        
        #endregion
        
        #region UI Update Methods
        
        /// <summary>
        /// Update the environment display with current values
        /// </summary>
        public void UpdateEnvironmentDisplay()
        {
            if (environmentManager == null) return;
            
            try
            {
                // Update season information
                seasonLabel.Text = environmentManager.CurrentSeason.ToString();
                environmentGauges[0].Value = (int)environmentManager.SeasonProgressPercent;
                
                // Update weather information
                weatherLabel.Text = environmentManager.CurrentWeather.ToString();
                if (weatherIcons.TryGetValue(environmentManager.CurrentWeather, out Image weatherImage))
                {
                    weatherIcon.Image = weatherImage;
                }
                
                // Update time information
                timeLabel.Text = $"Day {environmentManager.InGameTime.Day}, {environmentManager.InGameTime.ToString("h:mm tt")}";
                environmentGauges[1].Value = (int)environmentManager.DayProgressPercent;
                
                // Update environmental factors
                temperatureLabel.Text = $"Temperature: {environmentManager.Temperature:0.0}°F";
                environmentGauges[2].Value = NormalizeTemperatureToProgressBar(environmentManager.Temperature);
                
                humidityLabel.Text = $"Humidity: {environmentManager.Humidity:0.0}%";
                environmentGauges[3].Value = (int)environmentManager.Humidity;
                
                lightQualityLabel.Text = $"Light Quality: {environmentManager.LightQuality:0.0}%";
                environmentGauges[4].Value = (int)environmentManager.LightQuality;
                
                soilQualityLabel.Text = $"Soil Quality: {environmentManager.SoilQuality:0.0}%";
                environmentGauges[5].Value = (int)environmentManager.SoilQuality;
                
                airQualityLabel.Text = $"Air Quality: {environmentManager.AirQuality:0.0}%";
                environmentGauges[6].Value = (int)environmentManager.AirQuality;
                
                // Update tab-specific displays
                if (tabControl.SelectedTab == tabControl.TabPages["Weather Forecast"])
                {
                    UpdateForecastDisplay();
                }
                else if (tabControl.SelectedTab == tabControl.TabPages["Environmental Events"])
                {
                    UpdateEventDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating environment display: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update the forecast display with current forecast
        /// </summary>
        private void UpdateForecastDisplay()
        {
            if (environmentManager == null) return;
            
            try
            {
                // Get the forecast
                var forecast = environmentManager.WeatherForecast;
                
                // Update each forecast day
                for (int i = 0; i < Math.Min(4, forecast.Count); i++)
                {
                    var dayForecast = forecast[i];
                    
                    // Find the forecast panel
                    Control dayPanel = forecastPanel.Controls[0].Controls[0].Controls[i];
                    if (dayPanel is Panel panel)
                    {
                        // Find weather icon
                        var controls = panel.Controls.Find($"day{i}WeatherIcon", true);
                        if (controls.Length > 0 && controls[0] is PictureBox weatherIcon)
                        {
                            if (weatherIcons.TryGetValue(dayForecast.Weather, out Image weatherImage))
                            {
                                weatherIcon.Image = weatherImage;
                            }
                        }
                        
                        // Find weather label
                        controls = panel.Controls.Find($"day{i}WeatherLabel", true);
                        if (controls.Length > 0 && controls[0] is Label weatherLabel)
                        {
                            weatherLabel.Text = $"Weather: {dayForecast.Weather}";
                        }
                        
                        // Find temperature label
                        controls = panel.Controls.Find($"day{i}TempLabel", true);
                        if (controls.Length > 0 && controls[0] is Label tempLabel)
                        {
                            tempLabel.Text = $"Temperature: {dayForecast.Temperature:0.0}°F";
                        }
                        
                        // Find probability label
                        controls = panel.Controls.Find($"day{i}ProbabilityLabel", true);
                        if (controls.Length > 0 && controls[0] is Label probLabel)
                        {
                            probLabel.Text = $"Forecast Accuracy: {dayForecast.Probability}%";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating forecast display: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update the event display with current events
        /// </summary>
        private void UpdateEventDisplay()
        {
            if (environmentManager == null || eventListView == null) return;
            
            try
            {
                // Clear the lists
                eventListView.Items.Clear();
                
                // Get the second ListView for upcoming events
                ListView upcomingListView = null;
                foreach (Control c in eventPanel.Controls)
                {
                    if (c is SplitContainer splitContainer)
                    {
                        foreach (Control c2 in splitContainer.Panel2.Controls)
                        {
                            if (c2 is GroupBox groupBox)
                            {
                                foreach (Control c3 in groupBox.Controls)
                                {
                                    if (c3 is ListView listView)
                                    {
                                        upcomingListView = listView;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                
                if (upcomingListView != null)
                {
                    upcomingListView.Items.Clear();
                    
                    // Fill the lists
                    DateTime now = DateTime.Now;
                    
                    // Active events
                    foreach (var activeEvent in environmentManager.ActiveEvents)
                    {
                        ListViewItem item = new ListViewItem(activeEvent.Type.ToString());
                        
                        string intensityText = activeEvent.Intensity switch
                        {
                            >= 80 => "Extreme",
                            >= 60 => "Severe",
                            >= 40 => "Moderate",
                            _ => "Mild"
                        };
                        
                        item.SubItems.Add(intensityText);
                        item.SubItems.Add(activeEvent.StartTime.ToString("yyyy-MM-dd HH:mm"));
                        item.SubItems.Add($"{activeEvent.Duration.TotalDays:0.0} days");
                        
                        // Calculate time remaining
                        TimeSpan remaining = (activeEvent.StartTime + activeEvent.Duration) - now;
                        item.SubItems.Add($"{remaining.TotalHours:0.0} hours");
                        
                        // Set color based on intensity
                        if (activeEvent.Intensity >= 80)
                            item.BackColor = Color.FromArgb(255, 200, 200); // Red for extreme
                        else if (activeEvent.Intensity >= 60)
                            item.BackColor = Color.FromArgb(255, 225, 200); // Orange for severe
                            
                        eventListView.Items.Add(item);
                    }
                    
                    // Upcoming events
                    foreach (var upcomingEvent in environmentManager.UpcomingEvents)
                    {
                        ListViewItem item = new ListViewItem(upcomingEvent.Type.ToString());
                        
                        string intensityText = upcomingEvent.Intensity switch
                        {
                            >= 80 => "Extreme",
                            >= 60 => "Severe",
                            >= 40 => "Moderate",
                            _ => "Mild"
                        };
                        
                        item.SubItems.Add(intensityText);
                        
                        // Calculate time until start
                        TimeSpan untilStart = upcomingEvent.StartTime - now;
                        item.SubItems.Add($"{untilStart.TotalHours:0.0} hours");
                        item.SubItems.Add($"{upcomingEvent.Duration.TotalDays:0.0} days");
                        
                        // Set color based on intensity
                        if (upcomingEvent.Intensity >= 80)
                            item.BackColor = Color.FromArgb(255, 230, 230); // Light red for extreme
                        else if (upcomingEvent.Intensity >= 60)
                            item.BackColor = Color.FromArgb(255, 240, 230); // Light orange for severe
                            
                        upcomingListView.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating event display: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Normalize temperature to a progress bar value (0-100)
        /// </summary>
        private int NormalizeTemperatureToProgressBar(double temperature)
        {
            // Assume temperature range from -20°F to 120°F maps to 0-100%
            double normalizedTemp = (temperature + 20) / 140 * 100;
            return (int)Math.Max(0, Math.Min(100, normalizedTemp));
        }
        
        #endregion
    }
}