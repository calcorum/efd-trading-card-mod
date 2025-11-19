# Deploy to Game

Build and deploy the mod to the game's Mods folder.

1. Run `dotnet build -c Release`
2. Copy the following to `{DuckovPath}/Duckov_Data/Mods/TradingCardMod/`:
   - `TradingCardMod.dll`
   - `info.ini`
   - `preview.png` (if exists)
   - `CardSets/` folder
3. Report deployment status
