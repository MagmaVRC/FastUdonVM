using UnityEditor;
using UnityEngine;

namespace MagmaVRC.UdonVM
{
    /// <summary>Editor settings for the fast VM. Changes apply on the next play-mode entry.</summary>
    public static class FastUdonVMSettings
    {
        const string EnabledKey = "MagmaVRC.UdonVM.Enabled";
        const string FusedKey = "MagmaVRC.UdonVM.FusedOps";
        const string MenuEnabled = "VRChat SDK/Udon VM/Use Fast VM";
        const string MenuFused = "VRChat SDK/Udon VM/Fused Ops";
        const string MenuStats = "VRChat SDK/Udon VM/Log Stats";
        const string MenuReset = "VRChat SDK/Udon VM/Reset Stats";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        public static bool FusedOpsEnabled
        {
            get => EditorPrefs.GetBool(FusedKey, true);
            set => EditorPrefs.SetBool(FusedKey, value);
        }

        [MenuItem(MenuEnabled, priority = 2000)]
        static void ToggleEnabled()
        {
            Enabled = !Enabled;
            Debug.Log($"[UdonVM] Fast VM {(Enabled ? "enabled" : "disabled")}; applies on next play-mode entry.");
        }

        [MenuItem(MenuEnabled, true)]
        static bool ValidateEnabled()
        {
            Menu.SetChecked(MenuEnabled, Enabled);
            return true;
        }

        [MenuItem(MenuFused, priority = 2001)]
        static void ToggleFused() => FusedOpsEnabled = !FusedOpsEnabled;

        [MenuItem(MenuFused, true)]
        static bool ValidateFused()
        {
            Menu.SetChecked(MenuFused, FusedOpsEnabled);
            return true;
        }

        [MenuItem(MenuStats, priority = 2020)]
        static void LogStats()
        {
            Debug.Log($"[UdonVM] instructions={FastUdonVMStats.Instructions:N0} externs={FastUdonVMStats.Externs:N0} fused={FastUdonVMStats.FusedHits:N0} fallbacks={FastUdonVMStats.FusedFallbacks:N0}");
        }

        [MenuItem(MenuReset, priority = 2021)]
        static void ResetStats() => FastUdonVMStats.Reset();
    }
}
