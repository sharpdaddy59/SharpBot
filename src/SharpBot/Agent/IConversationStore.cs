namespace SharpBot.Agent;

public interface IConversationStore
{
    Conversation GetOrCreate(string chatId);
    void Reset(string chatId);
}

public sealed class Conversation
{
    public string ChatId { get; }
    public List<ChatMessage> Messages { get; } = new();

    public Conversation(string chatId) => ChatId = chatId;

    public void Append(ChatMessage message) => Messages.Add(message);
}
