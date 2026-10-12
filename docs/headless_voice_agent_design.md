# Headless Voice Agent — Architecture Design

**Author:** Greg
**Status:** Draft for review
**Last updated:** April 26, 2026

---

## 1. Overview

This document describes the architecture for a cross-platform, always-on voice-driven AI agent designed to run on inexpensive mini-PC hardware with a USB webcam (containing speaker and microphone) as the sole user interface. The agent is "headless" — no monitor, no keyboard — and is built in C#/.NET targeting Windows and Linux from a single codebase.

The design prioritizes:

- **Local execution.** All inference runs on-device. No cloud dependency at runtime.
- **Provider portability.** The language model is hidden behind `Microsoft.Extensions.AI.IChatClient` so the underlying LLM can be swapped without touching application code.
- **Phased delivery.** Ship a reliable text-and-tool-use MVP first, add vision later, migrate to a unified multimodal model when the ecosystem matures.

---

## 2. Target Hardware

| Component | Specification |
|---|---|
| CPU | AMD Ryzen 5 3550H (Zen+, 4 cores / 8 threads, 2.1 GHz base) |
| GPU | Radeon Vega 8 Mobile (iGPU, pre-RDNA, shared system RAM) |
| RAM | 16 GB DDR4 |
| Storage | NVMe SSD (assumed) |
| Peripherals | USB webcam with integrated microphone and speaker |
| Network | Optional; agent must function fully offline |

### 2.1 Hardware Implications for Inference

The Vega 8 iGPU is not a viable inference accelerator for this workload. ROCm does not meaningfully support pre-RDNA parts cross-platform, and Vulkan inference on a memory-shared iGPU offers marginal gains over CPU-only inference while contending with the audio subsystem for memory bandwidth. **All inference is planned as CPU-only on Zen+, with iGPU offload as an optional future optimization only after baseline measurements.**

Practical inference budget: **8–10 GB of RAM** is available for model weights and KV caches, after accounting for the OS, audio stack, .NET runtime, application logic, and other models in the pipeline.

### 2.2 Expected Throughput

| Pipeline stage | Realistic throughput on target hardware |
|---|---|
| Whisper.net base.en (INT8) | Approximately real-time |
| Silero VAD (ONNX) | Negligible overhead |
| Qwen 2.5 3B Instruct, Q4_K_M | 5–8 tok/s (sustained) |
| Piper TTS (medium voice) | Faster than real-time |
| Round-trip wake → response audio | 5–12 seconds, dominated by LLM generation |

These numbers are starting estimates and must be validated on the actual target hardware before committing to model selection.

---

## 3. Goals and Non-Goals

### 3.1 Goals

- Voice-driven interaction with a wake word, push-to-stop / barge-in, and conversational continuity across turns.
- Reliable tool calling for a curated set of intents (timers, smart-home actions, information lookups, etc.).
- Cross-platform from a single C#/.NET 10 codebase (Windows and Linux).
- Pluggable LLM behind `IChatClient`. Initial implementation uses Qwen 2.5 3B; future migration to Gemma 4 E4B without application-code changes.
- Optional, low-cost vision pipeline for presence detection.
- Deployable as a Worker Service / systemd unit. No console UI required at runtime.

### 3.2 Non-Goals

- Cloud LLM inference (out of scope for this design; can be a configuration-time backend swap if ever required).
- General-purpose computer-use or screen-reading agent behaviors.
- High-fidelity vision tasks (OCR, scene understanding, multi-person tracking).
- Sub-second voice latency. The hardware does not support it.

---

## 4. Architecture

### 4.1 Component Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Worker Service Host (.NET 10)                    │
│                                                                     │
│  ┌──────────────┐    ┌──────────────┐    ┌─────────────────────┐    │
│  │ Audio I/O    │───▶│  Wake Word   │───▶│  VAD (Silero ONNX)  │    │
│  │ PortAudio    │    │ (Porcupine)  │    │                     │    │
│  └──────────────┘    └──────────────┘    └──────────┬──────────┘    │
│         ▲                                           │               │
│         │ TTS audio                                 ▼               │
│  ┌──────┴───────┐                          ┌───────────────────┐    │
│  │ Piper TTS    │                          │  STT (Whisper.net)│    │
│  │ (process)    │                          │  base.en INT8     │    │
│  └──────┬───────┘                          └─────────┬─────────┘    │
│         │ text                                       │ transcript   │
│         │                                            ▼              │
│         │             ┌──────────────────────────────────────────┐  │
│         │             │      Intent Router (Lightweight)         │  │
│         │             │  Regex/keyword + optional ONNX classifier│  │
│         │             └─────┬────────────────────────────┬───────┘  │
│         │                   │ deterministic              │          │
│         │                   ▼                            ▼ free-form│
│         │           ┌───────────────┐         ┌──────────────────┐  │
│         │           │ Tool Handlers │         │ AIAgent          │  │
│         │           │ (timers, etc.)│         │ (ChatClientAgent)│  │
│         │           └───────┬───────┘         └─────┬────────────┘  │
│         │                   │                       │               │
│         │                   │     Microsoft Agent Framework         │
│         │                   │     ─────────────────────────         │
│         │                   │     IChatClient (swappable)           │
│         │                   │           │                           │
│         │                   │           ▼                           │
│         │                   │     ┌───────────────────┐             │
│         │                   │     │  LlamaSharp       │             │
│         │                   │     │  Qwen 2.5 3B Q4   │             │
│         │                   │     │  GBNF-constrained │             │
│         │                   │     └───────────────────┘             │
│         │                   │                                       │
│         └◀──────────────────┴──────── response text ────────────────│
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  Vision Pipeline (Phase 2, optional)                        │    │
│  │  OpenCvSharp camera → YOLO-nano ONNX → presence events      │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.2 Pipeline Description

The audio pipeline is a state machine: **Idle → Wake → Capture → STT → Route → Respond → TTS → Idle**.

1. **Idle.** PortAudioSharp continuously feeds frames to the wake-word detector. CPU usage at idle is dominated by the wake-word model and should remain below 5%.
2. **Wake.** Porcupine (or openWakeWord) detects the trigger phrase. The system advances to capture mode and begins buffering audio.
3. **Capture.** Silero VAD identifies speech endpoints. Capture stops on a configurable silence threshold (e.g., 700 ms).
4. **STT.** The captured buffer is transcribed by Whisper.net (base.en, INT8). Streaming mode is used so transcription begins before the user finishes speaking.
5. **Route.** The transcript first hits a deterministic intent router. Common commands ("set a timer for X", "what time is it", "turn off the kitchen lights") are matched here and dispatched directly to a tool handler. Only utterances that don't match a known intent fall through to the LLM.
6. **Respond.** The LLM path produces a response, optionally invoking tools through the Agent Framework's function-calling middleware.
7. **TTS.** Piper synthesizes the response. Output is streamed to the speaker via PortAudio. Wake-word detection remains active throughout TTS playback to enable barge-in.

### 4.3 Why a Two-Tier Router

LLMs in the 3B-parameter class are unreliable as tool dispatchers when given a large set of choices. Each retry of a malformed tool call costs a full prompt evaluation, and on CPU-only inference this latency is user-visible. A deterministic first-pass router collapses latency on the 80–90% of utterances that match known patterns, and reserves LLM invocation for genuine free-form conversation. This is the single largest perceived-latency optimization available within the hardware budget.

---

## 5. Component Selection

### 5.1 Audio I/O — PortAudioSharp

NAudio is the most popular .NET audio library but is Windows-only. PortAudio is the long-standing cross-platform C library underlying many open-source audio applications, and PortAudioSharp provides .NET bindings. This abstracts WASAPI on Windows and ALSA/PulseAudio on Linux behind a single API.

### 5.2 Wake Word — Porcupine (commercial) or openWakeWord (OSS)

Running Whisper continuously to detect a wake phrase is wasteful and would dominate CPU. A dedicated wake-word model is required.

- **Porcupine (Picovoice)** is the production-quality choice. Free for personal use, commercial license for shipping products. Has a .NET SDK.
- **openWakeWord** is fully open-source (Apache 2.0), runs as ONNX, and supports custom wake words trained from synthetic data. Requires more integration work but no licensing.

Recommendation: prototype with Porcupine for speed of integration; reassess for shipping.

### 5.3 Voice Activity Detection — Silero VAD

Silero VAD is a small ONNX model (1–2 MB) that detects speech-vs-silence with high accuracy and minimal CPU. It's the standard for deciding when a user has stopped speaking. Runs via Microsoft.ML.OnnxRuntime.

### 5.4 Speech-to-Text — Whisper.net

Whisper.net wraps whisper.cpp and exposes it to .NET. The `base.en` model at INT8 quantization is the right size for this hardware: it fits in roughly 75 MB of RAM, transcribes faster than real-time on Zen+, and is accurate enough for command-and-control plus conversational turns.

`small.en` is more accurate but pushes inference time toward 2× real-time on this CPU and adds noticeable latency. `tiny.en` is faster but accuracy degrades enough that intent routing suffers downstream.

### 5.5 LLM — Qwen 2.5 3B Instruct (Phase 1) → Gemma 4 E4B (Phase 3)

**Qwen 2.5 3B Instruct, Q4_K_M GGUF**, accessed via LlamaSharp.

Rationale for the initial pick:

- Strong tool-calling and structured-output behavior at this size.
- Mature quantization recipes and well-tested GGUFs available.
- Apache 2.0 license.
- Fits comfortably in RAM with room for KV cache and other models.
- LlamaSharp implements `Microsoft.Extensions.AI.IChatClient`, so it slots directly into Microsoft Agent Framework's `ChatClientAgent`.

Alternatives considered: Phi-4-mini (similar quality, less proven on tool calling), Hermes 3 3B (excellent tool use but smaller community).

### 5.6 Tool Calling — GBNF Grammar-Constrained Sampling

LlamaSharp supports GBNF (GGML BNF) grammars on its sampling pipeline. By providing a grammar that matches the expected JSON tool-call schema, the model is mathematically prevented from emitting invalid output. This eliminates the retry loops that plague free-form JSON tool calling on small models.

The Microsoft Agent Framework's `UseFunctionInvocation()` middleware handles dispatch once the model emits a valid tool call. The grammar constraint is applied at the LlamaSharp layer, beneath `IChatClient`, so the rest of the agent pipeline is unaware of it.

### 5.7 Text-to-Speech — Piper

Piper is the de facto open-source TTS for edge devices. It produces natural-sounding speech, runs faster than real-time on this CPU, has voices in many languages, and has no .NET bindings — but it runs cleanly as a child process with stdin/stdout streaming, which is simpler to manage than a P/Invoke wrapper anyway.

### 5.8 Vision (Phase 2) — OpenCvSharp + YOLO-nano ONNX

For the "is the owner present" use case, full computer vision is overkill. A pipeline of OpenCvSharp for camera capture + a small YOLO-nano ONNX model for person detection running at 1–2 frames per second is sufficient and uses minimal CPU. This pipeline runs independently of the audio pipeline and emits presence events to the agent.

---

## 6. Microsoft Agent Framework Integration

### 6.1 Why Agent Framework

Agent Framework is the unified successor to Semantic Kernel and AutoGen and reached version 1.0 with stable APIs and a long-term-support commitment in early April 2026. For this project it provides:

- A consistent agent abstraction (`AIAgent`) decoupled from the underlying model provider.
- Built-in middleware for function invocation, logging, and content filtering.
- Pluggable memory and conversation-state management.
- A migration path: any future move to a hosted LLM, a different local backend, or a multi-agent orchestration is a matter of changing wiring, not rewriting application logic.

### 6.2 Agent Construction

```csharp
using LLama;
using LLama.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// 1. Load the local model via LlamaSharp
var modelParams = new ModelParams(ModelPath)
{
    ContextSize = 4096,
    GpuLayerCount = 0  // CPU-only on this hardware
};

using var weights = LLamaWeights.LoadFromFile(modelParams);
using var context = weights.CreateContext(modelParams);
var executor = new InteractiveExecutor(context);

// 2. Wrap in IChatClient (LlamaSharp ships one in v0.19+)
IChatClient llm = new LLamaSharpChatClient(executor);

// 3. Build the agent with function-invocation middleware
AIAgent agent = llm
    .AsBuilder()
    .UseFunctionInvocation()   // dispatches tool calls automatically
    .UseLogging()              // for diagnostics
    .Build()
    .CreateAIAgent(
        name: "VoiceAgent",
        instructions: SystemPrompt,
        tools: ToolCatalog.AllTools);
```

The `LLamaSharpChatClient` type name has shifted across LlamaSharp versions; verify against the current release.

### 6.3 The IChatClient Swap Point

The entire LLM layer is hidden behind `IChatClient`. In production deployment, this is registered through DI with a factory that reads configuration:

```csharp
services.AddSingleton<IChatClient>(sp =>
{
    var config = sp.GetRequiredService<IOptions<AgentOptions>>().Value;
    return config.Provider switch
    {
        "llamasharp" => CreateLlamaSharpClient(config),
        "ollama"     => new OllamaChatClient(new Uri(config.Endpoint), config.Model),
        "azure"      => CreateAzureOpenAIClient(config),
        _ => throw new InvalidOperationException($"Unknown provider {config.Provider}")
    };
});
```

This is the migration point for Gemma 4: when the ecosystem matures, the configuration changes from `llamasharp` + Qwen GGUF to either `llamasharp` + Gemma 4 E4B GGUF or `ollama` pointed at a local Ollama instance hosting Gemma 4. No application code changes.

---

## 7. KV Cache and Performance Discipline

CPU-only inference on a 3B model is extremely sensitive to KV cache invalidation. The single most impactful performance practice is keeping the prompt prefix byte-identical across turns:

- The system prompt and tool catalog are baked into the initial context once per session.
- Conversation history is **append-only**. Re-injecting "current state" at the top of the prompt invalidates the cache and forces full prompt re-evaluation each turn.
- The `InteractiveExecutor` (or its successor) is created once and reused.
- Tool-result injection is appended as conversation turns, not patched into the system prompt.

A cache hit on prompt prefix can reduce time-to-first-token from several seconds to under 200 ms.

---

## 8. Cross-Platform Concerns

| Concern | Approach |
|---|---|
| Audio backend | PortAudioSharp abstracts Windows (WASAPI) and Linux (ALSA/PulseAudio) |
| Native binaries | LlamaSharp, Whisper.net, ONNX Runtime all ship per-RID native binaries |
| Service hosting | Worker Service registered as Windows Service on Windows, systemd unit on Linux |
| File paths | Use `Path.Combine` and `Environment.SpecialFolder.LocalApplicationData` |
| Piper invocation | Bundle platform-specific Piper binaries; resolve at startup via `RuntimeInformation.IsOSPlatform` |
| Model files | Distribute as separate downloads, not bundled, to keep installer size manageable |

CI should produce both Windows (`win-x64`) and Linux (`linux-x64`) self-contained builds. Native dependencies must be verified end-to-end on both platforms; "compiles on Windows" is not evidence of Linux readiness.

---

## 9. Phased Implementation Plan

### Phase 1 — Voice MVP (initial focus)

- Audio pipeline: PortAudioSharp + Porcupine + Silero VAD + Whisper.net + Piper.
- LLM: Qwen 2.5 3B Q4_K_M via LlamaSharp.
- Microsoft Agent Framework with `ChatClientAgent` and `UseFunctionInvocation`.
- GBNF grammar for tool-call output.
- Two-tier intent router with a small set of deterministic tools (timers, time/date, basic info).
- Worker Service host with systemd and Windows Service deployment scripts.
- **Exit criteria:** End-to-end voice loop on both Windows and Linux; tool-call success rate >95% on the deterministic intent set; LLM-path response latency under 12 seconds for typical replies.

### Phase 2 — Vision and Robustness

- OpenCvSharp + YOLO-nano presence detection pipeline.
- Presence events surfaced as agent context (e.g., "the owner just entered the room").
- Wake-word retraining on real-room audio captures.
- Barge-in support during TTS playback.
- Per-NPC / per-context tool scoping to reduce prompt tokens.

### Phase 3 — Gemma 4 Migration

- Re-benchmark Gemma 4 E4B on target hardware once GGUF quantization recipes have stabilized (target: 3–6 months after initial Gemma 4 release).
- Validate tool-calling end-to-end through the Agent Framework → IChatClient → LlamaSharp/Ollama → Gemma 4 chain.
- Evaluate native audio input as a Whisper.net replacement; benchmark token-cost of audio frames in context vs. dedicated STT.
- Promote vision from a separate ONNX pipeline to native model invocation if quality and latency allow.

### Phase 4 — Production Hardening

- Telemetry via OpenTelemetry (Agent Framework has built-in support).
- Configuration management (appsettings.json + environment overrides).
- Update mechanism for models and binaries.
- Failure modes: graceful degradation when the LLM is unavailable (fall back to deterministic-only mode).

---

## 10. Open Questions and Risks

| Item | Risk | Mitigation |
|---|---|---|
| Vega 8 inference performance | Unknown until measured on target hardware | Benchmark before committing to model size |
| Porcupine licensing for shipping | Commercial product cost | Have openWakeWord ready as a drop-in alternative |
| Audio device variance across mini-PC vendors | Different webcams behave differently with ALSA/WASAPI | Test on at least two distinct hardware samples before declaring GA |
| Whisper.net cold-start time | First transcription is slow (model load) | Pre-load on service startup; keep model resident |
| LlamaSharp API churn | LlamaSharp is pre-1.0 and breaking changes occur | Pin to a tested version; review changelogs before upgrading |
| Gemma 4 tool-call format | Custom protocol; may not be wired through Ollama → Microsoft.Extensions.AI cleanly at first | Validate end-to-end before Phase 3 commit |
| Background noise / far-field audio | Webcam mics are not designed for distant speech | Investigate Krisp or RNNoise as a preprocessing step |

---

## 11. References

### Microsoft Agent Framework

- Overview — https://learn.microsoft.com/en-us/agent-framework/overview/
- GitHub — https://github.com/microsoft/agent-framework
- Version 1.0 announcement — https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/
- Providers overview — https://learn.microsoft.com/en-us/agent-framework/agents/providers/
- Agent based on any IChatClient — https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/chat-client-agent
- Middleware — https://learn.microsoft.com/en-us/agent-framework/agents/middleware/
- .NET Blog introduction — https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/

### Microsoft.Extensions.AI

- IChatClient API reference — https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.ichatclient

### Local Inference

- LlamaSharp — https://github.com/SciSharp/LLamaSharp
- LlamaSharp v0.19 release (IChatClient support) — https://github.com/SciSharp/LLamaSharp/releases
- Provider availability discussion — https://github.com/microsoft/agent-framework/discussions/1230
- Ollama provider docs — https://learn.microsoft.com/en-us/agent-framework/agents/providers/ollama

### Models

- Qwen 2.5 model card — https://huggingface.co/Qwen/Qwen2.5-3B-Instruct
- Gemma 4 model card — https://ai.google.dev/gemma/docs/core/model_card_4
- Gemma 4 announcement — https://blog.google/innovation-and-ai/technology/developers-tools/gemma-4/
- Gemma 4 on Hugging Face — https://huggingface.co/blog/gemma4
- Gemma 4 on Ollama — https://ollama.com/library/gemma4

### Audio / Vision Components

- Whisper.net — https://github.com/sandrohanea/whisper.net
- Silero VAD — https://github.com/snakers4/silero-vad
- Piper TTS — https://github.com/rhasspy/piper
- Porcupine (Picovoice) — https://picovoice.ai/platform/porcupine/
- openWakeWord — https://github.com/dscripka/openWakeWord
- PortAudioSharp — https://github.com/atsushieno/PortAudioSharp2
- OpenCvSharp — https://github.com/shimat/opencvsharp
- ONNX Runtime — https://onnxruntime.ai/

---

## 12. Appendix — Decision Record Summary

| Decision | Choice | Alternative considered | Reason |
|---|---|---|---|
| Audio library | PortAudioSharp | NAudio | Cross-platform requirement |
| Wake word | Porcupine (prototype), openWakeWord (eval for ship) | Continuous Whisper | CPU cost |
| VAD | Silero (ONNX) | WebRTC VAD | Accuracy vs cost |
| STT | Whisper.net base.en INT8 | small.en, tiny.en | Latency/accuracy balance |
| LLM (initial) | Qwen 2.5 3B Q4_K_M | Phi-4-mini, Hermes 3, Gemma 4 E4B | Tool-use reliability + maturity |
| LLM runtime | LlamaSharp | Ollama (local) | Lower process overhead, embedded in app |
| Tool calling | GBNF grammar constraint | Free-form JSON parsing | Eliminates retry loops |
| TTS | Piper | System TTS, Coqui | Quality + cross-platform |
| Vision | OpenCvSharp + YOLO-nano (Phase 2) | Native multimodal LLM | Defer until Gemma 4 phase |
| Agent orchestration | Microsoft Agent Framework 1.0 | Semantic Kernel, raw Microsoft.Extensions.AI | Long-term support + provider portability |
