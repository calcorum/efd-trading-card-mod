#!/bin/bash
# Deploy Trading Card Mod to Escape from Duckov
# Usage: ./deploy.sh [--release]

set -e

# Configuration
GAME_PATH="/mnt/NV2/SteamLibrary/steamapps/common/Escape from Duckov"
MOD_NAME="TradingCardMod"
MOD_DIR="$GAME_PATH/Duckov_Data/Mods/$MOD_NAME"

# Build configuration
BUILD_CONFIG="Debug"
if [[ "$1" == "--release" ]]; then
    BUILD_CONFIG="Release"
fi

echo "=== Trading Card Mod Deployment ==="
echo "Build config: $BUILD_CONFIG"
echo "Target: $MOD_DIR"
echo ""

# Build the project
echo "[1/4] Building project..."
dotnet build TradingCardMod.csproj -c "$BUILD_CONFIG" --verbosity quiet
if [[ $? -ne 0 ]]; then
    echo "ERROR: Build failed!"
    exit 1
fi
echo "      Build successful"

# Create mod directory if it doesn't exist
echo "[2/4] Creating mod directory..."
mkdir -p "$MOD_DIR"
mkdir -p "$MOD_DIR/CardSets"

# Copy mod files
echo "[3/4] Copying mod files..."
cp "bin/$BUILD_CONFIG/netstandard2.1/$MOD_NAME.dll" "$MOD_DIR/"
cp "info.ini" "$MOD_DIR/"

# Copy preview if it exists
if [[ -f "preview.png" ]]; then
    cp "preview.png" "$MOD_DIR/"
fi

# Copy card sets
echo "[4/4] Copying card sets..."
if [[ -d "CardSets" ]]; then
    cp -r CardSets/* "$MOD_DIR/CardSets/" 2>/dev/null || true
fi

echo ""
echo "=== Deployment Complete ==="
echo "Mod installed to: $MOD_DIR"
echo ""
echo "Contents:"
ls -la "$MOD_DIR/"
echo ""
echo "Card sets:"
ls -la "$MOD_DIR/CardSets/" 2>/dev/null || echo "  (none)"
