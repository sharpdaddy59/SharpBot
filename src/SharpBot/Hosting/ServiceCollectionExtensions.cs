using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpBot.Agent;
using SharpBot.Config;
using SharpBot.Llm;
using SharpBot.Secrets;
using SharpBot.Setup;
using SharpBot.Tools;
using SharpBot.Tools.BuiltIn;
using SharpBot.Transport;

namespace SharpBot.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharpBot(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SharpBotOptions>()
            .Bind(configuration.GetSection(SharpBotOptions.SectionName));

        services.AddHttpClient<HuggingFaceClient>(c =>
        {
            c.Timeout = TimeSpan.FromHours(2);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("SharpBot/0.2 (+https://github.com/sharpdaddy59/SharpBot)");
        });

        // HttpClient per tool that needs outbound HTTP — each gets its own name so we can tune timeouts
        // per tool later if needed.
        services.AddHttpClient<FetchUrlTool>();
        services.AddHttpClient<WeatherTool>(c => c.Timeout = TimeSpan.FromSeconds(15));

        // Shared stores used by built-in tools.
        services.AddSingleton<NoteStore>();

        // Built-in tools: each IBuiltInTool instance is discovered by BuiltInToolHost via DI.
        services.AddSingleton<IBuiltInTool, CurrentTimeTool>();
        services.AddSingleton<IBuiltInTool, CalculatorTool>();
        services.AddSingleton<IBuiltInTool>(sp => sp.GetRequiredService<FetchUrlTool>());
        services.AddSingleton<IBuiltInTool>(sp => sp.GetRequiredService<WeatherTool>());
        services.AddSingleton<IBuiltInTool, ReadFileTool>();
        services.AddSingleton<IBuiltInTool, ListFilesTool>();
        services.AddSingleton<IBuiltInTool, SaveNoteTool>();
        services.AddSingleton<IBuiltInTool, RecallNoteTool>();
        services.AddSingleton<IBuiltInTool, ListNotesTool>();
        services.AddSingleton<IBuiltInTool, DeleteNoteTool>();

        // Tool hosts: built-in + MCP are both concrete singletons, aggregated behind IToolHost.
        services.AddSingleton<BuiltInToolHost>();
        services.AddSingleton<McpToolHost>();
        services.AddSingleton<IToolHost>(sp => new CompositeToolHost(
            new IToolHost[]
            {
                sp.GetRequiredService<BuiltInToolHost>(),
                sp.GetRequiredService<McpToolHost>(),
            },
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CompositeToolHost>>()));

        services.AddSingleton<ConfigWriter>();
        services.AddSingleton<ISecretStore, FileSecretStore>();
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        services.AddSingleton<IIntentRouter, RegexIntentRouter>();
        services.AddSingleton<ILlmClient, LlamaSharpClient>();
        services.AddSingleton<IChatTransport, TelegramTransport>();
        services.AddSingleton<AgentLoop>();

        return services;
    }
}
