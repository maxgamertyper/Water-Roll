using BepInEx;
using BepInEx.Configuration;
using BoplFixedMath;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace WaterRoll
{
    [BepInPlugin("com.maxgamertyper1.waterroll", "Water Roll", "2.0.0")]
    public class WaterRoll : BaseUnityPlugin
    {
        internal static ConfigFile config;
        internal static ConfigEntry<bool> WaterBoost;
        internal static ConfigEntry<bool> AntiStalemate;
        internal static ConfigEntry<float> StalemateTime;

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

            config = ((BaseUnityPlugin)this).Config;
            WaterBoost = config.Bind<bool>("General", "Water Boost", true, "if the roll ends in the water, it will boost you out");
            AntiStalemate = config.Bind<bool>("Stalemate", "AntiStalemate", true, "if the player is stuck in the water for x (Stalemate Time) amount of time, they will die");
            StalemateTime = config.Bind<float>("Stalemate", "Stalemate Time", 20f, "The amount of time before the player is killed from AntiStalemate");

            Patches.AntiStalemate = AntiStalemate.Value;
            Patches.StalemateTime = StalemateTime.Value;


            Patch(harmony, typeof(Roll), "ReleaseDash", "RollStarted", false);
            Patch(harmony, typeof(Roll), "OnDisable", "RollEnded", false);
            if (WaterBoost.Value)
            {
                Patch(harmony, typeof(Roll), "ExitAbility", "RollExit", true);
            }
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

        private void Update()
        {
            Patches.Update();
        }

    }
    public class Patches // check for time in water
    {
        static Dictionary<int, float> PlayerData = new Dictionary<int, float>();
        public static bool AntiStalemate;
        public static float StalemateTime;
        public static void RollStarted(ref Roll __instance)
        {
            DestroyIfOutsideSceneBounds kill = __instance.GetComponent<DestroyIfOutsideSceneBounds>();
            kill.enabled = false;
            if (AntiStalemate)
            {
                try
                {
                    PlayerData[__instance.player.Id] = 0;
                }
                catch { }
            }
        }
        public static void RollEnded(ref Roll __instance)
        {
            DestroyIfOutsideSceneBounds kill = __instance.GetComponent<DestroyIfOutsideSceneBounds>();
            kill.enabled = true;
            if (AntiStalemate)
            {
                try
                {
                    PlayerData.Remove(__instance.player.Id);
                }
                catch { }
            }
        }

        public static void Update()
        {
            List<int> keys = new List<int>(PlayerData.Keys);

            foreach (int key in keys)
            {
                try 
                {
                    int playerId = key;
                    float playerTime = PlayerData[key];
                    playerTime += Time.deltaTime;
                    PlayerData[playerId] = playerTime;
                }
                catch { }
            }
        }

        public static bool RollExit(ref Roll __instance)
        {
            Fix pos = __instance.player.Position.y;
            if (pos < SceneBounds.WaterHeight && __instance.dashTime < __instance.timeSinceRelease)
            {
                foreach (var kvp in PlayerData)
                {
                    int playerId = kvp.Key;
                    float playerTime = kvp.Value;
                    if (playerTime >= StalemateTime && playerId == __instance.player.Id)
                    {
                        return true;
                    }
                }
                __instance.timeSinceRelease = __instance.timeSinceRelease-(Fix).25f;
                return false;
            }
            return true;
        }
    }
}
