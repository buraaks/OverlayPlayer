using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using OverlayPlayer.Helpers;
using OverlayPlayer.Models;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace OverlayPlayer
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _notifyIcon = null!;
        private ToolStripMenuItem _stopStartMenuItem = null!;
        private AppSettings _settings = null!;
        private HotkeyHelper? _hotkeyHelper;
        private System.Windows.Threading.DispatcherTimer? _slideshowTimer;
        private double _originalWidth = 300;
        private double _originalHeight = 300;

        public MainWindow()
        {
            try
            {
                _settings = AppSettings.Load();
                CheckInitialLanguage();
                LocalizationService.SetLanguage(_settings.Language);
                InitializeComponent();
                ApplySettings();
                this.Loaded += MainWindow_Loaded;
                SetupTrayIcon();
                
                MainVideo.MediaOpened += (s, e) => {
                    _originalWidth = MainVideo.NaturalVideoWidth;
                    _originalHeight = MainVideo.NaturalVideoHeight;
                    UpdateWindowSize();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationService.Get("InitError") + ex.Message);
            }
        }

        private void CheckInitialLanguage()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\OverlayPlayer", false))
                {
                    if (key != null)
                    {
                        var lang = key.GetValue("Language")?.ToString();
                        if (!string.IsNullOrEmpty(lang))
                        {
                            _settings.Language = lang switch
                            {
                                "Turkish" => "tr",
                                _ => "en"
                            };
                            _settings.Save();
                            using (var writeKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\OverlayPlayer", true))
                            {
                                writeKey?.DeleteValue("Language", false);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void ApplySettings()
        {
            this.Opacity = _settings.Opacity;
            UpdateWindowSize();
            MainVideo.Volume = _settings.Volume;
            MainVideo.IsMuted = _settings.IsMuted;
            ApplyRotation();
            ApplyZOrder();
            ToggleSlideshow(_settings.IsSlideshowEnabled);
            WindowHelper.SetWindowClickThrough(this, !_settings.IsInteractive);
        }

        public void ApplySettingsFromDialog()
        {
            ApplySettings();
            SetupTrayIcon(); // Refresh tray menu if language changed
        }

        private void ApplyRotation()
        {
            if (VideoRotation != null) VideoRotation.Angle = _settings.RotationAngle;
            if (GifRotation != null) GifRotation.Angle = _settings.RotationAngle;
        }

        private void ApplyZOrder()
        {
            WindowHelper.SetWindowZOrder(this, _settings.ShowOnTop, _settings.IsWallpaperMode);
        }

        private void SetupTrayIcon()
        {
            if (_notifyIcon != null) _notifyIcon.Dispose();
            _notifyIcon = new NotifyIcon();
            
            try
            {
                var iconUri = new Uri("pack://application:,,,/logo.png");
                var iconInfo = Application.GetResourceStream(iconUri);
                if (iconInfo != null)
                {
                    using (var stream = iconInfo.Stream)
                    using (var bitmap = new System.Drawing.Bitmap(stream))
                    {
                        IntPtr hIcon = bitmap.GetHicon();
                        try { using (var newIcon = System.Drawing.Icon.FromHandle(hIcon)) { _notifyIcon.Icon = (System.Drawing.Icon)newIcon.Clone(); } }
                        finally { WindowHelper.DestroyIcon(hIcon); }
                    }
                }
                else { _notifyIcon.Icon = System.Drawing.SystemIcons.Application; }
            }
            catch { _notifyIcon.Icon = System.Drawing.SystemIcons.Application; }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Overlay Player";
            var contextMenu = new ContextMenuStrip();
            
            // Core Actions
            contextMenu.Items.Add(LocalizationService.Get("ChangeMedia"), null, (s, e) => SelectAndLoadFile());

            // 90 Rotation remains as a quick action (opinionated, but useful)
            contextMenu.Items.Add(LocalizationService.Get("Rotate90"), null, (s, e) => {
                _settings.RotationAngle = (_settings.RotationAngle + 90) % 360;
                _settings.Save();
                ApplyRotation();
            });

            contextMenu.Items.Add(new ToolStripSeparator());

            // Settings Window Trigger
            contextMenu.Items.Add(LocalizationService.Get("Settings"), null, (s, e) => {
                var settingsWin = new SettingsWindow(_settings);
                settingsWin.Show();
            });

            contextMenu.Items.Add(new ToolStripSeparator());

            _stopStartMenuItem = new ToolStripMenuItem(_settings.Language == "tr" ? "Durdur" : "Stop", null, OnStopStartClicked);
            contextMenu.Items.Add(_stopStartMenuItem);

            contextMenu.Items.Add(LocalizationService.Get("Exit"), null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void OnInteractiveToggled(object? sender, EventArgs e)
        {
            _settings.IsInteractive = !_settings.IsInteractive;
            WindowHelper.SetWindowClickThrough(this, !_settings.IsInteractive);
            _settings.Save();
            if (_settings.IsInteractive) MessageBox.Show(LocalizationService.Get("EditModeActive"));
        }

        private void OnAutoStartToggled(object? sender, EventArgs e) { bool currentState = WindowHelper.IsAutoStartEnabled(); WindowHelper.SetAutoStart(!currentState); }

        private void OnStopStartClicked(object? sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible) { this.Hide(); _stopStartMenuItem.Text = LocalizationService.Get("Start"); }
            else { this.Show(); _stopStartMenuItem.Text = LocalizationService.Get("Stop"); }
        }

        private void ExitApplication() { _hotkeyHelper?.Dispose(); _notifyIcon.Dispose(); _slideshowTimer?.Stop(); Application.Current.Shutdown(); }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PositionWindow();
            WindowHelper.SetWindowClickThrough(this, !_settings.IsInteractive);
            _hotkeyHelper = new HotkeyHelper(this);
            _hotkeyHelper.HotkeyPressed += () => OnStopStartClicked(null, null);
            if (!string.IsNullOrEmpty(_settings.LastFilePath) && File.Exists(_settings.LastFilePath)) LoadMedia(_settings.LastFilePath);
            else { await System.Threading.Tasks.Task.Delay(100); SelectAndLoadFile(); }
        }

        private void PositionWindow() { var workingArea = SystemParameters.WorkArea; this.Left = 0; this.Top = workingArea.Height - this.Height; }

        private void UpdateWindowSize()
        {
            if (_settings.LockAspectRatio && _originalWidth > 0 && _originalHeight > 0)
            {
                double ratio = _originalWidth / _originalHeight;
                if (ratio > 1) { this.Width = _settings.WindowSize; this.Height = _settings.WindowSize / ratio; }
                else { this.Height = _settings.WindowSize; this.Width = _settings.WindowSize * ratio; }
            }
            else { this.Width = _settings.WindowSize; this.Height = _settings.WindowSize; }
            PositionWindow();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (_settings.IsInteractive) this.DragMove(); }

        private void SelectAndLoadFile()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog {
                    Title = LocalizationService.Get("SelectMediaTitle"),
                    Filter = $"{LocalizationService.Get("AllMediaFiles")}|*.mp4;*.gif;*.png;*.jpg;*.jpeg;*.bmp;*.avi;*.mov;*.wmv|Videos|*.mp4;*.avi;*.mov;*.wmv|Images & GIFs|*.gif;*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*"
                };
                if (openFileDialog.ShowDialog() == true) { LoadMediaWithPersistence(openFileDialog.FileName); }
                else if (string.IsNullOrEmpty(_settings.LastFilePath)) Application.Current.Shutdown();
            } catch { }
        }

        private void LoadMediaWithPersistence(string path)
        {
            _settings.LastFilePath = path;
            _settings.Save();
            LoadMedia(path);
            this.Show();
            _stopStartMenuItem.Text = LocalizationService.Get("Stop");
        }

        private void LoadMedia(string path)
        {
            try
            {
                string extension = Path.GetExtension(path).ToLower();
                string[] imageExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };
                if (extension == ".gif")
                {
                    MainVideo.Visibility = Visibility.Collapsed; MainGif.Visibility = Visibility.Visible;
                    var image = new System.Windows.Media.Imaging.BitmapImage(new Uri(path));
                    _originalWidth = image.PixelWidth; _originalHeight = image.PixelHeight;
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(MainGif, image);
                    UpdateWindowSize();
                }
                else if (Array.Exists(imageExtensions, e => e == extension))
                {
                    MainVideo.Visibility = Visibility.Collapsed; MainGif.Visibility = Visibility.Visible;
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(MainGif, null);
                    var image = new System.Windows.Media.Imaging.BitmapImage(new Uri(path));
                    _originalWidth = image.PixelWidth; _originalHeight = image.PixelHeight;
                    MainGif.Source = image;
                    UpdateWindowSize();
                }
                else
                {
                    MainVideo.Visibility = Visibility.Visible; MainGif.Visibility = Visibility.Collapsed;
                    MainVideo.Source = new Uri(path);
                    MainVideo.Play();
                }
            }
            catch (Exception ex) { MessageBox.Show(LocalizationService.Get("MediaLoadError") + ex.Message); }
        }

        private void ToggleSlideshow(bool enable)
        {
            if (_slideshowTimer == null)
            {
                _slideshowTimer = new System.Windows.Threading.DispatcherTimer();
                _slideshowTimer.Tick += (s, e) => PlayNextInFolder();
            }
            _slideshowTimer.Interval = TimeSpan.FromSeconds(_settings.SlideshowIntervalSeconds);
            if (enable) _slideshowTimer.Start();
            else _slideshowTimer.Stop();
        }

        private void PlayNextInFolder()
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.LastFilePath)) return;
                string? folder = Path.GetDirectoryName(_settings.LastFilePath);
                if (string.IsNullOrEmpty(folder)) return;
                string[] extensions = { ".mp4", ".gif", ".png", ".jpg", ".jpeg", ".bmp", ".avi", ".mov", ".wmv" };
                var files = Directory.GetFiles(folder).Where(f => extensions.Contains(Path.GetExtension(f).ToLower())).OrderBy(f => f).ToList();
                if (files.Count <= 1) return;
                int currentIndex = files.IndexOf(_settings.LastFilePath);
                int nextIndex = (currentIndex + 1) % files.Count;
                LoadMediaWithPersistence(files[nextIndex]);
            } catch { }
        }

        private void MainVideo_MediaEnded(object sender, RoutedEventArgs e) { MainVideo.Position = TimeSpan.Zero; MainVideo.Play(); }
    }
}

