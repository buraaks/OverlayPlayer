using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OverlayPlayer.Models
{
    public class GiphyResponse
    {
        [JsonPropertyName("data")]
        public List<GiphyObject> Data { get; set; } = new();

        [JsonPropertyName("pagination")]
        public GiphyPagination Pagination { get; set; } = new();

        [JsonPropertyName("meta")]
        public GiphyMeta Meta { get; set; } = new();
    }

    public class GiphyObject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("images")]
        public GiphyImages Images { get; set; } = new();
    }

    public class GiphyImages
    {
        [JsonPropertyName("original")]
        public GiphyImageDetail Original { get; set; } = new();

        [JsonPropertyName("fixed_height")]
        public GiphyImageDetail FixedHeight { get; set; } = new();
        
        [JsonPropertyName("downsized")]
        public GiphyImageDetail Downsized { get; set; } = new();
    }

    public class GiphyImageDetail
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("width")]
        public string Width { get; set; } = string.Empty;

        [JsonPropertyName("height")]
        public string Height { get; set; } = string.Empty;
        
        [JsonPropertyName("size")]
        public string Size { get; set; } = string.Empty;
    }

    public class GiphyPagination
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }
    }

    public class GiphyMeta
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = string.Empty;
    }
}
