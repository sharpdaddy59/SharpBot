using System.Text.Json;
using System.Text.Json.Nodes;

namespace SharpBot.Config;

public sealed class ConfigWriter
{
    private readonly string _path;
    private readonly Lock _lock = new();

    public ConfigWriter()
    {
        _path = System.IO.Path.Combine(AppContext.BaseDirectory, "data", "user-config.json");
    }

    public string FilePath => _path;

    public void SetModelPath(string modelPath) =>
        Update(root => EnsureSection(root, "SharpBot", "Llm")["ModelPath"] = modelPath);

    public void SetAllowedUserIds(IEnumerable<long> ids)
    {
        Update(root =>
        {
            var array = new JsonArray();
            foreach (var id in ids) array.Add(id);
            EnsureSection(root, "SharpBot", "Telegram")["AllowedUserIds"] = array;
        });
    }

    private void Update(Action<JsonObject> mutate)
    {
        lock (_lock)
        {
            var dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            JsonObject root;
            if (File.Exists(_path))
            {
                var existing = File.ReadAllText(_path);
                root = string.IsNullOrWhiteSpace(existing)
                    ? new JsonObject()
                    : (JsonObject)JsonNode.Parse(existing)!;
            }
            else
            {
                root = new JsonObject();
            }

            mutate(root);
            File.WriteAllText(_path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static JsonObject EnsureSection(JsonObject root, params string[] path)
    {
        var current = root;
        foreach (var key in path)
        {
            if (current[key] is not JsonObject next)
            {
                next = new JsonObject();
                current[key] = next;
            }
            current = next;
        }
        return current;
    }
}
