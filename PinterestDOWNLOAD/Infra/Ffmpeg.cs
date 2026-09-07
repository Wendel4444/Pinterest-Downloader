using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PinterestDOWNLOAD.Scraping;

namespace PinterestDOWNLOAD.Infra
{
    /// <summary>
    /// Localiza (ou baixa uma vez) o ffmpeg, usado para baixar videos que o Pinterest
    /// so serve como HLS (.m3u8).
    /// </summary>
    internal static class Ffmpeg
    {
        private const string UrlDownload =
            "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

        private static string PastaLocal => Path.Combine(AppPaths.RootDir, "ffmpeg");
        private static string ExeLocal => Path.Combine(PastaLocal, "ffmpeg.exe");

        /// <summary>Caminho de um ffmpeg utilizavel, ou <c>null</c> se nao houver.</summary>
        public static string? Localizar()
        {
            if (File.Exists(ExeLocal))
                return ExeLocal;

            // ffmpeg no PATH?
            try
            {
                using var p = Process.Start(new ProcessStartInfo("ffmpeg", "-version")
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
                        return "ffmpeg";
                }
            }
            catch { /* nao esta no PATH */ }

            return null;
        }

        /// <summary>
        /// Garante um ffmpeg disponivel: usa o que existir ou baixa o build do BtbN
        /// para %LOCALAPPDATA%\PinterestDOWNLOAD\ffmpeg\. Retorna o caminho ou <c>null</c>.
        /// </summary>
        public static async Task<string?> GarantirAsync(
            RunLogger log, IProgress<StatusUpdate> progresso, CancellationToken token)
        {
            string? existente = Localizar();
            if (existente is not null)
            {
                log.Info($"ffmpeg encontrado: {existente}");
                return existente;
            }

            log.Info("ffmpeg nao encontrado — baixando uma vez (~80 MB) do BtbN/FFmpeg-Builds...");
            progresso.Report(new StatusUpdate("Baixando o ffmpeg (uma vez so, ~80 MB)..."));

            Directory.CreateDirectory(PastaLocal);
            string zipTmp = Path.Combine(PastaLocal, "ffmpeg-download.zip");

            try
            {
                using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) })
                using (var resp = await http.GetAsync(UrlDownload, HttpCompletionOption.ResponseHeadersRead, token))
                {
                    resp.EnsureSuccessStatusCode();
                    long? total = resp.Content.Headers.ContentLength;

                    await using var origem = await resp.Content.ReadAsStreamAsync(token);
                    await using var arquivo = new FileStream(zipTmp, FileMode.Create, FileAccess.Write, FileShare.None);
                    var buffer = new byte[1 << 16];
                    long baixado = 0;
                    int lido;
                    while ((lido = await origem.ReadAsync(buffer, token)) > 0)
                    {
                        await arquivo.WriteAsync(buffer.AsMemory(0, lido), token);
                        baixado += lido;
                        if (total is > 0)
                            progresso.Report(new StatusUpdate(
                                $"Baixando ffmpeg... {baixado / 1_048_576} / {total / 1_048_576} MB",
                                (int)(baixado / 1024), (int)(total.Value / 1024)));
                    }
                }

                progresso.Report(new StatusUpdate("Extraindo o ffmpeg..."));
                using (var zip = ZipFile.OpenRead(zipTmp))
                {
                    foreach (string alvo in new[] { "ffmpeg.exe", "ffprobe.exe" })
                    {
                        var entrada = zip.Entries.FirstOrDefault(e =>
                            e.FullName.EndsWith("/bin/" + alvo, StringComparison.OrdinalIgnoreCase));
                        if (entrada is not null)
                            entrada.ExtractToFile(Path.Combine(PastaLocal, alvo), overwrite: true);
                    }
                }
                File.Delete(zipTmp);

                if (File.Exists(ExeLocal))
                {
                    log.Info($"ffmpeg instalado em {ExeLocal}");
                    return ExeLocal;
                }
                log.Erro("Baixou o zip mas nao achei ffmpeg.exe dentro dele.");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.Erro($"Falha ao obter o ffmpeg: {ex.Message}. " +
                         "Instale o ffmpeg manualmente e coloque no PATH, ou " +
                         $"copie ffmpeg.exe para {PastaLocal}.");
            }

            return null;
        }
    }
}
