using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace APKingdom2Crown;

public class InGameLogger : MonoBehaviour
{
    private TMP_Text tmp;
    private RectTransform panel;
    private readonly Queue<string> logs = new Queue<string>();
    private const int MaxLogs = 5;

    // Target size in pixels at 1920x1080 reference resolution
    private const float RefWidth = 1920f;
    private const float RefHeight = 1080f;
    private const float RefX = 1576f;
    private const float RefY = 0f;
    private const float RefW = 344f;
    private const float RefH = 160f;

    public Canvas canvas;

    private void Awake()
    {
        canvas = PluginUI.UILogCanvas;
        if (canvas == null)
        {
            var canvasGO = new GameObject("APInGameCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create Panel
        var panelGO = new GameObject("APLogPanel");
        panelGO.transform.SetParent(canvas.transform);
        panel = panelGO.AddComponent<RectTransform>();

        // Anchor bottom-right
        panel.anchorMin = new Vector2(0, 0);
        panel.anchorMax = new Vector2(0, 0);
        panel.pivot = new Vector2(0, 0);

        // Scale to screen size
        float scaleX = Screen.width / RefWidth;
        float scaleY = Screen.height / RefHeight;

        panel.anchoredPosition = new Vector2(RefX * scaleX, RefY * scaleY);
        panel.sizeDelta = new Vector2(RefW * scaleX, RefH * scaleY);

        // Background (optional)
        var img = panelGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f); // semi-transparent black

        // Create TMP text
        var textGO = new GameObject("APLogText");
        textGO.transform.SetParent(panelGO.transform);
        tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.font = PluginUI.KingdomTMPFont; // Ensure font is loaded
        tmp.color = new Color32(0xff, 0xf2, 0xdb, 255);
        tmp.enableWordWrapping = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.fontSize = 24;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        var textRT = tmp.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.offsetMin = new Vector2(5, 5);
        textRT.offsetMax = new Vector2(-5, -5);
    }

    public void AddLog(string text)
    {

        if (tmp == null) return;

        logs.Enqueue(text);
        if (logs.Count > MaxLogs) logs.Dequeue();

        tmp.text = "";
        foreach (var log in logs)
        {
            tmp.text += $"• {log}\n";
        }
    }

    public void ClearLogs()
    {
        logs.Clear();
        if (tmp != null)
            tmp.text = "";
    }
}
