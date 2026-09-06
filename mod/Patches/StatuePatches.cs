using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace APKingdom2Crown;

[HarmonyPatch(typeof(Statue), "Awake")]
public class StatueAwakePatch
{
    static void Postfix(Statue __instance)
    {
        var go = __instance.gameObject;

        if (go.GetComponent(Il2CppType.Of<StatueBlocker>()) == null)
        {
            go.AddComponent(Il2CppType.Of<StatueBlocker>());
            Plugin.Log.LogInfo($"[AP] StatueBlocker added to {__instance.deity}");
        }
    }
}

[HarmonyPatch(typeof(Statue), "Pay")]
public class StatueOnPayPatch
{
    static void Prefix(Statue __instance)
    {
        if (!Plugin.APconnected) return;
        if (__instance.deityStatus == Statue.DeityStatus.GemLocked)
        {
            switch (__instance.deity)
            {
                case Statue.Deity.Archer:
                    Plugin.AP?.CompleteLocation(APIDRegistry.Get("Archery Unlocks"));
                    break;
                case Statue.Deity.Worker:
                    Plugin.AP?.CompleteLocation(APIDRegistry.Get("Building Unlocks"));
                    break;
                case Statue.Deity.Knight:
                    Plugin.AP?.CompleteLocation(APIDRegistry.Get("Knights Unlocks"));
                    break;
                case Statue.Deity.Farmer:
                    Plugin.AP?.CompleteLocation(APIDRegistry.Get("Scythe Unlocks"));
                    break;
            }
        }
        Plugin.Log.LogInfo($"[AP] Hermit Brough → Sending Check for Location ");
    }
}

public class StatueBlocker : MonoBehaviour
{
    private Statue statue;
    private int checkTimer = 30; // Check roughly twice per second

    private void Awake()
    {
        statue = GetComponent<Statue>();
        if (statue == null)
        {
            Plugin.Log.LogWarning("[AP] StatueBlocker added to object without Statue component!");
            Destroy(this);
            return;
        }
    }

    private void Update()
    {
        if (!Plugin.APconnected) return;
        // Only check occasionally to save performance
        if (checkTimer-- > 0) return;
        checkTimer = 30;

        bool allowed = false;
        switch (statue.deity)
        {
            case Statue.Deity.Archer:
                allowed = APItemRegistry.IsCollected(APIDRegistry.Get("Archery Use"));
                break;
            case Statue.Deity.Worker:
                allowed = APItemRegistry.IsCollected(APIDRegistry.Get("Building Use"));
                break;
            case Statue.Deity.Knight:
                allowed = APItemRegistry.IsCollected(APIDRegistry.Get("Knights Use"));
                break;
            case Statue.Deity.Farmer:
                allowed = APItemRegistry.IsCollected(APIDRegistry.Get("Scythe Use"));
                break;
        }

        statue.forceBlockPayment = !allowed;
    }
}
