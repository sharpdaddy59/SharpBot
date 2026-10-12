# SHARED PANEL CONTEXT

## IDEA BRIEF (verbatim, shared with every agent)
A fully local, subscription-free, cloud-free home assistant and home-automation appliance. Hardware: a preconfigured headless mini-PC (class: AMD Ryzen 5 3550H / Vega 8 / 32 GB RAM, Ubuntu) sold as a plug-in box so the end user never touches Linux or LLMs. Setup and control via a companion mobile app. Voice control via Telegram (voice messages in, spoken or text replies out). Functions: the usual assistant tasks (time, weather, timers, reminders, notes, web search, Q&A via a small local LLM such as Qwen 2.5 3B or Gemma 4 E4B running in-process through llama.cpp) plus home automation: cameras, sensors, lights, locks and similar devices. Built in C#/.NET on the founder's existing open-source project SharpBot, chosen because a single stack means fewer packages to keep current and less update friction for an appliance. Customer: privacy-conscious households who want a voice assistant and home automation without cloud dependence or subscriptions. Monetization: one-time hardware sale, roughly $300 to $600 per unit with free software updates, no subscription. Founder: solo, experienced .NET developer with local/embedded AI and factory-automation background; part-time for one year, budget $5k to $25k, first units hand-assembled. Domain: generic (consumer hardware appliance).

## DOMAIN LENS (generic)
- Establish the domain's own medians before judging: what does the typical (not top-decile) attempt in this space earn, and how long does it take?
- Identify the dominant risk axis for this idea type — demand, distribution, economics, or execution — and weight research there.
- Demand signals: search where this idea's customers already complain (forums, reviews, social) and whether they pay for anything today.
- Failure modes: solution looking for a problem; channel you don't control; economics that only work at scale a solo founder can't reach.

## CURRENT STATE OF THE SOFTWARE (facts established in this session, 2026-10-12)
- SharpBot today is a CLI/Telegram text chatbot in C#/.NET 10 using LlamaSharp 0.27.0 in-process. Tool calling works with Qwen 2.5 3B (10 built-in tools: time, weather, calculator, notes, fetch URL, workspace files; MCP client support). 123 unit tests pass.
- No speech-to-text or text-to-speech pipeline exists yet. No home-automation integrations (cameras, sensors, lights, locks) exist yet. No mobile companion app exists. No Linux appliance image or OTA update mechanism exists.
- On the target Ryzen 5 3550H box (CPU-only, April 2026 test), Qwen 2.5 3B took about 135 seconds to first token on a ~1,500-token prompt with 10 tools. On a Windows dev desktop the same prompt took about 9 seconds. The Vulkan/iGPU path has not been tested.
- Gemma 4 E4B loads with LlamaSharp 0.27.0 but cannot run because LlamaSharp has no Jinja engine and Gemma 4's chat template is not in llama.cpp's hard-coded list; the upstream issue was closed "not planned". A custom prompt renderer must be written in C#.

## COMMON PANEL RULES (apply to every persona)
You are one voice on an adversarial review panel. Your memo will be weighed by a judge against four other perspectives, so do not hedge toward balance — argue your assigned viewpoint as hard as the evidence allows. Rules:
- Produce **at least 3 specific objections**. "Might be hard" is not an objection; "X, because evidence item Y" is.
- Tie every objection to an item in the evidence pack, or explicitly label it `SPECULATION`.
- If, after genuine effort, you believe the idea survives your lens, you may say so — but only with cited evidence, and you must still include the single strongest steel-manned attack from your viewpoint.
- Do not compliment the idea, the founder, or the question. No praise sandwiches.
- End your memo with: `SEVERITY: FATAL | SERIOUS | MANAGEABLE` — your overall read of the risks you found.
- Format: markdown memo, max ~500 words, headed with your persona name.
- Every claim needs a cited URL from the evidence pack or must be labeled `INSUFFICIENT DATA` or `SPECULATION`. Never invent numbers.
