using System;
using Coatsink.Common;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

namespace APKingdom2Crown;

public class KeyHandler : MonoBehaviour
{
    private Managers _managers;
    private int _managerSearchCooldown = 0;

    private void Update()
    {
        if (_managers == null)
        {
            TryFindManagers();
            return;
        }
        HandInput();
        if(Plugin.APconnected != PluginUI.UILogCanvas.gameObject.active)PluginUI.UILogCanvas.gameObject.SetActive(Plugin.APconnected);
    }

    private void HandInput()
    {
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {

            Plugin.Log.LogInfo("Butter Fingers");

            // Get the Il2CppSystem.Type for Player
            Il2CppSystem.Type PlayerType = Il2CppSystem.Type.GetType("Player, Assembly-CSharp");

            // Find all objects of that type
            var cbObjects = UnityEngine.Object.FindObjectsOfType(PlayerType, true);
            foreach (var obj in cbObjects)
            {
                // Convert from Il2CppSystem.Object to Player safely
                var cb = Il2CppObjectPool.Get<Player>(obj.Pointer);
                // Snapshot the counts before dropping - TryDropCurrency mutates cb.wallet.Coins/Gems,
                // so looping against the live property (the old code did `i < cb.wallet.Coins`) makes
                // the loop bound shrink out from under itself and only drops roughly half the wallet.
                int coinCount = cb.wallet.Coins;
                for (int i = 0; i < coinCount; i++)
                {
                    cb.TryDropCurrency(CurrencyType.Coins);
                }
                int gemCount = cb.wallet.Gems;
                for (int i = 0; i < gemCount; i++)
                {
                    cb.TryDropCurrency(CurrencyType.Gems);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            Plugin.Log.LogInfo("Curse of Midas");

            // Get the Il2CppSystem.Type for Player
            Il2CppSystem.Type PlayerType = Il2CppSystem.Type.GetType("Player, Assembly-CSharp");

            // Find all objects of that type
            var cbObjects = UnityEngine.Object.FindObjectsOfType(PlayerType, true);
            foreach (var obj in cbObjects)
            {
                // Convert from Il2CppSystem.Object to Player safely
                var cb = Il2CppObjectPool.Get<Player>(obj.Pointer);
                cb.wallet.AddCurrency(CurrencyType.Coins, 99);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2))
        {

            // Get the Il2CppSystem.Type for Player
            Il2CppSystem.Type PlayerType = Il2CppSystem.Type.GetType("Player, Assembly-CSharp");

            // Find all objects of that type
            var cbObjects = UnityEngine.Object.FindObjectsOfType(PlayerType, true);
            foreach (var obj in cbObjects)
            {

                var cb = Il2CppObjectPool.Get<Player>(obj.Pointer);
                if (!cb.gameObject) continue;
                if (!cb.gameObject.GetComponent<Petrifiable>()) continue;
                cb.gameObject.GetComponent<Petrifiable>().StartPetrify(1, 30);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            Plugin.Log.LogInfo("Lost Crown Debug");

            // Get the Il2CppSystem.Type for Player
            Il2CppSystem.Type PlayerType = Il2CppSystem.Type.GetType("Player, Assembly-CSharp");

            // Find all objects of that type
            var cbObjects = UnityEngine.Object.FindObjectsOfType(PlayerType, true);
            foreach (var obj in cbObjects)
            {
                // Convert from Il2CppSystem.Object to Player safely
                var cb = Il2CppObjectPool.Get<Player>(obj.Pointer);
                if (cb.hasCrown) cb.LoseCrownDebug();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Keypad4)) DeathLink();
        else if (Input.GetKeyDown(KeyCode.Keypad5)) PluginUI.Logger.AddLog("Received Sword of Destiny!");
        else if (Input.GetKeyDown(KeyCode.Keypad6)) PluginUI.Logger.ClearLogs();
    }
    public void DeathLink()
    {
        Plugin.Log.LogInfo("Deaht Link test");

        // Get the Il2CppSystem.Type for Player
        Il2CppSystem.Type PlayerType = Il2CppSystem.Type.GetType("Player, Assembly-CSharp");

        // Find all objects of that type
        var cbObjects = UnityEngine.Object.FindObjectsOfType(PlayerType, true);
        foreach (var obj in cbObjects)
        {
            // Convert from Il2CppSystem.Object to Player safely
            var cb = Il2CppObjectPool.Get<Player>(obj.Pointer);
            cb.OnReceiveDamageHandler(1, gameObject, DamageSource.Fire);
        }
    }
    private void TryFindManagers()
    {
        if (_managerSearchCooldown > 0)
        {
            _managerSearchCooldown--;
            return;
        }

        try
        {
            _managers = FindObjectOfType<Managers>(true);
        }
        catch (Exception)
        {
            // Can throw during scene bootstrap before the interop object is fully valid -
            // treat it the same as "not found yet" and retry on the next cooldown window.
            _managers = null;
        }

        if (_managers != null)
        {
            Plugin.Log.LogInfo("Managers found.");
        }
        else
        {
            _managerSearchCooldown = 100; // Retry every 100 frames
        }
    }

}
