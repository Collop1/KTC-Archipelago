using HarmonyLib;

namespace APKingdom2Crown;

[HarmonyPatch(typeof(Portal), "OnDestroy")]
public class PortalOnDestroyPatch
{
    static void Prefix(Portal __instance)
    {
        if (!Plugin.APconnected) return;
        // Only "Regular" portals are the enemy-spawning kind the player actually breaks.
        // Cliff and Dock portals get destroyed for unrelated reasons (e.g. Dock during
        // the boat-arrival sequence at the start of an island) and would otherwise fire
        // a spurious progressive check.
        if (__instance.type != Portal.Type.Regular) return;

        Plugin.Log.LogInfo($"[AP] Portal destroyed (type={__instance.type}) → Checking progressive sequence...");
        APHelper.SendNextProgressiveCheck("Destroy Portal Progressive", 23);
    }
}
