using System;
using HarmonyLib;
using UnityEngine;

namespace TradingCardMod
{
    /// <summary>
    /// Contains all Harmony patches for the Trading Card Mod.
    /// Patches are applied in ModBehaviour.Start() via Harmony.PatchAll().
    /// </summary>
    public static class Patches
    {
        /// <summary>
        /// Unique Harmony ID for this mod. Used to identify and unpatch if needed.
        /// </summary>
        public const string HarmonyId = "com.manticorum.tradingcardgame.duckov";

        private static Harmony? _harmony;

        /// <summary>
        /// Apply all Harmony patches defined in this assembly.
        /// </summary>
        public static void ApplyPatches()
        {
            try
            {
                _harmony = new Harmony(HarmonyId);
                _harmony.PatchAll();
                Debug.Log($"[TradingCardMod] Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Failed to apply Harmony patches: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Remove all Harmony patches applied by this mod.
        /// Called during mod unload to clean up.
        /// </summary>
        public static void RemovePatches()
        {
            try
            {
                _harmony?.UnpatchAll(HarmonyId);
                Debug.Log($"[TradingCardMod] Harmony patches removed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TradingCardMod] Failed to remove Harmony patches: {ex.Message}");
            }
        }
    }

    // ==========================================================================
    // Safety Patches - Prevent crashes from missing mod items
    // ==========================================================================

    /// <summary>
    /// Patch to prevent crashes when loading saves with mod items that aren't registered yet.
    /// Logs a warning for missing mod items instead of letting the game crash.
    /// </summary>
    [HarmonyPatch(typeof(ItemStatsSystem.ItemAssetsCollection), "GetPrefab", new Type[] { typeof(int) })]
    public static class GetPrefab_SafetyPatch
    {
        [HarmonyPostfix]
        public static void Postfix(int typeID, ItemStatsSystem.Item __result)
        {
            // Check if this TypeID is in our mod's range and wasn't found
            if (typeID >= 100000 && __result == null)
            {
                Debug.LogWarning($"[TradingCardMod] Item TypeID {typeID} not found. Item was likely saved before mod loaded. It will be lost.");
            }
        }
    }
}
