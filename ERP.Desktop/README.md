# Nina ERP Desktop

O cliente usa `https://localhost:7029/` por padrão para desenvolvimento local.
Para uma instalação em outra máquina, defina `NINAERP_API_BASE_URL` no ambiente
do processo para uma URL HTTPS absoluta, sem usuário, senha ou token na URL.

Exemplo: `https://erp.example.internal/`.

URLs HTTP, relativas ou com credenciais são rejeitadas antes do login.
