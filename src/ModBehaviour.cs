using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using ItemStatsSystem;
using SodaCraft.Localizations;
using Duckov.Modding;
using Duckov.Utilities;

namespace TradingCardMod
{
    /// <summary>
    /// Main entry point for the Trading Card Mod.
    /// Inherits from Duckov.Modding.ModBehaviour as required by the mod system.
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private static ModBehaviour? _instance;
        public static ModBehaviour Instance => _instance!;

        /// <summary>
        /// Mod name for ModConfig integration.
        /// </summary>
        public const string MOD_NAME = "TradingCardMod";

        /// <summary>
        /// Gets the list of registered card items (for loot injection).
        /// </summary>
        public List<Item> GetRegisteredItems() => _registeredItems;

        /// <summary>
        /// Gets cards for a specific set (used by PackUsageBehavior).
        /// </summary>
        public List<TradingCard> GetCardsBySet(string setName)
        {
            if (_cardsBySet.TryGetValue(setName, out var cards))
            {
                return cards;
            }
            return new List<TradingCard>();
        }

        /// <summary>
        /// Gets the card name to TypeID mapping (used by PackUsageBehavior).
        /// </summary>
        public Dictionary<string, int> GetCardTypeIds() => _cardNameToTypeId;

        /// <summary>
        /// Gets slot configuration for a specific pack (used by PackUsageBehavior).
        /// </summary>
        public List<PackSlot> GetPackSlots(string setName, string packName)
        {
            string key = $"{setName}|{packName}";
            if (_packDefinitions.TryGetValue(key, out var pack))
            {
                return pack.Slots;
            }
            Debug.LogWarning($"[TradingCardMod] Pack definition not found: {key}");
            return new List<PackSlot>();
        }

        // Base game item ID to clone (135 is commonly used for collectibles)
        private const int BASE_ITEM_ID = 135;

        // Storage item IDs (high range to avoid conflicts)
        private const int BINDER_ITEM_ID = 200001;
        private const int CARD_BOX_ITEM_ID = 200002;

        private string _modPath = string.Empty;
        private List<TradingCard> _loadedCards = new List<TradingCard>();
        private List<Item> _registeredItems = new List<Item>();
        private List<GameObject> _createdGameObjects = new List<GameObject>();
        private Tag? _tradingCardTag;
        private Item? _binderItem;
        private Item? _cardBoxItem;

        // Track cards and IDs per set for pack creation
        private Dictionary<string, List<TradingCard>> _cardsBySet = new Dictionary<string, List<TradingCard>>();
        private Dictionary<string, int> _cardNameToTypeId = new Dictionary<string, int>();
        private List<Item> _registeredPacks = new List<Item>();

        // Store pack definitions for runtime lookup (key = "SetName|PackName")
        private Dictionary<string, CardPack> _packDefinitions = new Dictionary<string, CardPack>();

        // Debug: track spawn cycling
        private int _debugSpawnIndex = 0;
        private List<Item> _allSpawnableItems = new List<Item>();

        /// <summary>
        /// Called when the GameObject is created. Initialize early to register items before saves load.
        /// </summary>
        void Awake()
        {
            _instance = this;

            // Get the mod's directory path
            _modPath = Path.GetDirectoryName(GetType().Assembly.Location) ?? string.Empty;

            Debug.Log("[TradingCardMod] Mod awakening (early init)...");
            Debug.Log($"[TradingCardMod] Mod path: {_modPath}");

            // Apply Harmony patches FIRST - before anything else
            Patches.ApplyPatches();

            try
            {
                // Log all available tags for reference
                TagHelper.LogAvailableTags();

                // Create our custom tag first
                _tradingCardTag = TagHelper.GetOrCreateTradingCardTag();

                // Load and register cards - do this early so saves can load them
                LoadCardSets();

                // Create storage items
                CreateStorageItems();

                // Create card packs
                CreateCardPacks();

                // Build spawnable items list (cards + storage + packs)
                _allSpawnableItems.AddRange(_registeredItems);
                if (_binderItem != null) _allSpawnableItems.Add(_binderItem);
                if (_cardBoxItem != null) _allSpawnableItems.Add(_cardBoxItem);
                _allSpawnableItems.AddRange(_registeredPacks);

                Debug.Log("[TradingCardMod] Mod initialized successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Failed to load card sets: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Called when the mod is enabled. Set up ModConfig integration.
        /// </summary>
        void OnEnable()
        {
            ModManager.OnModActivated += OnModActivated;

            // Check if ModConfig is already loaded
            if (ModConfigAPI.IsAvailable())
            {
                Debug.Log("[TradingCardMod] ModConfig already available");
                SetupModConfig();
            }
        }

        /// <summary>
        /// Called when another mod is activated.
        /// </summary>
        private void OnModActivated(ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            if (info.name == ModConfigAPI.ModConfigName)
            {
                Debug.Log("[TradingCardMod] ModConfig activated");
                SetupModConfig();
            }
        }

        /// <summary>
        /// Called when the mod is disabled. Clean up ModConfig subscriptions.
        /// </summary>
        void OnDisable()
        {
            ModManager.OnModActivated -= OnModActivated;
        }

        /// <summary>
        /// Set up ModConfig to display loaded card sets information.
        /// Uses dropdowns with single options to create read-only displays.
        /// </summary>
        private void SetupModConfig()
        {
            if (!ModConfigAPI.IsAvailable())
            {
                return;
            }

            // Build info string showing loaded card sets
            var setInfo = new List<string>();
            foreach (var setEntry in _cardsBySet)
            {
                setInfo.Add($"{setEntry.Key}: {setEntry.Value.Count} cards");
            }

            string loadedSetsInfo = setInfo.Count > 0
                ? string.Join(", ", setInfo)
                : "No card sets loaded";

            // Use dropdowns with single options to create read-only displays
            // Total cards display
            var cardsOption = new System.Collections.Generic.SortedDictionary<string, object>
            {
                { $"{_loadedCards.Count} cards", _loadedCards.Count }
            };
            ModConfigAPI.SafeAddDropdownList(
                MOD_NAME,
                "TotalCards",
                "Total Cards Loaded",
                cardsOption,
                typeof(int),
                _loadedCards.Count
            );

            // Total packs display
            var packsOption = new System.Collections.Generic.SortedDictionary<string, object>
            {
                { $"{_registeredPacks.Count} packs", _registeredPacks.Count }
            };
            ModConfigAPI.SafeAddDropdownList(
                MOD_NAME,
                "TotalPacks",
                "Total Packs Loaded",
                packsOption,
                typeof(int),
                _registeredPacks.Count
            );

            // Card sets info - one entry per set for clarity
            int setIndex = 0;
            foreach (var setEntry in _cardsBySet)
            {
                var setOption = new System.Collections.Generic.SortedDictionary<string, object>
                {
                    { $"{setEntry.Value.Count} cards", setEntry.Value.Count }
                };
                ModConfigAPI.SafeAddDropdownList(
                    MOD_NAME,
                    $"Set_{setIndex}",
                    $"Set: {setEntry.Key}",
                    setOption,
                    typeof(int),
                    setEntry.Value.Count
                );
                setIndex++;
            }

            Debug.Log("[TradingCardMod] ModConfig setup completed");
        }

        /// <summary>
        /// Scans the CardSets directory and loads all card definitions.
        /// </summary>
        private void LoadCardSets()
        {
            string cardSetsPath = Path.Combine(_modPath, "CardSets");

            if (!Directory.Exists(cardSetsPath))
            {
                Debug.LogWarning($"[TradingCardMod] CardSets directory not found at: {cardSetsPath}");
                Directory.CreateDirectory(cardSetsPath);
                return;
            }

            string[] setDirectories = Directory.GetDirectories(cardSetsPath);
            Debug.Log($"[TradingCardMod] Found {setDirectories.Length} card set directories");

            foreach (string setDir in setDirectories)
            {
                LoadCardSet(setDir);
            }

            Debug.Log($"[TradingCardMod] Total cards loaded: {_loadedCards.Count}");
            Debug.Log($"[TradingCardMod] Total items registered: {_registeredItems.Count}");
            Debug.Log("[TradingCardMod] DEBUG: Press F9 to spawn items (cycles through cards, then binder, then box)");

            // Clear the search cache so our items can be found
            ClearSearchCache();
        }

        /// <summary>
        /// Clears the ItemAssetsCollection search cache so dynamically registered items can be found.
        /// </summary>
        private void ClearSearchCache()
        {
            try
            {
                var cacheField = typeof(ItemAssetsCollection).GetField("cachedSearchResults",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (cacheField != null)
                {
                    var cache = cacheField.GetValue(null) as System.Collections.IDictionary;
                    if (cache != null)
                    {
                        cache.Clear();
                        Debug.Log("[TradingCardMod] Cleared search cache for loot table integration");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TradingCardMod] Could not clear search cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates storage items (binder and card box) for holding trading cards.
        /// </summary>
        private void CreateStorageItems()
        {
            if (_tradingCardTag == null)
            {
                Debug.LogError("[TradingCardMod] Cannot create storage items - TradingCard tag not created!");
                return;
            }

            // Create Card Binder (9 slots = 3x3 grid)
            _binderItem = StorageHelper.CreateCardStorage(
                BINDER_ITEM_ID,
                "Card Binder",
                "A binder for storing and organizing trading cards. Holds 9 cards.",
                9,
                0.5f,  // weight
                500,   // value
                _tradingCardTag
            );

            // Create Card Box (36 slots = bulk storage)
            _cardBoxItem = StorageHelper.CreateCardStorage(
                CARD_BOX_ITEM_ID,
                "Card Box",
                "A large box for bulk storage of trading cards. Holds 36 cards.",
                36,
                2.0f,  // weight
                1500,  // value
                _tradingCardTag
            );
        }

        /// <summary>
        /// Creates card packs for each loaded card set.
        /// </summary>
        private void CreateCardPacks()
        {
            foreach (var setEntry in _cardsBySet)
            {
                string setName = setEntry.Key;
                var cards = setEntry.Value;

                if (cards.Count == 0)
                {
                    Debug.LogWarning($"[TradingCardMod] No cards in set {setName}, skipping pack creation");
                    continue;
                }

                // Create default pack for this set
                string setDirectory = Path.Combine(_modPath, "CardSets", setName);
                string imagesDirectory = Path.Combine(setDirectory, "images");

                CardPack defaultPack = PackParser.CreateDefaultPack(setName, imagesDirectory);

                // Check for user-defined packs
                string packsFile = Path.Combine(setDirectory, "packs.txt");
                var userPacks = PackParser.ParseFile(packsFile, setName, imagesDirectory);

                // Create all packs
                var allPacks = new List<CardPack> { defaultPack };
                allPacks.AddRange(userPacks);

                foreach (var pack in allPacks)
                {
                    // Store pack definition for runtime lookup
                    string packKey = $"{pack.SetName}|{pack.PackName}";
                    _packDefinitions[packKey] = pack;

                    // Load pack icon
                    Sprite? packIcon = null;
                    if (File.Exists(pack.ImagePath))
                    {
                        packIcon = LoadSpriteFromFile(pack.ImagePath, pack.GenerateTypeID());
                    }

                    // Create pack item
                    Item? packItem = PackHelper.CreatePackItem(pack, packIcon);
                    if (packItem != null)
                    {
                        _registeredPacks.Add(packItem);
                    }
                }
            }

            Debug.Log($"[TradingCardMod] Created {_registeredPacks.Count} card packs");
        }

        /// <summary>
        /// Update is called every frame. Used for debug input handling.
        /// </summary>
        void Update()
        {
            // Debug: Press F9 to spawn an item
            if (Input.GetKeyDown(KeyCode.F9))
            {
                SpawnDebugItem();
            }
        }

        /// <summary>
        /// Spawns items for testing - cycles through cards, then storage items.
        /// </summary>
        private void SpawnDebugItem()
        {
            if (_allSpawnableItems.Count == 0)
            {
                Debug.LogWarning("[TradingCardMod] No items registered to spawn!");
                return;
            }

            // Cycle through all spawnable items
            Item prefab = _allSpawnableItems[_debugSpawnIndex % _allSpawnableItems.Count];
            _debugSpawnIndex++;

            try
            {
                // Instantiate a fresh copy of the item (don't send prefab directly)
                Item instance = ItemAssetsCollection.InstantiateSync(prefab.TypeID);
                if (instance != null)
                {
                    // Use game's utility to give item to player
                    ItemUtilities.SendToPlayer(instance);
                    Debug.Log($"[TradingCardMod] Spawned: {instance.DisplayName} (ID: {instance.TypeID})");
                }
                else
                {
                    Debug.LogError($"[TradingCardMod] Failed to instantiate {prefab.DisplayName} (ID: {prefab.TypeID})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Failed to spawn item: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a single card set from a directory.
        /// Expects a cards.txt file with pipe-separated values.
        /// </summary>
        private void LoadCardSet(string setDirectory)
        {
            string setName = Path.GetFileName(setDirectory);
            string cardsFile = Path.Combine(setDirectory, "cards.txt");

            if (!File.Exists(cardsFile))
            {
                Debug.LogWarning($"[TradingCardMod] No cards.txt found in {setName}");
                return;
            }

            Debug.Log($"[TradingCardMod] Loading card set: {setName}");

            try
            {
                // Use CardParser to load cards
                string imagesDirectory = Path.Combine(setDirectory, "images");
                var cards = CardParser.ParseFile(cardsFile, imagesDirectory);

                // Initialize set tracking
                if (!_cardsBySet.ContainsKey(setName))
                {
                    _cardsBySet[setName] = new List<TradingCard>();
                }

                foreach (var card in cards)
                {
                    // Validate card
                    var errors = CardParser.ValidateCard(card);
                    if (errors.Count > 0)
                    {
                        foreach (var error in errors)
                        {
                            Debug.LogWarning($"[TradingCardMod] {card.CardName}: {error}");
                        }
                        continue;
                    }

                    _loadedCards.Add(card);
                    _cardsBySet[setName].Add(card);

                    // Register card as game item
                    RegisterCardWithGame(card);
                }

                Debug.Log($"[TradingCardMod] Loaded {cards.Count} cards from {setName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Error loading card set {setName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a game item for a trading card using clone + reflection.
        /// </summary>
        private void RegisterCardWithGame(TradingCard card)
        {
            try
            {
                // Get base item to clone
                Item original = ItemAssetsCollection.GetPrefab(BASE_ITEM_ID);
                if (original == null)
                {
                    Debug.LogError($"[TradingCardMod] Base item ID {BASE_ITEM_ID} not found!");
                    return;
                }

                // Clone the item
                GameObject clone = UnityEngine.Object.Instantiate(original.gameObject);
                clone.name = $"TradingCard_{card.SetName}_{card.CardName}";
                UnityEngine.Object.DontDestroyOnLoad(clone);
                _createdGameObjects.Add(clone);

                Item item = clone.GetComponent<Item>();
                if (item == null)
                {
                    Debug.LogError($"[TradingCardMod] Cloned object has no Item component!");
                    return;
                }

                // Set item properties via reflection
                int typeId = card.GenerateTypeID();
                string locKey = $"TC_{card.SetName}_{card.CardName}".Replace(" ", "_");

                item.SetPrivateField("typeID", typeId);
                item.SetPrivateField("weight", card.Weight);
                item.SetPrivateField("displayName", locKey);
                item.SetPrivateField("order", 0);
                item.SetPrivateField("maxStackCount", 1);

                // Use public setters for properties that have them
                item.Value = card.Value;
                item.Quality = card.GetQuality();

                // Set display quality based on rarity
                SetDisplayQuality(item, card.GetQuality());

                // Set tags
                item.Tags.Clear();

                // Add Luxury tag (for selling at shops)
                Tag? luxuryTag = TagHelper.GetTargetTag("Luxury");
                if (luxuryTag != null)
                {
                    item.Tags.Add(luxuryTag);
                }

                // ============================================================
                // TODO: REMOVE BEFORE RELEASE - TEST TAGS FOR LOOT SPAWNING
                // These tags make cards appear frequently in loot for testing.
                // Replace with appropriate tags (Collection, Misc, etc.) or
                // implement proper loot table integration before shipping.
                // ============================================================
                Tag? medicTag = TagHelper.GetTargetTag("Medic");
                if (medicTag != null)
                {
                    item.Tags.Add(medicTag);
                }

                Tag? toolTag = TagHelper.GetTargetTag("Tool");
                if (toolTag != null)
                {
                    item.Tags.Add(toolTag);
                }
                // ============================================================

                // Add our custom TradingCard tag
                if (_tradingCardTag != null)
                {
                    item.Tags.Add(_tradingCardTag);
                }

                // Load and set icon
                Sprite? cardSprite = LoadSpriteFromFile(card.ImagePath, typeId);
                if (cardSprite != null)
                {
                    item.SetPrivateField("icon", cardSprite);
                }
                else
                {
                    Debug.LogWarning($"[TradingCardMod] Using default icon for {card.CardName}");
                }

                // Set localization
                LocalizationManager.SetOverrideText(locKey, card.CardName);
                LocalizationManager.SetOverrideText($"{locKey}_Desc", card.GetDescription());

                // Register with game's item system
                if (ItemAssetsCollection.AddDynamicEntry(item))
                {
                    _registeredItems.Add(item);
                    _cardNameToTypeId[card.CardName] = typeId;
                    Debug.Log($"[TradingCardMod] Registered: {card.CardName} (ID: {typeId})");
                }
                else
                {
                    Debug.LogError($"[TradingCardMod] Failed to register {card.CardName}!");
                    UnityEngine.Object.Destroy(clone);
                    _createdGameObjects.Remove(clone);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Error registering {card.CardName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the DisplayQuality enum based on quality level.
        /// </summary>
        private void SetDisplayQuality(Item item, int quality)
        {
            // DisplayQuality is cast from int - matching AdditionalCollectibles pattern
            // Values: 0=Common, 2=Uncommon, 3=Rare, 4=Epic, 5=Legendary, 6=Mythic
            int displayValue;
            switch (quality)
            {
                case 2:
                    displayValue = 2;
                    break;
                case 3:
                    displayValue = 3;
                    break;
                case 4:
                    displayValue = 4;
                    break;
                case 5:
                    displayValue = 5;
                    break;
                case 6:
                case 7:
                    displayValue = 6;
                    break;
                default:
                    displayValue = 0;
                    break;
            }
            item.DisplayQuality = (DisplayQuality)displayValue;
        }

        /// <summary>
        /// Loads a sprite from an image file (PNG/JPG).
        /// </summary>
        private Sprite? LoadSpriteFromFile(string imagePath, int itemId)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    Debug.LogWarning($"[TradingCardMod] Image not found: {imagePath}");
                    return null;
                }

                // Read image bytes
                byte[] imageData = File.ReadAllBytes(imagePath);

                // Create texture and load image
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, imageData))
                {
                    Debug.LogError($"[TradingCardMod] Failed to load image (not PNG/JPG?): {imagePath}");
                    return null;
                }

                texture.filterMode = FilterMode.Bilinear;
                texture.Apply();

                // Create sprite from texture
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                // Create holder to keep texture/sprite alive
                GameObject holder = new GameObject($"CardIcon_{itemId}");
                UnityEngine.Object.DontDestroyOnLoad(holder);
                _createdGameObjects.Add(holder);

                // Store references on the holder to prevent GC
                var resourceHolder = holder.AddComponent<CardResourceHolder>();
                resourceHolder.Texture = texture;
                resourceHolder.Sprite = sprite;

                return sprite;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Error loading sprite: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Called when the mod is unloaded. Clean up registered items.
        /// </summary>
        void OnDestroy()
        {
            Debug.Log("[TradingCardMod] Mod unloading, cleaning up...");

            // Remove Harmony patches
            Patches.RemovePatches();

            // Remove registered items from game
            foreach (var item in _registeredItems)
            {
                if (item != null)
                {
                    ItemAssetsCollection.RemoveDynamicEntry(item);
                }
            }
            _registeredItems.Clear();

            // Destroy created GameObjects (including icon holders)
            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }
            _createdGameObjects.Clear();

            // Clean up storage items
            StorageHelper.Cleanup();

            // Clean up packs
            PackHelper.Cleanup();

            // Clean up tags
            TagHelper.Cleanup();

            _loadedCards.Clear();
            _cardsBySet.Clear();
            _cardNameToTypeId.Clear();
            _registeredPacks.Clear();
            _packDefinitions.Clear();
            _allSpawnableItems.Clear();

            Debug.Log("[TradingCardMod] Cleanup complete.");
        }
    }

    /// <summary>
    /// Component to hold texture and sprite references to prevent garbage collection.
    /// </summary>
    public class CardResourceHolder : MonoBehaviour
    {
        public Texture2D? Texture;
        public Sprite? Sprite;
    }
}
