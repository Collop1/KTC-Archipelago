using System;
using UnityEngine;

namespace APKingdom2Crown;

// Per-frame driver: drains the Archipelago queue, applies items, polls checks, enforces gating and
// periodically saves per-seed progress.
public class APRunner : MonoBehaviour
{
    public APRunner(IntPtr ptr) : base(ptr) { }

    private float _nextSave;

    private void Update()
    {
        AP.Pump();
        Step(ItemHandler.Update);
        Step(ItemEffects.Tick);
        Step(CheckTracker.Update);
        Step(Gating.Update);

        if (Time.unscaledTime >= _nextSave)
        {
            _nextSave = Time.unscaledTime + 10f;
            if (AP.State.Dirty) AP.State.Save();
        }
    }

    private void OnApplicationQuit()
    {
        AP.State.Save();
        ClientSettings.Save();
    }

    private static void Step(Action step)
    {
        try { step(); }
        catch (Exception e) { Plugin.Log.LogError($"[AP] {step.Method.DeclaringType?.Name}: {e}"); }
    }
}
