using System;
using System.Collections.Generic;
using System.Linq;
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
            "Sunken Gold (Junk)" => "It's covered in seaweed and worth nothing.",
            "Broken Crown (Junk)" => "Someone else's bad night. Not yours. Yet.",
            "Tuft of Grass (Junk)" => "Your steed looks interested. You are not.",
            "Greed Mask Shard (Junk)" => "It's still warm.",
            "Soggy Bread (Junk)" => "Even the vagrants won't touch it.",
            "Bent Arrow (Junk)" => "Your archers politely decline.",
            "Lost Banner (Junk)" => "It belongs to a kingdom long gone.",
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

    public static void StamDam(string source)
    {
        var steed = GameAccess.Player?.steed;
        if (steed == null)
        {
            Trap("Stam Dam! ...but you're on foot.", source);
            return;
        }
        steed.BecomeTired();
        Trap("Stam Dam! Your mount is exhausted.", source);
    }

    // --- Bad king traps ---

    private static string _beggarTag, _peasantTag;

    // The game swaps a character for another kind by tag; find the tags for vagrants and plain villagers.
    private static void FindCharacterTags()
    {
        if (_beggarTag != null && _peasantTag != null) return;
        try
        {
            foreach (var pair in GameAccess.Holder.tagCharacterPairs)
            {
                var character = pair.Value;
                if (character == null) continue;
                if (_beggarTag == null && character.GetComponent<Beggar>() != null) _beggarTag = pair.Key;
                else if (_peasantTag == null && character.GetComponent<Peasant>() != null) _peasantTag = pair.Key;
            }
        }
        catch (Exception e) { Plugin.Log.LogWarning($"[AP] Character tag lookup failed: {e.Message}"); }
    }

    private static bool HasJob(Character c) =>
        c.GetComponent<Archer>() != null || c.GetComponent<Worker>() != null || c.GetComponent<Farmer>() != null || c.GetComponent<Pikeman>() != null;

    // Subjects that can desert: villagers and anyone with a job, but not knights.
    private static List<Character> Subjects(bool needJob)
    {
        var list = new List<Character>();
        foreach (var c in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            if (c.GetComponent<Beggar>() != null || c.GetComponent<Knight>() != null) continue;
            bool job = HasJob(c);
            if (needJob ? job : job || c.GetComponent<Peasant>() != null) list.Add(c);
        }
        return list;
    }

    public static void Desertion(string source, int amount)
    {
        FindCharacterTags();
        if (_beggarTag == null)
        {
            Trap("Desertion! ...but nobody knows how to leave.", source);
            return;
        }
        var subjects = Subjects(needJob: false);
        int left = 0;
        for (int i = 0; i < amount && subjects.Count > 0; i++)
        {
            var c = subjects[Random.Range(0, subjects.Count)];
            subjects.Remove(c);
            try
            {
                // Swap them for a vagrant the game's own way (so every job list lets go of them), then let the
                // vagrant vanish a moment later rather than hang around town waiting for a coin.
                var deserter = c.ReplaceBy(_beggarTag);
                if (deserter != null) Deserters.Add((deserter.gameObject, Time.unscaledTime + DeserterLingerSeconds));
                left++;
            }
            catch (Exception e) { Plugin.Log.LogWarning($"[AP] Desertion failed for {c.name}: {e.Message}"); }
        }
        Trap(left == 0 ? "Desertion! ...but you have no subjects to lose." : $"Desertion! {left} subject{(left == 1 ? "" : "s")} gave up and left your kingdom.", source);
    }

    private const float DeserterLingerSeconds = 1.5f;
    private static readonly List<(GameObject Go, float When)> Deserters = new();

    private static void TickDeserters()
    {
        for (int i = Deserters.Count - 1; i >= 0; i--)
        {
            var (go, when) = Deserters[i];
            if (Time.unscaledTime < when) continue;
            Deserters.RemoveAt(i);
            if (go == null) continue;
            try
            {
                var damageable = go.GetComponent<Damageable>();
                if (damageable != null) damageable.DestroyNow();
                else Object.Destroy(go);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[AP] Deserter cleanup: {e.Message}");
                try { Object.Destroy(go); } catch { }
            }
        }
    }

    public static void ToolTheft(string source, int amount)
    {
        int fromRacks = 0, fromHands = 0;

        // Shop racks first.
        foreach (var tool in Object.FindObjectsByType<DroppableTool>(FindObjectsSortMode.None))
        {
            if (fromRacks >= amount) break;
            if (tool == null) continue;
            var shop = tool.parentShopRef;
            if (shop == null) continue;
            try
            {
                shop.NotifyDroppableCollected(tool);
                Object.Destroy(tool.gameObject);
                fromRacks++;
            }
            catch (Exception e) { Plugin.Log.LogWarning($"[AP] Tool theft from rack failed: {e.Message}"); }
        }

        // Then out of your units' hands: they go back to being plain villagers.
        if (fromRacks < amount)
        {
            FindCharacterTags();
            if (_peasantTag != null)
            {
                var workers = Subjects(needJob: true);
                while (fromRacks + fromHands < amount && workers.Count > 0)
                {
                    var c = workers[Random.Range(0, workers.Count)];
                    workers.Remove(c);
                    try
                    {
                        c.ReplaceBy(_peasantTag);
                        fromHands++;
                    }
                    catch (Exception e) { Plugin.Log.LogWarning($"[AP] Tool theft from {c.name} failed: {e.Message}"); }
                }
            }
        }

        int total = fromRacks + fromHands;
        Trap(total == 0
            ? "Tool Theft! ...but there was nothing to steal."
            : $"Tool Theft! The Greed stole {total} tool{(total == 1 ? "" : "s")} ({fromRacks} from the racks, {fromHands} from your people).", source);
    }

    // --- DeathLink ---

    private static float _crownFlingUntil;
    private static float _crownFlingX;

    // What someone else's death does to you: your purse is emptied (coins gone, gems stolen until you liberate the
    // island) and/or your crown is flung off screen towards the nearest portal.
    public static void DeathLink(string source, bool crown, bool wallet)
    {
        var player = GameAccess.Player;
        if (player == null) return;
        LastDeathLinkEffectTime = Time.unscaledTime;

        var parts = new List<string>();
        if (wallet) parts.Add(EmptyPurse(player));
        if (crown) parts.Add(FlingCrown(player));
        parts.RemoveAll(string.IsNullOrEmpty);
        Trap("DeathLink! " + (parts.Count > 0 ? string.Join(" ", parts) : "...but you had nothing to lose."), source);
    }

    private static string EmptyPurse(Player player)
    {
        var wallet = player.wallet;
        if (wallet == null) return "";
        int coins = wallet.Coins;
        if (coins > 0) wallet.RemoveCurrency(CurrencyType.Coins, coins);
        var gems = StolenGems.Steal(wallet);
        var coinText = coins > 0 ? $"Your {coins} coin{(coins == 1 ? " is" : "s are")} gone." : "";
        return string.Join(" ", new[] { coinText, gems }.Where(t => t.Length > 0));
    }

    private static string FlingCrown(Player player)
    {
        if (!player.hasCrown) return "";

        // Four half-screens away: well out of sight. Towards the nearest live portal if there is one, but not
        // right on top of it; otherwise the way you're facing.
        float px = player.transform.position.x;
        float view = HalfViewWidth(player);
        float distance = view * 4f;
        float dir = GameAccess.FacingDirection(player);
        float nearest = float.MaxValue;
        foreach (var portal in Object.FindObjectsByType<Portal>(FindObjectsSortMode.None))
        {
            if (portal == null || portal.type == Portal.Type.Dock) continue;
            if (portal.state is Portal.State.Destroyed or Portal.State.Crumbled) continue;
            float d = Mathf.Abs(portal.transform.position.x - px);
            if (d >= nearest) continue;
            nearest = d;
            dir = Mathf.Sign(portal.transform.position.x - px);
        }
        if (nearest < float.MaxValue) distance = Mathf.Min(distance, nearest - view * 0.5f);
        distance = Mathf.Max(distance, view * 1.1f);

        float target = ClampToLevel(px + dir * distance);
        player.LoseCrown(new Vector2(dir * 8f, 8f), false, false);
        _crownFlingX = target;
        _crownFlingUntil = Time.unscaledTime + 2f; // the dropped crown may only appear next frame
        Plugin.Log.LogInfo($"[AP] Flinging crown from x={px:F0} to x={target:F0} (half screen {view:F1})");
        return nearest < float.MaxValue ? "Your crown went flying towards the Greed!" : "Your crown went flying!";
    }

    // Half the visible width, in world units at the monarch's depth.
    private static float HalfViewWidth(Player player)
    {
        try
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var pos = player.transform.position;
                float depth = Mathf.Abs(pos.z - cam.transform.position.z);
                float left = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)).x;
                float right = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth)).x;
                float half = Mathf.Abs(right - left) / 2f;
                if (half > 1f && half < 200f) return half;
            }
        }
        catch (Exception e) { Plugin.Log.LogDebug($"[AP] View width: {e.Message}"); }
        return 15f;
    }

    // Keeps a position on the island's ground, a little in from either end.
    private static float ClampToLevel(float x)
    {
        try
        {
            foreach (var level in Object.FindObjectsByType<Level>(FindObjectsSortMode.None))
            {
                var ground = level == null ? null : level.GroundCollider;
                if (ground == null) continue;
                var bounds = ground.bounds;
                return Mathf.Clamp(x, bounds.min.x + 8f, bounds.max.x - 8f);
            }
        }
        catch (Exception e) { Plugin.Log.LogDebug($"[AP] Level bounds: {e.Message}"); }
        return x;
    }

    // Called every frame: finishes moving a flung crown once the game has spawned it.
    public static void Tick()
    {
        if (Deserters.Count > 0) TickDeserters();
        if (_crownFlingUntil <= 0f) return;
        if (Time.unscaledTime > _crownFlingUntil)
        {
            _crownFlingUntil = 0f;
            return;
        }
        try
        {
            var crown = GameAccess.Kingdom?.droppedP1Crown;
            if (crown == null) return;
            var pos = crown.transform.position;
            crown.transform.position = new Vector3(_crownFlingX, pos.y + 1f, pos.z);
            var body = crown.GetComponent<Rigidbody2D>();
            if (body != null) body.position = new Vector2(_crownFlingX, pos.y + 1f);
            Plugin.Log.LogInfo($"[AP] Crown flung to x={_crownFlingX:F0}");
            _crownFlingUntil = 0f;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Crown fling failed: {e.Message}");
            _crownFlingUntil = 0f;
        }
    }
}
