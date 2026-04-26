using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SharpBot.Agent;
using SharpBot.Config;
using SharpBot.Tools;

namespace SharpBot.Tests;

public class CompositeToolHostTests
{
    private static CompositeToolHost MakeHost(int maxBytes, params IToolHost[] inner) =>
        new(
            inner,
            Options.Create(new SharpBotOptions { ToolHost = new ToolHostOptions { MaxResultBytes = maxBytes } }),
            NullLogger<CompositeToolHost>.Instance);

    [Fact]
    public void Result_under_cap_passes_through_unchanged()
    {
        var host = MakeHost(maxBytes: 1000);
        var result = host.Truncate("hello world", "core.test");
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Result_exactly_at_cap_passes_through_unchanged()
    {
        var host = MakeHost(maxBytes: 100);
        var input = new string('x', 100);
        var result = host.Truncate(input, "core.test");
        Assert.Equal(input, result);
    }

    [Fact]
    public void Result_over_cap_is_truncated()
    {
        var host = MakeHost(maxBytes: 200);
        var input = new string('x', 5000);
        var result = host.Truncate(input, "core.test");
        Assert.True(result.Length < 5000, $"expected truncation but got {result.Length} chars");
        Assert.Contains("truncated", result);
        Assert.Contains("core.test", result);
    }

    [Fact]
    public void Truncated_result_preserves_head_and_tail()
    {
        var host = MakeHost(maxBytes: 200);
        // Distinctive markers at each end so we can prove both survived.
        var input = "HEAD_MARKER" + new string('-', 5000) + "TAIL_MARKER";
        var result = host.Truncate(input, "core.test");
        Assert.StartsWith("HEAD_MARKER", result);
        Assert.EndsWith("TAIL_MARKER", result);
        Assert.Contains("[... truncated", result);
    }

    [Fact]
    public void Disabled_when_max_bytes_is_zero()
    {
        var host = MakeHost(maxBytes: 0);
        var input = new string('x', 1_000_000);
        var result = host.Truncate(input, "core.test");
        Assert.Equal(input, result);
    }

    [Fact]
    public void Disabled_when_max_bytes_is_negative()
    {
        var host = MakeHost(maxBytes: -1);
        var input = new string('x', 1_000_000);
        var result = host.Truncate(input, "core.test");
        Assert.Equal(input, result);
    }

    [Fact]
    public void Pathologically_small_cap_returns_marker_only()
    {
        // Cap so small the truncation marker itself can't fit. Must not crash;
        // returns just the marker so the model still sees a signal.
        var host = MakeHost(maxBytes: 10);
        var input = new string('x', 5000);
        var result = host.Truncate(input, "core.test");
        Assert.Contains("truncated", result);
        Assert.DoesNotContain("xxxxx", result);
    }

    [Fact]
    public async Task ExecuteAsync_truncates_inner_host_output()
    {
        // Integration-style: prove that the cap actually fires at the boundary,
        // not just that the helper works in isolation.
        var fake = new FakeToolHost(new string('z', 50_000));
        var host = MakeHost(maxBytes: 500, fake);
        await host.InitializeAsync();

        var call = new ToolCall(Id: "1", Name: "fake.echo", ArgumentsJson: "{}");
        var result = await host.ExecuteAsync(call);

        Assert.True(result.Length < 50_000);
        Assert.Contains("truncated", result);
        Assert.Contains("fake.echo", result);
    }

    private sealed class FakeToolHost : IToolHost
    {
        private readonly string _payload;

        public FakeToolHost(string payload) { _payload = payload; }

        public IReadOnlyList<ToolDescriptor> AvailableTools { get; } =
            new[] { new ToolDescriptor("fake.echo", "Returns a fixed string for testing.", "{}") };

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default) =>
            Task.FromResult(_payload);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
