using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PinterestDOWNLOAD.Infra
{
    /// <summary>
    /// Preferencias do usuario, persistidas em %LOCALAPPDATA%\PinterestDOWNLOAD\settings.json.
    /// </summary>
    internal sealed class AppConfig
    {
        public string? UltimaUrl { get; set; }
        public string? UltimaPasta { get; set; }
        public bool BaixarImagens { get; set; } = true;
        public bool BaixarVideos { get; set; } = true;

        private int _downloadsSimultaneos = 5;
        public int DownloadsSimultaneos
        {
            get => _downloadsSimultaneos;
            set => _downloadsSimultaneos = Math.Clamp(value, 1, 16);
        }

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(AppPaths.SettingsFile))
                {
                    string json = File.ReadAllText(AppPaths.SettingsFile);
                    return JsonSerializer.Deserialize<AppConfig>(json, JsonOpts) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha ao ler settings.json: {ex.Message}");
            }
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                AppPaths.EnsureCreated();
                File.WriteAllText(AppPaths.SettingsFile, JsonSerializer.Serialize(this, JsonOpts));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha ao salvar settings.json: {ex.Message}");
            }
        }
    }
}
