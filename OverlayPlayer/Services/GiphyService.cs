using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OverlayPlayer.Models;

namespace OverlayPlayer.Services
{
    public class GiphyService : IDisposable
    {
        private static readonly HttpClient _sharedHttpClient = new HttpClient();
        private readonly HttpClient _httpClient;
        private readonly bool _useSharedClient;
        private bool _disposed = false;
        private const string BaseUrl = "https://api.giphy.com/v1";

        public GiphyService(bool useSharedClient = true)
        {
            _useSharedClient = useSharedClient;
            _httpClient = useSharedClient ? _sharedHttpClient : new HttpClient();
        }

        public async Task<GiphyResponse?> Search(string query, string apiKey, bool isSticker = false, int limit = 25, int offset = 0)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                System.Diagnostics.Debug.WriteLine("GiphyService.Search: API key is empty");
                return null;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                System.Diagnostics.Debug.WriteLine("GiphyService.Search: Query is empty");
                return null;
            }

            try
            {
                string encodedQuery = Uri.EscapeDataString(query);
                string type = isSticker ? "stickers" : "gifs";
                string url = $"{BaseUrl}/{type}/search?api_key={apiKey}&q={encodedQuery}&limit={limit}&offset={offset}&rating=g&lang=en";
                
                using var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"GiphyService.Search: HTTP {response.StatusCode} - {errorContent}");
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return null;

                try
                {
                    return JsonSerializer.Deserialize<GiphyResponse>(json);
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GiphyService.Search: JSON deserialization error - {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.Search: Error - {ex.Message}");
                return null;
            }
        }

        public async Task<GiphyResponse?> Trending(string apiKey, bool isSticker = false, int limit = 25, int offset = 0)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            try
            {
                string type = isSticker ? "stickers" : "gifs";
                string url = $"{BaseUrl}/{type}/trending?api_key={apiKey}&limit={limit}&offset={offset}&rating=g";
                
                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json)) return null;

                return JsonSerializer.Deserialize<GiphyResponse>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.Trending: Error - {ex.Message}");
                return null;
            }
        }

        public async Task<string?> DownloadGif(string url, string id)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                System.Diagnostics.Debug.WriteLine("GiphyService.DownloadGif: URL is empty");
                return null;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                System.Diagnostics.Debug.WriteLine("GiphyService.DownloadGif: ID is empty");
                return null;
            }

            try
            {
                string cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "OverlayPlayer",
                    "Cache"
                );

                try
                {
                    if (!Directory.Exists(cacheDir))
                    {
                        Directory.CreateDirectory(cacheDir);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GiphyService.DownloadGif: Failed to create cache directory - {ex.Message}");
                    return null;
                }

                string filePath = Path.Combine(cacheDir, $"{id}.gif");

                if (File.Exists(filePath))
                {
                    return filePath;
                }

                try
                {
                    var imageBytes = await _httpClient.GetByteArrayAsync(url);
                    
                    if (imageBytes == null || imageBytes.Length == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("GiphyService.DownloadGif: Downloaded file is empty");
                        return null;
                    }

                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    return filePath;
                }
                catch (HttpRequestException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GiphyService.DownloadGif: HTTP request error - {ex.Message}");
                    return null;
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GiphyService.DownloadGif: File I/O error - {ex.Message}");
                    return null;
                }
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.DownloadGif: Request timeout - {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.DownloadGif: Unexpected error - {ex.Message}");
                return null;
            }
        }

        public async Task<bool> TestApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            try
            {
                string url = $"{BaseUrl}/gifs/trending?api_key={apiKey}&limit=1&rating=g";
                using var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return false;
                    }

                    try
                    {
                        var result = JsonSerializer.Deserialize<GiphyResponse>(json);
                        return result != null && result.Data != null && result.Data.Count > 0;
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GiphyService.TestApiKeyAsync: HTTP {response.StatusCode}");
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.TestApiKeyAsync: HTTP request error - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.TestApiKeyAsync: Unexpected error - {ex.Message}");
                return false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Only dispose if we created our own HttpClient
                    if (!_useSharedClient)
                    {
                        _httpClient?.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
