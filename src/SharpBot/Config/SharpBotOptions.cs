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

    /// <summary>Max bytes of raw response body the fetch_url tool will consume. Responses beyond are truncated.</summary>
    public int FetchMaxBytes { get; set; } = 30_000;

    /// <summary>When true, HTML responses are stripped to visible text before being returned to the model.</summary>
    public bool FetchStripHtml { get; set; } = true;

    /// <summary>Timeout in seconds for the fetch_url tool.</summary>
    public int FetchTimeoutSeconds { get; set; } = 20;
}

public sealed class LlmOptions
{
    public string ModelPath { get; set; } = "";
    public int ContextSize { get; set; } = 8192;
    public int GpuLayerCount { get; set; }
    public string SystemPrompt { get; set; } = "";

    /// <summary>
    /// When true, routes llama.cpp's Info/Debug native logs through Serilog (very chatty).
    /// When false (default), only Warn/Error native logs surface — keeps startup output clean.
    /// </summary>
    public bool VerboseNativeLogs { get; set; }
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
