using System;
using System.Windows;
using System.Windows.Controls;
using OverlayPlayer.Helpers;
using OverlayPlayer.Models;
using MessageBox = System.Windows.MessageBox;


namespace OverlayPlayer
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        private bool _isInitializing = true;

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadCurrentSettings();
            _isInitializing = false;
        }

        private void LoadCurrentSettings()
        {
            TitleText.Text = LocalizationService.Get("Settings");
            ResetButton.Content = LocalizationService.Get("ResetDefaults");

            // Lang
            foreach (ComboBoxItem item in LangCombo.Items)
            {
                if (item.Tag.ToString() == _settings.Language)
                {
                    LangCombo.SelectedItem = item;
                    break;
                }
            }
            
            UpdateUIFromSettings();
        }

        private void UpdateUIFromSettings()
        {
            _isInitializing = true;
            OpacitySlider.Value = _settings.Opacity;
            SizeSlider.Value = _settings.WindowSize;
            AspectRatioCheck.IsChecked = _settings.LockAspectRatio;
            InteractiveCheck.IsChecked = _settings.IsInteractive;
            AutoStartCheck.IsChecked = WindowHelper.IsAutoStartEnabled();
            MuteCheck.IsChecked = _settings.IsMuted;
            VolumeSlider.Value = _settings.Volume;

            if (_settings.ShowOnTop) TopmostRadio.IsChecked = true;
            else if (_settings.IsWallpaperMode) WallpaperRadio.IsChecked = true;
            else NormalRadio.IsChecked = true;

            SlideshowCheck.IsChecked = _settings.IsSlideshowEnabled;
            foreach (ComboBoxItem item in IntervalCombo.Items)
            {
                if (item.Tag.ToString() == _settings.SlideshowIntervalSeconds.ToString())
                {
                    IntervalCombo.SelectedItem = item;
                    break;
                }
            }
            _isInitializing = false;
        }

        private void SaveAndApply()
        {
            if (_isInitializing) return;

            // Apply all settings
            _settings.Save();
            
            // Apply to MainWindow immediately
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.ApplySettingsFromDialog();
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset all settings to defaults?", "Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _settings.Opacity = 1.0;
                _settings.WindowSize = 300;
                _settings.Volume = 0.5;
                _settings.IsMuted = false;
                _settings.LockAspectRatio = true;
                _settings.IsInteractive = false;
                _settings.ShowOnTop = true;
                _settings.IsWallpaperMode = false;
                _settings.IsSlideshowEnabled = false;
                _settings.SlideshowIntervalSeconds = 10;
                
                UpdateUIFromSettings();
                _isInitializing = false; // Force enable save
                SaveAndApply();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LangCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            var tag = (LangCombo.SelectedItem as ComboBoxItem)?.Tag.ToString();
            if (tag != null)
            {
                _settings.Language = tag;
                LocalizationService.SetLanguage(tag);
                LoadCurrentSettings(); // Refresh UI strings
                SaveAndApply();
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            _settings.Opacity = e.NewValue;
            SaveAndApply();
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            _settings.WindowSize = e.NewValue;
            SaveAndApply();
        }

        private void AspectRatioCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settings.LockAspectRatio = AspectRatioCheck.IsChecked ?? true;
            SaveAndApply();
        }

        private void InteractiveCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settings.IsInteractive = InteractiveCheck.IsChecked ?? false;
            SaveAndApply();
        }

        private void AutoStartCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            WindowHelper.SetAutoStart(AutoStartCheck.IsChecked ?? false);
        }

        private void MuteCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settings.IsMuted = MuteCheck.IsChecked ?? false;
            SaveAndApply();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            _settings.Volume = e.NewValue;
            SaveAndApply();
        }

        private void ZOrder_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settings.ShowOnTop = TopmostRadio.IsChecked ?? false;
            _settings.IsWallpaperMode = WallpaperRadio.IsChecked ?? false;
            SaveAndApply();
        }

        private void SlideshowCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _settings.IsSlideshowEnabled = SlideshowCheck.IsChecked ?? false;
            SaveAndApply();
        }

        private void IntervalCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            var tag = (IntervalCombo.SelectedItem as ComboBoxItem)?.Tag.ToString();
            if (int.TryParse(tag, out int seconds))
            {
                _settings.SlideshowIntervalSeconds = seconds;
                SaveAndApply();
            }
        }
    }
}
