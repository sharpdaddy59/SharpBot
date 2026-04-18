namespace SharpBot.Config;

public sealed class SharpBotOptions
{
    public const string SectionName = "SharpBot";

    public string DataDirectory { get; set; } = "data";
    public LlmOptions Llm { get; set; } = new();
    public TelegramOptions Telegram { get; set; } = new();
    public McpOptions Mcp { get; set; } = new();
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
