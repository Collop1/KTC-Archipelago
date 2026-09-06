using HarmonyLib;

namespace APKingdom2Crown;

[HarmonyPatch(typeof(Chest), "ReceiveOpenCall")]
public class ChestOpenPatch
{
    static void Postfix(Chest __instance)
    {
        Plugin.Log.LogInfo($"[AP] Chest.ReceiveOpenCall fired (currencyType={__instance.currencyType}, APconnected={Plugin.APconnected})");
        if (!Plugin.APconnected) return;
        if (__instance.currencyType == CurrencyType.Coins)
        {
            APHelper.SendNextProgressiveCheck("Progressive Coin Chest", 9);
        }
        else if (__instance.currencyType == CurrencyType.Gems)
        {
            APHelper.SendNextProgressiveCheck("Progressive Coin Gem", 12);
        }
    }
}
