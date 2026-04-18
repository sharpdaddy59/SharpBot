using System.Text.Json;
using Microsoft.Extensions.Options;
using SharpBot.Config;

namespace SharpBot.Tools.BuiltIn;

/// <summary>
/// Simple persistent key-value store backing the save_note/recall_note/list_notes/delete_note
/// built-in tools. State lives in {DataDirectory}/notes.json. Serialized access via a lock —
/// writes are not expected to be high-rate and the file stays small.
/// </summary>
public sealed class NoteStore
{
    private readonly string _path;
    private readonly Lock _lock = new();
    private Dictionary<string, string> _notes;

    public NoteStore(IOptions<SharpBotOptions> options)
    {
        var dir = options.Value.DataDirectory;
        _path = Path.Combine(dir, "notes.json");
        _notes = Load(_path);
    }

    // Internal ctor for tests.
    internal NoteStore(string path)
    {
        _path = path;
        _notes = Load(path);
    }

    public string FilePath => _path;

    public void Save(string key, string value)
    {
        lock (_lock)
        {
            _notes[key] = value;
            Persist();
        }
    }

    public string? Recall(string key)
    {
        lock (_lock)
        {
            return _notes.GetValueOrDefault(key);
        }
    }

    public IReadOnlyList<KeyValuePair<string, string>> List()
    {
        lock (_lock)
        {
            return _notes
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public bool Delete(string key)
    {
        lock (_lock)
        {
            if (!_notes.Remove(key)) return false;
            Persist();
            return true;
        }
    }

    private static Dictionary<string, string> Load(string path)
    {
        if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.Ordinal);
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>(StringComparer.Ordinal);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private void Persist()
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(_notes, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
