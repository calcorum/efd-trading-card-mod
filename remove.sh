#!/bin/bash
# Remove Trading Card Mod from Escape from Duckov
# Usage: ./remove.sh [--backup]

set -e

# Configuration
GAME_PATH="/mnt/NV2/SteamLibrary/steamapps/common/Escape from Duckov"
MOD_NAME="TradingCardMod"
MOD_DIR="$GAME_PATH/Duckov_Data/Mods/$MOD_NAME"
BACKUP_DIR="$HOME/.local/share/TradingCardMod_backup"

echo "=== Trading Card Mod Removal ==="
echo "Target: $MOD_DIR"
echo ""

# Check if mod exists
if [[ ! -d "$MOD_DIR" ]]; then
    echo "Mod not installed at: $MOD_DIR"
    exit 0
fi

# Backup option
if [[ "$1" == "--backup" ]]; then
    echo "[1/2] Creating backup..."
    mkdir -p "$BACKUP_DIR"
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)
    BACKUP_PATH="$BACKUP_DIR/${MOD_NAME}_$TIMESTAMP"
    cp -r "$MOD_DIR" "$BACKUP_PATH"
    echo "      Backup saved to: $BACKUP_PATH"
    echo "[2/2] Removing mod..."
else
    echo "[1/1] Removing mod..."
fi

# Remove the mod
rm -rf "$MOD_DIR"

echo ""
echo "=== Removal Complete ==="
echo "Mod removed from: $MOD_DIR"

# Show backup location if created
if [[ "$1" == "--backup" ]]; then
    echo "Backup location: $BACKUP_PATH"
fi
