using SharpBot.Tools.BuiltIn;

namespace SharpBot.Tests;

public class NoteStoreTests : IDisposable
{
    private readonly string _path;

    public NoteStoreTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"sharpbot-notes-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Save_then_recall_roundtrip()
    {
        var store = new NoteStore(_path);
        store.Save("favorite_color", "blue");
        Assert.Equal("blue", store.Recall("favorite_color"));
    }

    [Fact]
    public void Recall_unknown_key_returns_null()
    {
        var store = new NoteStore(_path);
        Assert.Null(store.Recall("nope"));
    }

    [Fact]
    public void Save_overwrites_existing_note()
    {
        var store = new NoteStore(_path);
        store.Save("color", "red");
        store.Save("color", "green");
        Assert.Equal("green", store.Recall("color"));
    }

    [Fact]
    public void Notes_persist_across_instances()
    {
        var first = new NoteStore(_path);
        first.Save("k", "v");

        var second = new NoteStore(_path);
        Assert.Equal("v", second.Recall("k"));
    }

    [Fact]
    public void List_returns_sorted_by_key_case_insensitive()
    {
        var store = new NoteStore(_path);
        store.Save("banana", "yellow");
        store.Save("apple", "red");
        store.Save("Cherry", "dark");

        var list = store.List();

        Assert.Equal(3, list.Count);
        Assert.Equal("apple", list[0].Key);
        Assert.Equal("banana", list[1].Key);
        Assert.Equal("Cherry", list[2].Key);
    }

    [Fact]
    public void Delete_returns_true_when_note_existed()
    {
        var store = new NoteStore(_path);
        store.Save("k", "v");
        Assert.True(store.Delete("k"));
        Assert.Null(store.Recall("k"));
    }

    [Fact]
    public void Delete_returns_false_when_note_absent()
    {
        var store = new NoteStore(_path);
        Assert.False(store.Delete("never-existed"));
    }

    [Fact]
    public void Empty_list_when_no_notes()
    {
        var store = new NoteStore(_path);
        Assert.Empty(store.List());
    }
}
