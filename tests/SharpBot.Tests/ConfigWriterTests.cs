using System.Text.Json;
using System.Text.Json.Nodes;
using SharpBot.Config;

namespace SharpBot.Tests;

public class ConfigWriterTests : IDisposable
{
    private readonly string _path;

    public ConfigWriterTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"sharpbot-cfg-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void SetModelPath_creates_file_and_nested_section()
    {
        var writer = new ConfigWriter(_path);
        writer.SetModelPath("models/qwen.gguf");

        Assert.True(File.Exists(_path));

        var root = JsonNode.Parse(File.ReadAllText(_path))!.AsObject();
        Assert.Equal("models/qwen.gguf", root["SharpBot"]!["Llm"]!["ModelPath"]!.GetValue<string>());
    }

    [Fact]
    public void SetModelPath_preserves_other_keys()
    {
        // Pre-existing user config with unrelated data
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, """
            {
              "SharpBot": {
                "Llm": { "ContextSize": 16384 },
                "CustomUserKey": "keep me"
              },
              "TopLevelCustom": 42
            }
            """);

        var writer = new ConfigWriter(_path);
        writer.SetModelPath("models/new.gguf");

        var root = JsonNode.Parse(File.ReadAllText(_path))!.AsObject();
        Assert.Equal("models/new.gguf", root["SharpBot"]!["Llm"]!["ModelPath"]!.GetValue<string>());
        Assert.Equal(16384, root["SharpBot"]!["Llm"]!["ContextSize"]!.GetValue<int>());
        Assert.Equal("keep me", root["SharpBot"]!["CustomUserKey"]!.GetValue<string>());
        Assert.Equal(42, root["TopLevelCustom"]!.GetValue<int>());
    }

    [Fact]
    public void SetAllowedUserIds_writes_array()
    {
        var writer = new ConfigWriter(_path);
        writer.SetAllowedUserIds(new long[] { 111, 222, 333 });

        var root = JsonNode.Parse(File.ReadAllText(_path))!.AsObject();
        var arr = root["SharpBot"]!["Telegram"]!["AllowedUserIds"]!.AsArray();
        Assert.Equal(3, arr.Count);
        Assert.Equal(111, arr[0]!.GetValue<long>());
        Assert.Equal(222, arr[1]!.GetValue<long>());
        Assert.Equal(333, arr[2]!.GetValue<long>());
    }

    [Fact]
    public void Sequential_writes_preserve_each_other()
    {
        var writer = new ConfigWriter(_path);
        writer.SetModelPath("models/first.gguf");
        writer.SetAllowedUserIds(new long[] { 42 });
        writer.SetModelPath("models/second.gguf");

        var root = JsonNode.Parse(File.ReadAllText(_path))!.AsObject();
        Assert.Equal("models/second.gguf", root["SharpBot"]!["Llm"]!["ModelPath"]!.GetValue<string>());
        Assert.Single(root["SharpBot"]!["Telegram"]!["AllowedUserIds"]!.AsArray());
    }

    [Fact]
    public void FilePath_returns_configured_path()
    {
        var writer = new ConfigWriter(_path);
        Assert.Equal(_path, writer.FilePath);
    }

    [Fact]
    public void Output_is_valid_json()
    {
        var writer = new ConfigWriter(_path);
        writer.SetModelPath("x.gguf");
        writer.SetAllowedUserIds(new long[] { 1, 2 });

        // Must be re-parseable — i.e. not a JSON-fragment / invalid output.
        var text = File.ReadAllText(_path);
        var doc = JsonDocument.Parse(text);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }
}
