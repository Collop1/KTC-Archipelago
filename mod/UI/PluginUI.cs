using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace APKingdom2Crown;

public static class PluginUI
{
    public static Canvas UILogCanvas;
    public static TMP_FontAsset KingdomTMPFont;
    public static InGameLogger Logger;

    public static void SetupUI()
    {
        // Create Canvas
        if (UILogCanvas == null)
        {
            GameObject canvasGO = new GameObject("AP_UI_Canvas");
            UILogCanvas = canvasGO.AddComponent<Canvas>();
            UILogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            UnityEngine.Object.DontDestroyOnLoad(canvasGO);
        }

        if (KingdomTMPFont != null) return;

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in fonts)
        {
            if (font != null && font.name.Contains("Kingdom", StringComparison.OrdinalIgnoreCase))
            {
                KingdomTMPFont = font;
                Plugin.Log.LogInfo("[AP] Kingdom_TMP font found!");
                break;
            }
        }

        if (KingdomTMPFont == null)
            Plugin.Log.LogWarning("[AP] Kingdom_TMP font not found, default font will be used!");

        // Add InGameLogger
        if (Logger == null)
        {
            GameObject go = new GameObject("AP_InGameLogger");
            go.transform.SetParent(UILogCanvas.transform, false);
            Logger = go.AddComponent<InGameLogger>();
        }
    }
}
