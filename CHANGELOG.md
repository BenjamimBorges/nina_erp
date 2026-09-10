# Changelog — Nina ERP

## 0.2.0-hml — 2026-09-10

Release candidate de homologação das etapas 00–20.

### Adicionado
- Administração de usuários por empresa com perfis Admin, Manager, Stock e Cashier.
- Login multiempresa explícito por CNPJ + usuário + senha.
- Massa de homologação determinística: 3 perfis administrativos, 3 vendedores, 20 produtos, 4 clientes e vendas de teste.
- Testes funcionais de API, RBAC, isolamento multiempresa, rate limiting, injeção, headers e carga leve.
- Readiness operacional e tela administrativa de Qualidade.
- Manifesto SHA-256 de release, assinatura de artefatos, atualização controlada e rollback operacional.
- Runbook de piloto e extensão documentada das etapas 16–20.

### Segurança
- A autenticação deixa de resolver usuário apenas por username e passa a exigir o tenant por CNPJ.
- JWT sai do JSON/localStorage e passa a sessão por cookie HttpOnly + SameSite=Strict; HTTPS sempre força Secure.
- Escritas por cookie possuem proteção anti-CSRF e a API envia CSP/headers defensivos.
- Alteração de senha/perfil/status revoga imediatamente sessões antigas via `userVersion`.
- Testes de IDOR/isolamento horizontal e mass assignment de `CompanyId` foram adicionados.
- Massa de homologação bloqueada em Production e sem senha fixa no fonte.
- Proteção contra remoção/desativação do último administrador ativo.
- Bootstrap reproduzível do SDK .NET 10 para CI, Windows/Linux online e pacote offline com validação SHA-512.

### Não homologado
- Assinatura Authenticode depende de certificado real.
- NF-e/NFC-e de produção depende de provedor/certificado homologado e validação contábil/fiscal.
- Testes Windows/PostgreSQL/.NET devem passar no runner/VM antes do piloto real.
