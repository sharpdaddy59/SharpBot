# Target Customer

I'm the privacy-conscious household owner from the brief. I don't trust Alexa, I've read the headlines, and I don't want to run Linux. Here's my honest reaction.

**Do I have this problem?** Partly, but I've learned to live with it. I bought cloud speakers anyway. So did half of 12,000 people surveyed, while 52% of them worried the device was listening (https://www.bloomberg.com/news/articles/2019-09-05/users-worry-about-smart-speaker-privacy-keep-on-buying-them). Researchers call people like me "resigned" (https://record.umich.edu/?p=46272). When I didn't buy a speaker, price was the reason, not privacy (https://feeds2.benzinga.com/tech/17/10/10194340/the-top-reason-some-wont-buy-a-smart-speaker-price-not-privacy). The pain I actually feel is a vendor killing my devices. Amazon removed local processing by automatic update (https://newsletter.openhomefoundation.org/ai-voice-control-at-what-cost/), and Insteon left people's hubs dead overnight (https://www.theregister.com/2022/04/19/insteon_cloud_shutdown/). **Most important finding:** the evidence pack contains no one saying "I'd pay for a local box that just works." Under Direct Reddit, HN and HA-forum quotes it says `INSUFFICIENT DATA`. The demand for this exact product is unproven.

**Objections:**

1. **A one-person vendor is the risk I'm running from.** I'd be trusting a part-time solo founder to keep my locks and cameras working. Mycroft ran out of money and sold backer units on eBay (https://www.opensourceforu.com/2023/02/open-source-virtual-assistant-project-mycroft-is-cash-strapped/). Futurehome went bankrupt and then charged a subscription to keep devices working (https://www.techdirt.com/?p=511300). "Free updates forever" from one person sounds like Insteon waiting to happen. Nabu Casa has about 30 staff and is profitable (https://www.builtinnyc.com/company/nabu-casa), so it feels safer than you.

2. **There's already a cheaper option from a trusted name.** HA Green is $199 and Voice PE is $69 (https://www.home-assistant.io/green/ ; https://www.home-assistant.io/voice-pe). That's about $268, with no subscription, from a project with 699k+ installs (https://analytics.home-assistant.io/). You want $300–600 for a product with no home-automation integrations yet (panel-context current state). Homey Pro at €399 and Hubitat at $179.95 already sell "local, no subscription" (https://homey.app/en-ae/wiki/homey-pro-vs-hubitat/).

3. **Voice through Telegram confuses me.** I'd have to open a chat app and record a voice note to turn on a light? My partner already "refuses to open Home Assistant" (https://rl.klein.ruhr/r/homeautomation/comments/1v3db1l/my_partner_refuses_to_open_home_assistant/oz2x7y2/?context=3) and "just want[s] to turn the lights on" (https://community.home-assistant.io/t/a-room-first-home-assistant-frontend-for-the-rest-of-the-household/1020977). Telegram home control exists only as a hobbyist niche (https://hub.docker.com/r/lordraw/hassgram). Telegram is also a cloud service, which undercuts your "cloud-free" pitch. `SPECULATION` on how buyers would read that.

4. **It would be too slow to live with.** Your own box took about 135 seconds to first token (panel-context). A Pi 5 takes 5–8 seconds and people already call that slow (https://www.promptquorum.com/es/power-local-llm/build-local-voice-assistant-2026). Two minutes to answer "what time is it" means I return it. Setup difficulty drives smart-home returns (https://parksassociates.com/blogs/home-systems-and-controls/difficulty-setting-up-smart-home-devices-is-a-significant-driver-of-product-returns4).

**Switching cost:** I'd have to re-pair every device, retrain my family, and bet on an unknown brand. Your box gives me nothing Green plus Voice PE doesn't, except an LLM that doesn't work yet.

**What I'd pay:** $150–250 at most, and only once it controls my existing devices and answers in a few seconds. That price is my own guess (`SPECULATION`), anchored on the $199 Green. At $300–600 I'd pay Homey's price for Homey's track record.

**First impression:** it reads like a bot project hunting for a market. It's "local LLM" first and solves my actual problem second, and "single-stack C#" means nothing to me.

SEVERITY: FATAL
