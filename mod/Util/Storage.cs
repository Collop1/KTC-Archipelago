using System;
using System.IO;
using System.Text.Json;
using BepInEx;

namespace APKingdom2Crown;

// Small JSON files under BepInEx/config/APKingdom2Crown. Written by the mod itself (menu settings,
// per-seed progress), never meant to be hand edited.
internal static class Storage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, IncludeFields = false };

    public static string Folder
    {
        get
        {
            var dir = Path.Combine(Paths.ConfigPath, "APKingdom2Crown");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static T Load<T>(string path) where T : new()
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? new T();
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Could not read {path}: {e.Message}");
        }
        return new T();
    }

    public static void Save<T>(string path, T value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(value, Options));
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[AP] Could not write {path}: {e.Message}");
        }
    }

    public static string SafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}

// Connection details and menu preferences, remembered between launches.
internal sealed class ClientSettings
{
    public string Host { get; set; } = "archipelago.gg:38281";
    public string Slot { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberPassword { get; set; } = true;
    public bool AutoConnect { get; set; }

    public float WindowScale { get; set; } = 1f;
    public float MenuOpacity { get; set; } = 0.95f;
    public float WindowX { get; set; } = 40f;
    public float WindowY { get; set; } = 90f;
    public float WindowWidth { get; set; } = 680f;
    public float WindowHeight { get; set; } = 520f;
    public int MenuKey { get; set; } = (int)UnityEngine.KeyCode.F2;

    public bool FeedEnabled { get; set; } = true;
    public int FeedCorner { get; set; } // 0 bottom right, 1 bottom left, 2 top right, 3 top left
    public float FeedDuration { get; set; } = 8f;
    public int FeedLines { get; set; } = 6;
    public float FeedScale { get; set; } = 1f;
    public bool FeedOnlyMine { get; set; } = true;

    private static string PathOnDisk => System.IO.Path.Combine(Storage.Folder, "client.json");

    public static ClientSettings Current { get; private set; } = new();

    public static void Load() => Current = Storage.Load<ClientSettings>(PathOnDisk);

    public static void Save()
    {
        var copy = (ClientSettings)Current.MemberwiseClone();
        if (!copy.RememberPassword) copy.Password = "";
        Storage.Save(PathOnDisk, copy);
    }
}
