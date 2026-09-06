using HarmonyLib;

namespace APKingdom2Crown;

[HarmonyPatch(typeof(Hermit), "Update")]
public class HermitUpdatePatch
{
    static void Postfix(Hermit __instance)
    {
        if (!Plugin.APconnected) return;
        switch (__instance.Type)
        {
            case Hermit.HermitType.Ballista:
                __instance.forceBlockPayment = !APItemRegistry.IsCollected(APIDRegistry.Get("Hermit Ballista Use"));
                break;
            case Hermit.HermitType.Baker:
                __instance.forceBlockPayment = !APItemRegistry.IsCollected(APIDRegistry.Get("Hermit Bakery Use"));
                break;
            case Hermit.HermitType.Horn:
                __instance.forceBlockPayment = !APItemRegistry.IsCollected(APIDRegistry.Get("Hermit Horn Use"));
                break;
            case Hermit.HermitType.Knight:
                __instance.forceBlockPayment = !APItemRegistry.IsCollected(APIDRegistry.Get("Hermit Warrior Use"));
                break;
            case Hermit.HermitType.Horse:
                __instance.forceBlockPayment = !APItemRegistry.IsCollected(APIDRegistry.Get("Hermit Stable Use"));
                break;
        }
    }
}

[HarmonyPatch(typeof(Cabin), "Pay")]
public class CabinOnPayPatch
{
    static void Prefix(Cabin __instance)
    {
        if (!Plugin.APconnected) return;
        __instance.hermitPrefab.forceBlockPayment = true;

        switch (__instance.hermitType)
        {
            case Hermit.HermitType.Ballista:
                Plugin.AP?.CompleteLocation(APIDRegistry.Get("Hermit Ballista Unlock"));
                break;
            case Hermit.HermitType.Baker:
                Plugin.AP?.CompleteLocation(APIDRegistry.Get("Hermit Bakery Unlock"));
                break;
            case Hermit.HermitType.Horn:
                Plugin.AP?.CompleteLocation(APIDRegistry.Get("Hermit Horn Unlock"));
                break;
            case Hermit.HermitType.Knight:
                Plugin.AP?.CompleteLocation(APIDRegistry.Get("Hermit Warrior Unlock"));
                break;
            case Hermit.HermitType.Horse:
                Plugin.AP?.CompleteLocation(APIDRegistry.Get("Hermit Stable Unlock"));
                break;
        }
        Plugin.Log.LogInfo($"[AP] Hermit Brough → Sending Check for Location ");
    }
}
