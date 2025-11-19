using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using ItemStatsSystem;
using SodaCraft.Localizations;
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
                // Create our custom tag first
                _tradingCardTag = TagHelper.GetOrCreateTradingCardTag();

                // Load and register cards - do this early so saves can load them
                LoadCardSets();

                // Create storage items
                CreateStorageItems();

                // Build spawnable items list (cards + storage)
                _allSpawnableItems.AddRange(_registeredItems);
                if (_binderItem != null) _allSpawnableItems.Add(_binderItem);
                if (_cardBoxItem != null) _allSpawnableItems.Add(_cardBoxItem);

                Debug.Log("[TradingCardMod] Mod initialized successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Failed to load card sets: {ex.Message}");
                Debug.LogException(ex);
            }
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
            Item itemToSpawn = _allSpawnableItems[_debugSpawnIndex % _allSpawnableItems.Count];
            _debugSpawnIndex++;

            try
            {
                // Use game's utility to give item to player
                ItemUtilities.SendToPlayer(itemToSpawn);
                Debug.Log($"[TradingCardMod] Spawned: {itemToSpawn.DisplayName} (ID: {itemToSpawn.TypeID})");
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
                item.SetPrivateField("value", card.Value);
                item.SetPrivateField("displayName", locKey);
                item.SetPrivateField("quality", card.GetQuality());
                item.SetPrivateField("order", 0);
                item.SetPrivateField("maxStackCount", 1);

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

            // Clean up tags
            TagHelper.Cleanup();

            _loadedCards.Clear();
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
