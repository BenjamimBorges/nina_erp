# Nina ERP — Implementação das Etapas 20–30

Release candidate: **0.3.0-hml**  
Data da consolidação: **2026-09-10**  
Estado comercial: **homologação; `productionApproved=false`**.

Este documento registra a implementação das etapas 20–30 do roadmap histórico do Nina ERP. Ele não substitui os gates definidos no Script Mestre: uma funcionalidade é distinguida entre implementada, compilada, testada, integrada, homologada e pronta para produção.

## Etapa 20 — Dashboard e relatórios

**Implementado:** serviço de reporting tenant-scoped, endpoints de relatórios e telas operacionais. As métricas usam as fontes transacionais já consolidadas e mantêm `CompanyId` no recorte de dados.

**Entregas principais:** relatório por vendedor, mix de pagamentos, dashboard operacional e consultas por período. O BI não usa joins multiempresa sem filtro explícito.

**Aceite atual:** implementado e incluído nos gates estáticos/HTTP. A conciliação final com um banco PostgreSQL real permanece no CI/VM.

## Etapa 21 — Fiscal

**Implementado:** readiness fiscal, estados persistidos, idempotência, numeração concorrente, inutilização, cancelamento e validação de autorização real. A interface diferencia configuração pronta de autorização fiscal efetiva.

**Restrição deliberada:** nenhum documento é tratado como autorizado sem chave de 44 dígitos e protocolo real. Provider/certificado/SEFAZ de produção continuam externos ao repositório e exigem homologação por UF e validação contábil/fiscal.

## Etapa 22 — Testes de integração

**Implementado:** suíte PostgreSQL, cenários de migration em banco vazio e legado, smoke HTTP funcional, segurança, carga leve e testes das etapas de expansão. O workflow usa PostgreSQL 17 e .NET 10.0.401.

**Novos cenários 20–30:** quatro-olhos, replay omnichannel idempotente, projeções mobile por tenant, BI por empresa, bloqueio de ação sensível pela IA e exclusividade/reaquisição de leases distribuídos.

**Aceite atual:** código dos testes e CI implementados. Como o container desta sessão não possui `dotnet`, `psql` ou Docker, sua execução .NET/PostgreSQL permanece pendente no runner autoritativo.

## Etapa 23 — .NET 10 LTS

**Consolidado:** todos os projetos alvo usam .NET 10; `global.json` fixa SDK **10.0.401** no feature band 10.0.4xx e `dotnet-ef` está na linha 10. O bootstrap suporta SDK instalado, instalação online e arquivo offline com SHA-512.

## Etapa 24 — Hardening e atualização

**Consolidado:** cookie HttpOnly/SameSite, Secure forçado em HTTPS, anti-CSRF, CSP/headers, revogação de sessão por `userVersion`, segredos obrigatórios, rate limiting, manifesto de release, assinatura verificável, backup pré-update e rollback explícito.

**Produção:** assinatura Authenticode real, TLS/certificado e ensaio de update/rollback em VM Windows continuam critérios de homologação.

## Etapa 25 — Omnichannel

**Implementado como núcleo de integração:** `ChannelOrder`/itens, normalização, hash de payload, idempotência por empresa, catálogo e validação de tenant. O mesmo pedido externo repetido com o mesmo conteúdo retorna o registro anterior; replay divergente é rejeitado.

**Não simulado:** não há conector fictício de marketplace/e-commerce. Adapters externos serão adicionados somente quando houver provider real.

## Etapa 26 — Mobile / PWA

**Implementado:** endpoints móveis sobre a mesma API central e PWA React com `manifest.webmanifest` e service worker. O mobile consulta resumo, vendas recentes e baixo estoque sem duplicar regra de domínio.

**Segurança:** service worker não faz cache de respostas `/api`, reduzindo risco de persistência indevida de dados empresariais no navegador.

## Etapa 27 — Aprovações

**Implementado:** `ApprovalRequest`, estados, expiração, idempotência, ownership por empresa, perfil mínimo e regra de quatro-olhos. Solicitante não aprova a própria exceção.

**Integração inicial:** o mecanismo está disponível para operações que precisem exceção/autorização e já possui UI/API e testes de segurança. Novos tipos podem ser vinculados ao fluxo sem alterar o princípio de separação de funções.

## Etapa 28 — BI avançado

**Implementado:** read model mensal e por vendedor, refresh controlado e endpoints executivos. A projeção é separada do modelo transacional e sempre filtrada por empresa.

**Escala:** materialização/read models podem evoluir conforme métricas de desempenho; nenhuma infraestrutura distribuída adicional foi introduzida sem evidência de gargalo.

## Etapa 29 — Assistente Nina

**Implementado em modo consultivo:** consultas sobre indicadores do ERP, fontes de resposta e trilha de auditoria. A auditoria armazena hash da pergunta, não o prompt bruto.

**Bloqueios:** emissão fiscal, alteração de estoque, estorno/pagamento e outras ações sensíveis não são executadas autonomamente pelo assistente. O endpoint tem limite dedicado de taxa e payload.

## Etapa 30 — Preparação para escala distribuída

**Implementado sem microsserviços prematuros:** `DistributedLease`, `SystemNode`, heartbeat, leases concorrentes no PostgreSQL e endpoint administrativo de telemetria. Feature flag mantém o modo distribuído desligado por padrão.

**Gate multinó:** o CI está preparado para iniciar duas instâncias da API contra o mesmo PostgreSQL 17 e exigir que ambas apareçam saudáveis na telemetria antes do smoke 20–30.

**Próxima evolução apenas com métricas:** Redis, broker/event bus, réplica de leitura ou separação em serviços só devem ser introduzidos quando latência, throughput ou disponibilidade comprovarem necessidade.

## Estado consolidado

| Etapa | Estado nesta release candidate |
|---|---|
| 20 | Implementado; integração PostgreSQL pendente de runner |
| 21 | Estrutura/readiness implementados; homologação fiscal real pendente |
| 22 | Testes/CI implementados; execução .NET/PostgreSQL pendente de runner |
| 23 | Consolidado em .NET 10 |
| 24 | Hardening implementado; VM/assinatura real pendentes |
| 25 | Núcleo omnichannel implementado; adapters externos pendentes |
| 26 | API móvel/PWA implementada |
| 27 | Workflow de aprovações implementado |
| 28 | BI avançado/read models implementados |
| 29 | Assistente consultivo implementado; ações sensíveis bloqueadas |
| 30 | Coordenação/telemetria implementadas; escala externa condicionada a métricas |

Nenhuma dessas classificações transforma automaticamente a release em produção. `productionApproved=false` permanece obrigatório até build/test .NET, migrations PostgreSQL, publish/instalação Windows, restore, segurança dinâmica e homologações externas gerarem evidência reproduzível.
