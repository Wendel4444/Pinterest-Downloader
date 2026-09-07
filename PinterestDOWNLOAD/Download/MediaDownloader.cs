using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PinterestDOWNLOAD.Infra;
using PinterestDOWNLOAD.Scraping;

namespace PinterestDOWNLOAD.Download
{
    internal sealed record ResultadoDownload(int Baixados, int Pulados, int Falhas);

    /// <summary>
    /// Baixa as midias em paralelo (limitado), com retry/backoff, deteccao de extensao
    /// pelo Content-Type, deduplicacao por manifesto e "pular o que ja existe".
    /// </summary>
    internal sealed class MediaDownloader
    {
        private static readonly HttpClient Http = CriarHttpClient();

        private const int MaxTentativas = 3;
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
            "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

        private readonly RunLogger _log;
        private readonly int _paralelismo;
        private readonly string? _cookieHeader;
        private string? _ffmpegPath;
        private string? _ytDlpPath;

        public MediaDownloader(RunLogger log, int paralelismo, string? cookieHeader = null)
        {
            _log = log;
            _paralelismo = Math.Clamp(paralelismo, 1, 16);
            _cookieHeader = string.IsNullOrWhiteSpace(cookieHeader) ? null : cookieHeader;
        }

        private static HttpClient CriarHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                MaxConnectionsPerServer = 32,
                UseCookies = false, // enviamos o cabecalho Cookie manualmente
            };
            var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(100) };
            http.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            http.DefaultRequestHeaders.Add("Referer", "https://www.pinterest.com/");
            return http;
        }

        public async Task<ResultadoDownload> BaixarTodasAsync(
            IReadOnlyCollection<MediaItem> midias,
            string pastaDestino,
            IProgress<StatusUpdate> progresso,
            CancellationToken token)
        {
            Directory.CreateDirectory(pastaDestino);
            var manifesto = Manifesto.Carregar(pastaDestino);

            // Grava a lista crua encontrada — ajuda a separar "nao achou" de "nao baixou".
            try
            {
                await File.WriteAllLinesAsync(
                    Path.Combine(AppPaths.LogsDirFor(pastaDestino), "_links_encontrados.txt"),
                    midias.Select(m => $"{m.Tipo}\t{m.Url}"), token);
            }
            catch (Exception ex) { _log.Aviso($"Nao gravou _links_encontrados.txt: {ex.Message}"); }

            bool temPin = midias.Any(m => m.Tipo == TipoMidia.VideoPin);
            bool temHls = midias.Any(m => m.Tipo == TipoMidia.VideoHls);

            if (temHls || temPin)
            {
                _ffmpegPath = await Ffmpeg.GarantirAsync(_log, progresso, token);
                if (_ffmpegPath is null)
                    _log.Aviso("Sem ffmpeg: videos HLS / com audio separado podem falhar.");
            }
            if (temPin)
            {
                _ytDlpPath = await YtDlp.GarantirAsync(_log, progresso, token);
                if (_ytDlpPath is null)
                    _log.Aviso("Sem yt-dlp: os videos de pin nao serao baixados.");
            }

            int baixados = 0, pulados = 0, falhas = 0, processados = 0;
            int total = midias.Count;
            using var limite = new SemaphoreSlim(_paralelismo);
            var novasNoManifesto = new ConcurrentBag<string>();

            var tarefas = midias.Select(async midia =>
            {
                await limite.WaitAsync(token);
                try
                {
                    if (manifesto.Contem(midia.Url))
                    {
                        Interlocked.Increment(ref pulados);
                        return;
                    }

                    var resultado = await BaixarUmaAsync(midia, pastaDestino, token);
                    switch (resultado)
                    {
                        case DownloadUnitario.Baixado:
                            Interlocked.Increment(ref baixados);
                            novasNoManifesto.Add(midia.Url);
                            break;
                        case DownloadUnitario.JaExistia:
                            Interlocked.Increment(ref pulados);
                            novasNoManifesto.Add(midia.Url);
                            break;
                        default:
                            Interlocked.Increment(ref falhas);
                            break;
                    }
                }
                finally
                {
                    int feitos = Interlocked.Increment(ref processados);
                    progresso.Report(new StatusUpdate(
                        $"Baixando {feitos}/{total}  (ok {baixados}, pulados {pulados}, falhas {falhas})",
                        feitos, total));
                    limite.Release();
                }
            });

            try
            {
                await Task.WhenAll(tarefas);
            }
            finally
            {
                manifesto.Adicionar(novasNoManifesto);
                manifesto.Salvar();
            }

            _log.Info($"Download: {baixados} baixados, {pulados} pulados, {falhas} falhas.");
            return new ResultadoDownload(baixados, pulados, falhas);
        }

        private enum DownloadUnitario { Baixado, JaExistia, Falhou }

        private async Task<DownloadUnitario> BaixarUmaAsync(MediaItem midia, string pasta, CancellationToken token)
        {
            if (midia.Tipo == TipoMidia.VideoPin)
                return await BaixarViaYtDlpAsync(midia, pasta, token);
            if (midia.Tipo == TipoMidia.VideoHls)
                return await BaixarHlsAsync(midia, pasta, token);

            string[] candidatas = midia.UrlFallback is { Length: > 0 } fb
                ? new[] { midia.Url, fb }
                : new[] { midia.Url };

            Exception? ultimoErro = null;

            foreach (string url in candidatas)
            {
                for (int tentativa = 1; tentativa <= MaxTentativas; tentativa++)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Get, url);
                        req.Headers.TryAddWithoutValidation("Accept", "*/*");
                        if (_cookieHeader is not null)
                            req.Headers.TryAddWithoutValidation("Cookie", _cookieHeader);
                        if (midia.Tipo == TipoMidia.Video)
                            req.Headers.TryAddWithoutValidation("Range", "bytes=0-");

                        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);

                        if (resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Gone)
                        {
                            _log.Aviso($"{(int)resp.StatusCode} em {url} — tentando fallback se houver.");
                            break; // proxima URL candidata
                        }
                        resp.EnsureSuccessStatusCode();

                        string? mediaType = resp.Content.Headers.ContentType?.MediaType;

                        // Sintoma "video baixou como a capa": pedimos um video e veio uma imagem
                        // ou uma pagina HTML. Nao salva um arquivo enganoso.
                        if (midia.Tipo == TipoMidia.Video && mediaType is not null &&
                            (mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                             mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)))
                        {
                            _log.Aviso($"Video retornou '{mediaType}' (nao e video): {url}");
                            break; // tenta a proxima candidata; se nao houver, conta como falha
                        }

                        string ext = ExtensaoDe(mediaType, url, midia.Tipo);
                        string nome = $"{midia.Tipo}_{HashCurto(midia.Url)}{ext}";
                        string caminho = Path.Combine(pasta, nome);

                        if (File.Exists(caminho))
                        {
                            _log.Info($"Ja existia: {nome}");
                            return DownloadUnitario.JaExistia;
                        }

                        string tmp = caminho + ".part";
                        long bytes = 0;
                        await using (var origem = await resp.Content.ReadAsStreamAsync(token))
                        await using (var arquivo = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await origem.CopyToAsync(arquivo, token);
                            bytes = arquivo.Length;
                        }
                        File.Move(tmp, caminho, overwrite: true);

                        _log.Info($"OK  {nome}  [{mediaType ?? "?"}, {bytes / 1024} KB]  <-  {url}");
                        return DownloadUnitario.Baixado;
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        ultimoErro = ex;
                        if (tentativa < MaxTentativas)
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, tentativa)), token);
                    }
                }
            }

            _log.Erro($"FALHOU  {midia.Url}  |  {ultimoErro?.Message}");
            return DownloadUnitario.Falhou;
        }

        private async Task<DownloadUnitario> BaixarHlsAsync(MediaItem midia, string pasta, CancellationToken token)
        {
            if (_ffmpegPath is null)
            {
                _log.Erro($"FALHOU (sem ffmpeg)  {midia.Url}");
                return DownloadUnitario.Falhou;
            }

            string nome = $"Video_{HashCurto(midia.Url)}.mp4";
            string caminho = Path.Combine(pasta, nome);
            if (File.Exists(caminho))
            {
                _log.Info($"Ja existia: {nome}");
                return DownloadUnitario.JaExistia;
            }

            string tmp = Path.Combine(pasta, nome + ".part.mp4");
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* ignore */ }

            var psi = new ProcessStartInfo(_ffmpegPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            foreach (string arg in new[]
                     {
                         "-y", "-loglevel", "error",
                         "-user_agent", UserAgent,
                         "-headers", "Referer: https://www.pinterest.com/\r\n",
                         "-i", midia.Url,
                         "-c", "copy", "-bsf:a", "aac_adtstoasc",
                         tmp,
                     })
            {
                psi.ArgumentList.Add(arg);
            }

            try
            {
                using var proc = Process.Start(psi)
                    ?? throw new InvalidOperationException("nao consegui iniciar o ffmpeg");
                string erro = await proc.StandardError.ReadToEndAsync(token);
                await proc.WaitForExitAsync(token);

                if (proc.ExitCode == 0 && File.Exists(tmp) && new FileInfo(tmp).Length > 0)
                {
                    File.Move(tmp, caminho, overwrite: true);
                    _log.Info($"OK  {nome}  [HLS via ffmpeg, {new FileInfo(caminho).Length / 1024} KB]  <-  {midia.Url}");
                    return DownloadUnitario.Baixado;
                }

                _log.Erro($"FALHOU (ffmpeg {proc.ExitCode})  {midia.Url}  |  {UmaLinha(erro)}");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* ignore */ }
                throw;
            }
            catch (Exception ex)
            {
                _log.Erro($"FALHOU (ffmpeg)  {midia.Url}  |  {ex.Message}");
            }

            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { /* ignore */ }
            return DownloadUnitario.Falhou;
        }

        private async Task<DownloadUnitario> BaixarViaYtDlpAsync(MediaItem midia, string pasta, CancellationToken token)
        {
            if (_ytDlpPath is null)
            {
                _log.Erro($"FALHOU (sem yt-dlp)  {midia.Url}");
                return DownloadUnitario.Falhou;
            }

            string baseNome = $"Video_{HashCurto(midia.Url)}";
            string alvoMp4 = Path.Combine(pasta, baseNome + ".mp4");
            if (File.Exists(alvoMp4))
            {
                _log.Info($"Ja existia: {baseNome}.mp4");
                return DownloadUnitario.JaExistia;
            }

            // Limpa restos de tentativa anterior.
            foreach (var resto in Directory.EnumerateFiles(pasta, baseNome + ".*"))
                try { File.Delete(resto); } catch { /* ignore */ }

            var psi = new ProcessStartInfo(_ytDlpPath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            void Arg(string a) => psi.ArgumentList.Add(a);
            Arg("--no-playlist");
            Arg("--no-warnings");
            Arg("--no-progress");
            Arg("--quiet");
            Arg("--force-overwrites");
            Arg("-f"); Arg("bv*+ba/b");
            Arg("--merge-output-format"); Arg("mp4");
            Arg("--remux-video"); Arg("mp4");
            if (_ffmpegPath is not null && !_ffmpegPath.Equals("ffmpeg", StringComparison.OrdinalIgnoreCase))
            {
                Arg("--ffmpeg-location"); Arg(_ffmpegPath);
            }
            Arg("--user-agent"); Arg(UserAgent);
            Arg("--add-header"); Arg("Referer:https://www.pinterest.com/");
            if (_cookieHeader is not null)
            {
                Arg("--add-header"); Arg("Cookie:" + _cookieHeader);
            }
            Arg("-o"); Arg(Path.Combine(pasta, baseNome + ".%(ext)s"));
            Arg(midia.Url);

            try
            {
                using var proc = Process.Start(psi)
                    ?? throw new InvalidOperationException("nao consegui iniciar o yt-dlp");
                string erro = await proc.StandardError.ReadToEndAsync(token);
                await proc.WaitForExitAsync(token);

                string? gerado = Directory.EnumerateFiles(pasta, baseNome + ".*")
                    .FirstOrDefault(f => !f.EndsWith(".part") && !f.EndsWith(".ytdl"));

                if (proc.ExitCode == 0 && gerado is not null && new FileInfo(gerado).Length > 0)
                {
                    if (!gerado.Equals(alvoMp4, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Move(gerado, alvoMp4, overwrite: true); gerado = alvoMp4; }
                        catch { /* deixa com a extensao que veio */ }
                    }
                    _log.Info($"OK  {Path.GetFileName(gerado)}  " +
                              $"[yt-dlp, {new FileInfo(gerado).Length / 1024} KB]  <-  {midia.Url}");
                    return DownloadUnitario.Baixado;
                }

                _log.Erro($"FALHOU (yt-dlp {proc.ExitCode})  {midia.Url}  |  {UmaLinha(erro)}");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Erro($"FALHOU (yt-dlp)  {midia.Url}  |  {ex.Message}");
            }

            foreach (var resto in Directory.EnumerateFiles(pasta, baseNome + ".*"))
                try { File.Delete(resto); } catch { /* ignore */ }
            return DownloadUnitario.Falhou;
        }

        private static string UmaLinha(string texto)
        {
            texto = texto.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return texto.Length > 300 ? texto[..300] + "..." : texto;
        }

        internal static string ExtensaoDe(string? contentType, string url, TipoMidia tipo)
        {
            string ext = contentType?.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/svg+xml" => ".svg",
                "video/mp4" => ".mp4",
                "video/webm" => ".webm",
                "video/quicktime" => ".mov",
                _ => ""
            };
            if (ext.Length > 0) return ext;

            string semQuery = url.Split('?')[0];
            string doUrl = Path.GetExtension(semQuery).ToLowerInvariant();
            if (doUrl is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".mp4" or ".webm" or ".mov")
                return doUrl == ".jpeg" ? ".jpg" : doUrl;

            return tipo == TipoMidia.Video ? ".mp4" : ".jpg";
        }

        internal static string HashCurto(string texto)
        {
            byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(texto));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }

        /// <summary>Conjunto de URLs ja baixadas com sucesso, em _logs/manifest.json.</summary>
        private sealed class Manifesto
        {
            private readonly string _caminho;
            private readonly HashSet<string> _urls;

            private Manifesto(string caminho, HashSet<string> urls)
            {
                _caminho = caminho;
                _urls = urls;
            }

            public static Manifesto Carregar(string pastaDestino)
            {
                string dir = AppPaths.LogsDirFor(pastaDestino);
                Directory.CreateDirectory(dir);
                string caminho = Path.Combine(dir, "manifest.json");
                var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    if (File.Exists(caminho))
                    {
                        var lidas = JsonSerializer.Deserialize<string[]>(File.ReadAllText(caminho));
                        if (lidas is not null) urls.UnionWith(lidas);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Manifesto ilegivel: {ex.Message}");
                }
                return new Manifesto(caminho, urls);
            }

            public bool Contem(string url) => _urls.Contains(url);
            public void Adicionar(IEnumerable<string> urls) => _urls.UnionWith(urls);

            public void Salvar()
            {
                try
                {
                    File.WriteAllText(_caminho,
                        JsonSerializer.Serialize(_urls.OrderBy(u => u), new JsonSerializerOptions { WriteIndented = true }));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Nao salvou o manifesto: {ex.Message}");
                }
            }
        }
    }
}
