using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BonsaiGotchi.BreedingSystem;

namespace BonsaiGotchi.CollectionUI
{
    /// <summary>
    /// Form for managing the bonsai collection, breeding, and seeds
    /// </summary>
    public partial class CollectionManagerForm : Form
    {
        private readonly BreedingManager breedingManager;
        private readonly Random random;
        
        // UI components
        private TabControl tabControl;
        private ListView collectionListView;
        private ListView seedsListView;
        private Panel detailsPanel;
        private PictureBox previewImage;
        private RichTextBox detailsTextBox;
        
        // Selected items
        private BonsaiSpecimen selectedSpecimen;
        private BonsaiSeed selectedSeed;
        
        // Breeding panel components
        private ComboBox parent1ComboBox;
        private ComboBox parent2ComboBox;
        private Button crossPollinate;
        private Button collectSeed;
        private Button plantSeed;
        
        // Image cache
        private Dictionary<string, Image> thumbnailCache = new();
        
        /// <summary>
        /// Initialize the collection manager form
        /// </summary>
        public CollectionManagerForm(BreedingManager breedingManager, Random random)
        {
            this.breedingManager = breedingManager;
            this.random = random;
            
            InitializeComponent();
            InitializeCollectionUI();
        }
        
        /// <summary>
        /// Initialize the form's components
        /// </summary>
        private void InitializeComponent()
        {
            // Form setup
            Text = "Bonsai Collection & Breeding";
            Size = new Size(1000, 700);
            MinimumSize = new Size(800, 600);
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
            TabPage collectionTab = new TabPage("Bonsai Collection")
            {
                BackColor = Color.FromArgb(248, 249, 250)
            };
            
            TabPage seedsTab = new TabPage("Seeds")
            {
                BackColor = Color.FromArgb(248, 249, 250)
            };
            
            TabPage breedingTab = new TabPage("Breeding")
            {
                BackColor = Color.FromArgb(248, 249, 250)
            };
            
            tabControl.TabPages.Add(collectionTab);
            tabControl.TabPages.Add(seedsTab);
            tabControl.TabPages.Add(breedingTab);
            
            // Set up tabs
            SetupCollectionTab(collectionTab);
            SetupSeedsTab(seedsTab);
            SetupBreedingTab(breedingTab);
            
            Controls.Add(tabControl);
            
            // Add event handlers
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
        }
        
        /// <summary>
        /// Initialize the collection UI
        /// </summary>
        private void InitializeCollectionUI()
        {
            // Load collection data
            RefreshCollectionList();
            RefreshSeedsList();
        }
        
        #region Collection Tab Setup
        
        /// <summary>
        /// Set up the collection tab
        /// </summary>
        private void SetupCollectionTab(TabPage tab)
        {
            // Create split container
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = tab.Height / 2,
                Panel1MinSize = 200,
                Panel2MinSize = 200
            };
            
            // Create collection list view
            collectionListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                Font = new Font("Segoe UI", 10F),
                GridLines = true
            };
            
            // Add columns
            collectionListView.Columns.Add("Name", 200);
            collectionListView.Columns.Add("Species", 150);
            collectionListView.Columns.Add("Age", 80);
            collectionListView.Columns.Add("Stage", 120);
            collectionListView.Columns.Add("Style", 150);
            collectionListView.Columns.Add("Rarity", 100);
            
            // Add ImageList for icons
            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size(32, 32);
            collectionListView.SmallImageList = imageList;
            
            // Details panel
            detailsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            // Create details layout
            TableLayoutPanel detailsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            
            // Preview image
            previewImage = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(10)
            };
            
            // Details text
            detailsTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Margin = new Padding(0, 10, 10, 10)
            };
            
            // Add to layout
            detailsLayout.Controls.Add(previewImage, 0, 0);
            detailsLayout.Controls.Add(detailsTextBox, 1, 0);
            detailsPanel.Controls.Add(detailsLayout);
            
            // Add components to split container
            splitContainer.Panel1.Controls.Add(collectionListView);
            splitContainer.Panel2.Controls.Add(detailsPanel);
            
            // Add split container to tab
            tab.Controls.Add(splitContainer);
            
            // Add event handlers
            collectionListView.SelectedIndexChanged += CollectionListView_SelectedIndexChanged;
            collectionListView.DoubleClick += CollectionListView_DoubleClick;
        }
        
        #endregion
        
        #region Seeds Tab Setup
        
        /// <summary>
        /// Set up the seeds tab
        /// </summary>
        private void SetupSeedsTab(TabPage tab)
        {
            // Create split container
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = tab.Height / 2,
                Panel1MinSize = 200,
                Panel2MinSize = 200
            };
            
            // Create seeds list view
            seedsListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                Font = new Font("Segoe UI", 10F),
                GridLines = true
            };
            
            // Add columns
            seedsListView.Columns.Add("ID", 80);
            seedsListView.Columns.Add("Parent Species", 150);
            seedsListView.Columns.Add("Parent 1", 150);
            seedsListView.Columns.Add("Parent 2", 150);
            seedsListView.Columns.Add("Created", 150);
            seedsListView.Columns.Add("Rarity", 100);
            
            // Add ImageList for icons
            ImageList seedImageList = new ImageList();
            seedImageList.ImageSize = new Size(24, 24);
            seedsListView.SmallImageList = seedImageList;
            
            // Details panel for seeds (reusing the same panel)
            Panel seedDetailsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            
            // Create seed details layout
            TableLayoutPanel seedDetailsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            seedDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            seedDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            
            // Seed details text
            RichTextBox seedDetailsText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Margin = new Padding(10)
            };
            
            // Plant seed button panel
            FlowLayoutPanel seedButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10),
                WrapContents = false
            };
            
            // Plant seed button
            plantSeed = new Button
            {
                Text = "Plant Selected Seed",
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                Enabled = false
            };
            plantSeed.FlatAppearance.BorderSize = 0;
            plantSeed.Click += PlantSeed_Click;
            
            seedButtonPanel.Controls.Add(plantSeed);
            
            // Add to layout
            seedDetailsLayout.Controls.Add(seedDetailsText, 0, 0);
            seedDetailsLayout.Controls.Add(seedButtonPanel, 0, 1);
            seedDetailsPanel.Controls.Add(seedDetailsLayout);
            
            // Add components to split container
            splitContainer.Panel1.Controls.Add(seedsListView);
            splitContainer.Panel2.Controls.Add(seedDetailsPanel);
            
            // Add split container to tab
            tab.Controls.Add(splitContainer);
            
            // Add event handlers
            seedsListView.SelectedIndexChanged += SeedsListView_SelectedIndexChanged;
        }
        
        #endregion
        
        #region Breeding Tab Setup
        
        /// <summary>
        /// Set up the breeding tab
        /// </summary>
        private void SetupBreedingTab(TabPage tab)
        {
            // Create main panel
            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20)
            };
            
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            
            // Add breeding instructions
            GroupBox instructionsBox = new GroupBox
            {
                Text = "Breeding Instructions",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            
            Label instructions = new Label
            {
                Dock = DockStyle.Fill,
                Text = "To breed bonsai trees, select two mature trees (Mature or Elder stage) as parents.\n\n" +
                      "Cross-pollination gives better results with compatible but diverse parent trees.\n\n" +
                      "You can also collect seeds from a single mature tree, but they will have less variety.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(52, 73, 94),
                Padding = new Padding(10)
            };
            
            instructionsBox.Controls.Add(instructions);
            
            // Add breeding controls
            TableLayoutPanel breedingPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1
            };
            
            breedingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            breedingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            breedingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            breedingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            breedingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            
            // Parent 1 combo box
            parent1ComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            
            Label plusLabel = new Label
            {
                Text = "+",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            
            // Parent 2 combo box
            parent2ComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            
            Label equalsLabel = new Label
            {
                Text = "=",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            
            // Breeding actions panel
            FlowLayoutPanel actionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight
            };
            
            // Cross-pollinate button
            crossPollinate = new Button
            {
                Text = "Cross Pollinate",
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Enabled = false
            };
            crossPollinate.FlatAppearance.BorderSize = 0;
            
            // Collect seed button
            collectSeed = new Button
            {
                Text = "Collect Seed",
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                Enabled = false
            };
            collectSeed.FlatAppearance.BorderSize = 0;
            
            actionsPanel.Controls.Add(crossPollinate);
            actionsPanel.Controls.Add(collectSeed);
            
            // Add event handlers
            crossPollinate.Click += CrossPollinate_Click;
            collectSeed.Click += CollectSeed_Click;
            parent1ComboBox.SelectedIndexChanged += Parent_SelectedIndexChanged;
            parent2ComboBox.SelectedIndexChanged += Parent_SelectedIndexChanged;
            
            // Add to breeding panel
            breedingPanel.Controls.Add(parent1ComboBox, 0, 0);
            breedingPanel.Controls.Add(plusLabel, 1, 0);
            breedingPanel.Controls.Add(parent2ComboBox, 2, 0);
            breedingPanel.Controls.Add(equalsLabel, 3, 0);
            breedingPanel.Controls.Add(actionsPanel, 4, 0);
            
            // Add result display
            RichTextBox breedingResults = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White
            };
            
            // Add to main panel
            mainPanel.Controls.Add(instructionsBox, 0, 0);
            mainPanel.Controls.Add(breedingPanel, 0, 1);
            mainPanel.Controls.Add(breedingResults, 0, 2);
            
            // Add to tab
            tab.Controls.Add(mainPanel);
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle tab selection changes
        /// </summary>
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Refresh data when switching tabs
            if (tabControl.SelectedTab.Text == "Bonsai Collection")
            {
                RefreshCollectionList();
            }
            else if (tabControl.SelectedTab.Text == "Seeds")
            {
                RefreshSeedsList();
            }
            else if (tabControl.SelectedTab.Text == "Breeding")
            {
                RefreshBreedingOptions();
            }
        }
        
        /// <summary>
        /// Handle collection list item selection
        /// </summary>
        private void CollectionListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (collectionListView.SelectedItems.Count > 0)
            {
                // Get selected specimen
                string selectedId = collectionListView.SelectedItems[0].Tag.ToString();
                selectedSpecimen = breedingManager.GetSpecimen(Guid.Parse(selectedId));
                
                // Display details
                if (selectedSpecimen != null)
                {
                    DisplaySpecimenDetails(selectedSpecimen);
                }
            }
            else
            {
                selectedSpecimen = null;
                ClearDetailsDisplay();
            }
        }
        
        /// <summary>
        /// Handle collection list item double-click
        /// </summary>
        private void CollectionListView_DoubleClick(object sender, EventArgs e)
        {
            if (selectedSpecimen?.BonsaiInstance != null)
            {
                // Set as active bonsai
                breedingManager.SetActiveBonsai(selectedSpecimen.BonsaiInstance);
                
                // Show confirmation message
                MessageBox.Show(
                    $"{selectedSpecimen.Name} is now your active bonsai!",
                    "Active Bonsai Changed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        
        /// <summary>
        /// Handle seeds list item selection
        /// </summary>
        private void SeedsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (seedsListView.SelectedItems.Count > 0)
            {
                // Get selected seed
                string selectedId = seedsListView.SelectedItems[0].Tag.ToString();
                selectedSeed = breedingManager.Seeds.Find(s => s.Id.ToString() == selectedId);
                
                // Display seed details
                if (selectedSeed != null)
                {
                    DisplaySeedDetails(selectedSeed);
                    plantSeed.Enabled = true;
                }
            }
            else
            {
                selectedSeed = null;
                plantSeed.Enabled = false;
            }
        }
        
        /// <summary>
        /// Handle parent selection change
        /// </summary>
        private void Parent_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateBreedingButtons();
        }
        
        /// <summary>
        /// Handle cross-pollinate button click
        /// </summary>
        private void CrossPollinate_Click(object sender, EventArgs e)
        {
            try
            {
                // Get selected parents
                if (parent1ComboBox.SelectedItem == null || parent2ComboBox.SelectedItem == null)
                {
                    MessageBox.Show(
                        "Please select two parent trees for breeding.",
                        "Selection Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                
                var parent1 = (BonsaiSpecimen)parent1ComboBox.SelectedItem;
                var parent2 = (BonsaiSpecimen)parent2ComboBox.SelectedItem;
                
                // Try breeding
                var seed = breedingManager.TryBreed(parent1.Id, parent2.Id);
                
                if (seed != null)
                {
                    // Success!
                    MessageBox.Show(
                        $"Success! You've created a new {seed.Rarity} seed by cross-pollinating {parent1.Name} and {parent2.Name}.",
                        "Breeding Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                        
                    // Refresh seeds list
                    RefreshSeedsList();
                    
                    // Show the seeds tab
                    tabControl.SelectedIndex = tabControl.TabPages.IndexOf(tabControl.TabPages["Seeds"]);
                }
                else
                {
                    // Failed
                    MessageBox.Show(
                        "The breeding attempt failed. The trees may not be compatible, or they might not be healthy enough.",
                        "Breeding Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred during breeding: {ex.Message}",
                    "Breeding Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Handle collect seed button click
        /// </summary>
        private void CollectSeed_Click(object sender, EventArgs e)
        {
            try
            {
                // Get selected parent
                if (parent1ComboBox.SelectedItem == null)
                {
                    MessageBox.Show(
                        "Please select a parent tree to collect seeds from.",
                        "Selection Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                
                var parent = (BonsaiSpecimen)parent1ComboBox.SelectedItem;
                
                // Try collecting seeds
                var seed = breedingManager.CollectSeed(parent.Id);
                
                if (seed != null)
                {
                    // Success!
                    MessageBox.Show(
                        $"Success! You've collected a {seed.Rarity} seed from {parent.Name}.",
                        "Seed Collection Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                        
                    // Refresh seeds list
                    RefreshSeedsList();
                    
                    // Show the seeds tab
                    tabControl.SelectedIndex = tabControl.TabPages.IndexOf(tabControl.TabPages["Seeds"]);
                }
                else
                {
                    // Failed
                    MessageBox.Show(
                        "Failed to collect seeds. The tree may not be mature enough or healthy enough.",
                        "Seed Collection Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred during seed collection: {ex.Message}",
                    "Seed Collection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Handle plant seed button click
        /// </summary>
        private void PlantSeed_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedSeed == null)
                {
                    MessageBox.Show(
                        "Please select a seed to plant.",
                        "Selection Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                
                // Show name input dialog
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

                Label nameLabel = new Label
                {
                    Text = "Enter a name for your bonsai:",
                    Left = 20,
                    Top = 20,
                    Width = 260
                };

                TextBox nameTextBox = new TextBox
                {
                    Left = 20,
                    Top = 50,
                    Width = 260,
                    Text = GenerateDefaultName(selectedSeed)
                };

                Button confirmButton = new Button
                {
                    Text = "Plant",
                    Left = 110,
                    Top = 80,
                    DialogResult = DialogResult.OK
                };

                nameDialog.Controls.Add(nameLabel);
                nameDialog.Controls.Add(nameTextBox);
                nameDialog.Controls.Add(confirmButton);
                nameDialog.AcceptButton = confirmButton;

                if (nameDialog.ShowDialog() == DialogResult.OK)
                {
                    string name = nameTextBox.Text.Trim();
                    if (string.IsNullOrEmpty(name)) name = GenerateDefaultName(selectedSeed);

                    // Plant the seed
                    var newBonsai = breedingManager.PlantSeed(selectedSeed.Id, name);
                    
                    if (newBonsai != null)
                    {
                        // Success!
                        MessageBox.Show(
                            $"Success! You've planted the seed and created a new bonsai named {name}.",
                            "Seed Planted",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                            
                        // Refresh collections
                        RefreshCollectionList();
                        RefreshSeedsList();
                        
                        // Show the collection tab
                        tabControl.SelectedIndex = tabControl.TabPages.IndexOf(tabControl.TabPages["Bonsai Collection"]);
                    }
                    else
                    {
                        // Failed - should not happen, but just in case
                        MessageBox.Show(
                            "Failed to plant the seed. Please try again.",
                            "Planting Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while planting the seed: {ex.Message}",
                    "Planting Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        
        #endregion
        
        #region UI Update Methods
        
        /// <summary>
        /// Refresh the collection list view
        /// </summary>
        private void RefreshCollectionList()
        {
            collectionListView.Items.Clear();
            
            foreach (var specimen in breedingManager.Collection)
            {
                ListViewItem item = new ListViewItem(specimen.Name);
                
                // Add sub-items
                item.SubItems.Add(specimen.Genetics?.Species ?? "Unknown");
                item.SubItems.Add(specimen.Age.ToString());
                item.SubItems.Add(specimen.Stage.ToString());
                item.SubItems.Add(specimen.Genetics?.DominantStyle.ToString() ?? "Standard");
                item.SubItems.Add(specimen.Genetics?.Rarity.ToString() ?? "Common");
                
                // Add tag for identification
                item.Tag = specimen.Id.ToString();
                
                // Add to list
                collectionListView.Items.Add(item);
            }
        }
        
        /// <summary>
        /// Refresh the seeds list view
        /// </summary>
        private void RefreshSeedsList()
        {
            seedsListView.Items.Clear();
            
            foreach (var seed in breedingManager.Seeds)
            {
                ListViewItem item = new ListViewItem(seed.Id.ToString().Substring(0, 8) + "...");
                
                // Add sub-items
                item.SubItems.Add(seed.Genetics?.Species ?? "Unknown");
                item.SubItems.Add(seed.Parent1Name);
                item.SubItems.Add(seed.Parent2Name);
                item.SubItems.Add(seed.CreationDate.ToString("yyyy-MM-dd"));
                item.SubItems.Add(seed.Rarity.ToString());
                
                // Add tag for identification
                item.Tag = seed.Id.ToString();
                
                // Set icon based on rarity
                int imageIndex = (int)seed.Rarity;
                item.ImageIndex = Math.Min(imageIndex, seedsListView.SmallImageList.Images.Count - 1);
                
                // Add to list
                seedsListView.Items.Add(item);
            }
        }
        
        /// <summary>
        /// Refresh breeding options
        /// </summary>
        private void RefreshBreedingOptions()
        {
            // Clear combos
            parent1ComboBox.Items.Clear();
            parent2ComboBox.Items.Clear();
            
            // Filter mature bonsai
            var eligibleBonsai = breedingManager.Collection
                .Where(s => s.Stage >= GrowthStage.MatureTree)
                .ToList();
            
            // Add to combo boxes
            foreach (var specimen in eligibleBonsai)
            {
                parent1ComboBox.Items.Add(specimen);
                parent2ComboBox.Items.Add(specimen);
            }
            
            // Set display member (ToString implementation of specimen)
            parent1ComboBox.DisplayMember = "Name";
            parent2ComboBox.DisplayMember = "Name";
            
            // Disable buttons initially
            UpdateBreedingButtons();
        }
        
        /// <summary>
        /// Update breeding buttons based on selections
        /// </summary>
        private void UpdateBreedingButtons()
        {
            // Enable/disable cross-pollinate based on both parents selected
            crossPollinate.Enabled = parent1ComboBox.SelectedItem != null && parent2ComboBox.SelectedItem != null;
            
            // Enable/disable collect seed based on first parent selected
            collectSeed.Enabled = parent1ComboBox.SelectedItem != null;
        }
        
        /// <summary>
        /// Display specimen details
        /// </summary>
        private void DisplaySpecimenDetails(BonsaiSpecimen specimen)
        {
            // Display genetics description in details text box
            string details = "";
            
            if (specimen.Genetics != null)
            {
                details = specimen.Genetics.GetGeneticDescription();
            }
            else
            {
                details = "No genetic data available for this bonsai.";
            }
            
            // Add additional info
            details += $"\n\nAcquired: {specimen.AcquisitionDate:yyyy-MM-dd}";
            details += $"\nCurrent Age: {specimen.Age} days";
            details += $"\nGrowth Stage: {specimen.Stage}";
            
            // Show if it's the active bonsai
            if (breedingManager.ActiveBonsai?.Id == specimen.Id)
            {
                details += "\n\n[ACTIVE BONSAI]";
            }
            
            detailsTextBox.Text = details;
            
            // Generate a placeholder image based on genetics
            if (previewImage != null)
            {
                // Use cached image if available
                string imageKey = specimen.Id.ToString();
                if (thumbnailCache.ContainsKey(imageKey))
                {
                    previewImage.Image = thumbnailCache[imageKey];
                }
                else
                {
                    // Generate a placeholder image based on genetics
                    Image thumbnail = GenerateBonsaiThumbnail(specimen);
                    thumbnailCache[imageKey] = thumbnail;
                    previewImage.Image = thumbnail;
                }
            }
        }
        
        /// <summary>
        /// Display seed details
        /// </summary>
        private void DisplaySeedDetails(BonsaiSeed seed)
        {
            if (seedsListView.SelectedItems.Count == 0) return;
            
            // Get the rich text box in the split container
            var detailsTextBox = ((seedsListView.Parent as SplitContainer)?.Panel2.Controls[0] as Panel)?.Controls[0] as TableLayoutPanel;
            if (detailsTextBox == null) return;
            
            var seedDetailsText = detailsTextBox.Controls[0] as RichTextBox;
            if (seedDetailsText == null) return;
            
            // Display seed details
            string details = $"Seed ID: {seed.Id}\n\n";
            details += $"Rarity: {seed.Rarity}\n";
            details += $"Created: {seed.CreationDate:yyyy-MM-dd HH:mm}\n\n";
            details += $"Parent 1: {seed.Parent1Name}\n";
            details += $"Parent 2: {seed.Parent2Name}\n\n";
            
            // Add genetics info if available
            if (seed.Genetics != null)
            {
                details += "Genetic Profile:\n";
                details += $"Species: {seed.Genetics.Species}\n";
                details += $"Dominant Style: {seed.Genetics.DominantStyle}\n\n";
                
                // Show special traits if any
                if (seed.Genetics.SpecialTraits.Count > 0)
                {
                    details += "Special Traits:\n";
                    foreach (var trait in seed.Genetics.SpecialTraits)
                    {
                        details += $"• {trait}\n";
                    }
                }
            }
            
            seedDetailsText.Text = details;
        }
        
        /// <summary>
        /// Clear the details display
        /// </summary>
        private void ClearDetailsDisplay()
        {
            detailsTextBox.Clear();
            previewImage.Image = null;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Generate a default name for a new bonsai based on seed
        /// </summary>
        private string GenerateDefaultName(BonsaiSeed seed)
        {
            if (seed == null) return "Bonsai";
            
            // Use species and a number
            string species = seed.Genetics?.Species ?? "Bonsai";
            
            // Keep just the main part of the species (e.g., "Pine" from "Japanese White Pine")
            string[] speciesParts = species.Split(' ');
            string simplifiedSpecies = speciesParts[speciesParts.Length - 1];
            
            // Add a random number
            int number = random.Next(100, 1000);
            
            return $"{simplifiedSpecies}-{number}";
        }
        
        /// <summary>
        /// Generate a simple thumbnail image for a bonsai based on its genetics
        /// </summary>
        private Image GenerateBonsaiThumbnail(BonsaiSpecimen specimen)
        {
            // Create a blank image
            Bitmap thumbnail = new Bitmap(200, 200);
            using Graphics g = Graphics.FromImage(thumbnail);
            
            // Fill background
            g.Clear(Color.FromArgb(248, 249, 250));
            
            if (specimen.Genetics != null)
            {
                // Choose colors based on genetics
                Color trunkColor = BrownForSpecies(specimen.Genetics.Species);
                Color leafColor = GreenForSpecies(specimen.Genetics.Species);
                
                // Modify colors based on special traits
                foreach (var trait in specimen.Genetics.SpecialTraits)
                {
                    switch (trait)
                    {
                        case SpecialTrait.RedLeaves:
                            leafColor = Color.FromArgb(180, 50, 50);
                            break;
                            
                        case SpecialTrait.GoldenFoliage:
                            leafColor = Color.FromArgb(218, 165, 32);
                            break;
                            
                        case SpecialTrait.VarlegetedLeaves:
                            leafColor = Color.FromArgb(
                                (leafColor.R + 100) / 2,
                                Math.Min(255, leafColor.G + 50),
                                leafColor.B);
                            break;
                            
                        case SpecialTrait.IridescentBark:
                            trunkColor = Color.FromArgb(
                                trunkColor.R,
                                Math.Min(255, trunkColor.G + 30),
                                Math.Min(255, trunkColor.B + 50));
                            break;
                    }
                }
                
                // Draw the bonsai based on style
                switch (specimen.Genetics.DominantStyle)
                {
                    case BonsaiStyle.FormalUpright:
                        DrawFormalUpright(g, trunkColor, leafColor);
                        break;
                        
                    case BonsaiStyle.InformalUpright:
                        DrawInformalUpright(g, trunkColor, leafColor);
                        break;
                        
                    case BonsaiStyle.Windswept:
                        DrawWindswept(g, trunkColor, leafColor);
                        break;
                        
                    case BonsaiStyle.Cascade:
                        DrawCascade(g, trunkColor, leafColor);
                        break;
                        
                    case BonsaiStyle.Slanting:
                        DrawSlanting(g, trunkColor, leafColor);
                        break;
                        
                    default:
                        DrawFormalUpright(g, trunkColor, leafColor);
                        break;
                }
                
                // Draw pot
                using SolidBrush potBrush = new SolidBrush(Color.FromArgb(160, 82, 45));
                g.FillRectangle(potBrush, 60, 160, 80, 30);
            }
            
            return thumbnail;
        }
        
        /// <summary>
        /// Draw a formal upright bonsai
        /// </summary>
        private void DrawFormalUpright(Graphics g, Color trunkColor, Color leafColor)
        {
            // Draw trunk
            using SolidBrush trunkBrush = new SolidBrush(trunkColor);
            g.FillRectangle(trunkBrush, 95, 80, 10, 80);
            
            // Draw branches
            g.FillRectangle(trunkBrush, 75, 100, 20, 5);
            g.FillRectangle(trunkBrush, 105, 90, 20, 5);
            g.FillRectangle(trunkBrush, 70, 120, 25, 5);
            g.FillRectangle(trunkBrush, 105, 110, 25, 5);
            
            // Draw foliage
            using SolidBrush leafBrush = new SolidBrush(leafColor);
            g.FillEllipse(leafBrush, 50, 70, 50, 50);
            g.FillEllipse(leafBrush, 100, 60, 50, 50);
            g.FillEllipse(leafBrush, 45, 90, 40, 40);
            g.FillEllipse(leafBrush, 115, 80, 40, 40);
        }
        
        /// <summary>
        /// Draw an informal upright bonsai
        /// </summary>
        private void DrawInformalUpright(Graphics g, Color trunkColor, Color leafColor)
        {
            // Draw curved trunk
            using Pen trunkPen = new Pen(trunkColor, 10);
            g.DrawCurve(trunkPen, new Point[] {
                new Point(100, 160),
                new Point(95, 120),
                new Point(105, 80),
                new Point(100, 60)
            });
            
            // Draw branches
            using SolidBrush trunkBrush = new SolidBrush(trunkColor);
            g.FillRectangle(trunkBrush, 75, 100, 20, 5);
            g.FillRectangle(trunkBrush, 105, 90, 20, 5);
            g.FillRectangle(trunkBrush, 70, 120, 25, 5);
            
            // Draw foliage
            using SolidBrush leafBrush = new SolidBrush(leafColor);
            g.FillEllipse(leafBrush, 55, 75, 45, 45);
            g.FillEllipse(leafBrush, 100, 55, 45, 45);
            g.FillEllipse(leafBrush, 45, 95, 40, 40);
            g.FillEllipse(leafBrush, 115, 70, 40, 40);
        }
        
        /// <summary>
        /// Draw a windswept bonsai
        /// </summary>
        private void DrawWindswept(Graphics g, Color trunkColor, Color leafColor)
        {
            // Draw swept trunk
            using Pen trunkPen = new Pen(trunkColor, 10);
            g.DrawCurve(trunkPen, new Point[] {
                new Point(100, 160),
                new Point(120, 130),
                new Point(140, 100),
                new Point(150, 80)
            });
            
            // Draw branches
            using SolidBrush trunkBrush = new SolidBrush(trunkColor);
            g.FillRectangle(trunkBrush, 115, 120, 25, 5);
            g.FillRectangle(trunkBrush, 135, 100, 25, 5);
            g.FillRectangle(trunkBrush, 150, 80, 20, 5);
            
            // Draw foliage
            using SolidBrush leafBrush = new SolidBrush(leafColor);
            g.FillEllipse(leafBrush, 120, 100, 50, 40);
            g.FillEllipse(leafBrush, 140, 75, 45, 35);
            g.FillEllipse(leafBrush, 160, 60, 30, 30);
        }
        
        /// <summary>
        /// Draw a cascade bonsai
        /// </summary>
        private void DrawCascade(Graphics g, Color trunkColor, Color leafColor)
        {
            // Draw cascade trunk
            using Pen trunkPen = new Pen(trunkColor, 10);
            g.DrawCurve(trunkPen, new Point[] {
                new Point(100, 160),
                new Point(90, 130),
                new Point(70, 150),
                new Point(50, 180)
            });
            
            // Draw branches
            using SolidBrush trunkBrush = new SolidBrush(trunkColor);
            g.FillRectangle(trunkBrush, 90, 120, 20, 5);
            g.FillRectangle(trunkBrush, 70, 140, 20, 5);
            
            // Draw foliage
            using SolidBrush leafBrush = new SolidBrush(leafColor);
            g.FillEllipse(leafBrush, 75, 100, 50, 40);
            g.FillEllipse(leafBrush, 40, 130, 45, 35);
            g.FillEllipse(leafBrush, 20, 160, 50, 40);
        }
        
        /// <summary>
        /// Draw a slanting bonsai
        /// </summary>
        private void DrawSlanting(Graphics g, Color trunkColor, Color leafColor)
        {
            // Draw slanting trunk
            using Pen trunkPen = new Pen(trunkColor, 10);
            g.DrawCurve(trunkPen, new Point[] {
                new Point(80, 160),
                new Point(100, 120),
                new Point(120, 80),
                new Point(130, 60)
            });
            
            // Draw branches
            using SolidBrush trunkBrush = new SolidBrush(trunkColor);
            g.FillRectangle(trunkBrush, 95, 120, 25, 5);
            g.FillRectangle(trunkBrush, 115, 90, 25, 5);
            g.FillRectangle(trunkBrush, 130, 70, 20, 5);
            
            // Draw foliage
            using SolidBrush leafBrush = new SolidBrush(leafColor);
            g.FillEllipse(leafBrush, 75, 100, 50, 40);
            g.FillEllipse(leafBrush, 105, 70, 45, 40);
            g.FillEllipse(leafBrush, 125, 50, 35, 30);
        }
        
        /// <summary>
        /// Get a brown color appropriate for the species
        /// </summary>
        private Color BrownForSpecies(string species)
        {
            if (string.IsNullOrEmpty(species))
                return Color.FromArgb(139, 69, 19); // Default brown
                
            // Different species have different bark colors
            if (species.Contains("Pine") || species.Contains("Cedar"))
                return Color.FromArgb(101, 67, 33); // Dark reddish brown
                
            if (species.Contains("Maple"))
                return Color.FromArgb(165, 42, 42); // Medium brown
                
            if (species.Contains("Elm"))
                return Color.FromArgb(160, 120, 90); // Grey-brown
                
            if (species.Contains("Juniper"))
                return Color.FromArgb(205, 133, 63); // Light brown
                
            // Default
            return Color.FromArgb(139, 69, 19);
        }
        
        /// <summary>
        /// Get a green color appropriate for the species
        /// </summary>
        private Color GreenForSpecies(string species)
        {
            if (string.IsNullOrEmpty(species))
                return Color.FromArgb(34, 139, 34); // Default forest green
                
            // Different species have different leaf colors
            if (species.Contains("Pine") || species.Contains("Cedar") || species.Contains("Juniper"))
                return Color.FromArgb(0, 100, 0); // Dark green
                
            if (species.Contains("Maple"))
                return Color.FromArgb(60, 179, 113); // Medium spring green
                
            if (species.Contains("Elm"))
                return Color.FromArgb(85, 107, 47); // Dark olive green
                
            if (species.Contains("Jade"))
                return Color.FromArgb(0, 128, 0); // Green
                
            // Default
            return Color.FromArgb(34, 139, 34);
        }
        
        #endregion
    }
}