# Nina ERP — Implementação das etapas 13–20

> O Script Mestre formal fornecido termina na Etapa 15. As Etapas 16–20 abaixo são uma extensão de homologação/release desta evolução; não alteram o significado das Etapas 13–15 originais.

## Etapa 13 — Qualidade e produto comercial

**Implementado:** CI Linux/PostgreSQL e publish Windows; testes unitários, integração e HTTP; carga leve; scripts de backup/restore; update com backup prévio; rollback explícito; manifesto SHA-256; verificação Authenticode/ thumbprint; build self-contained; Inno Setup; changelog e documentação operacional.

**Gate:** nenhum update de release é considerado confiável apenas por checksum; executáveis/instalador também precisam de assinatura válida. Migrations destrutivas não são aplicadas automaticamente em produção.

**Ainda depende de ambiente externo:** assinatura com certificado real, Windows limpo, restore real e execução do CI .NET/PostgreSQL.

## Etapa 14 — Piloto comercial

**Implementado:** runbook, preflight e massa de homologação para percorrer compra → recebimento → estoque → caixa → venda → pagamento → financeiro → relatórios. O preflight agora inclui E2E transacional, carga e segurança.

**Regra:** emissão fiscal real não é simulada; o piloto permanece em homologação até provedor/certificado e cenários tributários serem validados.

## Etapa 15 — Expansão

**Implementado:** feature flags conservadoras e readiness. Omnichannel, Mobile API, Approvals, Assistant e Distributed Scale permanecem desligados por padrão. Nenhuma IA pode autonomamente emitir documento fiscal, alterar estoque ou executar pagamento.

## Etapa 16 — Gestão de usuários e RBAC (extensão)

**Implementado:** API/tela administrativa para criar usuários, trocar perfil, ativar/desativar e redefinir senha; login tenant-aware; senha forte; proteção do último Admin. Alterações administrativas revogam imediatamente sessões existentes via `userVersion`.

## Etapa 17 — Massa de homologação (extensão)

**Implementado:** seed não produtivo e idempotente com 3 perfis administrativos (`Admin`, `Manager`, `Stock`), 3 vendedores (`Cashier`), 20 produtos com EAN-13 válido/único, custo/preço distintos, 4 clientes em CE/SP/PE/MA e cenários de vendas.

O estoque inicial de 100 unidades por produto entra pelo pedido `HML-PO-001`, recebido pelo serviço de compras, gerando movimentações e conta a pagar. Há 4 vendas em dinheiro concluídas e 1 PIX deliberadamente pendente.

## Etapa 18 — E2E funcional (extensão)

**Implementado:** testes PostgreSQL e HTTP para seed idempotente, compra/recebimento, estoque, vendas por clientes/vendedores distintos, pagamento/caixa, idempotência, PIX pendente, usuários e readiness. `verify-pilot.sh` executa também o E2E transacional.

## Etapa 19 — Segurança, carga e recuperação (extensão)

**Implementado:** sessão por cookie HttpOnly/SameSite, Secure obrigatório em HTTPS, anti-CSRF, CSP, revogação de sessão, IDOR/tenant tests, mass-assignment test, RBAC, injection probe, rate limiting, carga concorrente leve, backup/restore e procedimentos de recuperação.

## Etapa 20 — Release candidate comercial (extensão)

**Implementado:** `0.2.0-hml`, `productionApproved=false`, painel de Qualidade/Readiness, transporte seguro explicitamente visível, pipeline de release, SDK .NET 10 reproduzível por CI/online/offline e pacote fonte limpo.

**Critério de fechamento:** Etapa 20 fica **implementada como release candidate**, mas não “homologada para produção” até CI .NET/PostgreSQL, migrations em banco vazio + legado, publish/instalação Windows, TLS real, backup/restore e validação fiscal/contábil passarem com evidência.
