using System;
using System.Drawing;
using System.Windows.Forms;

namespace BonsaiGotchi.MiniGames
{
    /// <summary>
    /// Base class for all mini-games
    /// </summary>
    public abstract class MiniGameBase : Form
    {
        protected readonly Random random = new Random();
        
        // Game result
        public double Score { get; protected set; }
        public bool IsCompleted { get; protected set; }
        public string ResultMessage { get; protected set; }
        
        // Common UI elements
        protected Label titleLabel;
        protected Label instructionsLabel;
        protected Button closeButton;
        
        public MiniGameBase()
        {
            // Common setup for all mini-games
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 249, 250);
            
            // Initialize common controls
            InitializeCommonControls();
            
            // Apply common styling
            ApplyGameStyling();
        }
        
        private void InitializeCommonControls()
        {
            titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 50,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White
            };
            
            instructionsLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(10)
            };
            
            closeButton = new Button
            {
                Text = "Close",
                Size = new Size(100, 40),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            closeButton.FlatAppearance.BorderSize = 0;
            
            closeButton.Click += (s, e) => Close();
            
            Controls.Add(closeButton);
            Controls.Add(instructionsLabel);
            Controls.Add(titleLabel);
        }
        
        protected virtual void ApplyGameStyling()
        {
            // Base styling that can be overridden by specific games
        }
        
        /// <summary>
        /// Show result and update score
        /// </summary>
        protected void CompleteGame(double score, string message)
        {
            Score = Math.Max(0, Math.Min(100, score));
            ResultMessage = message;
            IsCompleted = true;
            
            // Show result dialog
            MessageBox.Show(
                $"{message}\n\nScore: {Score:0.0}/100",
                "Game Result",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            
            // Close automatically after showing result
            Close();
        }
    }
}