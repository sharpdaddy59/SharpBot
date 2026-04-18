using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;
using SharpBot.Llm;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class ChatCommand : AsyncCommand<ChatCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly ILlmClient _llm;
    private readonly SharpBotOptions _options;

    public ChatCommand(ILlmClient llm, IOptions<SharpBotOptions> options)
    {
        _llm = llm;
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
                .StartAsync("Loading model (first call takes a few seconds)...", async _ =>
                {
                    await _llm.InitializeAsync(cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("Run [green]sharpbot models download[/] first, or check [green]sharpbot doctor[/].");
            return 1;
        }

        AnsiConsole.MarkupLine("[green]Ready.[/]");
        AnsiConsole.WriteLine();

        var convo = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(_options.Llm.SystemPrompt))
        {
            convo.Add(new ChatMessage(ChatRole.System, _options.Llm.SystemPrompt));
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            string input;
            try
            {
                AnsiConsole.Markup("[blue]you[/] > ");
                var line = Console.ReadLine();
                if (line is null) break; // stdin closed
                input = line;
            }
            catch (OperationCanceledException) { break; }

            if (string.IsNullOrWhiteSpace(input)) break;

            convo.Add(new ChatMessage(ChatRole.User, input));

            LlmResponse response;
            try
            {
                response = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("thinking...", async _ =>
                        await _llm.InferAsync(convo, Array.Empty<ToolDescriptor>(), cancellationToken).ConfigureAwait(false))
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Inference failed:[/] {Markup.Escape(ex.Message)}");
                convo.RemoveAt(convo.Count - 1); // drop the unanswered user message
                continue;
            }

            var text = response.Text ?? string.Empty;
            AnsiConsole.Markup("[green]bot[/] > ");
            AnsiConsole.WriteLine(text);
            AnsiConsole.WriteLine();

            convo.Add(new ChatMessage(ChatRole.Assistant, text, response.ToolCalls));
        }

        return 0;
    }
}
