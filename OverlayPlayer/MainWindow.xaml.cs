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
                InitializeComponent();
                ApplySettings();
                this.Loaded += MainWindow_Loaded;
                SetupTrayIcon();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Başlatma hatası: " + ex.Message);
            }
        }

        private void ApplySettings()
        {
            this.Opacity = _settings.Opacity;
            this.Width = _settings.WindowSize;
            this.Height = _settings.WindowSize;
        }

        private void SetupTrayIcon()
        {
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
            
            // Medya Değiştir
            contextMenu.Items.Add("Medyayı Değiştir", null, (s, e) => SelectAndLoadFile());
            
            contextMenu.Items.Add(new ToolStripSeparator());

            // Saydamlık Menüsü
            var opacityMenu = new ToolStripMenuItem("Saydamlık");
            AddOpacityOption(opacityMenu, "%20", 0.2);
            AddOpacityOption(opacityMenu, "%40", 0.4);
            AddOpacityOption(opacityMenu, "%60", 0.6);
            AddOpacityOption(opacityMenu, "%80", 0.8);
            AddOpacityOption(opacityMenu, "%100", 1.0);
            contextMenu.Items.Add(opacityMenu);

            // Boyut Menüsü
            var sizeMenu = new ToolStripMenuItem("Boyut");
            AddSizeOption(sizeMenu, "Küçük (200x200)", 200);
            AddSizeOption(sizeMenu, "Orta (300x300)", 300);
            AddSizeOption(sizeMenu, "Büyük (400x400)", 400);
            AddSizeOption(sizeMenu, "Dev (600x600)", 600);
            contextMenu.Items.Add(sizeMenu);

            contextMenu.Items.Add(new ToolStripSeparator());

            // İnteraktif Mod
            _interactiveMenuItem = new ToolStripMenuItem("Konum Değiştir (Kilit Aç)", null, OnInteractiveToggled);
            _interactiveMenuItem.Checked = _settings.IsInteractive;
            contextMenu.Items.Add(_interactiveMenuItem);

            // Başlangıçta Çalıştır
            _autoStartMenuItem = new ToolStripMenuItem("Başlangıçta Çalıştır", null, OnAutoStartToggled);
            _autoStartMenuItem.Checked = WindowHelper.IsAutoStartEnabled();
            contextMenu.Items.Add(_autoStartMenuItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Durdur / Başlat
            _stopStartMenuItem = new ToolStripMenuItem("Durdur", null, OnStopStartClicked);
            contextMenu.Items.Add(_stopStartMenuItem);

            // Kapat
            contextMenu.Items.Add("Kapat", null, (s, e) => ExitApplication());

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
                PositionWindow(); // Boyut değişince köşeye tekrar oturt
                _settings.Save();
            });
            item.Checked = Math.Abs(_settings.WindowSize - value) < 0.1;
            parent.DropDownItems.Add(item);
        }

        private void OnInteractiveToggled(object? sender, EventArgs e)
        {
            _settings.IsInteractive = !_settings.IsInteractive;
            _interactiveMenuItem.Checked = _settings.IsInteractive;
            WindowHelper.SetWindowClickThrough(this, !_settings.IsInteractive);
            _settings.Save();

            if (_settings.IsInteractive)
                MessageBox.Show("Düzenleme modu aktif! Medyayı farenizle sürükleyebilirsiniz. İşiniz bitince kilidi tekrar kapatmayı unutmayın.");
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
                _stopStartMenuItem.Text = "Başlat";
            }
            else
            {
                this.Show();
                _stopStartMenuItem.Text = "Durdur";
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
                    Title = "Medya Seç (Video, GIF veya Resim)",
                    Filter = "Tüm Medya Dosyaları|*.mp4;*.gif;*.png;*.jpg;*.jpeg;*.bmp;*.avi;*.mov;*.wmv|Videolar|*.mp4;*.avi;*.mov;*.wmv|Resimler & GIFler|*.gif;*.png;*.jpg;*.jpeg;*.bmp|Tüm Dosyalar|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _settings.LastFilePath = openFileDialog.FileName;
                    _settings.Save();
                    LoadMedia(_settings.LastFilePath);
                    this.Show();
                    _stopStartMenuItem.Text = "Durdur";
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
            catch (Exception ex) { MessageBox.Show("Medya yükleme hatası: " + ex.Message); }
        }

        private void MainVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            MainVideo.Position = TimeSpan.Zero;
            MainVideo.Play();
        }
    }
}

