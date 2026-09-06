using System.Collections.Generic;

namespace APKingdom2Crown;

public static class APHelper
{
    public static int? GetNextUncheckedLocation(string baseName, int count)
    {
        if (Plugin.AP?.Session?.Locations.AllLocationsChecked == null)
        {
            Plugin.Log.LogWarning("[AP] Session or location data not ready.");
            return null;
        }

        var checkedLocations = Plugin.AP.Session.Locations.AllLocationsChecked;

        for (int i = 1; i <= count; i++)
        {
            string locName = $"{baseName} {i}";
            var id = APIDRegistry.Get(locName);
            if (id == -1)
            {
                Plugin.Log.LogWarning($"[AP] Unknown location name: {locName}");
                continue;
            }

            if (!checkedLocations.Contains(id))
            {
                Plugin.Log.LogInfo($"[AP] Next unchecked location found: {locName} ({id})");
                return id;
            }
        }

        Plugin.Log.LogInfo($"[AP] All locations for {baseName} already checked.");
        return null;
    }

    public static void SendNextProgressiveCheck(string baseName, int totalCount)
    {
        int? next = GetNextUncheckedLocation(baseName, totalCount);
        if (next == null)
        {
            Plugin.Log.LogInfo($"[AP] No unchecked progressive location for {baseName}.");
            return;
        }

        Plugin.Log.LogInfo($"[AP] Sending check for progressive location: {baseName} ({next})");
        Plugin.AP?.CompleteLocation((int)next);
    }
}

public static class APIDRegistry
{
    private const int BASE_ID = 755067;
    private static int _nextId = BASE_ID;
    private static readonly Dictionary<string, int> _ids = new();

    /// <summary>
    /// Register a name → ID mapping.
    /// </summary>
    public static void AddNew(string name, int? id = null)
    {
        if (_ids.ContainsKey(name))
        {
            Plugin.Log.LogWarning($"[AP] Duplicate ID registration skipped for '{name}'");
            return;
        }

        int finalId = id ?? _nextId++;
        _ids[name] = finalId;
        Plugin.Log.LogInfo($"[AP] Registered '{name}' → {finalId}");
    }

    /// <summary>
    /// Retrieve a registered ID by name.
    /// </summary>
    public static int Get(string name)
    {
        if (_ids.TryGetValue(name, out int id))
            return id;

        Plugin.Log.LogError($"[AP] No ID found for '{name}'");
        return -1;
    }

    /// <summary>
    /// Convenience: get all registered IDs.
    /// </summary>
    public static IReadOnlyDictionary<string, int> All => _ids;
}

public static class APIDsRegisterClass
{
    public static void RegisterAllIds()
    {
        // --- Progressive Portals ---
        APIDRegistry.AddNew("Destroy Portal Progressive 1", 755067);            //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 2", 755068);            //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 3", 755069);            //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 4", 755070);            //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 5", 755071);            //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 6", 755072);            //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 7", 755073);            //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 8", 755074);            //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 9", 755075);            //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 10", 755076);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 11", 755077);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 12", 755078);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 13", 755079);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 14", 755080);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 15", 755081);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 16", 755082);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 17", 755083);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 18", 755084);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 19", 755085);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 20", 755086);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 21", 755087);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 22", 755088);           //Location  1
        APIDRegistry.AddNew("Destroy Portal Progressive 23", 755089);           //Location  1

        // --- Progressive Castle Unlock ---
        APIDRegistry.AddNew("Progressive Castle Unlock T1", 755091);            //Item      1
        APIDRegistry.AddNew("Progressive Castle Unlock T2", 755092);            //Item      1
        APIDRegistry.AddNew("Progressive Castle Unlock T3", 755093);            //Item      1
        APIDRegistry.AddNew("Progressive Castle Unlock T4", 755094);            //Item      1
        APIDRegistry.AddNew("Progressive Castle Unlock T5", 755095);  //Stone   //Item      1
        APIDRegistry.AddNew("Progressive Castle Unlock T6", 755177);  //Stone   //Item      1
        APIDRegistry.AddNew("Progressive Castle Unlock T7", 755178);  //Iron    //Item      1

        // --- Hermits ---
        APIDRegistry.AddNew("Hermit Ballista Unlock", 755096);                  //Location  1
        APIDRegistry.AddNew("Hermit Ballista Use", 755097);                     //Item      1
        APIDRegistry.AddNew("Hermit Bakery Unlock", 755098);                    //Location  1
        APIDRegistry.AddNew("Hermit Bakery Use", 755099);                       //Item      1
        APIDRegistry.AddNew("Hermit Horn Unlock", 755100);                      //Location  1
        APIDRegistry.AddNew("Hermit Horn Use", 755101);                         //Item      1
        APIDRegistry.AddNew("Hermit Stable Unlock", 755102);                    //Location  1
        APIDRegistry.AddNew("Hermit Stable Use", 755103);                       //Item      1
        APIDRegistry.AddNew("Hermit Warrior Unlock", 755104);                   //Location  1
        APIDRegistry.AddNew("Hermit Warrior Use", 755105);                      //Item      1

        // --- Island 1 ---
        APIDRegistry.AddNew("Island 1 Liberation", 755106);                     //Location  1
        APIDRegistry.AddNew("Progressive Coin Chest 1", 755107);                //Location  1
        APIDRegistry.AddNew("Progressive Coin Chest 2", 755108);                //Location  1
        APIDRegistry.AddNew("Progressive Coin Chest 3", 755109);                //Location  1

        // --- Island 2 ---
        APIDRegistry.AddNew("Island 2 Liberation", 755110);                     //Location  1
        APIDRegistry.AddNew("Progressive Coin Chest 4", 755111);                //Location  1
        APIDRegistry.AddNew("Progressive Coin Chest 5", 755112);                //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 1", 755113);                  //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 2", 755114);                  //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 3", 755115);                  //Location  1
        APIDRegistry.AddNew("Island 2 Dog", 755116);                            //Location

        // --- Island 3 ---
        APIDRegistry.AddNew("Island 3 Liberation", 755117);                     //Location
        APIDRegistry.AddNew("Progressive Coin Chest 6", 755118);                //Location  1
        APIDRegistry.AddNew("Progressive Coin Chest 7", 755119);                //Location  1
        APIDRegistry.AddNew("Progressive Coin Chest 8", 755120);                //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 4", 755121);                  //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 5", 755122);                  //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 6", 755123);                  //Location  1

        // --- Island 4 ---
        APIDRegistry.AddNew("Island 4 Liberation", 755124);                     //Location
        APIDRegistry.AddNew("Progressive Coin Chest 9", 755125);                //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 7", 755126);                  //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 8", 755127);                  //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 9", 755128);                  //Location  1

        // --- Island 5 ---
        APIDRegistry.AddNew("Island 5 Liberation", 755129);                     //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 10", 755130);                 //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 11", 755131);                 //Location  1
        APIDRegistry.AddNew("Progressive Coin Gem 12", 755132);                 //Location  1

        // --- Mount ---
        APIDRegistry.AddNew("Bear Unlocks", 755133);                            //Location  1
        APIDRegistry.AddNew("Stag Unlocks", 755134);                            //Location  1
        APIDRegistry.AddNew("Lizard Unlocks", 755135);                          //Location  1
        APIDRegistry.AddNew("Unicorn Unlocks", 755136);                         //Location  1
        APIDRegistry.AddNew("Wolf P1 Unlocks", 755137);                         //Location  1
        APIDRegistry.AddNew("Warhorse P1 Unlocks", 755138);                     //Location  1
        APIDRegistry.AddNew("Horse Fast Unlocks", 755139);                      //Location  1
        APIDRegistry.AddNew("Griffin P1 Unlocks", 755140);                      //Location  1
        APIDRegistry.AddNew("Griffin Skull P1 Unlocks", 755141);                //Location  1
        APIDRegistry.AddNew("Horse Stamina Unlocks", 755142);                   //Location  1
        APIDRegistry.AddNew("Wolf_norselands Unlocks", 755143);                 //Location  1
        APIDRegistry.AddNew("Reindeer Unlocks", 755144);                        //Location  1
        APIDRegistry.AddNew("Horse Burst Unlocks", 755145);                     //Location  1
        APIDRegistry.AddNew("Rainbow Pony Unlocks", 755146);                    //Location  1
        APIDRegistry.AddNew("Warhorse Plague P1 Unlocks", 755147);              //Location  1
        APIDRegistry.AddNew("Wolf Norselands P1 Unlocks", 755148);              //Location  1
        APIDRegistry.AddNew("Unicorn Dark Unlocks", 755149);                    //Location  1

        APIDRegistry.AddNew("Bear Use", 755150);                                //Items     1
        APIDRegistry.AddNew("Stag Use", 755151);                                //Items     1
        APIDRegistry.AddNew("Lizard Use", 755152);                              //Items     1
        APIDRegistry.AddNew("Unicorn Use", 755153);                             //Items     1
        APIDRegistry.AddNew("Wolf P1 Use", 755154);                             //Items     1
        APIDRegistry.AddNew("Warhorse P1 Use", 755155);                         //Items     1
        APIDRegistry.AddNew("Horse Fast Use", 755156);                          //Items     1
        APIDRegistry.AddNew("Griffin P1 Use", 755157);                          //Items     1
        APIDRegistry.AddNew("Griffin Skull P1 Use", 755158);                    //Items     1
        APIDRegistry.AddNew("Horse Stamina Use", 755159);                       //Items     1
        APIDRegistry.AddNew("Wolf_norselands Use", 755160);                     //Items     1
        APIDRegistry.AddNew("Reindeer Use", 755161);                            //Items     1
        APIDRegistry.AddNew("Horse Burst Use", 755162);                         //Items     1
        APIDRegistry.AddNew("Rainbow Pony Use", 755163);                        //Items     1
        APIDRegistry.AddNew("Warhorse Plague P1 Use", 755164);                  //Items     1
        APIDRegistry.AddNew("Wolf Norselands P1 Use", 755165);                  //Items     1
        APIDRegistry.AddNew("Unicorn Dark Use", 755166);                        //Items     1

        // --- Status ---
        APIDRegistry.AddNew("Archery Use", 755167);                             //Items     1
        APIDRegistry.AddNew("Scythe Use", 755168);                              //Items     1
        APIDRegistry.AddNew("Building Use", 755169);                            //Items     1
        APIDRegistry.AddNew("Knights Use", 755170);                             //Items     1

        APIDRegistry.AddNew("Archery Unlocks", 755171);                         //Location  1
        APIDRegistry.AddNew("Scythe Unlocks", 755172);                          //Location  1
        APIDRegistry.AddNew("Building Unlocks", 755173);                        //Location  1
        APIDRegistry.AddNew("Knights Unlocks", 755174);                         //Location  1

        // --- Tech Use ---
        APIDRegistry.AddNew("Stone Technology", 755175);                        //Item      1
        APIDRegistry.AddNew("Iron Technology", 755176);                         //Item      1

        // NOTE: was 755177 previously, which collided with "Progressive Castle Unlock T6".
        // Bumped to the next free id after T7 (755178) so junk has its own id.
        APIDRegistry.AddNew("Random Game Effect (junk)", 755179);               //Junk

        int[] hermitUseIds = { 755097, 755099, 755101, 755103, 755105 };
        int[] mountUseIds = { 755150, 755151, 755152, 755153, 755154, 755155, 755156, 755157, 755158, 755159, 755160, 755161, 755162, 755163, 755164, 755165, 755166 };
        int[] statusUseIds = { 755167, 755168, 755169, 755170 };
        int[] techUseIds = { 755175, 755176 };
        int[] castleUseIds = { 755091, 755092, 755093, 755094, 755095 };
        int[] junkIds = { 755179 };

        APItemRegistry.RegisterItems(hermitUseIds);
        APItemRegistry.RegisterItems(mountUseIds);
        APItemRegistry.RegisterItems(statusUseIds);
        APItemRegistry.RegisterItems(techUseIds);
        APItemRegistry.RegisterItems(castleUseIds);
        APItemRegistry.RegisterItems(junkIds);
    }

    public static int GetID(string name) => APIDRegistry.Get(name);
}
