#!/usr/bin/env bash
set -euo pipefail

: "${NINAERP_TEST_CONNECTION:?NINAERP_TEST_CONNECTION is required}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
mkdir -p artifacts/ci

printf '\n[1/8] Tool restore\n'
dotnet tool restore

printf '\n[2/8] Restore + build test graph and API startup project\n'
dotnet restore src/NinaERP.Tests/NinaERP.Tests.csproj
dotnet restore src/NinaERP.API/NinaERP.API.csproj
dotnet build src/NinaERP.Tests/NinaERP.Tests.csproj -c Release --no-restore
dotnet build src/NinaERP.API/NinaERP.API.csproj -c Release --no-restore

printf '\n[3/8] Unit/contract tests\n'
dotnet test src/NinaERP.Tests/NinaERP.Tests.csproj -c Release --no-build --filter 'Category!=Integration' --logger 'trx;LogFileName=unit.trx' --results-directory artifacts/ci/tests

printf '\n[4/8] EF migration metadata\n'
dotnet ef migrations list --project src/NinaERP.Infrastructure/NinaERP.Infrastructure.csproj --startup-project src/NinaERP.API/NinaERP.API.csproj --no-build

dotnet ef migrations has-pending-model-changes --project src/NinaERP.Infrastructure/NinaERP.Infrastructure.csproj --startup-project src/NinaERP.API/NinaERP.API.csproj --no-build

printf '\n[5/8] Generate idempotent migration script\n'
dotnet ef migrations script 0 --idempotent --project src/NinaERP.Infrastructure/NinaERP.Infrastructure.csproj --startup-project src/NinaERP.API/NinaERP.API.csproj --no-build --output artifacts/ci/ninaerp-migrations.sql

test -s artifacts/ci/ninaerp-migrations.sql

printf '\n[6/8] PostgreSQL integration tests: empty database + constraints + reporting + legacy upgrade\n'
dotnet test src/NinaERP.Tests/NinaERP.Tests.csproj -c Release --no-build --filter 'Category=Integration' --logger 'trx;LogFileName=postgres.trx' --results-directory artifacts/ci/tests

printf '\n[7/8] Verify API release output\n'
test -f src/NinaERP.API/bin/Release/net10.0/NinaERP.API.dll

printf '\n[8/8] Completed PostgreSQL gate\n'
