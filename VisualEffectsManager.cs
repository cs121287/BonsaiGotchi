using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BonsaiGotchi
{
    /// <summary>
    /// Manages visual effects for the bonsai tree based on its state
    /// </summary>
    public class VisualEffectsManager
    {
        private readonly Random random = new();
        private readonly RichTextBox treeDisplay;
        private readonly int treeWidth;
        private readonly int treeHeight;
        
        // Cache for effect positions
        private List<int> leafPositions = new();
        private List<int> trunkPositions = new();
        private List<int> topPositions = new();
        
        public VisualEffectsManager(RichTextBox treeDisplay, int width, int height)
        {
            this.treeDisplay = treeDisplay;
            treeWidth = width;
            treeHeight = height;
        }
        
        /// <summary>
        /// Analyzes the tree display to find different element positions
        /// </summary>
        public void AnalyzeTree()
        {
            if (treeDisplay == null) return;
            
            // Clear existing cache
            leafPositions.Clear();
            trunkPositions.Clear();
            topPositions.Clear();
            
            // Get the text
            string text = treeDisplay.Text;
            if (string.IsNullOrEmpty(text)) return;
            
            // Scan the entire text to categorize elements
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                
                // Identify leaves
                if ("●○◆◇◈◉◊⬢⬡⬟⬠⬣".Contains(c))
                {
                    leafPositions.Add(i);
                }
                // Identify trunk and branches
                else if ("║╣╠╦╩╬╧╨╤╥┃┏┓┗┛┣┫┳┻".Contains(c))
                {
                    trunkPositions.Add(i);
                }
            }
            
            // Find top positions (for snow, etc.)
            if (leafPositions.Count > 0 || trunkPositions.Count > 0)
            {
                // Scan from top to bottom, looking for the first branch or leaf in each column
                for (int col = 0; col < treeWidth; col++)
                {
                    for (int row = 0; row < treeHeight; row++)
                    {
                        int index = row * (treeWidth + 1) + col; // +1 for newline
                        
                        if (index < text.Length)
                        {
                            char c = text[index];
                            if (c != ' ' && "·˙∶\r\n".IndexOf(c) == -1) // Skip spaces and non-branch chars
                            {
                                topPositions.Add(index);
                                break; // Only get the first character in each column
                            }
                        }
                    }
                }
            }
        }
        
        #region Visual Effects
        
        /// <summary>
        /// Apply pest visuals to the tree (brown spots on leaves)
        /// </summary>
        public void ApplyPestEffect(double intensity)
        {
            if (treeDisplay == null || leafPositions.Count == 0) return;
            
            // Number of affected leaves based on intensity (0-100)
            int affectedCount = (int)Math.Ceiling(leafPositions.Count * (intensity / 100.0));
            affectedCount = Math.Min(affectedCount, leafPositions.Count);
            
            // Select random leaf positions
            var selectedPositions = GetRandomPositions(leafPositions, affectedCount);
            
            // Apply effect
            foreach (int pos in selectedPositions)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                treeDisplay.SelectionColor = Color.FromArgb(139, 69, 19); // Brown for pests
            }
        }
        
        /// <summary>
        /// Apply disease visuals to the tree (yellowing leaves)
        /// </summary>
        public void ApplyDiseaseEffect(double intensity)
        {
            if (treeDisplay == null || leafPositions.Count == 0) return;
            
            // Number of affected leaves based on intensity (0-100)
            int affectedCount = (int)Math.Ceiling(leafPositions.Count * (intensity / 100.0));
            affectedCount = Math.Min(affectedCount, leafPositions.Count);
            
            // Select random leaf positions
            var selectedPositions = GetRandomPositions(leafPositions, affectedCount);
            
            // Apply effect
            foreach (int pos in selectedPositions)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                treeDisplay.SelectionColor = Color.FromArgb(218, 165, 32); // Golden yellow for disease
            }
        }
        
        /// <summary>
        /// Apply drought visuals to the tree (brown leaves)
        /// </summary>
        public void ApplyDroughtEffect(double intensity)
        {
            if (treeDisplay == null || leafPositions.Count == 0) return;
            
            // Number of affected leaves based on intensity (0-100)
            int affectedCount = (int)Math.Ceiling(leafPositions.Count * (intensity / 100.0));
            affectedCount = Math.Min(affectedCount, leafPositions.Count);
            
            // Select random leaf positions
            var selectedPositions = GetRandomPositions(leafPositions, affectedCount);
            
            // Apply effect
            foreach (int pos in selectedPositions)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                treeDisplay.SelectionColor = Color.FromArgb(165, 42, 42); // Brown for drought
            }
        }
        
        /// <summary>
        /// Apply overwatering visuals to the tree (darkened trunk)
        /// </summary>
        public void ApplyOverwateringEffect(double intensity)
        {
            if (treeDisplay == null || trunkPositions.Count == 0) return;
            
            // Number of affected trunk positions based on intensity (0-100)
            int affectedCount = (int)Math.Ceiling(trunkPositions.Count * (intensity / 100.0));
            affectedCount = Math.Min(affectedCount, trunkPositions.Count);
            
            // Select random trunk positions
            var selectedPositions = GetRandomPositions(trunkPositions, affectedCount);
            
            // Apply effect
            foreach (int pos in selectedPositions)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                treeDisplay.SelectionColor = Color.FromArgb(70, 40, 20); // Dark brown for overwatering
            }
        }
        
        /// <summary>
        /// Apply seasonal effect to the tree based on season
        /// </summary>
        public void ApplySeasonalEffect(Season season, Weather weather)
        {
            if (treeDisplay == null) return;
            
            switch (season)
            {
                case Season.Spring:
                    ApplySpringEffect();
                    break;
                case Season.Summer:
                    ApplySummerEffect();
                    break;
                case Season.Autumn:
                    ApplyAutumnEffect();
                    break;
                case Season.Winter:
                    ApplyWinterEffect(weather == Weather.Snow);
                    break;
            }
            
            // Apply additional weather effects
            ApplyWeatherEffect(weather);
        }
        
        /// <summary>
        /// Apply spring visuals (bright green leaves, some flowers)
        /// </summary>
        private void ApplySpringEffect()
        {
            if (leafPositions.Count == 0) return;
            
            // Make all leaves bright green
            foreach (int pos in leafPositions)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                
                // Get current color
                Color currentColor = treeDisplay.SelectionColor;
                
                // Adjust to bright spring green
                int r = Math.Max(0, Math.Min(255, currentColor.R - 20));
                int g = Math.Max(0, Math.Min(255, currentColor.G + 30));
                int b = Math.Max(0, Math.Min(255, currentColor.B + 10));
                
                treeDisplay.SelectionColor = Color.FromArgb(r, g, b);
            }
            
            // Add some flowers (pink dots) to random leaves
            int flowerCount = leafPositions.Count / 10; // 10% of leaves have flowers
            var flowerPositions = GetRandomPositions(leafPositions, flowerCount);
            
            foreach (int pos in flowerPositions)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                treeDisplay.SelectionColor = Color.FromArgb(255, 182, 193); // Light pink for flowers
            }
        }
        
        /// <summary>
        /// Apply summer visuals (deep green leaves)
        /// </summary>
        private void ApplySummerEffect()
        {
            if (leafPositions.Count == 0) return;
            
            // Make all leaves deep green
            foreach (int pos in leafPositions)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                
                // Get current color
                Color currentColor = treeDisplay.SelectionColor;
                
                // Adjust to deep summer green
                int r = Math.Max(0, Math.Min(255, currentColor.R - 10));
                int g = Math.Max(0, Math.Min(255, currentColor.G - 20));
                int b = Math.Max(0, Math.Min(255, currentColor.B - 10));
                
                treeDisplay.SelectionColor = Color.FromArgb(r, g, b);
            }
        }
        
        /// <summary>
        /// Apply autumn visuals (red/orange leaves)
        /// </summary>
        private void ApplyAutumnEffect()
        {
            if (leafPositions.Count == 0) return;
            
            // Make leaves autumn colors
            foreach (int pos in leafPositions)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                
                // Choose a random autumn color
                double colorRoll = random.NextDouble();
                Color autumnColor;
                
                if (colorRoll < 0.3)
                {
                    // Red
                    autumnColor = Color.FromArgb(178, 34, 34);
                }
                else if (colorRoll < 0.6)
                {
                    // Orange
                    autumnColor = Color.FromArgb(255, 140, 0);
                }
                else if (colorRoll < 0.9)
                {
                    // Yellow
                    autumnColor = Color.FromArgb(255, 215, 0);
                }
                else
                {
                    // Brown
                    autumnColor = Color.FromArgb(139, 69, 19);
                }
                
                treeDisplay.SelectionColor = autumnColor;
            }
        }
        
        /// <summary>
        /// Apply winter visuals (sparse leaves, possibly snow)
        /// </summary>
        private void ApplyWinterEffect(bool withSnow)
        {
            if (leafPositions.Count == 0) return;
            
            // Remove most leaves for winter
            int leavesToKeep = leafPositions.Count / 5; // Keep only 20% of leaves
            var leavesToRemove = GetRandomPositions(leafPositions, leafPositions.Count - leavesToKeep);
            
            // Replace leaves with spaces
            foreach (int pos in leavesToRemove)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                treeDisplay.SelectedText = " ";
            }
            
            // Apply snow if needed
            if (withSnow && topPositions.Count > 0)
            {
                ApplySnowEffect();
            }
        }
        
        /// <summary>
        /// Apply snow effect to the tree tops
        /// </summary>
        private void ApplySnowEffect()
        {
            if (topPositions.Count == 0) return;
            
            // Add snow to about 60% of top positions
            int snowCount = (int)(topPositions.Count * 0.6);
            var snowPositions = GetRandomPositions(topPositions, snowCount);
            
            foreach (int pos in snowPositions)
            {
                treeDisplay.SelectionStart = pos;
                treeDisplay.SelectionLength = 1;
                
                // Save the original character and color
                char originalChar = treeDisplay.Text[pos];
                Color originalColor = treeDisplay.SelectionColor;
                
                // Apply snow effect (white color)
                treeDisplay.SelectionColor = Color.White;
            }
        }
        
        /// <summary>
        /// Apply weather effects to the tree
        /// </summary>
        private void ApplyWeatherEffect(Weather weather)
        {
            switch (weather)
            {
                case Weather.Rain:
                    // Nothing to do - rain is already a separate animation
                    break;
                    
                case Weather.Humid:
                    // Make leaves slightly darker and more vibrant
                    if (leafPositions.Count > 0)
                    {
                        foreach (int pos in leafPositions)
                        {
                            treeDisplay.SelectionStart = pos;
                            treeDisplay.SelectionLength = 1;
                            
                            // Get current color
                            Color currentColor = treeDisplay.SelectionColor;
                            
                            // Adjust for humidity
                            int r = Math.Max(0, Math.Min(255, currentColor.R - 10));
                            int g = Math.Max(0, Math.Min(255, currentColor.G + 10));
                            int b = Math.Max(0, Math.Min(255, currentColor.B + 10));
                            
                            treeDisplay.SelectionColor = Color.FromArgb(r, g, b);
                        }
                    }
                    break;
                    
                case Weather.Wind:
                    // Visual "leaning" effect - shift some leaf positions
                    if (leafPositions.Count > 0)
                    {
                        int leavesToShift = leafPositions.Count / 3; // Shift 1/3 of leaves
                        var shiftPositions = GetRandomPositions(leafPositions, leavesToShift);
                        
                        foreach (int pos in shiftPositions)
                        {
                            // Only process if we can shift right
                            if (pos + 1 < treeDisplay.Text.Length && treeDisplay.Text[pos + 1] == ' ')
                            {
                                treeDisplay.SelectionStart = pos;
                                treeDisplay.SelectionLength = 1;
                                char leaf = treeDisplay.Text[pos];
                                Color leafColor = treeDisplay.SelectionColor;
                                
                                // Replace with space
                                treeDisplay.SelectedText = " ";
                                
                                // Place leaf to the right
                                treeDisplay.SelectionStart = pos + 1;
                                treeDisplay.SelectionLength = 1;
                                treeDisplay.SelectedText = leaf.ToString();
                                treeDisplay.SelectionStart = pos + 1;
                                treeDisplay.SelectionLength = 1;
                                treeDisplay.SelectionColor = leafColor;
                            }
                        }
                    }
                    break;
                    
                case Weather.Storm:
                    // Dramatic effect - remove some leaves and shift others
                    if (leafPositions.Count > 0)
                    {
                        int leavesToRemove = leafPositions.Count / 4; // Remove 1/4 of leaves
                        var removePositions = GetRandomPositions(leafPositions, leavesToRemove);
                        
                        foreach (int pos in removePositions)
                        {
                            treeDisplay.SelectionStart = pos;
                            treeDisplay.SelectionLength = 1;
                            treeDisplay.SelectedText = " ";
                        }
                        
                        // Then apply wind effect on remaining leaves
                        ApplyWeatherEffect(Weather.Wind);
                    }
                    break;
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Get a random subset of positions
        /// </summary>
        private List<int> GetRandomPositions(List<int> sourceList, int count)
        {
            // Create a copy of the source list
            List<int> positions = new(sourceList);
            List<int> result = new();
            
            // Shuffle and take the first 'count' elements
            int n = Math.Min(count, positions.Count);
            for (int i = 0; i < n; i++)
            {
                int index = random.Next(positions.Count);
                result.Add(positions[index]);
                positions.RemoveAt(index);
            }
            
            return result;
        }
        
        #endregion
    }
}