using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace APKingdom2Crown;

// The always-on notification feed: items you send and receive, traps, and connection news, fading out
// after a few seconds. Lives in a screen corner, independent of the menu.
internal static class Feed
{
    private sealed class Entry
    {
        public string Text;
        public string Plain;
        public float Born;
    }

    private static readonly List<Entry> Entries = new();
    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Compiled);
    private const float Width = 460f;
    private const float FadeTime = 1f;

    public static void Add(string richText)
    {
        Entries.Add(new Entry { Text = richText, Plain = Tags.Replace(richText, ""), Born = Time.unscaledTime });
        if (Entries.Count > 30) Entries.RemoveAt(0);
    }

    public static void Draw()
    {
        var settings = ClientSettings.Current;
        if (!settings.FeedEnabled) return;

        float now = Time.unscaledTime;
        Entries.RemoveAll(e => now - e.Born > settings.FeedDuration + FadeTime);
        if (Entries.Count == 0) return;

        float scale = Mathf.Clamp(settings.FeedScale, 0.5f, 2.5f);
        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        float screenW = Screen.width / scale;
        float screenH = Screen.height / scale;
        bool right = settings.FeedCorner is 0 or 2;
        bool bottom = settings.FeedCorner is 0 or 1;
        float x = right ? screenW - Width - 14f : 14f;

        int first = Mathf.Max(0, Entries.Count - Mathf.Clamp(settings.FeedLines, 1, 20));
        var heights = new List<float>();
        float total = 0f;
        for (int i = first; i < Entries.Count; i++)
        {
            float h = LineHeight(Entries[i].Text);
            heights.Add(h);
            total += h + 3f;
        }

        float y = bottom ? screenH - total - 14f : 14f;
        for (int i = first; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            float h = heights[i - first];
            float age = now - entry.Born;
            float alpha = age > settings.FeedDuration ? Mathf.Clamp01(1f - (age - settings.FeedDuration) / FadeTime) : 1f;
            var rect = new Rect(x, y, Width, h);

            // GUI.DrawTexture is stripped from this build, so the backdrop is a box with a solid style.
            GUI.color = new Color(0.06f, 0.05f, 0.03f, alpha * 0.6f);
            GUI.Box(rect, GUIContent.none, Styles.Solid);
            var textRect = new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, rect.height - 4f);
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.Label(new Rect(textRect.x + 1f, textRect.y + 1f, textRect.width, textRect.height), entry.Plain, Styles.FeedText);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(textRect, entry.Text, Styles.FeedText);
            y += h + 3f;
        }

        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    private static float LineHeight(string text)
    {
        try { return Mathf.Max(24f, Styles.FeedText.CalcHeight(new GUIContent(text), Width - 16f) + 4f); }
        catch { return 24f; }
    }
}
