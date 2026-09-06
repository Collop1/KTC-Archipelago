using UnityEngine;

namespace APKingdom2Crown;

public class ArchipelagoPauseUI : MonoBehaviour
{
    private string host = "archipelago.gg:38281";
    private string slot = "Monarch";
    private string password = "";
    private int deathLinkMode = 0;
    private readonly string[] deathLinkOptions = new string[] { "None", "Easy", "Hard" };
    private void Awake()
    {
        // Load from your plugin config
        host = Plugin.ConfigHost.Value;
        slot = Plugin.ConfigSlot.Value;
        password = Plugin.ConfigPassword.Value;
        deathLinkMode = Plugin.ConfigDeathLinkMode.Value;
    }
    private void OnGUI()
    {
        if (!gameObject.activeSelf) return;

        // Simple static layout using GUI buttons (no TextField)
        float y = 100;
        float x = 100;

        if (!Plugin.APconnected)
        {
            GUI.Box(new Rect(x, y, 300, 210), "Archipelago Connect");
            y += 20;

            GUI.Label(new Rect(x + 10, y, 280, 25), $"Host: {host}");
            y += 20;

            GUI.Label(new Rect(x + 10, y, 280, 25), $"Slot: {slot}");
            y += 20;

            GUI.Label(new Rect(x + 10, y, 280, 25), "Death Link Mode:");
            y += 25;
            if (GUI.Button(new Rect(x + 10, y, 280, 25), deathLinkOptions[deathLinkMode]))
            {
                deathLinkMode = (deathLinkMode + 1) % deathLinkOptions.Length;
            }
            y += 30;
            if (GUI.Button(new Rect(x + 10, y, 280, 25), "Reload Config"))
            {
                Plugin.ReloadConfig();
                host = Plugin.ConfigHost.Value;
                slot = Plugin.ConfigSlot.Value;
                password = Plugin.ConfigPassword.Value;
            }
            y += 25;
            if (GUI.Button(new Rect(x + 10, y, 280, 25), "Open Config"))
                Plugin.OpenConfig();
            y += 30;

            if (GUI.Button(new Rect(x + 10, y, 280, 30), "Connect")) Connect();
        }
        else
        {
            GUI.Box(new Rect(x, y, 300, 120), $"Connected to {host} as {slot}");
            if (GUI.Button(new Rect(x + 10, y + 50, 280, 30), "Disconnect")) Disconnect();
        }
    }
    private async void Connect()
    {
        Plugin.Log.LogInfo($"[AP] Connecting to {host} as {slot} with DeathLink {deathLinkOptions[deathLinkMode]}");
        Plugin.ConfigDeathLinkMode.Value = deathLinkMode;
        Plugin.APconnected = await Plugin.AP.Connect(host, slot, password);

    }

    private async void Disconnect()
    {
        Plugin.Log.LogInfo("[AP] Disconnecting from Archipelago");
        await Plugin.AP.Session.Socket.DisconnectAsync();
        Plugin.APconnected = false;
    }
}
