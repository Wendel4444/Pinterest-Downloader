using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PinterestDOWNLOAD.Scraping;

namespace PinterestDOWNLOAD.Infra
{
    /// <summary>
    /// Localiza (ou baixa uma vez) o yt-dlp, que faz a extracao e o download do video
    /// completo de uma pagina de pin do Pinterest — sem abrir o navegador.
    /// </summary>
    internal static class YtDlp
    {
        private const string UrlDownload =
            "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

        private static string Pasta => Path.Combine(AppPaths.RootDir, "yt-dlp");
        public static string ExeLocal => Path.Combine(Pasta, "yt-dlp.exe");

        public static string? Localizar()
        {
            if (File.Exists(ExeLocal))
                return ExeLocal;

            try
            {
                using var p = Process.Start(new ProcessStartInfo("yt-dlp", "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                if (p is not null)
                {
                    p.WaitForExit(5000);
                    if (p.HasExited && p.ExitCode == 0)
                        return "yt-dlp";
                }
            }
            catch { /* nao esta no PATH */ }

            return null;
        }

        public static async Task<string?> GarantirAsync(
            RunLogger log, IProgress<StatusUpdate> progresso, CancellationToken token)
        {
            string? existente = Localizar();
            if (existente is not null)
            {
                log.Info($"yt-dlp encontrado: {existente}");
                return existente;
            }

            log.Info("yt-dlp nao encontrado — baixando uma vez (~17 MB)...");
            progresso.Report(new StatusUpdate("Baixando o yt-dlp (uma vez so, ~17 MB)..."));
            Directory.CreateDirectory(Pasta);

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                byte[] bytes = await http.GetByteArrayAsync(UrlDownload, token);
                await File.WriteAllBytesAsync(ExeLocal, bytes, token);
                log.Info($"yt-dlp instalado em {ExeLocal}");
                return ExeLocal;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.Erro($"Falha ao baixar o yt-dlp: {ex.Message}. " +
                         $"Baixe yt-dlp.exe manualmente e coloque em {Pasta} (ou no PATH).");
                return null;
            }
        }
    }
}
