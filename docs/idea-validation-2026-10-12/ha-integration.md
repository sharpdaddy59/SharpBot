# How SharpBot adds value to an existing Home Assistant install

Follow-up to the [validation report](report.md) (verdict: PIVOT to Home Assistant). Written 2026-10-12 after the question "HA Green could never run local AI, so how would we add this to Home Assistant?"

## The Green objection, answered

The Green ($199, RK3566, 4 GB) cannot run an LLM, and the report glossed over that. But Home Assistant is designed to borrow a brain from another machine. Its built-in Ollama integration points at any server URL on the network, uses that server as the Assist conversation agent, and can hand the model the Assist API so it controls exposed devices through tool calls ([docs](https://www.home-assistant.io/integrations/ollama)). So "pivot to Home Assistant" does not mean "run on the Green". It means SharpBot becomes the thing the Green talks to.

## Three ways SharpBot can plug in

| Option | Shape | For | Against |
|---|---|---|---|
| **1. Brain box beside the hub (recommended)** | SharpBot runs on a separate x86 box (Brazos class) and exposes an Ollama-compatible chat endpoint. HA's Ollama integration consumes it as the Assist conversation agent. SharpBot's Telegram bot calls HA's conversation API for voice notes. | Works with any existing HA install, including Green. Stays 100% C#. HA keeps every device integration, Voice PE handles far-field voice. Reframes the hardware as "the local AI brain for your Home Assistant", a companion rather than a competitor. | Two tool systems (HA's Assist API tools vs SharpBot's built-ins) must be reconciled. Only matters if it beats a stock Ollama install (see below). |
| **2. One x86 box running HAOS with SharpBot as an add-on** | HAOS on generic x86-64; SharpBot packaged as a Supervisor-managed Docker container (self-contained .NET binary). | Cleanest single-box story. Brazos-class hardware has RAM and CPU for both. | Ties the appliance to HAOS. Supervised installs on Ubuntu are officially unsupported, so Ubuntu goes away on that box. |
| **3. Custom integration via HACS** | A thin Python shim registers SharpBot as a conversation agent. | Most control over the agent contract. | Breaks the single-stack preference; second codebase to maintain. Skip unless option 1 hits a wall. |

## What Home Assistant would actually use the local model for

1. **The conversation agent behind Assist.** Without an LLM, Assist is a sentence matcher. "Turn on the kitchen light" works; "it's a bit dark in here" or "turn off everything upstairs except the hallway" fails. With an LLM agent, HA passes the exposed entities as tools and the model interprets fuzzy, compound or contextual requests. HA's "prefer handling commands locally" setting tries the built-in matcher first and falls back to the model, which is the same pattern as SharpBot's `RegexIntentRouter` fast path.
2. **General questions through the same microphone.** Once the model is the agent, Voice PE answers unit conversions, weather, trivia and so on from local data. This is the Alexa-replacement half. HA's own docs still call it experimental.
3. **Services automations can call.** Automations can invoke the conversation agent or the AI Task service to generate text or structured data: "summarize what happened in the house while I was away", "write the notification for this doorbell event", extract a value from a sensor dump. This is where privacy earns its keep, because the input is the home's state history.
4. **Camera image description.** AI Task accepts image attachments, so a vision-capable model can describe a doorbell or driveway frame. Gemma 4 E4B is multimodal, but LlamaSharp's vision path must be verified before promising this.
5. **Automation authoring help.** HA uses the agent to suggest names, icons, areas and automation drafts in the UI. Minor, but free once the agent exists.

## Constraints from the HA docs that shape the design

- HA recommends exposing **fewer than 25 entities** to small models because they make mistakes with large tool lists.
- HA suggests running **two instances of the same model**, one for conversation and one with device control, because small models struggle to do both. This is the same tool-selection weakness SharpBot hit with Qwen in April 2026; the intent router plus a deliberately small device tool list is a direct answer to it.
- Default context is 8k tokens; the Assist API prompt consumes part of that.
- The Assist API device-control path only works with models that support tool calling (Qwen 2.5 and Gemma 4 both qualify; Gemma 4 still needs a custom prompt renderer in SharpBot).

## The honest counterpoint: why not just Ollama?

Ollama already does everything in the list above on any spare PC. SharpBot only matters here if it delivers something Ollama plus a stock model does not. The brain-box pitch can stand on exactly three things:

1. **Usable response time on cheap hardware**, via the intent router fast path and per-conversation KV-cache reuse. Unproven on Brazos (135 s TTFT CPU-only in April; Vulkan untested).
2. **The Telegram voice bridge**, which HA has no built-in equivalent for (only hobbyist glue: hassgram, n8n workflows).
3. **Zero-setup appliance packaging**: plug in, point HA at it from the companion app, done.

If the Brazos latency test fails, item 1 is gone and items 2 and 3 are too thin to sell hardware on. The latency gate in the report's "Cheapest next validation step" therefore still decides everything.

## What this changes about the next step

- The latency gate is unchanged.
- The demand probe gets sharper: the demo is SharpBot answering through Voice PE and a Telegram voice note while a Green does the device control. The deposit page sells a brain box to people who already own HA. That is a smaller leap for a buyer than replacing a hub, and the $268 "Green + Voice PE" figure stops being a competitor's price and becomes the prerequisite.
- Engineering gap to test option 1 is modest and all C#: an Ollama-compatible `/api/chat` endpoint over the existing `LlamaSharpClient`, plus a Telegram-to-HA-conversation-API bridge. Neither needs the Gemma 4 renderer to start.
