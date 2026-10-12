# Idea Validation: SharpBot Local Home-Assistant Appliance

**Date:** 2026-10-12
**Domain:** generic (consumer hardware appliance)
**Verdict:** PIVOT

The appliance as described is a NO-GO. I did not reach that by counting five FATAL memos. It rests on three evidence-backed findings that hold however thin the demand research was.

1. **The incumbent already offers the headline promise, for less.** Home Assistant gives away "local, no subscription", and Green ($199) plus Voice PE ($69) costs about $268 (https://www.home-assistant.io/green/ ; https://www.home-assistant.io/voice-pe).
2. **Hardware cost leaves almost no room.** A 32 GB 3550H unit costs $274 wholesale at 1–99 units (https://www.globalsources.com/product/mini-pc_1224414489f.htm), and DRAM prices are still rising (https://www.techradar.com/pro/experts-warn-pc-prices-could-rise-even-more-in-2026-as-ram-costs-set-to-soar-higher).
3. **The product does not work yet.** Six required components do not exist, and the one benchmark on the target box shows about 135 s to first token (panel context).

The panel's demand case is weaker than its tone. It rests on 2017–2022 cloud-speaker surveys and on missing forum quotes, which is absence of evidence, not evidence of absence.

**The pivot:** stop building a competing appliance. Ship SharpBot as an open-source conversational and voice layer on top of Home Assistant: a Telegram voice-note-to-action bridge plus a local-LLM conversation agent, as an HA add-on. Defer hardware until the add-on shows pull.
- **Evidence for:** HA has 699,274+ opt-in installs (https://analytics.home-assistant.io/). HA calls AI in the smart home "experimental" (https://www.home-assistant.io/voice-pe). No built-in path for acting on a voice note was found, though HA has a Telegram integration (https://www.home-assistant.io/integrations/telegram_chatbot). Memos 3 and 5 both land here independently.
- **Still unknown:** whether anyone pays for it. This pivot most plausibly yields users and stars, not the $300–600 hardware revenue in the brief.

## Market size and competitors (answering the user's question)

- **Addressable base:** Home Assistant has 699,274 opt-in active installs, which is a floor (https://analytics.home-assistant.io/), and over 2M households (https://www.builtinnyc.com/company/nabu-casa). The size of the privacy-focused smart-home market is INSUFFICIENT DATA; no market report was searched.
- **Direct competitors:**
  - HA Green + Voice PE, about $268
  - Homey Pro, €399 / $449 one-time (https://homey.app/en-ae/wiki/homey-pro-vs-hubitat/)
  - Hubitat C-8 Pro, $179.95 (https://www.promptquorum.com/es/smart-home/best-smart-home-hubs-2027)
  - HAOS-preloaded N100 boxes, about $288 (https://alaniazapoved.ru/products/home-assistant-pulcro-tk-qbe-mini-pc-n100-cpu-16gb-lpddr5-ra/205846931)
- **Subscription alternatives:**
  - Alexa+, $19.99/mo or free with Prime (https://www.techradar.com/home/smart-home/alexa-plus-subscription-pricing-how-much-it-costs)
  - Google Home Premium, $10–20/mo (https://www.channelnews.com.au/google-takes-on-amazon-with-gemini-powered-smart-home-overhaul/)
- **Premium installer channel:** Josh.ai, from $30/mo (https://josh.ai/stories/joshai-debuts-scalable-software-plans-for-every-project)
- **Adjacent one-time-price box:** Umbrel Home, $699 (https://www.nasdaq.com/articles/umbrel-announces-new-umbrel-home-plug-and-play-server)
- **Failed predecessors:**
  - Mycroft (https://www.opensourceforu.com/2023/02/open-source-virtual-assistant-project-mycroft-is-cash-strapped/)
  - Snips (https://venturebeat.com/business/sonos-acquires-privacy-conscious-ai-startup-snips-for-37-5-million)
  - Futurehome (https://www.techdirt.com/?p=511300)

## Scorecard

| Dimension | Rating | Basis |
|---|---|---|
| Demand (real, frequent, paid-for pain) | INSUFFICIENT DATA | People pay for local voice (Voice PE sell-outs, possibly supply-limited: https://thepihut.com/collections/all/products/home-assistant-voice-preview-edition), but no evidence exists for this appliance, and no 2024–26 privacy-purchase data was found. |
| Competition (room to win) | LOW | HA offers the same claim free, and the $268 bundle undercuts the price. The founder has zero device integrations against HA's catalog (https://www.home-assistant.io/voice-pe ; panel context). |
| Economics (path to the income goal at medians) | LOW | $274 wholesale against a $300–600 price, DRAM costs up 15–20% (sources above), returns up to 14.9% (https://eightx.co/blog/average-electronics-return-rate-benchmarks), and unfunded free updates. |
| Distribution (a channel that actually reaches customers) | LOW | Median Tindie product: 4 orders (https://blog.tindie.com/?p=8731). Hardware crowdfunding: 42% fail (https://circuithub.com/post/how-to-deliver-your-kickstarter-hardware). The founder owns no channel. |
| Founder fit (skills, hours, opportunity cost) | LOW for the appliance, HIGH for the pivot | Core software skills are present. STT/TTS, integrations, mobile app, OTA and a fast enough box are all missing (panel context). Weekly hours are INSUFFICIENT DATA. |

Only one dimension is INSUFFICIENT DATA, so a NEEDS-EVIDENCE verdict is not forced. The three LOW ratings are evidence-backed and would not change even if demand proved real.

## Top 3 risks (ranked)

1. **The scope is too big for a part-time year.** The brief needs a voice stack, a smart-home hub, mobile apps and an appliance OS, all starting from text chat. Raised by memos 4 and 5. Evidence: panel context; Mycroft (a funded team) failed (URL above).
2. **The incumbent gives away the value proposition.** Raised by memos 1, 2 and 3. Evidence: https://www.home-assistant.io/voice-pe ; https://analytics.home-assistant.io/
3. **One-time revenue with free updates for lock and camera control creates a support and security liability with no money behind it.** Raised by memos 1, 4 and 5. Evidence: Futurehome (https://www.techdirt.com/?p=511300). Support load per unit is INSUFFICIENT DATA.

## Strongest point in favor

The incumbent admits a gap and users report it:
- HA calls smart-home AI "experimental" (https://www.home-assistant.io/voice-pe).
- Non-technical household members reject HA's UI (https://community.home-assistant.io/t/a-room-first-home-assistant-frontend-for-the-rest-of-the-household/1020977).
- Amazon removed local processing from Echo via automatic update (https://newsletter.openhomefoundation.org/ai-voice-control-at-what-cost/).
- A one-time-price, free-update box (Umbrel Home, $699) exists as a model.
- A privacy-focused local voice hub (Atmos Home) raised over $1.2M (https://www.cepro.com/news/atmos-home-debuts-privacy-focused-voice-control-hub/26512/).

So the gap exists. The evidence says the way to reach it is on top of HA, not against it.

## Alternatives considered

1. **HA add-on / Telegram voice bridge (recommended pivot).**
   - For: 699k+ HA installs and no built-in voice-note-to-action path (https://analytics.home-assistant.io/ ; https://www.home-assistant.io/integrations/telegram_chatbot).
   - Against: hobbyist versions already exist (https://hub.docker.com/r/lordraw/hassgram ; https://n8n.io/workflows/8411-voice-and-text-control-for-home-assistant-using-telegram-whisper-and-gemini/), and there is no revenue evidence.
2. **Software-only image on recommended hardware.** The founder sells nothing physical; buyers buy their own mini-PC.
   - For: removes the BOM, FCC and returns exposure (https://compliancegate.com/?p=7230 ; https://parksassociates.com/blogs/home-systems-and-controls/difficulty-setting-up-smart-home-devices-is-a-significant-driver-of-product-returns4).
   - Against: still competes with free HAOS, and the setup burden moves back to the buyer.
3. **Pure open source for stars.**
   - For: matches the founder's stated motive (user profile), with zero support liability.
   - Against: no income. Willow and Rhasspy stalled without funding (https://github.com/toverainc/willow-inference-server ; https://sumguy.com/home-assistant-voice-wyoming-vs-rhasspy/).
4. **Dealer/installer niche (a Josh.ai-style channel).**
   - For: installers sell $500–800 hardware inside $3k–10k installs (https://restrepoinnovations.com/blog/josh-ai-for-crestron.html).
   - Against: that figure is a competitor's opinion, dealer margins and volume are INSUFFICIENT DATA, and a solo part-timer cannot run a dealer network (SPECULATION).

## Objections dismissed

- **"Demand is disproven" (memos 1, 2, 4):** downgraded. The cited surveys are 2017–2022 and about cloud speakers (https://feeds2.benzinga.com/tech/17/10/10194340/the-top-reason-some-wont-buy-a-smart-speaker-price-not-privacy). They are stale and off-target. Missing Reddit quotes reflect impaired research, not proof of no demand.
- **"Two minutes to answer 'what time is it'" (memos 2, 4):** overstated. The 135 s figure is for a ~1,500-token, 10-tool prompt on CPU only, and Vulkan is untested (panel context). It is a real red flag, but not yet the product's latency.
- **"Our voice hardware sells out" as proof of demand (memo 3):** the evidence notes stock-outs may reflect limited supply, and no unit counts exist.
- **"HA could copy it in one quarter" (memo 3)** and **"$150–250 maximum willingness to pay" (memo 2):** kept in view but labeled SPECULATION.
- **"Single C# stack is invisible to buyers":** not dismissed. It stands, and no evidence says otherwise.

## What evidence would change this verdict

- **A full Telegram voice-note-to-reply turn under about 10 s on the 3550H via Vulkan.** This would raise Founder fit for any hardware variant.
- **Direct forum or HN posts from non-technical households asking for a preconfigured local LLM box, or 2024–26 survey data on local AI as a purchase driver.** This would move Demand off INSUFFICIENT DATA.
- **Unit sales for Umbrel Home, Homey or Voice PE.** This would let Economics and Distribution be judged against real medians.
- **Refundable deposits from strangers at $300+ despite the $268 HA comparison** (see the next step). This would reopen the appliance option.

## Cheapest next validation step

This takes one week and less than $100. All thresholds below are set by the judge in advance; they are not derived from the evidence.

1. **Latency gate (days 1–2, free).** Time 10 Telegram voice-note turns (STT, Qwen 2.5 3B, reply) on the Brazos box using Vulkan.
   - **Fail:** median above 10 s. Stop all hardware plans.
2. **Demand probe (days 3–7, under $100).** Post a 2-minute demo video of SharpBot controlling an HA light from a Telegram voice note to r/homeassistant and the HA community forum. Link one page that offers two things side by side, with "Green + Voice PE = $268" shown next to the box price:
   - (a) a free add-on waitlist
   - (b) a $25 refundable deposit for a preconfigured box
3. **Read the result:**
   - **Pivot passes:** 100+ add-on signups.
   - **Appliance stays alive:** 5+ deposits from strangers.
   - **Appliance is dead:** fewer than 2 deposits.

## Panel severity summary

| Persona | Severity |
|---|---|
| Skeptical Investor | FATAL |
| Target Customer | FATAL |
| Competitor Strategist | FATAL |
| Pre-mortem Analyst | FATAL |
| Solo-Operator Realist | FATAL (as scoped) |

## Appendix: research gaps

- **Small-vendor local-LLM appliance successes:** search Kickstarter, Indiegogo, Crowd Supply, r/homeassistant and r/LocalLLaMA.
- **Nabu Casa revenue, Green/Voice PE units and Cloud subscriber count:** check Open Home Foundation annual reports, HA State of the Union posts and analytics.home-assistant.io.
- **HA Yellow price/status and the 2026 voice roadmap:** check the home-assistant.io blog.
- **Mycroft Mark II units delivered, and OVOS/Neon traction:** check the OVOS blog and Mycroft backer updates.
- **Willow maintenance status:** check GitHub commit history.
- **Official Hubitat, Homey and SmartThings US prices, and Amazon/Google local-processing claims:** check vendor pages.
- **Josh.ai hardware pricing, dealer margins and install base:** ask dealers or Josh.ai sales.
- **Apple home hub launch and price:** check Apple newsroom.
- **Real Qwen 2.5 3B and Gemma 4 E4B performance on the 3550H:** benchmark on Brazos with Vulkan.
- **Direct "I'd pay for a local box" quotes:** search Reddit, HN and the HA forum by hand.
- **2025–26 cloud shutdowns (MyQ, Wink, Nest Hub), Alexa+ complaints and adoption:** search news and r/alexa.
- **Privacy smart-home market reports and 2024–26 purchase-driver surveys:** check Parks Associates and Statista.
- **Google Trends and GitHub star growth (home-llm, Willow, OVOS, Assist):** check trends.google.com and star-history.com.
- **Umbrel and Start9 unit sales, and CasaOS/HAOS monetization:** check company posts and community forums.
- **Kickstarter smart-home median raise and fulfilment rate:** check Kicktraq.
- **Current Amazon price of a 32 GB 3550H mini-PC:** check Amazon or camelcamelcamel.
- **Product liability insurance for a small hardware seller:** get broker quotes.
- **App-store fees and mobile app maintenance effort:** check Apple and Google developer pages.
- **Support load per unit:** use HA forum volume as a proxy.
- **Linux image and OTA maintenance cost:** prototype with Mender or RAUC and time it.
- **Whether buyers value "single stack, fewer packages":** ask in the demand probe.
- **FCC status of reselling an unmodified certified PC:** read FCC KDB 896810.
- **Evidence of small vendors netting $25–50k a year, and unit math at $450:** compute after the BOM, fee, return and support figures above are verified.

---

*Method: three research agents (competitors, demand, distribution/economics) gathered web evidence; five isolated adversarial personas wrote memos against the evidence pack; a judge synthesized the verdict. The demand research was thin (about 8 searches, few direct forum quotes), which the judge accounted for.*

## Follow-up

- [How SharpBot adds value to an existing Home Assistant install](ha-integration.md): integration options (brain box, HAOS add-on, HACS shim), what HA would use a local model for, and why the pitch only stands if it beats stock Ollama.
