using System;
using System.Collections.Generic;
using UnityEngine;
using ItemStatsSystem;
using SodaCraft.Localizations;
using Duckov.Utilities;

namespace TradingCardMod
{
    /// <summary>
    /// Helper class for creating card pack items.
    /// </summary>
    public static class PackHelper
    {
        // Base item to clone for packs (same as cards)
        private const int BASE_ITEM_ID = 135;

        // Track created packs for cleanup
        private static readonly List<Item> _createdPacks = new List<Item>();
        private static readonly List<GameObject> _createdGameObjects = new List<GameObject>();

        /// <summary>
        /// Creates a card pack item with gacha functionality.
        /// </summary>
        public static Item? CreatePackItem(
            CardPack pack,
            Sprite? icon = null)
        {
            try
            {
                // Get base item to clone
                Item original = ItemAssetsCollection.GetPrefab(BASE_ITEM_ID);
                if (original == null)
                {
                    Debug.LogError($"[TradingCardMod] Base item ID {BASE_ITEM_ID} not found for pack!");
                    return null;
                }

                // Clone the item
                GameObject clone = UnityEngine.Object.Instantiate(original.gameObject);
                clone.name = $"CardPack_{pack.SetName}_{pack.PackName}";
                UnityEngine.Object.DontDestroyOnLoad(clone);
                _createdGameObjects.Add(clone);

                Item item = clone.GetComponent<Item>();
                if (item == null)
                {
                    Debug.LogError("[TradingCardMod] Cloned pack object has no Item component!");
                    return null;
                }

                // Set item properties
                int typeId = pack.GenerateTypeID();
                string locKey = $"TC_Pack_{pack.SetName}_{pack.PackName}".Replace(" ", "_");

                item.SetPrivateField("typeID", typeId);
                item.SetPrivateField("weight", pack.Weight);
                item.SetPrivateField("displayName", locKey);
                item.SetPrivateField("order", 0);
                item.SetPrivateField("maxStackCount", 10); // Packs can stack

                // Use public setters
                item.Value = pack.Value;
                item.Quality = 3; // Uncommon quality for packs
                item.DisplayQuality = (DisplayQuality)3;

                // Set tags
                item.Tags.Clear();

                // Add Misc tag for loot spawning
                Tag? miscTag = TagHelper.GetTargetTag("Misc");
                if (miscTag != null)
                {
                    item.Tags.Add(miscTag);
                }

                // Set icon if provided
                if (icon != null)
                {
                    item.SetPrivateField("icon", icon);
                }

                // Set up UsageUtilities for the "Use" context menu
                // First, get or create UsageUtilities component
                UsageUtilities usageUtils = clone.GetComponent<UsageUtilities>();
                if (usageUtils == null)
                {
                    usageUtils = clone.AddComponent<UsageUtilities>();
                }

                // Clear any existing behaviors from the cloned base item
                usageUtils.behaviors.Clear();

                // Add our custom usage behavior for gacha
                var usageBehavior = clone.AddComponent<PackUsageBehavior>();
                usageBehavior.SetName = pack.SetName;
                usageBehavior.PackName = pack.PackName;
                // Note: Slots, AvailableCards, and CardTypeIds are looked up at runtime via ModBehaviour

                // Register our behavior with UsageUtilities
                usageUtils.behaviors.Add(usageBehavior);

                // Set the Item's usageUtilities field to enable the Use option
                item.SetPrivateField("usageUtilities", usageUtils);

                // Set localization
                LocalizationManager.SetOverrideText(locKey, pack.PackName);
                string slotDesc = pack.Slots.Count == 1 ? "1 card" : $"{pack.Slots.Count} cards";
                LocalizationManager.SetOverrideText($"{locKey}_Desc",
                    $"Open to receive {slotDesc} from {pack.SetName}");

                // Register with game
                if (ItemAssetsCollection.AddDynamicEntry(item))
                {
                    _createdPacks.Add(item);
                    Debug.Log($"[TradingCardMod] Registered pack: {pack.PackName} (ID: {typeId}, Slots: {pack.Slots.Count})");
                    return item;
                }
                else
                {
                    Debug.LogError($"[TradingCardMod] Failed to register pack {pack.PackName}!");
                    UnityEngine.Object.Destroy(clone);
                    _createdGameObjects.Remove(clone);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Error creating pack {pack.PackName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all pack items created by this helper.
        /// </summary>
        public static IReadOnlyList<Item> GetCreatedPacks()
        {
            return _createdPacks.AsReadOnly();
        }

        /// <summary>
        /// Cleans up all packs created by this helper.
        /// </summary>
        public static void Cleanup()
        {
            foreach (var item in _createdPacks)
            {
                if (item != null)
                {
                    ItemAssetsCollection.RemoveDynamicEntry(item);
                }
            }
            _createdPacks.Clear();

            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go);
                }
            }
            _createdGameObjects.Clear();

            Debug.Log("[TradingCardMod] PackHelper cleaned up.");
        }
    }
}
