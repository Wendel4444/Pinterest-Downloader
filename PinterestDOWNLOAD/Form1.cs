using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace PinterestDOWNLOAD
{
    public partial class Form1 : Form
    {
        // Pega qualquer tamanho de miniatura do Pinterest (236x, 474x, 564x, 170x170, etc),
        // não só os 3 tamanhos fixos que o código original tratava.
        private static readonly Regex RegexTamanhoImagem = new Regex(@"/\d+x\d*/", RegexOptions.Compiled);

        public Form1()
        {
            InitializeComponent();
        }

        private void btnSelecionarPasta_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fdb = new FolderBrowserDialog())
            {
                if (fdb.ShowDialog() == DialogResult.OK)
                {
                    txtPasta.Text = fdb.SelectedPath;
                }
            }
        }

        private async void btnBaixar_Click(object sender, EventArgs e)
        {
            string url = txtUrl.Text;
            string pastaDestino = txtPasta.Text;

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(pastaDestino))
            {
                MessageBox.Show("Por favor, coloque o link e escolha a pasta!");
                return;
            }

            lblStatus.Text = "Status: Abrindo navegador...";
            btnBaixar.Enabled = false;

            try
            {
                await Task.Run(() => ExtrairEbaixarMidias(url, pastaDestino));
                lblStatus.Text = "Status: Concluído! Todas as mídias foram baixadas.";
            }
            catch (Exception ex)
            {
                // Antes, se algo desse errado aqui (ChromeDriver não encontrado, versão
                // do Chrome incompatível, etc.) o erro simplesmente sumia e a tela ficava
                // travada sem nenhuma explicação. Agora ele aparece pra você.
                lblStatus.Text = "Status: Erro! Veja a mensagem.";
                MessageBox.Show($"Deu erro durante o processo:\n\n{ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBaixar.Enabled = true;
            }
        }

        private void ExtrairEbaixarMidias(string url, string pastaDestino)
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--disable-notifications");
            options.AddArgument("--start-maximized");

            // Perfil Portátil
            string caminhoApp = AppDomain.CurrentDomain.BaseDirectory;
            string perfilPortatil = Path.Combine(caminhoApp, "PerfilPinterest");
            options.AddArgument($"--user-data-dir={perfilPortatil}");

            HashSet<string> linksDasMidias = new HashSet<string>();

            using (IWebDriver driver = new ChromeDriver(options))
            {
                driver.Navigate().GoToUrl(url);

                // Em vez de torcer pra 45s serem suficientes pra você logar, agora ele
                // espera de verdade: você clica OK quando o board já estiver carregado.
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show(
                        "Se precisar, faça login no Pinterest na janela do Chrome que abriu.\n\n" +
                        "Quando o board estiver carregado na tela, clique OK para começar a rolagem.",
                        "Aguardando", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });

                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                long alturaAnterior = (long)js.ExecuteScript("return document.body.scrollHeight");
                int tentativasSemMudanca = 0;
                const int maxTentativasSemMudanca = 3; // dá algumas chances antes de considerar "acabou"

                while (tentativasSemMudanca < maxTentativasSemMudanca)
                {
                    js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                    Thread.Sleep(3000);

                    ColetarMidiasDaTela(driver, linksDasMidias);

                    this.Invoke((MethodInvoker)delegate
                    {
                        lblStatus.Text = $"Status: rolando a página... {linksDasMidias.Count} mídias encontradas até agora.";
                    });

                    long novaAltura = (long)js.ExecuteScript("return document.body.scrollHeight");
                    if (novaAltura == alturaAnterior)
                    {
                        // O Pinterest às vezes demora pra carregar o próximo lote de pins.
                        // Em vez de parar na primeira vez que a altura não muda, insiste um pouco.
                        tentativasSemMudanca++;
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        tentativasSemMudanca = 0;
                    }
                    alturaAnterior = novaAltura;
                }

                // Última coleta, já que o scroll final pode ter carregado itens novos
                ColetarMidiasDaTela(driver, linksDasMidias);

                this.Invoke((MethodInvoker)delegate
                {
                    lblStatus.Text = $"Status: {linksDasMidias.Count} mídias encontradas. Baixando...";
                });

                BaixarDeVerdade(linksDasMidias, pastaDestino);
            }
        }

        private void ColetarMidiasDaTela(IWebDriver driver, HashSet<string> linksDasMidias)
        {
            // 1. CAÇA AS IMAGENS
            var imagensNaTela = driver.FindElements(By.TagName("img"));
            foreach (var img in imagensNaTela)
            {
                string link = img.GetAttribute("src");
                if (!string.IsNullOrEmpty(link) && link.Contains("pinimg.com"))
                {
                    string linkAltaQualidade = RegexTamanhoImagem.Replace(link, "/originals/");
                    linksDasMidias.Add(linkAltaQualidade);
                }
            }

            // 2. CAÇA OS VÍDEOS — olha tanto o <video> quanto as <source> dentro dele,
            // porque em vários pins o src real fica só na tag <source>, não na <video>.
            var videosNaTela = driver.FindElements(By.TagName("video"));
            foreach (var video in videosNaTela)
            {
                string linkVid = video.GetAttribute("src");
                if (!string.IsNullOrEmpty(linkVid) && linkVid.StartsWith("http"))
                {
                    linksDasMidias.Add(linkVid);
                }

                var sources = video.FindElements(By.TagName("source"));
                foreach (var source in sources)
                {
                    string linkSource = source.GetAttribute("src");
                    if (!string.IsNullOrEmpty(linkSource) && linkSource.StartsWith("http"))
                    {
                        linksDasMidias.Add(linkSource);
                    }
                }
            }
        }

        private void BaixarDeVerdade(HashSet<string> links, string pastaDestino)
        {
            using (HttpClient cliente = new HttpClient())
            {
                // Sem User-Agent/Referer, o CDN do Pinterest (pinimg.com) recusa boa parte
                // dos downloads com erro 403 — provável causa de "baixa poucas ou nenhuma mídia".
                cliente.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                cliente.DefaultRequestHeaders.Add("Referer", "https://www.pinterest.com/");
                cliente.Timeout = TimeSpan.FromSeconds(60);

                int contador = 1;   // só sobe quando o arquivo é salvo com sucesso (mantém nomes sem "buracos")
                int processados = 0;
                int falhas = 0;

                foreach (string link in links)
                {
                    processados++;
                    try
                    {
                        byte[] dadosDaMidia = cliente.GetByteArrayAsync(link).Result;

                        string extensao = (link.Contains(".mp4") || link.Contains("/videos/")) ? ".mp4" : ".jpg";
                        string nomeArquivo = $"Referencia_{contador}{extensao}";
                        string caminhoCompleto = Path.Combine(pastaDestino, nomeArquivo);

                        File.WriteAllBytes(caminhoCompleto, dadosDaMidia);

                        this.Invoke((MethodInvoker)delegate
                        {
                            lblStatus.Text = $"Baixando: {processados} de {links.Count} ({falhas} falharam)";
                        });

                        contador++;
                    }
                    catch
                    {
                        // Link quebrado, expirado ou bloqueado — ignora e segue pra próxima
                        falhas++;
                        continue;
                    }
                }
            }
        }
    }
}