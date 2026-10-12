# Competitor Strategist

**Kill memo from Head of Strategy, Nabu Casa (Home Assistant).** Nabu Casa is roughly 30 people, profitable and has no outside investors (https://www.builtinnyc.com/company/nabu-casa). Home Assistant has 699,274 opt-in active installs (https://analytics.home-assistant.io/) and more than 2M households.

**Recommendation: do nothing. Distribution gravity will starve this entrant.**

### Objection 1: Their headline claim is something we already give away
"Local, no subscription" is our default. Home Assistant is free and self-hostable, and Cloud is optional (https://www.home-assistant.io/voice-pe ; https://www.nabucasa.com/pricing/). Green ($199) plus Voice PE ($69) comes to about $268, below their $300–$600 price (https://www.home-assistant.io/green/ ; https://www.home-assistant.io/voice-pe). We don't need to cut prices. We are already cheaper, and our voice hardware sells out (https://thepihut.com/collections/all/products/home-assistant-voice-preview-edition).

### Objection 2: Their one real difference copies cheaply, and the hobbyists already did it
Their only feature we don't ship is a "Telegram voice note → local LLM → action" path. We already have a native Telegram integration with `send_voice` (https://www.home-assistant.io/integrations/telegram_chatbot). Community projects (hassgram, an n8n Whisper+Telegram workflow) already glue the rest together (https://n8n.io/workflows/8411-voice-and-text-control-for-home-assistant-using-telegram-whisper-and-gemini/). Making it official is a blueprint or an add-on, which fits in one quarter (SPECULATION on effort). LLM agents already plug into Assist through Ollama (https://www.home-assistant.io/blog/2025/09/11/ai-in-home-assistant). Copying it would cost us almost nothing, but we don't need to, because the entrant has no device integrations at all.

### Objection 3: They have to rebuild what we already have
Home automation (cameras, locks, sensors) is the half of their brief that sells hardware, and their software has zero integrations today (panel-context, current state). Our moat is the integration catalog plus the community that maintains it. A solo part-time founder cannot rebuild that in a year. Their sensible move is to sit on top of Home Assistant, and then they are a reseller of our ecosystem, one of the preconfigured-box sellers already at about $288 (https://alaniazapoved.ru/products/home-assistant-pulcro-tk-qbe-mini-pc-n100-cpu-16gb-lpddr5-ra/205846931).

### Objection 4: Their hardware rules out the "just works" pitch
On their own box, time to first token was about 135 seconds (panel-context). Our users already accept 5–8 s local voice as "slightly slower" (https://www.promptquorum.com/es/power-local-llm/build-local-voice-assistant-2026 ; https://joinhomeshift.com/home-assistant-voice-control/). Buyers who struggle with setup return devices (https://parksassociates.com/blogs/home-systems-and-controls/difficulty-setting-up-smart-home-devices-is-a-significant-driver-of-product-returns4). A 2-minute reply will look broken to them and come back as a return. Rising DRAM prices also squeeze their margin harder than ours (https://www.techradar.com/pro/experts-warn-pc-prices-could-rise-even-more-in-2026-as-ram-costs-set-to-soar-higher).

### Objection 5: The graveyard is full of this exact pitch
Mycroft ran out of cash and sold backer units on eBay at $499 (https://www.opensourceforu.com/2023/02/open-source-virtual-assistant-project-mycroft-is-cash-strapped/). Snips was bought and shut down (https://venturebeat.com/business/sonos-acquires-privacy-conscious-ai-startup-snips-for-37-5-million). Our evidence found no commercially successful small-vendor local-LLM home appliance (evidence pack, INSUFFICIENT DATA).

### What we can't copy, and the niche we'd concede
- **Hard to copy:** an "ask-anything" conversational box whose household setup never touches YAML or dashboards. Our own docs call AI in the smart home "experimental" (https://www.home-assistant.io/voice-pe), and our UX loses non-technical partners ("My partner refuses to open Home Assistant", https://community.home-assistant.io/t/a-room-first-home-assistant-frontend-for-the-rest-of-the-household/1020977).
- **Conceded niche:** privacy-motivated households that will pay a premium for full LLM Q&A with no tinkering, plus the Futurehome and Insteon orphans who distrust any company-run hub (https://www.techdirt.com/?p=511300).
- **Is it big enough to feed them?** INSUFFICIENT DATA. The surveys we have are from 2017–2022 and say price beats privacy (https://feeds2.benzinga.com/tech/17/10/10194340/the-top-reason-some-wont-buy-a-smart-speaker-price-not-privacy). On Tindie the median product had 4 orders (https://blog.tindie.com/?p=8731).

**Steel-man of the threat:** if they execute perfectly, they become the "Home Assistant for the spouse" layer, running on Home Assistant. That helps us more than it hurts.

SEVERITY: FATAL
