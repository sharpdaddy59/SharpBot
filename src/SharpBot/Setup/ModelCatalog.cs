namespace SharpBot.Setup;

public sealed record CuratedModel(
    string DisplayName,
    string HuggingFaceRepo,
    string Filename,
    long SizeBytes,
    int RecommendedRamGb,
    bool IsGated,
    string Notes)
{
    public string SizeDisplay => SizeBytes switch
    {
        >= 1_000_000_000 => $"{SizeBytes / 1_000_000_000.0:F1} GB",
        >= 1_000_000 => $"{SizeBytes / 1_000_000.0:F0} MB",
        _ => $"{SizeBytes} B",
    };
}

public static class ModelCatalog
{
    // Order: open models first so zero-token users see them at the top of the picker.
    public static readonly IReadOnlyList<CuratedModel> Curated = new[]
    {
        new CuratedModel(
            "Qwen 2.5 3B Instruct (Q4_K_M)",
            "bartowski/Qwen2.5-3B-Instruct-GGUF",
            "Qwen2.5-3B-Instruct-Q4_K_M.gguf",
            2_100_000_000L,
            6,
            IsGated: false,
            "Recommended default. Open license, solid tool calling."),
        new CuratedModel(
            "Qwen 2.5 7B Instruct (Q4_K_M)",
            "bartowski/Qwen2.5-7B-Instruct-GGUF",
            "Qwen2.5-7B-Instruct-Q4_K_M.gguf",
            4_700_000_000L,
            12,
            IsGated: false,
            "Better reasoning; slower on CPU. Open license."),
        new CuratedModel(
            "Qwen 2.5 1.5B Instruct (Q4_K_M)",
            "bartowski/Qwen2.5-1.5B-Instruct-GGUF",
            "Qwen2.5-1.5B-Instruct-Q4_K_M.gguf",
            1_100_000_000L,
            4,
            IsGated: false,
            "Tiny, fast. Fine for simple intent routing. Open license."),
        new CuratedModel(
            "Gemma 3 4B Instruct (Q4_K_M)",
            "bartowski/google_gemma-3-4b-it-GGUF",
            "google_gemma-3-4b-it-Q4_K_M.gguf",
            2_500_000_000L,
            8,
            IsGated: true,
            "Strong all-rounder. Requires accepting Google's license on HF + token."),
        new CuratedModel(
            "Gemma 3 1B Instruct (Q4_K_M)",
            "bartowski/google_gemma-3-1b-it-GGUF",
            "google_gemma-3-1b-it-Q4_K_M.gguf",
            800_000_000L,
            4,
            IsGated: true,
            "Tiny Gemma. Requires HF token."),
        new CuratedModel(
            "Llama 3.2 3B Instruct (Q4_K_M)",
            "bartowski/Llama-3.2-3B-Instruct-GGUF",
            "Llama-3.2-3B-Instruct-Q4_K_M.gguf",
            2_000_000_000L,
            6,
            IsGated: true,
            "Meta license — requires HF token."),
    };
}
