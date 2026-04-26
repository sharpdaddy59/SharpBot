using System.Reflection;
using LLama.Native;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SharpBot.Commands;
using SharpBot.Hosting;
using Spectre.Console.Cli;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Must run before any LLamaSharp activity. On Linux single-file deploys the
// native libs are extracted without their SONAME symlinks, so we create them.
NativeLibraryFixup.EnsureLinuxSonames();

try
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("SHARPBOT_ENV") ?? "Production"}.json",
            optional: true, reloadOnChange: true)
        .AddJsonFile("data/user-config.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables(prefix: "SHARPBOT_")
        .Build();

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(
                configuration["SharpBot:DataDirectory"] ?? "data",
                "logs",
                "sharpbot-.log"),
            rollingInterval: RollingInterval.Day)
        .CreateLogger();

    // Route llama.cpp's native stderr spam through Serilog so it obeys our log level config.
    // Info/Debug are dropped unless SharpBot:Llm:VerboseNativeLogs=true — keeps startup readable.
    var verboseNativeLogs = configuration.GetValue<bool>("SharpBot:Llm:VerboseNativeLogs");
    // Specific Warnings that fire on every inference but are purely informational — we know we're
    // running with a smaller context than the model was trained at, that's intentional. The
    // "No library was loaded" message fires dozens of times per session from LLamaSharp's own
    // safety instrumentation; benign in our case since the library IS loaded by the time we use it.
    var benignWarningSubstrings = new[]
    {
        "n_ctx_seq",
        "n_ctx_per_seq",
        "No library was loaded before calling native apis",
    };
    NativeLibraryConfig.All.WithLogCallback((level, message) =>
    {
        var text = message?.TrimEnd('\n', '\r', ' ');
        if (string.IsNullOrEmpty(text)) return;
        switch (level)
        {
            case LLamaLogLevel.Error: Log.Error("[llama] {Msg}", text); break;
            case LLamaLogLevel.Warning:
                if (benignWarningSubstrings.Any(s => text!.Contains(s, StringComparison.Ordinal))) break;
                Log.Warning("[llama] {Msg}", text);
                break;
            case LLamaLogLevel.Info:
                if (verboseNativeLogs) Log.Information("[llama] {Msg}", text);
                break;
            default:
                if (verboseNativeLogs) Log.Debug("[llama] {Msg}", text);
                break;
        }
    });

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging(b => b.AddSerilog(dispose: true));
    services.AddSharpBot(configuration);

    var registrar = new TypeRegistrar(services);
    var app = new CommandApp<RunCommand>(registrar);
    app.Configure(config =>
    {
        config.SetApplicationName("sharpbot");
        config.SetApplicationVersion(
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "0.0.0-dev");

        config.AddCommand<RunCommand>("run")
            .WithDescription("Run the bot (default when no command given).");

        config.AddCommand<ChatCommand>("chat")
            .WithDescription("Local chat REPL — talk to the model from the console (no Telegram needed).");

        config.AddCommand<SetupCommand>("setup")
            .WithDescription("Interactive first-run wizard — pick a model, paste a Telegram token, pair a user.");

        config.AddBranch("models", models =>
        {
            models.SetDescription("Manage local LLM model files.");
            models.AddCommand<ModelsListCommand>("list")
                .WithDescription("Show curated + installed GGUF models.");
            models.AddCommand<ModelsDownloadCommand>("download")
                .WithDescription("Download a GGUF model from Hugging Face.");
        });

        config.AddBranch("hf", hf =>
        {
            hf.SetDescription("Manage the HuggingFace token used for gated model downloads.");
            hf.AddCommand<HfLoginCommand>("login")
                .WithDescription("Save a HuggingFace read token.");
            hf.AddCommand<HfLogoutCommand>("logout")
                .WithDescription("Remove the saved HuggingFace token.");
            hf.AddCommand<HfStatusCommand>("status")
                .WithDescription("Show whether a token is saved.");
        });

        config.AddBranch("tools", tools =>
        {
            tools.SetDescription("List and debug all tools (built-in + MCP) available to the LLM.");
            tools.AddCommand<ToolsListCommand>("list")
                .WithDescription("List all tools from every host (built-in + MCP), marking their source.");
            tools.AddCommand<ToolsTestCommand>("test")
                .WithDescription("Invoke any tool by qualified name with JSON args.");
        });

        config.AddBranch("mcp", mcp =>
        {
            mcp.SetDescription("MCP-specific diagnostics (server connectivity, per-server tool listings).");
            mcp.AddCommand<McpListCommand>("list")
                .WithDescription("List configured MCP servers and their tools.");
            mcp.AddCommand<McpTestCommand>("test")
                .WithDescription("Invoke a single MCP tool by qualified name ('server.tool') with JSON args.");
        });

        config.AddBranch("tg", tg =>
        {
            tg.SetDescription("Manage the Telegram bot token.");
            tg.AddCommand<TgLoginCommand>("login")
                .WithDescription("Save a Telegram bot token from @BotFather.");
            tg.AddCommand<TgLogoutCommand>("logout")
                .WithDescription("Remove the saved Telegram bot token.");
            tg.AddCommand<TgStatusCommand>("status")
                .WithDescription("Show whether a Telegram token is saved and which users are paired.");
        });

        config.AddCommand<PairCommand>("pair")
            .WithDescription("Pair a Telegram user — first message to the bot wins.");

        config.AddCommand<DoctorCommand>("doctor")
            .WithDescription("Sanity-check config, model file, Telegram token, and MCP servers.");
    });

    return await app.RunAsync(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
