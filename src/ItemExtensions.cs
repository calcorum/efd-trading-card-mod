using System;
using System.Reflection;
using UnityEngine;

namespace TradingCardMod
{
    /// <summary>
    /// Extension methods for reflection-based field access on Unity objects.
    /// Used to set private fields on cloned game items since we can't use
    /// constructors or public setters for internal game types.
    /// </summary>
    public static class ItemExtensions
    {
        /// <summary>
        /// Sets a private field value on an object using reflection.
        /// </summary>
        /// <typeparam name="T">The type of the value to set.</typeparam>
        /// <param name="obj">The object containing the field.</param>
        /// <param name="fieldName">The name of the private field.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
        /// <exception cref="ArgumentException">Thrown when field is not found.</exception>
        public static void SetPrivateField<T>(this object obj, string fieldName, T value)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            Type type = obj.GetType();
            FieldInfo field = null;

            // Search up the inheritance hierarchy for the field
            while (type != null && field == null)
            {
                field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type = type.BaseType;
            }

            if (field == null)
            {
                throw new ArgumentException(
                    $"Field '{fieldName}' not found on type '{obj.GetType().Name}' or its base types.");
            }

            field.SetValue(obj, value);
        }

        /// <summary>
        /// Gets a private field value from an object using reflection.
        /// </summary>
        /// <typeparam name="T">The expected type of the field value.</typeparam>
        /// <param name="obj">The object containing the field.</param>
        /// <param name="fieldName">The name of the private field.</param>
        /// <returns>The field value cast to type T.</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
        /// <exception cref="ArgumentException">Thrown when field is not found.</exception>
        /// <exception cref="InvalidCastException">Thrown when field value cannot be cast to T.</exception>
        public static T GetPrivateField<T>(this object obj, string fieldName)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            Type type = obj.GetType();
            FieldInfo field = null;

            // Search up the inheritance hierarchy for the field
            while (type != null && field == null)
            {
                field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                type = type.BaseType;
            }

            if (field == null)
            {
                throw new ArgumentException(
                    $"Field '{fieldName}' not found on type '{obj.GetType().Name}' or its base types.");
            }

            return (T)field.GetValue(obj);
        }

        /// <summary>
        /// Attempts to set a private field value, logging errors instead of throwing.
        /// Useful for non-critical field assignments where failure shouldn't halt execution.
        /// </summary>
        /// <typeparam name="T">The type of the value to set.</typeparam>
        /// <param name="obj">The object containing the field.</param>
        /// <param name="fieldName">The name of the private field.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool TrySetPrivateField<T>(this object obj, string fieldName, T value)
        {
            try
            {
                obj.SetPrivateField(fieldName, value);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TradingCardMod] Failed to set field '{fieldName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to get a private field value, returning default on failure.
        /// Useful for optional field access where failure shouldn't halt execution.
        /// </summary>
        /// <typeparam name="T">The expected type of the field value.</typeparam>
        /// <param name="obj">The object containing the field.</param>
        /// <param name="fieldName">The name of the private field.</param>
        /// <param name="defaultValue">The default value to return on failure.</param>
        /// <returns>The field value if successful, otherwise defaultValue.</returns>
        public static T TryGetPrivateField<T>(this object obj, string fieldName, T defaultValue = default)
        {
            try
            {
                return obj.GetPrivateField<T>(fieldName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TradingCardMod] Failed to get field '{fieldName}': {ex.Message}");
                return defaultValue;
            }
        }
    }
}
