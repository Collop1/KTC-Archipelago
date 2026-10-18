using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace APKingdom2Crown;

// Thin wrappers over GUILayout that avoid making game-side garbage every frame.
//
// Through the interop layer every GUILayout.Label("text", style, options...) copies the text into a new game-side
// string and wraps the options in a new game-side array (even when there are none), and GUILayout.Width(...) makes
// a new option object each time. The game runs without incremental GC, so that garbage turned into full
// stop-the-world collections several times a second while the menu was open. Here the text is turned into a
// GUIContent once per distinct string and option arrays are built once, so drawing the same menu again allocates
// next to nothing on the game's side.
internal static class Gui
{
    // Passing a typed null picks the overloads that take the game-side array directly; Unity treats null as "no options".
    public static readonly Il2CppReferenceArray<GUILayoutOption> None = null;

    private const int MaxCachedContents = 4000;
    private static readonly Dictionary<string, GUIContent> Contents = new();
    private static readonly Dictionary<(int Kind, float A, float B), Il2CppReferenceArray<GUILayoutOption>> Options = new();

    public static GUIContent Content(string text)
    {
        text ??= "";
        if (Contents.TryGetValue(text, out var content)) return content;
        if (Contents.Count >= MaxCachedContents) Contents.Clear(); // text that changes every frame; start over
        content = new GUIContent(text);
        Contents[text] = content;
        return content;
    }

    public static void Label(string text, GUIStyle style, Il2CppReferenceArray<GUILayoutOption> options = null) =>
        GUILayout.Label(Content(text), style, options);

    public static bool Button(string text, GUIStyle style, Il2CppReferenceArray<GUILayoutOption> options = null) =>
        GUILayout.Button(Content(text), style, options);

    public static void BeginHorizontal() => GUILayout.BeginHorizontal(None);
    public static void BeginVertical() => GUILayout.BeginVertical(None);
    public static void BeginVertical(GUIStyle style, Il2CppReferenceArray<GUILayoutOption> options = null) =>
        GUILayout.BeginVertical(style, options);

    public static Il2CppReferenceArray<GUILayoutOption> Width(float width) =>
        Cached(0, width, 0f, () => new[] { GUILayout.Width(width) });

    public static Il2CppReferenceArray<GUILayoutOption> Height(float height) =>
        Cached(1, height, 0f, () => new[] { GUILayout.Height(height) });

    public static Il2CppReferenceArray<GUILayoutOption> Size(float width, float height) =>
        Cached(2, width, height, () => new[] { GUILayout.Width(width), GUILayout.Height(height) });

    public static Il2CppReferenceArray<GUILayoutOption> HeightExpandWidth(float height) =>
        Cached(3, height, 0f, () => new[] { GUILayout.Height(height), GUILayout.ExpandWidth(true) });

    public static Il2CppReferenceArray<GUILayoutOption> HeightMinWidth(float height, float minWidth) =>
        Cached(4, height, minWidth, () => new[] { GUILayout.Height(height), GUILayout.MinWidth(minWidth) });

    public static Il2CppReferenceArray<GUILayoutOption> MinWidth(float minWidth) =>
        Cached(5, minWidth, 0f, () => new[] { GUILayout.MinWidth(minWidth) });

    public static Il2CppReferenceArray<GUILayoutOption> ExpandWidth =>
        Cached(6, 0f, 0f, () => new[] { GUILayout.ExpandWidth(true) });

    public static Il2CppReferenceArray<GUILayoutOption> ExpandHeight =>
        Cached(7, 0f, 0f, () => new[] { GUILayout.ExpandHeight(true) });

    private static Il2CppReferenceArray<GUILayoutOption> Cached(int kind, float a, float b, System.Func<GUILayoutOption[]> make)
    {
        var key = (kind, a, b);
        if (!Options.TryGetValue(key, out var array))
            Options[key] = array = new Il2CppReferenceArray<GUILayoutOption>(make());
        return array;
    }
}
