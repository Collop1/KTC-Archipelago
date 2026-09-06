using HarmonyLib;

namespace APKingdom2Crown;

[HarmonyPatch(typeof(Kingdom), "Update")]
public class KingdomUpdatePatch
{
    static void Prefix(Kingdom __instance)
    {
        if (!Plugin.APconnected) return;
        __instance.StoneBuildingUnlocked = APItemRegistry.IsCollected(APIDRegistry.Get("Stone Technology"));
        __instance.IronBuildingUnlocked = APItemRegistry.IsCollected(APIDRegistry.Get("Iron Technology"));

        // Handle campfire when castle not yet built
        if (!__instance.castle)
        {
            var campfireUpgrade = __instance.campfire?.GetComponent<PayableUpgrade>();
            if (campfireUpgrade != null)
                campfireUpgrade.forceBlockPayment = false;
            return;
        }

        // Get the upgrade component once
        var upgrade = __instance.castle.GetComponent<PayableUpgrade>();
        if (upgrade == null)
            return;

        string unlockName = __instance.castle.level switch
        {
            Castle.Level.Castle1 => "Progressive Castle Unlock T1",
            Castle.Level.Castle2 => "Progressive Castle Unlock T2",
            Castle.Level.Castle3 => "Progressive Castle Unlock T3",
            Castle.Level.Castle4 => "Progressive Castle Unlock T4",
            Castle.Level.Castle5 => "Progressive Castle Unlock T5",
            Castle.Level.Castle6 => "Progressive Castle Unlock T6",
            Castle.Level.Castle7 => "Progressive Castle Unlock T7",
            _ => null
        };

        if (unlockName != null)
            upgrade.forceBlockPayment = !APItemRegistry.IsCollected(APIDRegistry.Get(unlockName));
    }
}
