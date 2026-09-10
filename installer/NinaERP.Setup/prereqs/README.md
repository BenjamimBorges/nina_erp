# Pré-requisitos offline do Nina ERP

O build principal é self-contained, portanto **não precisa instalar o .NET Runtime** no computador final.

Para uma instalação totalmente offline, coloque nesta pasta antes de compilar o instalador:

- `MicrosoftEdgeWebView2RuntimeInstallerX64.exe` — Microsoft Edge WebView2 Evergreen Runtime.
- Instalador oficial do PostgreSQL 16 x64, se você quiser distribuir o servidor de banco junto ao pacote. O NinaERP.Setup não redistribui esse binário por padrão.

Em Windows 11 o WebView2 normalmente já está presente, mas o setup verifica a disponibilidade e pode executar o instalador offline quando ele estiver incluído.
