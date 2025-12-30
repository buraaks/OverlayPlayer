using System.Collections.Generic;

namespace OverlayPlayer.Helpers
{
    public static class LocalizationService
    {
        private static string _currentLanguage = "en";

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["en"] = new()
            {
                ["ChangeMedia"] = "Change Media",
                ["Settings"] = "Settings",
                ["Save"] = "Save",
                ["Close"] = "Close",
                ["ResetDefaults"] = "Reset to Defaults",
                ["Opacity"] = "Opacity",
                ["Size"] = "Size",
                ["Small"] = "Small (200x200)",
                ["Medium"] = "Medium (300x300)",
                ["Large"] = "Large (400x400)",
                ["Huge"] = "Huge (600x600)",
                ["Unlock"] = "Change Position (Unlock)",
                ["RunAtStartup"] = "Run at Startup",
                ["Stop"] = "Stop",
                ["Start"] = "Start",
                ["Exit"] = "Exit",
                ["Language"] = "Language",
                ["Rotate90"] = "Rotate 90°",
                ["Volume"] = "Volume",
                ["Mute"] = "Mute",
                ["Topmost"] = "Always on Top",
                ["WallpaperMode"] = "Wallpaper Mode (Back)",
                ["LockAspectRatio"] = "Lock Aspect Ratio",
                ["Slideshow"] = "Slideshow (Folder)",
                ["Interval"] = "Interval",
                ["Seconds"] = "Seconds",
                ["EditModeActive"] = "Edit mode is active! You can drag the media with your mouse. Don't forget to lock it again when you're done.",
                ["InitError"] = "Initialization error: ",
                ["MediaLoadError"] = "Media loading error: ",
                ["SelectMediaTitle"] = "Select Media (Video, GIF or Image)",
                ["AllMediaFiles"] = "All Media Files",
                ["SearchGiphy"] = "Search Giphy",
                ["GiphyApiKey"] = "Giphy API Key",
                ["EnterGiphyKey"] = "Enter Giphy API Key",
                ["Search"] = "Search",
                ["Trending"] = "Trending",
                ["NoResults"] = "No results found.",
                ["Downloading"] = "Downloading..."
            },
            ["tr"] = new()
            {
                ["ChangeMedia"] = "Medyayı Değiştir",
                ["Settings"] = "Ayarlar",
                ["Save"] = "Kaydet",
                ["Close"] = "Kapat",
                ["ResetDefaults"] = "Varsayılana Sıfırla",
                ["Opacity"] = "Saydamlık",
                ["Size"] = "Boyut",
                ["Small"] = "Küçük (200x200)",
                ["Medium"] = "Orta (300x300)",
                ["Large"] = "Büyük (400x400)",
                ["Huge"] = "Dev (600x600)",
                ["Unlock"] = "Konumu Değiştir (Kilidi Aç)",
                ["RunAtStartup"] = "Başlangıçta Çalıştır",
                ["Stop"] = "Durdur",
                ["Start"] = "Başlat",
                ["Exit"] = "Kapat",
                ["Language"] = "Dil / Language",
                ["Rotate90"] = "90° Döndür",
                ["Volume"] = "Ses Seviyesi",
                ["Mute"] = "Sesi Kapat",
                ["Topmost"] = "Her Zaman Üstte",
                ["WallpaperMode"] = "Arka Plan Modu (En Alta Al)",
                ["LockAspectRatio"] = "En-Boy Oranını Koru",
                ["Slideshow"] = "Slayt Gösterisi (Klasör)",
                ["Interval"] = "Geçiş Süresi",
                ["Seconds"] = "Saniye",
                ["EditModeActive"] = "Düzenleme modu aktif! Medyayı farenizle sürükleyebilirsiniz. İşiniz bitince tekrar kilitlemeyi unutmayın.",
                ["InitError"] = "Başlatma hatası: ",
                ["MediaLoadError"] = "Medya yükleme hatası: ",
                ["SelectMediaTitle"] = "Medya Seç (Video, GIF veya Resim)",
                ["AllMediaFiles"] = "Tüm Medya Dosyaları",
                ["SearchGiphy"] = "Giphy'de Ara",
                ["GiphyApiKey"] = "Giphy API Anahtarı",
                ["EnterGiphyKey"] = "Giphy API Anahtarını Girin",
                ["Search"] = "Ara",
                ["Trending"] = "Trendler",
                ["NoResults"] = "Sonuç bulunamadı.",
                ["Downloading"] = "İndiriliyor..."
            }
        };

        public static void SetLanguage(string lang)
        {
            if (Translations.ContainsKey(lang))
                _currentLanguage = lang;
        }

        public static string Get(string key)
        {
            if (Translations[_currentLanguage].TryGetValue(key, out var translation))
                return translation;
            return key;
        }
    }
}
