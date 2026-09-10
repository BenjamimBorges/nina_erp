#!/usr/bin/env bash
set -euo pipefail
: "${NINAERP_BASE_URL:?NINAERP_BASE_URL is required}"
: "${NINAERP_HOMOLOGATION_PASSWORD:?NINAERP_HOMOLOGATION_PASSWORD is required}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"
mkdir -p artifacts/ci/pilot
scripts/functional/api-homologation-smoke.sh
python3 scripts/functional/api-transaction-e2e.py
python3 scripts/load/api-load-smoke.py
scripts/security/api-security-smoke.sh
{
 echo 'Nina ERP pilot preflight: PASS'
 echo '- functional homologation smoke: PASS'
 echo '- transactional E2E: PASS'
 echo '- lightweight load smoke: PASS'
 echo '- API security smoke: PASS'
 echo '- fiscal production authorization: NOT VALIDATED BY THIS SCRIPT'
 echo '- Windows installer/signing: NOT VALIDATED BY THIS SCRIPT'
} | tee artifacts/ci/pilot/result.txt
