#!/usr/bin/env bash
set -euo pipefail

# BCE CLI installer
# Usage: curl -sL https://bicepcostestimator.net/install.sh | bash

REPO="polatengin/washington"
INSTALL_DIR="${INSTALL_DIR:-}"

echo "Installing BCE CLI..."

OS="$(uname -s | tr '[:upper:]' '[:lower:]')"
ARCH="$(uname -m)"

case "$ARCH" in
  x86_64)  ARCH="x64" ;;
  aarch64) ARCH="arm64" ;;
  arm64)   ARCH="arm64" ;;
  *)
    echo "Error: Unsupported architecture: $ARCH"
    exit 1
    ;;
esac

case "$OS" in
  linux)  PLATFORM="linux-${ARCH}" ;;
  darwin) PLATFORM="osx-${ARCH}" ;;
  *)
    echo "Error: Unsupported OS: $OS"
    exit 1
    ;;
esac

if [[ -z "$INSTALL_DIR" ]]; then
  case "$OS" in
    linux)
      INSTALL_DIR="$HOME/.local/bin"
      ;;
    darwin)
      if [[ "$ARCH" == "arm64" ]]; then
        INSTALL_DIR="/opt/homebrew/bin"
      else
        INSTALL_DIR="/usr/local/bin"
      fi
      ;;
  esac
fi

if ! mkdir -p "$INSTALL_DIR" >/dev/null 2>&1 || [[ ! -w "$INSTALL_DIR" ]]; then
  echo "Error: Install directory is not writable: ${INSTALL_DIR}"
  echo ""
  echo "Re-run with a writable install directory by setting INSTALL_DIR:"
  echo ""
  echo '  curl -sL https://bicepcostestimator.net/install.sh | INSTALL_DIR="/path/on/your/PATH" bash'
  echo ""
  exit 1
fi

LATEST=$(curl -fsSL "https://api.github.com/repos/${REPO}/releases/latest" | grep '"tag_name"' | sed -E 's/.*"([^"]+)".*/\1/')

if [ -z "$LATEST" ]; then
  echo "Error: Could not determine latest release."
  exit 1
fi

DOWNLOAD_URL="https://github.com/${REPO}/releases/download/${LATEST}/bce-${PLATFORM}"
TARGET_PATH="${INSTALL_DIR}/bce"

echo "  Version:  ${LATEST}"
echo "  Platform: ${PLATFORM}"
echo "  Target:   ${INSTALL_DIR}"

curl -fsSL "$DOWNLOAD_URL" -o "$TARGET_PATH"
chmod +x "$TARGET_PATH"

case ":$PATH:" in
  *":${INSTALL_DIR%/}:"*)
    ;;
  *)
    echo ""
    echo "${INSTALL_DIR} is not on your PATH."
    echo "Run the following now, or add it to your shell config:"
    echo ""
    echo "  export PATH=\"\$PATH:${INSTALL_DIR}\""
    echo ""
    ;;
esac

echo "Run 'bce --help' to get started."
