using System.Text.Json;

namespace SharpBot.Tools.BuiltIn;

/// <summary>
/// A first-party tool implemented directly in SharpBot (no external process, no runtime dep).
/// The <see cref="BuiltInToolHost"/> collects every registered IBuiltInTool, prefixes each
/// with "core." to produce the qualified name exposed to the LLM, and routes calls back.
/// </summary>
public interface IBuiltInTool
{
    /// <summary>Short bare name of the tool, e.g. "current_time". Must not contain '.'.</summary>
    string Name { get; }

    /// <summary>Human-readable description surfaced to the LLM for tool selection.</summary>
    string Description { get; }

    /// <summary>JSON Schema string describing the tool's arguments.</summary>
    string ParametersJsonSchema { get; }

    /// <summary>Execute the tool with the given JSON arguments. Return value is surfaced to the LLM.</summary>
    Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken);
}
