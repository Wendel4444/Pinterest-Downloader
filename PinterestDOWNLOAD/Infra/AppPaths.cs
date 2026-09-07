using System;
using System.IO;

namespace PinterestDOWNLOAD.Infra
{
    /// <summary>
    /// Caminhos usados pelo app. Tudo o que precisa sobreviver a um "Clean"
    /// ou a um rebuild fica em %LOCALAPPDATA%, nunca dentro da pasta bin/.
    /// </summary>
    internal static class AppPaths
    {
        public static string RootDir { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PinterestDOWNLOAD");

        /// <summary>Perfil do Chrome (mantem o login do Pinterest entre execucoes).</summary>
        public static string ChromeProfileDir { get; } = Path.Combine(RootDir, "chrome-profile");

        public static string SettingsFile { get; } = Path.Combine(RootDir, "settings.json");

        public static void EnsureCreated()
        {
            Directory.CreateDirectory(RootDir);
            Directory.CreateDirectory(ChromeProfileDir);
        }

        /// <summary>Subpasta de logs/manifesto dentro da pasta de destino escolhida pelo usuario.</summary>
        public static string LogsDirFor(string pastaDestino) => Path.Combine(pastaDestino, "_logs");
    }
}
