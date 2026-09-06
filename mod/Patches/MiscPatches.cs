using HarmonyLib;

namespace APKingdom2Crown;

[HarmonyPatch(typeof(DogSpawn), "FreeDog")]
public class DogSpawnFreeDogPatch
{
    static void Prefix(DogSpawn __instance)
    {
        if (!Plugin.APconnected) return;
        Plugin.AP?.CompleteLocation(APIDRegistry.Get("Hermit Ballista Unlock"));
        Plugin.Log.LogInfo($"[AP] Dog acquired  → Island 2 Dog");
    }
}

[HarmonyPatch(typeof(Player), "LoseCrown")]
public class PlayerLoseCrownPatch
{
    static void Postfix(Player __instance)
    {
        if (!Plugin.APconnected) return;
        //TODO Death Link Packet
        Plugin.AP?.Session.Socket.SendPacket(null);
        Plugin.Log.LogInfo($"[AP] Dog acquired  → Island 2 Dog");
    }
}
