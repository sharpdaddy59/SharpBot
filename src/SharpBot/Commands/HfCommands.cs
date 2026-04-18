using SharpBot.Secrets;
using SharpBot.Setup;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SharpBot.Commands;

public sealed class HfLoginCommand : AsyncCommand<HfLoginCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[token]")]
        public string? Token { get; set; }
    }

    private readonly ISecretStore _secrets;
    private readonly HuggingFaceClient _hf;

    public HfLoginCommand(ISecretStore secrets, HuggingFaceClient hf)
    {
        _secrets = secrets;
        _hf = hf;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[bold]HuggingFace login[/]").LeftJustified());
        AnsiConsole.MarkupLine("Some models (Gemma, Llama) require accepting a license on HuggingFace and a read token.");
        AnsiConsole.MarkupLine("  1. Create a token: [blue]https://huggingface.co/settings/tokens[/]  (type: [yellow]Read[/])");
        AnsiConsole.MarkupLine("  2. For gated models, accept the license on the model's page first.");
        AnsiConsole.WriteLine();

        var token = settings.Token ?? AnsiConsole.Prompt(
            new TextPrompt<string>("HF token:")
                .Secret()
                .Validate(t => string.IsNullOrWhiteSpace(t)
                    ? ValidationResult.Error("Token cannot be empty.")
                    : ValidationResult.Success()));

        AnsiConsole.MarkupLine("[grey]Validating…[/]");
        bool valid;
        try
        {
            valid = await _hf.ValidateTokenAsync(token, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Could not reach HuggingFace:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        if (!valid)
        {
            AnsiConsole.MarkupLine("[red]HuggingFace rejected that token.[/] Double-check it at https://huggingface.co/settings/tokens");
            return 1;
        }

        _secrets.Set(SecretKeys.HuggingFaceToken, token);
        _secrets.Save();
        AnsiConsole.MarkupLine("[green]Token saved.[/] You can now download gated models.");
        return 0;
    }
}

public sealed class HfLogoutCommand : Command<HfLogoutCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly ISecretStore _secrets;
    public HfLogoutCommand(ISecretStore secrets) => _secrets = secrets;

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        _secrets.Delete(SecretKeys.HuggingFaceToken);
        _secrets.Save();
        AnsiConsole.MarkupLine("[green]HuggingFace token removed.[/]");
        return 0;
    }
}

public sealed class HfStatusCommand : Command<HfStatusCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    private readonly ISecretStore _secrets;
    public HfStatusCommand(ISecretStore secrets) => _secrets = secrets;

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var token = _secrets.Get(SecretKeys.HuggingFaceToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            AnsiConsole.MarkupLine("[yellow]No HuggingFace token saved.[/] Run [green]sharpbot hf login[/] to add one.");
            return 0;
        }

        var masked = token.Length > 8
            ? $"{token[..4]}…{token[^4..]}"
            : "****";
        AnsiConsole.MarkupLine($"[green]Logged in.[/] Token: [grey]{masked}[/]");
        return 0;
    }
}
