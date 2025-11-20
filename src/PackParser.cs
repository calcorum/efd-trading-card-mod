using System;
using System.Collections.Generic;
using System.IO;

namespace TradingCardMod
{
    /// <summary>
    /// Parses pack definition files (packs.txt) into CardPack objects.
    /// </summary>
    public static class PackParser
    {
        /// <summary>
        /// Parses a packs.txt file into a list of CardPack objects.
        /// </summary>
        /// <param name="filePath">Path to the packs.txt file.</param>
        /// <param name="setName">The card set name these packs belong to.</param>
        /// <param name="imagesDirectory">Directory containing pack images.</param>
        /// <returns>List of parsed CardPack objects.</returns>
        public static List<CardPack> ParseFile(string filePath, string setName, string imagesDirectory)
        {
            var packs = new List<CardPack>();

            if (!File.Exists(filePath))
            {
                return packs;
            }

            string[] lines = File.ReadAllLines(filePath);
            CardPack? currentPack = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // Check if this is a slot definition (indented line)
                bool isIndented = line.StartsWith("  ") || line.StartsWith("\t");

                if (isIndented && currentPack != null)
                {
                    // Parse slot definition
                    PackSlot? slot = ParseSlotLine(trimmedLine);
                    if (slot != null)
                    {
                        currentPack.Slots.Add(slot);
                    }
                }
                else if (!isIndented)
                {
                    // Parse pack header line
                    CardPack? pack = ParsePackHeader(trimmedLine, setName, imagesDirectory);
                    if (pack != null)
                    {
                        if (currentPack != null)
                        {
                            packs.Add(currentPack);
                        }
                        currentPack = pack;
                    }
                }
            }

            // Don't forget the last pack
            if (currentPack != null)
            {
                packs.Add(currentPack);
            }

            return packs;
        }

        /// <summary>
        /// Parses a pack header line.
        /// Format: PackName | Image | Value
        /// </summary>
        private static CardPack? ParsePackHeader(string line, string setName, string imagesDirectory)
        {
            string[] parts = line.Split('|');

            if (parts.Length < 3)
            {
                return null;
            }

            string packName = parts[0].Trim();
            string imageFile = parts[1].Trim();

            if (!int.TryParse(parts[2].Trim(), out int value))
            {
                value = 100;
            }

            float weight = 0.1f;
            if (parts.Length >= 4 && float.TryParse(parts[3].Trim(), out float parsedWeight))
            {
                weight = parsedWeight;
            }

            return new CardPack
            {
                PackName = packName,
                SetName = setName,
                ImageFile = imageFile,
                ImagePath = Path.Combine(imagesDirectory, imageFile),
                Value = value,
                Weight = weight,
                IsDefault = false
            };
        }

        /// <summary>
        /// Parses a slot definition line.
        /// Format: RARITY: Common:100, Uncommon:50, Rare:10
        ///     or: CARDS: CardName:100, CardName:50
        /// </summary>
        private static PackSlot? ParseSlotLine(string line)
        {
            var slot = new PackSlot();

            if (line.StartsWith("RARITY:", StringComparison.OrdinalIgnoreCase))
            {
                slot.UseRarityWeights = true;
                string weightsStr = line.Substring(7).Trim();
                slot.RarityWeights = ParseWeights(weightsStr);
            }
            else if (line.StartsWith("CARDS:", StringComparison.OrdinalIgnoreCase))
            {
                slot.UseRarityWeights = false;
                string weightsStr = line.Substring(6).Trim();
                slot.CardWeights = ParseWeights(weightsStr);
            }
            else
            {
                // Default to rarity weights if no prefix
                slot.UseRarityWeights = true;
                slot.RarityWeights = ParseWeights(line);
            }

            return slot;
        }

        /// <summary>
        /// Parses a comma-separated list of Name:Weight pairs.
        /// </summary>
        private static Dictionary<string, float> ParseWeights(string weightsStr)
        {
            var weights = new Dictionary<string, float>();

            string[] pairs = weightsStr.Split(',');
            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split(':');
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    if (float.TryParse(keyValue[1].Trim(), out float weight))
                    {
                        weights[key] = weight;
                    }
                }
            }

            return weights;
        }

        /// <summary>
        /// Creates a default pack for a card set.
        /// </summary>
        /// <param name="setName">The card set name.</param>
        /// <param name="imagesDirectory">Directory for pack images.</param>
        /// <returns>A default CardPack with standard slot weights.</returns>
        public static CardPack CreateDefaultPack(string setName, string imagesDirectory)
        {
            string packName = $"{setName} Pack";
            string imageFile = "pack.png";

            return new CardPack
            {
                PackName = packName,
                SetName = setName,
                ImageFile = imageFile,
                ImagePath = Path.Combine(imagesDirectory, imageFile),
                Value = 100,
                Weight = 0.1f,
                IsDefault = true,
                Slots = DefaultPackSlots.GetDefaultSlots()
            };
        }

        /// <summary>
        /// Validates a CardPack and returns any errors found.
        /// </summary>
        public static List<string> ValidatePack(CardPack pack)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(pack.PackName))
            {
                errors.Add("Pack name is required");
            }

            if (pack.Slots.Count == 0)
            {
                errors.Add("Pack must have at least one slot");
            }

            foreach (var slot in pack.Slots)
            {
                if (slot.UseRarityWeights && slot.RarityWeights.Count == 0)
                {
                    errors.Add("Rarity slot must have at least one weight defined");
                }
                else if (!slot.UseRarityWeights && slot.CardWeights.Count == 0)
                {
                    errors.Add("Card slot must have at least one card defined");
                }
            }

            if (pack.Value < 0)
            {
                errors.Add("Pack value must be non-negative");
            }

            return errors;
        }
    }
}
