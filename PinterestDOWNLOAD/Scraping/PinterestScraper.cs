using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using PinterestDOWNLOAD.Infra;

namespace PinterestDOWNLOAD.Scraping
{
    internal sealed class OpcoesColeta
    {
        public bool ColetarImagens { get; init; } = true;
        public bool ColetarVideos { get; init; } = true;
    }

    /// <summary>
    /// Abre o board no Chrome (via Selenium Manager) e coleta as midias.
    ///
    /// Estrategia principal: depois do login, chama a API interna do Pinterest de dentro
    /// da pagina (fetch para /resource/BoardFeedResource/...), paginando o board inteiro.
    /// Traz a imagem original de cada pin e a lista de quais pins sao video.
    ///
    /// Reserva (se a API responder 403 / mudar de formato): rola o board coletando as
    /// &lt;img&gt; e descobre os pins de video passando o mouse.
    ///
    /// Em ambos os casos, cada pin de video vira um <see cref="TipoMidia.VideoPin"/>
    /// (URL da pagina do pin) e quem baixa o video completo e o yt-dlp, no downloader.
    /// </summary>
    internal sealed class PinterestScraper
    {
        // Miniatura do Pinterest: /236x/, /474x/, /564x/, /170x170/, /736x/, /75x75_RS/ ...
        private static readonly Regex RegexTamanhoImagem =
            new(@"/\d+x\d*(?:_[A-Za-z]+)?/", RegexOptions.Compiled);

        // Sub-playlist HLS (variante de resolucao ou faixa de audio) — ficamos so com a master.
        private static readonly Regex RegexSubPlaylistHls =
            new(@"_(?:\d+w|audio)\.m3u8", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const int MaxRolagensSemNovidade = 4;

        private readonly RunLogger _log;

        public PinterestScraper(RunLogger log) => _log = log;

        /// <summary>Cookies da sessao logada, no formato do cabecalho HTTP "Cookie".</summary>
        public string? CookieHeader { get; private set; }

        /// <param name="aguardarConfirmacaoLogin">
        /// Bloqueia ate o usuario confirmar na UI que ja fez login / abriu o board certo.
        /// Deve lancar <see cref="OperationCanceledException"/> se a operacao for cancelada.
        /// </param>
        public IReadOnlyCollection<MediaItem> Coletar(
            string url,
            OpcoesColeta opcoes,
            Action aguardarConfirmacaoLogin,
            IProgress<StatusUpdate> progresso,
            CancellationToken token)
        {
            AppPaths.EnsureCreated();

            var options = new ChromeOptions();
            options.AddArgument("--disable-notifications");
            options.AddArgument("--start-maximized");
            options.AddArgument("--mute-audio");
            options.AddArgument("--autoplay-policy=no-user-gesture-required");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddArgument($"--user-data-dir={AppPaths.ChromeProfileDir}");

            var midias = new HashSet<MediaItem>();
            var urlsVideoVistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ChromeDriver driver;
            try
            {
                driver = new ChromeDriver(options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Nao consegui abrir o Chrome. Verifique se o Google Chrome esta instalado " +
                    "e atualizado. Detalhe: " + ex.Message, ex);
            }

            try
            {
                driver.Navigate().GoToUrl(url);
                _log.Info($"Board aberto: {url}");

                progresso.Report(new StatusUpdate(
                    "Faca login no Pinterest (se necessario) e clique em \"Ja fiz login\"."));
                aguardarConfirmacaoLogin();
                token.ThrowIfCancellationRequested();

                var js = (IJavaScriptExecutor)driver;

                bool apiOk = ColetarViaApi(driver, opcoes, midias, urlsVideoVistas, progresso, token);

                if (!apiOk)
                {
                    _log.Aviso("A coleta via API do Pinterest nao funcionou; usando rolagem no board.");
                    RolarEColetarDoDom(driver, js, opcoes, midias, urlsVideoVistas, progresso, token);
                }

                CookieHeader = MontarCookieHeader(driver);

                int qtdVideo = midias.Count(m => m.Tipo is TipoMidia.Video or TipoMidia.VideoHls or TipoMidia.VideoPin);
                _log.Info($"Coleta terminada: {midias.Count} midias " +
                          $"({midias.Count(m => m.Tipo == TipoMidia.Imagem)} imagens, {qtdVideo} videos).");

                return midias;
            }
            finally
            {
                try { driver.Quit(); } catch { /* ja pode ter fechado */ }
                driver.Dispose();
            }
        }

        // ----------------------------------------------------------------------
        //  Metodo principal: API interna do Pinterest
        // ----------------------------------------------------------------------

        private bool ColetarViaApi(
            IWebDriver driver,
            OpcoesColeta opcoes,
            HashSet<MediaItem> midias,
            HashSet<string> urlsVideoVistas,
            IProgress<StatusUpdate> progresso,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            progresso.Report(new StatusUpdate("Lendo o board pela API do Pinterest..."));

            var js = (IJavaScriptExecutor)driver;
            try { driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(10); }
            catch { /* ignore */ }

            object? bruto;
            try
            {
                bruto = js.ExecuteAsyncScript(ScriptApiBoard);
            }
            catch (Exception ex)
            {
                _log.Aviso($"API: erro ao executar o script ({ex.Message}).");
                return false;
            }

            if (bruto is not Dictionary<string, object> res)
            {
                _log.Aviso("API: resposta em formato inesperado.");
                return false;
            }

            foreach (var linha in LerLista(res, "log").OfType<string>())
                _log.Aviso("API: " + linha);

            if (res.TryGetValue("error", out var err))
            {
                _log.Aviso($"API: {err}");
                return false;
            }

            var idsVideo = LerLista(res, "videoPinIds").OfType<string>().ToList();

            string origem;
            try { origem = new Uri(driver.Url).GetLeftPart(UriPartial.Authority); }
            catch { origem = "https://www.pinterest.com"; }

            int vidPins = 0;
            if (opcoes.ColetarVideos)
                foreach (var id in idsVideo)
                    if (midias.Add(new MediaItem(TipoMidia.VideoPin, $"{origem}/pin/{id}/")))
                        vidPins++;

            _log.Info($"API: boardId={res.GetValueOrDefault("boardId")}, " +
                      $"paginas={res.GetValueOrDefault("pages")}, pins de video={idsVideo.Count}, " +
                      $"itens brutos={res.GetValueOrDefault("count")}");

            int imgs = 0;
            foreach (var item in LerLista(res, "items").OfType<Dictionary<string, object>>())
            {
                string tipo = item.GetValueOrDefault("type")?.ToString() ?? "";
                string? u = item.GetValueOrDefault("url")?.ToString();
                if (string.IsNullOrWhiteSpace(u)) continue;

                if (tipo == "image" && opcoes.ColetarImagens && midias.Add(new MediaItem(TipoMidia.Imagem, u)))
                    imgs++;
                else if ((tipo is "video" or "hls") && opcoes.ColetarVideos)
                    RegistrarVideo(u, midias, urlsVideoVistas); // pins sem id: usa o palpite do feed
            }

            _log.Info($"API coletou {imgs} imagens e {vidPins} pins de video.");
            return imgs > 0 || vidPins > 0 || midias.Count > 0;
        }

        private static IEnumerable<object> LerLista(Dictionary<string, object> dic, string chave) =>
            dic.TryGetValue(chave, out var v) && v is IEnumerable<object> col
                ? col
                : Enumerable.Empty<object>();

        // JS rodado dentro da pagina do Pinterest (usuario ja logado).
        private const string ScriptApiBoard = """
            const done = arguments[arguments.length - 1];
            (async () => {
              const out = [];
              const seen = new Set();
              const log = [];
              const videoPinIds = new Set(); // ids dos pins que sao video (resolvidos depois, pin a pin)
              let pages = 0;
              let appVersion = '';

              function pushImg(images) {
                if (!images) return;
                const b = images.orig || images['1200x'] || images['736x'] || images['564x'] || Object.values(images)[0];
                if (b && b.url && !seen.has(b.url)) { seen.add(b.url); out.push({ type: 'image', url: b.url }); }
              }
              function pushMedia(v) {
                if (!v || !v.url || seen.has(v.url)) return;
                seen.add(v.url);
                out.push({ type: v.kind === 'hls' ? 'hls' : 'video', url: v.url });
              }
              // Escolhe a melhor faixa de um video_list: mp4 "mc" > qualquer mp4 > hls master nao-iht > hls.
              function bestFromVideoList(vl) {
                if (!vl) return null;
                const arr = Object.values(vl).filter(v => v && v.url);
                const isIht = u => /\/videos\/iht\//i.test(u);
                const isSub = u => /_(?:\d+w|audio)\.m3u8/i.test(u);
                const mp4mc = arr.filter(v => /\.mp4(\?|$)/i.test(v.url) && !isIht(v.url))
                                 .sort((a, b) => (b.width || 0) - (a.width || 0))[0];
                if (mp4mc) return { url: mp4mc.url, kind: 'mp4' };
                const hlsFull = arr.find(v => /\.m3u8/i.test(v.url) && !isSub(v.url) && !isIht(v.url));
                if (hlsFull) return { url: hlsFull.url, kind: 'hls' };
                const mp4any = arr.filter(v => /\.mp4(\?|$)/i.test(v.url))
                                  .sort((a, b) => (b.width || 0) - (a.width || 0))[0];
                if (mp4any) return { url: mp4any.url, kind: 'mp4' };
                const hlsAny = arr.find(v => /\.m3u8/i.test(v.url) && !isSub(v.url));
                return hlsAny ? { url: hlsAny.url, kind: 'hls' } : null;
              }
              function collectVideoFromPin(pin) {
                const cands = [];
                if (pin.videos) { const b = bestFromVideoList(pin.videos.video_list); if (b) cands.push(b); }
                const sp = pin.story_pin_data;
                if (sp && Array.isArray(sp.pages))
                  for (const pg of sp.pages)
                    for (const blk of (pg.blocks || []))
                      if (blk && blk.video) { const b = bestFromVideoList(blk.video.video_list); if (b) cands.push(b); }
                return cands;
              }
              function pushPin(pin) {
                if (!pin || typeof pin !== 'object') return;
                const vids = collectVideoFromPin(pin);
                if (vids.length) {
                  if (pin.id) videoPinIds.add(String(pin.id));
                  else vids.forEach(pushMedia); // sem id: usa o palpite do feed mesmo
                }
                const sp = pin.story_pin_data;
                if (sp && Array.isArray(sp.pages)) {
                  for (const pg of sp.pages) {
                    for (const blk of (pg.blocks || []))
                      if (blk && blk.image && blk.image.images) pushImg(blk.image.images);
                    if (pg.image && pg.image.images) pushImg(pg.image.images);
                  }
                }
                const cs = pin.carousel_data && pin.carousel_data.carousel_slots;
                if (Array.isArray(cs)) for (const slot of cs) pushImg(slot.images);
                pushImg(pin.images);
              }

              function cookie(nome) {
                const m = document.cookie.match(new RegExp('(?:^|; )' + nome + '=([^;]*)'));
                return m ? decodeURIComponent(m[1]) : '';
              }

              async function resource(name, options) {
                const params = new URLSearchParams();
                params.set('source_url', location.pathname);
                params.set('data', JSON.stringify({ options, context: {} }));
                const r = await fetch('/resource/' + name + '/get/?' + params.toString(), {
                  method: 'GET',
                  headers: {
                    'x-requested-with': 'XMLHttpRequest',
                    'x-app-version': appVersion,
                    'x-pinterest-appstate': 'active',
                    'x-pinterest-source-url': location.pathname,
                    'x-pinterest-pws-handler': 'www/[username]/[slug].js',
                    'x-csrftoken': cookie('csrftoken') || '1234',
                    'accept': 'application/json, text/javascript, */*; q=0.01'
                  },
                  credentials: 'include'
                });
                if (!r.ok) throw new Error(name + ' HTTP ' + r.status);
                return r.json();
              }

              async function feed(name, baseOptions) {
                let bookmark = null;
                for (let i = 0; i < 800; i++) {
                  pages++;
                  const options = Object.assign({}, baseOptions);
                  if (bookmark) options.bookmarks = [bookmark];
                  let j;
                  try { j = await resource(name, options); }
                  catch (e) { log.push(name + ': ' + e); break; }
                  const rr = j.resource_response || {};
                  const data = rr.data || [];
                  const list = Array.isArray(data) ? data : (data.pins || []);
                  for (const pin of list) pushPin(pin);
                  bookmark = rr.bookmark ||
                    (j.resource && j.resource.options && j.resource.options.bookmarks && j.resource.options.bookmarks[0]) ||
                    null;
                  if (!bookmark || bookmark === '-end-') break;
                }
              }

              try {
                const html = document.documentElement.innerHTML;
                let pws = null;
                try { const el = document.getElementById('__PWS_DATA__'); if (el) pws = JSON.parse(el.textContent); } catch (e) { log.push('PWS parse: ' + e); }

                const mv = html.match(/"app_version"\s*:\s*"([^"]+)"/);
                if (mv) appVersion = mv[1];

                let boardId = null;
                try {
                  const rs = pws && pws.props && pws.props.initialReduxState;
                  if (rs && rs.boards) boardId = Object.keys(rs.boards).find(k => /^\d+$/.test(k));
                } catch (e) { log.push('boardId PWS: ' + e); }
                if (!boardId) {
                  const mb = html.match(/"board_id"\s*:\s*"(\d+)"/) || html.match(/\\"id\\":\\"(\d{6,})\\"[^}]*\\"type\\":\\"board\\"/);
                  if (mb) boardId = mb[1];
                }

                const m = location.pathname.match(/^\/([^\/]+)\/([^\/]+)/);
                const username = m ? decodeURIComponent(m[1]) : '';
                const slug = m ? decodeURIComponent(m[2]) : '';

                if (!boardId && username && slug) {
                  for (const fsk of ['detailed', 'board_page', null]) {
                    try {
                      const opt = { username, slug };
                      if (fsk) opt.field_set_key = fsk;
                      const b = await resource('BoardResource', opt);
                      boardId = b.resource_response && b.resource_response.data && b.resource_response.data.id;
                      if (boardId) break;
                    } catch (e) { log.push('BoardResource(' + fsk + '): ' + e); }
                  }
                }
                if (!boardId) { done({ error: 'nao consegui o id do board', log: log }); return; }

                await feed('BoardFeedResource', { board_id: boardId, page_size: 25 });

                try {
                  const s = await resource('BoardSectionsResource', { board_id: boardId });
                  const secs = (s.resource_response && s.resource_response.data) || [];
                  for (const sec of secs)
                    await feed('BoardSectionPinsResource', { section_id: sec.id, page_size: 25 });
                } catch (e) { log.push('secoes: ' + e); }

                done({ ok: true, boardId: String(boardId), pages: pages,
                       videoPinIds: Array.from(videoPinIds), count: out.length, items: out, log: log });
              } catch (e) {
                done({ error: String((e && e.stack) || e), log: log });
              }
            })();
            """;

        // ----------------------------------------------------------------------
        //  Reserva: rola o board coletando <img> e descobrindo os pins de video
        // ----------------------------------------------------------------------

        private void RolarEColetarDoDom(
            IWebDriver driver,
            IJavaScriptExecutor js,
            OpcoesColeta opcoes,
            HashSet<MediaItem> midias,
            HashSet<string> urlsVideoVistas,
            IProgress<StatusUpdate> progresso,
            CancellationToken token)
        {
            long alturaAnterior = ObterAltura(js);
            int rolagensSemNovidade = 0;
            int totalAnterior = 0;

            while (rolagensSemNovidade < MaxRolagensSemNovidade)
            {
                token.ThrowIfCancellationRequested();

                js.ExecuteScript("window.scrollBy(0, window.innerHeight * 0.85);");
                Aguardar(1500, token);

                if (opcoes.ColetarImagens)
                    ColetarImagensDoDom(driver, midias);

                if (midias.Count != totalAnterior)
                {
                    totalAnterior = midias.Count;
                    progresso.Report(new StatusUpdate($"Rolando... {midias.Count} imagens encontradas."));
                }

                long novaAltura = ObterAltura(js);
                bool noFim = js.ExecuteScript(
                    "return (window.innerHeight + window.scrollY) >= document.body.scrollHeight - 50;") is true;

                if (novaAltura == alturaAnterior && noFim)
                {
                    rolagensSemNovidade++;
                    Aguardar(2500, token);
                }
                else
                {
                    rolagensSemNovidade = 0;
                }
                alturaAnterior = novaAltura;
            }

            if (opcoes.ColetarImagens)
                ColetarImagensDoDom(driver, midias);

            if (opcoes.ColetarVideos)
            {
                var pinsDeVideo = IdentificarPinsDeVideo(driver, js, progresso, token);
                _log.Info($"{pinsDeVideo.Count} pins de video localizados.");
                foreach (var pinUrl in pinsDeVideo)
                    midias.Add(new MediaItem(TipoMidia.VideoPin, pinUrl));
            }
        }

        private List<string> IdentificarPinsDeVideo(
            IWebDriver driver, IJavaScriptExecutor js, IProgress<StatusUpdate> progresso, CancellationToken token)
        {
            progresso.Report(new StatusUpdate("Localizando os pins de video (passando o mouse)..."));
            js.ExecuteScript("window.scrollTo(0, 0);");
            Aguardar(1200, token);

            var actions = new Actions(driver);
            var pinsHoverados = new HashSet<string>();
            var pinsDeVideo = new HashSet<string>();
            int passesSemNovidade = 0;

            while (passesSemNovidade < MaxRolagensSemNovidade)
            {
                token.ThrowIfCancellationRequested();
                int antes = pinsHoverados.Count;

                foreach (var pin in driver.FindElements(By.CssSelector("a[href*='/pin/']")))
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        string? href = pin.GetDomProperty("href");
                        if (string.IsNullOrEmpty(href) || !pinsHoverados.Add(href) || !pin.Displayed)
                            continue;
                        actions.MoveToElement(pin).Perform();
                    }
                    catch
                    {
                        continue; // stale / fora da viewport
                    }
                    Aguardar(140, token);

                    foreach (var v in driver.FindElements(By.TagName("video")))
                    {
                        try
                        {
                            var a = v.FindElement(By.XPath("ancestor::a[contains(@href,'/pin/')][1]"));
                            string? h = a.GetDomProperty("href");
                            if (!string.IsNullOrEmpty(h))
                                pinsDeVideo.Add(SemQuery(h));
                        }
                        catch
                        {
                            /* video sem <a> ancestral com /pin/ */
                        }
                    }
                }

                progresso.Report(new StatusUpdate($"Localizando pins de video... {pinsDeVideo.Count} ate agora."));
                js.ExecuteScript("window.scrollBy(0, window.innerHeight * 0.7);");
                Aguardar(1000, token);

                bool noFim = js.ExecuteScript(
                    "return (window.innerHeight + window.scrollY) >= document.body.scrollHeight - 50;") is true;

                if (pinsHoverados.Count == antes && noFim)
                    passesSemNovidade++;
                else
                    passesSemNovidade = 0;
            }

            return pinsDeVideo.ToList();
        }

        private void RegistrarVideo(string? url, HashSet<MediaItem> destino, HashSet<string> vistas)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return;
            if (!url.Contains("pinimg.com", StringComparison.OrdinalIgnoreCase))
                return;

            if (url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase))
            {
                if (RegexSubPlaylistHls.IsMatch(url)) return;
                if (!vistas.Add(url)) return;
                destino.Add(new MediaItem(TipoMidia.VideoHls, url));
                return;
            }

            if (!vistas.Add(url)) return;
            destino.Add(new MediaItem(TipoMidia.Video, url));
        }

        // ----------------------------------------------------------------------
        //  Auxiliares
        // ----------------------------------------------------------------------

        private static string SemQuery(string url)
        {
            int q = url.IndexOf('?');
            return q >= 0 ? url[..q] : url;
        }

        private static string? MontarCookieHeader(IWebDriver driver)
        {
            try
            {
                var cookies = driver.Manage().Cookies.AllCookies;
                return cookies.Count == 0
                    ? null
                    : string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
            }
            catch
            {
                return null;
            }
        }

        private static long ObterAltura(IJavaScriptExecutor js) =>
            Convert.ToInt64(js.ExecuteScript("return document.body.scrollHeight") ?? 0L);

        private static void Aguardar(int ms, CancellationToken token)
        {
            if (token.WaitHandle.WaitOne(ms))
                token.ThrowIfCancellationRequested();
        }

        private void ColetarImagensDoDom(IWebDriver driver, HashSet<MediaItem> destino)
        {
            foreach (var img in driver.FindElements(By.TagName("img")))
            {
                string? melhor;
                try
                {
                    melhor = MelhorUrlDaImagem(
                        img.GetDomAttribute("srcset"),
                        img.GetDomProperty("currentSrc") ?? img.GetDomProperty("src"));
                }
                catch (StaleElementReferenceException)
                {
                    continue;
                }

                if (melhor is null || !melhor.Contains("pinimg.com"))
                    continue;

                string originals = RegexTamanhoImagem.Replace(melhor, "/originals/");
                destino.Add(new MediaItem(TipoMidia.Imagem, originals,
                    UrlFallback: originals != melhor ? melhor : null));
            }
        }

        /// <summary>Pega a URL de maior largura do srcset; cai para o src quando nao ha srcset.</summary>
        internal static string? MelhorUrlDaImagem(string? srcset, string? src)
        {
            if (!string.IsNullOrWhiteSpace(srcset))
            {
                string? melhor = null;
                int melhorLargura = -1;
                foreach (string parte in srcset.Split(','))
                {
                    string[] campos = parte.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (campos.Length == 0) continue;

                    int largura = 0;
                    if (campos.Length > 1 && campos[1].EndsWith('w'))
                        int.TryParse(campos[1][..^1], out largura);

                    if (largura >= melhorLargura)
                    {
                        melhorLargura = largura;
                        melhor = campos[0];
                    }
                }
                if (melhor is not null) return melhor;
            }
            return string.IsNullOrWhiteSpace(src) ? null : src;
        }
    }
}
