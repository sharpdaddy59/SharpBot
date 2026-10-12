# Code Review — local-lizard tool-calling issues

*Review generated 2026-04-26. Pairs with `local-lizard-review-request.md` in this folder.*

## Context

local-lizard is a parallel C# local-LLM project (different team, OpenClaw agents on the Brazos box) running Qwen 2.5 3B Q4_K_M via LlamaSharp 0.26.0 on Linux. They've hit four reported issues with tool calling and asked for a review against SharpBot's working approach. **No code changes — analysis and written recommendations only.** The deliverable is this review for them to act on.

Repo reviewed: https://github.com/sharpdaddy59/local-lizard
Request doc: `local-lizard-review-request.md` (in this folder)

---

## Headline diagnosis

**One root cause is doing most of the damage:** local-lizard is using a **custom, bespoke tool-call protocol** (`<|tool_call|>call:name{key:val}<tool_call|>`) that doesn't match anything Qwen 2.5 was trained on. Qwen's native tool-call format is XML-tagged JSON: `<tool_call>{"name":"...","arguments":{...}}</tool_call>`. Forcing a 3B model to learn a non-native format from system-prompt examples is fragile — and it's failing in exactly the ways the request describes.

Stacked on top: a chat-template default (`PromptFormat = "gemma4"`) that doesn't match Qwen's actual chat template, plus a small but exact mismatch between the GBNF grammar and the example in the system prompt that explains why the grammar never fires.

Fix the format mismatches and most of the symptoms collapse together.

---

## Issue-by-issue

### Issue 1: Wrong tool selection (`lookup_fact` chosen for "weather in Dallas")

The prompt example literally shows `search_web{query:<|"|>weather<|"|>}`. The model still picked `lookup_fact` with no args. Two reinforcing causes:

1. **Tool descriptions don't tell the model what NOT to use.** At 3B, exclusion wording is load-bearing. `lookup_fact`'s description says "Look up a previously stored fact" — the verb "look up" is a near-synonym for "search," and the model conflates them. SharpBot's pattern: include explicit anti-uses ("Does NOT search the web. Use ONLY for facts the user previously asked you to remember.").
2. **The intent router has no web/weather/news intent.** That's actually correct — those are too varied to regex — but it means every web-style query goes to the LLM, and if the LLM is confused (above), there's no safety net.

**Recommendations:**
- Rewrite `lookup_fact` description with exclusion wording — "Retrieves a fact you previously stored with `remember_fact`. Does NOT search the web or look up live data."
- Rewrite `search_web` description with inclusion wording — "Use for ANY question about current events, weather, news, prices, locations, or anything not already in the conversation. Prefer this over guessing."
- Don't add weather/news to the intent router. Let the LLM handle, but make the descriptions unambiguous enough that it can.

### Issue 2: GBNF grammar not firing

Two distinct bugs here, both fixable independently — but I'd recommend a deeper fix.

1. **The example in the system prompt is inconsistent with the grammar.** The grammar at `tool-call.gbnf` requires `<|tool_call|>` (two pipes around `tool_call`). The example in `ToolRegistry.cs` writes `<|tool_call>` (one closing pipe). The model follows the example, emits the malformed token, and the lazy grammar's trigger pattern `<|tool_call|>call:` never matches. That's a 1-character bug.
2. **Lazy grammar trigger mid-stream:** LlamaSharp's `AddLazyGrammar` does match anywhere in the generated stream (not just position 0 — that part of the request doc is incorrect as far as I know). The mid-stream concern isn't the real issue; the example/grammar inconsistency above is.

**Recommendation: drop the GBNF entirely.** Reasons:
- The grammar+example coupling is exactly the kind of subtle inconsistency that bit them here — and it'll bite again any time either side moves.
- SharpBot proved that `FrequencyPenalty = 0` and `PresencePenalty = 0` (commit 5a2da9b) keep tool-call output well-formed *without* grammar. The penalties were eating the `"` character — which appears in every JSON tool call — and the model would emit malformed JSON. With penalties zeroed, regex extraction post-hoc works reliably across Qwen, Gemma, and Llama.
- Grammar locks you into a single tool-call format. Native Qwen format, MCP tool schemas, future tool variations all become awkward.
- Specifically: switch to Qwen's native `<tool_call>{json}</tool_call>` format and parse with regex on the completed text. SharpBot's `LlamaSharpClient.ExtractToolCalls` does exactly this.

If they want to keep grammar regardless — at minimum, generate the example STRING from the same source as the grammar, so they can't drift.

### Issue 3: Multi-turn context failure

The model loses the thread after two turns and falls back to `get_time` when asked to do something it agreed to. Strongly suggests **chat template mismatch**.

`Configuration.cs` defaults `PromptFormat = "gemma4"`. They're running **Qwen 2.5 3B**. Qwen's chat template uses `<|im_start|>user`, `<|im_end|>`, etc. — the Gemma 4 template uses `<|turn>user`, `<turn|>`. If the config wasn't overridden to "chatml", the model is being prompted in a format it doesn't recognize as turn-structured, and it can't reason about conversational state because it doesn't see distinct turns.

Even on `chatml`, manual ChatML rendering is brittle — the exact whitespace, EOT token handling, and system-prompt placement all matter and differ subtly per model.

**Recommendation: replace manual format rendering with `LLamaTemplate`.** Every GGUF embeds the model's real chat template in metadata. LLamaSharp reads it:

```csharp
var template = new LLamaTemplate(weights, strict: true) { AddAssistant = true };
foreach (var msg in messages) template.Add(msg.Role, msg.Content);
var prompt = LLamaTemplate.Encoding.GetString(template.Apply().ToArray());
```

This is what SharpBot does (`src/SharpBot/Llm/LlamaSharpClient.cs` around lines 96–101). It works across Qwen, Gemma, and Llama with no per-model branching, no `PromptFormat` config flag, no risk of drift.

Side benefit: when the prompt prefix is byte-identical across turns (which template + append-only history gives you), LlamaSharp's KV cache can reuse the prefill — turn N+1's time-to-first-token drops from several seconds to under 200 ms. local-lizard's current `StringBuilder` approach across iterations probably reconstructs the prompt fresh each time, defeating cache reuse even when the executor would otherwise allow it.

### Issue 4: Example argument-name mismatch (`query` vs `q`)

The system prompt example shows `search_web{query:...}` but the tool's `RunAsync` expects `q=...`. The state-machine parser hides the symptom (it accepts both `:` and `=`), but the model is being shown the wrong arg name and would call `search_web{query:"weather"}` — which the tool then rejects.

**Recommendation:** auto-generate the example from the same source as the tool descriptor. Hand-curated examples drift; generated examples can't.

Better, see Q4 below: switch to JSON args and avoid the bespoke parser entirely.

---

## Answers to the five questions

### Q1. How to structure tool descriptions for Qwen 2.5 3B?

- **Lead with the use case, not the function name.** "Use this when the user asks about X" beats "This function does Y."
- **Include exclusion wording** for any tool that shares verbs with another tool (`lookup_fact` vs `search_web`, `remember_fact` vs notes-style tools, etc.).
- **Keep descriptions short.** 3B models drown in long descriptions. One sentence of inclusion + one sentence of exclusion is usually enough.
- **Put the tool name into the description prose** — small models lean on it as a keyword.
- **Don't rely on a single example to teach format.** If you're going to use examples, generate them from the live tool definitions.

### Q2. Can GBNF lazy grammar trigger mid-stream?

Yes — `AddLazyGrammar` matches the trigger pattern anywhere in the generated stream, not just at position 0. The reported "doesn't fire" is almost certainly the example-vs-grammar mismatch (single pipe vs double pipe), not a position-0 limitation.

But: my real recommendation is to **drop GBNF**. SharpBot's `FrequencyPenalty = 0, PresencePenalty = 0` + regex extraction is simpler, model-agnostic, MCP-friendly, and proven on Qwen 2.5 3B specifically (it's SharpBot's curated default model). Grammar buys you mathematical correctness on a problem the simpler approach already solves.

### Q3. Is `LLamaTemplate` better than manual ChatML rendering?

Strongly yes. It reads the chat template from the GGUF metadata, so you get the exact format the model was trained on without per-model branching. Eliminates an entire class of subtle bug (turn separators, EOT tokens, system-prompt placement, BOS/EOS handling). One line to construct, one line per message.

This is probably the single most impactful change local-lizard could make — it likely fixes Issue 3 outright and makes Issues 1 and 2 less fragile by removing one source of "model doesn't quite understand the input."

### Q4. Switch tool args from `key=value` to JSON?

Yes. Three reasons:

1. **Qwen 2.5 was trained on JSON tool args.** You're swimming with the current.
2. **`System.Text.Json` is more forgiving and standard** than the bespoke state-machine parser with `<|"|>` delimiters and brace counting.
3. **MCP integration is JSON-native.** If they ever want to compose external tool servers (which SharpBot does), being JSON already cuts the conversion layer.

Concretely: tools take `JsonElement arguments` instead of a string. Tool definitions expose a JSON schema for parameters. The model emits `{"q":"weather in Dallas"}` natively. This is what SharpBot does (`src/SharpBot/Tools/BuiltIn/BuiltInToolHost.cs`) and it's reliable at 3B.

### Q5. Other LlamaSharp sampling-pipeline tricks?

In rough order of impact:

1. **Set `FrequencyPenalty = 0` and `PresencePenalty = 0`.** This is the single biggest tool-call reliability fix SharpBot found. Both penalties penalize tokens that appear repeatedly — including `"` and `:` and `{`, which appear in *every* JSON tool call. The penalties literally eat the JSON. Default LlamaSharp values are non-zero, which is why this bites silently. (SharpBot commit 5a2da9b.)
2. **Keep `RepeatPenalty = 1.1`.** Gentle anti-loop without breaking structured output.
3. **`Temperature = 0.7–0.8`** for chat. Higher randomness breaks tool-call format adherence.
4. **Per-conversation KV cache reuse.** Create the executor once per conversation, append history monotonically. SharpBot's `LlamaSharpClient` keeps a `Dictionary<string, ConversationState>` — turn N+1 prefills only the new tokens, which on this hardware drops latency dramatically.
5. **Brief warmup on first turn.** Some models (Gemma especially, but worth doing for any) produce empty output on their first 1-2 inferences against a fresh executor. SharpBot does two `MaxTokens=1` warmup calls then resets the cache (commit d851557). Adds ~200 ms once per conversation; saves a confusing silent-first-turn.

---

## Recommended order of operations for them

If they only do one thing: **adopt `LLamaTemplate`** (fixes Issue 3, makes Issues 1 and 2 more tractable).

If they do three things:
1. `LLamaTemplate` for chat formatting (Issue 3)
2. Drop bespoke tool-call format → switch to Qwen's native `<tool_call>{json}</tool_call>` + regex extraction + zeroed penalties (Issues 2, 4)
3. Rewrite `lookup_fact` and `search_web` descriptions with exclusion wording (Issue 1)

The intent router and GBNF grammar can both be removed in the process. Less code, more reliable behavior. local-lizard's current architecture is fighting the model rather than aligning with it; the SharpBot approach is to do less and let the model's training do the work.

---

## Critical files to share with them (their paths, for reference)

- `src/LocalLizard.LocalLLM/LlmEngine.cs` — the main change site. Replace manual ChatML/Gemma4 rendering with `LLamaTemplate`. Switch tool-call iteration to use Qwen's native format.
- `src/LocalLizard.LocalLLM/Tools/ToolCallParser.cs` — replace with regex-on-completed-text (see SharpBot's `ExtractToolCalls`).
- `src/LocalLizard.LocalLLM/Tools/ToolExecutionPipeline.cs` — switch tool args from string `key=value` to `JsonElement`.
- `src/LocalLizard.LocalLLM/Tools/ToolRegistry.cs` — rewrite descriptions, generate examples from tool definitions instead of hand-curating, drop the format-example block once on native Qwen format.
- `src/LocalLizard.LocalLLM/Tools/tool-call.gbnf` — delete.
- `src/LocalLizard.LocalLLM/LazyToolGrammarPipeline.cs` — delete (or keep dormant).
- `src/LocalLizard.Common/Configuration.cs` — drop `PromptFormat` and `ToolGrammarEnabled`. Set sampling defaults to `FrequencyPenalty=0, PresencePenalty=0, RepeatPenalty=1.1, Temperature=0.75`.

## Reference: where in SharpBot to crib from

If they want concrete code to compare against, point them at:
- `src/SharpBot/Llm/LlamaSharpClient.cs` — `LLamaTemplate` usage, KV cache reuse, anti-prompt handling, regex tool-call extraction
- `src/SharpBot/Config/SharpBotOptions.cs` — sampling defaults with explanatory comments on why each value matters
- Commit `5a2da9b` — the sampling-penalty fix, with rationale in the commit message
- `src/SharpBot/Tools/BuiltIn/BuiltInToolHost.cs` — JsonElement-based tool argument handling
- `src/SharpBot/Agent/RegexIntentRouter.cs` — narrow, deterministic intent router (not a catch-all)

GitHub: https://github.com/sharpdaddy59/SharpBot

---

## Verification

This is an analysis deliverable; verification is by them, not by me. The check would be: after they apply the fixes, the four reported scenarios should resolve:

1. "What's the weather in Dallas Texas?" → model emits `<tool_call>{"name":"search_web","arguments":{"q":"weather in Dallas Texas"}}</tool_call>` and the tool fires.
2. Tool calls are well-formed every time, with no trailing hallucinated text.
3. "Give me top news headlines" → "Yes please" → model still searches for news, doesn't drift to `get_time`.
4. The example arg name in any prompt examples matches the actual tool's expected arg name (or examples are generated, not hand-written).

If they want a quick smoke-test before doing the full conversion: just zero the sampling penalties and switch to `LLamaTemplate`. That alone should noticeably improve reliability without touching the tool-call format.
