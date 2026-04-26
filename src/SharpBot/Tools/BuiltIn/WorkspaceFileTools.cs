using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SharpBot.Config;

namespace SharpBot.Tools.BuiltIn;

/// <summary>
/// Shared helpers for workspace-sandboxed file tools. Resolves paths relative to the configured
/// workspace directory and rejects anything that escapes it.
/// </summary>
internal static class WorkspaceSandbox
{
    public static string GetRoot(BuiltInToolsOptions options)
    {
        var root = Path.GetFullPath(options.WorkspaceDirectory);
        Directory.CreateDirectory(root); // ensure it exists so relative probes don't error
        return root;
    }

    public static bool TryResolve(string root, string relativePath, out string fullPath, out string? error)
    {
        fullPath = "";
        error = null;

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            fullPath = root;
            return true;
        }

        try
        {
            var combined = Path.IsPathFullyQualified(relativePath)
                ? relativePath
                : Path.Combine(root, relativePath);
            fullPath = Path.GetFullPath(combined);
        }
        catch (Exception ex)
        {
            error = $"Invalid path '{relativePath}': {ex.Message}";
            return false;
        }

        // Make sure the resolved path is inside the workspace root.
        var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
        {
            error = $"Path '{relativePath}' is outside the workspace directory.";
            return false;
        }

        return true;
    }
}

public sealed class ReadFileTool : IBuiltInTool
{
    private readonly BuiltInToolsOptions _options;

    public ReadFileTool(IOptions<SharpBotOptions> options) => _options = options.Value.BuiltInTools;

    public string Name => "read_file";
    public string Description =>
        "Read the contents of an existing text file inside the workspace directory. " +
        "Use ONLY when the user explicitly asks to read, open, or show a specific named file — " +
        "do NOT use to generate, write, or create new content (that's the model's own job, not a tool). " +
        "Paths are relative to the workspace root. Reads up to ~100 KB; larger files are truncated.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "path": {
              "type": "string",
              "description": "Relative path within the workspace directory (e.g. 'notes.txt', 'sub/dir/file.md')."
            }
          },
          "required": ["path"]
        }
        """;

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (arguments.ValueKind != JsonValueKind.Object ||
            !arguments.TryGetProperty("path", out var pathEl) ||
            pathEl.ValueKind != JsonValueKind.String)
        {
            return "Error: missing required 'path' string argument.";
        }

        var root = WorkspaceSandbox.GetRoot(_options);
        if (!WorkspaceSandbox.TryResolve(root, pathEl.GetString()!, out var full, out var err))
        {
            return "Error: " + err;
        }
        if (!File.Exists(full))
        {
            return $"Error: file not found: {Path.GetRelativePath(root, full)}";
        }

        const int limit = 100_000;
        await using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        var buffer = new byte[limit];
        var total = 0;
        int read;
        while (total < buffer.Length &&
               (read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
        }
        var truncated = stream.Position < stream.Length;

        var text = Encoding.UTF8.GetString(buffer, 0, total);
        return truncated ? text + "\n\n[...truncated at 100 KB]" : text;
    }
}

public sealed class ListFilesTool : IBuiltInTool
{
    private readonly BuiltInToolsOptions _options;

    public ListFilesTool(IOptions<SharpBotOptions> options) => _options = options.Value.BuiltInTools;

    public string Name => "list_files";
    public string Description =>
        "List files and subdirectories inside the workspace directory (non-recursive). " +
        "Pass an optional 'path' to list a subdirectory, or leave empty for the workspace root.";

    public string ParametersJsonSchema => """
        {
          "type": "object",
          "properties": {
            "path": {
              "type": "string",
              "description": "Optional relative subdirectory. Defaults to the workspace root."
            }
          }
        }
        """;

    public Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var relative = "";
        if (arguments.ValueKind == JsonValueKind.Object &&
            arguments.TryGetProperty("path", out var pathEl) &&
            pathEl.ValueKind == JsonValueKind.String)
        {
            relative = pathEl.GetString() ?? "";
        }

        var root = WorkspaceSandbox.GetRoot(_options);
        if (!WorkspaceSandbox.TryResolve(root, relative, out var full, out var err))
        {
            return Task.FromResult("Error: " + err);
        }
        if (!Directory.Exists(full))
        {
            return Task.FromResult($"Error: directory not found: {Path.GetRelativePath(root, full)}");
        }

        var sb = new StringBuilder();
        var entries = Directory.EnumerateFileSystemEntries(full).OrderBy(e => e, StringComparer.OrdinalIgnoreCase);
        var count = 0;
        foreach (var entry in entries)
        {
            var rel = Path.GetRelativePath(root, entry);
            var kind = Directory.Exists(entry) ? "dir " : "file";
            sb.AppendLine($"{kind}  {rel}");
            count++;
        }
        if (count == 0) sb.AppendLine("(empty)");
        return Task.FromResult(sb.ToString().TrimEnd());
    }
}
