using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using APHint = Archipelago.MultiClient.Net.Models.Hint;
using Coatsink.Common;
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
        KeyCode.F2, KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11,
        KeyCode.Insert, KeyCode.Home, KeyCode.BackQuote,
    };

    private const int WindowId = 734022;
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
    private bool _resizing;
    private GUI.WindowFunction _windowFunc;
    private bool _showPassword;
    private string _toast = "";
    private float _toastUntil;

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
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        if (!visible)
        {
            TextInput.ClearFocus();
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

        if (_visible && Event.current.type == EventType.MouseDown) TextInput.ClearFocus(); // clicking a box re-focuses it

        Feed.Draw();
        if (!_visible) return;

        var s = ClientSettings.Current;
        float scale = Mathf.Clamp(s.WindowScale, 0.5f, 2.5f);
        var oldMatrix = GUI.matrix;
        var oldBg = GUI.backgroundColor;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        float maxW = Screen.width / scale, maxH = Screen.height / scale;
        _rect.width = Mathf.Clamp(_rect.width, 520f, Mathf.Max(520f, maxW));
        _rect.height = Mathf.Clamp(_rect.height, 380f, Mathf.Max(380f, maxH));
        _rect.x = Mathf.Clamp(_rect.x, 0f, Mathf.Max(0f, maxW - 80f));
        _rect.y = Mathf.Clamp(_rect.y, 0f, Mathf.Max(0f, maxH - 60f));

        GUI.backgroundColor = new Color(1f, 1f, 1f, Mathf.Clamp(s.MenuOpacity, 0.3f, 1f));
        _windowFunc ??= (Action<int>)DrawWindow;
        _rect = GUI.Window(WindowId, _rect, _windowFunc, GUIContent.none, Styles.Window);

        GUI.backgroundColor = oldBg;
        GUI.matrix = oldMatrix;
    }

    private void DrawWindow(int id)
    {
        try
        {
            GUILayout.BeginVertical();

            GUILayout.BeginVertical(Styles.Header, GUILayout.Height(HeaderHeight));
            GUILayout.Label("Archipelago: Kingdom Two Crowns", Styles.Title);
            GUILayout.Label($"v{MyPluginInfo.PLUGIN_VERSION}   |   {StatusLine()}", Styles.Subtitle);
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            DrawSidebar();
            GUILayout.BeginVertical();
            if (_tab == Tab.Chat)
            {
                DrawChatTab();
            }
            else
            {
                _scroll[(int)_tab] = GUILayout.BeginScrollView(_scroll[(int)_tab]);
                DrawTab();
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Time.unscaledTime < _toastUntil ? _toast : "", Styles.Small);
            GUILayout.FlexibleSpace();
            var grip = GUILayoutUtility.GetRect(18f, 18f);
            GUI.Label(grip, "<color=#998C73>//</color>", Styles.Small);
            HandleResize(grip);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[AP] Menu: {e}");
        }
        GUI.DragWindow(new Rect(0f, 0f, _rect.width, HeaderHeight));
    }

    private static string StatusLine()
    {
        if (AP.Connected) return $"<color={Styles.HexGood}>Connected</color> as {AP.Escape(AP.SlotName)}   {AP.CheckedCount}/{AP.LocationCount} checks";
        if (AP.Connecting) return $"<color={Styles.HexGold}>Connecting...</color>";
        return $"<color={Styles.HexBad}>Not connected</color>";
    }

    private void DrawSidebar()
    {
        GUILayout.BeginVertical(Styles.Header, GUILayout.Width(SidebarWidth));
        GUILayout.Space(6f);
        for (int i = 0; i < TabLabels.Length; i++)
        {
            if (GUILayout.Button(TabLabels[i], _tab == (Tab)i ? Styles.TabActive : Styles.Tab, GUILayout.Height(30f)))
            {
                _tab = (Tab)i;
                TextInput.ClearFocus();
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{(KeyCode)ClientSettings.Current.MenuKey} to close", Styles.Credit);
        GUILayout.Space(4f);
        GUILayout.EndVertical();
    }

    private void HandleResize(Rect grip)
    {
        var e = Event.current;
        if (e.type == EventType.MouseDown && grip.Contains(e.mousePosition))
        {
            _resizing = true;
            e.Use();
        }
        else if (e.type == EventType.MouseUp && _resizing)
        {
            _resizing = false;
            SaveWindow();
        }
        if (_resizing && e.type == EventType.MouseDrag)
        {
            _rect.width = Mathf.Max(520f, _rect.width + e.delta.x);
            _rect.height = Mathf.Max(380f, _rect.height + e.delta.y);
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

    private static void BeginCard(string title)
    {
        GUILayout.BeginVertical(Styles.Card);
        GUILayout.Label(title, Styles.Section);
    }

    private static void EndCard() => GUILayout.EndVertical();

    private static void Row(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, Styles.Body, GUILayout.Width(190f));
        GUILayout.Label(value, Styles.Body);
        GUILayout.EndHorizontal();
    }

    private static bool Pill(bool on, string onText = "ON", string offText = "OFF")
    {
        var old = GUI.backgroundColor;
        GUI.backgroundColor = on ? Styles.On : Styles.Off;
        bool clicked = GUILayout.Button(on ? onText : offText, Styles.Pill, GUILayout.Width(64f));
        GUI.backgroundColor = old;
        return clicked;
    }

    private static bool PillRow(string label, bool on)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, Styles.Body, GUILayout.Width(280f));
        bool clicked = Pill(on);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        return clicked;
    }

    private static float SliderRow(string label, float value, float min, float max, string format)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, Styles.Body, GUILayout.Width(170f));
        GUILayout.Label(value.ToString(format), Styles.Body, GUILayout.Width(44f));
        float v = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        return v;
    }

    // A button that needs a second click within a few seconds, for anything destructive.
    private static bool ConfirmButton(string label, string key, GUIStyle style, params GUILayoutOption[] options)
    {
        bool armed = _armed.TryGetValue(key, out var until) && Time.unscaledTime < until;
        if (!GUILayout.Button(armed ? $"<color={Styles.HexBad}>Confirm?</color>" : label, style, options)) return false;
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
        GUILayout.Label("Server address (host:port)", Styles.Dim);
        _host.Draw(Styles.TextField, GUILayout.Height(28f));
        GUILayout.Label("Slot name", Styles.Dim);
        _slot.Draw(Styles.TextField, GUILayout.Height(28f));
        GUILayout.Label("Password (leave empty if the room has none)", Styles.Dim);
        GUILayout.BeginHorizontal();
        _password.Masked = !_showPassword;
        _password.Draw(Styles.TextField, GUILayout.Height(28f), GUILayout.ExpandWidth(true));
        if (GUILayout.Button(_showPassword ? "Hide" : "Show", Styles.ButtonSmall, GUILayout.Width(60f), GUILayout.Height(28f)))
            _showPassword = !_showPassword;
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        if (AP.Connecting)
        {
            GUILayout.Button("Connecting...", Styles.Button, GUILayout.Height(32f), GUILayout.Width(180f));
        }
        else if (AP.Connected)
        {
            if (GUILayout.Button("Disconnect", Styles.Button, GUILayout.Height(32f), GUILayout.Width(180f))) AP.Disconnect();
        }
        else if (GUILayout.Button($"<b>Connect</b>", Styles.Button, GUILayout.Height(32f), GUILayout.Width(180f)))
        {
            ConnectFromFields();
        }
        GUILayout.Space(10f);
        GUILayout.Label(AP.StatusIsError ? $"<color={Styles.HexBad}>{AP.Escape(AP.Status)}</color>" : AP.Escape(AP.Status), Styles.Body);
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
            int liberated = Enumerable.Range(1, GameData.IslandCount).Count(GameAccess.IsIslandSecured);
            Row("Goal", $"Liberate {AP.Slot.IslandsToLiberate} island(s), {liberated} done");
            Row("Island unlocks", AP.Slot.IslandUnlocks ? "On (islands are items)" : "Off (islands open in order)");
            GUILayout.BeginHorizontal();
            GUILayout.Label("DeathLink", Styles.Body, GUILayout.Width(190f));
            if (Pill(AP.DeathLinkOn)) AP.SetDeathLink(!AP.DeathLinkOn);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EndCard();
        }

        BeginCard("HOW TO");
        GUILayout.Label($"Press <b>{(KeyCode)s.MenuKey}</b> to open or close this menu (change it in Settings).", Styles.Small);
        GUILayout.Label("Click a box to type. Enter submits, Esc stops typing, Ctrl+V pastes, Ctrl+Backspace clears. Your monarch won't move while you type.", Styles.Small);
        GUILayout.Label("Received and sent items also pop up in the corner feed, so you only need this menu to connect, hint, chat or check progress.", Styles.Small);
        EndCard();
    }

    // ---------------------------------------------------------------- ITEMS

    private void DrawItemsTab()
    {
        BeginCard("UNLOCKS");
        int castle = ItemHandler.Count(GameData.ProgressiveCastle);
        Row("Castle upgrades", $"{castle} / {GameData.MaxCastleUpgrades}   <color={Styles.HexDim}>(castle can reach level {Math.Min(7, castle + 1)})</color>");
        Row("Technology", $"{Check(ItemHandler.Has(GameData.StoneTech))} Stone   {Check(ItemHandler.Has(GameData.IronTech))} Iron");
        var islands = string.Join("   ", Enumerable.Range(1, GameData.IslandCount).Select(i => $"{Check(Gating.IsIslandUnlocked(i))} {i}"));
        Row("Islands", AP.Slot.IslandUnlocks ? islands : "All open (in order)");
        foreach (var group in GameData.ItemGroups.Where(g => g.Title is "Statues" or "Hermits" or "Mounts"))
        {
            GUILayout.Label(group.Title, Styles.Dim);
            for (int i = 0; i < group.Names.Length; i += 3)
            {
                GUILayout.BeginHorizontal();
                foreach (var name in group.Names.Skip(i).Take(3))
                    GUILayout.Label($"{Check(ItemHandler.Has(name))} {name}", Styles.Small, GUILayout.Width(170f));
                GUILayout.EndHorizontal();
            }
        }
        if (ItemHandler.DebugGrants.Count > 0)
            GUILayout.Label($"<color={Styles.HexTrap}>Includes {ItemHandler.DebugGrants.Values.Sum()} debug grant(s) from the Debug tab.</color>", Styles.Small);
        EndCard();

        BeginCard($"RECEIVED ITEMS ({AP.Received.Count})");
        if (AP.Received.Count == 0) GUILayout.Label("Nothing yet.", Styles.Dim);
        for (int i = AP.Received.Count - 1; i >= 0; i--)
        {
            var item = AP.Received[i];
            var from = item.Sender == AP.SlotName ? "you" : AP.Escape(item.Sender);
            var where = string.IsNullOrEmpty(item.Location) ? "" : $" <color={Styles.HexDim}>({AP.Escape(item.Location)})</color>";
            GUILayout.Label($"<color={ItemColor(item.Flags)}>{AP.Escape(item.Name)}</color>  from <color={Styles.HexPlayer}>{from}</color>{where}", Styles.Small);
        }
        EndCard();
    }

    // ---------------------------------------------------------------- CHECKS

    private static List<string> MissingLocations()
    {
        if (!AP.Connected) return new List<string>();
        if (_missingCacheChecked != AP.CheckedCount || Time.unscaledTime - _missingCacheTime > 3f)
        {
            _missingCache = AP.MissingLocationNames().OrderBy(n => n).ToList();
            _missingCacheChecked = AP.CheckedCount;
            _missingCacheTime = Time.unscaledTime;
        }
        return _missingCache;
    }

    private void DrawChecksTab()
    {
        int island = GameAccess.CurrentIsland;

        BeginCard("PROGRESS");
        Row("Checks done", AP.Connected ? $"{AP.CheckedCount} / {AP.LocationCount}" : "connect to see");
        Row("Current island", island > 0 ? island.ToString() : "-");
        if (AP.State.PendingLocations.Count > 0)
            Row("Waiting to send", $"<color={Styles.HexGold}>{AP.State.PendingLocations.Count} (will send on reconnect)</color>");
        EndCard();

        BeginCard("MILESTONES");
        foreach (var milestone in AP.Slot.Milestones.Keys)
        {
            var (interval, count) = AP.Slot.Milestones[milestone];
            if (count == 0) continue;
            int value = CheckTracker.MilestoneCount(milestone);
            var next = CheckTracker.NextMilestone(milestone);
            var nextText = next.HasValue
                ? $"next check at {next.Value}  <color={Styles.HexDim}>(every {interval}, {count} checks)</color>"
                : $"<color={Styles.HexGood}>all {count} done</color>";
            Row(GameData.Milestones[milestone].Label, $"<b>{value}</b>   {nextText}");
        }
        if (AP.Slot.ForestChecks > 0)
            Row("Forests cleared", $"<b>{AP.State.Counter(CheckTracker.ForestCounter)}</b> / {AP.Slot.ForestChecks}");
        EndCard();

        if (island > 0)
        {
            BeginCard($"ISLAND {island}");
            foreach (GameData.IslandThing thing in Enum.GetValues(typeof(GameData.IslandThing)))
            {
                int expected = GameData.ExpectedCount(island, thing);
                if (expected == 0) continue;
                AP.State.CountedPositions.TryGetValue($"{thing}@{island}", out var seen);
                int done = Enumerable.Range(1, expected).Count(k => AP.IsChecked(GameData.IslandThingLocation(island, thing, k)));
                Row(GameData.IslandThingLabel(thing), $"{done} / {expected} checked  <color={Styles.HexDim}>({seen?.Count ?? 0} found here)</color>");
            }
            Row("Build the Boat", Check(AP.IsChecked(GameData.BoatLocation(island))));
            if (island > 1) Row("Arrive", Check(AP.IsChecked(GameData.ArriveLocation(island))));
            Row("Liberate", Check(AP.IsChecked(GameData.LiberateLocation(island))));
            int offset = AP.State.WaveOffset(island);
            if (offset > 0) Row("Waves Gone By", $"<color={Styles.HexTrap}>+{offset} days</color>");
            EndCard();
        }

        var missing = MissingLocations();
        BeginCard($"MISSING CHECKS ({missing.Count})");
        if (!AP.Connected) GUILayout.Label("Connect to see which checks are left.", Styles.Dim);
        else GUILayout.Label($"Hint points: {AP.HintPoints}. Hinting a location reveals what's there.", Styles.Dim);
        foreach (var name in missing)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(AP.Escape(name), Styles.Small);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Hint", Styles.ButtonSmall, GUILayout.Width(50f))) RequestHint(name, location: true);
            GUILayout.EndHorizontal();
        }
        EndCard();
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
        if (!AP.Connected) GUILayout.Label("Connect to request hints.", Styles.Dim);
        else
        {
            int cost = AP.HintCost;
            GUILayout.Label($"Hint points: <b>{AP.HintPoints}</b>{(cost > 0 ? $"   (a hint costs {cost})" : "")}", Styles.Body);
        }
        GUILayout.BeginHorizontal();
        _hint.Draw(Styles.TextField, GUILayout.Height(28f), GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Hint item", Styles.ButtonSmall, GUILayout.Width(80f), GUILayout.Height(28f))) RequestHint(_hint.Text, false);
        if (GUILayout.Button("Hint location", Styles.ButtonSmall, GUILayout.Width(100f), GUILayout.Height(28f))) RequestHint(_hint.Text, true);
        GUILayout.EndHorizontal();

        var wanted = new List<string>();
        if (ItemHandler.Count(GameData.ProgressiveCastle) < GameData.MaxCastleUpgrades) wanted.Add(GameData.ProgressiveCastle);
        if (!ItemHandler.Has(GameData.StoneTech)) wanted.Add(GameData.StoneTech);
        if (!ItemHandler.Has(GameData.IronTech)) wanted.Add(GameData.IronTech);
        if (AP.Slot.IslandUnlocks)
            for (int i = 2; i <= GameData.IslandCount; i++)
                if (!ItemHandler.Has(GameData.IslandUnlock(i))) wanted.Add(GameData.IslandUnlock(i));
        if (wanted.Count > 0)
        {
            GUILayout.Label("Quick hints for key items you don't have yet:", Styles.Dim);
            for (int i = 0; i < wanted.Count; i += 3)
            {
                GUILayout.BeginHorizontal();
                foreach (var name in wanted.Skip(i).Take(3))
                    if (GUILayout.Button(name, Styles.ButtonSmall, GUILayout.Width(170f))) RequestHint(name, false);
                GUILayout.EndHorizontal();
            }
        }
        if (!string.IsNullOrEmpty(AP.LastServerReply))
        {
            GUILayout.Space(4f);
            GUILayout.Label("Server: " + AP.LastServerReply, Styles.Small);
        }
        EndCard();

        var hints = AP.Hints.OrderBy(h => h.Found).ThenBy(h => h.ReceivingPlayer != AP.SlotId).ToList();
        BeginCard($"HINTS ({hints.Count})");
        if (hints.Count == 0) GUILayout.Label("No hints yet.", Styles.Dim);
        foreach (var hint in hints)
        {
            string receiver = hint.ReceivingPlayer == AP.SlotId ? $"<color={Styles.HexMe}>Your</color>" : $"<color={Styles.HexPlayer}>{AP.Escape(AP.PlayerName(hint.ReceivingPlayer))}'s</color>";
            string finder = hint.FindingPlayer == AP.SlotId ? $"<color={Styles.HexMe}>your</color>" : $"<color={Styles.HexPlayer}>{AP.Escape(AP.PlayerName(hint.FindingPlayer))}'s</color>";
            string item = $"<color={ItemColor(hint.ItemFlags)}>{AP.Escape(AP.ItemName(hint.ItemId, hint.ReceivingPlayer))}</color>";
            string loc = $"<color={Styles.HexLocation}>{AP.Escape(AP.LocationName(hint.LocationId, hint.FindingPlayer))}</color>";
            string entrance = string.IsNullOrEmpty(hint.Entrance) ? "" : $" ({AP.Escape(hint.Entrance)})";
            string state = hint.Found ? $"<color={Styles.HexGood}>found</color>" : $"<color={Styles.HexGold}>not found</color>";
            GUILayout.Label($"{receiver} {item} is at {loc} in {finder} world{entrance}  [{state}]", Styles.Small);
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
        GUILayout.BeginHorizontal();
        GUILayout.Label("Everything the server says: items for everyone, hints, chat and command replies.", Styles.Dim);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(_chatOnlyMine ? "Showing: mine" : "Showing: all", Styles.ButtonSmall, GUILayout.Width(110f))) _chatOnlyMine = !_chatOnlyMine;
        if (GUILayout.Button(_chatAutoScroll ? "Auto-scroll on" : "Auto-scroll off", Styles.ButtonSmall, GUILayout.Width(110f))) _chatAutoScroll = !_chatAutoScroll;
        GUILayout.EndHorizontal();

        if (_chatAutoScroll && _chatSeenVersion != AP.LogVersion)
        {
            _chatScroll.y = float.MaxValue;
            _chatSeenVersion = AP.LogVersion;
        }

        _chatScroll = GUILayout.BeginScrollView(_chatScroll, Styles.Card, GUILayout.ExpandHeight(true));
        int shown = 0;
        for (int i = Math.Max(0, AP.Log.Count - 250); i < AP.Log.Count; i++)
        {
            var line = AP.Log[i];
            if (_chatOnlyMine && !line.RelatedToMe) continue;
            GUILayout.Label(line.Text, Styles.LogLine);
            shown++;
        }
        if (shown == 0) GUILayout.Label(AP.Connected ? "Nothing yet." : "Connect to see the server log.", Styles.Dim);
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        _chat.Draw(Styles.TextField, GUILayout.Height(28f), GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Send", Styles.Button, GUILayout.Width(70f), GUILayout.Height(28f))) SendChat(_chat.Text);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        foreach (var cmd in new[] { "!remaining", "!missing", "!hint", "!collect", "!release" })
        {
            if (GUILayout.Button(cmd, Styles.ButtonSmall, GUILayout.Width(84f)))
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
        GUILayout.Label("Give yourself any item or trap to see how it behaves. Debug grants only exist locally and are lost when the game closes. Sending a check below is real and goes to the server.", Styles.Dim);
        if (PillRow("Apply AP rules while not connected", ItemHandler.SimulateGating))
            ItemHandler.SimulateGating = !ItemHandler.SimulateGating;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear debug grants", Styles.ButtonSmall, GUILayout.Width(150f))) ItemHandler.ClearDebugGrants();
        GUILayout.Label($"{ItemHandler.DebugGrants.Values.Sum()} granted, {ItemHandler.PendingEffects} effect(s) queued, {Gating.BlockedCount} payable(s) blocked", Styles.Small);
        GUILayout.EndHorizontal();
        if (!GameAccess.IsPlaying) GUILayout.Label($"<color={Styles.HexGold}>Effects wait until you're in a game.</color>", Styles.Small);
        EndCard();

        BeginCard("GIVE ITEM");
        foreach (var group in GameData.ItemGroups)
        {
            GUILayout.Label(group.Title, Styles.Dim);
            for (int i = 0; i < group.Names.Length; i += 3)
            {
                GUILayout.BeginHorizontal();
                foreach (var name in group.Names.Skip(i).Take(3))
                {
                    var label = name;
                    if (group.Kind == GameData.ItemKind.Unlock)
                    {
                        int n = ItemHandler.Count(name);
                        if (n > 0) label = $"{name} <color={Styles.HexGood}>({n})</color>";
                    }
                    if (GUILayout.Button(label, Styles.ButtonSmall, GUILayout.Width(170f))) ItemHandler.DebugGive(name);
                }
                GUILayout.EndHorizontal();
            }
        }
        EndCard();

        BeginCard("SIMULATE EVENTS");
        GUILayout.Label("Bumps the counters the same way the game would (sends checks if a milestone is reached).", Styles.Dim);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+1 Night", Styles.ButtonSmall)) CheckTracker.AddMilestone(GameData.Milestone.Nights);
        if (GUILayout.Button("+5 Trees", Styles.ButtonSmall)) CheckTracker.AddMilestone(GameData.Milestone.Trees, 5);
        if (GUILayout.Button("+1 Forest", Styles.ButtonSmall)) CheckTracker.OnForestCleared(new IntPtr(Environment.TickCount));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+5 Greedlings", Styles.ButtonSmall)) for (int i = 0; i < 5; i++) CheckTracker.OnEnemyKilled(EnemyType.TrollWeak);
        if (GUILayout.Button("+1 Floater", Styles.ButtonSmall)) CheckTracker.OnEnemyKilled(EnemyType.Squid);
        if (GUILayout.Button("+1 Breeder", Styles.ButtonSmall)) CheckTracker.OnEnemyKilled(EnemyType.Boss);
        if (GUILayout.Button("+1 Crown Stealer", Styles.ButtonSmall)) CheckTracker.OnEnemyKilled(EnemyType.Stealer);
        GUILayout.EndHorizontal();
        EndCard();

        BeginCard("GAME STATE");
        DrawGameState();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Skip to night", Styles.ButtonSmall)) SkipTime(toNight: true);
        if (GUILayout.Button("Skip to day", Styles.ButtonSmall)) SkipTime(toNight: false);
        if (GUILayout.Button("Refresh island census", Styles.ButtonSmall)) _census = Census();
        GUILayout.EndHorizontal();
        GUILayout.Label(_census, Styles.Small);
        EndCard();

        BeginCard("SEND A CHECK");
        if (!AP.Connected)
        {
            GUILayout.Label("Connect to send checks by hand.", Styles.Dim);
        }
        else
        {
            GUILayout.Label("For when a check didn't fire. Click Send twice to confirm.", Styles.Dim);
            foreach (var name in MissingLocations())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(AP.Escape(name), Styles.Small);
                GUILayout.FlexibleSpace();
                if (ConfirmButton("Send", "send:" + name, Styles.ButtonSmall, GUILayout.Width(70f))) AP.SendLocation(name);
                GUILayout.EndHorizontal();
            }
        }
        EndCard();

        BeginCard("RESET");
        GUILayout.BeginHorizontal();
        if (ConfirmButton("Reset milestone counters", "reset-counters", Styles.ButtonSmall, GUILayout.Width(200f)))
        {
            AP.State.Counters.Clear();
            AP.State.Dirty = true;
            Toast("Milestone counters reset.");
        }
        if (ConfirmButton("Forget counted chests/portals", "reset-positions", Styles.ButtonSmall, GUILayout.Width(220f)))
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
            var secured = string.Join(" ", Enumerable.Range(1, GameData.IslandCount).Select(i => $"{i}:{(GameAccess.IsIslandSecured(i) ? "yes" : "no")}"));
            Row("Islands liberated", secured);
            Row("Wave offset here", $"+{AP.State.WaveOffset(GameAccess.CurrentIsland)} days");
            Row("Gating", $"{(Gating.Active ? "active" : "off")}, islands {(Gating.IslandGatingActive ? "gated" : "open")}");
            if (GameAccess.CastleLevel >= 7) Row("Full upgrade", CheckTracker.FullUpgradeStatus().Detail);
        }
        catch (Exception e)
        {
            GUILayout.Label($"(state unavailable: {AP.Escape(e.Message)})", Styles.Dim);
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
            string Expect(GameData.IslandThing t) => island > 0 ? $"/{GameData.ExpectedCount(island, t)}" : "";
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
        float scale = SliderRow("Window scale", s.WindowScale, 0.6f, 2f, "F2");
        float opacity = SliderRow("Menu opacity", s.MenuOpacity, 0.4f, 1f, "F2");
        if (Math.Abs(scale - s.WindowScale) > 0.001f) { s.WindowScale = scale; changed = true; }
        if (Math.Abs(opacity - s.MenuOpacity) > 0.001f) { s.MenuOpacity = opacity; changed = true; }
        GUILayout.BeginHorizontal();
        GUILayout.Label("Menu key", Styles.Body, GUILayout.Width(170f));
        if (GUILayout.Button(((KeyCode)s.MenuKey).ToString(), Styles.ButtonSmall, GUILayout.Width(100f)))
        {
            int i = Array.IndexOf(MenuKeys, (KeyCode)s.MenuKey);
            s.MenuKey = (int)MenuKeys[(i + 1) % MenuKeys.Length];
            changed = true;
        }
        GUILayout.Label("<color=#998C73>(F1, F3 and F4 belong to Kingdom Enhanced)</color>", Styles.Small);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Reset window size and position", Styles.ButtonSmall, GUILayout.Width(240f)))
        {
            _rect = new Rect(40f, 90f, 680f, 520f);
            s.WindowScale = 1f;
            changed = true;
            SaveWindow();
        }
        EndCard();

        BeginCard("NOTIFICATION FEED");
        GUILayout.Label("Items you send and receive, traps and connection news pop up in a screen corner even with the menu closed.", Styles.Dim);
        if (PillRow("Show the feed", s.FeedEnabled)) { s.FeedEnabled = !s.FeedEnabled; changed = true; }
        if (PillRow("Only my items (hide other players' sends and chat)", s.FeedOnlyMine)) { s.FeedOnlyMine = !s.FeedOnlyMine; changed = true; }
        GUILayout.BeginHorizontal();
        GUILayout.Label("Corner", Styles.Body, GUILayout.Width(170f));
        string[] corners = { "Bottom right", "Bottom left", "Top right", "Top left" };
        if (GUILayout.Button(corners[Mathf.Clamp(s.FeedCorner, 0, 3)], Styles.ButtonSmall, GUILayout.Width(120f)))
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
        if (GUILayout.Button("Show a test notification", Styles.ButtonSmall, GUILayout.Width(200f)))
            Feed.Add($"<color={Styles.HexMe}>{AP.Escape(string.IsNullOrEmpty(AP.SlotName) ? "Monarch" : AP.SlotName)}</color> found their <color={Styles.HexProgression}>Stone Technology</color> (<color={Styles.HexLocation}>Island 1: Coin Chest 1</color>)");
        EndCard();

        BeginCard("FILES");
        GUILayout.Label($"Settings and per-seed progress are saved in {AP.Escape(Storage.Folder)}. The old APKingdom2Crown.cfg is no longer used and can be deleted.", Styles.Dim);
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
