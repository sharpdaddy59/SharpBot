using System.Text.Json;
using Microsoft.Extensions.Options;
using SharpBot.Config;

namespace SharpBot.Secrets;

public sealed class FileSecretStore : ISecretStore
{
    private readonly string _path;
    private readonly Dictionary<string, string> _values;
    private readonly Lock _lock = new();

    public FileSecretStore(IOptions<SharpBotOptions> options)
    {
        var dataDir = options.Value.DataDirectory;
        Directory.CreateDirectory(dataDir);
        _path = Path.Combine(dataDir, "secrets.json");
        _values = Load(_path);
    }

    public string? Get(string key)
    {
        lock (_lock) return _values.TryGetValue(key, out var v) ? v : null;
    }

    public void Set(string key, string value)
    {
        lock (_lock) _values[key] = value;
    }

    public void Delete(string key)
    {
        lock (_lock) _values.Remove(key);
    }

    public void Save()
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(_values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
    }

    private static Dictionary<string, string> Load(string path)
    {
        if (!File.Exists(path)) return new Dictionary<string, string>();
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }
}

public static class SecretKeys
{
    public const string TelegramBotToken = "telegram.bot_token";
    public const string HuggingFaceToken = "huggingface.token";
}
