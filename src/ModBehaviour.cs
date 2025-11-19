using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using ItemStatsSystem;
using SodaCraft.Localizations;

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

        private string _modPath = string.Empty;
        private List<TradingCard> _loadedCards = new List<TradingCard>();

        /// <summary>
        /// Called when the mod is loaded. Initialize the card system here.
        /// </summary>
        void Start()
        {
            _instance = this;

            // Get the mod's directory path
            _modPath = Path.GetDirectoryName(GetType().Assembly.Location) ?? string.Empty;

            Debug.Log("[TradingCardMod] Mod initialized!");
            Debug.Log($"[TradingCardMod] Mod path: {_modPath}");

            // Apply Harmony patches
            Patches.ApplyPatches();

            try
            {
                LoadCardSets();
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
        }

        /// <summary>
        /// Loads a single card set from a directory.
        /// Expects a cards.txt file with pipe-separated values.
        /// </summary>
        /// <param name="setDirectory">Path to the card set directory</param>
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
                string[] lines = File.ReadAllLines(cardsFile);
                int cardCount = 0;

                foreach (string line in lines)
                {
                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    TradingCard? card = ParseCardLine(line, setDirectory);
                    if (card != null)
                    {
                        _loadedCards.Add(card);
                        cardCount++;
                        // TODO: Register card with game's item system
                        // RegisterCardWithGame(card);
                    }
                }

                Debug.Log($"[TradingCardMod] Loaded {cardCount} cards from {setName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Error loading card set {setName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a single line from cards.txt into a TradingCard object.
        /// Format: CardName | SetName | SetNumber | ImageFile | Rarity | Weight | Value
        /// </summary>
        private TradingCard? ParseCardLine(string line, string setDirectory)
        {
            string[] parts = line.Split('|');

            if (parts.Length < 7)
            {
                Debug.LogWarning($"[TradingCardMod] Invalid card line (expected 7 fields): {line}");
                return null;
            }

            try
            {
                return new TradingCard
                {
                    CardName = parts[0].Trim(),
                    SetName = parts[1].Trim(),
                    SetNumber = int.Parse(parts[2].Trim()),
                    ImagePath = Path.Combine(setDirectory, "images", parts[3].Trim()),
                    Rarity = parts[4].Trim(),
                    Weight = float.Parse(parts[5].Trim()),
                    Value = int.Parse(parts[6].Trim())
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TradingCardMod] Failed to parse card line: {line} - {ex.Message}");
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

            // TODO: Remove registered items from game
            // foreach (var card in _loadedCards)
            // {
            //     ItemAssetsCollection.RemoveDynamicEntry(card.ItemPrefab);
            // }

            _loadedCards.Clear();
        }
    }

    /// <summary>
    /// Represents a trading card's data loaded from a card set file.
    /// </summary>
    public class TradingCard
    {
        public string CardName { get; set; } = string.Empty;
        public string SetName { get; set; } = string.Empty;
        public int SetNumber { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string Rarity { get; set; } = string.Empty;
        public float Weight { get; set; }
        public int Value { get; set; }

        // TODO: Add Unity prefab reference once we understand the item system better
        // public Item? ItemPrefab { get; set; }

        /// <summary>
        /// Generates a unique TypeID for this card to avoid conflicts.
        /// Uses hash of set name + card name for uniqueness.
        /// </summary>
        public int GenerateTypeID()
        {
            // Start from a high number to avoid conflicts with base game items
            // Use hash to ensure consistency across loads
            string uniqueKey = $"TradingCard_{SetName}_{CardName}";
            return 100000 + Math.Abs(uniqueKey.GetHashCode() % 900000);
        }
    }
}
