using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using OverlayPlayer.Helpers;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace OverlayPlayer
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _notifyIcon = null!;
        private ToolStripMenuItem _stopStartMenuItem = null!;
        private string? _lastSelectedPath;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                this.Loaded += MainWindow_Loaded;
                SetupTrayIcon();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Başlatma hatası: " + ex.Message);
            }
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
                        // İkonun tutamacını al
                        IntPtr hIcon = bitmap.GetHicon();
                        try
                        {
                            // Icon.FromHandle ile ikonu oluştur
                            using (var newIcon = System.Drawing.Icon.FromHandle(hIcon))
                            {
                                // İkonu kopyalayarak sızıntıyı önle ve boyutu koru
                                _notifyIcon.Icon = (System.Drawing.Icon)newIcon.Clone();
                            }
                        }
                        finally
                        {
                            // hIcon'u serbest bırak (GDI sızıntısını önler)
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
            contextMenu.Items.Add("Değiştir", null, (s, e) => SelectAndLoadFile());
            _stopStartMenuItem = new ToolStripMenuItem("Durdur", null, OnStopStartClicked);
            contextMenu.Items.Add(_stopStartMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Kapat", null, (s, e) => ExitApplication());

            _notifyIcon.ContextMenuStrip = contextMenu;
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
            try { WindowHelper.SetWindowClickThrough(this); } catch { }
            await System.Threading.Tasks.Task.Delay(100);
            SelectAndLoadFile();
        }

        private void PositionWindow()
        {
            var workingArea = SystemParameters.WorkArea;
            this.Left = 0;
            this.Top = workingArea.Height - this.Height;
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
                    _lastSelectedPath = openFileDialog.FileName;
                    LoadMedia(_lastSelectedPath);
                    this.Show();
                    _stopStartMenuItem.Text = "Durdur";
                }
                else if (string.IsNullOrEmpty(_lastSelectedPath))
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
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(MainGif, null); // Eski GIF varsa temizle
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
