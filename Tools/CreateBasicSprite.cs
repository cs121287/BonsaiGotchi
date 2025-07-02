using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BonsaiGotchiGame.Tools
{
    public static class CreateBasicSprite
    {
        public static void CreateBasicSpriteFiles()
        {
            try
            {
                // Create the Assets/Images directory if it doesn't exist
                string imagesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
                if (!Directory.Exists(imagesDirectory))
                {
                    Directory.CreateDirectory(imagesDirectory);
                }
                
                // Create a basic sprite for each state
                CreateSprite(Path.Combine(imagesDirectory, "idle.png"), Colors.LightGreen, Colors.DarkGreen);
                CreateSprite(Path.Combine(imagesDirectory, "growing.png"), Colors.LightYellow, Colors.Green);
                CreateSprite(Path.Combine(imagesDirectory, "blooming.png"), Colors.Pink, Colors.Green);
                CreateSprite(Path.Combine(imagesDirectory, "sleeping.png"), Colors.LightBlue, Colors.Green);
                CreateSprite(Path.Combine(imagesDirectory, "thirsty.png"), Colors.LightBlue, Colors.Yellow);
                CreateSprite(Path.Combine(imagesDirectory, "unhealthy.png"), Colors.LightGray, Colors.Red);
                CreateSprite(Path.Combine(imagesDirectory, "wilting.png"), Colors.Brown, Colors.DarkRed);
                
                Console.WriteLine("Basic sprite files created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating basic sprite files: {ex.Message}");
            }
        }
        
        private static void CreateSprite(string filePath, Color fillColor, Color outlineColor)
        {
            int width = 256;
            int height = 256;
            
            var drawingVisual = new DrawingVisual();
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(fillColor), new Pen(new SolidColorBrush(outlineColor), 10), 
                    new Rect(20, 20, width - 40, height - 40));
                
                // Draw simple plant shape
                var path = new System.Windows.Media.PathGeometry();
                var figure = new System.Windows.Media.PathFigure();
                figure.StartPoint = new Point(width / 2, height - 40);
                
                // Stem
                figure.Segments.Add(new System.Windows.Media.LineSegment(new Point(width / 2, height / 2), true));
                
                // Leaves
                figure.Segments.Add(new System.Windows.Media.BezierSegment(
                    new Point(width / 2 + 30, height / 2 - 10),
                    new Point(width / 2 + 60, height / 2 - 30),
                    new Point(width / 2 + 90, height / 2 - 20),
                    true));
                
                figure.Segments.Add(new System.Windows.Media.LineSegment(new Point(width / 2, height / 2), true));
                
                figure.Segments.Add(new System.Windows.Media.BezierSegment(
                    new Point(width / 2 - 30, height / 2 - 30),
                    new Point(width / 2 - 60, height / 2 - 50),
                    new Point(width / 2 - 90, height / 2 - 40),
                    true));
                
                figure.Segments.Add(new System.Windows.Media.LineSegment(new Point(width / 2, height / 2), true));
                
                // Top branch
                figure.Segments.Add(new System.Windows.Media.LineSegment(new Point(width / 2, height / 4), true));
                
                path.Figures.Add(figure);
                
                dc.DrawGeometry(new SolidColorBrush(outlineColor), new Pen(new SolidColorBrush(Colors.Black), 2), path);
            }
            
            var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(drawingVisual);
            
            // Save the image to file
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));
            
            using (FileStream fs = File.Create(filePath))
            {
                encoder.Save(fs);
            }
            
            Console.WriteLine($"Created sprite file: {filePath}");
        }
    }
}