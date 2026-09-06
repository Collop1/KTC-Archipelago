using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace APKingdom2Crown;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static bool APconnected = false;
    internal static new ManualLogSource Log;
    internal static Archipelago.APConnection AP;

    public static ConfigEntry<string> ConfigHost;
    public static ConfigEntry<string> ConfigSlot;
    public static ConfigEntry<string> ConfigPassword;
    public static ConfigEntry<int> ConfigDeathLinkMode;

    public static Plugin Instance { get; private set; }

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo("Loading Archipelago integration...");
        Instance = this;

        ConfigHost = Config.Bind("Archipelago", "Host", "archipelago.gg:38281",
    "The address and port of the Archipelago server.");

        ConfigSlot = Config.Bind("Archipelago", "Slot", "Monarch",
            "Your Archipelago slot name.");

        ConfigPassword = Config.Bind("Archipelago", "Password", "",
            "Your Archipelago game password.");

        ConfigDeathLinkMode = Config.Bind("Archipelago", "DeathLinkMode", 0,
            "Death Link Mode: 0=None, 1=Easy, 2=Hard");

        TouchTypes();

        ClassInjector.RegisterTypeInIl2Cpp<ProgressUpdateChecks>();
        ClassInjector.RegisterTypeInIl2Cpp<StatueBlocker>();
        ClassInjector.RegisterTypeInIl2Cpp<ArchipelagoPauseUI>();
        ClassInjector.RegisterTypeInIl2Cpp<InGameLogger>();


        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        APIDsRegisterClass.RegisterAllIds();

        AddComponent<KeyHandler>();

        AP = new Archipelago.APConnection();

        Log.LogInfo("Archipelago Kingdom Two Crowns mod Fully initialized!");
    }
    public static void OpenConfig()
    {
        System.Diagnostics.Process.Start("notepad.exe", Instance.Config.ConfigFilePath);
    }
    public static void ReloadConfig()
    {
        Instance.Config.Reload();
    }

    public static void TouchTypes()
    {
        _ = typeof(ProgressUpdateChecks);
        _ = typeof(StatueBlocker);
        _ = typeof(ArchipelagoPauseUI);
        _ = typeof(InGameLogger);
    }

}
