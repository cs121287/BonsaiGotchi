using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BonsaiGotchi.MiniGames
{
    /// <summary>
    /// Mini-game for pruning the bonsai through a pattern-matching puzzle
    /// </summary>
    public class PruningPuzzleGame : MiniGameBase
    {
        private Panel gamePanel;
        private Panel targetPanel;
        private Panel currentPanel;
        private Button[] cutButtons;
        private Label moveCounter;
        private Label difficultyLabel;

        private int gridSize = 5; // 5x5 grid
        private bool[,] targetPattern;
        private bool[,] currentPattern;
        private int movesUsed = 0;
        private int maxMoves = 12;
        private int difficulty = 1; // 1-3 scale

        public PruningPuzzleGame() : base()
        {
            // Form setup
            Text = "Pruning Puzzle";
            Size = new Size(700, 600);

            // Set game-specific labels
            titleLabel.Text = "Bonsai Pruning Puzzle";
            instructionsLabel.Text = "Prune your bonsai by clicking the cut buttons to match the target pattern.\nYou have a limited number of cuts to shape your tree perfectly.";

            // Create game layout
            CreateGameLayout();

            // Initialize game
            InitializeGame();
        }

        private void CreateGameLayout()
        {
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Create top control panel
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };

            // Moves counter
            moveCounter = new Label
            {
                Text = "Cuts: 0 / 12",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            // Difficulty label
            difficultyLabel = new Label
            {
                Text = "Difficulty: Easy",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(200, 10)
            };

            // Reset button
            Button resetButton = new Button
            {
                Text = "Reset",
                Size = new Size(100, 30),
                Location = new Point(350, 5),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            resetButton.FlatAppearance.BorderSize = 0;
            resetButton.Click += (s, e) => InitializeGame();

            topPanel.Controls.Add(moveCounter);
            topPanel.Controls.Add(difficultyLabel);
            topPanel.Controls.Add(resetButton);

            // Create game container with two panels side by side
            Panel gameContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Target pattern panel (left)
            Panel targetContainer = new Panel
            {
                Width = 300,
                Dock = DockStyle.Left,
                Padding = new Padding(5)
            };

            Label targetLabel = new Label
            {
                Text = "Target Shape",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            targetPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };

            targetContainer.Controls.Add(targetPanel);
            targetContainer.Controls.Add(targetLabel);

            // Current pattern panel (right)
            Panel currentContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            Label currentLabel = new Label
            {
                Text = "Your Bonsai",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            currentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };

            currentContainer.Controls.Add(currentPanel);
            currentContainer.Controls.Add(currentLabel);

            // Add cut buttons to the grid (top, right, bottom, left)
            CreateCutButtons();

            gameContainer.Controls.Add(currentContainer);
            gameContainer.Controls.Add(targetContainer);

            mainPanel.Controls.Add(gameContainer);
            mainPanel.Controls.Add(topPanel);

            Controls.Add(mainPanel);
            closeButton.BringToFront();
        }

        private void CreateCutButtons()
        {
            cutButtons = new Button[4]; // Top, Right, Bottom, Left

            // Top cut button
            cutButtons[0] = new Button
            {
                Text = "▼",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(80, 30),
                Location = new Point(460, 160),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = 0
            };

            // Right cut button
            cutButtons[1] = new Button
            {
                Text = "◄",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(30, 80),
                Location = new Point(590, 270),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = 1
            };

            // Bottom cut button
            cutButtons[2] = new Button
            {
                Text = "▲",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(80, 30),
                Location = new Point(460, 380),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = 2
            };

            // Left cut button
            cutButtons[3] = new Button
            {
                Text = "►",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(30, 80),
                Location = new Point(380, 270),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Tag = 3
            };

            foreach (Button btn in cutButtons)
            {
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += CutButton_Click;
                Controls.Add(btn);
                btn.BringToFront();
            }
        }

        private void InitializeGame()
        {
            // Set difficulty based on random number or could be passed in
            difficulty = random.Next(1, 4); // 1, 2, or 3

            // Adjust max moves based on difficulty
            maxMoves = difficulty switch
            {
                1 => 12,
                2 => 10,
                3 => 8,
                _ => 12
            };

            difficultyLabel.Text = difficulty switch
            {
                1 => "Difficulty: Easy",
                2 => "Difficulty: Medium",
                3 => "Difficulty: Hard",
                _ => "Difficulty: Easy"
            };

            // Reset moves counter
            movesUsed = 0;
            UpdateMovesDisplay();

            // Generate target pattern based on difficulty
            targetPattern = GenerateRandomPattern(difficulty);

            // Initialize current pattern (all cells filled)
            currentPattern = new bool[gridSize, gridSize];
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    currentPattern[i, j] = true;
                }
            }

            // Display patterns
            DrawPatterns();

            // Enable/reset buttons
            foreach (Button btn in cutButtons)
            {
                btn.Enabled = true;
            }
        }

        private bool[,] GenerateRandomPattern(int difficulty)
        {
            bool[,] pattern = new bool[gridSize, gridSize];

            // Start with all cells filled
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    pattern[i, j] = true;
                }
            }

            // Make cuts based on difficulty
            int cuts = difficulty switch
            {
                1 => random.Next(2, 5), // Easy: 2-4 cuts
                2 => random.Next(4, 7), // Medium: 4-6 cuts
                3 => random.Next(6, 9), // Hard: 6-8 cuts
                _ => random.Next(2, 5)
            };

            // Make random cuts to generate the target pattern
            for (int i = 0; i < cuts; i++)
            {
                int cutDirection = random.Next(4); // 0=top, 1=right, 2=bottom, 3=left
                int cutPosition = random.Next(gridSize);

                MakeCut(pattern, cutDirection, cutPosition);
            }

            return pattern;
        }

        private void MakeCut(bool[,] pattern, int direction, int position)
        {
            switch (direction)
            {
                case 0: // Cut from top
                    for (int i = 0; i <= position; i++)
                    {
                        for (int j = 0; j < gridSize; j++)
                        {
                            pattern[i, j] = false;
                        }
                    }
                    break;
                case 1: // Cut from right
                    for (int i = 0; i < gridSize; i++)
                    {
                        for (int j = position; j < gridSize; j++)
                        {
                            pattern[i, j] = false;
                        }
                    }
                    break;
                case 2: // Cut from bottom
                    for (int i = position; i < gridSize; i++)
                    {
                        for (int j = 0; j < gridSize; j++)
                        {
                            pattern[i, j] = false;
                        }
                    }
                    break;
                case 3: // Cut from left
                    for (int i = 0; i < gridSize; i++)
                    {
                        for (int j = 0; j <= position; j++)
                        {
                            pattern[i, j] = false;
                        }
                    }
                    break;
            }
        }

        private void DrawPatterns()
        {
            // Clear both panels
            targetPanel.Controls.Clear();
            currentPanel.Controls.Clear();

            // Calculate cell size
            int cellSize = Math.Min(targetPanel.Width, targetPanel.Height) / gridSize - 4;

            // Draw target pattern
            DrawPatternOnPanel(targetPanel, targetPattern, cellSize, Color.ForestGreen);

            // Draw current pattern
            DrawPatternOnPanel(currentPanel, currentPattern, cellSize, Color.ForestGreen);
        }

        private void DrawPatternOnPanel(Panel panel, bool[,] pattern, int cellSize, Color color)
        {
            // Calculate offset to center pattern
            int offsetX = (panel.Width - (gridSize * cellSize)) / 2;
            int offsetY = (panel.Height - (gridSize * cellSize)) / 2;

            // Draw each cell
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    if (pattern[i, j])
                    {
                        Panel cell = new Panel
                        {
                            Size = new Size(cellSize, cellSize),
                            Location = new Point(offsetX + j * cellSize, offsetY + i * cellSize),
                            BackColor = color
                        };

                        // Rounded corners for cells
                        cell.Paint += (s, e) =>
                        {
                            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            using var brush = new SolidBrush(color);
                            e.Graphics.FillEllipse(brush, 0, 0, cellSize, cellSize);
                        };

                        panel.Controls.Add(cell);
                    }
                }
            }

            // Draw grid lines
            for (int i = 0; i <= gridSize; i++)
            {
                // Horizontal lines
                Panel hLine = new Panel
                {
                    Size = new Size(gridSize * cellSize, 1),
                    Location = new Point(offsetX, offsetY + i * cellSize),
                    BackColor = Color.LightGray
                };

                // Vertical lines
                Panel vLine = new Panel
                {
                    Size = new Size(1, gridSize * cellSize),
                    Location = new Point(offsetX + i * cellSize, offsetY),
                    BackColor = Color.LightGray
                };

                panel.Controls.Add(hLine);
                panel.Controls.Add(vLine);
            }
        }

        private void CutButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is int direction)
            {
                // Show cut position selector
                int position = ShowCutPositionSelector(direction);
                
                if (position >= 0 && position < gridSize)
                {
                    // Make the cut
                    MakeCut(currentPattern, direction, position);
                    
                    // Increment moves counter
                    movesUsed++;
                    UpdateMovesDisplay();
                    
                    // Redraw patterns
                    DrawPatterns();
                    
                    // Check for game end conditions
                    CheckGameStatus();
                }
            }
        }
        
        private int ShowCutPositionSelector(int direction)
        {
            // Create a form to select cut position
            using Form selectorForm = new Form
            {
                Text = "Select Cut Position",
                Size = new Size(300, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label instructionLabel = new Label
            {
                Text = direction switch
                {
                    0 => "Select row from top:",
                    1 => "Select column from right:",
                    2 => "Select row from bottom:",
                    3 => "Select column from left:",
                    _ => "Select position:"
                },
                AutoSize = true,
                Location = new Point(20, 20)
            };
            
            TrackBar positionSlider = new TrackBar
            {
                Minimum = 0,
                Maximum = gridSize - 1,
                Value = gridSize / 2,
                TickFrequency = 1,
                Width = 250,
                Location = new Point(20, 50)
            };
            
            Label positionLabel = new Label
            {
                Text = $"Position: {positionSlider.Value + 1}",
                AutoSize = true,
                Location = new Point(20, 80)
            };
            
            positionSlider.ValueChanged += (s, e) => 
            {
                positionLabel.Text = $"Position: {positionSlider.Value + 1}";
            };
            
            Button confirmButton = new Button
            {
                Text = "Cut",
                DialogResult = DialogResult.OK,
                Location = new Point(110, 80),
                Size = new Size(80, 30)
            };
            
            selectorForm.Controls.Add(instructionLabel);
            selectorForm.Controls.Add(positionSlider);
            selectorForm.Controls.Add(positionLabel);
            selectorForm.Controls.Add(confirmButton);
            
            // Show the selector and return the selected position
            if (selectorForm.ShowDialog() == DialogResult.OK)
            {
                return positionSlider.Value;
            }
            
            return -1; // Cancelled
        }
        
        private void UpdateMovesDisplay()
        {
            moveCounter.Text = $"Cuts: {movesUsed} / {maxMoves}";
        }
        
        private void CheckGameStatus()
        {
            // Check if patterns match
            bool patternsMatch = true;
            
            for (int i = 0; i < gridSize && patternsMatch; i++)
            {
                for (int j = 0; j < gridSize && patternsMatch; j++)
                {
                    if (targetPattern[i, j] != currentPattern[i, j])
                    {
                        patternsMatch = false;
                    }
                }
            }
            
            // Check if out of moves
            bool outOfMoves = movesUsed >= maxMoves;
            
            // End game conditions
            if (patternsMatch || outOfMoves)
            {
                // Disable cut buttons
                foreach (Button btn in cutButtons)
                {
                    btn.Enabled = false;
                }
                
                // Calculate score
                double score;
                string message;
                
                if (patternsMatch)
                {
                    // Calculate score based on efficiency (fewer moves = better score)
                    score = 100.0 * (1.0 - (double)movesUsed / maxMoves);
                    score = Math.Max(60, score); // Minimum score for completing is 60%
                    
                    message = $"Perfect pruning! You matched the target shape in {movesUsed} cuts.";
                }
                else
                {
                    // Calculate partial score based on how many cells match
                    int totalCells = gridSize * gridSize;
                    int matchingCells = 0;
                    
                    for (int i = 0; i < gridSize; i++)
                    {
                        for (int j = 0; j < gridSize; j++)
                        {
                            if (targetPattern[i, j] == currentPattern[i, j])
                            {
                                matchingCells++;
                            }
                        }
                    }
                    
                    score = 50.0 * matchingCells / totalCells; // Max 50% for partial match
                    message = $"Out of cuts! Your bonsai is {(score*2):0}% similar to the target shape.";
                }
                
                // End the game
                CompleteGame(score, message);
            }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Clean up event handlers
            foreach (Button btn in cutButtons)
            {
                btn.Click -= CutButton_Click;
            }
            
            base.OnFormClosing(e);
        }
    }
}