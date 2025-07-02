using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BonsaiGotchiGame.Services
{
    public class AnimationService
    {
        // Button click animation
        public static void AnimateButtonClick(Button? button)
        {
            if (button == null) return;

            ScaleTransform transform = new ScaleTransform(1, 1);
            button.RenderTransform = transform;
            button.RenderTransformOrigin = new Point(0.5, 0.5);

            DoubleAnimation scaleDownAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.95,
                Duration = TimeSpan.FromMilliseconds(100)
            };

            DoubleAnimation scaleUpAnimation = new DoubleAnimation
            {
                From = 0.95,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(100)
            };

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(scaleDownAnimation);
            Storyboard.SetTarget(scaleDownAnimation, button);
            Storyboard.SetTargetProperty(scaleDownAnimation, new PropertyPath("RenderTransform.ScaleX"));

            Storyboard storyboard2 = new Storyboard();
            storyboard2.Children.Add(scaleUpAnimation);
            Storyboard.SetTarget(scaleUpAnimation, button);
            Storyboard.SetTargetProperty(scaleUpAnimation, new PropertyPath("RenderTransform.ScaleX"));

            DoubleAnimation scaleDownAnimationY = new DoubleAnimation
            {
                From = 1.0,
                To = 0.95,
                Duration = TimeSpan.FromMilliseconds(100)
            };

            DoubleAnimation scaleUpAnimationY = new DoubleAnimation
            {
                From = 0.95,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(100)
            };

            storyboard.Children.Add(scaleDownAnimationY);
            Storyboard.SetTarget(scaleDownAnimationY, button);
            Storyboard.SetTargetProperty(scaleDownAnimationY, new PropertyPath("RenderTransform.ScaleY"));

            storyboard2.Children.Add(scaleUpAnimationY);
            Storyboard.SetTarget(scaleUpAnimationY, button);
            Storyboard.SetTargetProperty(scaleUpAnimationY, new PropertyPath("RenderTransform.ScaleY"));

            storyboard.Completed += (s, e) => storyboard2.Begin();
            storyboard.Begin();
        }

        // Status message fade animation
        public static void AnimateStatusMessage(TextBlock? statusTextBlock)
        {
            if (statusTextBlock == null) return;

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.5,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(fadeOut);
            Storyboard.SetTarget(fadeOut, statusTextBlock);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));

            Storyboard storyboard2 = new Storyboard();
            storyboard2.Children.Add(fadeIn);
            Storyboard.SetTarget(fadeIn, statusTextBlock);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));

            storyboard.Completed += (s, e) => storyboard2.Begin();
            storyboard.Begin();
        }

        // Progress bar animation
        public static void AnimateProgressChange(ProgressBar? progressBar, double oldValue, double newValue)
        {
            if (progressBar == null) return;

            DoubleAnimation animation = new DoubleAnimation
            {
                From = oldValue,
                To = newValue,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);
        }

        // Pulse animation for critical state
        public static void PulseElement(UIElement? element, TimeSpan duration)
        {
            if (element == null) return;

            ScaleTransform transform = new ScaleTransform(1, 1);
            element.RenderTransform = transform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            DoubleAnimation scaleAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.05,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(duration)
            };

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }

        // Fade in animation for elements
        public static void FadeInElement(UIElement? element, TimeSpan duration)
        {
            if (element == null) return;

            element.Opacity = 0;
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
    }
}