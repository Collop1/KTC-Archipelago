using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace APKingdom2Crown;

[HarmonyPatch(typeof(ProgressHelper), "Awake")]
public class ProgressHelperAwakePatch
{
    static void Postfix(ProgressHelper __instance)
    {
        var go = __instance.gameObject;

        // Check if component exists
        if (go.GetComponent(Il2CppType.Of<ProgressUpdateChecks>()) == null)
        {
            go.AddComponent(Il2CppType.Of<ProgressUpdateChecks>());
            Plugin.Log.LogInfo("[AP] ProgressUpDateChecks added to ProgressHelper");
        }
    }
}

public class ProgressUpdateChecks : MonoBehaviour
{
    private ProgressHelper PH;
    private bool[] lastStates;
    private int checkTimer = 30; // Check every ~0.5 seconds
    private bool goalAlreadySent = false;

    private void Awake()
    {
        PH = GetComponent<ProgressHelper>();
        if (PH == null)
        {
            Plugin.Log.LogWarning("[AP] ProgressUpdateChecks added to object without ProgressHelper component!");
            Destroy(this);
            return;
        }

        if (PH.securedIslands != null)
        {
            lastStates = new bool[PH.securedIslands.Length];
            for (int i = 0; i < PH.securedIslands.Length; i++)
                lastStates[i] = PH.securedIslands[i];
        }
    }

    private void Update()
    {
        if (!Plugin.APconnected) return;
        if (checkTimer-- > 0) return;
        checkTimer = 30;

        if (PH.securedIslands == null || lastStates == null)
            return;

        bool allSecured = true;

        for (int i = 0; i < PH.securedIslands.Length; i++)
        {
            bool current = PH.securedIslands[i];
            if (current != lastStates[i])
            {
                if (current)
                {
                    Plugin.Log.LogInfo($"[AP] Island {i + 1} Liberated → Sending Check");
                    Plugin.AP?.CompleteLocation(APIDRegistry.Get($"Island {i + 1} Liberation"));
                }

                lastStates[i] = current;
            }

            if (!current)
                allSecured = false;
        }

        if (allSecured && !goalAlreadySent)
        {
            goalAlreadySent = true;
            Plugin.Log.LogInfo("[AP] All Islands Secured → Goal Achieved!");
            Plugin.AP?.Session.SetGoalAchieved();
        }
    }
}
