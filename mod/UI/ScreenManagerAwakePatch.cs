using HarmonyLib;
using Il2CppInterop.Runtime;

namespace APKingdom2Crown;

[HarmonyPatch(typeof(ScreenManager), "Awake")]
public class ScreenManagerAwakePatch
{
    static void Postfix(ScreenManager __instance)
    {
        if (PluginUI.UILogCanvas == null) PluginUI.SetupUI();

        var go = __instance.transform.GetChild(5).gameObject;

        // Check if component exists
        if (go.GetComponent(Il2CppType.Of<ArchipelagoPauseUI>()) == null)
        {
            go.AddComponent(Il2CppType.Of<ArchipelagoPauseUI>());
            Plugin.Log.LogInfo("[AP] APConnectionUI added to ScreenManager → Pause Menue");
        }
    }
}
