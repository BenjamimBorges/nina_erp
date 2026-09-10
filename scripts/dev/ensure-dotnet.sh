#!/usr/bin/env bash
set -euo pipefail
VERSION="${NINAERP_DOTNET_VERSION:-10.0.401}"
INSTALL_DIR="${NINAERP_DOTNET_ROOT:-$HOME/.ninaerp/dotnet}"
OFFLINE_ARCHIVE="${NINAERP_DOTNET_ARCHIVE:-}"
OFFLINE_SHA512="${NINAERP_DOTNET_ARCHIVE_SHA512:-}"

have_compatible_dotnet() {
  command -v dotnet >/dev/null 2>&1 || return 1
  local req_major req_minor req_patch req_band
  IFS=. read -r req_major req_minor req_patch <<<"$VERSION"
  req_band=$(( (req_patch / 100) * 100 ))
  dotnet --list-sdks 2>/dev/null | awk '{print $1}' | awk -F. \
    -v maj="$req_major" -v min="$req_minor" -v patch="$req_patch" -v band="$req_band" \
    '$1==maj && $2==min && int($3/100)*100==band && $3>=patch {found=1} END{exit(found?0:1)}'
}
if have_compatible_dotnet; then
  echo "SDK .NET 10 disponível: $(dotnet --version)"
  exit 0
fi
mkdir -p "$INSTALL_DIR"
if [[ -n "$OFFLINE_ARCHIVE" ]]; then
  [[ -f "$OFFLINE_ARCHIVE" ]] || { echo "Arquivo offline não encontrado: $OFFLINE_ARCHIVE" >&2; exit 2; }
  if [[ -n "$OFFLINE_SHA512" ]]; then
    actual_sha512="$(sha512sum "$OFFLINE_ARCHIVE" | awk '{print $1}')"
    [[ "${actual_sha512,,}" == "${OFFLINE_SHA512,,}" ]] || { echo "SHA-512 do SDK offline não confere." >&2; exit 5; }
    echo "SHA-512 do SDK offline validado."
  else
    echo "AVISO: SDK offline sem SHA-512. Em release, defina NINAERP_DOTNET_ARCHIVE_SHA512." >&2
  fi
  rm -rf "$INSTALL_DIR"/*
  tar -xzf "$OFFLINE_ARCHIVE" -C "$INSTALL_DIR"
else
  tmp="$(mktemp)"; trap 'rm -f "$tmp"' EXIT
  if ! curl --fail --location --retry 3 --silent --show-error https://dot.net/v1/dotnet-install.sh -o "$tmp"; then
    cat >&2 <<MSG
Rede indisponível para baixar o SDK. Baixe o binário Linux x64 oficial do SDK $VERSION em outra máquina,
copie o .tar.gz e execute:
  NINAERP_DOTNET_ARCHIVE=/caminho/dotnet-sdk-$VERSION-linux-x64.tar.gz $0
MSG
    exit 3
  fi
  bash "$tmp" --version "$VERSION" --install-dir "$INSTALL_DIR" --no-path
fi
export DOTNET_ROOT="$INSTALL_DIR"
export PATH="$INSTALL_DIR:$PATH"
have_compatible_dotnet || { echo "Falha ao ativar SDK compatível com $VERSION em $INSTALL_DIR" >&2; exit 4; }
echo "SDK .NET pronto: $(dotnet --version) ($INSTALL_DIR)"
