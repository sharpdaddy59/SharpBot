namespace SharpBot.Secrets;

public interface ISecretStore
{
    string? Get(string key);
    void Set(string key, string value);
    void Delete(string key);
    void Save();
}
