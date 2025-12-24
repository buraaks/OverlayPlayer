using System;
using System.IO;
using System.Text.Json;

namespace OverlayPlayer.Models
{
    public class AppSettings
    {
        public string? LastFilePath { get; set; }
        public double Opacity { get; set; } = 1.0;
        public double WindowSize { get; set; } = 300;
        public bool IsInteractive { get; set; } = false;
        public bool AutoStart { get; set; } = false;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OverlayPlayer",
            "settings.json"
        );

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
