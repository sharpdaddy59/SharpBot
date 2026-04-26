# Tool calling in SharpBot

This is the "why we did it this way" doc for SharpBot's tool-calling layer. If you've ever wondered why a config knob is set to a particular value, why we don't use GBNF grammars, or why tool descriptions are written the way they are — this is your reference.

It's written for someone who can read C# but isn't necessarily steeped in LLM tool-calling. The TL;DR for experienced readers is at the bottom.

---

## What "tool calling" actually is

When you type "What time is it?" into SharpBot's chat REPL, the model doesn't know what time it is. It can guess, but it'll be wrong. The fix: give the model a way to *ask the host* to run a function and feed the answer back.

Mechanically, this is a four-step dance:

1. **The host injects tool definitions** into the system prompt. ("You have these tools available: `core.current_time`, `core.weather`, ...")
2. **The model emits a special-formatted block** in its response when it wants to call one. (`<tool_call>{"name":"core.current_time","arguments":{}}</tool_call>`)
3. **The host parses that block, runs the C# code**, and appends the result to the conversation as a `tool` role message.
4. **The model is invoked again** with the tool result in context, and now produces a normal text reply.

That's it. There's no magic transport between the model and the host — the model just emits text in a known format, and the host scans for it. This is why **format choice is everything**: pick a format the model knows, or be prepared to fight it.

The reason this is hard for small models (3B-class like Qwen 2.5 3B) is that they're inconsistent at format-following. A 70B model will reliably emit valid JSON. A 3B model will sometimes drop a quote, hallucinate a trailing word, or pick the wrong tool. SharpBot's design is built around making a 3B model behave reliably on cheap hardware.

---

## Principles

These are SharpBot's load-bearing decisions. Each one was either learned the hard way, or deliberately chosen to avoid a known failure mode.

### 1. Use the model's native tool-call format. Don't invent your own.

Qwen 2.5 was *trained* on `<tool_call>{"name":"...","arguments":{...}}</tool_call>`. SharpBot uses that format. Gemma uses ` ```json {...} ``` ` fenced blocks; SharpBot also parses those as a fallback. Llama 3.2 uses something else again, less consistently — supported best-effort.

The trap to avoid: building a custom tool-call protocol (`<|tool_call|>call:name{key:val}<tool_call|>` or similar) and trying to teach the model via in-context examples. A 3B model can't reliably learn a non-native format from a handful of examples. It will look right most of the time and fail in subtle ways the rest. We saw this exact failure in a parallel project — the model emitted *almost* the right format and the post-processing pipeline never matched.

**Where this lives:** [src/SharpBot/Llm/LlamaSharpClient.cs](../src/SharpBot/Llm/LlamaSharpClient.cs) — the `ExtractToolCalls` static method handles both formats.

### 2. Use the model's native chat template. Don't hand-roll one.

Every GGUF file embeds the chat template the model was trained on. Qwen uses `<|im_start|>user...<|im_end|>`. Gemma uses `<start_of_turn>user...<end_of_turn>`. Llama uses `<|start_header_id|>user<|end_header_id|>...<|eot_id|>`. The exact whitespace, the placement of the system prompt, the EOT token — all of it differs subtly per model and matters.

LlamaSharp ships a `LLamaTemplate` type that reads the template from the GGUF metadata. Use it:

```csharp
var template = new LLamaTemplate(weights, strict: true) { AddAssistant = true };
foreach (var msg in messages) template.Add(msg.Role, msg.Content);
var prompt = LLamaTemplate.Encoding.GetString(template.Apply().ToArray());
```

This is one of those choices that costs nothing and saves a class of "the model seems confused" bugs. No `PromptFormat` config flag, no per-model branching, no risk of the format drifting from the model's training.

**Where this lives:** [src/SharpBot/Llm/LlamaSharpClient.cs](../src/SharpBot/Llm/LlamaSharpClient.cs) around lines 96–101.

### 3. Zero out FrequencyPenalty and PresencePenalty.

This is the single subtlest, highest-impact tuning fact in the whole codebase, and it's only obvious in hindsight.

`FrequencyPenalty` and `PresencePenalty` are sampling parameters that down-weight tokens that have already appeared in the output. They exist to make creative text less repetitive. **They also down-weight the `"` character.** And `:`. And `{`. And every other character that appears repeatedly in JSON.

If you leave these at non-zero defaults, the model will start a tool call like `{"name": "core.curr` and then *the next quote character is suppressed*. The model picks a different token, you get malformed JSON, the parser fails, the bot apologizes, and the user is confused. The failure is silent and looks like "the model is stupid." The model isn't stupid — your sampler is eating its tool calls.

SharpBot defaults: `FrequencyPenalty = 0`, `PresencePenalty = 0`. If you want creative-writing-style output, bump them in a non-tool-using context only.

**Where this lives:** [src/SharpBot/Config/SharpBotOptions.cs](../src/SharpBot/Config/SharpBotOptions.cs) lines 67–78. Commit `5a2da9b` is the fix.

### 4. Keep RepeatPenalty at 1.1.

`RepeatPenalty` is different from the two above — it operates over a sliding window of recent tokens and penalizes whole-token repetition. At 1.1 it's gentle enough not to break structured output but strong enough to prevent the model from getting stuck in a loop. Below 1.0 the model will sometimes spiral on the same token. Above 1.2 it starts breaking JSON.

**Where this lives:** [src/SharpBot/Config/SharpBotOptions.cs](../src/SharpBot/Config/SharpBotOptions.cs) line 65.

### 5. Per-conversation KV cache reuse.

When the model processes a prompt, it builds an internal KV (key-value) cache representing the attention state for every token. Building this cache from scratch is expensive — on Brazos-class hardware it's seconds for a long prompt. **Reusing the cache across turns** is the difference between 5-second and 200-millisecond time-to-first-token.

The trick is that the cache is only reusable if the prompt prefix is *byte-identical* between turns. SharpBot keeps a `Dictionary<string, ConversationState>` keyed by conversation ID, each entry holding its own `LLamaContext` + `InteractiveExecutor`. Each turn, we render the new full prompt, compare it against what the executor already processed, and prefill only the delta.

The implications for everything else:
- **Conversation history is append-only.** Editing past turns invalidates the cache.
- **The system prompt and tool catalog are baked in once per session.** Re-injecting "current state" at the top breaks everything.
- **Tool results are appended as new turns**, not patched into the system prompt.

**Where this lives:** [src/SharpBot/Llm/LlamaSharpClient.cs](../src/SharpBot/Llm/LlamaSharpClient.cs) lines 113–141 (cache hit/miss logic), `ConversationState` partial class.

### 6. Regex extraction post-hoc, not GBNF grammar constraints.

GBNF (the grammar format llama.cpp accepts) lets you constrain sampling so the model can *only* emit tokens that match a grammar. In theory this guarantees well-formed tool calls. In practice it:

- Couples the grammar file to the prompt example (drift causes silent failures)
- Locks you into one tool-call format (no fallback for Gemma/Llama)
- Doesn't compose with MCP tool schemas without runtime grammar generation
- Is fragile to the trigger-pattern matching the actual model output exactly

SharpBot does the simpler thing: let the model generate freely (with zeroed penalties — see #3), then run a regex over the completed text to extract any `<tool_call>` or ` ```json ``` ` blocks. If the model emitted something malformed, we surface a useful error rather than silently retrying.

This works because principles #1, #2, and #3 keep the output well-formed in the first place. Grammar is a sledgehammer for a problem that doesn't exist when the foundation is right.

**Where this lives:** [src/SharpBot/Llm/LlamaSharpClient.cs](../src/SharpBot/Llm/LlamaSharpClient.cs) `ExtractToolCalls` regex.

### 7. Brief warmup on the first turn.

Some models (Gemma is the notorious one, but it's not unique) produce empty output on their first 1–2 inferences against a fresh executor. The model needs to "wake up" by processing a couple of throwaway tokens.

SharpBot does two `MaxTokens=1` warmup inferences when a conversation's executor is created, then resets the cache so the user's actual first turn gets a clean prefill. Adds ~200 ms once per conversation; saves a confusing silent-first-turn failure that's almost impossible to diagnose otherwise.

**Where this lives:** [src/SharpBot/Llm/LlamaSharpClient.cs](../src/SharpBot/Llm/LlamaSharpClient.cs) `WarmupExecutor` method (around line 211). Toggleable via `WarmupOnFirstTurn` — leave it on.

### 8. Tool arguments are JSON, parsed via JsonElement.

Tools take `JsonElement arguments`, not a custom string format. Three reasons:

- The models we target were trained on JSON tool args. Stay aligned with the training.
- `System.Text.Json` is more forgiving and standard than any bespoke parser.
- MCP is JSON-native; using JSON internally means zero conversion at the MCP boundary.

The model emits `{"q":"weather in Dallas"}`, we parse it into a `JsonElement`, the tool reads `arguments.GetProperty("q").GetString()`. Done.

**Where this lives:** [src/SharpBot/Tools/BuiltIn/IBuiltInTool.cs](../src/SharpBot/Tools/BuiltIn/IBuiltInTool.cs) and [src/SharpBot/Tools/BuiltIn/BuiltInToolHost.cs](../src/SharpBot/Tools/BuiltIn/BuiltInToolHost.cs).

### 9. Narrow deterministic intent router. Not a catch-all.

For utterances we can recognize unambiguously with a regex ("what time is it"), we skip the LLM entirely and dispatch directly to a tool. This is the deterministic intent router.

The temptation is to keep adding patterns ("what's the weather", "search for X", "remind me to..."). Resist it. The router should fast-path utterances where:

- The regex is narrow enough that false positives are nearly impossible
- The mapped tool is the only sensible answer

Anything else falls through to the LLM, which is where ambiguous, conversational, or compound queries belong. A router that tries to be a catch-all is a hand-rolled NLU system, and it will fail unpredictably.

SharpBot's router currently fast-paths only time/date queries. That's by design.

**Where this lives:** [src/SharpBot/Agent/RegexIntentRouter.cs](../src/SharpBot/Agent/RegexIntentRouter.cs). Wired into [AgentLoop.cs:67–87](../src/SharpBot/Agent/AgentLoop.cs).

### 10. Identical-call-twice loop detection.

If the model emits the *exact same tool call* two iterations in a row, it's not absorbing the tool result. Almost always this means the model isn't reading `role=tool` messages correctly — usually a chat-template mismatch (Gemma is a frequent offender). Keep iterating and it'll spin forever, fans roaring.

SharpBot detects this and aborts with a useful error pointing at the likely cause ("try Qwen 2.5 3B instead").

**Where this lives:** [src/SharpBot/Agent/AgentLoop.cs](../src/SharpBot/Agent/AgentLoop.cs) lines ~89–107.

---

## The pipeline, traced

Here's the journey of a single user turn through SharpBot's tool layer. Useful when something goes wrong and you need to know where to set a breakpoint.

1. **A message arrives** via `IChatTransport.IncomingMessagesAsync` (Telegram, REPL, or eventually voice). [Transport/IChatTransport.cs](../src/SharpBot/Transport/IChatTransport.cs)
2. **`AgentLoop.HandleAsync`** picks it up. The user message is appended to the conversation. [Agent/AgentLoop.cs](../src/SharpBot/Agent/AgentLoop.cs)
3. **Intent router runs first.** If it matches, the mapped tool dispatches directly via `IToolHost.ExecuteAsync`, the result becomes the assistant's reply, we're done. [Agent/RegexIntentRouter.cs](../src/SharpBot/Agent/RegexIntentRouter.cs)
4. **Otherwise: the LLM is invoked** via `ILlmClient.StreamInferAsync`. [Llm/ILlmClient.cs](../src/SharpBot/Llm/ILlmClient.cs)
5. **Inside `LlamaSharpClient.StreamInferAsync`:** the conversation is rendered through `LLamaTemplate`, the system prompt has the tool catalog injected (Qwen XML format), the KV cache is checked for delta-prefill opportunity, the model generates. [Llm/LlamaSharpClient.cs](../src/SharpBot/Llm/LlamaSharpClient.cs)
6. **`StreamingTextEmitter`** buffers tokens, holding back anything that might be the start of a tool-call marker. Only safe prose surfaces as `LlmStreamEvent.TextDelta`. [Llm/StreamingTextEmitter.cs](../src/SharpBot/Llm/StreamingTextEmitter.cs)
7. **At end-of-generation, `ExtractToolCalls`** scans the complete output for `<tool_call>` and ` ```json ``` ` blocks. The terminal `LlmStreamEvent.Final` event carries the parsed `LlmResponse`.
8. **Back in `AgentLoop`:** if the response has tool calls, dispatch each via `IToolHost.ExecuteAsync` (which routes built-in vs MCP), append the result as a `role=tool` message, and loop back to step 4. Cap at 8 iterations.
9. **When the model produces a tool-call-free response,** send it via `IChatTransport.SendAsync` and we're done.

---

## Writing tool descriptions for small models

This came out of reviewing a parallel project that hit a `lookup_fact` vs `search_web` confusion. The lessons apply to any small-model tool description.

- **Lead with the use case, not the function name.** "Use this when the user asks about current events" beats "Searches the web."
- **Include exclusion wording** when two tools share verbs. "Look up a previously stored fact. Does NOT search the web or look up live data." vs "Search the web for current information. Use this for ANY question about weather, news, prices, locations."
- **Keep them short.** One sentence of inclusion + one sentence of exclusion. 3B models drown in long descriptions.
- **Put the tool name into the prose.** Small models lean on it as a keyword for retrieval.
- **Don't rely on a single example to teach format.** If you must include examples, generate them from live tool definitions so they can't drift.

---

## What we deliberately don't do

- **Microsoft Agent Framework (yet).** The `AIAgent` / `ChatClientAgent` abstraction is appealing but would mean rewriting the working tool-extraction in [LlamaSharpClient](../src/SharpBot/Llm/LlamaSharpClient.cs) against `AIFunction` semantics, re-validating KV-cache discipline against `ChatClientAgent`'s prompt assembly, and re-expressing the MCP bridge. The cost is real; the benefit (provider portability) is mostly already provided by `ILlmClient`. Defer until there's a concrete need.
- **GBNF grammars.** See principle #6.
- **Custom tool-call protocols.** See principle #1.
- **Manual chat-template strings.** See principle #2.
- **A catch-all intent router.** See principle #9.

---

## TL;DR

For experienced readers — SharpBot's tool-calling sticks to four rules:

1. Use the model's native chat template via `LLamaTemplate`.
2. Use the model's native tool-call format. Parse with regex post-hoc.
3. Zero `FrequencyPenalty` and `PresencePenalty`. Keep `RepeatPenalty = 1.1`.
4. Maintain per-conversation KV cache discipline (append-only history, byte-identical prefix).

Everything else is in service of those four. Most "tool calling is unreliable on small models" complaints are a violation of one of them.
