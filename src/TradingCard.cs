using System;

namespace TradingCardMod
{
    /// <summary>
    /// Represents a trading card's data loaded from a card set file.
    /// This class contains no Unity dependencies and is fully testable.
    /// </summary>
    public class TradingCard
    {
        public string CardName { get; set; } = string.Empty;
        public string SetName { get; set; } = string.Empty;
        public int SetNumber { get; set; }
        public string ImageFile { get; set; } = string.Empty;
        public string Rarity { get; set; } = string.Empty;
        public float Weight { get; set; }
        public int Value { get; set; }
        public string? Description { get; set; }

        // Set at runtime after loading
        public string ImagePath { get; set; } = string.Empty;

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

        /// <summary>
        /// Gets the quality level (0-6) based on rarity string.
        /// </summary>
        public int GetQuality()
        {
            return CardParser.RarityToQuality(Rarity);
        }

        /// <summary>
        /// Gets the description, auto-generating if not provided.
        /// </summary>
        public string GetDescription()
        {
            if (!string.IsNullOrWhiteSpace(Description))
            {
                return Description;
            }
            return $"{SetName} #{SetNumber:D3} - {Rarity}";
        }
    }
}
