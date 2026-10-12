# Code Review Request: LocalLizard Tool Calling Issues

## Context

LocalLizard is a local-only C# AI assistant using Qwen 2.5 3B (Q4_K_M) + LLamaSharp 0.26.0 on Linux (no GPU). It has a tool-calling system with GBNF grammar constraints and a deterministic intent router. You already built a very similar project called SharpBot — this review asks you to analyze LocalLizard's specific pain points and recommend fixes.

**Do not change any code.** Analysis and written recommendations only.

## Key Files (all under `/shared/projects/local-lizard/`)

- `src/LocalLizard.LocalLLM/LlmEngine.cs` — main agent loop + tool call loop
- `src/LocalLizard.LocalLLM/LazyToolGrammarPipeline.cs` — GBNF lazy grammar sampler
- `src/LocalLizard.LocalLLM/Tools/ToolCallParser.cs` — parses `<|tool_call|>call:name{args}<tool_call|>` format
- `src/LocalLizard.LocalLLM/Tools/ToolExecutionPipeline.cs` — orchestrates tool dispatch
- `src/LocalLizard.LocalLLM/Tools/IntentRouter.cs` — regex-based deterministic router (5 intents)
- `src/LocalLizard.LocalLLM/Tools/tool-call.gbnf` — GBNF grammar for tool calls
- `src/LocalLizard.LocalLLM/Tools/ToolRegistry.cs` — tool definitions, generates system prompt
- `src/LocalLizard.Common/Configuration.cs` — config with `PromptFormat` (ChatML), `ToolGrammarEnabled`, `IntentRouterEnabled`

## Issue 1: Wrong Tool Selection + Missing Intent Router Match

**Prompt (user):**
> What's the weather in Dallas Texas?

**Expected behavior:** Route to `search_web{q:weather in Dallas Texas}` or a weather tool.

**Actual behavior:** Model called `lookup_fact` with no arguments. Error: `lookup_fact requires a key argument. Usage: key=fact_name`

The intent router didn't catch "weather" keywords (it only handles time/date patterns). The model conflated `lookup_fact` (local memory retrieval) with `search_web` (web search). The tool descriptions apparently didn't differentiate them well enough for a 3B model.

Tool descriptions as rendered to model:
- `search_web`: "Search the web for current information. Argument: q (search query). Example: q=weather in Dallas Texas"
- `lookup_fact`: "Look up a previously stored fact. Argument: key (the fact name to retrieve). Example: key=user_name"

## Issue 2: GBNF Grammar Not Firing

**Raw model output (from test run):**
```
I'll search the web for you....
<|tool_call>call:search_web{q:weather in Dallas Texas}webtoken
```

The `webtoken` is hallucinated text after the closing `}` — the GBNF grammar should have prevented this, but it didn't fire. The lazy grammar uses `AddLazyGrammar()` with trigger pattern `["<|tool_call|>call:"]`. Problem: the lazy grammar trigger pattern only matches from **position 0 of generated output**, but the model generates free text first (`"I'll search the web..."`) before the tool call tag. So the grammar never activates.

## Issue 3: Multi-Turn Context Failure

**Conversation:**
- User: "Give me some top news headlines"
- Model: "I don't have direct access to real-time news headlines. However, I can help you search. Would you like me to do that?"
- User: "Yes please"
- Model: "The current date and time is Sunday, April 26, 2026 at 9:41 AM."

The model completely lost the conversational thread after two turns and fell back to `get_time` instead of searching for news as agreed. This suggests the model can't maintain tool selection intent across multiple conversation turns.

## Issue 4: Example Argument Name Mismatch

The system prompt example at the end of `ToolRegistry.cs` renders:
```
For multiple arguments: <|tool_call>call:search_web{query:<|"|>weather<|"|>}<tool_call|>
```

But the actual `SearchWebTool.RunAsync` expects `q=value`, not `query=value`. The model sees `query:` in the example but the real tool parses `q:` — this mismatch would cause the model to use the wrong argument name.

## Questions for Review

1. How should tool descriptions be structured for Qwen 2.5 3B to reliably distinguish between similar tools (`search_web` vs `lookup_fact`)?

2. Is there a way to make the GBNF lazy grammar trigger mid-stream (not just at position 0), or is there an alternative approach to constrain tool call output without grammar?

3. For multi-turn context, is the `LLamaTemplate` approach (using GGUF's native chat template) better than manually rendering ChatML format? SharpBot uses it and seems to handle conversational state better.

4. Would switching tool arguments from `key=value` string parsing to JSON (`JsonElement`) reduce parsing failures at this model scale?

5. Are there other LLamaSharp sampling pipeline tricks that would make Qwen 3B's tool calls more reliable?
