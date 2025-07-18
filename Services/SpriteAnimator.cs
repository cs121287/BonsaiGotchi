using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Services
{
    public class SpriteAnimator : IDisposable
    {
        private readonly Image _targetImage;
        private BitmapSource? _currentSpriteSheet;
        private readonly DispatcherTimer _animationTimer = new();
        private int _currentFrame;
        private int _frameCount;
        private int _framesPerRow;
        private int _frameWidth;
        private int _frameHeight;
        private BonsaiState _currentState;
        private bool _useSingleImage = false;
        private bool _disposed = false;
        private readonly object _animationLock = new object(); // Add lock for thread safety

        // Dictionary mapping states to sprite sheet files
        private readonly Dictionary<BonsaiState, string> _stateToSpriteSheetMap = new()
        {
            { BonsaiState.Idle, "idle_sheet.png" },
            { BonsaiState.Growing, "playing_sheet.png" }, // Use playing as growing
            { BonsaiState.Blooming, "playing_sheet.png" }, // Use playing as blooming
            { BonsaiState.Sleeping, "sleeping_sheet.png" },
            { BonsaiState.Thirsty, "hungry_sheet.png" },  // Use hungry as thirsty
            { BonsaiState.Unhealthy, "sick_sheet.png" },
            { BonsaiState.Wilting, "tired_sheet.png" }
        };

        // Dictionary mapping states to single image files (fallback)
        private readonly Dictionary<BonsaiState, string> _stateToImageMap = new()
        {
            { BonsaiState.Idle, "idle.png" },
            { BonsaiState.Growing, "playing.png" },
            { BonsaiState.Blooming, "playing.png" },
            { BonsaiState.Sleeping, "sleeping.png" },
            { BonsaiState.Thirsty, "hungry.png" },
            { BonsaiState.Unhealthy, "sick.png" },
            { BonsaiState.Wilting, "tired.png" }
        };

        public SpriteAnimator(Image targetImage)
        {
            _targetImage = targetImage ?? throw new ArgumentNullException(nameof(targetImage));

            // Set specific size for the bonsai sprite (64x64) for new layout
            _targetImage.Width = 512;
            _targetImage.Height = 512;

            // Set rendering options for better quality when scaling small sprites
            RenderOptions.SetBitmapScalingMode(_targetImage, BitmapScalingMode.HighQuality);

            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Interval = TimeSpan.FromMilliseconds(150); // Default animation speed

            // Set default animation
            UpdateAnimation(BonsaiState.Idle);
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_targetImage == null || _currentSpriteSheet == null || _disposed) return;

            lock (_animationLock)
            {
                _currentFrame = (_currentFrame + 1) % _frameCount;
                UpdateDisplayedFrame();
            }
        }

        public void UpdateAnimation(BonsaiState state)
        {
            if (_disposed) return;

            lock (_animationLock)
            {
                if (_currentState == state && _animationTimer.IsEnabled && !_useSingleImage)
                    return; // Already playing this animation

                _currentState = state;

                // Get the appropriate sprite sheet filename
                string spriteSheetFilename = _stateToSpriteSheetMap.TryGetValue(state, out string? sheetPath)
                    ? sheetPath
                    : "idle_sheet.png"; // Default to idle if not found

                // First try loading from Content path (copied to output directory)
                string contentPath = Path.Combine("Assets", "Images", spriteSheetFilename);
                bool success = TryLoadSpriteSheet(contentPath);

                if (!success)
                {
                    // Try relative pack URI
                    string relativePath = $"/Assets/Images/{spriteSheetFilename}";
                    success = TryLoadSpriteSheetFromPack(relativePath);
                }

                if (!success)
                {
                    // Attempt to load a single image instead of a sheet as last resort
                    string imageFile = _stateToImageMap.TryGetValue(state, out string? imgPath)
                        ? imgPath
                        : "idle.png";

                    string singleImagePath = Path.Combine("Assets", "Images", imageFile);
                    success = TryLoadSingleImage(singleImagePath);

                    if (!success)
                    {
                        // Try pack URI for single image
                        string relativeImagePath = $"/Assets/Images/{imageFile}";
                        success = TryLoadSingleImageFromPack(relativeImagePath);

                        if (!success)
                        {
                            // As a last resort, try the fallback image
                            TryLoadSingleImage(Path.Combine("Assets", "Images", "fallback.png"));
                        }
                    }
                }

                // Reset animation state
                _currentFrame = 0;
                if (!_useSingleImage)
                {
                    UpdateDisplayedFrame();
                    _animationTimer.Start();
                }
                else
                {
                    _animationTimer.Stop();
                }
            }
        }

        private bool TryLoadSpriteSheet(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Relative);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze for thread safety and better performance

                _currentSpriteSheet = bitmap;
                SetupSpriteSheetParameters();
                _useSingleImage = false;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sprite sheet from path {filePath}: {ex.Message}");
                return false;
            }
        }

        private bool TryLoadSpriteSheetFromPack(string packUri)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(packUri, UriKind.Relative);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze for thread safety and better performance

                _currentSpriteSheet = bitmap;
                SetupSpriteSheetParameters();
                _useSingleImage = false;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sprite sheet from pack URI {packUri}: {ex.Message}");
                return false;
            }
        }

        private bool TryLoadSingleImage(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Relative);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze for thread safety

                // Use the dispatcher to update the UI element
                Application.Current?.Dispatcher.Invoke(() => {
                    if (!_disposed && _targetImage != null)
                    {
                        _targetImage.Source = bitmap;
                    }
                });

                _useSingleImage = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading single image from path {filePath}: {ex.Message}");
                return false;
            }
        }

        private bool TryLoadSingleImageFromPack(string packUri)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(packUri, UriKind.Relative);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze for thread safety

                // Use the dispatcher to update the UI element
                Application.Current?.Dispatcher.Invoke(() => {
                    if (!_disposed && _targetImage != null)
                    {
                        _targetImage.Source = bitmap;
                    }
                });

                _useSingleImage = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading single image from pack URI {packUri}: {ex.Message}");

                // Last resort - create a placeholder image
                CreatePlaceholderImage();
                return false;
            }
        }

        private void CreatePlaceholderImage()
        {
            try
            {
                // Create a simple placeholder image
                var drawingVisual = new DrawingVisual();
                using (DrawingContext dc = drawingVisual.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.LightGreen, new Pen(Brushes.DarkGreen, 2), new Rect(0, 0, 64, 64));
                    dc.DrawLine(new Pen(Brushes.Green, 2), new Point(0, 0), new Point(64, 64));
                    dc.DrawLine(new Pen(Brushes.Green, 2), new Point(64, 0), new Point(0, 64));
                    dc.DrawText(
                        new FormattedText("Bonsai",
                                        System.Globalization.CultureInfo.CurrentCulture,
                                        FlowDirection.LeftToRight,
                                        new Typeface("Arial"),
                                        12,
                                        Brushes.Black,
                                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip),
                        new Point(10, 25));
                }

                var renderTarget = new RenderTargetBitmap(128, 128, 96, 96, PixelFormats.Pbgra32);
                renderTarget.Render(drawingVisual);
                renderTarget.Freeze(); // Freeze for thread safety

                // Use the dispatcher to update the UI element
                Application.Current?.Dispatcher.Invoke(() => {
                    if (!_disposed && _targetImage != null)
                    {
                        _targetImage.Source = renderTarget;
                    }
                });

                _useSingleImage = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating placeholder image: {ex.Message}");
            }
        }

        private void SetupSpriteSheetParameters()
        {
            if (_currentSpriteSheet == null) return;

            // Determine sprite sheet properties (assuming uniform frames)
            _frameWidth = 128; // Default size - adjust based on your sprite sheets
            _frameHeight = 128;
            _framesPerRow = Math.Max(1, (int)(_currentSpriteSheet.PixelWidth / _frameWidth));
            int frameRows = Math.Max(1, (int)(_currentSpriteSheet.PixelHeight / _frameHeight));
            _frameCount = Math.Max(1, _framesPerRow * frameRows);
        }

        private void UpdateDisplayedFrame()
        {
            try
            {
                if (_targetImage == null || _currentSpriteSheet == null || _useSingleImage || _disposed)
                    return;

                // Calculate the position of the current frame in the sprite sheet
                int row = _currentFrame / _framesPerRow;
                int col = _currentFrame % _framesPerRow;

                // Safety checks for out-of-bounds conditions
                if (row * _frameHeight >= _currentSpriteSheet.PixelHeight ||
                    col * _frameWidth >= _currentSpriteSheet.PixelWidth)
                {
                    Console.WriteLine("Frame coordinates out of bounds for sprite sheet");
                    return;
                }

                // Define the rectangle of the current frame
                Int32Rect rect = new(
                    col * _frameWidth,
                    row * _frameHeight,
                    Math.Min(_frameWidth, _currentSpriteSheet.PixelWidth - (col * _frameWidth)),
                    Math.Min(_frameHeight, _currentSpriteSheet.PixelHeight - (row * _frameHeight)));

                // Create a cropped bitmap of just this frame
                CroppedBitmap croppedBitmap = new(_currentSpriteSheet, rect);
                croppedBitmap.Freeze(); // Freeze for thread safety

                // Set the image source using the dispatcher
                Application.Current?.Dispatcher.Invoke(() => {
                    if (!_disposed && _targetImage != null)
                    {
                        _targetImage.Source = croppedBitmap;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating frame: {ex.Message}");

                // If there's an error, switch to single image mode and create a placeholder
                _useSingleImage = true;
                CreatePlaceholderImage();
            }
        }

        public void Stop()
        {
            if (_disposed) return;

            try
            {
                _animationTimer.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping animation timer: {ex.Message}");
            }
        }

        public void Start()
        {
            if (_disposed) return;

            try
            {
                if (!_useSingleImage)
                    _animationTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting animation timer: {ex.Message}");
            }
        }

        public void SetAnimationSpeed(int milliseconds)
        {
            if (_disposed) return;

            try
            {
                _animationTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(10, milliseconds));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting animation speed: {ex.Message}");
            }
        }

        #region IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // Dispose managed resources
                        _animationTimer.Stop();
                        _animationTimer.Tick -= AnimationTimer_Tick;

                        // Clear references to bitmaps
                        _currentSpriteSheet = null;

                        // Clear the image source on the UI thread
                        Application.Current?.Dispatcher.Invoke(() => {
                            if (_targetImage != null)
                            {
                                _targetImage.Source = null;
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in SpriteAnimator disposal: {ex.Message}");
                    }
                }

                _disposed = true;
            }
        }

        ~SpriteAnimator()
        {
            Dispose(false);
        }
        #endregion
    }
}