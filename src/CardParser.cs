using System;
using System.Collections.Generic;
using System.IO;

namespace TradingCardMod
{
    /// <summary>
    /// Handles parsing of card definition files.
    /// This class contains no Unity dependencies and is fully testable.
    /// </summary>
    public static class CardParser
    {
        /// <summary>
        /// Maps rarity string to game quality level.
        /// Quality levels: 2=Common, 3=Uncommon, 4=Rare, 5=Very Rare, 6=Ultra Rare/Legendary
        /// </summary>
        public static int RarityToQuality(string rarity)
        {
            return rarity.Trim().ToLowerInvariant() switch
            {
                "common" => 2,
                "uncommon" => 3,
                "rare" => 4,
                "very rare" => 5,
                "ultra rare" => 6,
                "legendary" => 6,
                _ => 3 // Default to uncommon
            };
        }

        /// <summary>
        /// Parses a single line from cards.txt into a TradingCard object.
        /// Format: CardName | SetName | SetNumber | ImageFile | Rarity | Weight | Value | Description (optional)
        /// </summary>
        /// <param name="line">The line to parse</param>
        /// <returns>A TradingCard if parsing succeeds, null otherwise</returns>
        public static TradingCard? ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // Skip comments
            if (line.TrimStart().StartsWith("#"))
                return null;

            string[] parts = line.Split('|');

            if (parts.Length < 7)
                return null;

            try
            {
                var card = new TradingCard
                {
                    CardName = parts[0].Trim(),
                    SetName = parts[1].Trim(),
                    SetNumber = int.Parse(parts[2].Trim()),
                    ImageFile = parts[3].Trim(),
                    Rarity = parts[4].Trim(),
                    Weight = float.Parse(parts[5].Trim()),
                    Value = int.Parse(parts[6].Trim())
                };

                // Optional description field
                if (parts.Length >= 8 && !string.IsNullOrWhiteSpace(parts[7]))
                {
                    card.Description = parts[7].Trim();
                }

                return card;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parses all cards from a cards.txt file.
        /// </summary>
        /// <param name="filePath">Path to the cards.txt file</param>
        /// <param name="imagesDirectory">Directory containing card images</param>
        /// <returns>List of parsed cards with ImagePath set</returns>
        public static List<TradingCard> ParseFile(string filePath, string imagesDirectory)
        {
            var cards = new List<TradingCard>();

            if (!File.Exists(filePath))
                return cards;

            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                TradingCard? card = ParseLine(line);
                if (card != null)
                {
                    card.ImagePath = Path.Combine(imagesDirectory, card.ImageFile);
                    cards.Add(card);
                }
            }

            return cards;
        }

        /// <summary>
        /// Validates a TradingCard for required fields.
        /// </summary>
        /// <param name="card">The card to validate</param>
        /// <returns>List of validation errors, empty if valid</returns>
        public static List<string> ValidateCard(TradingCard card)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(card.CardName))
                errors.Add("CardName is required");

            if (string.IsNullOrWhiteSpace(card.SetName))
                errors.Add("SetName is required");

            if (card.SetNumber < 0)
                errors.Add("SetNumber must be non-negative");

            if (string.IsNullOrWhiteSpace(card.ImageFile))
                errors.Add("ImageFile is required");

            if (string.IsNullOrWhiteSpace(card.Rarity))
                errors.Add("Rarity is required");

            if (card.Weight < 0)
                errors.Add("Weight must be non-negative");

            if (card.Value < 0)
                errors.Add("Value must be non-negative");

            return errors;
        }

        /// <summary>
        /// Checks if a rarity string is valid.
        /// </summary>
        public static bool IsValidRarity(string rarity)
        {
            string normalized = rarity.Trim().ToLowerInvariant();
            return normalized switch
            {
                "common" => true,
                "uncommon" => true,
                "rare" => true,
                "very rare" => true,
                "ultra rare" => true,
                "legendary" => true,
                _ => false
            };
        }
    }
}
