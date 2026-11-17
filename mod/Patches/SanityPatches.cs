using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace APKingdom2Crown;

// Hooks for the "sanity" checks: buildings, recruits, animals, coins, grass, plus empty chests.

// Chests only give their check in this randomizer: nothing spills out.
[HarmonyPatch(typeof(Chest), nameof(Chest.OnTriggerStay2D))]
internal static class ChestEmptyPatch
{
    private static void Prefix(Chest __instance)
    {
        try
        {
            if (CheckTracker.Tracking && !__instance._wasTriggered && __instance.currencyAmount != 0)
                __instance.currencyAmount = 0;
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Empty chest patch: {e}"); }
    }
}

// The Stone Quarry / Iron Mine: the built building raises the kingdom's tech when it starts up.
[HarmonyPatch(typeof(EraUpgradeBuilding), nameof(EraUpgradeBuilding.Start))]
internal static class EraBuildingPatch
{
    private static void Postfix(EraUpgradeBuilding __instance)
    {
        try
        {
            // Buildings restored while an island loads were built earlier; only count new ones.
            if (GameAccess.IsPlaying) CheckTracker.OnTechBuilding(__instance.techAge);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Tech building patch: {e}"); }
    }
}

[HarmonyPatch(typeof(Hermit), nameof(Hermit.Build))]
internal static class HermitBuildPatch
{
    private static void Postfix(Hermit __instance)
    {
        try { CheckTracker.OnHermitBuilding(__instance.Type); }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Hermit build patch: {e}"); }
    }
}

// Unit sanity: a character promoted into a new role.
internal static class PromotionTracking
{
    private static readonly Dictionary<IntPtr, float> Recent = new();

    public static void After(Character result)
    {
        try
        {
            if (result == null || !GameAccess.IsPlaying) return;
            // One promotion can pass through both overloads; count it once.
            float now = Time.unscaledTime;
            if (Recent.TryGetValue(result.Pointer, out var when) && now - when < 2f) return;
            Recent[result.Pointer] = now;
            if (Recent.Count > 256) Recent.Clear();

            // Villagers are counted from the vagrant's side (VagrantRecruitPatch), not here.
            GameData.Milestone? kind =
                result.GetComponent<Archer>() != null ? GameData.Milestone.Archers
                : result.GetComponent<Worker>() != null ? GameData.Milestone.Builders
                : result.GetComponent<Farmer>() != null ? GameData.Milestone.Farmers
                : result.GetComponent<Knight>() != null ? GameData.Milestone.Knights
                : null;
            if (kind.HasValue) CheckTracker.AddMilestone(kind.Value);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Promotion tracking: {e}"); }
    }
}

// A vagrant picking up one of the monarch's coins joins the kingdom as a villager.
[HarmonyPatch(typeof(Beggar), nameof(Beggar.HandleOnPickupPlayerCurrency))]
internal static class VagrantRecruitPatch
{
    private static readonly Dictionary<IntPtr, float> Recent = new();

    private static void Postfix(Beggar __instance)
    {
        try
        {
            if (!GameAccess.IsPlaying) return;
            float now = Time.unscaledTime;
            if (Recent.TryGetValue(__instance.Pointer, out var when) && now - when < 2f) return;
            Recent[__instance.Pointer] = now;
            if (Recent.Count > 256) Recent.Clear();
            CheckTracker.AddMilestone(GameData.Milestone.Villagers);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Vagrant recruit patch: {e}"); }
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.Promote), new[] { typeof(DroppableTool), typeof(IUnitController) })]
internal static class PromoteToolPatch
{
    private static void Postfix(Character __result) => PromotionTracking.After(__result);
}

[HarmonyPatch(typeof(Character), nameof(Character.Promote), new[] { typeof(string), typeof(IUnitController) })]
internal static class PromoteTagPatch
{
    private static void Postfix(Character __result) => PromotionTracking.After(__result);
}

// Citizen houses: paying a coin at one sends out a villager.
[HarmonyPatch(typeof(CitizenHousePayable), nameof(CitizenHousePayable.Pay))]
internal static class CitizenHouseRecruitPatch
{
    private static void Postfix()
    {
        try
        {
            if (GameAccess.IsPlaying) CheckTracker.AddMilestone(GameData.Milestone.Villagers);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Citizen house patch: {e}"); }
    }
}

// Bribery: any coin a Greed picks up, on purpose or not.
[HarmonyPatch(typeof(Troll), nameof(Troll.PickupLoot))]
internal static class GreedPickupPatch
{
    private static void Postfix(Droppable droppable)
    {
        try
        {
            if (droppable == null) return;
            var currency = droppable.GetComponent<DroppableCurrency>();
            if (currency != null && currency.CurrencyType == CurrencyType.Coins) CheckTracker.AddMilestone(GameData.Milestone.Bribes);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Greed pickup patch: {e}"); }
    }
}

// Coins spent: the price of anything paid for with coins.
[HarmonyPatch(typeof(Payable), nameof(Payable.PerformPay))]
internal static class PaySpendPatch
{
    private static void Prefix(Payable __instance, out int __state)
    {
        __state = 0;
        try
        {
            if (__instance.Currency == CurrencyType.Coins) __state = __instance.Price;
        }
        catch { }
    }

    private static void Postfix(int __state)
    {
        try
        {
            if (__state > 0 && GameAccess.IsPlaying) CheckTracker.AddMilestone(GameData.Milestone.CoinsSpent, __state);
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Spend patch: {e}"); }
    }
}

// Grass: the monarch's mount starting to eat while standing on grass.
[HarmonyPatch(typeof(Steed), nameof(Steed.Eat))]
internal static class SteedEatPatch
{
    private static void Postfix(Steed __instance)
    {
        try
        {
            var rider = __instance.Rider;
            if (rider == null || (rider.groundType & Player.GroundType.Grass) == 0) return;
            CheckTracker.OnGrassEaten();
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Steed eat patch: {e}"); }
    }
}

// Coins lost to water.
[HarmonyPatch(typeof(Droppable), nameof(Droppable.DropInWater))]
internal static class DropInWaterPatch
{
    private static readonly Dictionary<IntPtr, float> Recent = new();

    private static void Postfix(Droppable __instance)
    {
        try
        {
            var currency = __instance.GetComponent<DroppableCurrency>();
            if (currency == null || currency.CurrencyType != CurrencyType.Coins) return;
            if (ItemEffects.SunkenGoldCoins.Contains(__instance.Pointer)) return; // the Sunken Gold junk item's own coin
            float now = Time.unscaledTime;
            if (Recent.TryGetValue(__instance.Pointer, out var when) && now - when < 5f) return;
            Recent[__instance.Pointer] = now;
            if (Recent.Count > 256) Recent.Clear();
            CheckTracker.OnCoinSunk();
        }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Water patch: {e}"); }
    }
}
