# Solo-Operator Realist

**Bottom line:** the scope is too big for one part-time person working for one year, and the economics give no money back to pay for the "free updates forever" promise.

**1. The build scope is several products, and only one exists today.** SharpBot has text chat and tool calling and nothing else (panel-context, current state). The brief also needs:
- speech-to-text and text-to-speech
- camera, sensor, light and lock integrations
- iOS and Android companion apps
- a Linux appliance image with OTA updates
- a custom C# prompt renderer for Gemma 4
- a fix for the 135-second time-to-first-token on the target box

None of the last six exist. Mycroft, a funded team, ran out of money before it could deliver its own local voice hardware (https://www.opensourceforu.com/2023/02/open-source-virtual-assistant-project-mycroft-is-cash-strapped/). The founder's weekly hours are INSUFFICIENT DATA. SPECULATION: getting even voice plus Home Assistant bridging plus a working image to a sellable state would use most of a part-time year, leaving nothing for launch, support or marketing.

**2. The founder hasn't shown the skills that sell and support consumer hardware.** There is no evidence of consumer hardware sales, mobile app publishing, customer support or paid marketing.
- **Setup and returns.** 27% of smart-home returners cited installation difficulty and 22% cited configuration problems (https://parksassociates.com/blogs/home-systems-and-controls/difficulty-setting-up-smart-home-devices-is-a-significant-driver-of-product-returns4). One vendor puts smart-home return rates at 14.9% (https://eightx.co/blog/average-electronics-return-rate-benchmarks). Each return is unpaid support time.
- **Compliance.** The assembler or importer can become the FCC responsible party (https://compliancegate.com/?p=7230).
- **Distribution.** The median Tindie product got 4 orders (https://blog.tindie.com/?p=8731). Closing this gap means paid marketing or many hours of community selling. The founder has budget for neither on top of the build.

**3. The unit economics can't pay for maintenance.** A 32 GB Ryzen 5 3550H unit costs $274 wholesale in quantities of 1-99 (https://www.globalsources.com/product/mini-pc_1224414489f.htm), and DRAM prices are still rising (https://www.techradar.com/pro/experts-warn-pc-prices-could-rise-even-more-in-2026-as-ram-costs-set-to-soar-higher).
- A $25k budget buys about 90 units at most, and that is before tooling and insurance (insurance cost is INSUFFICIENT DATA).
- At $450, gross margin is about $176 per unit before fees, shipping, returns and support (SPECULATION arithmetic on cited figures).
- SPECULATION: selling all 90 units gives about $16k gross margin at best, and the realistic case is far fewer units, per the Tindie median.

The income goal is INSUFFICIENT DATA. Even a modest $25-50k net per year has no supporting evidence (evidence pack, "None found"). There is also no subscription, so after the year, OTA updates and security patches for locks and cameras become unpaid work with no end date. Futurehome shows how this ends: after its bankruptcy, owners had to pay a new subscription or lose device control (https://www.techdirt.com/?p=511300).

**4. It costs the founder the project they actually chose.** SharpBot's stated purpose is open-source stars and community traction, not revenue (memory, unconfirmed). This plan turns it into a support-heavy commercial appliance competing with free Home Assistant, which has 699,274+ active installs (https://analytics.home-assistant.io/) and already has a Telegram integration (https://www.home-assistant.io/integrations/telegram_chatbot). A SharpBot add-on or Telegram voice bridge for Home Assistant would reach that installed base with no hardware, inventory or returns. SPECULATION: that is the better bet for both stars and possible revenue.

**5. Two weeks of illness breaks things a solo operator can't cover.** The product controls **locks and cameras**. A critical bug or failed OTA push during an absence means a door that won't open, and nobody else can fix it. SPECULATION: the support queue from a 15% return-rate category would also back up during the absence (eightx, above).

**Strongest steel-man against my position:** limit year one to roughly 20 hand-assembled units sold to Home Assistant hobbyists. That keeps the money at risk inside the budget. The founder's factory-automation background may cover device integration better than I assume. Mobile apps and appliance OTA updates remain undemonstrated.

SEVERITY: FATAL (as scoped for a one-person, part-time year)
