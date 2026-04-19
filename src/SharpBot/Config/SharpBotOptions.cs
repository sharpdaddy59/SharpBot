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

    /// <summary>
    /// Number of transformer layers to offload to GPU. 99 = "all of them" (llama.cpp clamps
    /// to the actual layer count). Has no effect unless the build was produced with a GPU
    /// backend (IncludeCuda=true etc.) — CPU-only builds silently ignore it.
    /// </summary>
    public int GpuLayerCount { get; set; } = 99;
    public string SystemPrompt { get; set; } = "";

    /// <summary>
    /// When true, routes llama.cpp's Info/Debug native logs through Serilog (very chatty).
    /// When false (default), only Warn/Error native logs surface — keeps startup output clean.
    /// </summary>
    public bool VerboseNativeLogs { get; set; }

    /// <summary>
    /// Maximum number of active per-conversation KV caches held in memory at once. When the
    /// limit is reached, the least-recently-used conversation is evicted. Each cache costs
    /// roughly 400–800 MB at 8K context for a 3B model — tune down on RAM-tight boxes.
    /// </summary>
    public int MaxActiveConversations { get; set; } = 4;

    // --- Sampling parameters --------------------------------------------------------------
    // These defaults are tuned against real-world use (ported from the Questora engine which
    // drives much smaller models reliably). The three repetition/frequency/presence penalties
    // are critical for small models (Gemma 1B, Phi-3) — without them the model gets stuck
    // emitting the same tool call or text over and over.

    /// <summary>Softmax temperature. Higher = more random. 0.7–0.8 is typical for chat.</summary>
    public float Temperature { get; set; } = 0.75f;

    /// <summary>Nucleus sampling cutoff. Keep the smallest set of tokens with cumulative probability above this.</summary>
    public float TopP { get; set; } = 0.92f;

    /// <summary>Top-k sampling cutoff. Only the K most likely tokens are considered.</summary>
    public int TopK { get; set; } = 40;

    /// <summary>Penalty applied to tokens that have recently appeared. Values above 1.0 discourage repetition; 1.1 is a gentle default that prevents loops without breaking structured output.</summary>
    public float RepeatPenalty { get; set; } = 1.1f;

    /// <summary>
    /// Scales down tokens proportional to how often they appeared. Combats loops in creative text,
    /// but penalizes characters like '"' which recur in structured JSON tool calls — default 0 so
    /// tool calls stay well-formed. Bump to 0.3–0.5 for creative chat without tool use.
    /// </summary>
    public float FrequencyPenalty { get; set; }

    /// <summary>
    /// Scales down tokens that have appeared at all. Encourages topic diversity in creative text,
    /// but breaks structured output for the same reason as FrequencyPenalty. Default 0.
    /// </summary>
    public float PresencePenalty { get; set; }

    /// <summary>Max tokens to generate per turn.</summary>
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// Do a brief warmup inference (2 × MaxTokens=1) the first time each conversation's
    /// executor starts. Some models — notably Gemma — produce empty output on their first
    /// one or two real inferences without this priming. Adds ~200 ms to the first turn of
    /// each new conversation; disable if you're sure your model doesn't need it.
    /// </summary>
    public bool WarmupOnFirstTurn { get; set; } = true;
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
