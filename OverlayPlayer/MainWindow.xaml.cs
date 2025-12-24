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
        private ToolStripMenuItem _interactiveMenuItem = null!;
        private ToolStripMenuItem _autoStartMenuItem = null!;
        private AppSettings _settings = null!;

        public MainWindow()
        {
            try
            {
                _settings = AppSettings.Load();
                
                // İlk kurulumda dili Registry'den çek (Installer'ın yazdığı yer)
                CheckInitialLanguage();

                LocalizationService.SetLanguage(_settings.Language);
                InitializeComponent();
                ApplySettings();
                this.Loaded += MainWindow_Loaded;
                SetupTrayIcon();
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
                // Eğer daha önce kaydedilmiş bir dil yoksa veya ilk çalışmaysa Registry'e bak
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
                            
                            // Tek seferlik oku, sonra Registry'den silelim/güncelleyelim ki settings baskın kalsın
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
            this.Width = _settings.WindowSize;
            this.Height = _settings.WindowSize;
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            if (VideoRotation != null)
            {
                VideoRotation.Angle = _settings.RotationAngle;
            }
            if (GifRotation != null)
            {
                GifRotation.Angle = _settings.RotationAngle;
            }
        }

        private void SetupTrayIcon()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Dispose();
            }

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
                        try
                        {
                            using (var newIcon = System.Drawing.Icon.FromHandle(hIcon))
                            {
                                _notifyIcon.Icon = (System.Drawing.Icon)newIcon.Clone();
                            }
                        }
                        finally
                        {
                            WindowHelper.DestroyIcon(hIcon);
                        }
                    }
                }
                else
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch (Exception)
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Overlay Player";

            var contextMenu = new ContextMenuStrip();
            
            // Change Media
            contextMenu.Items.Add(LocalizationService.Get("ChangeMedia"), null, (s, e) => SelectAndLoadFile());
            
            contextMenu.Items.Add(new ToolStripSeparator());

            // Opacity Menu
            var opacityMenu = new ToolStripMenuItem(LocalizationService.Get("Opacity"));
            AddOpacityOption(opacityMenu, "20%", 0.2);
            AddOpacityOption(opacityMenu, "40%", 0.4);
            AddOpacityOption(opacityMenu, "60%", 0.6);
            AddOpacityOption(opacityMenu, "80%", 0.8);
            AddOpacityOption(opacityMenu, "100%", 1.0);
            contextMenu.Items.Add(opacityMenu);

            // Size Menu
            var sizeMenu = new ToolStripMenuItem(LocalizationService.Get("Size"));
            AddSizeOption(sizeMenu, LocalizationService.Get("Small"), 200);
            AddSizeOption(sizeMenu, LocalizationService.Get("Medium"), 300);
            AddSizeOption(sizeMenu, LocalizationService.Get("Large"), 400);
            AddSizeOption(sizeMenu, LocalizationService.Get("Huge"), 600);
            contextMenu.Items.Add(sizeMenu);

            // Rotate 90 Button
            contextMenu.Items.Add(LocalizationService.Get("Rotate90"), null, (s, e) => {
                _settings.RotationAngle = (_settings.RotationAngle + 90) % 360;
                _settings.Save();
                ApplyRotation();
            });

            contextMenu.Items.Add(new ToolStripSeparator());

            // Language Menu
            var langMenu = new ToolStripMenuItem(LocalizationService.Get("Language"));
            AddLanguageOption(langMenu, "English", "en");
            AddLanguageOption(langMenu, "Türkçe", "tr");
            contextMenu.Items.Add(langMenu);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Interactive Mode

            _interactiveMenuItem = new ToolStripMenuItem(LocalizationService.Get("Unlock"), null, OnInteractiveToggled);
            _interactiveMenuItem.Checked = _settings.IsInteractive;
            contextMenu.Items.Add(_interactiveMenuItem);

            // Run at Startup
            _autoStartMenuItem = new ToolStripMenuItem(LocalizationService.Get("RunAtStartup"), null, OnAutoStartToggled);
            _autoStartMenuItem.Checked = WindowHelper.IsAutoStartEnabled();
            contextMenu.Items.Add(_autoStartMenuItem);

            contextMenu.Items.Add(new ToolStripSeparator());


            // Stop / Start
            _stopStartMenuItem = new ToolStripMenuItem(_settings.Language == "tr" ? "Durdur" : "Stop", null, OnStopStartClicked);
            contextMenu.Items.Add(_stopStartMenuItem);

            // Exit
            contextMenu.Items.Add(LocalizationService.Get("Exit"), null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void AddOpacityOption(ToolStripMenuItem parent, string text, double value)
        {
            var item = new ToolStripMenuItem(text, null, (s, e) => {
                _settings.Opacity = value;
                this.Opacity = value;
                _settings.Save();
            });
            item.Checked = Math.Abs(_settings.Opacity - value) < 0.01;
            parent.DropDownItems.Add(item);
        }

        private void AddSizeOption(ToolStripMenuItem parent, string text, double value)
        {
            var item = new ToolStripMenuItem(text, null, (s, e) => {
                _settings.WindowSize = value;
                this.Width = value;
                this.Height = value;
                PositionWindow(); 
                _settings.Save();
            });
            item.Checked = Math.Abs(_settings.WindowSize - value) < 0.1;
            parent.DropDownItems.Add(item);
        }

        private void AddLanguageOption(ToolStripMenuItem parent, string text, string langCode)
        {
            var item = new ToolStripMenuItem(text, null, (s, e) => {
                _settings.Language = langCode;
                _settings.Save();
                LocalizationService.SetLanguage(langCode);
                SetupTrayIcon(); // Re-setup tray menu to refresh translations
            });
            item.Checked = _settings.Language == langCode;
            parent.DropDownItems.Add(item);
        }

        private void OnInteractiveToggled(object? sender, EventArgs e)
        {
            _settings.IsInteractive = !_settings.IsInteractive;
            _interactiveMenuItem.Checked = _settings.IsInteractive;
            WindowHelper.SetWindowClickThrough(this, !_settings.IsInteractive);
            _settings.Save();

            if (_settings.IsInteractive)
                MessageBox.Show(LocalizationService.Get("EditModeActive"));
        }

        private void OnAutoStartToggled(object? sender, EventArgs e)
        {
            bool currentState = WindowHelper.IsAutoStartEnabled();
            WindowHelper.SetAutoStart(!currentState);
            _autoStartMenuItem.Checked = !currentState;
        }

        private void OnStopStartClicked(object? sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.Hide();
                _stopStartMenuItem.Text = LocalizationService.Get("Start");
            }
            else
            {
                this.Show();
                _stopStartMenuItem.Text = LocalizationService.Get("Stop");
            }
        }

        private void ExitApplication()
        {
            _notifyIcon.Dispose();
            Application.Current.Shutdown();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PositionWindow();
            WindowHelper.SetWindowClickThrough(this, !_settings.IsInteractive);
            
            if (!string.IsNullOrEmpty(_settings.LastFilePath) && File.Exists(_settings.LastFilePath))
            {
                LoadMedia(_settings.LastFilePath);
            }
            else
            {
                await System.Threading.Tasks.Task.Delay(100);
                SelectAndLoadFile();
            }
        }

        private void PositionWindow()
        {
            var workingArea = SystemParameters.WorkArea;
            this.Left = 0;
            this.Top = workingArea.Height - this.Height;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_settings.IsInteractive)
            {
                this.DragMove();
            }
        }

        private void SelectAndLoadFile()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = LocalizationService.Get("SelectMediaTitle"),
                    Filter = $"{LocalizationService.Get("AllMediaFiles")}|*.mp4;*.gif;*.png;*.jpg;*.jpeg;*.bmp;*.avi;*.mov;*.wmv|Videos|*.mp4;*.avi;*.mov;*.wmv|Images & GIFs|*.gif;*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _settings.LastFilePath = openFileDialog.FileName;
                    _settings.Save();
                    LoadMedia(_settings.LastFilePath);
                    this.Show();
                    _stopStartMenuItem.Text = LocalizationService.Get("Stop");
                }
                else if (string.IsNullOrEmpty(_settings.LastFilePath))
                {
                    Application.Current.Shutdown();
                }
            }
            catch { }
        }

        private void LoadMedia(string path)
        {
            try
            {
                string extension = Path.GetExtension(path).ToLower();
                string[] imageExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };

                if (extension == ".gif")
                {
                    MainVideo.Visibility = Visibility.Collapsed;
                    MainGif.Visibility = Visibility.Visible;
                    var image = new System.Windows.Media.Imaging.BitmapImage(new Uri(path));
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(MainGif, image);
                }
                else if (Array.Exists(imageExtensions, e => e == extension))
                {
                    MainVideo.Visibility = Visibility.Collapsed;
                    MainGif.Visibility = Visibility.Visible;
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(MainGif, null);
                    MainGif.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(path));
                }
                else
                {
                    MainVideo.Visibility = Visibility.Visible;
                    MainGif.Visibility = Visibility.Collapsed;
                    MainVideo.Source = new Uri(path);
                    MainVideo.Play();
                }
            }
            catch (Exception ex) { MessageBox.Show(LocalizationService.Get("MediaLoadError") + ex.Message); }
        }



        private void MainVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            MainVideo.Position = TimeSpan.Zero;
            MainVideo.Play();
        }
    }
}

