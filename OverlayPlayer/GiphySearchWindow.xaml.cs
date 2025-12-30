using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OverlayPlayer.Helpers;
using OverlayPlayer.Services;
using OverlayPlayer.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using Image = System.Windows.Controls.Image;

namespace OverlayPlayer
{
    public partial class GiphySearchWindow : Window
    {
        private readonly GiphyService _giphyService;
        private readonly string _apiKey;
        private readonly MainWindow _mainWindow;

        public GiphySearchWindow(MainWindow mainWindow, string apiKey)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _apiKey = apiKey;
            _giphyService = new GiphyService();
            ApplyLocalization();

            // Validate API key before loading
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                MessageBox.Show(
                    "Giphy API key is not configured. Please enter your API key in Settings.\n\n" +
                    "You can get a free API key from: https://developers.giphy.com/",
                    "API Key Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                LoadingText.Text = "Please configure API key in Settings";
                LoadingText.Visibility = Visibility.Visible;
                ResultsPanel.IsEnabled = false;
                SearchBox.IsEnabled = false;
                SearchButton.IsEnabled = false;
            }
            else
            {
                LoadTrending();
            }
        }

        private void ApplyLocalization()
        {
            TitleText.Text = LocalizationService.Get("SearchGiphy");
            SearchBox.Tag = LocalizationService.Get("Search"); // For placeholder behavior if we implemented it
            SearchButton.Content = LocalizationService.Get("Search");
        }

        private async void LoadTrending()
        {
            SetLoading(true);
            try
            {
                var response = await _giphyService.Trending(_apiKey).ConfigureAwait(false);
                
                // Switch back to UI thread before updating UI
                await Dispatcher.InvokeAsync(() =>
                {
                    DisplayResults(response);
                    SetLoading(false);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error loading trending: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SetLoading(false);
                });
            }
        }

        private async void DoSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            // Check API key before search
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                MessageBox.Show(
                    "Giphy API key is not configured. Please enter your API key in Settings.",
                    "API Key Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            SetLoading(true);
            try
            {
                var response = await _giphyService.Search(query, _apiKey).ConfigureAwait(false);
                
                // Switch back to UI thread before updating UI
                await Dispatcher.InvokeAsync(() =>
                {
                    if (response == null || response.Data == null || response.Data.Count == 0)
                    {
                        MessageBox.Show(
                            "No results found or API request failed. Please check your API key and internet connection.",
                            "No Results",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    DisplayResults(response);
                    SetLoading(false);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error searching: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SetLoading(false);
                });
            }
        }

        private void DisplayResults(GiphyResponse? response)
        {
            // Wrap entire method in try-catch to catch any unhandled exceptions
            try
            {
                // Clear on UI thread
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => DisplayResults(response));
                    return;
                }

                // Safely clear children - use a more defensive approach
                try
                {
                    // Create a list of children to remove to avoid modification during iteration
                    var childrenToRemove = new System.Collections.Generic.List<UIElement>();
                    foreach (UIElement child in ResultsPanel.Children)
                    {
                        childrenToRemove.Add(child);
                    }
                    
                    foreach (var child in childrenToRemove)
                    {
                        try
                        {
                            ResultsPanel.Children.Remove(child);
                        }
                        catch { }
                    }
                }
                catch (Exception clearEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error clearing ResultsPanel: {clearEx.GetType().Name} - {clearEx.Message}");
                    // If all else fails, just continue - old items will be replaced
                }

                if (response == null || response.Data == null) return;

                // Get data list safely - create a copy to avoid collection modification issues
                var dataList = response.Data.ToList(); // Create a copy
                if (dataList == null || dataList.Count == 0) return;

                // Limit the number of items to prevent memory issues
                int maxItems = Math.Min(dataList.Count, 50);
                
                // Use foreach for safer iteration, but limit the count
                int processedCount = 0;
                foreach (var item in dataList)
                {
                    if (processedCount >= maxItems) break;
                    processedCount++;
                    
                    // Null-safe checks for Images and its properties
                    if (item?.Images == null) continue;

                    string? url = item.Images.FixedHeight?.Url;
                    // Using FixedHeight for better grid layout, fallback to Original if empty
                    if (string.IsNullOrEmpty(url)) 
                    {
                        url = item.Images.Original?.Url;
                    }

                    // Skip if no valid URL found
                    if (string.IsNullOrEmpty(url)) continue;

                    try
                    {
                        var border = new Border
                        {
                            Width = 150,
                            Height = 150,
                            Margin = new Thickness(5),
                            CornerRadius = new CornerRadius(8),
                            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                            ClipToBounds = true,
                            Cursor = Cursors.Hand
                        };

                        var image = new Image
                        {
                            Stretch = Stretch.UniformToFill,
                            Visibility = Visibility.Visible
                        };

                        // Use the preview URL or downsized for faster loading in the grid
                        var previewUrl = item.Images.Downsized?.Url;
                        if (string.IsNullOrEmpty(previewUrl))
                        {
                            // Fallback: try to load from FixedHeight or Original URL
                            previewUrl = item.Images.FixedHeight?.Url ?? item.Images.Original?.Url;
                        }

                        if (!string.IsNullOrEmpty(previewUrl))
                        {
                            // Load image directly without WpfAnimatedGif to avoid index errors
                            // This will show static previews, but avoids the index exception
                            try
                            {
                                var bitmapImage = new BitmapImage();
                                bitmapImage.BeginInit();
                                bitmapImage.UriSource = new Uri(previewUrl);
                                bitmapImage.CacheOption = BitmapCacheOption.OnDemand;
                                bitmapImage.EndInit();
                                
                                // Use regular Image.Source instead of WpfAnimatedGif to avoid index errors
                                image.Source = bitmapImage;
                                
                                // Note: GIFs won't animate in preview, but they will work when selected
                            }
                            catch (UriFormatException ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Invalid image URL: {previewUrl}, Error: {ex.Message}");
                            }
                            catch (System.Net.WebException ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Network error loading image: {previewUrl}, Error: {ex.Message}");
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Index error loading image: {previewUrl}, Error: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to load image: {previewUrl}, Error: {ex.Message}, Type: {ex.GetType().Name}");
                            }
                        }

                        border.Child = image;
                        border.MouseLeftButtonUp += (s, e) => OnGifSelected(item);

                        // Safely add to panel
                        try
                        {
                            ResultsPanel.Children.Add(border);
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Index error adding border to panel: {ex.Message}");
                            // Skip this item if we can't add it
                            continue;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error adding border to panel: {ex.Message}");
                            // Skip this item if we can't add it
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle any errors during UI element creation
                        System.Diagnostics.Debug.WriteLine($"Error creating result item: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't show MessageBox to avoid spam
                System.Diagnostics.Debug.WriteLine($"Error in DisplayResults: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                // Don't show MessageBox here - let global handler deal with it if needed
            }
        }

        private async void OnGifSelected(GiphyObject item)
        {
            SetLoading(true, LocalizationService.Get("Downloading"));
            try
            {
                // Null-safe check for Images and Original
                var url = item?.Images?.Original?.Url;
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("Invalid GIF URL. Please try another GIF.");
                    return;
                }

                var localPath = await _giphyService.DownloadGif(url, item?.Id ?? "unknown");
                if (localPath != null)
                {
                    _mainWindow.LoadMediaWithPersistence(localPath);
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to download GIF. Please check your internet connection and try again.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading GIF: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void SetLoading(bool isLoading, string text = "")
        {
            if (isLoading)
            {
                LoadingText.Text = string.IsNullOrEmpty(text) ? "Loading..." : text;
                LoadingText.Visibility = Visibility.Visible;
                ResultsPanel.IsEnabled = false;
            }
            else
            {
                LoadingText.Visibility = Visibility.Collapsed;
                ResultsPanel.IsEnabled = true;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            DoSearch(SearchBox.Text);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoSearch(SearchBox.Text);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _giphyService?.Dispose();
            base.OnClosed(e);
        }
    }
}
