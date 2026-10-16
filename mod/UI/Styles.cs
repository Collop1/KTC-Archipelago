using UnityEngine;

namespace APKingdom2Crown;

// IMGUI styles in the same dark-and-gold look as Kingdom Enhanced's menu. Built lazily inside OnGUI,
// the only place GUI.skin is available.
internal static class Styles
{
    public static readonly Color Bg = new(0.06f, 0.05f, 0.03f);
    public static readonly Color Panel = new(0.10f, 0.08f, 0.05f);
    public static readonly Color CardColor = new(0.13f, 0.10f, 0.06f);
    public static readonly Color Gold = new(0.90f, 0.72f, 0.30f);
    public static readonly Color Text = new(0.95f, 0.90f, 0.75f);
    public static readonly Color TextDim = new(0.60f, 0.55f, 0.45f);
    public static readonly Color On = new(0.22f, 0.72f, 0.32f);
    public static readonly Color Off = new(0.45f, 0.18f, 0.18f);
    public static readonly Color Danger = new(0.96f, 0.35f, 0.35f);

    public const string HexGold = "#E6B84D";
    public const string HexDim = "#998C73";
    public const string HexGood = "#5BD16E";
    public const string HexBad = "#F05A5A";
    public const string HexProgression = "#C9A0FF";
    public const string HexUseful = "#6D8BE8";
    public const string HexFiller = "#00EEEE";
    public const string HexTrap = "#FA8072";
    public const string HexLocation = "#00FF7F";
    public const string HexPlayer = "#FAFAD2";
    public const string HexMe = "#EE00EE";

    public static Texture2D White;
    public static GUIStyle Window, Header, Title, Subtitle, Section, Body, Dim, Small, Button, ButtonSmall,
        Tab, TabActive, Card, TextField, TextFieldFocused, FeedText, Pill, Credit, LogLine, Solid;

    private static bool _built;

    public static void Build()
    {
        if (_built && White != null) return;
        _built = true;

        White = MakeTexture(Color.white);
        var bg = MakeTexture(Bg);
        var panel = MakeTexture(Panel);
        var card = MakeTexture(CardColor);
        var btn = MakeTexture(new Color(0.18f, 0.14f, 0.08f));
        var btnHot = MakeTexture(new Color(0.26f, 0.20f, 0.10f));
        var btnActive = MakeTexture(new Color(0.40f, 0.30f, 0.12f));
        var field = MakeTexture(new Color(0.04f, 0.035f, 0.02f));
        var fieldFocus = MakeTexture(new Color(0.09f, 0.07f, 0.035f));

        // Flat white box, tinted with GUI.color.
        Solid = new GUIStyle { border = new RectOffset(0, 0, 0, 0), padding = new RectOffset(0, 0, 0, 0) };
        Solid.normal.background = White;

        Window = new GUIStyle(GUI.skin.box) { padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0) };
        Window.normal.background = bg;
        Window.onNormal.background = bg;

        Header = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6), margin = new RectOffset(0, 0, 0, 0) };
        Header.normal.background = panel;

        Card = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 6, 10), margin = new RectOffset(4, 4, 4, 6) };
        Card.normal.background = card;

        Title = Label(17, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
        Subtitle = Label(11, TextDim, FontStyle.Normal, TextAnchor.MiddleCenter);
        Section = Label(12, Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
        Section.padding = new RectOffset(2, 2, 6, 4);
        Body = Label(12, Text, FontStyle.Normal, TextAnchor.MiddleLeft);
        Body.wordWrap = true;
        Dim = Label(11, TextDim, FontStyle.Italic, TextAnchor.MiddleLeft);
        Dim.wordWrap = true;
        Small = Label(11, Text, FontStyle.Normal, TextAnchor.MiddleLeft);
        Small.wordWrap = true;
        LogLine = Label(12, Text, FontStyle.Normal, TextAnchor.UpperLeft);
        LogLine.wordWrap = true;
        LogLine.margin = new RectOffset(2, 2, 1, 1);
        Credit = Label(9, TextDim, FontStyle.Normal, TextAnchor.LowerLeft);

        Button = new GUIStyle(GUI.skin.button) { fontSize = 12, richText = true, padding = new RectOffset(8, 8, 5, 5) };
        Button.normal.background = btn;
        Button.hover.background = btnHot;
        Button.active.background = btnActive;
        Button.focused.background = btn;
        Button.normal.textColor = Text;
        Button.hover.textColor = Gold;
        Button.active.textColor = Gold;
        Button.focused.textColor = Text;

        ButtonSmall = new GUIStyle(Button) { fontSize = 11, padding = new RectOffset(6, 6, 3, 3) };

        Pill = new GUIStyle(Button) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, fixedHeight = 22f };

        Tab = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(14, 6, 7, 7), margin = new RectOffset(0, 0, 1, 1),
        };
        Tab.normal.background = null;
        Tab.hover.background = btn;
        Tab.active.background = btnHot;
        Tab.normal.textColor = TextDim;
        Tab.hover.textColor = Gold;
        Tab.active.textColor = Gold;
        TabActive = new GUIStyle(Tab);
        TabActive.normal.background = btn;
        TabActive.normal.textColor = Gold;

        TextField = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13, richText = true, alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 8, 6, 6), clipping = TextClipping.Clip,
        };
        TextField.normal.background = field;
        TextField.hover.background = fieldFocus;
        TextField.active.background = fieldFocus;
        TextField.normal.textColor = Text;
        TextField.hover.textColor = Text;
        TextField.active.textColor = Text;
        TextFieldFocused = new GUIStyle(TextField);
        TextFieldFocused.normal.background = fieldFocus;
        TextFieldFocused.normal.textColor = Gold;

        FeedText = Label(14, Text, FontStyle.Bold, TextAnchor.MiddleLeft);
        FeedText.wordWrap = true;
    }

    private static GUIStyle Label(int size, Color color, FontStyle fontStyle, TextAnchor anchor)
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fontStyle, alignment = anchor, richText = true };
        style.normal.textColor = color;
        return style;
    }

    private static Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    public static string Colored(string text, string hex) => $"<color={hex}>{text}</color>";
}
