# Nina ERP — Operação, backup e recuperação

## Objetivos iniciais do piloto

Os valores abaixo são objetivos operacionais da primeira build e devem ser revisados após medição no piloto:

- **RPO alvo:** 60 minutos. Uma falha não deve exigir perda superior a uma hora de dados confirmados.
- **RTO alvo:** 120 minutos. O procedimento documentado deve permitir restaurar o serviço em até duas horas no ambiente dimensionado para o piloto.
- **Retenção padrão de backups:** 30 dias, com cópia fora do servidor de aplicação recomendada.

Esses números não são garantias automáticas: devem ser comprovados por exercícios periódicos de restauração.

## Backup lógico

Use `scripts/windows/Backup-Database.ps1`. O script executa `pg_dump` em formato custom, valida o arquivo com `pg_restore --list`, gera SHA-256 e remove backups locais mais antigos que a retenção configurada.

Recomendação para o piloto: backup completo diário e antes de cada migration/upgrade. Copie o arquivo `.backup` e o `.sha256` para armazenamento separado do host do PostgreSQL.

## Restore

Use `scripts/windows/Restore-Database.ps1`. Faça restauração primeiro em ambiente isolado sempre que possível. Depois do restore:

1. valide o checksum do backup;
2. execute a restauração com o serviço Nina ERP parado;
3. aplique somente migrations previstas para aquela versão;
4. execute `/health` e o smoke test;
5. concilie quantidade de empresas, usuários, vendas, transações, saldos, títulos e documentos fiscais;
6. somente então libere clientes na rede.

## PITR / WAL

Para operação que exija RPO inferior ao intervalo entre backups completos, habilite `archive_mode` e `archive_command` no PostgreSQL e envie WAL para armazenamento durável. PITR deve ser ensaiado em servidor separado. A configuração concreta depende do PostgreSQL, sistema operacional e armazenamento utilizados; não é habilitada automaticamente pelo instalador desta etapa.

## Replicação e failover

A arquitetura permanece **monólito modular + PostgreSQL**. Réplica de leitura, streaming replication e failover só devem ser ativados quando o piloto demonstrar necessidade e houver monitoramento para detectar lag e split-brain. O Nina ERP não assume que uma réplica é gravável.

## Segredos e certificados

- Não versionar senhas, certificados privados, CSC ou tokens de adquirentes.
- Produção aborta a inicialização se JWT ou connection string ainda contiverem placeholders `CHANGE-ME`.
- Referências de segredos fiscais devem apontar para armazenamento protegido; o banco não deve conter a chave privada do certificado.
- Restrinja ACL da pasta `%ProgramData%\NinaERP` e da pasta de backups.

## Observabilidade

- Toda requisição recebe `X-Correlation-ID` e o mesmo identificador entra na auditoria/log.
- Escritas HTTP em `/api` geram `AuditLog` sem registrar corpo, senha, token ou payload fiscal.
- Outbox de pagamentos mantém `Attempts`, `LastError`, `NextAttemptAt` e exige conciliação ao esgotar retries.
- `/health` indica disponibilidade do PostgreSQL sem expor credenciais.

## Teste periódico

No mínimo mensalmente no piloto: restaurar o último backup em ambiente isolado, medir RPO/RTO real, executar smoke tests e registrar resultado. Antes de releases com migration, fazer backup e um ensaio de upgrade sobre cópia representativa do banco.
