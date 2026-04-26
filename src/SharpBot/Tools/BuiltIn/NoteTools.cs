using System.Text;
using System.Text.Json;

namespace SharpBot.Tools.BuiltIn;

public sealed class SaveNoteTool : IBuiltInTool
{
    private readonly NoteStore _store;
    public SaveNoteTool(NoteStore store) => _store = store;

    public string Name => "save_note";
    public string Description =>
        "Save a short named note to persistent memory. Use this when the user asks you to remember " +
        "something. Keys should be short labels like 'favorite_color' or 'project_x_status'. " +
        "Overwrites any existing note with the same key.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "key":   { "type": "string", "description": "Short identifier, e.g. 'favorite_color'." },
            "value": { "type": "string", "description": "The content to remember." }
          },
          "required": ["key", "value"]
        }
        """;

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryGetString(arguments, "key", out var key))
            return Task.FromResult("Error: missing required 'key' string argument.");
        if (!TryGetString(arguments, "value", out var value))
            return Task.FromResult("Error: missing required 'value' string argument.");

        _store.Save(key!, value!);
        return Task.FromResult($"Saved note '{key}'.");
    }

    private static bool TryGetString(JsonElement obj, string name, out string? value)
    {
        value = null;
        if (obj.ValueKind != JsonValueKind.Object) return false;
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String) return false;
        value = el.GetString();
        return !string.IsNullOrEmpty(value);
    }
}

public sealed class RecallNoteTool : IBuiltInTool
{
    private readonly NoteStore _store;
    public RecallNoteTool(NoteStore store) => _store = store;

    public string Name => "recall_note";
    public string Description =>
        "Retrieve a note the user previously asked you to save with save_note. " +
        "Use ONLY when the user explicitly references something they told you to remember earlier — " +
        "do NOT use for general knowledge questions, fun facts, or anything you can answer from your own training. " +
        "If the user asks about saved notes but you don't know the key, call list_notes first.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "key": { "type": "string", "description": "Key of the note to retrieve." }
          },
          "required": ["key"]
        }
        """;

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty("key", out var keyEl) ||
            keyEl.ValueKind != JsonValueKind.String)
        {
            return Task.FromResult("Error: missing required 'key' string argument.");
        }

        var key = keyEl.GetString()!;
        var value = _store.Recall(key);
        return Task.FromResult(value is null
            ? $"No note found for key '{key}'."
            : value);
    }
}

public sealed class ListNotesTool : IBuiltInTool
{
    private readonly NoteStore _store;
    public ListNotesTool(NoteStore store) => _store = store;

    public string Name => "list_notes";
    public string Description =>
        "List every saved note (key + short preview of the value). Use this to discover what " +
        "the user has previously asked you to remember.";

    public string ParametersJsonSchema => """
        { "type": "object", "properties": {} }
        """;

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var notes = _store.List();
        if (notes.Count == 0) return Task.FromResult("(no notes saved)");

        var sb = new StringBuilder();
        foreach (var note in notes)
        {
            var preview = note.Value.Length > 60
                ? note.Value[..60].Replace('\n', ' ') + "..."
                : note.Value.Replace('\n', ' ');
            sb.Append(note.Key).Append(": ").AppendLine(preview);
        }
        return Task.FromResult(sb.ToString().TrimEnd());
    }
}

public sealed class DeleteNoteTool : IBuiltInTool
{
    private readonly NoteStore _store;
    public DeleteNoteTool(NoteStore store) => _store = store;

    public string Name => "delete_note";
    public string Description =>
        "Delete a previously saved note. Use this when the user asks you to forget something.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "key": { "type": "string", "description": "Key of the note to delete." }
          },
          "required": ["key"]
        }
        """;

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty("key", out var keyEl) ||
            keyEl.ValueKind != JsonValueKind.String)
        {
            return Task.FromResult("Error: missing required 'key' string argument.");
        }

        var key = keyEl.GetString()!;
        var existed = _store.Delete(key);
        return Task.FromResult(existed
            ? $"Deleted note '{key}'."
            : $"No note found for key '{key}' (nothing to delete).");
    }
}
