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

## Current Project Status

**Phase:** 2 - Core Card Framework
**Project Plan:** `.claude/scratchpad/PROJECT_PLAN.md`
**Technical Analysis:** `.claude/scratchpad/item-system-analysis.md`

### Implementation Approach: Clone + Reflection

Based on analysis of the AdditionalCollectibles mod, we're using a viable approach:

1. **Clone existing game items** as templates (not create from scratch)
2. **Use reflection** to set private fields (typeID, weight, value, etc.)
3. **Create custom tags** by cloning existing ScriptableObject tags
4. **Load sprites** from user files in `CardSets/*/images/`

Key patterns:
```csharp
// Clone base item
Item original = ItemAssetsCollection.GetPrefab(135);
GameObject clone = Object.Instantiate(original.gameObject);
Object.DontDestroyOnLoad(clone);

// Set properties via reflection
item.SetPrivateField("typeID", newId);
item.SetPrivateField("value", cardValue);

// Get/create tags
Tag tag = Resources.FindObjectsOfTypeAll<Tag>()
    .FirstOrDefault(t => t.name == "Luxury");
```

### Next Implementation Steps

1. Create `src/ItemExtensions.cs` - reflection helper methods
2. Create `src/TagHelper.cs` - tag operations
3. Update `src/ModBehaviour.cs` - use clone+reflection approach
4. Test card creation in-game

### Files to Create

| File | Purpose |
|------|---------|
| `src/ItemExtensions.cs` | `SetPrivateField()`, `GetPrivateField()` extensions |
| `src/TagHelper.cs` | `GetTargetTag()`, `CreateOrCloneTag()` |

## Research References

- **Decompiled game code:** `.claude/scratchpad/decompiled/`
- **Item system analysis:** `.claude/scratchpad/item-system-analysis.md`
- **AdditionalCollectibles mod:** Workshop ID 3591453758 (reference implementation)
