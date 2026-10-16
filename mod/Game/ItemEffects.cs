using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace APKingdom2Crown;

// What filler items and traps actually do in the game. Every effect logs to the feed so it's obvious
// (especially from the Debug tab) whether it worked.
internal static class ItemEffects
{
    private const string TrapColor = "#FA8072";

    // Set while a DeathLink-triggered effect could be the cause of losing the game, so that loss isn't
    // sent back out as a new DeathLink.
    public static float LastDeathLinkEffectTime = -999f;

    private static GameObject _beggarPrefab;

    private static string From(string source) =>
        string.IsNullOrEmpty(source) ? "" : $" <color=#AAAAAA>(from {AP.Escape(source)})</color>";

    private static void Trap(string text, string source) => Feed.Add($"<color={TrapColor}>{text}</color>{From(source)}");

    public static void GiveCurrency(CurrencyType type, int amount)
    {
        var player = GameAccess.Player;
        if (player == null || player.wallet == null) return;
        player.wallet.AddCurrency(type, amount);
    }

    public static void Junk(string itemName)
    {
        var flavour = itemName switch
        {
            "Sunken Gold" => "It's covered in seaweed and worth nothing.",
            "Broken Crown" => "Someone else's bad night. Not yours. Yet.",
            "Tuft of Grass" => "Your steed looks interested. You are not.",
            "Greed Mask Shard" => "It's still warm.",
            "Soggy Bread" => "Even the vagrants won't touch it.",
            "Bent Arrow" => "Your archers politely decline.",
            "Lost Banner" => "It belongs to a kingdom long gone.",
            _ => "It does nothing.",
        };
        Feed.Add($"<color=#AAAAAA>{AP.Escape(itemName)}: {flavour}</color>");
    }

    public static void SpawnVagrants(int count)
    {
        var kingdom = GameAccess.Kingdom;
        var player = GameAccess.Player;
        var prefab = FindBeggarPrefab();
        if (kingdom == null || player == null || prefab == null)
        {
            Feed.Add("<color=#AAAAAA>Vagrants could not find your kingdom.</color>");
            return;
        }

        float x = kingdom.CampfireWorldPosition;
        try
        {
            var castle = kingdom.castle;
            if (castle != null) x = castle.transform.position.x;
        }
        catch { }

        var basePos = player.transform.position;
        for (int i = 0; i < count; i++)
        {
            var pos = new Vector3(x + Random.Range(-3f, 3f), basePos.y, basePos.z);
            Object.Instantiate(prefab, pos, Quaternion.identity);
        }
        Feed.Add($"<color=#00EEEE>{count} vagrant{(count == 1 ? "" : "s")} wandered into your town centre.</color>");
    }

    private static GameObject FindBeggarPrefab()
    {
        if (_beggarPrefab != null) return _beggarPrefab;
        try
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || go.scene.name != null) continue; // prefabs only, not scene instances
                if (go.name == "Beggar" && go.GetComponent<Beggar>() != null)
                {
                    _beggarPrefab = go;
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Beggar prefab search failed: {e.Message}");
        }
        if (_beggarPrefab == null)
        {
            try
            {
                foreach (var character in GameAccess.Holder.charactersList)
                {
                    if (character != null && character.GetComponent<Beggar>() != null)
                    {
                        _beggarPrefab = character.gameObject;
                        break;
                    }
                }
            }
            catch { }
        }
        return _beggarPrefab;
    }

    public static void Trip(string source)
    {
        var player = GameAccess.Player;
        if (player == null) return;
        if (source == "DeathLink") LastDeathLinkEffectTime = Time.unscaledTime;
        if (!player.hasCrown)
        {
            Trap("Trip! ...but you have no crown to lose.", source);
            return;
        }
        float dir = GameAccess.FacingDirection(player);
        player.LoseCrown(new Vector2(-dir * 2.5f, 6f), false, false);
        Trap("Trip! Your crown went flying. Grab it before the Greed do!", source);
    }

    public static void SlipperyPouch(string source)
    {
        var player = GameAccess.Player;
        var wallet = player?.wallet;
        if (wallet == null) return;
        if (source == "DeathLink") LastDeathLinkEffectTime = Time.unscaledTime;

        int coins = wallet.Coins;
        int gems = wallet.Gems;
        if (coins + gems == 0)
        {
            Trap("Slippery Pouch! ...it was already empty.", source);
            return;
        }

        var origin = player.transform.position + Vector3.up * 1.5f;
        var spilled = new List<DroppableCurrency>();
        Spill(wallet, CurrencyType.Coins, coins, origin, spilled);
        Spill(wallet, CurrencyType.Gems, gems, origin, spilled);

        foreach (var currency in spilled)
        {
            try
            {
                var body = currency.GetComponent<Rigidbody2D>();
                if (body != null) body.AddForce(new Vector2(Random.Range(-9f, 9f), Random.Range(5f, 11f)), ForceMode2D.Impulse);
            }
            catch { }
        }
        Trap($"Slippery Pouch! {coins} coin{(coins == 1 ? "" : "s")} and {gems} gem{(gems == 1 ? "" : "s")} spilled everywhere.", source);
    }

    private static void Spill(Wallet wallet, CurrencyType type, int amount, Vector3 origin, List<DroppableCurrency> spilled)
    {
        if (amount <= 0) return;
        wallet.RemoveCurrency(type, amount);
        for (int i = 0; i < amount; i++)
        {
            try
            {
                var currency = CurrencyManagerExt.SpawnCurrency(type, origin, Quaternion.identity, null);
                if (currency != null) spilled.Add(currency);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[AP] Could not spill {type}: {e.Message}");
                wallet.AddCurrency(type, amount - i); // give back what we couldn't drop
                return;
            }
        }
    }

    public static void WaveBack(string source)
    {
        var enemies = GameAccess.Enemies;
        if (enemies == null) return;
        enemies.SpawnRetaliation();
        Trap("Wave Back! A portal retaliates. The Greed are coming!", source);
    }

    public static void WavesGoneBy(string source, int days)
    {
        int island = GameAccess.CurrentIsland;
        if (island == 0) return;
        var state = AP.State;
        state.WaveOffsets[island] = state.WaveOffset(island) + days;
        state.Dirty = true;
        Trap($"Waves Gone By! The Greed on Island {island} grow stronger (+{days} days, {state.WaveOffset(island)} total).", source);
    }

    public static void DamnedNight(string source)
    {
        var director = GameAccess.Director;
        if (director == null) return;
        if (!director.IsDaytime)
        {
            Trap("Damned Night! ...it's already night. Lucky you.", source);
            return;
        }
        float now = director.currentTime % 24f;
        float skip = director.dayEnd - now;
        if (skip <= 0f) skip += 24f;
        director.AdvanceTime(skip + 0.1f);
        Trap("Damned Night! Darkness falls early.", source);
    }

    public static void Decay(string source)
    {
        var kingdom = GameAccess.Kingdom;
        if (kingdom == null) return;
        float centre = kingdom.CampfireWorldPosition;

        Wall left = null, right = null;
        foreach (var wall in Object.FindObjectsByType<Wall>(FindObjectsSortMode.None))
        {
            if (wall == null || !wall.gameObject.activeInHierarchy || !wall.isIntact) continue;
            float x = wall.transform.position.x;
            if (x < centre)
            {
                if (left == null || x < left.transform.position.x) left = wall;
            }
            else if (right == null || x > right.transform.position.x) right = wall;
        }

        if (left == null && right == null)
        {
            Trap("Decay! ...but you have no walls to rot.", source);
            return;
        }
        var results = new List<string>();
        if (left != null) results.Add("left: " + DecayWall(kingdom, left));
        if (right != null) results.Add("right: " + DecayWall(kingdom, right));
        Trap($"Decay! Your outer walls rot ({string.Join(", ", results)}).", source);
    }

    private static string DecayWall(Kingdom kingdom, Wall wall)
    {
        int level = wall.level;
        int minLevel = MinWallLevel();
        try
        {
            var castle = kingdom.castle;
            if (level > minLevel && castle != null && castle.UpgradeWallToLevel(wall.gameObject, level - 1))
            {
                Plugin.Log.LogInfo($"[AP] Decay: wall at {wall.transform.position.x:F0} level {level} -> {level - 1}");
                return $"tier {level} to {level - 1}";
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Decay downgrade failed, destroying instead: {e.Message}");
        }
        wall.DestroyWall(-1);
        Plugin.Log.LogInfo($"[AP] Decay: wall at {wall.transform.position.x:F0} (level {level}) destroyed");
        return "crumbled";
    }

    private static int MinWallLevel()
    {
        int min = int.MaxValue;
        try
        {
            foreach (var prefab in GameAccess.Holder.wallPrefabs)
                if (prefab != null) min = Math.Min(min, prefab.level);
        }
        catch { }
        return min == int.MaxValue ? 1 : min;
    }
}
