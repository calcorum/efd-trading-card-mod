using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using Duckov.Utilities;
using SodaCraft.Localizations;

namespace TradingCardMod
{
    /// <summary>
    /// Helper class for creating storage items (binders, card boxes) with slot filtering.
    /// </summary>
    public static class StorageHelper
    {
        // Base item ID that has slots (used as template for storage items)
        private const int SLOTTED_ITEM_BASE_ID = 1255;

        // Track created storage items for cleanup
        private static readonly List<Item> _createdStorageItems = new List<Item>();
        private static readonly List<GameObject> _createdGameObjects = new List<GameObject>();

        /// <summary>
        /// Creates a card binder item with slots that only accept TradingCard tagged items.
        /// </summary>
        /// <param name="itemId">Unique ID for this storage item</param>
        /// <param name="displayName">Display name shown to player</param>
        /// <param name="description">Item description</param>
        /// <param name="slotCount">Number of card slots</param>
        /// <param name="weight">Item weight</param>
        /// <param name="value">Item value</param>
        /// <param name="tradingCardTag">The TradingCard tag for filtering</param>
        /// <param name="icon">Optional custom icon sprite</param>
        /// <returns>The created Item, or null on failure</returns>
        public static Item? CreateCardStorage(
            int itemId,
            string displayName,
            string description,
            int slotCount,
            float weight,
            int value,
            Tag tradingCardTag,
            Sprite? icon = null)
        {
            try
            {
                // Get base item with slots
                Item original = ItemAssetsCollection.GetPrefab(SLOTTED_ITEM_BASE_ID);
                if (original == null)
                {
                    Debug.LogError($"[TradingCardMod] Base slotted item ID {SLOTTED_ITEM_BASE_ID} not found!");
                    return null;
                }

                // Clone the item
                GameObject clone = UnityEngine.Object.Instantiate(original.gameObject);
                clone.name = $"CardStorage_{itemId}";
                UnityEngine.Object.DontDestroyOnLoad(clone);
                _createdGameObjects.Add(clone);

                Item item = clone.GetComponent<Item>();
                if (item == null)
                {
                    Debug.LogError("[TradingCardMod] Cloned storage object has no Item component!");
                    return null;
                }

                // Set basic properties
                string locKey = $"TC_Storage_{itemId}";
                item.SetPrivateField("typeID", itemId);
                item.SetPrivateField("weight", weight);
                item.SetPrivateField("value", value);
                item.SetPrivateField("displayName", locKey);
                item.SetPrivateField("quality", 3); // Uncommon quality
                item.SetPrivateField("order", 0);
                item.SetPrivateField("maxStackCount", 1);

                // Set display quality
                item.DisplayQuality = (DisplayQuality)3;

                // Set tags - storage items should be tools
                item.Tags.Clear();
                Tag? toolTag = TagHelper.GetTargetTag("Tool");
                if (toolTag != null)
                {
                    item.Tags.Add(toolTag);
                }

                // Configure slots to only accept TradingCard tagged items
                ConfigureCardSlots(item, tradingCardTag, slotCount);

                // Set icon if provided
                if (icon != null)
                {
                    item.SetPrivateField("icon", icon);
                }

                // Set localization
                LocalizationManager.SetOverrideText(locKey, displayName);
                LocalizationManager.SetOverrideText($"{locKey}_Desc", description);

                // Register with game
                if (ItemAssetsCollection.AddDynamicEntry(item))
                {
                    _createdStorageItems.Add(item);
                    Debug.Log($"[TradingCardMod] Registered storage: {displayName} (ID: {itemId}, Slots: {slotCount})");
                    return item;
                }
                else
                {
                    Debug.LogError($"[TradingCardMod] Failed to register storage {displayName}!");
                    UnityEngine.Object.Destroy(clone);
                    _createdGameObjects.Remove(clone);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Error creating storage {displayName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Configures an item's slots to only accept items with a specific tag.
        /// </summary>
        private static void ConfigureCardSlots(Item item, Tag requiredTag, int slotCount)
        {
            // Get template slot info if available
            Slot templateSlot = new Slot();
            if (item.Slots.Count > 0)
            {
                templateSlot = item.Slots[0];
            }

            // Clear existing slots
            item.Slots.Clear();

            // Create new slots with tag filtering
            for (int i = 0; i < slotCount; i++)
            {
                Slot newSlot = new Slot(templateSlot.Key);
                newSlot.SlotIcon = templateSlot.SlotIcon;

                // Set unique key for each slot
                typeof(Slot).GetField("key", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(newSlot, $"CardSlot{i}");

                // Add tag requirement - only TradingCard items can go in
                newSlot.requireTags.Add(requiredTag);

                item.Slots.Add(newSlot);
            }

            Debug.Log($"[TradingCardMod] Configured {slotCount} slots with TradingCard filter");
        }

        /// <summary>
        /// Gets all storage items created by this helper.
        /// </summary>
        public static IReadOnlyList<Item> GetCreatedStorageItems()
        {
            return _createdStorageItems.AsReadOnly();
        }

        /// <summary>
        /// Cleans up all storage items created by this helper.
        /// </summary>
        public static void Cleanup()
        {
            foreach (var item in _createdStorageItems)
            {
                if (item != null)
                {
                    ItemAssetsCollection.RemoveDynamicEntry(item);
                }
            }
            _createdStorageItems.Clear();

            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }
            _createdGameObjects.Clear();

            Debug.Log("[TradingCardMod] StorageHelper cleaned up.");
        }
    }
}
