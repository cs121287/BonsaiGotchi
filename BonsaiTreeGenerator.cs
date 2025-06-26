using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BonsaiTreeGenerator
{
    /// <summary>
    /// Main class for generating ASCII art bonsai trees
    /// </summary>
    public class BonsaiTreeGenerator
    {
        private readonly Random random;
        private readonly int seed;

        // Character sets for tree components
        private const string LeafChars = "●○◆◇◈◉◊⬢⬡";
        private const string BranchChars = "║╣╠╦╩╬╧╨╤╥┃┏┓┗┛┣┫┳┻";
        private const string TrunkChars = "║│┃";

        // Density influences how full the tree appears
        private double density;
        
        // Size parameters
        private int width;
        private int height;
        
        /// <summary>
        /// Creates a new bonsai tree generator with optional specified seed
        /// </summary>
        public BonsaiTreeGenerator(Random randomGenerator = null)
        {
            // Use provided random or create a new one
            random = randomGenerator ?? new Random();
            seed = random.Next();
            
            // Set default parameters
            width = 30;
            height = 20;
            density = 0.7;
        }

        /// <summary>
        /// Generate a bonsai tree with specified options
        /// </summary>
        public BonsaiTree GenerateTree(GenerationOptions options = null)
        {
            // Use provided options or create default
            options = options ?? new GenerationOptions();
            
            // Set random seed if specified
            Random treeRandom;
            if (options.Seed.HasValue)
            {
                treeRandom = new Random(options.Seed.Value);
            }
            else
            {
                treeRandom = random;
            }
            
            // Create a new tree with specified or default size
            BonsaiTree tree = new BonsaiTree(
                options.CanvasWidth > 0 ? options.CanvasWidth : width,
                options.CanvasHeight > 0 ? options.CanvasHeight : height);
            
            // Generate tree elements based on options
            GenerateTreeElements(tree, options, treeRandom);
            
            return tree;
        }
        
        /// <summary>
        /// Generate tree elements based on options
        /// </summary>
        private void GenerateTreeElements(BonsaiTree tree, GenerationOptions options, Random treeRandom)
        {
            int treeWidth = tree.Width;
            int treeHeight = tree.Height;
            
            // Calculate tree dimensions
            int trunkHeight = options.TreeHeight > 0 ? options.TreeHeight : treeHeight / 2;
            int trunkWidth = options.TrunkWidth > 0 ? options.TrunkWidth : 1;
            
            // Determine trunk position (applying tilt)
            int trunkBaseX = treeWidth / 2;
            int trunkTopX = trunkBaseX;
            
            // Apply tilt if specified
            if (options.TreeTilt != 0)
            {
                // Tilt can be -1.0 to 1.0, positive is tilt right
                int tiltAmount = (int)(options.TreeTilt * treeWidth / 4);
                trunkTopX = trunkBaseX + tiltAmount;
                trunkTopX = Math.Max(trunkWidth, Math.Min(treeWidth - trunkWidth, trunkTopX));
            }
            
            // Draw the pot
            DrawPot(tree, trunkBaseX, trunkWidth * 3);
            
            // Draw trunk
            DrawTrunk(tree, trunkBaseX, trunkTopX, trunkHeight, trunkWidth, options.TrunkCurve, treeRandom);
            
            // Draw branches
            int branchCount = options.BranchCount > 0 ? options.BranchCount : 5;
            DrawBranches(tree, trunkBaseX, trunkTopX, trunkHeight, branchCount, options.BranchSymmetry, options.GrowthDirection, treeRandom);
            
            // Draw foliage
            double leafDensity = options.LeafDensity > 0 ? options.LeafDensity : density;
            DrawFoliage(tree, trunkTopX, trunkHeight, leafDensity, options.WindEffect, treeRandom);
            
            // Apply leaf quality adjustments if specified
            if (options.LeafQuality < 1.0)
            {
                AdjustLeafQuality(tree, options.LeafQuality, treeRandom);
            }
        }
        
        /// <summary>
        /// Draw the pot at the bottom of the tree
        /// </summary>
        private void DrawPot(BonsaiTree tree, int centerX, int potWidth)
        {
            int potHeight = 2;
            int potY = tree.Height - potHeight;
            
            // Calculate pot bounds
            int potLeft = Math.Max(0, centerX - potWidth / 2);
            int potRight = Math.Min(tree.Width - 1, potLeft + potWidth);
            
            // Draw the top line of the pot
            for (int x = potLeft; x <= potRight; x++)
            {
                tree.SetCharAt(x, potY, '─');
                tree.SetColorAt(x, potY, Color.SaddleBrown);
            }
            
            // Draw the sides of the pot
            potY++;
            tree.SetCharAt(potLeft, potY, '╰');
            tree.SetColorAt(potLeft, potY, Color.SaddleBrown);
            
            tree.SetCharAt(potRight, potY, '╯');
            tree.SetColorAt(potRight, potY, Color.SaddleBrown);
            
            // Draw the bottom of the pot
            for (int x = potLeft + 1; x < potRight; x++)
            {
                tree.SetCharAt(x, potY, '─');
                tree.SetColorAt(x, potY, Color.SaddleBrown);
            }
        }
        
        /// <summary>
        /// Draw the trunk
        /// </summary>
        private void DrawTrunk(BonsaiTree tree, int baseX, int topX, int height, int width, double curveFactor, Random treeRandom)
        {
            // Calculate trunk bounds
            int trunkBottom = tree.Height - 3; // Just above pot
            int trunkTop = Math.Max(0, trunkBottom - height);
            
            // Draw a curved trunk from bottom to top
            for (int y = trunkBottom; y >= trunkTop; y--)
            {
                // Calculate x position along the trunk with curve
                double progress = (double)(trunkBottom - y) / (trunkBottom - trunkTop);
                
                // Apply curve using sine wave
                double curveOffset = 0;
                if (curveFactor > 0)
                {
                    // Sine wave curve with amplitude based on curve factor
                    curveOffset = Math.Sin(progress * Math.PI) * curveFactor * width * 2;
                }
                
                // Calculate x position with linear interpolation and curve
                int x = (int)(baseX + (topX - baseX) * progress + curveOffset);
                x = Math.Max(0, Math.Min(tree.Width - 1, x));
                
                // Draw trunk with thickness
                for (int w = -width / 2; w <= width / 2; w++)
                {
                    int drawX = x + w;
                    if (drawX >= 0 && drawX < tree.Width)
                    {
                        char trunkChar = GetRandomChar(TrunkChars, treeRandom);
                        tree.SetCharAt(drawX, y, trunkChar);
                        tree.SetColorAt(drawX, y, Color.FromArgb(139, 69, 19)); // Brown
                    }
                }
            }
        }
        
        /// <summary>
        /// Draw branches from the trunk
        /// </summary>
        private void DrawBranches(BonsaiTree tree, int baseX, int topX, int height, int count, double symmetry, double growthDirection, Random treeRandom)
        {
            // Calculate trunk positions
            int trunkBottom = tree.Height - 3;
            int trunkTop = Math.Max(0, trunkBottom - height);
            
            // Space branches evenly
            int spacing = height / (count + 1);
            
            for (int i = 1; i <= count; i++)
            {
                // Calculate y position for this branch
                int y = trunkBottom - i * spacing;
                if (y <= trunkTop) continue;
                
                // Calculate x position along the trunk
                double progress = (double)(trunkBottom - y) / (trunkBottom - trunkTop);
                int x = (int)(baseX + (topX - baseX) * progress);
                
                // Branch lengths
                int leftLength = treeRandom.Next(2, 8);
                int rightLength = leftLength;
                
                // Adjust for asymmetry
                if (symmetry < 1.0)
                {
                    double asymmetry = 1.0 - symmetry;
                    if (treeRandom.NextDouble() < asymmetry)
                    {
                        // Make branches asymmetric
                        leftLength = (int)(leftLength * (0.5 + treeRandom.NextDouble() * 0.5));
                        rightLength = (int)(rightLength * (0.5 + treeRandom.NextDouble() * 0.5));
                    }
                }
                
                // Adjust for growth direction
                if (growthDirection != 0)
                {
                    // Positive growth direction favors right branches
                    if (growthDirection > 0)
                    {
                        leftLength = (int)(leftLength * (1.0 - growthDirection * 0.5));
                        rightLength = (int)(rightLength * (1.0 + growthDirection * 0.5));
                    }
                    // Negative growth direction favors left branches
                    else
                    {
                        leftLength = (int)(leftLength * (1.0 + Math.Abs(growthDirection) * 0.5));
                        rightLength = (int)(rightLength * (1.0 - Math.Abs(growthDirection) * 0.5));
                    }
                }
                
                // Draw left branch
                for (int j = 1; j <= leftLength; j++)
                {
                    int drawX = x - j;
                    if (drawX >= 0)
                    {
                        char branchChar = j == 1 ? '╠' : '═';
                        tree.SetCharAt(drawX, y, branchChar);
                        tree.SetColorAt(drawX, y, Color.FromArgb(139, 69, 19)); // Brown
                    }
                }
                
                // Draw right branch
                for (int j = 1; j <= rightLength; j++)
                {
                    int drawX = x + j;
                    if (drawX < tree.Width)
                    {
                        char branchChar = j == 1 ? '╣' : '═';
                        tree.SetCharAt(drawX, y, branchChar);
                        tree.SetColorAt(drawX, y, Color.FromArgb(139, 69, 19)); // Brown
                    }
                }
            }
        }
        
        /// <summary>
        /// Draw foliage around the top of the tree
        /// </summary>
        private void DrawFoliage(BonsaiTree tree, int topX, int height, double density, double windEffect, Random treeRandom)
        {
            // Calculate foliage bounds
            int trunkBottom = tree.Height - 3;
            int trunkTop = Math.Max(0, trunkBottom - height);
            int foliageTop = Math.Max(0, trunkTop - 5);
            
            // Calculate foliage width
            int foliageWidth = (int)(tree.Width * 0.6);
            int foliageLeft = Math.Max(0, topX - foliageWidth / 2);
            int foliageRight = Math.Min(tree.Width - 1, foliageLeft + foliageWidth);
            
            // Apply wind effect to shift foliage
            if (windEffect != 0)
            {
                // Shift foliage in wind direction
                int shift = (int)(windEffect * foliageWidth * 0.3);
                foliageLeft += shift;
                foliageRight += shift;
                
                // Ensure bounds
                foliageLeft = Math.Max(0, foliageLeft);
                foliageRight = Math.Min(tree.Width - 1, foliageRight);
            }
            
            // Draw foliage with random leaves
            for (int y = foliageTop; y <= trunkTop + 2; y++)
            {
                for (int x = foliageLeft; x <= foliageRight; x++)
                {
                    // Skip if position already has a character
                    if (tree.GetCharAt(x, y) != ' ') continue;
                    
                    // Apply density check
                    if (treeRandom.NextDouble() < density * GetFoliageDensityFactor(x, y, topX, trunkTop, windEffect))
                    {
                        char leafChar = GetRandomChar(LeafChars, treeRandom);
                        tree.SetCharAt(x, y, leafChar);
                        
                        // Different greens for variety
                        Color leafColor = GetLeafColor(treeRandom);
                        tree.SetColorAt(x, y, leafColor);
                    }
                }
            }
        }
        
        /// <summary>
        /// Calculate density factor based on position and wind
        /// </summary>
        private double GetFoliageDensityFactor(int x, int y, int centerX, int centerY, double windEffect)
        {
            // Base density decreases with distance from center
            double distX = (x - centerX) / (double)centerX;
            double distY = (y - centerY) / (double)centerY;
            double dist = Math.Sqrt(distX * distX + distY * distY);
            
            // Base factor decreases with distance
            double factor = Math.Max(0, 1.0 - dist * 1.5);
            
            // Apply wind effect
            if (windEffect != 0)
            {
                // Wind pushes foliage to one side
                double windFactor = windEffect * 2; // -2.0 to 2.0
                double xOffset = (x - centerX) / (double)centerX; // -1.0 to 1.0
                
                // Higher density in wind direction
                if (windEffect > 0 && xOffset > 0) // Right wind
                {
                    factor *= (1.0 + windFactor * xOffset);
                }
                else if (windEffect < 0 && xOffset < 0) // Left wind
                {
                    factor *= (1.0 - windFactor * xOffset);
                }
            }
            
            return factor;
        }
        
        /// <summary>
        /// Adjust leaf quality based on health factor
        /// </summary>
        private void AdjustLeafQuality(BonsaiTree tree, double qualityFactor, Random treeRandom)
        {
            // Iterate through tree and adjust leaf colors
            for (int y = 0; y < tree.Height; y++)
            {
                for (int x = 0; x < tree.Width; x++)
                {
                    // Check if this is a leaf character
                    if (LeafChars.Contains(tree.GetCharAt(x, y).ToString()))
                    {
                        // Chance to modify leaf based on quality factor
                        if (treeRandom.NextDouble() > qualityFactor)
                        {
                            // Determine if we change color or remove leaf
                            if (treeRandom.NextDouble() < 0.7)
                            {
                                // Change to yellowed/brown leaf
                                Color[] poorColors = {
                                    Color.FromArgb(205, 133, 63), // Brown
                                    Color.FromArgb(218, 165, 32), // Golden
                                    Color.FromArgb(210, 180, 140)  // Tan
                                };
                                
                                tree.SetColorAt(x, y, poorColors[treeRandom.Next(poorColors.Length)]);
                            }
                            else
                            {
                                // Remove leaf entirely
                                tree.SetCharAt(x, y, ' ');
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Get a random leaf color
        /// </summary>
        private Color GetLeafColor(Random treeRandom)
        {
            // Different shades of green
            Color[] greens = {
                Color.FromArgb(0, 128, 0),      // Green
                Color.FromArgb(34, 139, 34),    // Forest Green
                Color.FromArgb(0, 100, 0),      // Dark Green
                Color.FromArgb(50, 205, 50),    // Lime Green
                Color.FromArgb(107, 142, 35)    // Olive Drab
            };
            
            return greens[treeRandom.Next(greens.Length)];
        }

        /// <summary>
        /// Get a random character from the provided character set
        /// </summary>
        private char GetRandomChar(string charSet, Random treeRandom)
        {
            return charSet[treeRandom.Next(charSet.Length)];
        }
        
        /// <summary>
        /// Get the seed used for the generator
        /// </summary>
        public int GetSeed()
        {
            return seed;
        }
    }
    
    /// <summary>
    /// Generation options for tree customization
    /// </summary>
    public class GenerationOptions
    {
        // Basic options
        public int? Seed { get; set; }
        public int CanvasWidth { get; set; } = 60;
        public int CanvasHeight { get; set; } = 30;
        
        // Tree structure
        public int TreeHeight { get; set; } = 0;
        public int TrunkWidth { get; set; } = 0;
        public double TreeTilt { get; set; } = 0;  // -1.0 to 1.0
        public double TrunkCurve { get; set; } = 0; // 0.0 to 1.0
        
        // Branches
        public int BranchCount { get; set; } = 0;
        public double BranchSymmetry { get; set; } = 1.0; // 0.0 to 1.0
        public double GrowthDirection { get; set; } = 0; // -1.0 to 1.0, negative favors left
        
        // Foliage
        public double LeafDensity { get; set; } = 0.5; // 0.0 to 1.0
        public double LeafQuality { get; set; } = 1.0; // 0.0 to 1.0
        public double WindEffect { get; set; } = 0; // -1.0 to 1.0, negative is left wind
    }
    
    /// <summary>
    /// Represents a generated bonsai tree with its visual representation
    /// </summary>
    public class BonsaiTree
    {
        // Tree dimensions
        public int Width { get; }
        public int Height { get; }
        
        // Character and color grids
        private readonly char[,] characterGrid;
        private readonly Color[,] colorGrid;
        
        /// <summary>
        /// Create a new empty bonsai tree
        /// </summary>
        public BonsaiTree(int width, int height)
        {
            Width = width;
            Height = height;
            characterGrid = new char[height, width];
            colorGrid = new Color[height, width];
            
            // Initialize with spaces
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    characterGrid[i, j] = ' ';
                    colorGrid[i, j] = Color.Black;
                }
            }
        }
        
        /// <summary>
        /// Set the character at the specified position
        /// </summary>
        public void SetCharAt(int x, int y, char c)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                characterGrid[y, x] = c;
            }
        }
        
        /// <summary>
        /// Get the character at the specified position
        /// </summary>
        public char GetCharAt(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return characterGrid[y, x];
            }
            return ' ';
        }
        
        /// <summary>
        /// Set the color at the specified position
        /// </summary>
        public void SetColorAt(int x, int y, Color color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                colorGrid[y, x] = color;
            }
        }
        
        /// <summary>
        /// Get the color at the specified position
        /// </summary>
        public Color GetColorAt(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return colorGrid[y, x];
            }
            return Color.Black;
        }
        
        /// <summary>
        /// Convert the tree to a string representation
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    sb.Append(characterGrid[i, j]);
                }
                if (i < Height - 1)
                {
                    sb.AppendLine();
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get a dictionary mapping of characters to colors
        /// </summary>
        public static Dictionary<string, Color> GetColorMapping()
        {
            Dictionary<string, Color> mapping = new Dictionary<string, Color>();
            
            // Tree elements
            mapping["♣"] = Color.ForestGreen; // Leaves
            mapping["♠"] = Color.ForestGreen; // Leaves
            mapping["●"] = Color.ForestGreen; // Leaves
            mapping["○"] = Color.ForestGreen; // Leaves
            mapping["◆"] = Color.ForestGreen; // Leaves
            mapping["◇"] = Color.ForestGreen; // Leaves
            mapping["◈"] = Color.LimeGreen;   // Leaves
            mapping["◉"] = Color.LimeGreen;   // Leaves
            mapping["◊"] = Color.Green;       // Leaves
            mapping["⬢"] = Color.Green;       // Leaves
            mapping["⬡"] = Color.Green;       // Leaves
            
            // Trunk and branches
            mapping["║"] = Color.SaddleBrown; // Trunk
            mapping["│"] = Color.SaddleBrown; // Trunk
            mapping["┃"] = Color.SaddleBrown; // Trunk
            mapping["╣"] = Color.SaddleBrown; // Branch connector
            mapping["╠"] = Color.SaddleBrown; // Branch connector
            mapping["╦"] = Color.SaddleBrown; // Branch connector
            mapping["╩"] = Color.SaddleBrown; // Branch connector
            mapping["╬"] = Color.SaddleBrown; // Branch connector
            mapping["═"] = Color.SaddleBrown; // Branch
            
            // Pot elements
            mapping["─"] = Color.SaddleBrown; // Pot
            mapping["╰"] = Color.SaddleBrown; // Pot corner
            mapping["╯"] = Color.SaddleBrown; // Pot corner
            
            return mapping;
        }
    }
}