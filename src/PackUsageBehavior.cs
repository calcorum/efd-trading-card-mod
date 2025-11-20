using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using UnityEngine;

namespace TradingCardMod
{
    /// <summary>
    /// Custom usage behavior for card packs that generates multiple cards based on slot weights.
    /// </summary>
    public class PackUsageBehavior : UsageBehavior
    {
        /// <summary>
        /// The card set this pack draws from.
        /// </summary>
        public string SetName = string.Empty;

        /// <summary>
        /// The pack name within the set (for looking up slot config).
        /// </summary>
        public string PackName = string.Empty;

        private bool _running;

        /// <summary>
        /// Gets available cards for this set from ModBehaviour.
        /// </summary>
        private List<TradingCard> GetAvailableCards()
        {
            if (ModBehaviour.Instance == null) return new List<TradingCard>();
            return ModBehaviour.Instance.GetCardsBySet(SetName);
        }

        /// <summary>
        /// Gets card type IDs from ModBehaviour.
        /// </summary>
        private Dictionary<string, int> GetCardTypeIds()
        {
            if (ModBehaviour.Instance == null) return new Dictionary<string, int>();
            return ModBehaviour.Instance.GetCardTypeIds();
        }

        /// <summary>
        /// Gets slot configuration from ModBehaviour.
        /// </summary>
        private List<PackSlot> GetSlots()
        {
            if (ModBehaviour.Instance == null) return new List<PackSlot>();
            return ModBehaviour.Instance.GetPackSlots(SetName, PackName);
        }

        public override DisplaySettingsData DisplaySettings => new DisplaySettingsData
        {
            display = true,
            description = $"Open to receive {GetSlots().Count} trading cards"
        };

        public override bool CanBeUsed(Item item, object user)
        {
            if (!(user is CharacterMainControl))
            {
                return false;
            }
            return true;
        }

        protected override void OnUse(Item item, object user)
        {
            CharacterMainControl character = user as CharacterMainControl;
            var slots = GetSlots();
            if (character != null && slots.Count > 0)
            {
                GenerateCards(item, character, slots).Forget();
            }
            else
            {
                Debug.LogWarning($"[TradingCardMod] Cannot open pack: character={character != null}, slots={slots.Count}");
            }
        }

        private async UniTask GenerateCards(Item packItem, CharacterMainControl character, List<PackSlot> slots)
        {
            if (_running)
            {
                return;
            }
            _running = true;

            var generatedCards = new List<string>();

            try
            {
                foreach (var slot in slots)
                {
                    int? cardTypeId = RollSlot(slot);
                    if (cardTypeId.HasValue)
                    {
                        Item card = await ItemAssetsCollection.InstantiateAsync(cardTypeId.Value);
                        if (card != null)
                        {
                            string cardName = card.DisplayName;
                            generatedCards.Add(cardName);

                            bool pickedUp = character.PickupItem(card);
                            if (!pickedUp && card != null)
                            {
                                if (card.ActiveAgent != null)
                                {
                                    card.AgentUtilities.ReleaseActiveAgent();
                                }
                                PlayerStorage.Push(card);
                            }
                        }
                    }
                }

                // Show notification
                if (generatedCards.Count > 0)
                {
                    string message = $"Received: {string.Join(", ", generatedCards)}";
                    Debug.Log($"[TradingCardMod] Pack opened: {message}");
                    // NotificationText.Push(message); // Uncomment if NotificationText is accessible
                }

                // Consume the pack after successfully generating cards
                ConsumeItem(packItem);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Error generating cards from pack: {ex.Message}");
            }

            _running = false;
        }

        /// <summary>
        /// Consumes one pack from the stack or detaches if only one left.
        /// </summary>
        private void ConsumeItem(Item item)
        {
            if (item == null) return;

            if (item.StackCount > 1)
            {
                item.StackCount -= 1;
            }
            else
            {
                item.Detach();
            }
        }

        /// <summary>
        /// Rolls a single slot to determine which card to give.
        /// </summary>
        private int? RollSlot(PackSlot slot)
        {
            if (slot.UseRarityWeights)
            {
                return RollByRarity(slot.RarityWeights);
            }
            else
            {
                return RollByCardName(slot.CardWeights);
            }
        }

        /// <summary>
        /// Selects a card based on rarity weights.
        /// </summary>
        private int? RollByRarity(Dictionary<string, float> rarityWeights)
        {
            // Build weighted list of cards based on rarity
            var weightedCards = new List<(int typeId, float weight)>();
            var availableCards = GetAvailableCards();
            var cardTypeIds = GetCardTypeIds();

            foreach (var card in availableCards)
            {
                if (cardTypeIds.TryGetValue(card.CardName, out int typeId))
                {
                    // Get the weight for this card's rarity
                    if (rarityWeights.TryGetValue(card.Rarity, out float weight) && weight > 0)
                    {
                        weightedCards.Add((typeId, weight));
                    }
                }
            }

            if (weightedCards.Count == 0)
            {
                Debug.LogWarning($"[TradingCardMod] No cards available for rarity roll (set: {SetName}, cards: {availableCards.Count})");
                return null;
            }

            return WeightedRandom(weightedCards);
        }

        /// <summary>
        /// Selects a card based on specific card name weights.
        /// </summary>
        private int? RollByCardName(Dictionary<string, float> cardWeights)
        {
            var weightedCards = new List<(int typeId, float weight)>();
            var cardTypeIds = GetCardTypeIds();

            foreach (var kvp in cardWeights)
            {
                if (cardTypeIds.TryGetValue(kvp.Key, out int typeId))
                {
                    weightedCards.Add((typeId, kvp.Value));
                }
                else
                {
                    Debug.LogWarning($"[TradingCardMod] Card '{kvp.Key}' not found for pack slot");
                }
            }

            if (weightedCards.Count == 0)
            {
                Debug.LogWarning("[TradingCardMod] No cards available for card name roll");
                return null;
            }

            return WeightedRandom(weightedCards);
        }

        /// <summary>
        /// Performs weighted random selection.
        /// </summary>
        private int WeightedRandom(List<(int typeId, float weight)> items)
        {
            float totalWeight = items.Sum(x => x.weight);
            float roll = UnityEngine.Random.Range(0f, totalWeight);

            float cumulative = 0f;
            foreach (var item in items)
            {
                cumulative += item.weight;
                if (roll <= cumulative)
                {
                    return item.typeId;
                }
            }

            // Fallback to last item
            return items[items.Count - 1].typeId;
        }
    }
}
