using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
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
        private System.Windows.Threading.DispatcherTimer? _topmostWatchdogTimer;
        private System.Drawing.Icon? _cachedTrayIcon;
        private double _originalWidth = 300;
        private double _originalHeight = 300;
        private string? _lastMediaFolder;
        private List<string>? _cachedFiles;
        private IntPtr _lastForegroundWindow = IntPtr.Zero;

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

                _topmostWatchdogTimer = new System.Windows.Threading.DispatcherTimer();
                _topmostWatchdogTimer.Interval = TimeSpan.FromMilliseconds(500); // 250ms -> 500ms for balance
                _topmostWatchdogTimer.Tick += (s, e) => {
                    if (_settings.ShowOnTop && this.Visibility == Visibility.Visible)
                    {
                        var currentForeground = WindowHelper.GetForegroundWindow();
                        if (currentForeground == _lastForegroundWindow) return;
                        _lastForegroundWindow = currentForeground;

                        ApplyZOrder();
                        
                        if (WindowHelper.IsOtherWindowFullscreen())
                        {
                            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                            if (hwnd != IntPtr.Zero)
                            {
                                int exStyle = WindowHelper.GetWindowLong(hwnd, WindowHelper.GWL_EXSTYLE);
                                WindowHelper.SetWindowLong(hwnd, WindowHelper.GWL_EXSTYLE, 
                                    exStyle | WindowHelper.WS_EX_TOPMOST | WindowHelper.WS_EX_TOOLWINDOW);
                            }
                            ApplyZOrder();
                        }
                    }
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
                        var lang = key.GetValue("Language")?.ToString()?.ToLower();
                        if (!string.IsNullOrEmpty(lang))
                        {
                            _settings.Language = lang switch
                            {
                                "turkish" => "tr",
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
            
            if (_settings.ShowOnTop) _topmostWatchdogTimer?.Start();
            else _topmostWatchdogTimer?.Stop();

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
                // Try to load from embedded icon first
                System.Drawing.Icon? appIcon = null;
                
                // Method 1: Try to load from application icon (logo.ico)
                try
                {
                    var iconUri = new Uri("pack://application:,,,/logo.ico");
                    var iconInfo = Application.GetResourceStream(iconUri);
                    if (iconInfo != null)
                    {
                        using (var stream = iconInfo.Stream)
                        {
                            appIcon = new System.Drawing.Icon(stream);
                        }
                    }
                }
                catch { }
                
                // Method 2: If icon not found, try PNG and convert
                if (appIcon == null)
                {
                    try
                    {
                        var iconUri = new Uri("pack://application:,,,/logo.png");
                        var iconInfo = Application.GetResourceStream(iconUri);
                        if (iconInfo != null)
                        {
                            using (var stream = iconInfo.Stream)
                            using (var bitmap = new System.Drawing.Bitmap(stream))
                            {
                                // Resize to 32x32 for better tray icon display
                                using (var resized = new System.Drawing.Bitmap(bitmap, new System.Drawing.Size(32, 32)))
                                {
                                    IntPtr hIcon = resized.GetHicon();
                                    try 
                                    { 
                                        appIcon = System.Drawing.Icon.FromHandle(hIcon);
                                        appIcon = (System.Drawing.Icon)appIcon.Clone(); // Clone to keep after handle is destroyed
                                    }
                                    finally { WindowHelper.DestroyIcon(hIcon); }
                                }
                            }
                        }
                    }
                    catch { }
                }
                
                // Method 3: Try to load from executable's icon
                if (appIcon == null)
                {
                    try
                    {
                        // For single-file apps, use AppContext.BaseDirectory instead of Assembly.Location
                        var baseDir = AppContext.BaseDirectory;
                        var exePath = System.IO.Path.Combine(baseDir, "OverlayPlayer.exe");
                        
                        // If exe not found in base directory, try current process path
                        if (!System.IO.File.Exists(exePath))
                        {
                            exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                        }
                        
                        if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                        {
                            appIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                        }
                    }
                    catch { }
                }
                
                // Set the icon
                if (appIcon != null)
                {
                    _cachedTrayIcon = appIcon;
                    _notifyIcon.Icon = _cachedTrayIcon;
                }
                else
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch 
            { 
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application; 
            }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Overlay Player";
            var contextMenu = new ContextMenuStrip();
            
            // Core Actions
            contextMenu.Items.Add(LocalizationService.Get("ChangeMedia"), null, (s, e) => SelectAndLoadFile());
            contextMenu.Items.Add(LocalizationService.Get("SearchGiphy"), null, (s, e) => {
                var searchWin = new GiphySearchWindow(this, _settings.GiphyApiKey);
                searchWin.Owner = this;
                searchWin.Show();
            });

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
                settingsWin.Owner = this;
                settingsWin.Show();
            });

            contextMenu.Items.Add(new ToolStripSeparator());

            _stopStartMenuItem = new ToolStripMenuItem(LocalizationService.Get("Stop"), null, OnStopStartClicked);
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


        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PositionWindow();
            WindowHelper.SetWindowClickThrough(this, !_settings.IsInteractive);
            
            // Ensure ToolWindow style is applied to help stay on top of fullscreen apps
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = WindowHelper.GetWindowLong(hwnd, WindowHelper.GWL_EXSTYLE);
            WindowHelper.SetWindowLong(hwnd, WindowHelper.GWL_EXSTYLE, exStyle | WindowHelper.WS_EX_TOOLWINDOW);

            _hotkeyHelper = new HotkeyHelper(this);
            _hotkeyHelper.HotkeyPressed += () => OnStopStartClicked(null, EventArgs.Empty);
            if (!string.IsNullOrEmpty(_settings.LastFilePath) && File.Exists(_settings.LastFilePath)) LoadMedia(_settings.LastFilePath);
            else 
            { 
                await System.Threading.Tasks.Task.Delay(100); 
                if (this.IsVisible) SelectAndLoadFile(); 
            }
        }

        private void PositionWindow()
        {
            // Kaydedilmiş konum varsa onu kullan
            if (_settings.WindowLeft.HasValue && _settings.WindowTop.HasValue)
            {
                double left = _settings.WindowLeft.Value;
                double top = _settings.WindowTop.Value;

                // Basic safety: Ensure window is not completely off-screen
                // VirtualScreen covers all monitors
                if (left < SystemParameters.VirtualScreenLeft - this.Width + 50 || 
                    left > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 50 ||
                    top < SystemParameters.VirtualScreenTop - 50 ||
                    top > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 50)
                {
                    SetDefaultPosition();
                }
                else
                {
                    this.Left = left;
                    this.Top = top;
                }
            }
            else
            {
                SetDefaultPosition();
            }
        }

        private void SetDefaultPosition()
        {
            var workingArea = SystemParameters.WorkArea;
            this.Left = 0;
            this.Top = workingArea.Height - this.Height;
        }

        private void UpdateWindowSize()
        {
            // Safety: Ensure WindowSize is within reasonable bounds
            if (_settings.WindowSize < 50) _settings.WindowSize = 50;
            if (_settings.WindowSize > 4000) _settings.WindowSize = 4000;

            if (_settings.LockAspectRatio && _originalWidth > 0 && _originalHeight > 0)
            {
                double ratio = _originalWidth / _originalHeight;
                if (ratio > 1) { this.Width = _settings.WindowSize; this.Height = _settings.WindowSize / ratio; }
                else { this.Height = _settings.WindowSize; this.Width = _settings.WindowSize * ratio; }
            }
            else { this.Width = _settings.WindowSize; this.Height = _settings.WindowSize; }
            
            // Sadece ilk yüklemede atau konum kaydedilmemişse konumlandır
            if (!_settings.WindowLeft.HasValue || !_settings.WindowTop.HasValue)
            {
                PositionWindow();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_settings.IsInteractive)
            {
                this.DragMove();
                // Sürükleme bittiğinde konumu kaydet
                _settings.WindowLeft = this.Left;
                _settings.WindowTop = this.Top;
                _settings.Save();
            }
        }

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

        public void LoadMediaWithPersistence(string path)
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
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(path);
                    // Decode at window size to save memory
                    image.DecodePixelWidth = (int)_settings.WindowSize;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    
                    _originalWidth = image.PixelWidth; _originalHeight = image.PixelHeight;
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(MainGif, image);
                    UpdateWindowSize();
                }
                else if (Array.Exists(imageExtensions, e => e == extension))
                {
                    MainVideo.Visibility = Visibility.Collapsed; MainGif.Visibility = Visibility.Visible;
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(MainGif, null);
                    
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(path);
                    // Decode at window size to save memory
                    image.DecodePixelWidth = (int)_settings.WindowSize;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    
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
            else { _slideshowTimer.Stop(); _cachedFiles = null; _lastMediaFolder = null; }
        }

        private void PlayNextInFolder()
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.LastFilePath)) return;
                string? folder = Path.GetDirectoryName(_settings.LastFilePath);
                if (string.IsNullOrEmpty(folder)) return;

                // Cache file list to avoid frequent disk IO
                if (_lastMediaFolder != folder || _cachedFiles == null)
                {
                    string[] extensions = { ".mp4", ".gif", ".png", ".jpg", ".jpeg", ".bmp", ".avi", ".mov", ".wmv" };
                    _cachedFiles = Directory.GetFiles(folder)
                        .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                        .OrderBy(f => f)
                        .ToList();
                    _lastMediaFolder = folder;
                }

                if (_cachedFiles == null || _cachedFiles.Count <= 1) return;
                
                int currentIndex = _cachedFiles.IndexOf(_settings.LastFilePath);
                if (currentIndex < 0) currentIndex = -1;
                
                int nextIndex = (currentIndex + 1) % _cachedFiles.Count;
                
                if (nextIndex >= 0 && nextIndex < _cachedFiles.Count)
                {
                    LoadMediaWithPersistence(_cachedFiles[nextIndex]);
                }
            } 
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PlayNextInFolder: {ex.Message}");
            }
        }

        private void MainVideo_MediaEnded(object sender, RoutedEventArgs e) { MainVideo.Position = TimeSpan.Zero; MainVideo.Play(); }

        private void ExitApplication() 
        { 
            try
            {
                _hotkeyHelper?.Dispose(); 
                _cachedTrayIcon?.Dispose();
                _notifyIcon?.Dispose(); 
                _slideshowTimer?.Stop(); 
                _topmostWatchdogTimer?.Stop();
                _settings?.Save(); // Guaranteed final save
            }
            catch { }
            Application.Current.Shutdown(); 
        }
    }
}