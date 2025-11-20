using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TradingCardMod
{
    /// <summary>
    /// ModConfig Safe API Wrapper Class - Provides non-throwing static interfaces.
    /// Adapted from ModConfigExample by FrozenFish259.
    /// </summary>
    public static class ModConfigAPI
    {
        public static string ModConfigName = "ModConfig";

        // Ensure this matches the number of ModConfig.ModBehaviour.VERSION
        private const int ModConfigVersion = 1;

        private static string TAG = $"[TradingCardMod] ModConfig_v{ModConfigVersion}";

        private static Type modBehaviourType;
        private static Type optionsManagerType;
        public static bool isInitialized = false;
        private static bool versionChecked = false;
        private static bool isVersionCompatible = false;

        /// <summary>
        /// Check version compatibility.
        /// </summary>
        private static bool CheckVersionCompatibility()
        {
            if (versionChecked)
                return isVersionCompatible;

            try
            {
                FieldInfo versionField = modBehaviourType.GetField("VERSION", BindingFlags.Public | BindingFlags.Static);
                if (versionField != null && versionField.FieldType == typeof(int))
                {
                    int modConfigVersion = (int)versionField.GetValue(null);
                    isVersionCompatible = (modConfigVersion == ModConfigVersion);

                    if (!isVersionCompatible)
                    {
                        Debug.LogError($"{TAG} Version mismatch! API version: {ModConfigVersion}, ModConfig version: {modConfigVersion}");
                        return false;
                    }

                    Debug.Log($"{TAG} Version check passed: {ModConfigVersion}");
                    versionChecked = true;
                    return true;
                }
                else
                {
                    Debug.LogWarning($"{TAG} Version field not found, skipping version check");
                    isVersionCompatible = true;
                    versionChecked = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Version check failed: {ex.Message}");
                isVersionCompatible = false;
                versionChecked = true;
                return false;
            }
        }

        /// <summary>
        /// Initialize ModConfigAPI, check if necessary functions exist.
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                if (isInitialized)
                    return true;

                modBehaviourType = FindTypeInAssemblies("ModConfig.ModBehaviour");
                if (modBehaviourType == null)
                {
                    Debug.LogWarning($"{TAG} ModConfig.ModBehaviour type not found, ModConfig may not be loaded");
                    return false;
                }

                optionsManagerType = FindTypeInAssemblies("ModConfig.OptionsManager_Mod");
                if (optionsManagerType == null)
                {
                    Debug.LogWarning($"{TAG} ModConfig.OptionsManager_Mod type not found");
                    return false;
                }

                if (!CheckVersionCompatibility())
                {
                    Debug.LogWarning($"{TAG} ModConfig version mismatch!");
                    return false;
                }

                string[] requiredMethods = {
                    "AddDropdownList",
                    "AddInputWithSlider",
                    "AddBoolDropdownList",
                    "AddOnOptionsChangedDelegate",
                    "RemoveOnOptionsChangedDelegate",
                };

                foreach (string methodName in requiredMethods)
                {
                    MethodInfo method = modBehaviourType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                    if (method == null)
                    {
                        Debug.LogError($"{TAG} Required method {methodName} not found");
                        return false;
                    }
                }

                isInitialized = true;
                Debug.Log($"{TAG} ModConfigAPI initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Initialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Find type in all loaded assemblies.
        /// </summary>
        private static Type FindTypeInAssemblies(string typeName)
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (Assembly assembly in assemblies)
                {
                    try
                    {
                        Type type = assembly.GetType(typeName);
                        if (type != null)
                        {
                            Debug.Log($"{TAG} Found type {typeName} in assembly {assembly.FullName}");
                            return type;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Assembly scan failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Safely add options changed event delegate.
        /// </summary>
        public static bool SafeAddOnOptionsChangedDelegate(Action<string> action)
        {
            if (!Initialize())
                return false;

            if (action == null)
                return false;

            try
            {
                MethodInfo method = modBehaviourType.GetMethod("AddOnOptionsChangedDelegate", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] { action });
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Failed to add options changed delegate: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely remove options changed event delegate.
        /// </summary>
        public static bool SafeRemoveOnOptionsChangedDelegate(Action<string> action)
        {
            if (!Initialize())
                return false;

            if (action == null)
                return false;

            try
            {
                MethodInfo method = modBehaviourType.GetMethod("RemoveOnOptionsChangedDelegate", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] { action });
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Failed to remove options changed delegate: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely add input with slider configuration item.
        /// </summary>
        public static bool SafeAddInputWithSlider(string modName, string key, string description, Type valueType, object defaultValue, Vector2? sliderRange = null)
        {
            key = $"{modName}_{key}";

            if (!Initialize())
                return false;

            try
            {
                MethodInfo method = modBehaviourType.GetMethod("AddInputWithSlider", BindingFlags.Public | BindingFlags.Static);

                object[] parameters = sliderRange.HasValue ?
                    new object[] { modName, key, description, valueType, defaultValue, sliderRange.Value } :
                    new object[] { modName, key, description, valueType, defaultValue, null };

                method.Invoke(null, parameters);

                Debug.Log($"{TAG} Added input with slider: {modName}.{key}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Failed to add input with slider {modName}.{key}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely add boolean dropdown list configuration item.
        /// </summary>
        public static bool SafeAddBoolDropdownList(string modName, string key, string description, bool defaultValue)
        {
            key = $"{modName}_{key}";

            if (!Initialize())
                return false;

            try
            {
                MethodInfo method = modBehaviourType.GetMethod("AddBoolDropdownList", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] { modName, key, description, defaultValue });

                Debug.Log($"{TAG} Added bool dropdown: {modName}.{key}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Failed to add bool dropdown {modName}.{key}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely add dropdown list configuration item.
        /// </summary>
        public static bool SafeAddDropdownList(string modName, string key, string description, System.Collections.Generic.SortedDictionary<string, object> options, Type valueType, object defaultValue)
        {
            key = $"{modName}_{key}";

            if (!Initialize())
                return false;

            try
            {
                MethodInfo method = modBehaviourType.GetMethod("AddDropdownList", BindingFlags.Public | BindingFlags.Static);
                method.Invoke(null, new object[] { modName, key, description, options, valueType, defaultValue });

                Debug.Log($"{TAG} Added dropdown list: {modName}.{key}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Failed to add dropdown list {modName}.{key}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Safely load configuration value.
        /// </summary>
        public static T SafeLoad<T>(string mod_name, string key, T defaultValue = default(T))
        {
            key = $"{mod_name}_{key}";

            if (!Initialize())
                return defaultValue;

            if (string.IsNullOrEmpty(key))
                return defaultValue;

            try
            {
                MethodInfo loadMethod = optionsManagerType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                if (loadMethod == null)
                    return defaultValue;

                MethodInfo genericLoadMethod = loadMethod.MakeGenericMethod(typeof(T));
                object result = genericLoadMethod.Invoke(null, new object[] { key, defaultValue });

                return (T)result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Failed to load config {key}: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Check if ModConfig is available.
        /// </summary>
        public static bool IsAvailable()
        {
            return Initialize();
        }
    }
}
