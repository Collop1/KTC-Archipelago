using System;
using Coatsink.Common;
using UnityEngine;

namespace APKingdom2Crown;

// Null-safe accessors for the game's singletons. Every getter swallows the interop exceptions the game
// throws while scenes are loading, so callers only ever need a null check.
internal static class GameAccess
{
    public static Managers Managers
    {
        get
        {
            try { return SingletonMonoBehaviour<Managers>.Inst; }
            catch { return null; }
        }
    }

    public static Kingdom Kingdom => Safe(() => Managers?.kingdom);
    public static Director Director => Safe(() => Managers?.director);
    public static Game Game => Safe(() => Managers?.game);
    public static EnemyManager Enemies => Safe(() => Managers?.enemies);
    public static Holder Holder => Safe(() => Managers?.holder);
    public static Player Player => Safe(() => Kingdom?.GetPlayer(0));

    // True while the monarch is on an island and the game isn't loading, sailing or in a menu.
    public static bool IsPlaying
    {
        get
        {
            try
            {
                var game = Game;
                return game != null && (game.state & Game.State.Playing) != 0 && Player != null && Kingdom != null;
            }
            catch { return false; }
        }
    }

    private static int? _landOne;

    // The game's index for island 1 (Land.LAND_ONE); read once at runtime instead of assuming 0 or 1.
    public static int LandOne
    {
        get
        {
            if (_landOne.HasValue) return _landOne.Value;
            try
            {
                _landOne = Land.LAND_ONE;
                Plugin.Log.LogInfo($"[AP] Land.LAND_ONE = {_landOne}");
            }
            catch
            {
                return 0;
            }
            return _landOne.Value;
        }
    }

    public static int LandToIsland(int land) => land - LandOne + 1;
    public static int IslandToLand(int island) => island - 1 + LandOne;

    public static int CurrentLand
    {
        get
        {
            try
            {
                var game = Game;
                if (game != null) return game.currentLand;
            }
            catch { }
            try { return CampaignSaveData.current?.currentLand ?? -1; }
            catch { return -1; }
        }
    }

    // Island number (1-5, or 1-6 in Norselands), or 0 if unknown. In Call of Olympus this is just the land
    // index + 1 and only used to tell islands apart.
    public static int CurrentIsland
    {
        get
        {
            var land = CurrentLand;
            if (land < 0) return 0;
            var island = LandToIsland(land);
            int max = AP.Slot.Campaign.CampaignWide ? 32 : AP.Slot.Campaign.IslandCount;
            return island >= 1 && island <= max ? island : 0;
        }
    }

    // Name of the loaded island's level config, e.g. "Greece_Land_God_Athena".
    public static string CurrentLevelConfigName
    {
        get
        {
            try { return Game?.currentLevelConfig?.name ?? ""; }
            catch { return ""; }
        }
    }

    // The loaded campaign's biome (BiomeHolder.Biomes), or -1 if unknown.
    public static int BiomeIndex
    {
        get
        {
            try { return BiomeHolder.Inst?.BiomeIndex ?? -1; }
            catch { return -1; }
        }
    }

    // False when connected to a seed for a different campaign than the one being played.
    public static bool CampaignMatches
    {
        get
        {
            if (AP.State.IsOffline && !AP.Connected) return true;
            int biome = BiomeIndex;
            return biome < 0 || biome == AP.Slot.Campaign.BiomeIndex;
        }
    }

    public static bool IsIslandSecured(int island)
    {
        try
        {
            var helper = Managers?.progressHelper;
            if (helper != null && helper.IsLandSecured(IslandToLand(island))) return true;
        }
        catch { }
        try
        {
            var campaign = CampaignSaveData.current;
            return campaign != null && campaign.GetSecuredIsland(IslandToLand(island));
        }
        catch { return false; }
    }

    public static int CastleLevel
    {
        get
        {
            try
            {
                var castle = Kingdom?.castle;
                return castle == null ? 0 : (int)castle.level + 1;
            }
            catch { return 0; }
        }
    }

    public static float FacingDirection(Component c)
    {
        try { return c.transform.localScale.x < 0 ? -1f : 1f; }
        catch { return 1f; }
    }

    private static T Safe<T>(Func<T> getter) where T : class
    {
        try { return getter(); }
        catch { return null; }
    }
}
