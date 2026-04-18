using SharpBot.Config;
using SharpBot.Setup;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class SetupCommand : AsyncCommand<SetupCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly HuggingFaceClient _hf;
    private readonly ConfigWriter _configWriter;

    public SetupCommand(HuggingFaceClient hf, ConfigWriter configWriter)
    {
        _hf = hf;
        _configWriter = configWriter;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold green]SharpBot setup[/]").LeftJustified());
        AnsiConsole.MarkupLine("Welcome! This wizard gets you running in a few minutes. Ctrl+C to cancel.");
        AnsiConsole.WriteLine();

        var step1 = await Step1PickAndDownloadModelAsync(cancellationToken).ConfigureAwait(false);
        if (step1 != 0) return step1;

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Next steps[/]").LeftJustified());
        AnsiConsole.MarkupLine("[grey]Steps 2 and 3 (Telegram token + user pairing) are coming in a future build.[/]");
        AnsiConsole.MarkupLine("For now, run [green]sharpbot doctor[/] to see what's configured.");
        return 0;
    }

    private async Task<int> Step1PickAndDownloadModelAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold]Step 1/3 — Choose a local LLM[/]").LeftJustified());
        AnsiConsole.MarkupLine("The model runs [green]in-process[/] — no Ollama, no LM Studio, no external server.");
        AnsiConsole.WriteLine();

        var choices = ModelCatalog.Curated.ToList();
        var model = AnsiConsole.Prompt(
            new SelectionPrompt<CuratedModel>()
                .Title("Pick a model:")
                .UseConverter(m =>
                {
                    var gate = m.IsGated ? " [yellow](gated — needs hf login)[/]" : "";
                    return $"{m.DisplayName} [grey]({m.SizeDisplay}, ~{m.RecommendedRamGb} GB RAM)[/]{gate}\n   {m.Notes}";
                })
                .AddChoices(choices));

        if (model.IsGated && !_hf.HasToken)
        {
            AnsiConsole.MarkupLine("[yellow]This model is gated and you don't have a HuggingFace token saved.[/]");
            AnsiConsole.MarkupLine($"  1. Accept the license at [blue]https://huggingface.co/{model.HuggingFaceRepo}[/]");
            AnsiConsole.MarkupLine("  2. Create a [yellow]Read[/] token at [blue]https://huggingface.co/settings/tokens[/]");
            AnsiConsole.MarkupLine("  3. Run [green]sharpbot hf login[/], then re-run [green]sharpbot setup[/].");
            return 1;
        }

        var destination = Path.Combine("models", model.Filename);

        if (File.Exists(destination))
        {
            AnsiConsole.MarkupLine($"[green]Already installed:[/] {destination}");
            _configWriter.SetModelPath(destination);
            AnsiConsole.MarkupLine($"[grey]Config updated:[/] {_configWriter.FilePath}");
            return 0;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Downloading [green]{model.DisplayName}[/] from [grey]{model.HuggingFaceRepo}[/]");
        AnsiConsole.MarkupLine($"Destination: [grey]{destination}[/]");
        AnsiConsole.WriteLine();

        try
        {
            await AnsiConsole.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new DownloadedColumn(),
                    new TransferSpeedColumn(),
                    new RemainingTimeColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask(model.Filename, maxValue: model.SizeBytes);
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        if (p.TotalBytes.HasValue) task.MaxValue = p.TotalBytes.Value;
                        task.Value = p.BytesRead;
                    });
                    await _hf.DownloadAsync(model.HuggingFaceRepo, model.Filename, destination, progress, cancellationToken)
                        .ConfigureAwait(false);
                    task.Value = task.MaxValue;
                }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return 130;
        }
        catch (HuggingFaceAuthException ex)
        {
            AnsiConsole.MarkupLine($"[red]HuggingFace rejected the request ({(int)ex.StatusCode}).[/]");
            AnsiConsole.MarkupLine($"  • Accept the license at [blue]https://huggingface.co/{model.HuggingFaceRepo}[/]");
            AnsiConsole.MarkupLine(ex.HadToken
                ? "  • Your saved token didn't cover this repo — accept the license, then retry."
                : "  • Then run [green]sharpbot hf login[/] and retry [green]sharpbot setup[/].");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Download failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        _configWriter.SetModelPath(destination);
        AnsiConsole.MarkupLine($"[green]Done.[/] Config updated at [grey]{_configWriter.FilePath}[/]");
        return 0;
    }
}
