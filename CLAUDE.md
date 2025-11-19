# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity mod for Escape from Duckov that adds a customizable trading card system with storage solutions. The framework supports user-generated content through simple pipe-separated text files, allowing non-programmers to add their own card sets.

## Build Commands

```bash
# Build the mod
dotnet build

# Build release version
dotnet build -c Release

# Output location: bin/Debug/netstandard2.1/TradingCardMod.dll
```

**Important**: Before building, update `DuckovPath` in `TradingCardMod.csproj` (line 10) to your actual game installation path.

## Architecture

### Mod Loading System

The game loads mods from `Duckov_Data/Mods/`. Each mod requires:
- A DLL with namespace matching `info.ini`'s `name` field
- A `ModBehaviour` class inheriting from `Duckov.Modding.ModBehaviour`
- The `info.ini` and `preview.png` files

### Key Classes

- **`ModBehaviour`** (`src/ModBehaviour.cs`): Main entry point. Inherits from `Duckov.Modding.ModBehaviour` (which extends `MonoBehaviour`). Handles card set loading in `Start()` and cleanup in `OnDestroy()`.

- **`Patches`** (`src/Patches.cs`): Harmony patch definitions. Uses `HarmonyLib` for runtime method patching. Patches are applied in `Start()` and removed in `OnDestroy()`.

- **`TradingCard`**: Data class representing card properties. Contains `GenerateTypeID()` for creating unique item IDs (100000+ range to avoid game conflicts).

### Dependencies

- **HarmonyLoadMod** (Workshop ID: 3589088839): Required mod dependency providing Harmony 2.4.1. Referenced at build time but not bundled to avoid version conflicts.

### Card Definition Format

Cards are defined in `CardSets/{SetName}/cards.txt` using pipe-separated values:
```
CardName | SetName | SetNumber | ImageFile | Rarity | Weight | Value
```

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

## TODO Items in Code

The following features need implementation (marked with TODO comments):
- `RegisterCardWithGame()` - Create Unity prefabs and register with ItemAssetsCollection
- Item cleanup in `OnDestroy()` - Remove registered items on mod unload
- `TradingCard.ItemPrefab` property - Store Unity prefab reference
