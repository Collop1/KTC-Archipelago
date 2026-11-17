using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using APHint = Archipelago.MultiClient.Net.Models.Hint;
using Coatsink.Common;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace APKingdom2Crown;

// The in-game Archipelago client: connect, see items and checks, hint, chat, debug, settings.
// Toggled with F2 by default. Styled after Kingdom Enhanced's menu (which uses F1, so both can coexist).
public class ModMenu : MonoBehaviour
{
    public ModMenu(IntPtr ptr) : base(ptr) { }

    private enum Tab { Connect, Items, Checks, Hints, Chat, Debug, Settings }

    private static readonly string[] TabLabels = { "CONNECT", "ITEMS", "CHECKS", "HINTS", "CHAT", "DEBUG", "SETTINGS" };

    private static readonly KeyCode[] MenuKeys =
    {
        KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9,
        KeyCode.F10, KeyCode.F11, KeyCode.Insert, KeyCode.Home, KeyCode.BackQuote,
    };

    private const float HeaderHeight = 54f;
    private const float SidebarWidth = 126f;

    private bool _visible;
    private Tab _tab = Tab.Connect;
    private readonly Vector2[] _scroll = new Vector2[TabLabels.Length];
    private Vector2 _chatScroll;
    private int _chatSeenVersion = -1;
    private bool _chatAutoScroll = true;
    private bool _chatOnlyMine;
    private Rect _rect;
    private bool _showPassword;
    private string _toast = "";
    private float _toastUntil;

    // Window scale is only applied once the slider is let go; rescaling mid-drag moves the slider out from
    // under the mouse.
    private bool _draggingScale;
    private float _pendingScale;

    // Background alpha for everything drawn inside the window (GUI.Window runs its contents after OnGUI has
    // already restored GUI.backgroundColor, so it has to be set again in there).
    private static float _opacity = 1f;

    private readonly TextInput _host = new() { Placeholder = "archipelago.gg:38281" };
    private readonly TextInput _slot = new() { Placeholder = "your slot name" };
    private readonly TextInput _password = new() { Placeholder = "(none)", Masked = true };
    private readonly TextInput _chat = new() { Placeholder = "Chat, or !hint / !remaining / !release ...", MaxLength = 400 };
    private readonly TextInput _hint = new() { Placeholder = "Item or location name" };

    private static readonly Dictionary<string, float> _armed = new();
    private static List<string> _missingCache = new();
    private static int _missingCacheChecked = -1;
    private static float _missingCacheTime;
    private string _census = "Press Refresh to count what's on this island.";

    private void Start()
    {
        var s = ClientSettings.Current;
        _host.Text = s.Host;
        _slot.Text = s.Slot;
        _password.Text = s.Password;
        _rect = new Rect(s.WindowX, s.WindowY, s.WindowWidth, s.WindowHeight);
        _host.OnSubmit = _ => ConnectFromFields();
        _slot.OnSubmit = _ => ConnectFromFields();
        _password.OnSubmit = _ => ConnectFromFields();
        _chat.OnSubmit = SendChat;
        _hint.OnSubmit = text => RequestHint(text, location: false);

        Feed.Add($"<color={Styles.HexGold}>Archipelago ready.</color> Press <b>{(KeyCode)s.MenuKey}</b> to open the menu.");
        if (s.AutoConnect && s.Slot.Length > 0) AP.Connect(s.Host, s.Slot, s.Password);
    }

    private void Update()
    {
        TextInput.HandleKeyboard();

        if (!TextInput.AnyFocused && Input.GetKeyDown((KeyCode)ClientSettings.Current.MenuKey))
            SetVisible(!_visible);

        if (_draggingScale && !Input.GetMouseButton(0))
        {
            _draggingScale = false;
            ClientSettings.Current.WindowScale = _pendingScale;
            ClientSettings.Save();
        }

        _drawThisFrame = _visible && NeedsDraw();
    }

    // --- Drawing on demand ---
    // Drawing the menu every frame through IMGUI made a steady stream of game-side garbage, and this game runs
    // without incremental GC, so the collections froze it several times a second. Instead the menu is drawn into a
    // picture (see UiBlocker) whenever something could have changed: on any input event (see OnGUI), while a
    // control is held, while typing, and otherwise a few times a second to keep counters fresh.
    private const float IdleRedrawSeconds = 0.25f;
    private bool _drawThisFrame;   // from Update: mouse moved, typing, a key, a refresh due...
    private bool _drawPass;        // decided on each Layout pass, used by the event pass that follows it
    private bool _drawRequested;   // an input event was handled; this frame's picture has to show the result
    private bool _forceDraw;
    private float _nextDraw;
    private Vector3 _lastMouse;

    private bool NeedsDraw()
    {
        if (!UiBlocker.CanCapture) return true; // no picture to fall back on: draw every frame like any IMGUI menu
        var mouse = Input.mousePosition;
        bool moved = (mouse - _lastMouse).sqrMagnitude > 0.25f;
        _lastMouse = mouse;
        bool draw = _forceDraw || moved
            || Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)
            || Input.mouseScrollDelta.sqrMagnitude > 0f || Input.anyKeyDown || TextInput.AnyFocused
            || _draggingScale || Time.unscaledTime >= _nextDraw;
        _forceDraw = false;
        return draw;
    }

    // --- Moving the window by its header, resizing it by the grip ---
    // Both run on IMGUI's own mouse events and hold GUIUtility.hotControl while the button is down, which also keeps
    // the menu redrawing every frame until it's let go.
    private Vector2 _grab;
    private Vector2 _sizeAtGrab;

    private void HandleWindowDrag(int id, Rect header)
    {
        var e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0 && header.Contains(e.mousePosition):
                GUIUtility.hotControl = id;
                _grab = e.mousePosition; // window-local, so it stays put under the cursor
                e.Use();
                break;
            case EventType.MouseDrag when GUIUtility.hotControl == id:
                _rect.x += e.mousePosition.x - _grab.x;
                _rect.y += e.mousePosition.y - _grab.y;
                e.Use();
                break;
            case EventType.MouseUp when GUIUtility.hotControl == id:
                GUIUtility.hotControl = 0;
                SaveWindow();
                e.Use();
                break;
        }
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        _forceDraw = true;
        if (!visible)
        {
            TextInput.ClearFocus();
            UiBlocker.Hide();
            SaveWindow();
        }
        try
        {
            var cursor = SingletonMonoBehaviour<CursorSystem>.Inst;
            if (cursor != null) cursor.SetForceVisibleCursor(visible);
            else Cursor.visible = visible;
        }
        catch
        {
            Cursor.visible = visible;
        }
    }

    private void OnGUI()
    {
        Styles.Build();
        var e = Event.current;
        var type = e.type;

        if (_visible && type == EventType.MouseDown) TextInput.ClearFocus(); // clicking a box re-focuses it

        Feed.Draw();
        if (!_visible) return;

        // IMGUI calls OnGUI twice per event, a Layout pass and then the event itself, and GUILayout needs both or
        // neither. So the choice is made on the Layout pass, which still carries the event's details: always draw
        // for input (a click, drag, scroll or key), while a control is held down, and otherwise only when the
        // picture is due. Between redraws the last picture stays on screen.
        if (type == EventType.Layout)
            _drawPass = _drawThisFrame || _drawRequested || GUIUtility.hotControl != 0 || CarriesInput(e)
                || Time.unscaledTime >= _nextDraw || !UiBlocker.CanCapture;
        if (!_drawPass) return;
        if (type != EventType.Layout && type != EventType.Repaint) _drawRequested = true;

        var s = ClientSettings.Current;
        float scale = Mathf.Clamp(s.WindowScale, 0.5f, 2.5f);
        float maxW = Screen.width / scale, maxH = Screen.height / scale;
        _rect.width = Mathf.Clamp(_rect.width, 520f, Mathf.Max(520f, maxW));
        _rect.height = Mathf.Clamp(_rect.height, 380f, Mathf.Max(380f, maxH));
        _rect.x = Mathf.Clamp(_rect.x, 0f, Mathf.Max(0f, maxW - 80f));
        _rect.y = Mathf.Clamp(_rect.y, 0f, Mathf.Max(0f, maxH - 60f));
        _opacity = Mathf.Clamp(s.MenuOpacity, 0.3f, 1f);

        bool repaint = type == EventType.Repaint;
        RenderTexture previous = null;
        bool captured = repaint && UiBlocker.BeginCapture(out previous);
        var oldMatrix = GUI.matrix;
        var oldBg = GUI.backgroundColor;
        try
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            GUI.backgroundColor = new Color(1f, 1f, 1f, _opacity);
            DrawWindow();
        }
        finally
        {
            GUI.backgroundColor = oldBg;
            GUI.matrix = oldMatrix;
            if (captured) UiBlocker.EndCapture(previous);
        }

        if (repaint)
        {
            _drawRequested = false;
            _nextDraw = Time.unscaledTime + IdleRedrawSeconds;
            // Keep the game's own UI (e.g. the pause menu) from taking clicks meant for this window.
            UiBlocker.Cover(new Rect(_rect.x * scale, _rect.y * scale, _rect.width * scale, _rect.height * scale));
        }
    }

    // A real input event, not a repaint: on the Layout pass these details are still the upcoming event's.
    private static bool CarriesInput(Event e) =>
        e.clickCount > 0 || e.button != 0 || e.delta.sqrMagnitude > 0f || e.keyCode != KeyCode.None || e.character != '\0';

    private void DrawWindow()
    {
        GUILayout.BeginArea(_rect, Styles.Window);
        try
        {
            HandleWindowDrag(GUIUtility.GetControlID(FocusType.Passive), new Rect(0f, 0f, _rect.width, HeaderHeight));

            Gui.BeginVertical();

            Gui.BeginVertical(Styles.Header, Gui.Height(HeaderHeight));
            Gui.Label("Archipelago: Kingdom Two Crowns", Styles.Title);
            Gui.Label($"v{MyPluginInfo.PLUGIN_VERSION}   |   {StatusLine()}", Styles.Subtitle);
            GUILayout.EndVertical();

            Gui.BeginHorizontal();
            DrawSidebar();
            Gui.BeginVertical();
            if (_tab == Tab.Chat)
            {
                DrawChatTab();
            }
            else
            {
                _scroll[(int)_tab] = GUILayout.BeginScrollView(_scroll[(int)_tab], Gui.None);
                DrawTab();
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            Gui.BeginHorizontal();
            Gui.Label(Time.unscaledTime < _toastUntil ? _toast : "", Styles.Small);
            GUILayout.FlexibleSpace();
            var grip = GUILayoutUtility.GetRect(18f, 18f, Gui.None);
            GUI.Label(grip, Gui.Content("<color=#998C73>//</color>"), Styles.Small);
            HandleResize(grip);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[AP] Menu: {e}");
        }
        GUILayout.EndArea();
    }

    private static string StatusLine()
    {
        if (AP.Connected) return $"<color={Styles.HexGood}>Connected</color> as {AP.Escape(AP.SlotName)}   {AP.CheckedCount}/{AP.LocationCount} checks";
        if (AP.Connecting) return $"<color={Styles.HexGold}>Connecting...</color>";
        return $"<color={Styles.HexBad}>Not connected</color>";
    }

    private void DrawSidebar()
    {
        Gui.BeginVertical(Styles.Header, Gui.Width(SidebarWidth));
        GUILayout.Space(6f);
        for (int i = 0; i < TabLabels.Length; i++)
        {
            if (Gui.Button(TabLabels[i], _tab == (Tab)i ? Styles.TabActive : Styles.Tab, Gui.Height(30f)))
            {
                _tab = (Tab)i;
                TextInput.ClearFocus();
            }
        }
        GUILayout.FlexibleSpace();
        Gui.Label($"{(KeyCode)ClientSettings.Current.MenuKey} to close", Styles.Credit);
        GUILayout.Space(4f);
        GUILayout.EndVertical();
    }

    private void HandleResize(Rect grip)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        var e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0 && grip.Contains(e.mousePosition):
                GUIUtility.hotControl = id;
                _grab = e.mousePosition; // the window's top-left doesn't move while resizing
                _sizeAtGrab = new Vector2(_rect.width, _rect.height);
                e.Use();
                break;
            case EventType.MouseDrag when GUIUtility.hotControl == id:
                _rect.width = Mathf.Max(520f, _sizeAtGrab.x + e.mousePosition.x - _grab.x);
                _rect.height = Mathf.Max(380f, _sizeAtGrab.y + e.mousePosition.y - _grab.y);
                e.Use();
                break;
            case EventType.MouseUp when GUIUtility.hotControl == id:
                GUIUtility.hotControl = 0;
                SaveWindow();
                e.Use();
                break;
        }
    }

    private void SaveWindow()
    {
        var s = ClientSettings.Current;
        s.WindowX = _rect.x;
        s.WindowY = _rect.y;
        s.WindowWidth = _rect.width;
        s.WindowHeight = _rect.height;
        ClientSettings.Save();
    }

    private void DrawTab()
    {
        switch (_tab)
        {
            case Tab.Connect: DrawConnectTab(); break;
            case Tab.Items: DrawItemsTab(); break;
            case Tab.Checks: DrawChecksTab(); break;
            case Tab.Hints: DrawHintsTab(); break;
            case Tab.Debug: DrawDebugTab(); break;
            case Tab.Settings: DrawSettingsTab(); break;
        }
    }

    // ---------------------------------------------------------------- widgets

    // Sort key that compares the numbers inside names as numbers: digits are zero-padded to a fixed width.
    private static string NaturalKey(string name) => Digits.Replace(name, PadNumber);
    private static readonly System.Text.RegularExpressions.Regex Digits = new(@"\d+");
    private static string PadNumber(System.Text.RegularExpressions.Match match) => match.Value.PadLeft(8, '0');

    // Long lists are shown a page at a time: drawing hundreds of rows every frame is what makes IMGUI slow.
    private const int PageSize = 25;
    private const int ChatLines = 150;
    private readonly Dictionary<string, int> _pages = new();

    // Draws "< Prev  Page 2 / 7  Next >" when a list needs more than one page; returns the first index to show.
    private int Pager(string key, int count)
    {
        int pages = Math.Max(1, (count + PageSize - 1) / PageSize);
        int page = Math.Min(_pages.TryGetValue(key, out var p) ? p : 0, pages - 1);
        if (pages > 1)
        {
            Gui.BeginHorizontal();
            if (Gui.Button("< Prev", Styles.ButtonSmall, Gui.Width(70f)) && page > 0) page--;
            Gui.Label($"  Page {page + 1} / {pages}", Styles.Small, Gui.Width(110f));
            if (Gui.Button("Next >", Styles.ButtonSmall, Gui.Width(70f)) && page < pages - 1) page++;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        _pages[key] = page;
        return page * PageSize;
    }

    // Up to four equal buttons side by side; one line of text each, so nothing gets cut off.
    private void ButtonRow((string Label, Action OnClick)[] buttons)
    {
        // Same width in every row so the columns line up (the card's inner width, split four ways).
        float width = Mathf.Floor(Mathf.Max(60f, (_rect.width - SidebarWidth - 90f) / 4f));
        Gui.BeginHorizontal();
        foreach (var (label, onClick) in buttons)
            if (Gui.Button(label, Styles.ButtonSmall, Gui.Size(width, 24f))) onClick();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private static readonly (string, Action)[][] SimulateRows =
    {
        new (string, Action)[]
        {
            ("+1 Night", () => CheckTracker.AddMilestone(GameData.Milestone.Nights)),
            ("+1 Charger", () => CheckTracker.OnEnemyKilled(EnemyType.Crusher)),
            ("+5 Trees", () => CheckTracker.AddMilestone(GameData.Milestone.Trees, 5)),
            ("DeathLink", ItemHandler.DebugDeathLink),
        },
        new (string, Action)[]
        {
            ("+5 Greedlings", () => { for (int i = 0; i < 5; i++) CheckTracker.OnEnemyKilled(EnemyType.TrollWeak); }),
            ("+1 Floater", () => CheckTracker.OnEnemyKilled(EnemyType.Squid)),
            ("+1 Breeder", () => CheckTracker.OnEnemyKilled(EnemyType.Boss)),
            ("+1 Crown Stealer", () => CheckTracker.OnEnemyKilled(EnemyType.Stealer)),
        },
        new (string, Action)[]
        {
            ("+1 Wall", () => CheckTracker.AddMilestone(GameData.Milestone.Walls)),
            ("+1 Tower", () => CheckTracker.AddMilestone(GameData.Milestone.Towers)),
            ("+1 Farm", () => CheckTracker.AddMilestone(GameData.Milestone.Farms)),
            ("+1 Citizen House", () => CheckTracker.AddMilestone(GameData.Milestone.CitizenHouses)),
        },
        new (string, Action)[]
        {
            ("+5 Villagers", () => CheckTracker.AddMilestone(GameData.Milestone.Villagers, 5)),
            ("+1 Archer", () => CheckTracker.AddMilestone(GameData.Milestone.Archers)),
            ("+1 Builder", () => CheckTracker.AddMilestone(GameData.Milestone.Builders)),
            ("+1 Farmer", () => CheckTracker.AddMilestone(GameData.Milestone.Farmers)),
        },
        new (string, Action)[]
        {
            ("+1 Knight", () => CheckTracker.AddMilestone(GameData.Milestone.Knights)),
            ("+1 Deer", () => CheckTracker.OnAnimalKilled(GameData.Milestone.Deer)),
            ("+1 Rabbit", () => CheckTracker.OnAnimalKilled(GameData.Milestone.Rabbits)),
            ("+1 Boar", () => CheckTracker.OnAnimalKilled(GameData.Milestone.Boars)),
        },
        new (string, Action)[]
        {
            ("+5 Greed bribes", () => CheckTracker.AddMilestone(GameData.Milestone.Bribes, 5)),
            ("+25 Coins spent", () => CheckTracker.AddMilestone(GameData.Milestone.CoinsSpent, 25)),
            ("+10 Grass", () => { for (int i = 0; i < 10; i++) CheckTracker.OnGrassEaten(); }),
            ("+5 Sunk coins", () => { for (int i = 0; i < 5; i++) CheckTracker.OnCoinSunk(); }),
        },
        new (string, Action)[]
        {
            ("+1 Greed Knight", () => CheckTracker.OnEnemyKilled(EnemyType.Knight)),
            ("Quarry built", () => CheckTracker.OnTechBuilding(TechnologyAge.Stone)),
            ("Mine built", () => CheckTracker.OnTechBuilding(TechnologyAge.Iron)),
        },
    };

    private static void BeginCard(string title)
    {
        Gui.BeginVertical(Styles.Card);
        Gui.Label(title, Styles.Section);
    }

    private static void EndCard() => GUILayout.EndVertical();

    private static void Row(string label, string value)
    {
        Gui.BeginHorizontal();
        Gui.Label(label, Styles.Body, Gui.Width(190f));
        Gui.Label(value, Styles.Body);
        GUILayout.EndHorizontal();
    }

    private static bool Pill(bool on, string onText = "ON", string offText = "OFF")
    {
        var old = GUI.backgroundColor;
        var tint = on ? Styles.On : Styles.Off;
        GUI.backgroundColor = new Color(tint.r, tint.g, tint.b, _opacity);
        bool clicked = Gui.Button(on ? onText : offText, Styles.Pill, Gui.Width(64f));
        GUI.backgroundColor = old;
        return clicked;
    }

    private static bool PillRow(string label, bool on)
    {
        Gui.BeginHorizontal();
        Gui.Label(label, Styles.Body, Gui.Width(280f));
        bool clicked = Pill(on);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        return clicked;
    }

    private static float SliderRow(string label, float value, float min, float max, string format)
    {
        Gui.BeginHorizontal();
        Gui.Label(label, Styles.Body, Gui.Width(170f));
        Gui.Label(value.ToString(format), Styles.Body, Gui.Width(44f));
        float v = GUILayout.HorizontalSlider(value, min, max, Gui.ExpandWidth);
        GUILayout.EndHorizontal();
        return v;
    }

    // A button that needs a second click within a few seconds, for anything destructive.
    private static bool ConfirmButton(string label, string key, GUIStyle style, Il2CppReferenceArray<GUILayoutOption> options)
    {
        bool armed = _armed.TryGetValue(key, out var until) && Time.unscaledTime < until;
        if (!Gui.Button(armed ? $"<color={Styles.HexBad}>Confirm?</color>" : label, style, options)) return false;
        if (armed)
        {
            _armed.Remove(key);
            return true;
        }
        _armed[key] = Time.unscaledTime + 4f;
        return false;
    }

    private void Toast(string message)
    {
        _toast = message;
        _toastUntil = Time.unscaledTime + 4f;
    }

    // Filled/hollow circles: the default IMGUI font has these, unlike tick and cross marks.
    private static string Check(bool yes) => yes ? $"<color={Styles.HexGood}>●</color>" : $"<color={Styles.HexBad}>○</color>";

    private static string ItemColor(ItemFlags flags) =>
        (flags & ItemFlags.Advancement) != 0 ? Styles.HexProgression
        : (flags & ItemFlags.Trap) != 0 ? Styles.HexTrap
        : (flags & ItemFlags.NeverExclude) != 0 ? Styles.HexUseful
        : Styles.HexFiller;

    // ---------------------------------------------------------------- CONNECT

    private void ConnectFromFields()
    {
        TextInput.ClearFocus();
        var s = ClientSettings.Current;
        s.Host = _host.Text.Trim();
        s.Slot = _slot.Text.Trim();
        s.Password = _password.Text;
        ClientSettings.Save();
        AP.Connect(s.Host, s.Slot, s.Password);
    }

    private void DrawConnectTab()
    {
        var s = ClientSettings.Current;

        BeginCard("ARCHIPELAGO SERVER");
        Gui.Label("Server address (host:port)", Styles.Dim);
        _host.Draw(Styles.TextField, Gui.Height(28f));
        Gui.Label("Slot name", Styles.Dim);
        _slot.Draw(Styles.TextField, Gui.Height(28f));
        Gui.Label("Password (leave empty if the room has none)", Styles.Dim);
        Gui.BeginHorizontal();
        _password.Masked = !_showPassword;
        _password.Draw(Styles.TextField, Gui.HeightExpandWidth(28f));
        if (Gui.Button(_showPassword ? "Hide" : "Show", Styles.ButtonSmall, Gui.Size(60f, 28f)))
            _showPassword = !_showPassword;
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        Gui.BeginHorizontal();
        if (AP.Connecting)
        {
            Gui.Button("Connecting...", Styles.Button, Gui.Size(180f, 32f));
        }
        else if (AP.Connected)
        {
            if (Gui.Button("Disconnect", Styles.Button, Gui.Size(180f, 32f))) AP.Disconnect();
        }
        else if (Gui.Button($"<b>Connect</b>", Styles.Button, Gui.Size(180f, 32f)))
        {
            ConnectFromFields();
        }
        GUILayout.Space(10f);
        Gui.Label(AP.StatusIsError ? $"<color={Styles.HexBad}>{AP.Escape(AP.Status)}</color>" : AP.Escape(AP.Status), Styles.Body);
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);

        if (PillRow("Remember password", s.RememberPassword))
        {
            s.RememberPassword = !s.RememberPassword;
            ClientSettings.Save();
        }
        if (PillRow("Connect automatically when the game starts", s.AutoConnect))
        {
            s.AutoConnect = !s.AutoConnect;
            ClientSettings.Save();
        }
        EndCard();

        if (AP.Connected)
        {
            BeginCard("SESSION");
            Row("Slot", $"{AP.Escape(AP.SlotName)} (slot {AP.SlotId}, team {AP.Team})");
            Row("Server", AP.Escape(AP.HostConnectedTo));
            Row("Checks", $"{AP.CheckedCount} / {AP.LocationCount}");
            Row("Items received", AP.Received.Count.ToString());
            var campaign = AP.Slot.Campaign;
            Row("Campaign", campaign.Display);
            if (campaign.CampaignWide)
                Row("Goal", "Defeat the World Serpent on Mount Olympus");
            else
                Row("Goal", $"Liberate {AP.Slot.GoalIslands} island(s), {CheckTracker.LiberatedIslands()} done");
            if (!campaign.CampaignWide)
                Row("Island unlocks", AP.Slot.UsesIslandUnlocks ? "On (islands are items)" : "Off (islands open in order)");
            Gui.BeginHorizontal();
            Gui.Label("DeathLink", Styles.Body, Gui.Width(190f));
            if (Pill(AP.DeathLinkOn)) AP.SetDeathLink(!AP.DeathLinkOn);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EndCard();
        }
        DrawCampaignWarning();
    }

    private static void DrawCampaignWarning()
    {
        if (GameAccess.CampaignMatches) return;
        BeginCard("WRONG CAMPAIGN");
        Gui.Label($"<color={Styles.HexBad}>This seed is for {AP.Slot.Campaign.Display}, but the game has a different campaign loaded.</color> " +
                        "Checks, items and locks are paused until you load or start a " + AP.Slot.Campaign.Display + " campaign.", Styles.Body);
        EndCard();
    }

    // ---------------------------------------------------------------- ITEMS

    private void DrawItemsTab()
    {
        BeginCard("UNLOCKS");
        int castle = ItemHandler.Count(GameData.ProgressiveCastle);
        Row("Castle upgrades", $"{castle} / {GameData.MaxCastleUpgrades}   <color={Styles.HexDim}>(castle can reach level {Math.Min(7, castle + 1)})</color>");
        Row("Technology", $"{Check(ItemHandler.Has(GameData.StoneTech))} Stone   {Check(ItemHandler.Has(GameData.IronTech))} Iron");
        var campaign = AP.Slot.Campaign;
        if (!campaign.CampaignWide)
        {
            var islands = string.Join("   ", Enumerable.Range(1, AP.Slot.IslandsInPlay).Select(i => $"{Check(Gating.IsIslandUnlocked(i))} {i}"));
            string inSeed = AP.Slot.IslandsLimited ? $"   <color={Styles.HexDim}>(this seed uses islands 1-{AP.Slot.IslandsInPlay})</color>" : "";
            Row("Islands", (AP.Slot.UsesIslandUnlocks ? islands : "Open in order") + inSeed);
        }
        if (AP.Slot.ProgressiveSacks > 0)
            Row("Coin sacks", $"{Gating.SacksReceived} / {AP.Slot.ProgressiveSacks}   <color={Styles.HexDim}>(your sack holds {Gating.PurseScale:0.##}x the usual)</color>");
        foreach (var group in GameData.ItemGroupsFor(AP.Slot).Where(g => g.Title is "Bombs" or "Statues" or "Hermits" or "Mounts" or "Powers" || g.Title == "Sack" && AP.Slot.ProgressiveSacks == 0))
        {
            Gui.Label(group.Title, Styles.Dim);
            for (int i = 0; i < group.Names.Length; i += 3)
            {
                Gui.BeginHorizontal();
                foreach (var name in group.Names.Skip(i).Take(3))
                    Gui.Label($"{Check(ItemHandler.Has(name))} {name}", Styles.Small, Gui.Width(170f));
                GUILayout.EndHorizontal();
            }
        }
        if (AP.State.StolenGems.Count > 0)
            Row("Stolen gems", $"<color={Styles.HexTrap}>{StolenGems.Describe()}</color>");
        if (ItemHandler.DebugGrants.Count > 0)
            Gui.Label($"<color={Styles.HexTrap}>Includes {ItemHandler.DebugGrants.Values.Sum()} debug grant(s) from the Debug tab.</color>", Styles.Small);
        EndCard();

        BeginCard($"RECEIVED ITEMS ({AP.Received.Count})");
        if (AP.Received.Count == 0) Gui.Label("Nothing yet.", Styles.Dim);
        int firstItem = Pager("received", AP.Received.Count);
        for (int j = firstItem; j < Math.Min(firstItem + PageSize, AP.Received.Count); j++)
        {
            var item = AP.Received[AP.Received.Count - 1 - j]; // newest first
            var from = item.Sender == AP.SlotName ? "you" : AP.Escape(item.Sender);
            var where = string.IsNullOrEmpty(item.Location) ? "" : $" <color={Styles.HexDim}>({AP.Escape(item.Location)})</color>";
            Gui.Label($"<color={ItemColor(item.Flags)}>{AP.Escape(item.Name)}</color>  from <color={Styles.HexPlayer}>{from}</color>{where}", Styles.Small);
        }
        EndCard();
    }

    // ---------------------------------------------------------------- CHECKS

    private static List<string> MissingLocations()
    {
        if (!AP.Connected) return new List<string>();
        if (_missingCacheChecked != AP.CheckedCount || Time.unscaledTime - _missingCacheTime > 3f)
        {
            _missingCache = AP.MissingLocationNames().OrderBy(NaturalKey, StringComparer.Ordinal).ToList();
            _missingCacheChecked = AP.CheckedCount;
            _missingCacheTime = Time.unscaledTime;
        }
        return _missingCache;
    }

    private void DrawChecksTab()
    {
        int island = GameAccess.CurrentIsland;

        DrawCampaignWarning();
        BeginCard("PROGRESS");
        Row("Checks done", AP.Connected ? $"{AP.CheckedCount} / {AP.LocationCount}" : "connect to see");
        Row("Current island", island > 0 ? island.ToString() : "-");
        if (AP.State.PendingLocations.Count > 0)
            Row("Waiting to send", $"<color={Styles.HexGold}>{AP.State.PendingLocations.Count} (will send on reconnect)</color>");
        EndCard();

        BeginCard(island > 0 && !AP.Slot.Campaign.CampaignWide ? $"MILESTONES (ISLAND {island} AND CAMPAIGN-WIDE)" : "MILESTONES");
        foreach (var milestone in AP.Slot.Milestones.Keys)
        {
            var (interval, count) = AP.Slot.Milestones[milestone];
            if (count == 0) continue;
            bool perIsland = AP.Slot.IsPerIsland(milestone);
            if (perIsland && island <= 0) continue;
            int value = CheckTracker.MilestoneCount(milestone);
            var next = CheckTracker.NextMilestone(milestone);
            var scope = perIsland ? $"on island {island}" : "campaign";
            var nextText = next.HasValue
                ? $"next check at {next.Value}  <color={Styles.HexDim}>(every {interval}, {count} checks, {scope})</color>"
                : $"<color={Styles.HexGood}>all {count} done</color>  <color={Styles.HexDim}>({scope})</color>";
            Row(GameData.Milestones[milestone].Label, $"<b>{value}</b>   {nextText}");
        }
        OneOffRow("Grass eaten", CheckTracker.GrassCounter, GameData.GrassTarget, GameData.GrassLocation);
        EndCard();

        var campaign = AP.Slot.Campaign;
        if (campaign.CampaignWide)
        {
            BeginCard($"{campaign.Display.ToUpperInvariant()} (WHOLE CAMPAIGN)");
            foreach (GameData.IslandThing thing in Enum.GetValues(typeof(GameData.IslandThing)))
            {
                int expected = GameData.ExpectedCount(campaign, 0, thing);
                if (expected == 0) continue;
                int done = Enumerable.Range(1, expected).Count(k => AP.IsChecked(GameData.IslandThingLocation(campaign, 0, thing, k)));
                Row(GameData.IslandThingLabel(thing), $"{done} / {expected} checked");
            }
            foreach (var quest in GameData.OlympusQuests.Values)
                Row(quest, Check(AP.IsChecked(quest)));
            int offset = AP.State.WaveOffset(island);
            if (offset > 0) Row("Waves Gone By", $"<color={Styles.HexTrap}>+{offset} days on this island</color>");
            EndCard();
        }
        else if (island > 0)
        {
            BeginCard($"ISLAND {island}");
            foreach (GameData.IslandThing thing in Enum.GetValues(typeof(GameData.IslandThing)))
            {
                int expected = GameData.ExpectedCount(campaign, island, thing);
                if (expected == 0) continue;
                AP.State.CountedPositions.TryGetValue($"{thing}@{island}", out var seen);
                int done = Enumerable.Range(1, expected).Count(k => AP.IsChecked(GameData.IslandThingLocation(campaign, island, thing, k)));
                Row(GameData.IslandThingLabel(thing), $"{done} / {expected} checked  <color={Styles.HexDim}>({seen?.Count ?? 0} found here)</color>");
            }
            if (campaign.Islands.TryGetValue(island, out var content) && content.Puzzle != null)
                Row($"{content.Puzzle}'s Puzzle", Check(AP.IsChecked(GameData.PuzzleLocation(island, content.Puzzle))));
            Row("Build the Boat", Check(AP.IsChecked(GameData.BoatLocation(island))));
            if (island > 1) Row("Arrive", Check(AP.IsChecked(GameData.ArriveLocation(island))));
            Row("Liberate", Check(AP.IsChecked(GameData.LiberateLocation(island))));
            int offset = AP.State.WaveOffset(island);
            if (offset > 0) Row("Waves Gone By", $"<color={Styles.HexTrap}>+{offset} days</color>");
            EndCard();
        }

        var missing = MissingLocations();
        BeginCard($"MISSING CHECKS ({missing.Count})");
        if (!AP.Connected) Gui.Label("Connect to see which checks are left.", Styles.Dim);
        else Gui.Label($"Hint points: {AP.HintPoints}. Hinting a location reveals what's there.", Styles.Dim);
        int firstMissing = Pager("missing", missing.Count);
        for (int i = firstMissing; i < Math.Min(firstMissing + PageSize, missing.Count); i++)
        {
            var name = missing[i];
            Gui.BeginHorizontal();
            Gui.Label(AP.Escape(name), Styles.Small, Gui.ExpandWidth);
            if (Gui.Button("Hint", Styles.ButtonSmall, Gui.Width(50f))) RequestHint(name, location: true);
            GUILayout.EndHorizontal();
        }
        EndCard();
    }

    private static void OneOffRow(string label, string counter, int target, string location)
    {
        if (AP.Connected && !AP.IsLocationInSeed(location)) return;
        int value = AP.State.Counter(counter);
        Row(label, AP.IsChecked(location) ? $"<b>{value}</b>   <color={Styles.HexGood}>done</color>" : $"<b>{value}</b> / {target}");
    }

    // ---------------------------------------------------------------- HINTS

    private void RequestHint(string text, bool location)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return;
        if (!AP.Connected)
        {
            Toast("Connect first.");
            return;
        }
        AP.Say(location ? $"!hint_location {text}" : $"!hint {text}");
        Toast($"Asked the server for a hint: {text}");
    }

    private void DrawHintsTab()
    {
        BeginCard("REQUEST A HINT");
        if (!AP.Connected) Gui.Label("Connect to request hints.", Styles.Dim);
        else
        {
            int cost = AP.HintCost;
            Gui.Label($"Hint points: <b>{AP.HintPoints}</b>{(cost > 0 ? $"   (a hint costs {cost})" : "")}", Styles.Body);
        }
        Gui.BeginHorizontal();
        _hint.Draw(Styles.TextField, Gui.HeightExpandWidth(28f));
        if (Gui.Button("Hint item", Styles.ButtonSmall, Gui.Size(80f, 28f))) RequestHint(_hint.Text, false);
        if (Gui.Button("Hint location", Styles.ButtonSmall, Gui.Size(100f, 28f))) RequestHint(_hint.Text, true);
        GUILayout.EndHorizontal();

        var wanted = new List<string>();
        if (ItemHandler.Count(GameData.ProgressiveCastle) < GameData.MaxCastleUpgrades) wanted.Add(GameData.ProgressiveCastle);
        if (!ItemHandler.Has(GameData.StoneTech)) wanted.Add(GameData.StoneTech);
        if (!ItemHandler.Has(GameData.IronTech)) wanted.Add(GameData.IronTech);
        if (AP.Slot.UsesIslandUnlocks)
            for (int i = 2; i <= AP.Slot.IslandsInPlay; i++)
                if (!ItemHandler.Has(GameData.IslandUnlock(i))) wanted.Add(GameData.IslandUnlock(i));
        if (wanted.Count > 0)
        {
            Gui.Label("Quick hints for key items you don't have yet:", Styles.Dim);
            for (int i = 0; i < wanted.Count; i += 3)
            {
                Gui.BeginHorizontal();
                foreach (var name in wanted.Skip(i).Take(3))
                    if (Gui.Button(name, Styles.ButtonSmall, Gui.Width(170f))) RequestHint(name, false);
                GUILayout.EndHorizontal();
            }
        }
        if (!string.IsNullOrEmpty(AP.LastServerReply))
        {
            GUILayout.Space(4f);
            Gui.Label("Server: " + AP.LastServerReply, Styles.Small);
        }
        EndCard();

        var hints = AP.Hints.OrderBy(h => h.Found).ThenBy(h => h.ReceivingPlayer != AP.SlotId).ToList();
        BeginCard($"HINTS ({hints.Count})");
        if (hints.Count == 0) Gui.Label("No hints yet.", Styles.Dim);
        int firstHint = Pager("hints", hints.Count);
        foreach (var hint in hints.Skip(firstHint).Take(PageSize))
        {
            string receiver = hint.ReceivingPlayer == AP.SlotId ? $"<color={Styles.HexMe}>Your</color>" : $"<color={Styles.HexPlayer}>{AP.Escape(AP.PlayerName(hint.ReceivingPlayer))}'s</color>";
            string finder = hint.FindingPlayer == AP.SlotId ? $"<color={Styles.HexMe}>your</color>" : $"<color={Styles.HexPlayer}>{AP.Escape(AP.PlayerName(hint.FindingPlayer))}'s</color>";
            string item = $"<color={ItemColor(hint.ItemFlags)}>{AP.Escape(AP.ItemName(hint.ItemId, hint.ReceivingPlayer))}</color>";
            string loc = $"<color={Styles.HexLocation}>{AP.Escape(AP.LocationName(hint.LocationId, hint.FindingPlayer))}</color>";
            string entrance = string.IsNullOrEmpty(hint.Entrance) ? "" : $" ({AP.Escape(hint.Entrance)})";
            string state = hint.Found ? $"<color={Styles.HexGood}>found</color>" : $"<color={Styles.HexGold}>not found</color>";
            Gui.Label($"{receiver} {item} is at {loc} in {finder} world{entrance}  [{state}]", Styles.Small);
        }
        EndCard();
    }

    // ---------------------------------------------------------------- CHAT

    private void SendChat(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!AP.Connected)
        {
            Toast("Connect first.");
            return;
        }
        AP.Say(text);
        _chat.Text = "";
        _chatAutoScroll = true;
    }

    private void DrawChatTab()
    {
        Gui.BeginHorizontal();
        Gui.Label("Everything the server says: items for everyone, hints, chat and command replies.", Styles.Dim);
        GUILayout.FlexibleSpace();
        if (Gui.Button(_chatOnlyMine ? "Showing: mine" : "Showing: all", Styles.ButtonSmall, Gui.Width(110f))) _chatOnlyMine = !_chatOnlyMine;
        if (Gui.Button(_chatAutoScroll ? "Auto-scroll on" : "Auto-scroll off", Styles.ButtonSmall, Gui.Width(110f))) _chatAutoScroll = !_chatAutoScroll;
        GUILayout.EndHorizontal();

        if (_chatAutoScroll && _chatSeenVersion != AP.LogVersion)
        {
            _chatScroll.y = float.MaxValue;
            _chatSeenVersion = AP.LogVersion;
        }

        _chatScroll = GUILayout.BeginScrollView(_chatScroll, Styles.Card, Gui.ExpandHeight);
        int shown = 0;
        for (int i = Math.Max(0, AP.Log.Count - ChatLines); i < AP.Log.Count; i++)
        {
            var line = AP.Log[i];
            if (_chatOnlyMine && !line.RelatedToMe) continue;
            Gui.Label(line.Text, Styles.LogLine);
            shown++;
        }
        if (shown == 0) Gui.Label(AP.Connected ? "Nothing yet." : "Connect to see the server log.", Styles.Dim);
        GUILayout.EndScrollView();

        Gui.BeginHorizontal();
        _chat.Draw(Styles.TextField, Gui.HeightExpandWidth(28f));
        if (Gui.Button("Send", Styles.Button, Gui.Size(70f, 28f))) SendChat(_chat.Text);
        GUILayout.EndHorizontal();

        Gui.BeginHorizontal();
        foreach (var cmd in new[] { "!remaining", "!missing", "!hint", "!collect", "!release" })
        {
            if (Gui.Button(cmd, Styles.ButtonSmall, Gui.Width(84f)))
            {
                if (cmd is "!collect" or "!release") _chat.Text = cmd;
                else SendChat(cmd);
            }
        }
        GUILayout.EndHorizontal();
    }

    // ---------------------------------------------------------------- DEBUG

    private void DrawDebugTab()
    {
        BeginCard("DEBUG");
        Gui.Label("Give yourself any item or trap to see how it behaves. Debug grants only exist locally and are lost when the game closes. Sending a check below is real and goes to the server.", Styles.Dim);
        if (PillRow("Apply AP rules while not connected", ItemHandler.SimulateGating))
            ItemHandler.SimulateGating = !ItemHandler.SimulateGating;
        Gui.BeginHorizontal();
        if (Gui.Button("Clear debug grants", Styles.ButtonSmall, Gui.Width(150f))) ItemHandler.ClearDebugGrants();
        Gui.Label($"{ItemHandler.DebugGrants.Values.Sum()} granted, {ItemHandler.PendingEffects} effect(s) queued, {Gating.BlockedCount} payable(s) blocked", Styles.Small);
        GUILayout.EndHorizontal();
        if (!GameAccess.IsPlaying) Gui.Label($"<color={Styles.HexGold}>Effects wait until you're in a game.</color>", Styles.Small);
        EndCard();

        BeginCard("GIVE ITEM");
        foreach (var group in GameData.ItemGroupsFor(AP.Slot))
        {
            Gui.Label(group.Title, Styles.Dim);
            for (int i = 0; i < group.Names.Length; i += 3)
            {
                Gui.BeginHorizontal();
                foreach (var name in group.Names.Skip(i).Take(3))
                {
                    var label = name;
                    if (group.Kind == GameData.ItemKind.Unlock)
                    {
                        int n = ItemHandler.Count(name);
                        if (n > 0) label = $"{name} <color={Styles.HexGood}>({n})</color>";
                    }
                    if (Gui.Button(label, Styles.ButtonSmall, Gui.Width(170f))) ItemHandler.DebugGive(name);
                }
                GUILayout.EndHorizontal();
            }
        }
        EndCard();

        BeginCard("SIMULATE EVENTS");
        Gui.Label("Bumps the counters the same way the game would (sends checks if a milestone is reached).", Styles.Dim);
        foreach (var row in SimulateRows) ButtonRow(row);
        EndCard();

        BeginCard("GAME STATE");
        DrawGameState();
        Gui.BeginHorizontal();
        if (Gui.Button("Skip to night", Styles.ButtonSmall)) SkipTime(toNight: true);
        if (Gui.Button("Skip to day", Styles.ButtonSmall)) SkipTime(toNight: false);
        if (Gui.Button("Refresh island census", Styles.ButtonSmall)) _census = Census();
        if (Gui.Button("Dump campaign data", Styles.ButtonSmall)) Toast("Written to " + CampaignDump.Run());
        GUILayout.EndHorizontal();
        Gui.Label(_census, Styles.Small);
        EndCard();

        BeginCard("SEND A CHECK");
        if (!AP.Connected)
        {
            Gui.Label("Connect to send checks by hand.", Styles.Dim);
        }
        else
        {
            Gui.Label("For when a check didn't fire. Click Send twice to confirm.", Styles.Dim);
            var missing = MissingLocations();
            int firstMissing = Pager("send", missing.Count);
            foreach (var name in missing.Skip(firstMissing).Take(PageSize))
            {
                Gui.BeginHorizontal();
                Gui.Label(AP.Escape(name), Styles.Small, Gui.ExpandWidth);
                if (ConfirmButton("Send", "send:" + name, Styles.ButtonSmall, Gui.Width(70f))) AP.SendLocation(name);
                GUILayout.EndHorizontal();
            }
        }
        EndCard();

        BeginCard("RESET");
        Gui.BeginHorizontal();
        if (ConfirmButton("Reset milestone counters", "reset-counters", Styles.ButtonSmall, Gui.Width(200f)))
        {
            AP.State.Counters.Clear();
            AP.State.Dirty = true;
            Toast("Milestone counters reset.");
        }
        if (ConfirmButton("Forget counted chests/portals", "reset-positions", Styles.ButtonSmall, Gui.Width(220f)))
        {
            AP.State.CountedPositions.Clear();
            AP.State.Dirty = true;
            Toast("Counted positions cleared.");
        }
        GUILayout.EndHorizontal();
        EndCard();
    }

    private static void DrawGameState()
    {
        try
        {
            var game = GameAccess.Game;
            var director = GameAccess.Director;
            var kingdom = GameAccess.Kingdom;
            Row("Game state", game != null ? game.state.ToString() : "-");
            Row("Land / island", $"land {GameAccess.CurrentLand} (LAND_ONE {GameAccess.LandOne}) = island {GameAccess.CurrentIsland}");
            if (director != null)
                Row("Time", $"day {director.CurrentIslandDays}, {(director.IsDaytime ? "day" : "night")} {director.currentTime % 24f:F1}h (day {director.dayStart:F0}-{director.dayEnd:F0}h)");
            if (kingdom != null)
            {
                Row("Castle", $"level {GameAccess.CastleLevel}, upgrade {(Gating.CastleUpgradeAllowed(GameAccess.CastleLevel) ? "allowed" : "blocked")}");
                Row("Tech", $"stone {kingdom.StoneBuildingUnlocked}, iron {kingdom.IronBuildingUnlocked}");
            }
            var campaign = CampaignSaveData.current;
            if (campaign != null) Row("Campaign", $"furthest land {campaign.furthestUnlockedLand}, current {campaign.currentLand}");
            Row("Campaign", $"seed: {AP.Slot.Campaign.Display} (biome {AP.Slot.Campaign.BiomeIndex}), loaded biome {GameAccess.BiomeIndex}, level '{GameAccess.CurrentLevelConfigName}'");
            if (!AP.Slot.Campaign.CampaignWide)
            {
                var secured = string.Join(" ", Enumerable.Range(1, AP.Slot.Campaign.IslandCount).Select(i => $"{i}:{(GameAccess.IsIslandSecured(i) ? "yes" : "no")}"));
                Row("Islands liberated", secured);
            }
            Row("Wave offset here", $"+{AP.State.WaveOffset(GameAccess.CurrentIsland)} days");
            Row("Gating", $"{(Gating.Active ? "active" : "off")}, islands {(Gating.IslandGatingActive ? "gated" : "open")}");
            if (!AP.Slot.Campaign.CampaignWide && AP.Slot.BombUnlocks)
                Row("Bombs here", Gating.BombsAllowedHere() ? "allowed" : "blocked (need this island's bombs)");
            var wallet = GameAccess.Player?.wallet;
            if (wallet != null)
                Row("Purse", $"{wallet.Coins} coins, {wallet.Gems} gems; sack holds {Gating.PurseScale:0.##}x (coins drawn at {Gating.CoinSize:0.##}x size)");
            if (AP.Slot.Campaign.CampaignWide)
                Row("Hermes's sack", Gating.HermesSackAllowed() ? "can be bought" : "blocked");
        }
        catch (Exception e)
        {
            Gui.Label($"(state unavailable: {AP.Escape(e.Message)})", Styles.Dim);
        }
    }

    private static void SkipTime(bool toNight)
    {
        var director = GameAccess.Director;
        if (director == null) return;
        if (director.IsDaytime != toNight) return;
        float now = director.currentTime % 24f;
        float target = toNight ? director.dayEnd : director.dayStart;
        float skip = target - now;
        if (skip <= 0f) skip += 24f;
        director.AdvanceTime(skip + 0.1f);
    }

    private static string Census()
    {
        try
        {
            int island = GameAccess.CurrentIsland;
            int regular = 0, cliff = 0, coin = 0, gem = 0;
            foreach (var p in Object.FindObjectsByType<Portal>(FindObjectsSortMode.None))
            {
                if (p == null) continue;
                if (p.type == Portal.Type.Regular) regular++;
                else if (p.type == Portal.Type.Cliff) cliff++;
            }
            foreach (var c in Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
            {
                if (c == null) continue;
                if (c.currencyType == CurrencyType.Gems) gem++;
                else coin++;
            }
            int statues = Object.FindObjectsByType<Statue>(FindObjectsSortMode.None).Length;
            int shrines = Object.FindObjectsByType<SteedSpawn>(FindObjectsSortMode.None).Length;
            int cabins = Object.FindObjectsByType<Cabin>(FindObjectsSortMode.None).Length;
            var walls = Object.FindObjectsByType<Wall>(FindObjectsSortMode.None).Where(w => w != null).Select(w => w.level).OrderBy(l => l).ToList();
            int towers = Object.FindObjectsByType<Tower>(FindObjectsSortMode.None).Length;
            var campaign = AP.Slot.Campaign;
            string Expect(GameData.IslandThing t) => island > 0 && !campaign.CampaignWide ? $"/{GameData.ExpectedCount(campaign, island, t)}" : "";
            var text =
                $"Island {island}: portals {regular}{Expect(GameData.IslandThing.Portal)} (+{cliff} cliff), " +
                $"coin chests {coin}{Expect(GameData.IslandThing.CoinChest)}, gem chests {gem}{Expect(GameData.IslandThing.GemChest)}, " +
                $"statues {statues}{Expect(GameData.IslandThing.Statue)}, mount shrines {shrines}{Expect(GameData.IslandThing.MountShrine)}, " +
                $"hermit cabins {cabins}{Expect(GameData.IslandThing.HermitCabin)}\n" +
                $"Walls: {walls.Count} (levels {string.Join(",", walls)}), towers: {towers}. Numbers after '/' are what the apworld expects; objects already used up may no longer exist.";
            Plugin.Log.LogInfo("[AP] Census: " + text);
            return text;
        }
        catch (Exception e)
        {
            return "Census failed: " + e.Message;
        }
    }

    // ---------------------------------------------------------------- SETTINGS

    private void DrawSettingsTab()
    {
        var s = ClientSettings.Current;
        bool changed = false;

        BeginCard("MENU");
        float shownScale = _draggingScale ? _pendingScale : s.WindowScale;
        float scale = SliderRow("Window scale", shownScale, 0.6f, 2f, "F2");
        if (Math.Abs(scale - shownScale) > 0.001f)
        {
            _pendingScale = scale;
            _draggingScale = true; // applied in Update once the mouse button is released
        }
        float opacity = SliderRow("Menu opacity", s.MenuOpacity, 0.3f, 1f, "F2");
        if (Math.Abs(opacity - s.MenuOpacity) > 0.001f) { s.MenuOpacity = opacity; changed = true; }
        Gui.BeginHorizontal();
        Gui.Label("Menu key", Styles.Body, Gui.Width(170f));
        if (Gui.Button(((KeyCode)s.MenuKey).ToString(), Styles.ButtonSmall, Gui.Width(100f)))
        {
            int i = Array.IndexOf(MenuKeys, (KeyCode)s.MenuKey);
            s.MenuKey = (int)MenuKeys[(i + 1) % MenuKeys.Length];
            changed = true;
        }
        Gui.Label("<color=#998C73>(if you also run Kingdom Enhanced, it uses F1, F3 and F4)</color>", Styles.Small);
        GUILayout.EndHorizontal();
        if (Gui.Button("Reset window size and position", Styles.ButtonSmall, Gui.Width(240f)))
        {
            _rect = new Rect(40f, 90f, 680f, 520f);
            s.WindowScale = 1f;
            _draggingScale = false;
            changed = true;
            SaveWindow();
        }
        EndCard();

        BeginCard("NOTIFICATION FEED");
        Gui.Label("Items you send and receive, traps and connection news pop up in a screen corner even with the menu closed.", Styles.Dim);
        if (PillRow("Show the feed", s.FeedEnabled)) { s.FeedEnabled = !s.FeedEnabled; changed = true; }
        if (PillRow("Only my items (hide other players' sends and chat)", s.FeedOnlyMine)) { s.FeedOnlyMine = !s.FeedOnlyMine; changed = true; }
        Gui.BeginHorizontal();
        Gui.Label("Corner", Styles.Body, Gui.Width(170f));
        string[] corners = { "Bottom right", "Bottom left", "Top right", "Top left" };
        if (Gui.Button(corners[Mathf.Clamp(s.FeedCorner, 0, 3)], Styles.ButtonSmall, Gui.Width(120f)))
        {
            s.FeedCorner = (s.FeedCorner + 1) % 4;
            changed = true;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        float duration = SliderRow("Seconds on screen", s.FeedDuration, 3f, 30f, "F0");
        float lines = SliderRow("Lines shown", s.FeedLines, 2f, 15f, "F0");
        float feedScale = SliderRow("Feed size", s.FeedScale, 0.6f, 2f, "F2");
        if (Math.Abs(duration - s.FeedDuration) > 0.01f) { s.FeedDuration = Mathf.Round(duration); changed = true; }
        if (Mathf.RoundToInt(lines) != s.FeedLines) { s.FeedLines = Mathf.RoundToInt(lines); changed = true; }
        if (Math.Abs(feedScale - s.FeedScale) > 0.001f) { s.FeedScale = feedScale; changed = true; }
        if (Gui.Button("Show a test notification", Styles.ButtonSmall, Gui.Width(200f)))
            Feed.Add($"<color={Styles.HexMe}>{AP.Escape(string.IsNullOrEmpty(AP.SlotName) ? "Monarch" : AP.SlotName)}</color> found their <color={Styles.HexProgression}>Stone Technology</color> (<color={Styles.HexLocation}>Island 1: Coin Chest 1</color>)");
        EndCard();

        if (changed) _settingsDirtyUntil = Time.unscaledTime + 1f;
        if (_settingsDirtyUntil > 0f && Time.unscaledTime > _settingsDirtyUntil)
        {
            _settingsDirtyUntil = 0f;
            ClientSettings.Save();
        }
    }

    private float _settingsDirtyUntil;
}
