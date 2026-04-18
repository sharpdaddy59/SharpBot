namespace SharpBot.Config;

public sealed class SharpBotOptions
{
    public const string SectionName = "SharpBot";

    public string DataDirectory { get; set; } = "data";
    public LlmOptions Llm { get; set; } = new();
    public TelegramOptions Telegram { get; set; } = new();
    public McpOptions Mcp { get; set; } = new();
    public BuiltInToolsOptions BuiltInTools { get; set; } = new();
}

public sealed class BuiltInToolsOptions
{
    /// <summary>Path used as the sandbox root for file-system built-ins (read_file, list_files).</summary>
    public string WorkspaceDirectory { get; set; } = "workspace";

    /// <summary>Max bytes of response body the fetch_url tool will return. Responses beyond are truncated.</summary>
    public int FetchMaxBytes { get; set; } = 100_000;

    /// <summary>Timeout in seconds for the fetch_url tool.</summary>
    public int FetchTimeoutSeconds { get; set; } = 20;
}

public sealed class LlmOptions
{
    public string ModelPath { get; set; } = "";
    public int ContextSize { get; set; } = 4096;
    public int GpuLayerCount { get; set; }
    public string SystemPrompt { get; set; } = "";
}

public sealed class TelegramOptions
{
    public List<long> AllowedUserIds { get; set; } = new();
    public int PollingTimeoutSeconds { get; set; } = 30;
}

public sealed class McpOptions
{
    public List<McpServerConfig> Servers { get; set; } = new();
}

public sealed class McpServerConfig
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public List<string> Args { get; set; } = new();
    public Dictionary<string, string> Env { get; set; } = new();
}
