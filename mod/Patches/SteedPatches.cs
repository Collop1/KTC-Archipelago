using System;
using HarmonyLib;

namespace APKingdom2Crown;

[HarmonyPatch(typeof(SteedSpawn), "Pay")]
public class SteedPayPatch
{
    static readonly string[] SteedUnlockNames =
    {
        "Bear Unlocks",
        "Stag Unlocks",
        "Lizard Unlocks",
        "Unicorn Unlocks",
        "Wolf P1 Unlocks",
        "Warhorse P1 Unlocks",
        "Horse Fast Unlocks",
        "Griffin P1 Unlocks",
        "Griffin Skull P1 Unlocks",
        "Horse Stamina Unlocks",
        "Wolf_norselands Unlocks",
        "Reindeer Unlocks",
        "Horse Burst Unlocks",
        "Rainbow Pony Unlocks",
        "Warhorse Plague P1 Unlocks",
        "Wolf Norselands P1 Unlocks",
        "Unicorn Dark Unlocks"
    };

    static void Postfix(SteedSpawn __instance)
    {
        if (!Plugin.APconnected) return;
        foreach (var steed in __instance.steeds)
        {
            steed.forceBlockPayment = true;

            foreach (var unlockName in SteedUnlockNames)
            {
                if (steed.name.Contains(unlockName.Replace(" Unlocks", ""), StringComparison.OrdinalIgnoreCase))
                {
                    var id = APIDRegistry.Get(unlockName);
                    if (id != -1)
                    {
                        Plugin.AP?.CompleteLocation(id);
                        Plugin.Log.LogInfo($"[AP] Steed '{steed.name}' → Completed location '{unlockName}' (ID {id})");
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"[AP] No registry entry found for '{unlockName}'");
                    }
                    break;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(Steed), "Update")]
public class SteedUpdatePatch
{
    static readonly string[] SteedUnlockNames =
    {
        "Bear Use",
        "Stag Use",
        "Lizard Use",
        "Unicorn Use",
        "Wolf P1 Use",
        "Warhorse P1 Use",
        "Horse Fast Use",
        "Griffin P1 Use",
        "Griffin Skull P1 Use",
        "Horse Stamina Use",
        "Wolf_norselands Use",
        "Reindeer Use",
        "Horse Burst Use",
        "Rainbow Pony Use",
        "Warhorse Plague P1 Use",
        "Wolf Norselands P1 Use",
        "Unicorn Dark Use"
    };

    static void Postfix(Steed __instance)
    {
        if (!Plugin.APconnected) return;
        foreach (var unlockName in SteedUnlockNames)
        {
            if (__instance.name.Contains(unlockName.Replace(" Use", ""), StringComparison.OrdinalIgnoreCase))
            {
                __instance.forceBlockPayment = !APItemRegistry.IsCollected(APIDRegistry.Get(unlockName));
                break;
            }
        }
    }

}
