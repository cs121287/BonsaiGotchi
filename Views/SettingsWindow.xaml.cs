using System;
using System.Windows;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            // Load current settings
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load settings from GameSettings.Instance
            try
            {
                var settings = GameSettings.Instance;
                if (settings != null)
                {
                    // Set UI controls based on settings values
                    AutoSaveCheckBox.IsChecked = settings.AutoSave;
                    
                    // Set the combo box selection for auto-save interval
                    int intervalIndex = GetIntervalIndex(settings.AutoSaveIntervalMinutes);
                    AutoSaveIntervalComboBox.SelectedIndex = intervalIndex;
                    
                    // Set time progression speed
                    int speedIndex = GetSpeedIndex(settings.TimeProgressionSpeed);
                    TimeProgressionSpeedComboBox.SelectedIndex = speedIndex;
                    
                    // Set sound and music settings
                    PlaySoundsCheckBox.IsChecked = settings.PlaySounds;
                    PlayMusicCheckBox.IsChecked = settings.PlayMusic;
                    SoundVolumeSlider.Value = settings.SoundVolume;
                    MusicVolumeSlider.Value = settings.MusicVolume;
                    
                    // Set UI settings
                    ShowTipsCheckBox.IsChecked = settings.ShowTips;
                    ThemeComboBox.SelectedIndex = settings.ThemeIndex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetIntervalIndex(int minutes)
        {
            return minutes switch
            {
                1 => 0,
                5 => 1,
                10 => 2,
                15 => 3,
                30 => 4,
                60 => 5,
                _ => 1  // Default to 5 minutes (index 1)
            };
        }

        private int GetSpeedIndex(int speed)
        {
            return speed switch
            {
                1 => 0,  // Normal
                2 => 1,  // 2x Speed
                5 => 2,  // 5x Speed
                10 => 3, // 10x Speed
                _ => 0   // Default to Normal
            };
        }

        private void SaveSettings()
        {
            try
            {
                var settings = GameSettings.Instance;
                if (settings != null)
                {
                    // Update settings from UI controls
                    settings.AutoSave = AutoSaveCheckBox.IsChecked ?? true;
                    
                    // Get interval value from combo box
                    settings.AutoSaveIntervalMinutes = GetIntervalValue(AutoSaveIntervalComboBox.SelectedIndex);
                    
                    // Get speed value from combo box
                    settings.TimeProgressionSpeed = GetSpeedValue(TimeProgressionSpeedComboBox.SelectedIndex);
                    
                    // Update sound and music settings
                    settings.PlaySounds = PlaySoundsCheckBox.IsChecked ?? true;
                    settings.PlayMusic = PlayMusicCheckBox.IsChecked ?? true;
                    settings.SoundVolume = (float)SoundVolumeSlider.Value;
                    settings.MusicVolume = (float)MusicVolumeSlider.Value;
                    
                    // Update UI settings
                    settings.ShowTips = ShowTipsCheckBox.IsChecked ?? true;
                    settings.ThemeIndex = ThemeComboBox.SelectedIndex;

                    // Save settings
                    settings.Save();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetIntervalValue(int index)
        {
            return index switch
            {
                0 => 1,   // 1 minute
                1 => 5,   // 5 minutes
                2 => 10,  // 10 minutes
                3 => 15,  // 15 minutes
                4 => 30,  // 30 minutes
                5 => 60,  // 60 minutes
                _ => 5    // Default to 5 minutes
            };
        }

        private int GetSpeedValue(int index)
        {
            return index switch
            {
                0 => 1,   // Normal
                1 => 2,   // 2x Speed
                2 => 5,   // 5x Speed
                3 => 10,  // 10x Speed
                _ => 1    // Default to Normal
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Reset to default values
                AutoSaveCheckBox.IsChecked = true;
                AutoSaveIntervalComboBox.SelectedIndex = 1;  // 5 minutes
                TimeProgressionSpeedComboBox.SelectedIndex = 0;  // Normal
                PlaySoundsCheckBox.IsChecked = true;
                PlayMusicCheckBox.IsChecked = true;
                SoundVolumeSlider.Value = 0.8;
                MusicVolumeSlider.Value = 0.5;
                ShowTipsCheckBox.IsChecked = true;
                ThemeComboBox.SelectedIndex = 0;  // Default theme
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting to defaults: {ex.Message}",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}