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
    // Example Harmony Patches
    // ==========================================================================
    //
    // Below are example patches showing common patterns. Uncomment and modify
    // as needed once you've identified the game methods to patch.
    //
    // To find methods to patch, use a decompiler (ILSpy) on the game DLLs in:
    // Duckov_Data/Managed/
    // ==========================================================================

    /*
    /// <summary>
    /// Example: Postfix patch that runs after a method completes.
    /// Use case: Log when items are added to inventory, modify return values.
    /// </summary>
    [HarmonyPatch(typeof(ItemStatsSystem.ItemUtilities), "SendToPlayer")]
    public static class SendToPlayer_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ItemStatsSystem.Item item)
        {
            // Check if the item is one of our trading cards
            // This runs after the original method completes
            Debug.Log($"[TradingCardMod] Item sent to player: {item.name}");
        }
    }

    /// <summary>
    /// Example: Prefix patch that runs before a method.
    /// Use case: Modify parameters, skip original method, validate inputs.
    /// </summary>
    [HarmonyPatch(typeof(SomeClass), "SomeMethod")]
    public static class SomeMethod_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int someParameter)
        {
            // Modify parameter before original method runs
            someParameter = someParameter * 2;

            // Return true to run original method, false to skip it
            return true;
        }
    }

    /// <summary>
    /// Example: Transpiler patch that modifies IL instructions.
    /// Use case: Complex modifications, inserting code mid-method.
    /// Note: Advanced technique - prefer Prefix/Postfix when possible.
    /// </summary>
    [HarmonyPatch(typeof(SomeClass), "SomeMethod")]
    public static class SomeMethod_Transpiler
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Modify and return the IL instructions
            return instructions;
        }
    }
    */
}
