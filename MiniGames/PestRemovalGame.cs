using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BonsaiGotchi.MiniGames
{
    /// <summary>
    /// Mini-game for removing pests from the bonsai
    /// </summary>
    public class PestRemovalGame : MiniGameBase
    {
        private Panel gamePanel;
        private Label timeLabel;
        private List<PictureBox> pests = new List<PictureBox>();
        private int pestsRemoved = 0;
        private int totalPests;
        private System.Windows.Forms.Timer gameTimer;
        private int secondsRemaining = 30;
        private bool gameActive = false;
        
        public PestRemovalGame() : base()
        {
            // Form setup
            Text = "Pest Removal";
            Size = new Size(600, 500);
            
            // Set game-specific labels
            titleLabel.Text = "Pest Removal Challenge";
            instructionsLabel.Text = "Click on the bugs to remove them from your bonsai tree!\nYou have 30 seconds to remove as many as possible.";
            
            // Create game panel
            CreateGamePanel();
            
            // Create timer display
            CreateTimerDisplay();
            
            // Initialize game
            InitializeGame();
        }
        
        private void CreateGamePanel()
        {
            gamePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.None
            };
            
            Controls.Add(gamePanel);
            gamePanel.BringToFront();
            closeButton.BringToFront();
        }
        
        private void CreateTimerDisplay()
        {
            timeLabel = new Label
            {
                Text = "Time: 30",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Size = new Size(150, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 20),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White
            };
            
            gamePanel.Controls.Add(timeLabel);
            timeLabel.BringToFront();
        }
        
        private void InitializeGame()
        {
            // Clear any existing pests
            foreach (var pest in pests)
            {
                gamePanel.Controls.Remove(pest);
                pest.Dispose();
            }
            pests.Clear();
            
            // Determine number of pests based on difficulty
            totalPests = random.Next(10, 21); // 10-20 pests
            pestsRemoved = 0;
            
            // Create game timer
            gameTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1 second
            };
            gameTimer.Tick += GameTimer_Tick;
            
            // Reset timer
            secondsRemaining = 30;
            timeLabel.Text = $"Time: {secondsRemaining}";
            
            // Start spawning pests
            SpawnInitialPests();
            
            // Start timer
            gameActive = true;
            gameTimer.Start();
        }
        
        private void SpawnInitialPests()
        {
            // Spawn initial batch of pests
            for (int i = 0; i < Math.Min(10, totalPests); i++)
            {
                SpawnPest();
            }
        }
        
        private void SpawnPest()
        {
            if (!gameActive) return;
            
            // Select random pest type (3 types)
            int pestType = random.Next(3);
            
            // Create pest image (we would use actual images in a real application)
            PictureBox pest = new PictureBox
            {
                Width = 30,
                Height = 30,
                BackColor = pestType switch
                {
                    0 => Color.FromArgb(139, 69, 19), // Brown beetle
                    1 => Color.FromArgb(34, 139, 34), // Green aphid
                    _ => Color.FromArgb(0, 0, 0)      // Black spider
                },
                Location = new Point(
                    random.Next(50, gamePanel.Width - 80),
                    random.Next(60, gamePanel.Height - 80)
                )
            };
            
            // Make it circular
            pest.Paint += (s, e) => 
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(pest.BackColor);
                e.Graphics.FillEllipse(brush, 0, 0, pest.Width, pest.Height);
            };
            
            // Add click handler
            pest.Click += Pest_Click;
            pest.Cursor = Cursors.Hand;
            
            // Add to panel and list
            pests.Add(pest);
            gamePanel.Controls.Add(pest);
            pest.BringToFront();
            
            // Start moving the pest
            System.Windows.Forms.Timer pestMover = new System.Windows.Forms.Timer
            {
                Interval = 500
            };
            pestMover.Tick += (s, e) => MovePest(pest);
            pestMover.Start();
        }
        
        private void MovePest(PictureBox pest)
        {
            if (!gameActive || pest.IsDisposed) return;
            
            // Move in a random direction
            int moveX = random.Next(-10, 11);
            int moveY = random.Next(-10, 11);
            
            // Keep within bounds
            int newX = Math.Max(0, Math.Min(gamePanel.Width - pest.Width, pest.Left + moveX));
            int newY = Math.Max(50, Math.Min(gamePanel.Height - pest.Height, pest.Top + moveY));
            
            pest.Location = new Point(newX, newY);
        }
        
        private void Pest_Click(object sender, EventArgs e)
        {
            if (!gameActive) return;
            
            if (sender is PictureBox pest)
            {
                // Remove the pest
                pests.Remove(pest);
                gamePanel.Controls.Remove(pest);
                pest.Dispose();
                
                // Increment counter
                pestsRemoved++;
                
                // Check if we've removed all pests
                if (pestsRemoved >= totalPests)
                {
                    EndGame(true);
                }
                else if (pests.Count < 5 && pestsRemoved < totalPests - 5)
                {
                    // Spawn a new pest if there are few on screen
                    SpawnPest();
                }
            }
        }
        
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (!gameActive) return;
            
            secondsRemaining--;
            timeLabel.Text = $"Time: {secondsRemaining}";
            
            // Maybe spawn a new pest
            if (random.NextDouble() < 0.2 && pests.Count < 12 && pestsRemoved < totalPests - pests.Count)
            {
                SpawnPest();
            }
            
            // Check if time's up
            if (secondsRemaining <= 0)
            {
                EndGame(false);
            }
        }
        
        private void EndGame(bool completedAll)
        {
            // Stop the game
            gameActive = false;
            gameTimer.Stop();
            
            // Calculate score
            double percentRemoved = (double)pestsRemoved / totalPests * 100;
            double timeBonus = completedAll ? (secondsRemaining / 30.0) * 20 : 0; // Up to 20 points for speed
            double finalScore = percentRemoved + timeBonus;
            
            // Generate message
            string message;
            if (completedAll)
            {
                message = $"Great job! You removed all {totalPests} pests with {secondsRemaining} seconds remaining!";
            }
            else
            {
                message = $"Time's up! You removed {pestsRemoved} out of {totalPests} pests.";
            }
            
            // Complete the game
            CompleteGame(finalScore, message);
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Clean up
            gameTimer?.Stop();
            gameTimer?.Dispose();
            
            foreach (var pest in pests)
            {
                pest.Click -= Pest_Click;
            }
            
            base.OnFormClosing(e);
        }
    }
}