using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace APKingdom2Crown;

// Debug tool: writes every campaign's per-island layout rules (how many chests, portals, statues, mount
// shrines, hermits... each island may get) plus what the current campaign's level blocks contain. This is
// what the apworld's per-island content tables are built from.
internal static class CampaignDump
{
    public static string Run()
    {
        var sb = new StringBuilder();
        try { DumpBiomes(sb); }
        catch (Exception e) { sb.AppendLine($"!! biome dump failed: {e}"); }
        try { DumpBlocks(sb); }
        catch (Exception e) { sb.AppendLine($"!! block dump failed: {e}"); }

        var path = Path.Combine(Storage.Folder, "campaign_dump.txt");
        File.WriteAllText(path, sb.ToString());
        Plugin.Log.LogInfo($"[AP] Campaign data written to {path}");
        return path;
    }

    private static void DumpBiomes(StringBuilder sb)
    {
        var holder = BiomeHolder.Inst;
        sb.AppendLine($"Current biome index: {holder.BiomeIndex}");
        var paths = holder.biomePathStrings;
        var biomes = holder.biomeData;
        for (int b = 0; b < biomes.Length; b++)
        {
            var biome = biomes[b];
            string path = paths != null && b < paths.Length ? paths[b] : "";
            if (biome == null && !string.IsNullOrEmpty(path))
            {
                // Biomes other than the current one load on demand from Resources.
                try
                {
                    var loaded = Resources.Load(path);
                    sb.AppendLine($"\n(Resources.Load '{path}' -> {(loaded == null ? "null" : loaded.GetIl2CppType().Name + " " + loaded.name)})");
                    biome = loaded?.TryCast<BiomeData>();
                    if (biome == null)
                    {
                        var go = loaded?.TryCast<GameObject>();
                        if (go != null) sb.AppendLine($"  (prefab components: {string.Join(", ", go.GetComponentsInChildren<Component>(true).Select(c => c.GetIl2CppType().Name).Distinct())})");
                    }
                }
                catch (Exception e) { sb.AppendLine($"\n(Resources.Load '{path}' failed: {e.Message})"); }
            }
            if (biome == null)
            {
                sb.AppendLine($"\n=== Biome {b} ({(BiomeHolder.Biomes)b}): not loaded (path '{path}')");
                continue;
            }
            DumpBiome(sb, $"Biome {b} ({(BiomeHolder.Biomes)b})", biome);
        }

        sb.AppendLine("\n=== Steed / shrine prefab swaps per biome");
        for (int b = 0; b < biomes.Length; b++)
        {
            BiomeData biome = biomes[b];
            if (biome == null && paths != null && b < paths.Length)
                try { biome = Resources.Load(paths[b])?.TryCast<BiomeData>(); } catch { }
            var swaps = biome?.swapData?.prefabSwapPool;
            if (swaps == null) continue;
            foreach (var swap in swaps)
            {
                if (swap?.original == null) continue;
                string Describe(GameObject go)
                {
                    if (go == null) return "null";
                    var steed = go.GetComponent<Steed>();
                    if (steed != null) return $"{go.name}:steed:{steed.steedType}";
                    var spawn = go.GetComponentInChildren<SteedSpawn>(true);
                    if (spawn != null) return $"{go.name}:shrine[{string.Join("/", spawn.steeds.Where(s => s != null).Select(s => s.steedType.ToString()))}]";
                    var statue = go.GetComponentInChildren<Statue>(true);
                    if (statue != null) return $"{go.name}:statue:{statue.deity}";
                    var cabin = go.GetComponentInChildren<Cabin>(true);
                    if (cabin != null) return $"{go.name}:cabin:{cabin.hermitType}";
                    return null;
                }
                var from = Describe(swap.original);
                var to = Describe(swap.swap);
                if (from != null || (to != null && to != "null"))
                    sb.AppendLine($"  {(BiomeHolder.Biomes)b}: {from ?? swap.original.name} -> {to ?? swap.swap?.name}");
            }
        }
        sb.AppendLine("\n=== Every SteedSpawn prefab in memory");
        foreach (var spawn in Resources.FindObjectsOfTypeAll<SteedSpawn>())
            if (spawn != null) sb.AppendLine($"  {spawn.name} (root {spawn.transform.root.name}) steeds=[{string.Join("/", spawn.steeds.Where(s => s != null).Select(s => s.steedType.ToString()))}]");
        sb.AppendLine("\n=== Every Steed prefab in memory");
        foreach (var steed in Resources.FindObjectsOfTypeAll<Steed>())
            if (steed != null) sb.AppendLine($"  {steed.name} {steed.steedType}");

        sb.AppendLine("\n=== Every BiomeData in memory");
        foreach (var biome in Resources.FindObjectsOfTypeAll<BiomeData>())
            if (biome != null) sb.AppendLine($"  {biome.name} configs={biome.levelConfigs?.Length ?? 0}");
        sb.AppendLine("\n=== Every LevelConfig in memory");
        foreach (var config in Resources.FindObjectsOfTypeAll<LevelConfig>())
            if (config != null) sb.AppendLine($"  {config.name}");
    }

    private static void DumpBiome(StringBuilder sb, string title, BiomeData biome)
    {
        sb.AppendLine($"\n=== {title}: {biome.name} block={biome.blockName} maxIslands={biome.MaxIslands} base={(biome.baseData != null ? biome.baseData.name : "-")}");
        var configs = biome.levelConfigs;
        if (configs == null) return;
        for (int i = 0; i < configs.Length; i++)
        {
            var config = configs[i];
            if (config == null) continue;
            sb.AppendLine($"  -- Config {i}: {config.name} block={config.blockName} quest={config.questType} coins={config.startingCoins} gems={config.startingGems} gemCount={config.gemCount} freeBoatParts={config.freeBoatParts} twoCliffs={config.twoCliffs} caveless={config.caveless} riverless={config.riverless}");
            if (config.groupCounts != null)
                foreach (var gc in config.groupCounts)
                    if (gc != null && gc.count.max > 0) sb.AppendLine($"     {gc.group} = {gc.count.min}..{gc.count.max}");
            var shared = config.sharedBlocksConfigs;
            if (shared == null) continue;
            foreach (var cfg in shared)
            {
                if (cfg == null) continue;
                sb.AppendLine($"     shared config {cfg.name}:");
                if (cfg.IslandWideGroupCounts != null)
                    foreach (var gc in cfg.IslandWideGroupCounts)
                        if (gc != null) sb.AppendLine($"       island-wide {gc.group} = {gc.count.min}..{gc.count.max}");
                if (cfg.CampaignWideGroupCounts != null)
                    foreach (var cw in cfg.CampaignWideGroupCounts)
                    {
                        if (cw == null) continue;
                        var rules = cw.SelectionRules == null ? "" : string.Join(",", cw.SelectionRules.Select(r => r.ToString()));
                        sb.AppendLine($"       campaign-wide {cw.Group} count={cw.Count} rules=[{rules}]");
                    }
            }
        }
    }

    private static void DumpScene(StringBuilder sb)
    {
        sb.AppendLine($"\n=== Objects on the current island ({GameAccess.CurrentIsland})");
        foreach (var s in Object.FindObjectsByType<SteedSpawn>(FindObjectsSortMode.None))
            sb.AppendLine($"  shrine {s.name} x={s.transform.position.x:F0} steeds=[{string.Join("/", s.steeds.Where(st => st != null).Select(st => $"{st.name}:{st.steedType}"))}]");
        foreach (var s in Object.FindObjectsByType<Steed>(FindObjectsSortMode.None))
            sb.AppendLine($"  steed {s.name} {s.steedType} x={s.transform.position.x:F0}");
        foreach (var s in Object.FindObjectsByType<Statue>(FindObjectsSortMode.None))
            sb.AppendLine($"  statue {s.name} {s.deity} {s.deityStatus} x={s.transform.position.x:F0}");
        foreach (var c in Object.FindObjectsByType<Cabin>(FindObjectsSortMode.None))
            sb.AppendLine($"  cabin {c.name} {c.hermitType} x={c.transform.position.x:F0}");
        foreach (var h in Object.FindObjectsByType<Hermit>(FindObjectsSortMode.None))
            sb.AppendLine($"  hermit {h.name} {h.Type} x={h.transform.position.x:F0}");
        foreach (var c in Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
            sb.AppendLine($"  chest {c.name} {c.currencyType} x={c.transform.position.x:F0}");
        foreach (var p in Object.FindObjectsByType<Portal>(FindObjectsSortMode.None))
            sb.AppendLine($"  portal {p.name} {p.type} {p.state} x={p.transform.position.x:F0}");
        foreach (var e in Object.FindObjectsByType<EraUpgradeBuilding>(FindObjectsSortMode.None))
            sb.AppendLine($"  era building {e.name} {e.techAge} x={e.transform.position.x:F0}");
        foreach (var m in Object.FindObjectsByType<MasonShrine>(FindObjectsSortMode.None))
            sb.AppendLine($"  mason shrine {m.name} x={m.transform.position.x:F0}");
        foreach (var w in Object.FindObjectsByType<Wharf>(FindObjectsSortMode.None))
            sb.AppendLine($"  wharf {w.name} x={w.transform.position.x:F0}");
        foreach (var d in Object.FindObjectsByType<DogSpawn>(FindObjectsSortMode.None))
            sb.AppendLine($"  dog spawn {d.name} x={d.transform.position.x:F0}");
    }

    private static void DumpBlocks(StringBuilder sb)
    {
        try { DumpScene(sb); }
        catch (Exception e) { sb.AppendLine($"!! scene dump failed: {e}"); }

        var level = GameAccess.Managers?.level;
        if (level == null)
        {
            sb.AppendLine("\nNo level loaded.");
            return;
        }
        sb.AppendLine($"\n=== Level blocks of the current biome ({level.BlocksSceneName})");
        foreach (var block in level.GetLevelBlocks())
        {
            if (block == null) continue;
            var contents = new StringBuilder();
            foreach (var c in block.GetComponentsInChildren<Chest>(true)) contents.Append($" chest:{c.currencyType}");
            foreach (var p in block.GetComponentsInChildren<Portal>(true)) contents.Append($" portal:{p.type}");
            foreach (var s in block.GetComponentsInChildren<Statue>(true)) contents.Append($" statue:{s.deity}");
            foreach (var s in block.GetComponentsInChildren<SteedSpawn>(true))
                contents.Append($" shrine:[{string.Join("/", s.steeds.Where(st => st != null).Select(st => st.steedType.ToString()))}]");
            foreach (var c in block.GetComponentsInChildren<Cabin>(true)) contents.Append($" cabin:{c.hermitType}");
            foreach (var _ in block.GetComponentsInChildren<DogSpawn>(true)) contents.Append(" dog");
            foreach (var _ in block.GetComponentsInChildren<Wharf>(true)) contents.Append(" wharf");
            foreach (var e in block.GetComponentsInChildren<EraUpgradeBuilding>(true)) contents.Append($" era:{e.techAge}");
            foreach (var _ in block.GetComponentsInChildren<MasonShrine>(true)) contents.Append(" mason");
            foreach (var _ in block.GetComponentsInChildren<Merchant>(true)) contents.Append(" merchant");
            foreach (var _ in block.GetComponentsInChildren<PayableGemChest>(true)) contents.Append(" gembank");
            foreach (var _ in block.GetComponentsInChildren<BeggarCamp>(true)) contents.Append(" camp");
            foreach (var p in block.GetComponentsInChildren<Payable>(true))
            {
                var type = p.GetIl2CppType().Name;
                if (type is "Chest" or "Statue" or "SteedSpawn" or "Cabin" or "Wharf" or "Merchant" or "PayableGemChest" or "Steed") continue;
                contents.Append($" payable:{type}");
            }
            sb.AppendLine($"  {block.name}: groups={block.groupOne}/{block.groupTwo}/{block.groupThree} side={block.side} needsUnlock={block.needsUnlock}{contents}");
        }
    }
}
