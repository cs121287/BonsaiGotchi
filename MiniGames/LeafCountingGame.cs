using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BonsaiGotchi.MiniGames
{
    /// <summary>
    /// Simple leaf counting mini-game for bonsai interaction
    /// </summary>
    public class LeafCountingGame : MiniGameBase
    {
        private Panel gamePanel;
        private Label promptLabel;
        private NumericUpDown guessInput;
        private Button submitButton;
        private Timer gameTimer;
        
        private int actualLeafCount;
        private int secondsRemaining = 20;
        private List<PictureBox> leaves = new List<PictureBox>();
        
        public LeafCountingGame() : base()
        {
            // Form setup
            Text = "Leaf Counting Game";
            Size = new Size(600, 500);
            
            // Set game-specific labels
            titleLabel.Text = "Bonsai Leaf Counting";
            instructionsLabel.Text = "Count the leaves on your bonsai tree and enter the number below.\nYou have 20 seconds!";
            
            // Create game layout
            CreateGameLayout();
            
            // Initialize game
            InitializeGame();
        }
        
        private void CreateGameLayout()
        {
            gamePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(20)
            };
            
            // Create timer display
            Label timerLabel = new Label
            {
                Text = "Time: 20s",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            
            // Create leaf display area
            Panel leafDisplayArea = new Panel
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Size = new Size(500, 300),
                Location = new Point(50, 60)
            };
            
            // Create bottom panel for input
            Panel inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100
            };
            
            // Create prompt label
            promptLabel = new Label
            {
                Text = "How many leaves do you see?",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 15)
            };
            
            // Create numeric input
            guessInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100,
                Value = 10,
                Font = new Font("Segoe UI", 14),
                Size = new Size(80, 30),
                Location = new Point(240, 10),
                TextAlign = HorizontalAlignment.Center
            };
            
            // Create submit button
            submitButton = new Button
            {
                Text = "Submit Guess",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(120, 40),
                Location = new Point(350, 10),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            submitButton.FlatAppearance.BorderSize = 0;
            submitButton.Click += SubmitButton_Click;
            
            // Assemble the layout
            inputPanel.Controls.Add(promptLabel);
            inputPanel.Controls.Add(guessInput);
            inputPanel.Controls.Add(submitButton);
            
            gamePanel.Controls.Add(timerLabel);
            gamePanel.Controls.Add(leafDisplayArea);
            gamePanel.Controls.Add(inputPanel);
            
            Controls.Add(gamePanel);
            
            // Keep references to needed controls
            gameTimer = new Timer
            {
                Interval = 1000 // 1 second
            };
            gameTimer.Tick += GameTimer_Tick;
        }
        
        private void InitializeGame()
        {
            // Reset game state
            secondsRemaining = 20;
            leaves.Clear();
            
            // Find the leaf display area
            Panel leafArea = null;
            foreach (Control c in gamePanel.Controls)
            {
                if (c is Panel panel && panel.BackColor == Color.White)
                {
                    leafArea = panel;
                    break;
                }
            }
            
            if (leafArea == null) return;
            
            // Clear any existing leaves
            leafArea.Controls.Clear();
            
            // Determine number of leaves (10-35)
            actualLeafCount = random.Next(10, 36);
            
            // Create leaves
            for (int i = 0; i < actualLeafCount; i++)
            {
                // Random leaf size (small variations)
                int leafSize = random.Next(15, 26);
                
                // Create leaf control (would use actual leaf images in a real app)
                PictureBox leaf = new PictureBox
                {
                    Width = leafSize,
                    Height = leafSize,
                    BackColor = Color.FromArgb(random.Next(30, 80), random.Next(120, 180), random.Next(30, 80)), // Green variations
                    Location = new Point(
                        random.Next(5, leafArea.Width - leafSize - 5),
                        random.Next(5, leafArea.Height - leafSize - 5)
                    )
                };
                
                // Make it oval shaped
                leaf.Paint += (s, e) => 
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var brush = new SolidBrush(leaf.BackColor);
                    e.Graphics.FillEllipse(brush, 0, 0, leaf.Width, leaf.Height);
                };
                
                leaves.Add(leaf);
                leafArea.Controls.Add(leaf);
                leaf.BringToFront();
            }
            
            // Reset the timer display
            Label timerLabel = null;
            foreach (Control c in gamePanel.Controls)
            {
                if (c is Label label && label.Text.StartsWith("Time:"))
                {
                    timerLabel = label;
                    break;
                }
            }
            
            if (timerLabel != null)
            {
                timerLabel.Text = $"Time: {secondsRemaining}s";
            }
            
            // Start the timer
            gameTimer.Start();
        }
        
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            secondsRemaining--;
            
            // Update timer display
            Label timerLabel = null;
            foreach (Control c in gamePanel.Controls)
            {
                if (c is Label label && label.Text.StartsWith("Time:"))
                {
                    timerLabel = label;
                    break;
                }
            }
            
            if (timerLabel != null)
            {
                timerLabel.Text = $"Time: {secondsRemaining}s";
            }
            
            // Check if time's up
            if (secondsRemaining <= 0)
            {
                gameTimer.Stop();
                
                // Auto-submit the current guess
                SubmitGuess();
            }
        }
        
        private void SubmitButton_Click(object sender, EventArgs e)
        {
            gameTimer.Stop();
            SubmitGuess();
        }
        
        private void SubmitGuess()
        {
            int guess = (int)guessInput.Value;
            int difference = Math.Abs(guess - actualLeafCount);
            
            // Calculate score based on accuracy
            double accuracy = Math.Max(0, 100 - (difference * 100.0 / actualLeafCount));
            
            string message;
            if (difference == 0)
            {
                message = $"Perfect! There were exactly {actualLeafCount} leaves!";
            }
            else if (difference <= 2)
            {
                message = $"Very close! There were {actualLeafCount} leaves.";
            }
            else if (difference <= 5)
            {
                message = $"Good try! There were {actualLeafCount} leaves.";
            }
            else
            {
                message = $"Not quite right. There were {actualLeafCount} leaves on the tree.";
            }
            
            // Complete the game
            CompleteGame(accuracy, message);
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Stop the timer
            gameTimer.Stop();
            gameTimer.Dispose();
            
            base.OnFormClosing(e);
        }
    }
}