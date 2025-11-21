# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity mod for Escape from Duckov that adds a customizable trading card system with storage solutions. The framework supports user-generated content through simple pipe-separated text files, allowing non-programmers to add their own card sets.

## Build Commands

```bash
# Build the mod
dotnet build TradingCardMod.csproj

# Build release version
dotnet build TradingCardMod.csproj -c Release

# Output location: bin/Debug/netstandard2.1/TradingCardMod.dll
```

**Important**: Before building, update `DuckovPath` in `TradingCardMod.csproj` (line 10) to your actual game installation path.

## Deployment

```bash
# Deploy to game (builds and copies all files)
./deploy.sh

# Deploy release build
./deploy.sh --release

# Remove mod from game
./remove.sh

# Remove with backup
./remove.sh --backup
```

## Testing

```bash
# Run all unit tests
dotnet test TradingCardMod.Tests.csproj

# Run tests with verbose output
dotnet test TradingCardMod.Tests.csproj --verbosity normal

# Run specific test class
dotnet test TradingCardMod.Tests.csproj --filter "FullyQualifiedName~CardParserTests"

# Run single test
dotnet test TradingCardMod.Tests.csproj --filter "ParseLine_ValidLineWithoutDescription_ReturnsCard"
```

**Test Coverage:** Parsing logic, validation, rarity mapping, TypeID generation, and description auto-generation are all tested. Unity-dependent code (item creation, tags) cannot be unit tested.

## Architecture

### Mod Loading System

The game loads mods from `Duckov_Data/Mods/`. Each mod requires:
- A DLL with namespace matching `info.ini`'s `name` field
- A `ModBehaviour` class inheriting from `Duckov.Modding.ModBehaviour`
- The `info.ini` and `preview.png` files

### Key Classes

- **`ModBehaviour`** (`src/ModBehaviour.cs`): Main entry point. Inherits from `Duckov.Modding.ModBehaviour` (which extends `MonoBehaviour`). Handles card set loading in `Start()` and cleanup in `OnDestroy()`.

- **`Patches`** (`src/Patches.cs`): Harmony patch definitions. Uses `HarmonyLib` for runtime method patching. Patches are applied in `Start()` and removed in `OnDestroy()`.

- **`TradingCard`** (`src/TradingCard.cs`): Data class representing card properties. Contains `GenerateTypeID()` for creating unique item IDs (100000+ range to avoid game conflicts).

- **`CardParser`** (`src/CardParser.cs`): Parses card definition files. Pure C# with no Unity dependencies, fully unit tested.

- **`ItemExtensions`** (`src/ItemExtensions.cs`): Reflection helpers for setting private fields on game objects.

- **`TagHelper`** (`src/TagHelper.cs`): Utilities for working with game tags, including creating custom tags.

- **`PackUsageBehavior`** (`src/PackUsageBehavior.cs`): Handles card pack opening mechanics. Implements gacha-style random card distribution based on rarity weights.

- **`ModConfigApi`** (`src/ModConfigApi.cs`): Optional integration with ModConfig mod. Adds card set information (set name, card number, rarity) to item descriptions in inventory.

### Dependencies

- **HarmonyLoadMod** (Workshop ID: 3589088839): Required mod dependency providing Harmony 2.4.1. Referenced at build time but not bundled to avoid version conflicts.

- **ModConfig** (Workshop ID: 3592433938): Optional mod dependency. When installed, enhances card descriptions with set information in the inventory UI.

### Card Definition Format

Cards are defined in `CardSets/{SetName}/cards.txt` using pipe-separated values:
```
CardName | SetName | SetNumber | ImageFile | Rarity | Weight | Value | Description (optional)
```

The Description field is optional. If provided, it will be displayed in the item's in-game description/tooltip.

Images go in `CardSets/{SetName}/images/`.

## Game API Reference

Key namespaces and APIs from the game:
- `ItemStatsSystem.ItemAssetsCollection.AddDynamicEntry(Item prefab)` - Register custom items
- `ItemStatsSystem.ItemAssetsCollection.RemoveDynamicEntry(Item prefab)` - Remove on unload
- `SodaCraft.Localizations.LocalizationManager.SetOverrideText(string key, string value)` - Localization
- `ItemStatsSystem.ItemUtilities.SendToPlayer(Item item)` - Give items to player

## Development Notes

- Target framework: .NET Standard 2.1
- C# language version: 8.0
- All logging uses `Debug.Log()` with `[TradingCardMod]` prefix
- Custom items need unique TypeIDs to avoid conflicts with base game and other mods

## Testing

1. Copy build output to `{GamePath}/Duckov_Data/Mods/TradingCardMod/`
2. Include: `TradingCardMod.dll`, `info.ini`, `preview.png`, `CardSets/` folder
3. Launch game and enable mod in Mods menu
4. Check game logs for `[TradingCardMod]` messages

## Current Project Status

**Phase:** 3 Complete - Storage & Pack System ✅
**Status:** Ready for first release candidate
**Project Plan:** `.claude/scratchpad/PROJECT_PLAN.md`
**Technical Analysis:** `.claude/scratchpad/item-system-analysis.md`

### Completed Features

- Cards load from `CardSets/*/cards.txt` files with optional descriptions
- Custom PNG images display as item icons
- Cards register as game items with proper TypeIDs
- Custom "TradingCard" tag for filtering
- Card packs with gacha-style mechanics (weighted random distribution)
- Storage system with slot-based filtering (9-slot binders, 18-slot boxes)
- ModConfig integration for enhanced card info display (set name, number, rarity)
- Debug spawn with F9 key (for testing)
- Deploy/remove scripts for quick iteration
- Unit tests for parsing logic and pack system

### Implementation Approach: Clone + Reflection

Based on analysis of the AdditionalCollectibles mod:

1. **Clone existing game items** as templates (base item ID 135)
2. **Use reflection** to set private fields (typeID, weight, value, etc.)
3. **Create custom tags** by cloning existing ScriptableObject tags
4. **Load sprites** from user files in `CardSets/*/images/`
5. **Attach custom behaviors** for pack opening mechanics

### Future Considerations

- Investigate new ItemBuilder API (added in recent game update) as potential replacement for reflection-based approach
- Additional storage variants or customization options
- Binder sheets which hold cards are are held by binders

### Log File Location

Unity logs (for debugging):
```
/mnt/NV2/SteamLibrary/steamapps/compatdata/3167020/pfx/drive_c/users/steamuser/AppData/LocalLow/TeamSoda/Duckov/Player.log
```

## Research References

- **Decompiled game code:** `.claude/scratchpad/decompiled/`
- **Item system analysis:** `.claude/scratchpad/item-system-analysis.md`
- **AdditionalCollectibles mod:** Workshop ID 3591453758 (reference implementation)
