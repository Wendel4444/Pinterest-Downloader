# 📌 Pinterest Downloader V1 BETA

Um aplicativo desenvolvido em C# (Windows Forms) para baixar pastas (boards) inteiras do Pinterest com um único clique. Ideal para artistas, motion designers e editores de vídeo que precisam coletar referências visuais e moodboards em alta qualidade de forma rápida.

## 🚀 Funcionalidades

- **Download em Massa:** Baixa todas as mídias de uma pasta do Pinterest automaticamente.
- **Suporte a Vídeos:** Além de imagens em alta resolução (`.jpg`), o app também detecta e baixa vídeos (`.mp4`) presentes no board. TESTE!!!
- **Scroll Infinito Automatizado:** O aplicativo rola a página automaticamente até o final para garantir que todo o conteúdo seja carregado e capturado. TESTE!!
- **Interface Intuitiva:** Interface gráfica simples, sem necessidade de rodar comandos no terminal.

## ⚠️ Como usar (Aviso de Login)

Por razões de segurança e para evitar acessar os dados pessoais do seu navegador padrão, este aplicativo utiliza um **Perfil de Navegação Portátil e Isolado**.

1. **Primeiro Uso:** Ao clicar em "Baixar tudo" pela primeira vez, uma janela automatizada do Google Chrome será aberta. Você terá **15 segundos** para fazer o seu login no Pinterest manualmente nessa janela. (O Pinterest exige login para visualizar pastas completas).
2. **Próximos Usos:** O aplicativo criará uma pasta chamada `PerfilPinterest` no diretório do programa para salvar a sua sessão. Nas próximas vezes que você usar, o login já estará salvo e a extração começará imediatamente de forma automática.

> *Sua senha não é interceptada, lida ou armazenada pelo código. O login ocorre diretamente nos servidores do Pinterest através da interface segura do Chrome.*

## 🛠️ Pré-requisitos

- Windows (10 ou 11).
- Ter o **Google Chrome** instalado no computador (o motor do app utiliza o ChromeDriver).
- Conexão estável com a internet.

## 💻 Como executar o projeto (Desenvolvedores)

Se você quiser clonar e compilar o código por conta própria:

1. Clone o repositório:
   ```bash
   git clone [https://github.com/SEU-USUARIO/PinterestDOWNLOAD.git](https://github.com/SEU-USUARIO/PinterestDOWNLOAD.git)
