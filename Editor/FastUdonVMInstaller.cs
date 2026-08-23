using System;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.ClientBindings;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.VM;

namespace MagmaVRC.UdonVM
{
    [InitializeOnLoad]
    static class FastUdonVMInstaller
    {
        const string HarmonyId = "dev.magmavrc.udonvm";
        static readonly IUdonVMTimeSource TimeSource = new UdonVMTimeSource();
        static bool _active;

        public static bool Patched { get; private set; }

        static FastUdonVMInstaller()
        {
            try
            {
                var harmony = new Harmony(HarmonyId);
                var target = AccessTools.Method(typeof(UdonManager), nameof(UdonManager.ConstructUdonVM));
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(FastUdonVMInstaller), nameof(ConstructPostfix)));
                Patched = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UdonVM] failed to patch UdonManager.ConstructUdonVM: {e.Message}");
            }
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.ExitingEditMode)
                {
                    _active = FastUdonVMSettings.Enabled;
                    FastUdonVMStats.Reset();
                    if (_active) Debug.Log("[UdonVM] Fast VM active for this play session.");
                }
            };
        }

        static void ConstructPostfix(UdonManager __instance, ref IUdonVM __result)
        {
            if (!_active || __result == null) return;
            var wrapper = __instance.GetWrapper();
            if (wrapper == null) return;
            __result = new FastUdonVM(wrapper, TimeSource, FastUdonVMSettings.FusedOpsEnabled);
        }
    }
}
