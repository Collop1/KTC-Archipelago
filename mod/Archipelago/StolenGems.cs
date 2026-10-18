using System;
using System.Linq;

namespace APKingdom2Crown;

// DeathLink gem theft: the Greed run off with the gems in your purse, and liberating the island they took them on
// wins them back. As a backstop they drop them after a number of nights, so a seed never gets stuck waiting.
internal static class StolenGems
{
    public static int Total => AP.State.StolenGems.Sum(e => e.Gems);

    // Takes every gem from the purse. Returns what to tell the player.
    public static string Steal(Wallet wallet)
    {
        int gems = wallet.Gems;
        if (gems <= 0) return "";
        var campaign = AP.Slot.Campaign;
        int island = GameAccess.CurrentIsland;
        if (!campaign.CampaignWide && island > 0 && GameAccess.IsIslandSecured(island))
            return "There are no Greed left here to take your gems.";

        wallet.RemoveCurrency(CurrencyType.Gems, gems);
        int nights = Math.Max(1, AP.Slot.StolenGemsReturnNights);
        AP.State.StolenGems.Add(new StolenGemsEntry { Island = island, Gems = gems, NightsLeft = nights });
        AP.State.Dirty = true;
        Plugin.Log.LogInfo($"[AP] Greed stole {gems} gems on island {island}");

        var what = gems == 1 ? "your gem" : $"your {gems} gems";
        return campaign.CampaignWide || island <= 0
            ? $"The Greed stole {what}; they'll drop them after {nights} nights."
            : $"The Greed stole {what}. Liberate Island {island} to win them back (or survive {nights} nights).";
    }

    // Called at every dawn.
    public static void OnNightSurvived()
    {
        var list = AP.State.StolenGems;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            list[i].NightsLeft--;
            AP.State.Dirty = true;
            if (list[i].NightsLeft <= 0) Return(i, "The Greed got bored and dropped");
        }
    }

    // Polled while playing: liberating the island the gems were stolen on gives them back.
    public static void Poll()
    {
        var list = AP.State.StolenGems;
        if (list.Count == 0 || AP.Slot.Campaign.CampaignWide) return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            int island = list[i].Island;
            if (island > 0 && GameAccess.IsIslandSecured(island))
                Return(i, $"Island {island} is free! The Greed dropped");
        }
    }

    private static void Return(int index, string reason)
    {
        var entry = AP.State.StolenGems[index];
        AP.State.StolenGems.RemoveAt(index);
        AP.State.Dirty = true;
        ItemEffects.GiveCurrency(CurrencyType.Gems, entry.Gems);
        Feed.Add($"<color=#00FF7F>{reason} the {entry.Gems} gem{(entry.Gems == 1 ? "" : "s")} they stole.</color>");
    }

    // For the menu: "7 (Island 3, or 4 nights)"
    public static string Describe() => string.Join(", ", AP.State.StolenGems.Select(e =>
        AP.Slot.Campaign.CampaignWide || e.Island <= 0
            ? $"{e.Gems} ({e.NightsLeft} nights)"
            : $"{e.Gems} (liberate Island {e.Island}, or {e.NightsLeft} nights)"));
}
