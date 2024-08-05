using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WaterRoll
{
    [BepInPlugin("com.maxgamertyper1.waterroll", "Water Roll", "1.0.0")]
    public class WaterRoll : BaseUnityPlugin
    {
        private void Log(string message)
        {
            Logger.LogInfo(message);
        }

        private void Awake()
        {
            // Plugin startup logic
            Log($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            DoPatching();
        }

        private void DoPatching()
        {
            var harmony = new Harmony("com.maxgamertyper1.waterroll");

            Patch(harmony, typeof(Roll), "ReleaseDash", "RollStarted", false);
            Patch(harmony, typeof(Roll), "OnDisable", "RollEnded", false);
        }

        private void OnDestroy()
        {
            Log($"Bye Bye From {PluginInfo.PLUGIN_GUID}");
        }

        private void Patch(Harmony harmony, Type OriginalClass, string OriginalMethod, string PatchMethod, bool prefix)
        {
            MethodInfo MethodToPatch = AccessTools.Method(OriginalClass, OriginalMethod); // the method to patch
            MethodInfo Patch = AccessTools.Method(typeof(Patches), PatchMethod);
            if (prefix)
            {
                harmony.Patch(MethodToPatch, new HarmonyMethod(Patch));
            }
            else
            {
                harmony.Patch(MethodToPatch, null, new HarmonyMethod(Patch));
            }
            Log($"Patched {OriginalMethod} in {OriginalClass.ToString()}");
        }

    }
    public class Patches
    {
        public static void RollStarted(ref Roll __instance)
        {
            DestroyIfOutsideSceneBounds kill = __instance.GetComponent<DestroyIfOutsideSceneBounds>();
            kill.enabled = false;
        }
        public static void RollEnded(ref Roll __instance)
        {
            DestroyIfOutsideSceneBounds kill = __instance.GetComponent<DestroyIfOutsideSceneBounds>();
            kill.enabled = true;
        }
    }
}
