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

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging(b => b.AddSerilog(dispose: true));
    services.AddSharpBot(configuration);

    var registrar = new TypeRegistrar(services);
    var app = new CommandApp<RunCommand>(registrar);
    app.Configure(config =>
    {
        config.SetApplicationName("sharpbot");
        config.SetApplicationVersion("0.1.0-dev");

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
