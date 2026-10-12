# SharpBot v0.4.1 — Brazos validation plan

**For:** OpenClaw agents running on Brazos (Ryzen 5 3550H mini-PC, Ubuntu 24.04, ROCWARE RC08 USB webcam attached).
**Goal:** Validate that the freshly-released SharpBot v0.4.1 actually works end-to-end on this hardware, capture performance baselines, and de-risk upcoming voice work.

**Note on raw output:** verbatim terminal output is much more useful than summaries. Don't paraphrase — paste the raw data, even if it looks ugly.

---

## Step 0 — Pull down the new release first

The previous SharpBot build that may be on the box is stale. Use **v0.4.1** specifically — that's the build under test.

```bash
cd ~
rm -rf sharpbot-v0.4.1   # if a prior attempt is sitting around
mkdir sharpbot-v0.4.1 && cd sharpbot-v0.4.1
wget https://github.com/sharpdaddy59/SharpBot/releases/download/v0.4.1/sharpbot-v0.4.1-linux-x64.tar.gz
tar xzf sharpbot-v0.4.1-linux-x64.tar.gz
cd sharpbot
./sharpbot --version
```

**Expected:** `--version` prints `0.4.1`. If it prints something else, you're running the wrong binary.

---

## Bucket 1 — End-to-end smoke test (~15 min)

The Linux build linked cleanly on the GitHub runner, but linking ≠ running. Confirm the binary actually works on real hardware against the existing local model.

### 1a. Doctor

```bash
./sharpbot doctor
```

**Capture:** the entire output verbatim. Any red lines, warnings, or "missing" reports are interesting.

### 1b. Chat REPL with the existing Qwen GGUF

```bash
SharpBot__Llm__ModelPath=/home/wily/ai/models/qwen-2.5-3b-instruct-q4_k_m.gguf ./sharpbot chat
```

Once at the `you >` prompt, run these three turns and capture raw terminal output for each:

1. **Intent router fast-path test:**
   Type: `what time is it`
   **Expected:** reply in under 1 second, *no* token-by-token streaming visible (the router skipped the LLM). The reply should include the current date/time.

2. **LLM-driven plain reply with streaming:**
   Type: `Tell me a short joke about computers.`
   **Expected:** tokens stream visibly one chunk at a time. The status line at the end of the response should look like `(ttft 1.2s, total 4.5s)` — capture both numbers.

3. **LLM-driven tool call:**
   Type: `What is 47 times 83?`
   **Expected:** model emits a `<tool_call>` for `core.calculator`, the REPL shows a `→ core.calculator(...)` trace line, then a final reply with the answer (3901).

Press Enter on an empty line or Ctrl+C to exit the REPL.

**Failure-mode notes:** if anything fails — stack trace, silent hang, wrong answer, garbled output — that's *more* valuable than a clean run. Capture the full error and what you typed to trigger it.

---

## Bucket 2 — Performance baselines (~30 min)

These numbers are the reference for "what should this hardware sustain" going forward, and they directly inform model-size decisions for the voice-agent work.

All measurements happen inside the chat REPL with the same model path as above.

### 2a. Cold start

Time from launching `./sharpbot chat` to the `Ready.` line appearing.

```bash
time SharpBot__Llm__ModelPath=/home/wily/ai/models/qwen-2.5-3b-instruct-q4_k_m.gguf ./sharpbot chat <<< ''
```

(The empty heredoc immediately exits the REPL after Ready.)
**Capture:** the `real` time reported by `time`.

### 2b. Time-to-first-token across turns

In a single chat session, run these prompts in order and capture the `ttft Xs, total Ys` status line for each:

1. `Hi there.` *(first turn — cold KV cache)*
2. `Tell me a fun fact.` *(second turn — should hit warm KV cache, ttft should drop dramatically)*
3. `Another one please.` *(third turn — same conversation, fully warm)*
4. `One more.` *(fourth turn)*

**Capture:** all four `(ttft Xs, total Ys)` lines.

What we want to see: turn 1's ttft is several seconds (full prefill), turns 2–4's ttft drops to well under a second (KV-cache reuse working). If turns 2–4 are still slow, the cache isn't reusing and that's a finding.

### 2c. Sustained tok/s

Same chat session, ask for a longer reply:

```
Write me a 200-word story about a curious cat exploring a library.
```

**Capture:** the full reply (so we can count tokens) and the `(ttft Xs, total Ys)` line. Rough tok/s = (reply length in chars / 4) / total seconds.

### 2d. Memory footprint

While the chat REPL is idle (showing `you >`):

```bash
ps -o pid,rss,vsz,comm -p $(pgrep sharpbot)
```

**Capture:** that line. Then start a long generation (e.g., the 200-word story prompt) and *while it's still generating*, run the same `ps` again in another terminal. **Capture:** the second line.

What we want: idle RSS, peak RSS during generation. The headless-voice-agent design assumed 8–10 GB for inference; let's see what actually happens.

---

## Bucket 3 — Audio + Vulkan pre-voice de-risk (~10 min)

Cheap checks that confirm assumptions before voice work starts.

### 3a. ALSA enumeration

```bash
arecord -l
aplay -l
```

**Capture:** both outputs verbatim.

What we want to see: the ROCWARE RC08 (USB Audio) is enumerated cleanly as one of the cards (probably card 1). No "device busy" or permission errors. If it shows up under a different card number than expected, that's fine — we just want to know its actual position.

### 3b. RC08 quick mic check (optional but useful)

```bash
arecord -D plughw:1,0 -d 3 -f cd test.wav 2>&1 | tail -5
ls -l test.wav
```

(Adjust `plughw:1,0` if the RC08 is a different card per 3a.)

**Capture:** the output and the resulting file size. A 3-second 44.1 kHz stereo WAV should be roughly 530 KB. If it's much smaller or `arecord` errors out, that's a finding.

### 3c. Vulkan / RADV health

```bash
vulkaninfo --summary 2>&1 | head -40
```

**Capture:** the output.

What we want: a `GPU0` entry naming the AMD Radeon Vega 8 Mobile (RADV), driver version present, no `ERROR` lines.

---

## What to send back

A single text dump (or attached file) with each section's terminal output, in the order above. Format-flexible — copy-paste from terminal is fine. Do NOT clean up or paraphrase; raw output is more diagnostic than summary.

If a section fails or doesn't apply (e.g., RC08 unplugged), say so and skip it; don't fake numbers.

Estimated total time: **45–60 minutes** at a relaxed pace.
