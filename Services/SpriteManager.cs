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

        public static void EnsureSpritesExist()
        {
            try
            {
                Console.WriteLine("Ensuring sprite files exist...");

                // Define possible sprite directories - Fixed CS8604 by adding null check with ?? operator
                string[] directories = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Images"),
                    Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty, "Assets", "Images")
                };

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

                        // Create all the required sprites
                        CreateSprite(Path.Combine(directory, "idle.png"), BonsaiState.Idle);
                        CreateSprite(Path.Combine(directory, "growing.png"), BonsaiState.Growing);
                        CreateSprite(Path.Combine(directory, "blooming.png"), BonsaiState.Blooming);
                        CreateSprite(Path.Combine(directory, "sleeping.png"), BonsaiState.Sleeping);
                        CreateSprite(Path.Combine(directory, "thirsty.png"), BonsaiState.Thirsty);
                        CreateSprite(Path.Combine(directory, "unhealthy.png"), BonsaiState.Unhealthy);
                        CreateSprite(Path.Combine(directory, "wilting.png"), BonsaiState.Wilting);

                        Console.WriteLine($"Created sprites in: {directory}");

                        // Stop after successfully creating sprites in one directory
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to create sprites in {directory}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring sprites exist: {ex.Message}");
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