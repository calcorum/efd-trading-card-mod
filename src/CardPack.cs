using System.Collections.Generic;

namespace TradingCardMod
{
    /// <summary>
    /// Represents a card pack that can be opened to receive random cards.
    /// </summary>
    public class CardPack
    {
        /// <summary>
        /// Display name of the pack.
        /// </summary>
        public string PackName { get; set; } = string.Empty;

        /// <summary>
        /// The card set this pack draws from.
        /// </summary>
        public string SetName { get; set; } = string.Empty;

        /// <summary>
        /// Path to the pack's icon image.
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// Filename of the pack image (before full path is set).
        /// </summary>
        public string ImageFile { get; set; } = string.Empty;

        /// <summary>
        /// In-game currency value of the pack.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Weight for appearing in loot (0 = not lootable).
        /// </summary>
        public float Weight { get; set; } = 0.1f;

        /// <summary>
        /// The slots in this pack, each with their own drop weights.
        /// </summary>
        public List<PackSlot> Slots { get; set; } = new List<PackSlot>();

        /// <summary>
        /// Whether this is an auto-generated default pack.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Generates a unique TypeID for this pack.
        /// Uses hash of set name + pack name for uniqueness.
        /// </summary>
        public int GenerateTypeID()
        {
            // Start from 300000 range for packs (cards are 100000+, storage is 200000+)
            string uniqueKey = $"CardPack_{SetName}_{PackName}";
            return 300000 + System.Math.Abs(uniqueKey.GetHashCode() % 100000);
        }
    }

    /// <summary>
    /// Represents a single slot in a card pack with drop weights.
    /// Each slot can use either rarity-based or card-specific weights.
    /// </summary>
    public class PackSlot
    {
        /// <summary>
        /// Whether this slot uses rarity-based weights (true) or specific card weights (false).
        /// </summary>
        public bool UseRarityWeights { get; set; } = true;

        /// <summary>
        /// Rarity-based weights. Key = rarity string (e.g., "Common"), Value = weight.
        /// Used when UseRarityWeights is true.
        /// </summary>
        public Dictionary<string, float> RarityWeights { get; set; } = new Dictionary<string, float>();

        /// <summary>
        /// Specific card weights. Key = card name, Value = weight.
        /// Used when UseRarityWeights is false.
        /// </summary>
        public Dictionary<string, float> CardWeights { get; set; } = new Dictionary<string, float>();
    }

    /// <summary>
    /// Default slot configurations for auto-generated packs.
    /// </summary>
    public static class DefaultPackSlots
    {
        /// <summary>
        /// Slot 1: Favors common cards.
        /// </summary>
        public static PackSlot CommonSlot => new PackSlot
        {
            UseRarityWeights = true,
            RarityWeights = new Dictionary<string, float>
            {
                { "Common", 100f },
                { "Uncommon", 30f },
                { "Rare", 5f },
                { "Very Rare", 1f },
                { "Ultra Rare", 0f },
                { "Legendary", 0f }
            }
        };

        /// <summary>
        /// Slot 2: Balanced towards uncommon.
        /// </summary>
        public static PackSlot UncommonSlot => new PackSlot
        {
            UseRarityWeights = true,
            RarityWeights = new Dictionary<string, float>
            {
                { "Common", 60f },
                { "Uncommon", 80f },
                { "Rare", 20f },
                { "Very Rare", 5f },
                { "Ultra Rare", 1f },
                { "Legendary", 1f }
            }
        };

        /// <summary>
        /// Slot 3: Better odds for rare+ cards.
        /// </summary>
        public static PackSlot RareSlot => new PackSlot
        {
            UseRarityWeights = true,
            RarityWeights = new Dictionary<string, float>
            {
                { "Common", 30f },
                { "Uncommon", 60f },
                { "Rare", 60f },
                { "Very Rare", 20f },
                { "Ultra Rare", 5f },
                { "Legendary", 5f }
            }
        };

        /// <summary>
        /// Gets the default slots for an auto-generated pack (3 slots).
        /// </summary>
        public static List<PackSlot> GetDefaultSlots()
        {
            return new List<PackSlot>
            {
                CommonSlot,
                UncommonSlot,
                RareSlot
            };
        }
    }
}
