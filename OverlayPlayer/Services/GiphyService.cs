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
        private const string BaseUrl = "https://api.giphy.com/v1/gifs";

        public GiphyService(bool useSharedClient = true)
        {
            _useSharedClient = useSharedClient;
            _httpClient = useSharedClient ? _sharedHttpClient : new HttpClient();
        }

        public async Task<GiphyResponse?> Search(string query, string apiKey, int limit = 25, int offset = 0)
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
                string url = $"{BaseUrl}/search?api_key={apiKey}&q={encodedQuery}&limit={limit}&offset={offset}&rating=g&lang=en";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"GiphyService.Search: HTTP {response.StatusCode} - {errorContent}");
                    
                    // Handle specific error codes
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("GiphyService.Search: Invalid API key");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        System.Diagnostics.Debug.WriteLine("GiphyService.Search: Rate limit exceeded");
                    }
                    
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    System.Diagnostics.Debug.WriteLine("GiphyService.Search: Empty response from API");
                    return null;
                }

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
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.Search: HTTP request error - {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.Search: Request timeout - {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.Search: Unexpected error - {ex.Message}");
                return null;
            }
        }

        public async Task<GiphyResponse?> Trending(string apiKey, int limit = 25, int offset = 0)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                System.Diagnostics.Debug.WriteLine("GiphyService.Trending: API key is empty");
                return null;
            }

            try
            {
                string url = $"{BaseUrl}/trending?api_key={apiKey}&limit={limit}&offset={offset}&rating=g";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"GiphyService.Trending: HTTP {response.StatusCode} - {errorContent}");
                    
                    // Handle specific error codes
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine("GiphyService.Trending: Invalid API key");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        System.Diagnostics.Debug.WriteLine("GiphyService.Trending: Rate limit exceeded");
                    }
                    
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    System.Diagnostics.Debug.WriteLine("GiphyService.Trending: Empty response from API");
                    return null;
                }

                try
                {
                    return JsonSerializer.Deserialize<GiphyResponse>(json);
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GiphyService.Trending: JSON deserialization error - {ex.Message}");
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.Trending: HTTP request error - {ex.Message}");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.Trending: Request timeout - {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GiphyService.Trending: Unexpected error - {ex.Message}");
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
                string url = $"{BaseUrl}/trending?api_key={apiKey}&limit=1&rating=g";
                var response = await _httpClient.GetAsync(url);
                
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
