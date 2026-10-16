using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using APHint = Archipelago.MultiClient.Net.Models.Hint;

namespace APKingdom2Crown;

internal sealed class ReceivedItem
{
    public int Index;
    public string Name;
    public string Sender;
    public string Location;
    public ItemFlags Flags;
}

internal sealed class LogLine
{
    public string Text; // IMGUI rich text
    public bool RelatedToMe;
    public float Time;
}

// The Archipelago connection. Socket callbacks arrive on background threads, so everything they do is
// queued and run on Unity's main thread by Pump() (called from APRunner.Update).
internal static class AP
{
    private static readonly Version ProtocolVersion = new(0, 6, 2);
    private const int MaxLogLines = 400;

    public static ArchipelagoSession Session { get; private set; }
    public static bool Connected => _loggedIn && (Session?.Socket?.Connected ?? false);
    public static bool Connecting { get; private set; }
    public static string Status { get; private set; } = "Not connected";
    public static bool StatusIsError { get; private set; }

    public static SlotSettings Slot { get; private set; } = SlotSettings.Default;
    public static string SlotName { get; private set; } = "";
    public static int SlotId { get; private set; } = -1;
    public static int Team { get; private set; }
    public static string Seed { get; private set; } = "";
    public static string HostConnectedTo { get; private set; } = "";

    // Persistent progress for the current seed. Kept after a disconnect so counting carries on and
    // pending checks can be flushed on reconnect.
    public static ModState State { get; private set; } = new();

    public static readonly List<ReceivedItem> Received = new();
    public static readonly List<LogLine> Log = new();
    public static APHint[] Hints { get; private set; } = Array.Empty<APHint>();
    public static string LastServerReply { get; private set; } = "";
    public static int LogVersion { get; private set; }

    private static readonly ConcurrentQueue<Action> MainThread = new();
    private static readonly HashSet<long> SentThisSession = new();
    private static DeathLinkService _deathLink;
    private static bool _loggedIn;
    private static bool _goalSent;

    public static event Action OnConnected;

    public static void Pump()
    {
        while (MainThread.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception e) { Plugin.Log.LogError($"[AP] {e}"); }
        }
    }

    public static void Connect(string host, string slot, string password)
    {
        if (Connecting) return;
        host = host.Trim();
        slot = slot.Trim();
        if (host.Length == 0 || slot.Length == 0)
        {
            SetStatus("Enter a server address and slot name first.", true);
            return;
        }

        Disconnect(quiet: true);
        Connecting = true;
        Received.Clear();
        SentThisSession.Clear();
        _goalSent = false;
        SetStatus($"Connecting to {host}...", false);

        Task.Run(() =>
        {
            ArchipelagoSession session = null;
            try
            {
                session = ArchipelagoSessionFactory.CreateSession(host);
                var s = session;
                int counter = 0;
                session.Items.ItemReceived += helper => OnItemReceived(s, helper, ref counter);
                session.MessageLog.OnMessageReceived += msg => MainThread.Enqueue(() => OnMessage(msg));
                session.Socket.SocketClosed += reason => MainThread.Enqueue(() => OnSocketClosed(session, reason));
                session.Socket.ErrorReceived += (e, message) => Plugin.Log.LogWarning($"[AP] Socket error: {message} {e?.Message}");
                Session = session;

                var result = session.TryConnectAndLogin(
                    GameData.GameName, slot, ItemsHandlingFlags.AllItems, ProtocolVersion,
                    Array.Empty<string>(), null, string.IsNullOrEmpty(password) ? null : password, true);

                if (result is LoginSuccessful success)
                {
                    MainThread.Enqueue(() => OnLoginSuccess(session, success, host, slot));
                }
                else
                {
                    var failure = (LoginFailure)result;
                    var errors = string.Join("; ", failure.Errors.Concat(failure.ErrorCodes.Select(c => c.ToString())));
                    CloseSocket(session.Socket);
                    MainThread.Enqueue(() =>
                    {
                        Connecting = false;
                        if (Session == session) Session = null;
                        SetStatus($"Login failed: {errors}", true);
                    });
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[AP] Connection failed: {e}");
                MainThread.Enqueue(() =>
                {
                    Connecting = false;
                    if (Session == session) Session = null;
                    SetStatus($"Connection failed: {e.GetBaseException().Message}", true);
                });
            }
        });
    }

    public static void Disconnect(bool quiet = false)
    {
        var session = Session;
        _loggedIn = false;
        Session = null;
        _deathLink = null;
        if (session != null)
        {
            var socket = session.Socket;
            Task.Run(() => CloseSocket(socket));
        }
        if (!quiet) SetStatus("Disconnected.", false);
        State.Save();
    }

    private static void OnLoginSuccess(ArchipelagoSession session, LoginSuccessful success, string host, string slot)
    {
        Connecting = false;
        if (Session != session) return; // a newer connect attempt replaced this one

        _loggedIn = true;
        Slot = SlotSettings.FromSlotData(success.SlotData);
        SlotName = slot;
        SlotId = success.Slot;
        Team = success.Team;
        Seed = session.RoomState.Seed ?? "";
        HostConnectedTo = host;

        if (State.Seed != Seed || State.Slot != SlotName)
        {
            State.Save();
            State = ModState.LoadFor(Seed, SlotName);
        }

        if (Slot.SlotDataVersion < 2)
            AddLog("<color=#FA8072>This room was generated with an older Kingdom Two Crowns apworld. Regenerate it with the apworld that came with this mod.</color>", true);

        try
        {
            _deathLink = DeathLinkProvider.CreateDeathLinkService(session);
            _deathLink.OnDeathLinkReceived += dl => MainThread.Enqueue(() => ItemHandler.QueueDeathLink(dl.Source, dl.Cause));
            DeathLinkOn = false;
            if (Slot.DeathLink) SetDeathLink(true);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AP] DeathLink unavailable: {e.Message}");
        }

        try
        {
            session.Hints.TrackHints(hints => MainThread.Enqueue(() => Hints = hints ?? Array.Empty<APHint>()), true);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Could not track hints: {e.Message}");
        }

        try { session.SetClientState(ArchipelagoClientState.ClientPlaying); }
        catch { /* not important */ }

        SetStatus($"Connected as {SlotName}.", false);
        Feed.Add($"<color=#00FF7F>Connected to Archipelago as {Escape(SlotName)}</color>");
        Plugin.Log.LogInfo($"[AP] Connected as {SlotName} (slot {SlotId}, team {Team}), seed {Seed}");

        FlushPending();
        OnConnected?.Invoke();
    }

    private static void OnSocketClosed(ArchipelagoSession session, string reason)
    {
        if (session != Session) return;
        var wasLoggedIn = _loggedIn;
        _loggedIn = false;
        Session = null;
        _deathLink = null;
        SetStatus($"Connection lost{(string.IsNullOrEmpty(reason) ? "" : ": " + reason)}", true);
        if (wasLoggedIn) Feed.Add("<color=#FA8072>Lost connection to Archipelago. Checks are saved and will be sent when you reconnect.</color>");
        State.Save();
    }

    // Runs on the socket thread: just tag each item with its position in the server's list.
    private static void OnItemReceived(ArchipelagoSession session, ReceivedItemsHelper helper, ref int counter)
    {
        while (helper.Any())
        {
            var item = helper.DequeueItem();
            var received = new ReceivedItem
            {
                Index = counter++,
                Name = item.ItemName ?? $"Unknown item {item.ItemId}",
                Sender = item.Player?.Name ?? "Server",
                Location = item.LocationDisplayName ?? "",
                Flags = item.Flags,
            };
            MainThread.Enqueue(() =>
            {
                if (Session == session) Received.Add(received);
            });
        }
    }

    private static void OnMessage(LogMessage message)
    {
        var text = ToRichText(message);
        bool related = false;
        bool showInFeed = false;

        switch (message)
        {
            case ItemSendLogMessage itemSend:
                related = itemSend.IsRelatedToActivePlayer;
                showInFeed = related || !ClientSettings.Current.FeedOnlyMine;
                if (message is HintItemSendLogMessage) showInFeed = related;
                break;
            case CommandResultLogMessage:
            case AdminCommandResultLogMessage:
                LastServerReply = text;
                related = true;
                showInFeed = true;
                break;
            case CountdownLogMessage:
            case GoalLogMessage:
                showInFeed = true;
                break;
            case ChatLogMessage:
            case ServerChatLogMessage:
                showInFeed = !ClientSettings.Current.FeedOnlyMine;
                break;
        }

        AddLog(text, related);
        if (showInFeed) Feed.Add(text);
    }

    public static void AddLog(string richText, bool relatedToMe)
    {
        Log.Add(new LogLine { Text = richText, RelatedToMe = relatedToMe, Time = UnityEngine.Time.unscaledTime });
        if (Log.Count > MaxLogLines) Log.RemoveRange(0, Log.Count - MaxLogLines);
        LogVersion++;
    }

    private static string ToRichText(LogMessage message)
    {
        var sb = new StringBuilder();
        foreach (var part in message.Parts)
        {
            sb.Append("<color=").Append(Readable(part.Color)).Append('>');
            sb.Append(Escape(part.Text));
            sb.Append("</color>");
        }
        return sb.ToString();
    }

    // The library's palette has a few colours (location green, useful blue) that vanish on a dark
    // background; swap those for the brighter ones Archipelago's own text client uses.
    private static string Readable(Color c)
    {
        var hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        return hex switch
        {
            "#008000" => Styles.HexLocation,
            "#0000FF" => Styles.HexUseful,
            "#6A5ACD" => Styles.HexUseful,
            "#000000" => "#FFFFFF",
            _ => hex,
        };
    }

    // IMGUI rich text has no escaping, so neutralise anything that looks like a tag.
    public static string Escape(string text) => (text ?? "").Replace("<", "‹").Replace(">", "›");

    private static void SetStatus(string status, bool error)
    {
        Status = status;
        StatusIsError = error;
        if (error) Plugin.Log.LogWarning($"[AP] {status}");
    }

    // --- Locations ---

    public static long LocationId(string name)
    {
        try { return Session?.Locations.GetLocationIdFromName(GameData.GameName, name) ?? -1; }
        catch { return -1; }
    }

    // False when the location isn't part of this seed (e.g. a milestone beyond the configured count).
    public static bool IsLocationInSeed(string name)
    {
        var session = Session;
        if (session == null) return true;
        var id = LocationId(name);
        return id >= 0 && session.Locations.AllLocations.Contains(id);
    }

    public static bool IsChecked(string name)
    {
        if (State.PendingLocations.Contains(name)) return true;
        var session = Session;
        if (session == null) return false;
        var id = LocationId(name);
        return id >= 0 && (SentThisSession.Contains(id) || session.Locations.AllLocationsChecked.Contains(id));
    }

    public static void SendLocation(string name)
    {
        if (!Connected)
        {
            if (!State.PendingLocations.Contains(name))
            {
                State.PendingLocations.Add(name);
                State.Dirty = true;
                Feed.Add($"<color=#AAAAAA>Check saved for later: {Escape(name)}</color>");
            }
            return;
        }

        var id = LocationId(name);
        if (id < 0)
        {
            Plugin.Log.LogWarning($"[AP] Unknown location name '{name}' (apworld/mod mismatch?)");
            return;
        }
        if (!Session.Locations.AllLocations.Contains(id)) return; // not in this seed
        if (!SentThisSession.Add(id)) return;
        if (Session.Locations.AllLocationsChecked.Contains(id)) return;

        Plugin.Log.LogInfo($"[AP] Check: {name} ({id})");
        var locations = Session.Locations;
        Task.Run(() => CompleteCheck(locations, id, name));
    }

    // Kept out of lambdas: lambdas touching MultiClient's Task-returning calls trip a compiler error
    // (CS0656, NullableAttribute) with this mix of interop references.
    private static void CompleteCheck(ILocationCheckHelper locations, long id, string name)
    {
        try { locations.CompleteLocationChecks(id); }
        catch (Exception e) { Plugin.Log.LogError($"[AP] Failed to send {name}: {e.GetBaseException().Message}"); }
    }

    private static void CloseSocket(IArchipelagoSocketHelper socket)
    {
        try { socket.DisconnectAsync().Wait(3000); }
        catch { /* already closed */ }
    }

    private static void FlushPending()
    {
        if (State.PendingLocations.Count == 0) return;
        var pending = State.PendingLocations.ToList();
        State.PendingLocations.Clear();
        State.Dirty = true;
        foreach (var name in pending) SendLocation(name);
        Feed.Add($"Sent {pending.Count} check(s) saved while offline.");
    }

    public static int CheckedCount => Session?.Locations.AllLocationsChecked.Count ?? 0;
    public static int LocationCount => Session?.Locations.AllLocations.Count ?? 0;

    public static IEnumerable<string> MissingLocationNames()
    {
        var session = Session;
        if (session == null) yield break;
        foreach (var id in session.Locations.AllMissingLocations)
        {
            if (SentThisSession.Contains(id)) continue;
            string name = null;
            try { name = session.Locations.GetLocationNameFromId(id, GameData.GameName); }
            catch { }
            yield return name ?? $"Location {id}";
        }
    }

    public static void ReportGoal()
    {
        if (_goalSent || !Connected) return;
        _goalSent = true;
        try
        {
            Session.SetGoalAchieved();
            Feed.Add("<color=#FFD700>Goal complete! The Greed have been driven back.</color>");
        }
        catch (Exception e)
        {
            _goalSent = false;
            Plugin.Log.LogError($"[AP] Could not send goal: {e.Message}");
        }
    }

    // --- Chat, hints, DeathLink ---

    public static void Say(string text)
    {
        if (!Connected || string.IsNullOrWhiteSpace(text)) return;
        try { Session.Say(text.Trim()); }
        catch (Exception e) { AddLog($"<color=#FA8072>Could not send: {Escape(e.Message)}</color>", true); }
    }

    // Starts from the slot's death_link option; the menu can flip it for this session.
    public static bool DeathLinkOn { get; private set; }

    public static void SetDeathLink(bool enabled)
    {
        if (_deathLink == null) return;
        try
        {
            if (enabled) _deathLink.EnableDeathLink();
            else _deathLink.DisableDeathLink();
            DeathLinkOn = enabled;
        }
        catch (Exception e) { Plugin.Log.LogWarning($"[AP] DeathLink toggle failed: {e.Message}"); }
    }

    public static void SendDeathLink(string cause)
    {
        if (!Connected || !DeathLinkOn) return;
        try
        {
            _deathLink.SendDeathLink(new DeathLink(SlotName, cause));
            Plugin.Log.LogInfo($"[AP] DeathLink sent: {cause}");
        }
        catch (Exception e) { Plugin.Log.LogWarning($"[AP] DeathLink send failed: {e.Message}"); }
    }

    public static string PlayerName(int slot)
    {
        try { return Session?.Players.GetPlayerAlias(slot) ?? $"Player {slot}"; }
        catch { return $"Player {slot}"; }
    }

    public static string PlayerGame(int slot)
    {
        try { return Session?.Players.GetPlayerInfo(slot)?.Game ?? ""; }
        catch { return ""; }
    }

    public static string ItemName(long id, int receivingSlot)
    {
        try { return Session?.Items.GetItemName(id, PlayerGame(receivingSlot)) ?? $"Item {id}"; }
        catch { return $"Item {id}"; }
    }

    public static string LocationName(long id, int findingSlot)
    {
        try { return Session?.Locations.GetLocationNameFromId(id, PlayerGame(findingSlot)) ?? $"Location {id}"; }
        catch { return $"Location {id}"; }
    }

    public static int HintPoints => Session?.RoomState.HintPoints ?? 0;

    public static int HintCost
    {
        get
        {
            try { return Session?.RoomState.HintCost ?? 0; }
            catch { return 0; }
        }
    }
}
