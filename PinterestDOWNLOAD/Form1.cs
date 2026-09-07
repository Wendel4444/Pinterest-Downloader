using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PinterestDOWNLOAD.Download;
using PinterestDOWNLOAD.Infra;
using PinterestDOWNLOAD.Scraping;

namespace PinterestDOWNLOAD
{
    public partial class Form1 : Form
    {
        private readonly AppConfig _config = AppConfig.Load();

        private CancellationTokenSource? _cts;
        private TaskCompletionSource<bool>? _loginTcs;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            txtUrl.Text = _config.UltimaUrl ?? string.Empty;
            txtPasta.Text = _config.UltimaPasta ?? string.Empty;
            chkImagens.Checked = _config.BaixarImagens;
            chkVideos.Checked = _config.BaixarVideos;
            numSimultaneos.Value = _config.DownloadsSimultaneos;
            rbPin.Checked = _config.ModoPinUnico;
            rbPasta.Checked = !_config.ModoPinUnico;
            AplicarModo();
        }

        private void ModoChanged(object? sender, EventArgs e) => AplicarModo();

        /// <summary>Ajusta a interface conforme o modo escolhido (board inteiro x um pin).</summary>
        private void AplicarModo()
        {
            bool pin = rbPin.Checked;

            lblUrl.Text = pin ? "URL do pin:" : "URL do board:";
            txtUrl.PlaceholderText = pin
                ? "https://www.pinterest.com/pin/123456789/"
                : "https://www.pinterest.com/usuario/board/";

            // Num pin unico so ha um item — os controles de board nao fazem sentido.
            lblSimultaneos.Visible = !pin;
            numSimultaneos.Visible = !pin;
            btnBaixar.Text = pin ? "Baixar" : "Baixar tudo";
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _cts?.Cancel();
            _loginTcs?.TrySetCanceled();
            SalvarPreferencias();
        }

        private void SalvarPreferencias()
        {
            _config.UltimaUrl = txtUrl.Text.Trim();
            _config.UltimaPasta = txtPasta.Text.Trim();
            _config.BaixarImagens = chkImagens.Checked;
            _config.BaixarVideos = chkVideos.Checked;
            _config.DownloadsSimultaneos = (int)numSimultaneos.Value;
            _config.ModoPinUnico = rbPin.Checked;
            _config.Save();
        }

        private void btnSelecionarPasta_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (Directory.Exists(txtPasta.Text))
                dlg.SelectedPath = txtPasta.Text;

            if (dlg.ShowDialog(this) == DialogResult.OK)
                txtPasta.Text = dlg.SelectedPath;
        }

        private void btnContinuar_Click(object sender, EventArgs e)
        {
            _loginTcs?.TrySetResult(true);
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Cancelando...";
            _cts?.Cancel();
            _loginTcs?.TrySetCanceled();
        }

        private async void btnBaixar_Click(object sender, EventArgs e)
        {
            string url = txtUrl.Text.Trim();
            string pasta = txtPasta.Text.Trim();

            if (!ValidarEntradas(url, ref pasta))
                return;

            txtPasta.Text = pasta;
            SalvarPreferencias();

            _cts = new CancellationTokenSource();
            _loginTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken token = _cts.Token;

            var progresso = new Progress<StatusUpdate>(AtualizarProgresso);
            txtLog.Clear();
            DefinirEstadoRodando(true);

            using var logger = new RunLogger(pasta, EscreverNoLog);
            try
            {
                var opcoes = new OpcoesColeta
                {
                    ColetarImagens = chkImagens.Checked,
                    ColetarVideos = chkVideos.Checked,
                };

                var scraper = new PinterestScraper(logger);
                var midias = await Task.Run(
                    () => scraper.Coletar(url, opcoes, AguardarConfirmacaoLogin, progresso, token), token);

                if (midias.Count == 0)
                {
                    lblStatus.Text = "Nenhuma midia encontrada. Veja o log e _links_encontrados.txt.";
                    return;
                }

                var downloader = new MediaDownloader(logger, (int)numSimultaneos.Value, scraper.CookieHeader);
                var res = await downloader.BaixarTodasAsync(midias, pasta, progresso, token);

                lblStatus.Text = $"Concluido: {res.Baixados} baixados, {res.Pulados} pulados, " +
                                 $"{res.Falhas} falhas. Logs em {Path.Combine(pasta, "_logs")}.";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Cancelado.";
                logger.Aviso("Operacao cancelada pelo usuario.");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Erro: " + ex.Message;
                logger.Erro(ex.ToString());
                MessageBox.Show(this, ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                DefinirEstadoRodando(false);
                _cts.Dispose();
                _cts = null;
                _loginTcs = null;
            }
        }

        /// <summary>Chamado no thread do scraper: libera o botao e espera a confirmacao do usuario.</summary>
        private void AguardarConfirmacaoLogin()
        {
            var tcs = _loginTcs ?? throw new InvalidOperationException("Sessao nao iniciada.");
            var token = _cts?.Token ?? CancellationToken.None;

            BeginInvoke(() =>
            {
                btnContinuar.Enabled = true;
                lblStatus.Text = "Faca login no Pinterest (se necessario) e clique em \"Ja fiz login\".";
            });
            try
            {
                tcs.Task.Wait(token);
            }
            catch (AggregateException ae) when (ae.InnerException is TaskCanceledException)
            {
                throw new OperationCanceledException();
            }
            finally
            {
                BeginInvoke(() => btnContinuar.Enabled = false);
            }
        }

        private bool ValidarEntradas(string url, ref string pasta)
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(pasta))
            {
                MessageBox.Show(this, "Preencha a URL do board e a pasta de destino.",
                    "Faltam dados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                !uri.Host.Contains("pinterest.", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "A URL nao parece ser do Pinterest (board ou pin).",
                    "URL invalida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool ehPin = PinterestScraper.EhUrlDePin(url);
            if (rbPin.Checked && !ehPin)
            {
                MessageBox.Show(this,
                    "Voce escolheu \"Pin unico\", mas a URL parece ser de um board.\n" +
                    "Troque para \"Pasta (board)\" ou cole a URL de um pin (.../pin/123.../).",
                    "Modo x URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (rbPasta.Checked && ehPin)
            {
                MessageBox.Show(this,
                    "Voce escolheu \"Pasta (board)\", mas a URL e de um pin.\n" +
                    "Troque para \"Pin unico\" ou cole a URL de um board.",
                    "Modo x URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!Directory.Exists(pasta))
            {
                var r = MessageBox.Show(this, $"A pasta nao existe:\n{pasta}\n\nCriar agora?",
                    "Pasta inexistente", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return false;
                try
                {
                    Directory.CreateDirectory(pasta);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Nao consegui criar a pasta: " + ex.Message,
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        private void DefinirEstadoRodando(bool rodando)
        {
            rbPasta.Enabled = !rodando;
            rbPin.Enabled = !rodando;
            txtUrl.Enabled = !rodando;
            txtPasta.Enabled = !rodando;
            btnSelecionarPasta.Enabled = !rodando;
            chkImagens.Enabled = !rodando;
            chkVideos.Enabled = !rodando;
            numSimultaneos.Enabled = !rodando;
            btnBaixar.Enabled = !rodando;

            btnCancelar.Enabled = rodando;
            if (!rodando)
            {
                btnContinuar.Enabled = false;
                progressBar.Value = 0;
            }
        }

        private void AtualizarProgresso(StatusUpdate s)
        {
            lblStatus.Text = s.Texto;
            if (s.TemBarra)
            {
                progressBar.Maximum = s.Total;
                progressBar.Value = Math.Clamp(s.Atual, 0, s.Total);
            }
        }

        private void EscreverNoLog(string linha)
        {
            if (!IsHandleCreated) return;
            try
            {
                BeginInvoke(() =>
                {
                    txtLog.AppendText(linha + Environment.NewLine);
                });
            }
            catch (ObjectDisposedException) { /* form fechando */ }
            catch (InvalidOperationException) { /* handle indo embora */ }
        }
    }
}
