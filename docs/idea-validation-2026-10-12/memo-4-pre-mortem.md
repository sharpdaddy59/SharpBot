# Pre-mortem Analyst

**Date: April 2028. The idea is dead.**

## Single most probable cause of death
**The box never became a product that could sell. It was a demo that took two minutes to answer.** At month 12 the founder was still building infrastructure: STT, TTS, home-automation integrations, a mobile app, a Linux image, OTA updates and a Gemma 4 prompt renderer. None of these existed at the start (panel-context, "Current State"). The one measured number on the target hardware was about 135 s to first token for Qwen 2.5 3B with 10 tools. A dev desktop managed about 9 s. The Vulkan path was never tested (panel-context). Pi-class local voice pipelines already feel slow at 5-8 s end to end (https://www.promptquorum.com/es/power-local-llm/build-local-voice-assistant-2026). The few early units went to technical buyers, and they compared the box to Home Assistant. SPECULATION: unit counts unknown.

## Top 3 failure paths, ranked

**1. Scope collapse on a part-time, one-year runway (most probable).**
The brief asks a solo part-timer to ship a voice assistant, a smart-home hub, an iOS/Android app and an appliance OS. Mycroft had a funded team and still ran out of money and failed to deliver Mark II rewards (https://www.opensourceforu.com/2023/02/open-source-virtual-assistant-project-mycroft-is-cash-strapped/). Hardware crowdfunding fails at about 42%, plus 9% cancelled (https://circuithub.com/post/how-to-deliver-your-kickstarter-hardware). Device integrations (cameras, locks, sensors) are the open-ended part. That breadth is the incumbent's moat, and the founder started from zero.

**2. No reason to buy over the incumbent.**
The pitch is "local, no subscription", and Home Assistant already offers that for free (https://www.home-assistant.io/voice-pe). A Green plus a Voice PE costs about $268 (https://www.home-assistant.io/green/ ; https://www.home-assistant.io/voice-pe). HA has 699k+ opt-in installs (https://analytics.home-assistant.io/) and is backed by a profitable company of about 30 people (https://www.builtinnyc.com/company/nabu-casa). Telegram voice notes as the main interface have no mainstream demand evidence, only hobbyist DIY projects (evidence pack, Telegram section). Every privacy-demand survey is from 2017-2022, and they show price ranking above privacy (https://feeds2.benzinga.com/tech/17/10/10194340/the-top-reason-some-wont-buy-a-smart-speaker-price-not-privacy).

**3. Unit economics bled out.**
Wholesale cost for a 32 GB 3550H box is $274 for 1-99 units (https://www.globalsources.com/product/mini-pc_1224414489f.htm). The DRAM shortage may last into 2027 (https://www.techradar.com/pro/experts-warn-pc-prices-could-rise-even-more-in-2026-as-ram-costs-set-to-soar-higher). Setup difficulty drives smart-home returns: 22% of returners cited configuration problems (https://parksassociates.com/blogs/home-systems-and-controls/difficulty-setting-up-smart-home-devices-is-a-significant-driver-of-product-returns4). Free updates forever with no recurring revenue is how Futurehome ended up bankrupt and then charging a subscription (https://www.techdirt.com/?p=511300). INSUFFICIENT DATA on support load per unit.

## Which assumption broke first
**"The 3550H box is good enough."** That was the first assumption to break. The brief treats the target hardware as settled, but the only benchmark says it isn't. The 9 s Windows result was mistaken for the product experience.

## Month-3 warning sign that was ignored
Latency on the Brazos test box never got under a conversational threshold, yet work continued on features rather than the bottleneck. SPECULATION about the founder's behavior. The 135 s figure itself is fact (panel-context).

## What a $100 test would have caught
1. **Free: one weekend running Vulkan/iGPU plus Whisper on the Brazos box**, timing a full voice turn from Telegram voice note to reply. Above about 10 s means the hardware tier or the product is wrong.
2. **About $100: a landing page with ads and a refundable $25-50 deposit**, posted in r/homeassistant and r/LocalLLaMA. Show the price next to "Green + Voice PE, $268". If fewer than a handful of strangers put down a deposit, stop. SPECULATION on thresholds, but no direct "I'd pay for this box" demand quotes were found (evidence pack, Demand, INSUFFICIENT DATA).

**SEVERITY: FATAL**
