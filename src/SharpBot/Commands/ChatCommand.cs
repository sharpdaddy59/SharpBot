using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;
using SharpBot.Llm;
using SharpBot.Tools;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class ChatCommand : AsyncCommand<ChatCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--no-tools")]
        public bool NoTools { get; set; }
    }

    private readonly ILlmClient _llm;
    private readonly IToolHost _toolHost;
    private readonly SharpBotOptions _options;

    public ChatCommand(ILlmClient llm, IToolHost toolHost, IOptions<SharpBotOptions> options)
    {
        _llm = llm;
        _toolHost = toolHost;
        _options = options.Value;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold green]SharpBot chat[/]").LeftJustified());
        AnsiConsole.MarkupLine($"Model: [grey]{_options.Llm.ModelPath}[/]");
        AnsiConsole.MarkupLine("Type a message and press Enter. Empty line or Ctrl+C to exit.");
        AnsiConsole.WriteLine();

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Loading model + tools (first call takes a few seconds)...", async _ =>
                {
                    await _llm.InitializeAsync(cancellationToken).ConfigureAwait(false);
                    await _toolHost.InitializeAsync(cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("Run [green]sharpbot models download[/] first, or check [green]sharpbot doctor[/].");
            return 1;
        }

        var toolsForLlm = settings.NoTools
            ? Array.Empty<ToolDescriptor>()
            : _toolHost.AvailableTools;

        AnsiConsole.MarkupLine(toolsForLlm.Count > 0
            ? $"[green]Ready.[/] [grey]{toolsForLlm.Count} tool(s) available to the model.[/]"
            : "[green]Ready.[/] [grey](no tools — pure chat mode)[/]");
        AnsiConsole.WriteLine();

        var convo = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(_options.Llm.SystemPrompt))
        {
            convo.Add(new ChatMessage(ChatRole.System, _options.Llm.SystemPrompt));
        }

        const int maxToolIterations = 8;

        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.Markup("[blue]you[/] > ");
            string? input;
            try { input = Console.ReadLine(); }
            catch (OperationCanceledException) { break; }

            if (input is null) break;
            if (string.IsNullOrWhiteSpace(input)) break;

            convo.Add(new ChatMessage(ChatRole.User, input));

            var finished = false;
            for (var iteration = 0; iteration < maxToolIterations && !finished; iteration++)
            {
                LlmResponse response;
                try
                {
                    response = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .StartAsync(iteration == 0 ? "thinking..." : $"thinking (iteration {iteration + 1})...", async _ =>
                            await _llm.InferAsync(convo, toolsForLlm, cancellationToken).ConfigureAwait(false))
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return 0; }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Inference failed:[/] {Markup.Escape(ex.Message)}");
                    // Drop the unanswered user message so the next turn starts clean.
                    convo.RemoveAt(convo.Count - 1);
                    break;
                }

                var text = response.Text ?? string.Empty;
                convo.Add(new ChatMessage(ChatRole.Assistant, text, response.ToolCalls));

                if (!response.HasToolCalls)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        AnsiConsole.Markup("[green]bot[/] > ");
                        AnsiConsole.WriteLine(text);
                        AnsiConsole.WriteLine();
                    }
                    finished = true;
                    continue;
                }

                foreach (var call in response.ToolCalls)
                {
                    AnsiConsole.MarkupLine($"[grey]→ {Markup.Escape(call.Name)}({Markup.Escape(Truncate(call.ArgumentsJson, 80))})[/]");
                    string result;
                    try
                    {
                        result = await _toolHost.ExecuteAsync(call, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result = $"Error: {ex.Message}";
                    }
                    AnsiConsole.MarkupLine($"[grey]← {Markup.Escape(Truncate(result, 120))}[/]");
                    convo.Add(new ChatMessage(ChatRole.Tool, result, ToolCallId: call.Id));
                }
            }

            if (!finished)
            {
                AnsiConsole.MarkupLine("[yellow](stopped — tool iteration limit reached)[/]");
            }
        }

        return 0;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
