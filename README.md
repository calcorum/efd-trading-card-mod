# Trading Card Mod for Escape from Duckov

A customizable trading card system that lets you add your own card sets to the game.

## Features

- **Custom Card Sets** - Create your own trading cards with custom artwork and stats
- **Card Packs** - Open randomized card packs with gacha-style rarity distribution
- **Storage System** - Organize your collection with 9-slot binders and 18-slot card boxes
- **User-Friendly Format** - Define cards using simple pipe-separated text files
- **ModConfig Integration** - Enhanced card info display when ModConfig is installed (optional)
- **No Programming Required** - Add new card sets without writing any code

## Requirements

**Required Mod Dependency:**
- [HarmonyLib (HarmonyLoadMod)](https://steamcommunity.com/sharedfiles/filedetails/?id=3589088839) - Subscribe on Steam Workshop

This mod requires the HarmonyLoadMod to be installed. It provides the Harmony library that many mods share to avoid version conflicts.

**Optional Mod Dependency:**
- [ModConfig](https://steamcommunity.com/sharedfiles/filedetails/?id=3592433938) - Subscribe on Steam Workshop

ModConfig is optional but recommended. When installed, it adds card set information (set name, card number, rarity) to the item description in your inventory, making it easier to identify and organize your cards.

## Installation

1. Subscribe to [HarmonyLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3589088839) on Steam Workshop
2. (Optional) Subscribe to [ModConfig](https://steamcommunity.com/sharedfiles/filedetails/?id=3592433938) for enhanced card descriptions
3. Build the mod (see Development section)
4. Copy the `TradingCardMod` folder to your game's `Duckov_Data/Mods` directory
5. Launch the game and enable the mods in the Mods menu (HarmonyLib is required, ModConfig is optional)

## Adding Card Sets

### Creating a New Card Set

1. Create a new folder in `CardSets/` with your set name (e.g., `CardSets/MyCards/`)
2. Create a `cards.txt` file in your folder
3. Create an `images/` subfolder for card artwork

### Card Definition Format

Cards are defined in `cards.txt` using pipe-separated values:

```
CardName | SetName | SetNumber | ImageFile | Rarity | Weight | Value | Description (optional)
```

Example:
```
Blue Dragon | Fantasy Set | 001 | blue_dragon.png | Ultra Rare | 0.05 | 500| A majestic dragon with scales of sapphire blue.
Fire Sprite | Fantasy Set | 002 | fire_sprite.png | Rare | 0.05 | 100
```

### Field Descriptions

| Field | Description | Example |
|-------|-------------|---------|
| CardName | Display name of the card | "Blue Dragon" |
| SetName | Name of the collection | "Fantasy Set" |
| SetNumber | Number for sorting (as integer) | 001 |
| ImageFile | Image filename in images/ folder | "blue_dragon.png" |
| Rarity | Card rarity tier | Common, Uncommon, Rare, Ultra Rare |
| Weight | Physical weight in game units | 0.05 |
| Value | In-game currency value | 500 |
| Description | Optional flavor text for the card | "A majestic dragon..." |

### Image Requirements

- Place images in your cardset's `images/` subfolder
- Recommended format: PNG

### Comments

Lines starting with `#` are treated as comments and ignored:

```
# This is a comment
# CardName | SetName | SetNumber | ImageFile | Rarity | Weight | Value
Blue Dragon | Fantasy Set | 001 | blue_dragon.png | Ultra Rare | 0.01 | 500
```

## Folder Structure

```
TradingCardMod/
├── TradingCardMod.dll
├── info.ini
├── preview.png
├── CardSets/
│   ├── ExampleSet/
│   │   ├── cards.txt
│   │   └── images/
│   │       └── (card images here)
│   └── YourCustomSet/
│       ├── cards.txt
│       └── images/
└── README.md
```

## Development

### Requirements

- .NET SDK (for .NET Standard 2.1)
- Escape from Duckov installed

### Building

1. Update `DuckovPath` in `TradingCardMod.csproj` to point to your game installation
2. Build the project:
   ```bash
   dotnet build
   ```
3. The compiled DLL will be in `bin/Debug/netstandard2.1/`

### Linux Steam Paths

Common Steam library locations on Linux:
- `~/.steam/steam/steamapps/common/Escape from Duckov`
- `~/.local/share/Steam/steamapps/common/Escape from Duckov`

### Testing

1. Copy the build output to `Duckov_Data/Mods/TradingCardMod/`
2. Launch the game through Steam
3. Enable the mod in the Mods menu
4. Check the game's log for `[TradingCardMod]` messages

## Troubleshooting

### "CardSets directory not found"
The mod will create this directory automatically. Add your card sets there.

### Cards not appearing
- Check that `cards.txt` follows the exact format (7 pipe-separated fields)
- Ensure image files exist in the `images/` subfolder
- Check the game log for parsing errors

### Build errors
- Verify `DuckovPath` in the .csproj points to your actual game installation
- Ensure you have .NET SDK installed with `dotnet --version`

## License

This mod is provided as-is for personal use. Do not distribute copyrighted card artwork.

## Credits

Built using the official Duckov modding framework and building on the awesome work of the AdditionalCollectibles mod.
