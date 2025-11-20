#!/bin/bash
# Deploy Trading Card Mod to Escape from Duckov
# Usage: ./deploy.sh [--release] [--no-example]

set -e

# Configuration
GAME_PATH="/mnt/NV2/SteamLibrary/steamapps/common/Escape from Duckov"
MOD_NAME="TradingCardMod"
MOD_DIR="$GAME_PATH/Duckov_Data/Mods/$MOD_NAME"

# Parse arguments
BUILD_CONFIG="Debug"
EXCLUDE_EXAMPLE=false

for arg in "$@"; do
    case $arg in
        --release)
            BUILD_CONFIG="Release"
            ;;
        --no-example)
            EXCLUDE_EXAMPLE=true
            ;;
    esac
done

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
    for set_dir in CardSets/*/; do
        set_name=$(basename "$set_dir")

        # Skip ExampleSet if --no-example flag is set
        if [[ "$EXCLUDE_EXAMPLE" == true && "$set_name" == "ExampleSet" ]]; then
            echo "      Skipping: $set_name (--no-example)"
            continue
        fi

        cp -r "$set_dir" "$MOD_DIR/CardSets/"
        echo "      Copied: $set_name"
    done
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
