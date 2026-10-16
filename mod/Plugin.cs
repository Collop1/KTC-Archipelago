using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace APKingdom2Crown;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo("Loading Archipelago integration...");

        ClientSettings.Load();

        ClassInjector.RegisterTypeInIl2Cpp<APRunner>();
        ClassInjector.RegisterTypeInIl2Cpp<ModMenu>();

        ApplyPatches();

        AddComponent<APRunner>();
        AddComponent<ModMenu>();
        AP.OnConnected += CheckTracker.ResendEarned;

        Log.LogInfo($"Archipelago for Kingdom Two Crowns v{MyPluginInfo.PLUGIN_VERSION} ready.");
    }

    // Patch each class on its own so one hook that no longer matches the game (after a game update,
    // say) doesn't take every other hook down with it.
    private static void ApplyPatches()
    {
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        var patchClasses = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0);
        int ok = 0, failed = 0;
        foreach (var type in patchClasses)
        {
            try
            {
                harmony.CreateClassProcessor(type).Patch();
                ok++;
            }
            catch (Exception e)
            {
                failed++;
                Log.LogError($"[AP] Patch {type.Name} failed: {e.GetBaseException().Message}");
            }
        }
        Log.LogInfo($"[AP] Applied {ok} patches ({failed} failed).");
    }
}
