using System.Collections.Generic;
using System.Linq;
using Duckov.Utilities;
using SodaCraft.Localizations;
using UnityEngine;

namespace TradingCardMod
{
    /// <summary>
    /// Helper class for working with the game's Tag system.
    /// Tags are ScriptableObjects used for filtering items in slots.
    /// </summary>
    public static class TagHelper
    {
        // Track tags we create for cleanup
        private static readonly List<Tag> _createdTags = new List<Tag>();

        /// <summary>
        /// Gets an existing game tag by name.
        /// </summary>
        /// <param name="tagName">The internal name of the tag (e.g., "Luxury", "Food").</param>
        /// <returns>The Tag if found, null otherwise.</returns>
        public static Tag GetTargetTag(string tagName)
        {
            return Resources.FindObjectsOfTypeAll<Tag>()
                .FirstOrDefault(t => t.name == tagName);
        }

        /// <summary>
        /// Creates a new custom tag by cloning an existing game tag.
        /// If a tag with the same name already exists, returns that tag instead.
        /// </summary>
        /// <param name="tagName">The internal name for the new tag.</param>
        /// <param name="displayName">The localized display name shown to players.</param>
        /// <param name="templateTagName">The name of an existing tag to clone (default: "Luxury").</param>
        /// <returns>The created or existing tag, or null if template not found.</returns>
        public static Tag CreateOrCloneTag(string tagName, string displayName, string templateTagName = "Luxury")
        {
            // Check if tag already exists
            Tag existing = Resources.FindObjectsOfTypeAll<Tag>()
                .FirstOrDefault(t => t.name == tagName);

            if (existing != null)
            {
                Debug.Log($"[TradingCardMod] Tag '{tagName}' already exists, reusing.");
                return existing;
            }

            // Find template tag to clone
            Tag template = Resources.FindObjectsOfTypeAll<Tag>()
                .FirstOrDefault(t => t.name == templateTagName);

            if (template == null)
            {
                Debug.LogError($"[TradingCardMod] Template tag '{templateTagName}' not found. Cannot create '{tagName}'.");
                return null;
            }

            // Clone the template
            Tag newTag = Object.Instantiate(template);
            newTag.name = tagName;
            Object.DontDestroyOnLoad(newTag);

            // Track for cleanup
            _createdTags.Add(newTag);

            // Set localization for display
            LocalizationManager.SetOverrideText($"Tag_{tagName}", displayName);
            LocalizationManager.SetOverrideText($"Tag_{tagName}_Desc", "");

            Debug.Log($"[TradingCardMod] Created custom tag '{tagName}' (display: '{displayName}').");
            return newTag;
        }

        /// <summary>
        /// Gets the "TradingCard" tag, creating it if it doesn't exist.
        /// This is the primary tag used to identify and filter trading cards.
        /// </summary>
        /// <returns>The TradingCard tag.</returns>
        public static Tag GetOrCreateTradingCardTag()
        {
            return CreateOrCloneTag("TradingCard", "Trading Card");
        }

        /// <summary>
        /// Gets the "BinderSheet" tag, creating it if it doesn't exist.
        /// This tag identifies binder sheets that can be stored in card binders.
        /// </summary>
        /// <returns>The BinderSheet tag.</returns>
        public static Tag GetOrCreateBinderSheetTag()
        {
            return CreateOrCloneTag("BinderSheet", "Binder Sheet");
        }

        /// <summary>
        /// Gets all tags created by this mod.
        /// </summary>
        /// <returns>List of tags created by the mod.</returns>
        public static IReadOnlyList<Tag> GetCreatedTags()
        {
            return _createdTags.AsReadOnly();
        }

        /// <summary>
        /// Cleans up all tags created by the mod.
        /// Should be called when the mod is unloaded.
        /// </summary>
        public static void Cleanup()
        {
            foreach (var tag in _createdTags)
            {
                if (tag != null)
                {
                    Object.Destroy(tag);
                }
            }
            _createdTags.Clear();
            Debug.Log("[TradingCardMod] TagHelper cleaned up.");
        }

        /// <summary>
        /// Logs all available tags in the game (for debugging).
        /// </summary>
        public static void LogAvailableTags()
        {
            var tags = Resources.FindObjectsOfTypeAll<Tag>();
            Debug.Log($"[TradingCardMod] Available tags ({tags.Length}):");
            foreach (var tag in tags.OrderBy(t => t.name))
            {
                Debug.Log($"  - {tag.name}");
            }
        }
    }
}
