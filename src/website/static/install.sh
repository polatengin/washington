#!/usr/bin/env bash
set -euo pipefail

# BCE CLI installer
# Usage: curl -sL https://bicepcostestimator.net/install.sh | bash

REPO="polatengin/washington"
INSTALL_DIR="${INSTALL_DIR:-$HOME/.bce/bin}"

echo "Installing BCE CLI..."

# Detect OS and architecture
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

# Get latest release
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

# Download and extract
mkdir -p "$INSTALL_DIR"
curl -fsSL "$DOWNLOAD_URL" -o "$TARGET_PATH"
chmod +x "$TARGET_PATH"

echo ""
echo "BCE CLI installed to ${TARGET_PATH}"

# Check if INSTALL_DIR is in PATH
if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
  echo ""
  echo "Add the following to your shell profile (.bashrc, .zshrc, etc.):"
  echo ""
  echo "  export PATH=\"\$PATH:${INSTALL_DIR}\""
  echo ""
fi

echo "Run 'bce --help' to get started."
