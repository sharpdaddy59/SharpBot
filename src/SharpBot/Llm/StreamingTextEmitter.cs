using System.Text;

namespace SharpBot.Llm;

/// <summary>
/// Buffers raw model-output chunks and decides when it's safe to surface them as
/// user-visible text deltas. The contract: never emit a chunk that contains, or
/// could be the start of, a tool-call marker. Once a full marker is observed, the
/// emitter goes silent for the rest of the inference — the surrounding pipeline
/// will run the post-hoc tool-call extractor on the complete buffer.
///
/// Marker prefixes recognized: <c>&lt;tool_call&gt;</c> (Qwen native) and <c>```</c>
/// (fenced JSON blocks used by Gemma/Llama fallback).
/// </summary>
public sealed class StreamingTextEmitter
{
    // Longest marker prefix we need to hold back. <tool_call> is 11 chars; we keep
    // up to (length-1) of any marker in the buffer until the next chunk disambiguates.
    private static readonly string[] Markers = { "<tool_call>", "```" };

    private readonly StringBuilder _buffer = new();
    private bool _suppressed;

    /// <summary>
    /// Push a chunk produced by the model. Returns the substring that's safe to display
    /// to the user, or null if nothing is ready yet (or the emitter has gone silent
    /// because a tool marker was observed).
    /// </summary>
    public string? Push(string chunk)
    {
        if (_suppressed || string.IsNullOrEmpty(chunk)) return null;

        _buffer.Append(chunk);
        return TryEmit();
    }

    /// <summary>
    /// Called once after the model finishes generation. Emits any remaining safe text
    /// (the buffer's contents minus any partial-marker tail), unless we already went
    /// silent on a tool marker. Returns null when there's nothing more to show.
    /// </summary>
    public string? Drain()
    {
        if (_suppressed || _buffer.Length == 0) return null;

        var rest = _buffer.ToString();
        _buffer.Clear();
        return rest;
    }

    private string? TryEmit()
    {
        var s = _buffer.ToString();

        var fullMarker = FindFullMarker(s);
        if (fullMarker >= 0)
        {
            // Emit any prose that came before the marker, then go silent.
            string? prose = fullMarker > 0 ? s[..fullMarker] : null;
            _buffer.Clear();
            _suppressed = true;
            return string.IsNullOrEmpty(prose) ? null : prose;
        }

        // Hold back any tail that could be the start of a marker.
        var unsafeTail = UnsafeTailLength(s);
        if (unsafeTail >= s.Length) return null;

        var emitLen = s.Length - unsafeTail;
        var emitted = s[..emitLen];
        _buffer.Remove(0, emitLen);
        return emitted.Length > 0 ? emitted : null;
    }

    private static int FindFullMarker(string s)
    {
        var earliest = -1;
        foreach (var m in Markers)
        {
            var idx = s.IndexOf(m, StringComparison.Ordinal);
            if (idx >= 0 && (earliest < 0 || idx < earliest)) earliest = idx;
        }
        return earliest;
    }

    private static int UnsafeTailLength(string s)
    {
        var maxLen = 0;
        foreach (var marker in Markers)
        {
            var maxCheck = Math.Min(marker.Length - 1, s.Length);
            for (var len = maxCheck; len > 0; len--)
            {
                if (s.AsSpan(s.Length - len).SequenceEqual(marker.AsSpan(0, len)))
                {
                    if (len > maxLen) maxLen = len;
                    break;
                }
            }
        }
        return maxLen;
    }
}
