using SharpBot.Config;
using SharpBot.Setup;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class ModelsListCommand : Command<ModelsListCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var table = new Table();
        table.AddColumns("Name", "Size", "Access", "Repo", "Notes");
        foreach (var m in ModelCatalog.Curated)
        {
            var access = m.IsGated ? "[yellow]gated[/]" : "[green]open[/]";
            table.AddRow(m.DisplayName, m.SizeDisplay, access, m.HuggingFaceRepo, m.Notes);
        }
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[grey]Gated models require[/] [green]sharpbot hf login[/][grey] + accepting the license on HuggingFace.[/]");
        return 0;
    }
}

public sealed class ModelsDownloadCommand : AsyncCommand<ModelsDownloadCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[name]")]
        public string? Name { get; set; }

        [CommandOption("--save-to-config")]
        public bool SaveToConfig { get; set; } = true;
    }

    private readonly HuggingFaceClient _hf;
    private readonly ConfigWriter _configWriter;

    public ModelsDownloadCommand(HuggingFaceClient hf, ConfigWriter configWriter)
    {
        _hf = hf;
        _configWriter = configWriter;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var model = SelectModel(settings.Name);
        if (model is null) return 1;

        var destination = Path.Combine("models", model.Filename);

        if (File.Exists(destination))
        {
            var overwrite = AnsiConsole.Confirm(
                $"[yellow]{destination}[/] already exists. Overwrite?",
                defaultValue: false);
            if (!overwrite)
            {
                AnsiConsole.MarkupLine("[grey]Skipped download.[/]");
                if (settings.SaveToConfig) SaveModelPath(destination);
                return 0;
            }
        }

        AnsiConsole.MarkupLine($"Downloading [green]{model.DisplayName}[/]");
        AnsiConsole.MarkupLine($"  from [grey]{model.HuggingFaceRepo}/{model.Filename}[/]");
        AnsiConsole.MarkupLine($"  to   [grey]{destination}[/]");
        AnsiConsole.WriteLine();

        try
        {
            await DownloadWithProgressAsync(model, destination, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return 130;
        }
        catch (HuggingFaceAuthException ex)
        {
            PrintAuthHelp(ex, model);
            return 1;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Download failed:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        AnsiConsole.MarkupLine($"[green]Done.[/] Saved to [grey]{destination}[/]");

        if (settings.SaveToConfig) SaveModelPath(destination);
        return 0;
    }

    private async Task DownloadWithProgressAsync(CuratedModel model, string destination, CancellationToken cancellationToken)
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

    private static void PrintAuthHelp(HuggingFaceAuthException ex, CuratedModel model)
    {
        AnsiConsole.MarkupLine($"[red]HuggingFace rejected the request ({(int)ex.StatusCode}).[/]");
        if (!ex.HadToken)
        {
            AnsiConsole.MarkupLine($"[yellow]This model is gated.[/] To download it:");
            AnsiConsole.MarkupLine($"  1. Open [blue]https://huggingface.co/{model.HuggingFaceRepo}[/] and accept the license.");
            AnsiConsole.MarkupLine("  2. Create a [yellow]Read[/] token at [blue]https://huggingface.co/settings/tokens[/]");
            AnsiConsole.MarkupLine("  3. Run [green]sharpbot hf login[/] and paste the token.");
            AnsiConsole.MarkupLine("  4. Retry this download.");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]You have a token saved, but it was rejected for this repo.[/]");
            AnsiConsole.MarkupLine($"  • Make sure you've accepted the license at [blue]https://huggingface.co/{model.HuggingFaceRepo}[/]");
            AnsiConsole.MarkupLine("  • Or pick a non-gated model (Qwen 2.5 variants) that doesn't need a license.");
        }
    }

    private void SaveModelPath(string destination)
    {
        _configWriter.SetModelPath(destination);
        AnsiConsole.MarkupLine($"[grey]Updated[/] [blue]{_configWriter.FilePath}[/] [grey]→ SharpBot:Llm:ModelPath = {destination}[/]");
    }

    private static CuratedModel? SelectModel(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var byName = ModelCatalog.Curated.FirstOrDefault(m =>
                m.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                m.Filename.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (byName is null)
            {
                AnsiConsole.MarkupLine($"[red]Unknown model:[/] {name}. Run [green]sharpbot models list[/].");
                return null;
            }
            return byName;
        }

        var choices = ModelCatalog.Curated.ToList();
        var chosen = AnsiConsole.Prompt(
            new SelectionPrompt<CuratedModel>()
                .Title("Pick a model to download:")
                .UseConverter(m =>
                {
                    var gate = m.IsGated ? " [yellow](gated — needs hf login)[/]" : "";
                    return $"{m.DisplayName} [grey]({m.SizeDisplay}, ~{m.RecommendedRamGb} GB RAM)[/]{gate} — {m.Notes}";
                })
                .AddChoices(choices));
        return chosen;
    }
}
