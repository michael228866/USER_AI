using HomunculusManager.Services;

namespace HomunculusManager.Models;

internal sealed class EditorSession
{
    private readonly string path;
    private ConfigDocument document;
    private Dictionary<string, int> baseline = [];
    private Dictionary<string, int> values = [];

    internal EditorSession(string path)
    {
        this.path = path;
        document = LuaConfigService.Open(path);
        Type = SettingsCatalog.Types[0].Key;
        Reload();
    }

    internal string Type { get; private set; }
    internal ConfigDocument Document => document;
    internal IReadOnlyDictionary<string, int> Values => values;
    internal int ChangedCount => values.Count(item => item.Value != baseline[item.Key]);
    internal bool IsDirty => ChangedCount > 0;
    internal bool IsChanged(string key) => values[key] != baseline[key];

    internal void Set(string key, int value)
    {
        var field = SettingsCatalog.Find(key);
        if (value < field.Min || value > field.Max)
            throw new ArgumentOutOfRangeException(key, $"允許範圍 {field.Min}–{field.Max}");
        values[key] = value;
    }

    internal bool Switch(string type, Func<bool> confirmDiscard)
    {
        if (!SettingsCatalog.Types.Any(x => x.Key == type)) throw new ArgumentException("未知生命體", nameof(type));
        if (type == Type) return true;
        if (IsDirty && !confirmDiscard()) return false;
        var nextDocument = LuaConfigService.Open(path);
        var nextValues = LuaConfigService.Read(nextDocument, type);
        Type = type;
        document = nextDocument;
        baseline = nextValues;
        values = new Dictionary<string, int>(nextValues);
        return true;
    }

    internal void Reset() => values = new Dictionary<string, int>(baseline);

    internal void ReloadFromDisk()
    {
        document = LuaConfigService.Open(path);
        Reload();
    }

    internal bool ApplyProfile(IReadOnlyDictionary<string, int> patch, Func<bool> confirmDiscard)
    {
        if (IsDirty && !confirmDiscard()) return false;
        var next = new Dictionary<string, int>(baseline);
        foreach (var (key, value) in patch)
        {
            var field = SettingsCatalog.Find(key);
            if (value < field.Min || value > field.Max) throw new ArgumentOutOfRangeException(key);
            if (!baseline.ContainsKey(key)) continue;
            next[key] = value;
        }
        values = next;
        return true;
    }

    internal void Save()
    {
        var changes = values.Where(x => x.Value != baseline[x.Key]).ToDictionary(x => x.Key, x => x.Value);
        document = LuaConfigService.Save(path, document, Type, changes);
        Reload();
    }

    private void Reload()
    {
        baseline = LuaConfigService.Read(document, Type);
        values = new Dictionary<string, int>(baseline);
    }
}
