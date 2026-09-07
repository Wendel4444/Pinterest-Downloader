# PinterestDOWNLOAD

App WinForms (.NET 10) que baixa as imagens e vídeos de um board público do Pinterest.

## Como funciona

1. Abre o board no Google Chrome via Selenium (o driver é resolvido sozinho pelo
   **Selenium Manager** — não é preciso instalar o ChromeDriver na mão).
2. Você faz login no Pinterest, se necessário, e clica em **"Já fiz login"**.
3. Lê o board inteiro pela API interna do Pinterest (`BoardFeedResource` + seções):
   - **imagens** na resolução original;
   - **vídeos**: a URL da página de cada pin de vídeo.
   - Se a API falhar, cai para rolar o board coletando `<img>` e achando os pins de vídeo.
4. Baixa tudo em paralelo (retry/backoff, extensão pelo `Content-Type`, deduplicação):
   - imagens e `.mp4` diretos via HTTP;
   - vídeos de pin via **yt-dlp** (baixado automaticamente na 1ª vez, ~17 MB);
   - `.m3u8` soltos via **ffmpeg** (baixado automaticamente na 1ª vez, ~80 MB).

## Uso

1. Cole a URL do board (`https://www.pinterest.com/usuario/nome-do-board/`).
2. Escolha a pasta de destino.
3. Marque se quer imagens, vídeos ou os dois, e quantos downloads simultâneos.
4. Clique em **Baixar tudo**, faça login na janela do Chrome e clique em **Já fiz login**.

Arquivos gerados dentro da pasta de destino:

- `Imagem_<hash>.jpg` / `Video_<hash>.mp4` — as mídias;
- `_logs/run_<data>.log` — log da execução;
- `_logs/_links_encontrados.txt` — tudo que foi encontrado (para conferência);
- `_logs/manifest.json` — URLs já baixadas; re-executar o app pula o que já veio.

## Dados do app

Ficam em `%LOCALAPPDATA%\PinterestDOWNLOAD\`:

- `chrome-profile/` — perfil do Chrome (mantém o login entre execuções);
- `settings.json` — última URL/pasta e preferências;
- `ffmpeg/`, `yt-dlp/` — ferramentas baixadas automaticamente na 1ª vez (se já
  tiver ffmpeg/yt-dlp no PATH, usa os do sistema).

## Requisitos

- Windows + .NET 10 SDK
- Google Chrome instalado e atualizado

```
dotnet build
dotnet run --project PinterestDOWNLOAD
```

## Aviso

Baixar boards em massa pode violar os [Termos de Serviço do Pinterest](https://policy.pinterest.com/terms-of-service).
Use para conteúdo próprio / uso pessoal e respeite direitos autorais.
