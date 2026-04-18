using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharpBot.Agent;
using SharpBot.Config;
using SharpBot.Llm;
using SharpBot.Secrets;
using SharpBot.Setup;
using SharpBot.Tools;
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
            c.DefaultRequestHeaders.UserAgent.ParseAdd("SharpBot/0.1 (+https://github.com/)");
        });

        services.AddSingleton<ConfigWriter>();
        services.AddSingleton<ISecretStore, FileSecretStore>();
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        services.AddSingleton<ILlmClient, LlamaSharpClient>();
        services.AddSingleton<IChatTransport, TelegramTransport>();
        services.AddSingleton<IToolHost, McpToolHost>();
        services.AddSingleton<AgentLoop>();

        return services;
    }
}
