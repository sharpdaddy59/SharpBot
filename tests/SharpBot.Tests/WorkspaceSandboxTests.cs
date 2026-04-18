using SharpBot.Config;
using SharpBot.Tools.BuiltIn;

namespace SharpBot.Tests;

public class WorkspaceSandboxTests : IDisposable
{
    private readonly string _root;
    private readonly BuiltInToolsOptions _options;

    public WorkspaceSandboxTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "sharpbot-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        _options = new BuiltInToolsOptions { WorkspaceDirectory = _root };
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Empty_relative_path_resolves_to_root()
    {
        var root = WorkspaceSandbox.GetRoot(_options);
        Assert.True(WorkspaceSandbox.TryResolve(root, "", out var full, out var err));
        Assert.Null(err);
        Assert.Equal(root, full);
    }

    [Fact]
    public void Simple_relative_path_stays_under_root()
    {
        var root = WorkspaceSandbox.GetRoot(_options);
        Assert.True(WorkspaceSandbox.TryResolve(root, "notes.txt", out var full, out _));
        Assert.StartsWith(root, full);
        Assert.EndsWith("notes.txt", full);
    }

    [Fact]
    public void Nested_relative_path_stays_under_root()
    {
        var root = WorkspaceSandbox.GetRoot(_options);
        Assert.True(WorkspaceSandbox.TryResolve(root, "sub/dir/file.md", out var full, out _));
        Assert.StartsWith(root, full);
    }

    [Fact]
    public void Dotdot_escape_is_rejected()
    {
        var root = WorkspaceSandbox.GetRoot(_options);
        Assert.False(WorkspaceSandbox.TryResolve(root, "../outside.txt", out _, out var err));
        Assert.NotNull(err);
        Assert.Contains("outside", err!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deep_dotdot_escape_is_rejected()
    {
        var root = WorkspaceSandbox.GetRoot(_options);
        Assert.False(WorkspaceSandbox.TryResolve(root, "a/b/../../../../outside.txt", out _, out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Absolute_path_outside_root_is_rejected()
    {
        var root = WorkspaceSandbox.GetRoot(_options);
        var outside = Path.Combine(Path.GetTempPath(), "definitely-outside-" + Guid.NewGuid().ToString("N"));
        Assert.False(WorkspaceSandbox.TryResolve(root, outside, out _, out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void Absolute_path_inside_root_is_accepted()
    {
        var root = WorkspaceSandbox.GetRoot(_options);
        var inside = Path.Combine(root, "inside.txt");
        Assert.True(WorkspaceSandbox.TryResolve(root, inside, out var full, out _));
        Assert.Equal(Path.GetFullPath(inside), full);
    }

    [Fact]
    public void GetRoot_creates_directory_if_missing()
    {
        var missing = Path.Combine(_root, "new-subdir");
        var options = new BuiltInToolsOptions { WorkspaceDirectory = missing };
        var root = WorkspaceSandbox.GetRoot(options);
        Assert.True(Directory.Exists(root));
    }
}
