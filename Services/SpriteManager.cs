using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Services
{
    public static class SpriteManager
    {
        // Default sprite dimensions
        private const int SpriteWidth = 256;
        private const int SpriteHeight = 256;
        private static readonly object _createLock = new object(); // Add lock for thread safety

        public static void EnsureSpritesExist()
        {
            try
            {
                Console.WriteLine("Ensuring sprite files exist...");

                // Define possible sprite directories - Use AppContext.BaseDirectory instead of Assembly.Location
                string[] directories = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Images"),
                    Path.Combine(AppContext.BaseDirectory, "Assets", "Images")
                };

                bool success = false;

                // Try to create sprites in the first directory that exists or can be created
                foreach (var directory in directories)
                {
                    try
                    {
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                            Console.WriteLine($"Created directory: {directory}");
                        }

                        lock (_createLock) // Ensure thread safety when creating sprites
                        {
                            // Create all the required sprites
                            CreateSprite(Path.Combine(directory, "idle.png"), BonsaiState.Idle);
                            CreateSprite(Path.Combine(directory, "growing.png"), BonsaiState.Growing);
                            CreateSprite(Path.Combine(directory, "blooming.png"), BonsaiState.Blooming);
                            CreateSprite(Path.Combine(directory, "sleeping.png"), BonsaiState.Sleeping);
                            CreateSprite(Path.Combine(directory, "thirsty.png"), BonsaiState.Thirsty);
                            CreateSprite(Path.Combine(directory, "unhealthy.png"), BonsaiState.Unhealthy);
                            CreateSprite(Path.Combine(directory, "wilting.png"), BonsaiState.Wilting);

                            // Create fallback.png file if it doesn't exist
                            CreateFallbackSprite(Path.Combine(directory, "fallback.png"));

                            // Also create growth stage specific sprites
                            foreach (GrowthStage stage in Enum.GetValues<GrowthStage>())
                            {
                                string stageName = stage.ToString().ToLower();
                                CreateGrowthStageSprite(Path.Combine(directory, $"bonsai_{stageName}.png"), stage);

                                // Create stage + state combination sprites
                                foreach (BonsaiState state in Enum.GetValues<BonsaiState>())
                                {
                                    string stateName = state.ToString().ToLower();
                                    CreateGrowthStageStateSprite(
                                        Path.Combine(directory, $"bonsai_{stageName}_{stateName}.png"),
                                        stage,
                                        state);
                                }
                            }

                            // Create lowercase "images" directory and add fallback image there too
                            // FIX: Add null check for Path.GetDirectoryName result
                            string? directoryName = Path.GetDirectoryName(directory);
                            if (!string.IsNullOrEmpty(directoryName))
                            {
                                string lowercaseImagesDir = Path.Combine(directoryName, "images");

                                if (!Directory.Exists(lowercaseImagesDir))
                                {
                                    Directory.CreateDirectory(lowercaseImagesDir);
                                    Console.WriteLine($"Created lowercase directory: {lowercaseImagesDir}");
                                }

                                // Create fallback image in lowercase folder
                                CreateFallbackSprite(Path.Combine(lowercaseImagesDir, "fallback.png"));

                                Console.WriteLine($"Created sprites in: {directory} and {lowercaseImagesDir}");
                            }
                            else
                            {
                                Console.WriteLine($"Created sprites in: {directory}");
                            }
                        }

                        // Flag success to break out of directory loop
                        success = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create sprites in {directory}: {ex.Message}");
                    }
                }

                if (!success)
                {
                    Console.WriteLine("WARNING: Failed to create sprites in any directory");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring sprites exist: {ex.Message}");
            }
        }

        private static void CreateFallbackSprite(string filePath)
        {
            try
            {
                // Only create the sprite if it doesn't already exist
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"Fallback sprite already exists: {filePath}");
                    return;
                }

                // Create the sprite image
                var drawingVisual = new DrawingVisual();

                using (DrawingContext dc = drawingVisual.RenderOpen())
                {
                    // Draw a pot
                    dc.DrawRectangle(
                        new SolidColorBrush(Colors.SaddleBrown),
                        new Pen(Brushes.Brown, 2),
                        new Rect(SpriteWidth / 2 - 40, SpriteHeight - 60, 80, 40));

                    // Draw a trunk
                    dc.DrawRectangle(
                        new SolidColorBrush(Colors.Brown),
                        null,
                        new Rect(SpriteWidth / 2 - 10, SpriteHeight / 2, 20, SpriteHeight / 2 - 60));

                    // Draw basic foliage (green circle)
                    dc.DrawEllipse(
                        new SolidColorBrush(Colors.ForestGreen),
                        new Pen(new SolidColorBrush(Colors.DarkGreen), 2),
                        new Point(SpriteWidth / 2, SpriteHeight / 3),
                        SpriteWidth / 3,
                        SpriteHeight / 4);

                    // Add "Fallback" text
                    var font = new Typeface("Arial");
                    var formattedText = new FormattedText(
                        "Fallback",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        font,
                        12,
                        Brushes.White,
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                    dc.DrawText(formattedText, new Point(SpriteWidth / 2 - 25, 10));
                }

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Render to bitmap
                var renderTarget = new RenderTargetBitmap(SpriteWidth, SpriteHeight, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(drawingVisual);

                // Save to file
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (FileStream fs = File.Create(filePath))
                {
                    encoder.Save(fs);
                }

                Console.WriteLine($"Created fallback sprite file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating fallback sprite file {filePath}: {ex.Message}");
            }
        }

        private static void CreateGrowthStageSprite(string filePath, GrowthStage stage)
        {
            try
            {
                // Only create the sprite if it doesn't already exist
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"Growth stage sprite already exists: {filePath}");
                    return;
                }

                // Create the sprite image
                var drawingVisual = new DrawingVisual();

                using (DrawingContext dc = drawingVisual.RenderOpen())
                {
                    // Draw a pot
                    dc.DrawRectangle(
                        new SolidColorBrush(Colors.SaddleBrown),
                        new Pen(Brushes.Brown, 2),
                        new Rect(SpriteWidth / 2 - 40, SpriteHeight - 60, 80, 40));

                    // Different size and appearance based on growth stage
                    int trunkHeight = 0;
                    int foliageSize = 0;
                    Color foliageColor = Colors.ForestGreen;

                    switch (stage)
                    {
                        case GrowthStage.Seedling:
                            trunkHeight = 20;
                            foliageSize = 25;
                            foliageColor = Colors.LightGreen;
                            break;
                        case GrowthStage.Sapling:
                            trunkHeight = 40;
                            foliageSize = 35;
                            foliageColor = Colors.LightGreen;
                            break;
                        case GrowthStage.YoungBonsai:
                            trunkHeight = 60;
                            foliageSize = 45;
                            foliageColor = Colors.ForestGreen;
                            break;
                        case GrowthStage.MatureBonsai:
                            trunkHeight = 80;
                            foliageSize = 60;
                            foliageColor = Colors.DarkGreen;
                            break;
                        case GrowthStage.ElderBonsai:
                            trunkHeight = 90;
                            foliageSize = 70;
                            foliageColor = Colors.DarkGreen;
                            break;
                        case GrowthStage.AncientBonsai:
                            trunkHeight = 100;
                            foliageSize = 80;
                            foliageColor = Colors.DarkOliveGreen;
                            break;
                        case GrowthStage.LegendaryBonsai:
                            trunkHeight = 110;
                            foliageSize = 90;
                            foliageColor = Colors.DarkSeaGreen;
                            break;
                    }

                    // Draw a trunk
                    dc.DrawRectangle(
                        new SolidColorBrush(Colors.Brown),
                        null,
                        new Rect(SpriteWidth / 2 - 10, SpriteHeight - 60 - trunkHeight, 20, trunkHeight));

                    // Draw foliage based on growth stage
                    dc.DrawEllipse(
                        new SolidColorBrush(foliageColor),
                        new Pen(new SolidColorBrush(Colors.DarkGreen), 2),
                        new Point(SpriteWidth / 2, SpriteHeight - 60 - trunkHeight - foliageSize / 2),
                        foliageSize,
                        foliageSize * 0.75);

                    // Add stage name
                    var font = new Typeface("Arial");
                    var formattedText = new FormattedText(
                        stage.ToString(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        font,
                        12,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                    dc.DrawText(formattedText, new Point(SpriteWidth / 2 - 30, 10));
                }

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Render to bitmap
                var renderTarget = new RenderTargetBitmap(SpriteWidth, SpriteHeight, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(drawingVisual);

                // Save to file
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (FileStream fs = File.Create(filePath))
                {
                    encoder.Save(fs);
                }

                Console.WriteLine($"Created growth stage sprite file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating growth stage sprite file {filePath}: {ex.Message}");
            }
        }

        private static void CreateGrowthStageStateSprite(string filePath, GrowthStage stage, BonsaiState state)
        {
            try
            {
                // Only create the sprite if it doesn't already exist
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"Growth stage + state sprite already exists: {filePath}");
                    return;
                }

                // Create the sprite image
                var drawingVisual = new DrawingVisual();
                var font = new Typeface("Arial");

                using (DrawingContext dc = drawingVisual.RenderOpen())
                {
                    // Draw a pot
                    dc.DrawRectangle(
                        new SolidColorBrush(Colors.SaddleBrown),
                        new Pen(Brushes.Brown, 2),
                        new Rect(SpriteWidth / 2 - 40, SpriteHeight - 60, 80, 40));

                    // Different size and appearance based on growth stage
                    int trunkHeight = 0;
                    int foliageSize = 0;
                    Color foliageColor = Colors.ForestGreen;

                    switch (stage)
                    {
                        case GrowthStage.Seedling:
                            trunkHeight = 20;
                            foliageSize = 25;
                            foliageColor = Colors.LightGreen;
                            break;
                        case GrowthStage.Sapling:
                            trunkHeight = 40;
                            foliageSize = 35;
                            foliageColor = Colors.LightGreen;
                            break;
                        case GrowthStage.YoungBonsai:
                            trunkHeight = 60;
                            foliageSize = 45;
                            foliageColor = Colors.ForestGreen;
                            break;
                        case GrowthStage.MatureBonsai:
                            trunkHeight = 80;
                            foliageSize = 60;
                            foliageColor = Colors.DarkGreen;
                            break;
                        case GrowthStage.ElderBonsai:
                            trunkHeight = 90;
                            foliageSize = 70;
                            foliageColor = Colors.DarkGreen;
                            break;
                        case GrowthStage.AncientBonsai:
                            trunkHeight = 100;
                            foliageSize = 80;
                            foliageColor = Colors.DarkOliveGreen;
                            break;
                        case GrowthStage.LegendaryBonsai:
                            trunkHeight = 110;
                            foliageSize = 90;
                            foliageColor = Colors.DarkSeaGreen;
                            break;
                    }

                    // Modify appearance based on state
                    switch (state)
                    {
                        case BonsaiState.Growing:
                            foliageColor = Color.FromRgb(
                                (byte)Math.Min(foliageColor.R + 40, 255),
                                (byte)Math.Min(foliageColor.G + 40, 255),
                                foliageColor.B);
                            break;
                        case BonsaiState.Blooming:
                            // Handled after drawing the foliage
                            break;
                        case BonsaiState.Sleeping:
                            foliageColor = Color.FromRgb(
                                (byte)(foliageColor.R * 0.8),
                                (byte)(foliageColor.G * 0.8),
                                (byte)Math.Min(foliageColor.B + 40, 255));
                            break;
                        case BonsaiState.Thirsty:
                            foliageColor = Color.FromRgb(
                                (byte)Math.Min(foliageColor.R + 40, 255),
                                (byte)(foliageColor.G * 0.7),
                                (byte)(foliageColor.B * 0.7));
                            break;
                        case BonsaiState.Unhealthy:
                            foliageColor = Color.FromRgb(
                                (byte)Math.Min(foliageColor.R + 60, 255),
                                (byte)(foliageColor.G * 0.6),
                                (byte)(foliageColor.B * 0.6));
                            break;
                        case BonsaiState.Wilting:
                            foliageColor = Color.FromRgb(
                                (byte)Math.Min(foliageColor.R + 40, 255),
                                (byte)(foliageColor.G * 0.5),
                                (byte)(foliageColor.B * 0.3));
                            break;
                    }

                    // Draw a trunk
                    dc.DrawRectangle(
                        new SolidColorBrush(Colors.Brown),
                        null,
                        new Rect(SpriteWidth / 2 - 10, SpriteHeight - 60 - trunkHeight, 20, trunkHeight));

                    // Draw foliage based on growth stage
                    dc.DrawEllipse(
                        new SolidColorBrush(foliageColor),
                        new Pen(new SolidColorBrush(Colors.DarkGreen), 2),
                        new Point(SpriteWidth / 2, SpriteHeight - 60 - trunkHeight - foliageSize / 2),
                        foliageSize,
                        foliageSize * 0.75);

                    // Add state-specific details
                    switch (state)
                    {
                        case BonsaiState.Blooming:
                            // Add flowers
                            dc.DrawEllipse(
                                new SolidColorBrush(Colors.DeepPink),
                                null,
                                new Point(SpriteWidth / 2 - foliageSize / 2, SpriteHeight - 60 - trunkHeight - foliageSize),
                                foliageSize / 10, foliageSize / 10);
                            dc.DrawEllipse(
                                new SolidColorBrush(Colors.DeepPink),
                                null,
                                new Point(SpriteWidth / 2 + foliageSize / 2, SpriteHeight - 60 - trunkHeight - foliageSize / 2),
                                foliageSize / 10, foliageSize / 10);
                            dc.DrawEllipse(
                                new SolidColorBrush(Colors.DeepPink),
                                null,
                                new Point(SpriteWidth / 2, SpriteHeight - 60 - trunkHeight - foliageSize),
                                foliageSize / 10, foliageSize / 10);
                            break;

                        case BonsaiState.Sleeping:
                            // "Z" symbols
                            var formattedText = new FormattedText(
                                "Z",
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                font,
                                16,
                                Brushes.DarkBlue,
                                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                            dc.DrawText(formattedText, new Point(SpriteWidth / 2 + foliageSize / 2,
                                SpriteHeight - 60 - trunkHeight - foliageSize));

                            var formattedText2 = new FormattedText(
                                "Z",
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                font,
                                20,
                                Brushes.DarkBlue,
                                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                            dc.DrawText(formattedText2, new Point(SpriteWidth / 2 + foliageSize / 2 + 10,
                                SpriteHeight - 60 - trunkHeight - foliageSize - 10));
                            break;

                        case BonsaiState.Thirsty:
                            // Water droplet
                            var dropGeometry = new PathGeometry();
                            var dropFigure = new PathFigure();
                            dropFigure.StartPoint = new Point(SpriteWidth / 2 + foliageSize,
                                SpriteHeight - 60 - trunkHeight - foliageSize / 2);
                            dropFigure.Segments.Add(
                                new BezierSegment(
                                    new Point(SpriteWidth / 2 + foliageSize - 5, SpriteHeight - 60 - trunkHeight - foliageSize / 2 + 5),
                                    new Point(SpriteWidth / 2 + foliageSize - 5, SpriteHeight - 60 - trunkHeight - foliageSize / 2 + 15),
                                    new Point(SpriteWidth / 2 + foliageSize, SpriteHeight - 60 - trunkHeight - foliageSize / 2 + 20),
                                    true));
                            dropFigure.Segments.Add(
                                new BezierSegment(
                                    new Point(SpriteWidth / 2 + foliageSize + 5, SpriteHeight - 60 - trunkHeight - foliageSize / 2 + 15),
                                    new Point(SpriteWidth / 2 + foliageSize + 5, SpriteHeight - 60 - trunkHeight - foliageSize / 2 + 5),
                                    new Point(SpriteWidth / 2 + foliageSize, SpriteHeight - 60 - trunkHeight - foliageSize / 2),
                                    true));
                            dropGeometry.Figures.Add(dropFigure);
                            dc.DrawGeometry(new SolidColorBrush(Colors.LightBlue), new Pen(Brushes.Blue, 1), dropGeometry);
                            break;
                    }

                    // Add stage and state names
                    var stageText = new FormattedText(
                        stage.ToString(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        font,
                        12,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                    dc.DrawText(stageText, new Point(10, 10));

                    var stateText = new FormattedText(
                        state.ToString(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        font,
                        12,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                    dc.DrawText(stateText, new Point(10, 30));
                }

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Render to bitmap
                var renderTarget = new RenderTargetBitmap(SpriteWidth, SpriteHeight, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(drawingVisual);

                // Save to file
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (FileStream fs = File.Create(filePath))
                {
                    encoder.Save(fs);
                }

                Console.WriteLine($"Created combined stage+state sprite file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating combined sprite file {filePath}: {ex.Message}");
            }
        }

        private static void CreateSprite(string filePath, BonsaiState state)
        {
            try
            {
                // Only create the sprite if it doesn't already exist
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"Sprite already exists: {filePath}");
                    return;
                }

                // Select colors based on state
                Color fillColor = Colors.LightGreen;
                Color outlineColor = Colors.DarkGreen;

                switch (state)
                {
                    case BonsaiState.Growing:
                        fillColor = Colors.LightYellow;
                        outlineColor = Colors.Green;
                        break;
                    case BonsaiState.Blooming:
                        fillColor = Colors.Pink;
                        outlineColor = Colors.Green;
                        break;
                    case BonsaiState.Sleeping:
                        fillColor = Colors.LightBlue;
                        outlineColor = Colors.Green;
                        break;
                    case BonsaiState.Thirsty:
                        fillColor = Colors.LightBlue;
                        outlineColor = Colors.Yellow;
                        break;
                    case BonsaiState.Unhealthy:
                        fillColor = Colors.LightGray;
                        outlineColor = Colors.Red;
                        break;
                    case BonsaiState.Wilting:
                        fillColor = Colors.Brown;
                        outlineColor = Colors.DarkRed;
                        break;
                }

                // Create the sprite image
                var drawingVisual = new DrawingVisual();

                // Fixed CS0165: Declared font before using it in switch statement
                var font = new Typeface("Arial");

                using (DrawingContext dc = drawingVisual.RenderOpen())
                {
                    // Draw a pot
                    dc.DrawRectangle(
                        new SolidColorBrush(Colors.SandyBrown),
                        new Pen(Brushes.Brown, 2),
                        new Rect(SpriteWidth / 2 - 40, SpriteHeight - 60, 80, 40));

                    // Draw a trunk
                    dc.DrawRectangle(
                        new SolidColorBrush(Colors.Brown),
                        null,
                        new Rect(SpriteWidth / 2 - 10, SpriteHeight / 2, 20, SpriteHeight / 2 - 60));

                    // Draw foliage based on state
                    dc.DrawEllipse(
                        new SolidColorBrush(fillColor),
                        new Pen(new SolidColorBrush(outlineColor), 2),
                        new Point(SpriteWidth / 2, SpriteHeight / 3),
                        SpriteWidth / 3,
                        SpriteHeight / 4);

                    // Add state-specific details
                    switch (state)
                    {
                        case BonsaiState.Blooming:
                            // Add flowers
                            dc.DrawEllipse(
                                new SolidColorBrush(Colors.DeepPink),
                                null,
                                new Point(SpriteWidth / 2 - 30, SpriteHeight / 3 - 20),
                                10, 10);
                            dc.DrawEllipse(
                                new SolidColorBrush(Colors.DeepPink),
                                null,
                                new Point(SpriteWidth / 2 + 40, SpriteHeight / 3),
                                10, 10);
                            dc.DrawEllipse(
                                new SolidColorBrush(Colors.DeepPink),
                                null,
                                new Point(SpriteWidth / 2, SpriteHeight / 3 - 30),
                                10, 10);
                            break;

                        case BonsaiState.Wilting:
                            // Drooping branches
                            var pathGeometry = new PathGeometry();
                            var pathFigure = new PathFigure();
                            pathFigure.StartPoint = new Point(SpriteWidth / 2, SpriteHeight / 2);
                            pathFigure.Segments.Add(
                                new BezierSegment(
                                    new Point(SpriteWidth / 2 + 20, SpriteHeight / 2 + 20),
                                    new Point(SpriteWidth / 2 + 40, SpriteHeight / 2 + 60),
                                    new Point(SpriteWidth / 2 + 60, SpriteHeight / 2 + 80),
                                    true));
                            pathGeometry.Figures.Add(pathFigure);
                            dc.DrawGeometry(null, new Pen(Brushes.Brown, 2), pathGeometry);
                            break;

                        case BonsaiState.Thirsty:
                            // Water droplet
                            var dropGeometry = new PathGeometry();
                            var dropFigure = new PathFigure();
                            dropFigure.StartPoint = new Point(SpriteWidth / 2 + 70, SpriteHeight / 2 - 20);
                            dropFigure.Segments.Add(
                                new BezierSegment(
                                    new Point(SpriteWidth / 2 + 60, SpriteHeight / 2 - 10),
                                    new Point(SpriteWidth / 2 + 60, SpriteHeight / 2 + 10),
                                    new Point(SpriteWidth / 2 + 70, SpriteHeight / 2 + 20),
                                    true));
                            dropFigure.Segments.Add(
                                new BezierSegment(
                                    new Point(SpriteWidth / 2 + 80, SpriteHeight / 2 + 10),
                                    new Point(SpriteWidth / 2 + 80, SpriteHeight / 2 - 10),
                                    new Point(SpriteWidth / 2 + 70, SpriteHeight / 2 - 20),
                                    true));
                            dropGeometry.Figures.Add(dropFigure);
                            dc.DrawGeometry(new SolidColorBrush(Colors.LightBlue), new Pen(Brushes.Blue, 1), dropGeometry);
                            break;

                        case BonsaiState.Sleeping:
                            // "Z" symbols - Now using the font declared outside the switch
                            var formattedText = new FormattedText(
                                "Z",
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                font,
                                16,
                                Brushes.DarkBlue,
                                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                            dc.DrawText(formattedText, new Point(SpriteWidth / 2 + 40, SpriteHeight / 3 - 30));

                            var formattedText2 = new FormattedText(
                                "Z",
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                font,
                                20,
                                Brushes.DarkBlue,
                                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                            dc.DrawText(formattedText2, new Point(SpriteWidth / 2 + 50, SpriteHeight / 3 - 40));
                            break;

                        case BonsaiState.Unhealthy:
                            // Warning sign
                            var warningGeometry = new PathGeometry();
                            var warningFigure = new PathFigure();
                            warningFigure.StartPoint = new Point(SpriteWidth / 2 + 60, SpriteHeight / 3 - 40);
                            warningFigure.Segments.Add(new LineSegment(new Point(SpriteWidth / 2 + 80, SpriteHeight / 3), true));
                            warningFigure.Segments.Add(new LineSegment(new Point(SpriteWidth / 2 + 40, SpriteHeight / 3), true));
                            warningFigure.IsClosed = true;
                            warningGeometry.Figures.Add(warningFigure);
                            dc.DrawGeometry(new SolidColorBrush(Colors.Yellow), new Pen(Brushes.Red, 2), warningGeometry);

                            var exclamation = new FormattedText(
                                "!",
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                font,  // Now correctly using the font declared outside
                                16,
                                Brushes.Red,
                                VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);
                            dc.DrawText(exclamation, new Point(SpriteWidth / 2 + 58, SpriteHeight / 3 - 30));
                            break;
                    }
                }

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(filePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Render to bitmap
                var renderTarget = new RenderTargetBitmap(SpriteWidth, SpriteHeight, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(drawingVisual);

                // Save to file
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (FileStream fs = File.Create(filePath))
                {
                    encoder.Save(fs);
                }

                Console.WriteLine($"Created sprite file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating sprite file {filePath}: {ex.Message}");
            }
        }
    }
}