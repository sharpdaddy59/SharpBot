using System.Collections.Concurrent;

namespace SharpBot.Agent;

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, Conversation> _conversations = new();

    public Conversation GetOrCreate(string chatId) =>
        _conversations.GetOrAdd(chatId, id => new Conversation(id));

    public void Reset(string chatId) => _conversations.TryRemove(chatId, out _);
}
