using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Lokanta.EditorTools
{
    /// <summary>
    /// Turns on Unity's ADVANCED TEXT GENERATOR (ATG).
    ///
    /// WHY IT IS NEEDED: Arabic letters JOIN UP. Text that reads "مرحبا" comes
    /// out of the standard generator with the letters separate and laid out
    /// left to right - unreadable. Joining (init/medi/fina), bidirectional
    /// ordering and line breaking only exist in ATG. The game has five
    /// languages and one of them is Arabic; without this setting that
    /// language is rubbish on screen.
    ///
    /// WHY HERE: the setting is a CHECKBOX (Edit > Project Settings >
    /// UI Toolkit > Enable Advanced Text Generator) and it lives in
    /// ProjectSettings/UIToolkitProjectSettings.asset. A box ticked by hand
    /// is unticked on a machine that has just cloned the repository, and
    /// Arabic breaks SILENTLY - so the build turns it on itself.
    ///
    /// WHY REFLECTION: the UIToolkitProjectSettings class is internal to
    /// Unity, it has no public API. Reflection is brittle, so IF IT CANNOT
    /// FIND IT, IT DOES NOT STAY QUIET - it prints a warning and lands in the
    /// build log.
    /// </summary>
    public static class AdvancedText
    {
        private const string TypeName =
            "UnityEditor.UIElements.UIToolkitProjectSettings, UnityEditor.UIElementsModule";

        [MenuItem("Lokanta/Turn on the advanced text generator")]
        public static void Enable()
        {
            if (Set(true)) Debug.Log("The advanced text generator is ON.");
        }

        /// <summary>
        /// Applies the setting. On failure it prints a warning and returns false.
        /// </summary>
        public static bool Set(bool value)
        {
            System.Type t = System.Type.GetType(TypeName);
            if (t == null)
            {
                Debug.LogWarning(
                    "UIToolkitProjectSettings not found - the advanced text " +
                    "generator could not be set. Arabic may be drawn wrongly.");
                return false;
            }

            PropertyInfo p = t.GetProperty(
                "enableAdvancedText",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (p == null || !p.CanWrite)
            {
                Debug.LogWarning(
                    "UIToolkitProjectSettings.enableAdvancedText could not be written - " +
                    "Arabic may be drawn wrongly.");
                return false;
            }

            p.SetValue(null, value);

            // CHECK IT BY READING IT BACK. Writing through reflection may
            // have silently done nothing; a setting that does not read back
            // what it wrote looks exactly like one that was never set at all.
            object back = p.GetValue(null);
            if (!(back is bool) || (bool)back != value)
            {
                Debug.LogWarning(
                    "The advanced text generator setting did not hold when read back.");
                return false;
            }
            return true;
        }
    }
}
