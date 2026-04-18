using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpBot.Config;
using SharpBot.Llm;
using SharpBot.Tools;
using SharpBot.Transport;

namespace SharpBot.Agent;

public sealed class AgentLoop
{
    private readonly IChatTransport _transport;
    private readonly ILlmClient _llm;
    private readonly IToolHost _tools;
    private readonly IConversationStore _store;
    private readonly SharpBotOptions _options;
    private readonly ILogger<AgentLoop> _logger;

    public AgentLoop(
        IChatTransport transport,
        ILlmClient llm,
        IToolHost tools,
        IConversationStore store,
        IOptions<SharpBotOptions> options,
        ILogger<AgentLoop> logger)
    {
        _transport = transport;
        _llm = llm;
        _tools = tools;
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await _llm.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _tools.InitializeAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("AgentLoop started. Tools available: {Count}", _tools.AvailableTools.Count);

        await foreach (var incoming in _transport.IncomingMessagesAsync(cancellationToken))
        {
            try
            {
                await HandleAsync(incoming, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message from {ChatId}", incoming.ChatId);
                await _transport.SendAsync(incoming.ChatId, "Sorry — something went wrong handling that.", cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task HandleAsync(IncomingMessage incoming, CancellationToken cancellationToken)
    {
        var convo = _store.GetOrCreate(incoming.ChatId);
        if (convo.Messages.Count == 0 && !string.IsNullOrWhiteSpace(_options.Llm.SystemPrompt))
        {
            convo.Append(new ChatMessage(ChatRole.System, _options.Llm.SystemPrompt));
        }
        convo.Append(new ChatMessage(ChatRole.User, incoming.Text));

        const int maxToolIterations = 8;
        for (var i = 0; i < maxToolIterations; i++)
        {
            var response = await _llm
                .InferAsync(incoming.ChatId, convo.Messages, _tools.AvailableTools, cancellationToken)
                .ConfigureAwait(false);

            convo.Append(new ChatMessage(ChatRole.Assistant, response.Text ?? string.Empty, response.ToolCalls));

            if (!response.HasToolCalls)
            {
                if (!string.IsNullOrWhiteSpace(response.Text))
                {
                    await _transport.SendAsync(incoming.ChatId, response.Text!, cancellationToken).ConfigureAwait(false);
                }
                return;
            }

            foreach (var call in response.ToolCalls)
            {
                var result = await _tools.ExecuteAsync(call, cancellationToken).ConfigureAwait(false);
                convo.Append(new ChatMessage(ChatRole.Tool, result, ToolCallId: call.Id));
            }
        }

        _logger.LogWarning("Tool iteration limit reached for {ChatId}", incoming.ChatId);
        await _transport.SendAsync(incoming.ChatId, "(stopped — tool iteration limit reached)", cancellationToken)
            .ConfigureAwait(false);
    }
}
